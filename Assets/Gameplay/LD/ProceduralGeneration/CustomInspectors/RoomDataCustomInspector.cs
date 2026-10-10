using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Gameplay.LD.ProceduralGeneration
{
    [CustomEditor(typeof(RoomData))]
    public class RoomDataCustomInspector : Editor
    {
        private const float CellSize = 70f;
        private const float EdgeThickness = 12f;
        
        private static readonly Vector2Int[] Dirs =
        {
            new Vector2Int(0, 1), new Vector2Int(1, 0),
            new Vector2Int(0, -1), new Vector2Int(-1, 0)
        };

        private static readonly string[] BrushNames = { "Wall", "Door", "Window", "None" };

        private Socket brush = Socket.Door;
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

            DrawBrushToolbar();
            DrawGrid((RoomData)target);
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
            brush = (Socket)GUILayout.Toolbar((int)brush, BrushNames, GUILayout.Height(24));
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
            if(cells == null || cells.Count <= 1) return true;
            
            var positions = new HashSet<Vector2Int>();
            foreach (var c in cells)
            {
                positions.Add(c.pos);
            }
            
            var visited = new HashSet<Vector2Int>();
            var queue = new Queue<Vector2Int>();
            
            queue.Enqueue(cells[0].pos);
            visited.Add(cells[0].pos);

            while (queue.Count > 0)
            {
                Vector2Int current = queue.Dequeue();

                foreach (var dir in Dirs)
                {
                    Vector2Int next = current + dir;
                    if (positions.Contains(next) && visited.Add(next))
                        queue.Enqueue(next);
                }

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
            
            var map = new Dictionary<Vector2Int, RoomCell>();
            Vector2Int min = data.cells[0].pos, max = data.cells[0].pos;
            foreach (var c in data.cells)
            {
                map[c.pos] = c;
                min = Vector2Int.Min(min, c.pos);
                max = Vector2Int.Max(max, c.pos);
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
            Vector2Int? toAdd = null;
            
            for (int y = max.y; y >= min.y; y--)
            for (int x = min.x; x <= max.x; x++)
            {
                var p = new Vector2Int(x, y);
                Rect r = CellRect(origin, p, min, max);

                if (!map.TryGetValue(p, out var cell))
                {
                    if (HasNeighbor(map, p) && DrawAddButton(r)) toAdd = p;
                    continue;
                }

                EditorGUI.DrawRect(Inset(r, 1), new Color(0.35f, 0.38f, 0.42f));
                GUI.Label(r, $"{p.x},{p.y}", CenteredMini());
                
                if (Event.current.type == EventType.ContextClick && r.Contains(Event.current.mousePosition))
                {
                    toRemove = cell;
                    Event.current.Use();
                }
            }

            foreach (var cell in data.cells)
            {
                Rect r = CellRect(origin, cell.pos, min, max);
                for (int d = 0; d < 4; d++)
                    DrawEdge(data, map, cell, d, r);
            }
            
            if (toAdd.HasValue) AddCell(data, map, toAdd.Value);
            else if (toRemove != null && data.cells.Count > 1) RemoveCell(data, map, toRemove);
        }

        private void DrawEdge(RoomData data, Dictionary<Vector2Int, RoomCell> map,
            RoomCell cell, int dir, Rect cellRect)
        {
            Rect e = EdgeRect(cellRect, dir);
            Socket s = Get(cell, dir);

            EditorGUI.DrawRect(e, SocketColor(s));
            EditorGUIUtility.AddCursorRect(e, MouseCursor.Link);

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0
                && e.Contains(Event.current.mousePosition))
            {
                Undo.RecordObject(data, "Paint Room Socket");
                Set(cell, dir, brush);
                
                if (map.TryGetValue(cell.pos + Dirs[dir], out var neighbor))
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
        

        private void AddCell(RoomData data, Dictionary<Vector2Int, RoomCell> map, Vector2Int pos)
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

        private void RemoveCell(RoomData data, Dictionary<Vector2Int, RoomCell> map, RoomCell cell)
        {
            Undo.RecordObject(data, "Remove Room Cell");
            for (int d = 0; d < 4; d++)
                if (map.TryGetValue(cell.pos + Dirs[d], out var n))
                    Set(n, (d + 2) % 4, Socket.Wall); // close the wall left behind
            data.cells.Remove(cell);
            EditorUtility.SetDirty(data);
        }
        

        private static bool HasNeighbor(Dictionary<Vector2Int, RoomCell> map, Vector2Int p)
        {
            foreach (var d in Dirs) if (map.ContainsKey(p + d)) return true;
            return false;
        }

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
                case Socket.Wall:   return new Color(0.10f, 0.10f, 0.10f);
                case Socket.Door:   return new Color(0.90f, 0.55f, 0.15f);
                case Socket.Window: return new Color(0.35f, 0.75f, 0.95f);
                default:            return new Color(0.30f, 0.30f, 0.30f, 0.35f); 
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