using System;
using System.Collections.Generic;
using UnityEngine;

namespace Gameplay.LD.ProceduralGeneration
{
    public class RoomVariant
    {
        public RoomData data;
        public int rotation;
        public Vector2Int[] pos;
        public Socket[][] sockets;
        public Vector2Int pivot;
    }

    public class PlacedRoom
    {
        public int id;
        public RoomVariant variant;
        public Vector2Int origin;
        public Vector2Int PivotCell => origin + variant.pivot;
    }

    public class MapResult
    {
        public List<PlacedRoom> rooms;
        public PlacedRoom start;
        public PlacedRoom final;
        public int pathLength;
        public int attempts;
    }

    public class MapGenerator
    {
        public int width = 4;
        public int height = 4;

        public Vector2Int startOrigin = Vector2Int.zero;
        public int minCellDistance = 3;

        public int minRoomDistance = 4;
        public bool requireAllReachable = true;
        public int maxAttempts = 300;
        public int solverBudget = 20000;
        public int maxVoidCells = 16;
        public int voidPercent = 30;

        private static readonly Vector2Int[] Delta = { new Vector2Int(0, -1), new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0) };

        private const int VoidId = -1;
        private static readonly Socket[] VoidSockets = { Socket.Wall, Socket.Wall, Socket.Wall, Socket.Wall };

        struct Cell
        {
            public int roomId;
            public Socket[] sockets;
        }

        readonly Dictionary<Vector2Int, Cell> grid = new Dictionary<Vector2Int, Cell>();
        readonly List<PlacedRoom> placed = new List<PlacedRoom>();
        readonly Dictionary<RoomData, int> used = new Dictionary<RoomData, int>();
        readonly Dictionary<RoomData, List<RoomVariant>> roomVariants = new Dictionary<RoomData, List<RoomVariant>>();

        int nextId;
        int nodes;
        int voidCount;

        static List<RoomVariant> GenerateRoomVariants(RoomData d)
        {
            var result = new List<RoomVariant>();
            var seen = new HashSet<string>();

            int n = d.cells.Count;
            var pos = new Vector2Int[n];
            var sock = new Socket[n][];
            for (int i = 0; i < n; i++)
            {
                pos[i] = d.cells[i].pos;
                sock[i] = d.cells[i].ToArray();
            }
            var pivot = Vector2Int.zero;

            int rots = d.allowRotation ? 4 : 1;
            for (int r = 0; r < rots; r++)
            {
                if (r > 0)
                {
                    for (int i = 0; i < n; i++)
                    {
                        pos[i] = new Vector2Int(-pos[i].y, pos[i].x);
                        var s = sock[i];
                        sock[i] = new[] { s[3], s[0], s[1], s[2] };
                    }
                    pivot = new Vector2Int(-pivot.y, pivot.x);
                }

                var first = pos[0];
                for (int i = 1; i < n; i++)
                {
                    if (pos[i].y < first.y || (pos[i].y == first.y && pos[i].x < first.x))
                    {
                        first = pos[i];
                    }
                }

                var variant = new RoomVariant
                {
                    data = d,
                    rotation = r,
                    pos = new Vector2Int[n],
                    sockets = new Socket[n][],
                    pivot = pivot - first,
                };

                for (int i = 0; i < n; i++)
                {
                    variant.pos[i] = pos[i] - first;
                    variant.sockets[i] = (Socket[])sock[i].Clone();
                }

                var order = new List<int>();
                for (int i = 0; i < n; i++)
                {
                    order.Add(i);
                }
                order.Sort((a, b) => variant.pos[a].y != variant.pos[b].y ? variant.pos[a].y.CompareTo(variant.pos[b].y) : variant.pos[a].x.CompareTo(variant.pos[b].x));
                var key = new System.Text.StringBuilder();
                foreach (int i in order)
                {
                    key.Append(variant.pos[i].x).Append(',').Append(variant.pos[i].y).Append(':');
                    foreach (var s in variant.sockets[i]) key.Append((int)s);
                    key.Append(';');
                }
                if (seen.Add(key.ToString())) result.Add(variant);
            }

            return result;
        }

        List<RoomVariant> Variants(RoomData d)
        {
            if (!roomVariants.TryGetValue(d, out var list))
                roomVariants[d] = list = GenerateRoomVariants(d);
            return list;
        }

        bool InBounds(Vector2Int p) => p.x >= 0 && p.y >= 0 && p.x < width && p.y < height;

        bool CanPlace(RoomVariant v, Vector2Int o)
        {
            var self = new HashSet<Vector2Int>();
            foreach (var t in v.pos)
            {
                var p = t + o;
                if (!InBounds(p) || grid.ContainsKey(p)) return false;
                self.Add(p);
            }

            for (int i = 0; i < v.pos.Length; i++)
            {
                var p = v.pos[i] + o;
                for (int d = 0; d < 4; d++)
                {
                    var q = p + Delta[d];
                    var s = v.sockets[i][d];

                    if (self.Contains(q)) continue;
                    if (!InBounds(q)) { if (s == Socket.Door) return false; continue; }

                    if (grid.TryGetValue(q, out var other))
                    {
                        if (other.roomId == VoidId)
                        {
                            if (s == Socket.Door) return false;
                            continue;
                        }
                        var os = other.sockets[(d + 2) % 4];
                        if ((s == Socket.Door) != (os == Socket.Door)) return false;
                        if (s == Socket.Window || os == Socket.Window) return false;
                    }
                }
            }
            return true;
        }

        PlacedRoom Place(RoomVariant v, Vector2Int o)
        {
            var pr = new PlacedRoom { id = nextId++, variant = v, origin = o };
            for (int i = 0; i < v.pos.Length; i++)
                grid[v.pos[i] + o] = new Cell { roomId = pr.id, sockets = v.sockets[i] };
            placed.Add(pr);
            used[v.data] = used.TryGetValue(v.data, out var c) ? c + 1 : 1;
            return pr;
        }

        void Remove(PlacedRoom pr)
        {
            for (int i = 0; i < pr.variant.pos.Length; i++) grid.Remove(pr.variant.pos[i] + pr.origin);
            placed.Remove(pr);
            used[pr.variant.data]--;
        }

        bool TryVoid(Vector2Int cell, System.Random rng, List<RoomData> fillers)
        {
            for (int d = 0; d < 4; d++)
            {
                if (grid.TryGetValue(cell + Delta[d], out var n) && n.roomId != VoidId
                    && n.sockets[(d + 2) % 4] == Socket.Door)
                    return false;
            }
            grid[cell] = new Cell { roomId = VoidId, sockets = VoidSockets };
            voidCount++;
            if (Solve(rng, fillers)) return true;
            grid.Remove(cell);
            voidCount--;
            return false;
        }

        bool Solve(System.Random rng, List<RoomData> fillers)
        {
            if (++nodes > solverBudget) return false;

            Vector2Int empty = default;
            bool found = false;
            for (int y = 0; y < height && !found; y++)
            for (int x = 0; x < width; x++)
                if (!grid.ContainsKey(new Vector2Int(x, y))) { empty = new Vector2Int(x, y); found = true; break; }
            if (!found) return true;

            bool canVoid = voidCount < maxVoidCells;
            bool voidFirst = canVoid && rng.Next(100) < voidPercent;
            if (voidFirst && TryVoid(empty, rng, fillers)) return true;
            if (nodes > solverBudget) return false;

            var options = new List<RoomVariant>();
            foreach (var d in fillers)
            {
                used.TryGetValue(d, out int u);
                if (u >= d.maxCount) continue;
                options.AddRange(Variants(d));
            }

            while (options.Count > 0)
            {
                int total = 0;
                foreach (var o in options) total += o.data.weight;
                int roll = rng.Next(total), idx = 0;
                for (; idx < options.Count; idx++)
                {
                    roll -= options[idx].data.weight;
                    if (roll < 0) break;
                }
                var v = options[idx];
                options.RemoveAt(idx);

                if (!CanPlace(v, empty)) continue;
                var pr = Place(v, empty);
                if (Solve(rng, fillers)) return true;
                Remove(pr);
                if (nodes > solverBudget) return false;
            }

            if (canVoid && !voidFirst && TryVoid(empty, rng, fillers)) return true;
            return false;
        }

        Dictionary<int, int> RoomDistances(PlacedRoom start)
        {
            var adj = new Dictionary<int, HashSet<int>>();
            foreach (var kv in grid)
            {
                for (int d = 0; d < 4; d++)
                {
                    if (kv.Value.sockets[d] != Socket.Door) continue;
                    if (!grid.TryGetValue(kv.Key + Delta[d], out var other)) continue;
                    if (other.roomId == kv.Value.roomId) continue;
                    if (!adj.TryGetValue(kv.Value.roomId, out var set)) adj[kv.Value.roomId] = set = new HashSet<int>();
                    set.Add(other.roomId);
                }
            }

            var dist = new Dictionary<int, int> { [start.id] = 1 };
            var q = new Queue<int>();
            q.Enqueue(start.id);
            while (q.Count > 0)
            {
                int r = q.Dequeue();
                if (!adj.TryGetValue(r, out var set)) continue;
                foreach (int n in set)
                    if (!dist.ContainsKey(n)) { dist[n] = dist[r] + 1; q.Enqueue(n); }
            }
            return dist;
        }

        public MapResult Generate(int seed, IList<RoomData> pool)
        {
            RoomData startData = null, finalData = null;
            var fillers = new List<RoomData>();
            foreach (var d in pool)
            {
                if (d.kind == RoomKind.Start) startData = d;
                else if (d.kind == RoomKind.Final) finalData = d;
                else fillers.Add(d);
            }
            if (startData == null || finalData == null) throw new InvalidOperationException("Need a start and final room");

            var rng = new System.Random(seed);

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                grid.Clear();
                placed.Clear();
                used.Clear();
                nextId = 0;
                nodes = 0;
                voidCount = 0;

                var sv = Variants(startData)[0];
                if (!CanPlace(sv, startOrigin)) throw new InvalidOperationException("La room de départ ne rentre pas à startOrigin.");
                var start = Place(sv, startOrigin);

                var candidates = new List<(RoomVariant v, Vector2Int o)>();
                foreach (var v in Variants(finalData))
                    for (int y = 0; y < height; y++)
                        for (int x = 0; x < width; x++)
                        {
                            var o = new Vector2Int(x, y);
                            if (CanPlace(v, o) && MinManhattan(v, o, start) >= minCellDistance)
                                candidates.Add((v, o));
                        }
                if (candidates.Count == 0) continue;
                var pick = candidates[rng.Next(candidates.Count)];
                var final = Place(pick.v, pick.o);

                if (!Solve(rng, fillers)) continue;

                var dist = RoomDistances(start);
                if (!dist.TryGetValue(final.id, out int pathLen) || pathLen < minRoomDistance) continue;
                if (requireAllReachable && dist.Count != placed.Count) continue;

                return new MapResult()
                {
                    rooms = new List<PlacedRoom>(placed),
                    start = start, final = final,
                    pathLength = pathLen, attempts = attempt
                };
            }
            return null;
        }

        int MinManhattan(RoomVariant v, Vector2Int o, PlacedRoom other)
        {
            int best = int.MaxValue;
            foreach (var a in v.pos)
                foreach (var b in other.variant.pos)
                {
                    var pa = a + o; var pb = b + other.origin;
                    best = Math.Min(best, Math.Abs(pa.x - pb.x) + Math.Abs(pa.y - pb.y));
                }
            return best;
        }
    }
}