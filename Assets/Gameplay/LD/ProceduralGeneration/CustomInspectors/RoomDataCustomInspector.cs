using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gameplay.LD.ProceduralGeneration
{
    [CustomEditor(typeof(RoomData))]
    public class RoomDataCustomInspector : Editor
    {
        private const float CellSize = 70f;
        private const float SideCellSize = 40f;
        private const float EdgeThickness = 12f;

        private static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(0, 1), new Vector2Int(1, 0),
            new Vector2Int(0, -1), new Vector2Int(-1, 0)
        };

        private static readonly string[] BrushNames = { "Wall", "Door", "None" };
        private static readonly Socket[] BrushSockets = { Socket.Wall, Socket.Door, Socket.None };
        private int brushIndex = 1;
        private Socket brush => BrushSockets[brushIndex];
        
        private int currentFloor;
        private int sideMode;
        private bool showRawCells;
        private Editor cachedPrefabEditor;

        private void OnDisable()
        {
            if (cachedPrefabEditor != null) DestroyImmediate(cachedPrefabEditor);
        }

        public override bool HasPreviewGUI() => ((RoomData)target).prefab != null;

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            var data = (RoomData)target;
            if (data.prefab == null) return;

            if (cachedPrefabEditor == null || cachedPrefabEditor.target != data.prefab)
                CreateCachedEditor(data.prefab, null, ref cachedPrefabEditor);

            cachedPrefabEditor?.OnPreviewGUI(r, background);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "cells");
            serializedObject.ApplyModifiedProperties();

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Room Layout", EditorStyles.boldLabel);

            var data = (RoomData)target;
            DrawBrushToolbar();
            DrawFloorToolbar(data);
            DrawGrid(data);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Side View", EditorStyles.boldLabel);
            DrawSideView(data);

            DrawLegend();

            showRawCells = EditorGUILayout.Foldout(showRawCells, "Raw cell data", true);
            if (showRawCells)
            {
                serializedObject.Update();
                EditorGUILayout.PropertyField(serializedObject.FindProperty("cells"), true);
                serializedObject.ApplyModifiedProperties();
            }
        }

        private void DrawBrushToolbar()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Types", GUILayout.Width(40));
            brushIndex = GUILayout.Toolbar(brushIndex, BrushNames, GUILayout.Height(24));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawFloorToolbar(RoomData data)
        {
            if (data.cells == null || data.cells.Count == 0) return;

            int minZ = 0, maxZ = 0;
            foreach (var c in data.cells)
            {
                minZ = Mathf.Min(minZ, c.pos.z);
                maxZ = Mathf.Max(maxZ, c.pos.z);
            }
            maxZ += 1;

            currentFloor = Mathf.Clamp(currentFloor, minZ, maxZ);

            var names = new string[maxZ - minZ + 1];
            for (int z = minZ; z <= maxZ; z++) names[z - minZ] = "Floor " + z;

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Floor", GUILayout.Width(40));
            currentFloor = GUILayout.Toolbar(currentFloor - minZ, names, GUILayout.Height(22)) + minZ;
            EditorGUILayout.EndHorizontal();
        }

        private void DrawLegend()
        {
            var data = (RoomData)target;
            if (!IsConnected(data.cells))
            {
                EditorGUILayout.HelpBox(
                    "Some cells are not connected to the rest of the room.",
                    MessageType.Warning);
            }
        }

        private static bool IsConnected(List<RoomCell> cells)
        {
            if (cells == null || cells.Count <= 1) return true;

            var positions = new HashSet<Vector3Int>();
            foreach (var c in cells) positions.Add(c.pos);

            var visited = new HashSet<Vector3Int>();
            var queue = new Queue<Vector3Int>();

            queue.Enqueue(cells[0].pos);
            visited.Add(cells[0].pos);

            while (queue.Count > 0)
            {
                Vector3Int current = queue.Dequeue();

                foreach (var dir in Dirs)
                {
                    var next = current + new Vector3Int(dir.x, dir.y, 0);
                    if (positions.Contains(next) && visited.Add(next))
                        queue.Enqueue(next);
                }

                var up = current + new Vector3Int(0, 0, 1);
                if (positions.Contains(up) && visited.Add(up)) queue.Enqueue(up);
                var down = current + new Vector3Int(0, 0, -1);
                if (positions.Contains(down) && visited.Add(down)) queue.Enqueue(down);
            }

            return visited.Count == positions.Count;
        }

        private void DrawGrid(RoomData data)
        {
            if (data.cells == null || data.cells.Count == 0)
            {
                data.cells = new List<RoomCell> { new RoomCell() };
                EditorUtility.SetDirty(data);
            }

            var map = new Dictionary<Vector3Int, RoomCell>();
            var xyOtherFloors = new HashSet<Vector2Int>();
            Vector2Int min = new Vector2Int(data.cells[0].pos.x, data.cells[0].pos.y);
            Vector2Int max = min;

            foreach (var c in data.cells)
            {
                map[c.pos] = c;
                var xy = new Vector2Int(c.pos.x, c.pos.y);
                min = Vector2Int.Min(min, xy);
                max = Vector2Int.Max(max, xy);
                if (c.pos.z != currentFloor) xyOtherFloors.Add(xy);
            }

            min -= Vector2Int.one;
            max += Vector2Int.one;

            int w = max.x - min.x + 1;
            int h = max.y - min.y + 1;

            Rect area = GUILayoutUtility.GetRect(w * CellSize, h * CellSize + 10,
                GUILayout.ExpandWidth(true));
            Vector2 origin = new Vector2(
                area.x + (area.width - w * CellSize) * 0.5f, area.y + 5);

            EditorGUI.DrawRect(area, new Color(0.15f, 0.15f, 0.15f, 0.4f));

            RoomCell toRemove = null;
            Vector3Int? toAdd = null;

            for (int y = max.y; y >= min.y; y--)
            for (int x = min.x; x <= max.x; x++)
            {
                var xy = new Vector2Int(x, y);
                var p = new Vector3Int(x, y, currentFloor);
                Rect r = CellRect(origin, xy, min, max);

                if (!map.TryGetValue(p, out var cell))
                {
                    if (xyOtherFloors.Contains(xy))
                        EditorGUI.DrawRect(Inset(r, 1), new Color(0.35f, 0.38f, 0.42f, 0.18f));

                    if (CanAddAt(map, p) && DrawAddButton(r)) toAdd = p;
                    continue;
                }

                EditorGUI.DrawRect(Inset(r, 1), new Color(0.35f, 0.38f, 0.42f));
                GUI.Label(r, $"{p.x},{p.y},{p.z}", CenteredMini());

                if (Event.current.type == EventType.ContextClick && r.Contains(Event.current.mousePosition))
                {
                    toRemove = cell;
                    Event.current.Use();
                }
            }

            foreach (var cell in data.cells)
            {
                if (cell.pos.z != currentFloor) continue;
                Rect r = CellRect(origin, new Vector2Int(cell.pos.x, cell.pos.y), min, max);
                for (int d = 0; d < 4; d++)
                    DrawEdge(data, map, cell, d, EdgeRect(r, d));
            }

            if (toAdd.HasValue) AddCell(data, map, toAdd.Value);
            else if (toRemove != null && data.cells.Count > 1) RemoveCell(data, map, toRemove);
        }

        private void DrawSideView(RoomData data)
        {
            if (data.cells == null || data.cells.Count == 0) return;

            sideMode = GUILayout.Toolbar(sideMode, SideModeNames, GUILayout.Height(22));
            bool xz = sideMode == 0;

            var count = new Dictionary<Vector2Int, int>();
            var linked = new HashSet<Vector2Int>();
            var all = new HashSet<Vector3Int>();
            foreach (var c in data.cells) all.Add(c.pos);

            int minH = int.MaxValue, maxH = int.MinValue;
            int minZ = int.MaxValue, maxZ = int.MinValue;

            foreach (var c in data.cells)
            {
                int hh = xz ? c.pos.x : c.pos.y;
                var key = new Vector2Int(hh, c.pos.z);
                count[key] = count.TryGetValue(key, out int n) ? n + 1 : 1;
                if (all.Contains(c.pos + new Vector3Int(0, 0, 1))) linked.Add(key);

                minH = Mathf.Min(minH, hh); maxH = Mathf.Max(maxH, hh);
                minZ = Mathf.Min(minZ, c.pos.z); maxZ = Mathf.Max(maxZ, c.pos.z);
            }

            int w = maxH - minH + 1;
            int h = maxZ - minZ + 1;

            Rect area = GUILayoutUtility.GetRect(w * SideCellSize + 20, h * SideCellSize + 20,
                GUILayout.ExpandWidth(true));
            Vector2 origin = new Vector2(
                area.x + (area.width - w * SideCellSize) * 0.5f, area.y + 10);

            EditorGUI.DrawRect(area, new Color(0.15f, 0.15f, 0.15f, 0.4f));

            foreach (var kv in count)
            {
                Rect r = new Rect(origin.x + (kv.Key.x - minH) * SideCellSize,
                                  origin.y + (maxZ - kv.Key.y) * SideCellSize,
                                  SideCellSize, SideCellSize);

                EditorGUI.DrawRect(Inset(r, 1), new Color(0.35f, 0.38f, 0.42f));
                GUI.Label(r, $"F{kv.Key.y}" + (kv.Value > 1 ? $" x{kv.Value}" : ""), CenteredMini());

                bool up = linked.Contains(kv.Key);
                EditorGUI.DrawRect(new Rect(r.x + 1, r.y, r.width - 2, up ? 4f : 2f),
                    up ? new Color(0.40f, 0.80f, 0.40f) : new Color(0.10f, 0.10f, 0.10f));
            }
        }

        public string[] SideModeNames { get; set; }

        private void DrawEdge(RoomData data, Dictionary<Vector3Int, RoomCell> map,
            RoomCell cell, int dir, Rect e)
        {
            Socket s = Get(cell, dir);

            EditorGUI.DrawRect(e, SocketColor(s));
            EditorGUIUtility.AddCursorRect(e, MouseCursor.Link);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0
                && e.Contains(Event.current.mousePosition))
            {
                Undo.RecordObject(data, "Paint Room Socket");
                Set(cell, dir, brush);

                if (map.TryGetValue(cell.pos + Offset(dir), out var neighbor))
                    Set(neighbor, (dir + 2) % 4, brush);

                EditorUtility.SetDirty(data);
                Event.current.Use();
                Repaint();
            }
        }

        private bool DrawAddButton(Rect r)
        {
            return GUI.Button(Inset(r, 12), "+");
        }

        private void AddCell(RoomData data, Dictionary<Vector3Int, RoomCell> map, Vector3Int pos)
        {
            Undo.RecordObject(data, "Add Room Cell");
            var cell = new RoomCell { pos = pos };
            for (int d = 0; d < 4; d++)
            {
                bool shared = map.TryGetValue(pos + Offset(d), out var n);
                Set(cell, d, shared ? Socket.None : Socket.Wall);
                if (shared) Set(n, (d + 2) % 4, Socket.None);
            }
            data.cells.Add(cell);
            EditorUtility.SetDirty(data);
        }

        private void RemoveCell(RoomData data, Dictionary<Vector3Int, RoomCell> map, RoomCell cell)
        {
            Undo.RecordObject(data, "Remove Room Cell");
            for (int d = 0; d < 4; d++)
                if (map.TryGetValue(cell.pos + Offset(d), out var n))
                    Set(n, (d + 2) % 4, Socket.Wall);
            data.cells.Remove(cell);
            EditorUtility.SetDirty(data);
        }

        private static bool CanAddAt(Dictionary<Vector3Int, RoomCell> map, Vector3Int p)
        {
            for (int d = 0; d < 4; d++)
                if (map.ContainsKey(p + Offset(d))) return true;

            return map.ContainsKey(p + new Vector3Int(0, 0, 1))
                || map.ContainsKey(p + new Vector3Int(0, 0, -1));
        }

        private static Vector3Int Offset(int d) => new Vector3Int(Dirs[d].x, Dirs[d].y, 0);

        private static Rect CellRect(Vector2 origin, Vector2Int p, Vector2Int min, Vector2Int max)
        {
            return new Rect(origin.x + (p.x - min.x) * CellSize,
                            origin.y + (max.y - p.y) * CellSize,
                            CellSize, CellSize);
        }

        private static Rect EdgeRect(Rect c, int dir)
        {
            float t = EdgeThickness;
            switch (dir)
            {
                case 0: return new Rect(c.x + t, c.y, c.width - 2 * t, t);
                case 1: return new Rect(c.xMax - t, c.y + t, t, c.height - 2 * t);
                case 2: return new Rect(c.x + t, c.yMax - t, c.width - 2 * t, t);
                default: return new Rect(c.x, c.y + t, t, c.height - 2 * t);
            }
        }

        private static Rect Inset(Rect r, float m) =>
            new Rect(r.x + m, r.y + m, r.width - 2 * m, r.height - 2 * m);

        private static Color SocketColor(Socket s)
        {
            switch (s)
            {
                case Socket.Wall: return new Color(0.10f, 0.10f, 0.10f);
                case Socket.Door: return new Color(0.90f, 0.55f, 0.15f);
                default:          return new Color(0.30f, 0.30f, 0.30f, 0.35f);
            }
        }

        private static GUIStyle CenteredMini()
        {
            var s = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter };
            return s;
        }

        private static Socket Get(RoomCell c, int d) =>
            d == 0 ? c.north : d == 1 ? c.east : d == 2 ? c.south : c.west;

        private static void Set(RoomCell c, int d, Socket s)
        {
            switch (d)
            {
                case 0: c.north = s; break;
                case 1: c.east = s; break;
                case 2: c.south = s; break;
                default: c.west = s; break;
            }
        }
    }
}