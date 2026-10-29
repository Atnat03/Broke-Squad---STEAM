using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Gameplay.LD.ProceduralGeneration
{
    public class RoomVariant
    {
        public RoomData data;
        public int rotation;
        public Vector3Int[] pos;
        public HashSet<Vector3Int> set;
        public Socket[][] sockets;
        public Vector3Int pivot;
        public int doorCount;
    }

    public class PlacedRoom
    {
        public int id;
        public RoomVariant variant;
        public Vector3Int origin;
        public Vector3Int PivotCell => origin + variant.pivot;
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
        public int floorCount = 2;
        public int targetRooms = 20;
        public int minRooms = 12;
        public int maxExtent = 8;
        public int minRoomsPerFloor = 1;

        public Vector2Int startOrigin = Vector2Int.zero;
        public int minCellDistance = 3;

        public int minRoomDistance = 4;
        public bool requireAllReachable = true;
        public int maxAttempts = 50;
        public int solverBudget = 2000;
        public int maxMillis = 2000;

        private static readonly Vector3Int[] Delta =
        {
            new Vector3Int(0, -1, 0), new Vector3Int(1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(-1, 0, 0)
        };

        struct Cell
        {
            public int roomId;
            public Socket[] sockets;
        }

        readonly Dictionary<Vector3Int, Cell> grid = new Dictionary<Vector3Int, Cell>();
        readonly List<PlacedRoom> placed = new List<PlacedRoom>();
        readonly Dictionary<RoomData, int> used = new Dictionary<RoomData, int>();
        readonly Dictionary<RoomData, List<RoomVariant>> roomVariants = new Dictionary<RoomData, List<RoomVariant>>();

        RoomData finalData;
        int nextId;
        int nodes;
        bool timeUp;
        Stopwatch clock;

        static List<RoomVariant> GenerateRoomVariants(RoomData d)
        {
            var result = new List<RoomVariant>();
            var seen = new HashSet<string>();

            int n = d.cells.Count;
            var pos = new Vector3Int[n];
            var sock = new Socket[n][];
            for (int i = 0; i < n; i++)
            {
                pos[i] = d.cells[i].pos;
                sock[i] = d.cells[i].ToArray();
            }
            var pivot = Vector3Int.zero;

            int rots = d.allowRotation ? 4 : 1;
            for (int r = 0; r < rots; r++)
            {
                if (r > 0)
                {
                    for (int i = 0; i < n; i++)
                    {
                        pos[i] = new Vector3Int(-pos[i].y, pos[i].x, pos[i].z);
                        var s = sock[i];
                        sock[i] = new[] { s[3], s[0], s[1], s[2] };
                    }
                    pivot = new Vector3Int(-pivot.y, pivot.x, pivot.z);
                }

                var first = pos[0];
                for (int i = 1; i < n; i++)
                {
                    if (pos[i].z < first.z
                        || (pos[i].z == first.z && pos[i].y < first.y)
                        || (pos[i].z == first.z && pos[i].y == first.y && pos[i].x < first.x))
                    {
                        first = pos[i];
                    }
                }

                var variant = new RoomVariant
                {
                    data = d,
                    rotation = r,
                    pos = new Vector3Int[n],
                    sockets = new Socket[n][],
                    pivot = pivot - first,
                };

                var self = new HashSet<Vector3Int>();
                for (int i = 0; i < n; i++)
                {
                    variant.pos[i] = pos[i] - first;
                    variant.sockets[i] = (Socket[])sock[i].Clone();
                    self.Add(variant.pos[i]);
                }
                variant.set = self;

                int doors = 0;
                for (int i = 0; i < n; i++)
                    for (int dir = 0; dir < 4; dir++)
                        if (variant.sockets[i][dir] == Socket.Door && !self.Contains(variant.pos[i] + Delta[dir]))
                            doors++;
                variant.doorCount = doors;

                var order = new List<int>();
                for (int i = 0; i < n; i++) order.Add(i);
                order.Sort((a, b) =>
                {
                    if (variant.pos[a].z != variant.pos[b].z) return variant.pos[a].z.CompareTo(variant.pos[b].z);
                    if (variant.pos[a].y != variant.pos[b].y) return variant.pos[a].y.CompareTo(variant.pos[b].y);
                    return variant.pos[a].x.CompareTo(variant.pos[b].x);
                });
                var key = new System.Text.StringBuilder();
                foreach (int i in order)
                {
                    key.Append(variant.pos[i].x).Append(',').Append(variant.pos[i].y).Append(',').Append(variant.pos[i].z).Append(':');
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

        bool InBounds(Vector3Int p) =>
            p.z >= 0 && p.z < floorCount &&
            Math.Abs(p.x - startOrigin.x) <= maxExtent &&
            Math.Abs(p.y - startOrigin.y) <= maxExtent;

        bool CanPlace(RoomVariant v, Vector3Int o)
        {
            for (int i = 0; i < v.pos.Length; i++)
            {
                var p = v.pos[i] + o;
                if (!InBounds(p) || grid.ContainsKey(p)) return false;
            }

            for (int i = 0; i < v.pos.Length; i++)
            {
                var p = v.pos[i] + o;
                for (int d = 0; d < 4; d++)
                {
                    var q = p + Delta[d];
                    if (v.set.Contains(q - o)) continue;

                    if (grid.TryGetValue(q, out var other))
                    {
                        bool a = v.sockets[i][d] == Socket.Door;
                        bool b = other.sockets[(d + 2) % 4] == Socket.Door;
                        if (a != b) return false;
                    }
                    else if (v.sockets[i][d] == Socket.Door && !InBounds(q))
                        return false;
                }
            }
            return true;
        }

        PlacedRoom Place(RoomVariant v, Vector3Int o)
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

        List<(Vector3Int cell, int dir)> OpenDoors()
        {
            var list = new List<(Vector3Int, int)>();
            foreach (var kv in grid)
                for (int d = 0; d < 4; d++)
                    if (kv.Value.sockets[d] == Socket.Door && !grid.ContainsKey(kv.Key + Delta[d]))
                        list.Add((kv.Key, d));
            return list;
        }

        bool FinalPlaced() => used.TryGetValue(finalData, out int c) && c > 0;

        int[] FloorCounts()
        {
            var c = new int[floorCount];
            foreach (var k in grid.Keys) c[k.z]++;
            return c;
        }

        int Weight(RoomVariant v, Vector3Int o, int[] floors, int minFloor)
        {
            int w = v.data.weight * 10;
            int outgoing = Math.Max(1, v.doorCount - 1);
            w = placed.Count < targetRooms ? w * outgoing : w / outgoing;
            if (v.data == finalData && !FinalPlaced() && placed.Count > targetRooms / 2) w *= 5;

            foreach (var p in v.pos)
                if (floors[p.z + o.z] == minFloor) { w *= 3; break; }

            return Math.Max(1, w);
        }

        List<(RoomVariant v, Vector3Int o)> Options(Vector3Int cell, int dir, List<RoomData> fillers)
        {
            var options = new List<(RoomVariant, Vector3Int)>();
            var target = cell + Delta[dir];
            int need = (dir + 2) % 4;

            foreach (var d in fillers)
            {
                used.TryGetValue(d, out int u);
                int cap = d == finalData ? 1 : d.maxCount;
                if (u >= cap) continue;
                foreach (var variant in Variants(d))
                    for (int i = 0; i < variant.pos.Length; i++)
                        if (variant.sockets[i][need] == Socket.Door)
                        {
                            var origin = target - variant.pos[i];
                            if (CanPlace(variant, origin)) options.Add((variant, origin));
                        }
            }
            return options;
        }

        bool Solve(System.Random rng, List<RoomData> fillers)
        {
            if (timeUp) return false;
            if (++nodes > solverBudget) return false;
            if ((nodes & 31) == 0 && clock.ElapsedMilliseconds > maxMillis) { timeUp = true; return false; }

            var open = OpenDoors();
            if (open.Count == 0) return placed.Count >= minRooms && FinalPlaced();

            List<(RoomVariant v, Vector3Int o)> options = null;
            int start = rng.Next(open.Count);
            for (int k = 0; k < open.Count; k++)
            {
                var (cell, dir) = open[(start + k) % open.Count];
                var opts = Options(cell, dir, fillers);
                if (opts.Count == 0) return false;
                if (options == null || opts.Count < options.Count) options = opts;
                if (options.Count == 1) break;
            }

            var floors = FloorCounts();
            int minFloor = int.MaxValue;
            foreach (int f in floors) minFloor = Math.Min(minFloor, f);

            var weights = new List<int>(options.Count);
            foreach (var op in options) weights.Add(Weight(op.v, op.o, floors, minFloor));

            while (options.Count > 0)
            {
                int total = 0;
                foreach (int w in weights) total += w;
                int roll = rng.Next(total), idx = 0;
                for (; idx < options.Count; idx++)
                {
                    roll -= weights[idx];
                    if (roll < 0) break;
                }
                var (v, o) = options[idx];
                options.RemoveAt(idx);
                weights.RemoveAt(idx);

                var pr = Place(v, o);
                if (Solve(rng, fillers)) return true;
                Remove(pr);
                if (timeUp || nodes > solverBudget) return false;
            }

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

        bool FloorsValid()
        {
            if (minRoomsPerFloor <= 0) return true;
            var perFloor = new HashSet<int>[floorCount];
            for (int i = 0; i < floorCount; i++) perFloor[i] = new HashSet<int>();
            foreach (var kv in grid) perFloor[kv.Key.z].Add(kv.Value.roomId);
            for (int i = 0; i < floorCount; i++)
                if (perFloor[i].Count < minRoomsPerFloor) return false;
            return true;
        }

        public MapResult Generate(int seed, IList<RoomData> pool)
        {
            var starts = new List<RoomData>();
            var finals = new List<RoomData>();
            var baseFillers = new List<RoomData>();
            foreach (var d in pool)
            {
                if (d.kind == RoomKind.Start) starts.Add(d);
                else if (d.kind == RoomKind.Final) finals.Add(d);
                else baseFillers.Add(d);
            }
            if (starts.Count == 0 || finals.Count == 0) throw new InvalidOperationException("Need a start and final room");

            var rng = new System.Random(seed);
            clock = Stopwatch.StartNew();
            
            
            timeUp = false;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (timeUp || clock.ElapsedMilliseconds > maxMillis) break;

                grid.Clear();
                placed.Clear();
                used.Clear();
                nextId = 0;
                nodes = 0;
                
                var startData = starts[rng.Next(starts.Count)];
                finalData = finals[rng.Next(finals.Count)];
                var fillers = new List<RoomData>(baseFillers) { finalData };

                var sv = Variants(startData)[0];
                
                var startCell = new Vector3Int(startOrigin.x, startOrigin.y, 0);
                if (!CanPlace(sv, startCell)) throw new InvalidOperationException("La room de départ ne rentre pas à startOrigin.");
                var start = Place(sv, startCell);

                var initialOpen = OpenDoors();
                for (int f = 0; f < floorCount; f++)
                {
                    bool hasDoor = false;
                    foreach (var od in initialOpen)
                        if (od.cell.z == f) { hasDoor = true; break; }
                    if (!hasDoor) throw new InvalidOperationException("La room de départ n'a aucune porte à l'étage " + f);
                }

                if (!Solve(rng, fillers)) continue;
                if (!FloorsValid()) continue;

                PlacedRoom final = placed.Find(p => p.variant.data == finalData);
                if (final == null) continue;
                if (MinManhattan(final.variant, final.origin, start) < minCellDistance) continue;

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

        int MinManhattan(RoomVariant v, Vector3Int o, PlacedRoom other)
        {
            int best = int.MaxValue;
            foreach (var a in v.pos)
                foreach (var b in other.variant.pos)
                {
                    var pa = a + o; var pb = b + other.origin;
                    best = Math.Min(best, Math.Abs(pa.x - pb.x) + Math.Abs(pa.y - pb.y) + Math.Abs(pa.z - pb.z));
                }
            return best;
        }
    }
}