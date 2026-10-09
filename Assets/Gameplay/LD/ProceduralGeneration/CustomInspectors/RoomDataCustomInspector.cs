using UnityEditor;
using UnityEngine;

namespace Gameplay.LD.ProceduralGeneration
{
    [CustomEditor(typeof(RoomData))]
    public class RoomDataCustomInspector : Editor
    {
        private const float CellSize = 70f;
        private const float EdgeThickness = 12f;
        private Socket brush = Socket.Door;
        
        private static Socket Get(RoomCell c, int d) => d switch
        {
            0 => c.north,
            1 => c.east,
            2 => c.south,
            _ => c.west
        };
        
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
        
        
        private static Rect EdgeRect(Rect c, int dir)
        {
            float t = EdgeThickness;
            switch (dir)
            {
                case 0:  return new Rect(c.x + t,c.y,c.width - 2*t, t);
                case 1:  return new Rect(c.xMax - t, c.y + t, t, c.height - 2*t);
                case 2:  return new Rect(c.x + t, c.yMax - t, c.width - 2*t, t);
                default: return new Rect(c.x, c.y + t, t, c.height - 2*t);
            }
        }
        
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Room Layout", EditorStyles.boldLabel);
            
            DrawGrid((RoomData)target);
        }


        private void DrawGrid(RoomData roomData)
        {
            Rect area = GUILayoutUtility.GetRect(CellSize * 3, CellSize * 3);
            Rect cellRect = new Rect(area.x + CellSize, area.y + CellSize, CellSize, CellSize);
            EditorGUI.DrawRect(cellRect, new Color(0.35f, 0.38f, 0.42f));

            RoomCell cell = roomData.cells[0];
            for (int d = 0; d < 4; d++)
            {
                EditorGUI.DrawRect(EdgeRect(cellRect, d), SocketColor(Get(cell, d)));                
            }
        }
        
        private void DrawEdge(RoomData data, RoomCell cell, int dir, Rect cellRect)
        {
            Rect e = EdgeRect(cellRect, dir);
            EditorGUI.DrawRect(e, SocketColor(Get(cell, dir)));
            EditorGUIUtility.AddCursorRect(e, MouseCursor.Link);

            Event ev = Event.current;
            if (ev.type == EventType.MouseDown && ev.button == 0 && e.Contains(ev.mousePosition))
            {
                Undo.RecordObject(data, "Paint Room Socket");  
                Set(cell, dir, brush);
                EditorUtility.SetDirty(data);                   
                ev.Use();                                      
                Repaint();
            }
        }
    }
}