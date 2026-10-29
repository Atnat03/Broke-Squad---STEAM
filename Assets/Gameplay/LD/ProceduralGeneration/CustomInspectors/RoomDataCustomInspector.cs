using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gameplay.LD.ProceduralGeneration
{
    [CustomEditor(typeof(RoomData))]
    public class RoomDataCustomInspector : Editor
    {
        private const float CellSize = 70f;
        private const float Edge = 12f;
        private const float SideCellSize = 40f;
        private const int FloorCount = 2;

        private static readonly Vector3Int[] Dirs =
        {
            new Vector3Int(0, -1, 0), new Vector3Int(1, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(-1, 0, 0)
        };

        private static readonly string[] BrushNames = { "Wall", "Door", "None" };
        private static readonly Socket[] BrushSockets = { Socket.Wall, Socket.Door, Socket.None };
        private static readonly string[] FloorNames = { "Floor 0", "Floor 1" };

        private static readonly string[] SideModeNames = { "Front (X/Z)", "Side (Y/Z)" };

        private int brush = 1;
        private int floor;
        private int sideMode;
        private bool showRaw;
        private Editor prefabEditor;

        private void OnDisable()
        {
            if (prefabEditor != null) DestroyImmediate(prefabEditor);
        }

        public override bool HasPreviewGUI() => ((RoomData)target).prefab != null;

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            var prefab = ((RoomData)target).prefab;
            if (prefab == null) return;
            if (prefabEditor == null || prefabEditor.target != prefab) CreateCachedEditor(prefab, null, ref prefabEditor);
            prefabEditor.OnPreviewGUI(r, background);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "cells");
            serializedObject.ApplyModifiedProperties();

            var data = (RoomData)target;
            if (data.cells == null || data.cells.Count == 0)
            {
                data.cells = new List<RoomCell> { new RoomCell() };
                EditorUtility.SetDirty(data);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Room Layout", EditorStyles.boldLabel);
            brush = GUILayout.Toolbar(brush, BrushNames, GUILayout.Height(24));
            floor = GUILayout.Toolbar(floor, FloorNames, GUILayout.Height(22));

            DrawGrid(data);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Side View", EditorStyles.boldLabel);
            DrawSideView(data);

            DrawWarnings(data);

            showRaw = EditorGUILayout.Foldout(showRaw, "Raw cell data", true);
            if (!showRaw) return;
            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cells"), true);
            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawWarnings(RoomData data)
        {
            foreach (var c in data.cells)
                if (c.pos.z < 0 || c.pos.z >= FloorCount)
                {
                    EditorGUILayout.HelpBox("Some cells are outside floors 0-1.", MessageType.Error);
                    break;
                }

            if (!IsConnected(data.cells))
                EditorGUILayout.HelpBox("Some cells are not connected to the rest of the room.", MessageType.Warning);
        }

        private static bool IsConnected(List<RoomCell> cells)
        {
            var all = new HashSet<Vector3Int>();
            foreach (var c in cells) all.Add(c.pos);

            var visited = new HashSet<Vector3Int> { cells[0].pos };
            var queue = new Queue<Vector3Int>();
            queue.Enqueue(cells[0].pos);

            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                foreach (var n in Neighbors(p))
                    if (all.Contains(n) && visited.Add(n)) queue.Enqueue(n);
            }
            return visited.Count == all.Count;
        }

        private static IEnumerable<Vector3Int> Neighbors(Vector3Int p)
        {
            foreach (var d in Dirs) yield return p + d;
            yield return p + Vector3Int.forward;
            yield return p - Vector3Int.forward;
        }

        private void DrawGrid(RoomData data)
        {
            var map = new Dictionary<Vector3Int, RoomCell>();
            var min = new Vector2Int(data.cells[0].pos.x, data.cells[0].pos.y);
            var max = min;
            foreach (var c in data.cells)
            {
                map[c.pos] = c;
                min = Vector2Int.Min(min, new Vector2Int(c.pos.x, c.pos.y));
                max = Vector2Int.Max(max, new Vector2Int(c.pos.x, c.pos.y));
            }
            min -= Vector2Int.one;
            max += Vector2Int.one;

            int w = max.x - min.x + 1;
            int h = max.y - min.y + 1;
            Rect area = GUILayoutUtility.GetRect(w * CellSize, h * CellSize + 10, GUILayout.ExpandWidth(true));
            var origin = new Vector2(area.x + (area.width - w * CellSize) * 0.5f, area.y + 5);
            EditorGUI.DrawRect(area, new Color(0.15f, 0.15f, 0.15f, 0.4f));

            RoomCell toRemove = null;
            Vector3Int? toAdd = null;
            var e = Event.current;

            for (int y = min.y; y <= max.y; y++)
            for (int x = min.x; x <= max.x; x++)
            {
                var p = new Vector3Int(x, y, floor);
                var r = new Rect(origin.x + (x - min.x) * CellSize, origin.y + (y - min.y) * CellSize, CellSize, CellSize);
                var inner = new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2);

                if (!map.TryGetValue(p, out var cell))
                {
                    if (map.ContainsKey(new Vector3Int(x, y, 1 - floor)))
                        EditorGUI.DrawRect(inner, new Color(0.35f, 0.38f, 0.42f, 0.18f));
                    if (CanAddAt(map, p) && GUI.Button(new Rect(r.x + 12, r.y + 12, r.width - 24, r.height - 24), "+"))
                        toAdd = p;
                    continue;
                }

                EditorGUI.DrawRect(inner, new Color(0.35f, 0.38f, 0.42f));
                GUI.Label(r, $"{x},{y},{floor}", EditorStyles.centeredGreyMiniLabel);

                if (e.type == EventType.ContextClick && r.Contains(e.mousePosition))
                {
                    toRemove = cell;
                    e.Use();
                }

                for (int d = 0; d < 4; d++)
                    DrawEdge(data, map, cell, d, EdgeRect(r, d));
            }

            if (toAdd.HasValue) AddCell(data, map, toAdd.Value);
            else if (toRemove != null && data.cells.Count > 1) RemoveCell(data, map, toRemove);
        }

        private void DrawSideView(RoomData data)
        {
            sideMode = GUILayout.Toolbar(sideMode, SideModeNames, GUILayout.Height(22));

            var count = new Dictionary<Vector2Int, int>();
            var all = new HashSet<Vector3Int>();
            foreach (var c in data.cells) all.Add(c.pos);

            int minH = int.MaxValue, maxH = int.MinValue;
            foreach (var c in data.cells)
            {
                int hh = sideMode == 0 ? c.pos.x : c.pos.y;
                var key = new Vector2Int(hh, Mathf.Clamp(c.pos.z, 0, FloorCount - 1));
                count[key] = count.TryGetValue(key, out int n) ? n + 1 : 1;
                minH = Mathf.Min(minH, hh);
                maxH = Mathf.Max(maxH, hh);
            }

            int w = maxH - minH + 1;
            Rect area = GUILayoutUtility.GetRect(w * SideCellSize + 20, FloorCount * SideCellSize + 20, GUILayout.ExpandWidth(true));
            var origin = new Vector2(area.x + (area.width - w * SideCellSize) * 0.5f, area.y + 10);
            EditorGUI.DrawRect(area, new Color(0.15f, 0.15f, 0.15f, 0.4f));

            foreach (var kv in count)
            {
                var r = new Rect(origin.x + (kv.Key.x - minH) * SideCellSize,
                                 origin.y + (FloorCount - 1 - kv.Key.y) * SideCellSize,
                                 SideCellSize, SideCellSize);
                EditorGUI.DrawRect(new Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2), new Color(0.35f, 0.38f, 0.42f));
                GUI.Label(r, $"F{kv.Key.y}" + (kv.Value > 1 ? $" x{kv.Value}" : ""), EditorStyles.centeredGreyMiniLabel);

                bool linked = false;
                foreach (var p in all)
                    if (p.z == kv.Key.y && (sideMode == 0 ? p.x : p.y) == kv.Key.x && all.Contains(p + Vector3Int.forward))
                    {
                        linked = true;
                        break;
                    }
                EditorGUI.DrawRect(new Rect(r.x + 1, r.y, r.width - 2, linked ? 4f : 2f),
                    linked ? new Color(0.40f, 0.80f, 0.40f) : new Color(0.10f, 0.10f, 0.10f));
            }
        }

        private void DrawEdge(RoomData data, Dictionary<Vector3Int, RoomCell> map, RoomCell cell, int dir, Rect r)
        {
            EditorGUI.DrawRect(r, SocketColor(Get(cell, dir)));
            EditorGUIUtility.AddCursorRect(r, MouseCursor.Link);

            var e = Event.current;
            if (e.type != EventType.MouseDown || e.button != 0 || !r.Contains(e.mousePosition)) return;

            Undo.RecordObject(data, "Paint Room Socket");
            Set(cell, dir, BrushSockets[brush]);
            if (map.TryGetValue(cell.pos + Dirs[dir], out var n)) Set(n, (dir + 2) % 4, BrushSockets[brush]);
            EditorUtility.SetDirty(data);
            e.Use();
            Repaint();
        }

        private static void AddCell(RoomData data, Dictionary<Vector3Int, RoomCell> map, Vector3Int pos)
        {
            Undo.RecordObject(data, "Add Room Cell");
            var cell = new RoomCell { pos = pos };
            for (int d = 0; d < 4; d++)
            {
                bool shared = map.TryGetValue(pos + Dirs[d], out var n);
                Set(cell, d, shared ? Socket.None : Socket.Wall);
                if (shared) Set(n, (d + 2) % 4, Socket.None);
            }
            data.cells.Add(cell);
            EditorUtility.SetDirty(data);
        }

        private static void RemoveCell(RoomData data, Dictionary<Vector3Int, RoomCell> map, RoomCell cell)
        {
            Undo.RecordObject(data, "Remove Room Cell");
            for (int d = 0; d < 4; d++)
                if (map.TryGetValue(cell.pos + Dirs[d], out var n)) Set(n, (d + 2) % 4, Socket.Wall);
            data.cells.Remove(cell);
            EditorUtility.SetDirty(data);
        }

        private static bool CanAddAt(Dictionary<Vector3Int, RoomCell> map, Vector3Int p)
        {
            foreach (var d in Dirs)
                if (map.ContainsKey(p + d)) return true;
            return map.ContainsKey(new Vector3Int(p.x, p.y, 1 - p.z));
        }

        private static Rect EdgeRect(Rect c, int dir)
        {
            switch (dir)
            {
                case 0: return new Rect(c.x + Edge, c.y, c.width - 2 * Edge, Edge);
                case 1: return new Rect(c.xMax - Edge, c.y + Edge, Edge, c.height - 2 * Edge);
                case 2: return new Rect(c.x + Edge, c.yMax - Edge, c.width - 2 * Edge, Edge);
                default: return new Rect(c.x, c.y + Edge, Edge, c.height - 2 * Edge);
            }
        }

        private static Color SocketColor(Socket s)
        {
            switch (s)
            {
                case Socket.Wall: return new Color(0.10f, 0.10f, 0.10f);
                case Socket.Door: return new Color(0.90f, 0.55f, 0.15f);
                default: return new Color(0.30f, 0.30f, 0.30f, 0.35f);
            }
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