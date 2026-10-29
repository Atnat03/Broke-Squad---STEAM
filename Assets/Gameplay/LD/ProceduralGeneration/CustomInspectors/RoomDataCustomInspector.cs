using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Gameplay.LD.ProceduralGeneration
{
    [CustomEditor(typeof(RoomData))]
    public class RoomDataCustomInspector : Editor
    {
        private const float CellSizeInPixels = 70f;
        private const float EdgeThicknessInPixels = 12f;
        private const float SideCellSizeInPixels = 40f;
        private const int NumberOfFloors = 2;

        // 0 = north, 1 = east, 2 = south, 3 = west
        private static readonly Vector3Int[] directionOffsets =
        {
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(-1, 0, 0)
        };

        private static readonly string[] brushNames = { "Wall", "Door", "None" };
        private static readonly Socket[] brushSockets = { Socket.Wall, Socket.Door, Socket.None };
        private static readonly string[] floorNames = { "Floor 0", "Floor 1" };
        private static readonly string[] sideModeNames = { "Front (X/Z)", "Side (Y/Z)" };

        private int selectedBrushIndex = 1;
        private int selectedFloor = 0;
        private int selectedSideMode = 0;
        private bool showRawCellData = false;
        private Editor cachedPrefabEditor;

        private void OnDisable()
        {
            if (cachedPrefabEditor != null)
            {
                DestroyImmediate(cachedPrefabEditor);
            }
        }

        public override bool HasPreviewGUI()
        {
            RoomData roomData = (RoomData)target;
            return roomData.prefab != null;
        }

        public override void OnPreviewGUI(Rect previewRect, GUIStyle background)
        {
            RoomData roomData = (RoomData)target;
            GameObject prefab = roomData.prefab;
            if (prefab == null) return;

            if (cachedPrefabEditor == null || cachedPrefabEditor.target != prefab)
            {
                CreateCachedEditor(prefab, null, ref cachedPrefabEditor);
            }
            cachedPrefabEditor.OnPreviewGUI(previewRect, background);
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawPropertiesExcluding(serializedObject, "m_Script", "cells");
            serializedObject.ApplyModifiedProperties();

            RoomData roomData = (RoomData)target;
            if (roomData.cells == null || roomData.cells.Count == 0)
            {
                roomData.cells = new List<RoomCell>();
                roomData.cells.Add(new RoomCell());
                EditorUtility.SetDirty(roomData);
            }

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Room Layout", EditorStyles.boldLabel);
            selectedBrushIndex = GUILayout.Toolbar(selectedBrushIndex, brushNames, GUILayout.Height(24));
            selectedFloor = GUILayout.Toolbar(selectedFloor, floorNames, GUILayout.Height(22));

            DrawTopDownGrid(roomData);

            EditorGUILayout.Space(8);
            EditorGUILayout.LabelField("Side View", EditorStyles.boldLabel);
            DrawSideView(roomData);

            DrawWarnings(roomData);

            showRawCellData = EditorGUILayout.Foldout(showRawCellData, "Raw cell data", true);
            if (!showRawCellData) return;

            serializedObject.Update();
            EditorGUILayout.PropertyField(serializedObject.FindProperty("cells"), true);
            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawWarnings(RoomData roomData)
        {
            foreach (RoomCell cell in roomData.cells)
            {
                if (cell.pos.z < 0 || cell.pos.z >= NumberOfFloors)
                {
                    EditorGUILayout.HelpBox("Some cells are outside floors 0-1.", MessageType.Error);
                    break;
                }
            }

            if (!AreAllCellsConnected(roomData.cells))
            {
                EditorGUILayout.HelpBox("Some cells are not connected to the rest of the room.", MessageType.Warning);
            }
        }

        private static bool AreAllCellsConnected(List<RoomCell> cells)
        {
            HashSet<Vector3Int> allPositions = new HashSet<Vector3Int>();
            foreach (RoomCell cell in cells)
            {
                allPositions.Add(cell.pos);
            }

            HashSet<Vector3Int> visitedPositions = new HashSet<Vector3Int>();
            visitedPositions.Add(cells[0].pos);

            Queue<Vector3Int> queue = new Queue<Vector3Int>();
            queue.Enqueue(cells[0].pos);

            while (queue.Count > 0)
            {
                Vector3Int currentPosition = queue.Dequeue();
                List<Vector3Int> neighbours = GetNeighbourPositions(currentPosition);

                foreach (Vector3Int neighbour in neighbours)
                {
                    if (allPositions.Contains(neighbour) && !visitedPositions.Contains(neighbour))
                    {
                        visitedPositions.Add(neighbour);
                        queue.Enqueue(neighbour);
                    }
                }
            }

            return visitedPositions.Count == allPositions.Count;
        }

        private static List<Vector3Int> GetNeighbourPositions(Vector3Int position)
        {
            List<Vector3Int> neighbours = new List<Vector3Int>();
            for (int i = 0; i < directionOffsets.Length; i++)
            {
                neighbours.Add(position + directionOffsets[i]);
            }
            neighbours.Add(position + Vector3Int.forward);
            neighbours.Add(position - Vector3Int.forward);
            return neighbours;
        }

        private void DrawTopDownGrid(RoomData roomData)
        {
            Dictionary<Vector3Int, RoomCell> cellsByPosition = new Dictionary<Vector3Int, RoomCell>();
            Vector2Int minPosition = new Vector2Int(roomData.cells[0].pos.x, roomData.cells[0].pos.y);
            Vector2Int maxPosition = minPosition;

            foreach (RoomCell cell in roomData.cells)
            {
                cellsByPosition[cell.pos] = cell;
                minPosition = Vector2Int.Min(minPosition, new Vector2Int(cell.pos.x, cell.pos.y));
                maxPosition = Vector2Int.Max(maxPosition, new Vector2Int(cell.pos.x, cell.pos.y));
            }
            
            minPosition -= Vector2Int.one;
            maxPosition += Vector2Int.one;

            int columnCount = maxPosition.x - minPosition.x + 1;
            int rowCount = maxPosition.y - minPosition.y + 1;

            Rect gridArea = GUILayoutUtility.GetRect(columnCount * CellSizeInPixels, rowCount * CellSizeInPixels + 10, GUILayout.ExpandWidth(true));
            Vector2 gridOrigin = new Vector2(gridArea.x + (gridArea.width - columnCount * CellSizeInPixels) * 0.5f, gridArea.y + 5);
            EditorGUI.DrawRect(gridArea, new Color(0.15f, 0.15f, 0.15f, 0.4f));

            RoomCell cellToRemove = null;
            bool hasCellToAdd = false;
            Vector3Int positionToAdd = Vector3Int.zero;
            Event currentEvent = Event.current;

            for (int y = minPosition.y; y <= maxPosition.y; y++)
            {
                for (int x = minPosition.x; x <= maxPosition.x; x++)
                {
                    Vector3Int position = new Vector3Int(x, y, selectedFloor);

                    Rect cellRect = new Rect(
                        gridOrigin.x + (x - minPosition.x) * CellSizeInPixels,
                        gridOrigin.y + (y - minPosition.y) * CellSizeInPixels,
                        CellSizeInPixels,
                        CellSizeInPixels);
                    Rect innerRect = new Rect(cellRect.x + 1, cellRect.y + 1, cellRect.width - 2, cellRect.height - 2);

                    if (!cellsByPosition.ContainsKey(position))
                    {
                        // show a faint square if the other floor has a cell here
                        Vector3Int positionOnOtherFloor = new Vector3Int(x, y, 1 - selectedFloor);
                        if (cellsByPosition.ContainsKey(positionOnOtherFloor))
                        {
                            EditorGUI.DrawRect(innerRect, new Color(0.35f, 0.38f, 0.42f, 0.18f));
                        }

                        if (CanAddCellAt(cellsByPosition, position))
                        {
                            Rect buttonRect = new Rect(cellRect.x + 12, cellRect.y + 12, cellRect.width - 24, cellRect.height - 24);
                            if (GUI.Button(buttonRect, "+"))
                            {
                                hasCellToAdd = true;
                                positionToAdd = position;
                            }
                        }
                        continue;
                    }

                    RoomCell cell = cellsByPosition[position];

                    EditorGUI.DrawRect(innerRect, new Color(0.35f, 0.38f, 0.42f));
                    GUI.Label(cellRect, x + "," + y + "," + selectedFloor, EditorStyles.centeredGreyMiniLabel);

                    if (currentEvent.type == EventType.ContextClick && cellRect.Contains(currentEvent.mousePosition))
                    {
                        cellToRemove = cell;
                        currentEvent.Use();
                    }

                    for (int direction = 0; direction < 4; direction++)
                    {
                        Rect edgeRect = GetEdgeRect(cellRect, direction);
                        DrawEdge(roomData, cellsByPosition, cell, direction, edgeRect);
                    }
                }
            }

            if (hasCellToAdd)
            {
                AddCell(roomData, cellsByPosition, positionToAdd);
            }
            else if (cellToRemove != null && roomData.cells.Count > 1)
            {
                RemoveCell(roomData, cellsByPosition, cellToRemove);
            }
        }

        private void DrawSideView(RoomData roomData)
        {
            selectedSideMode = GUILayout.Toolbar(selectedSideMode, sideModeNames, GUILayout.Height(22));
            
            Dictionary<Vector2Int, int> cellCountBySquare = new Dictionary<Vector2Int, int>();
            HashSet<Vector3Int> allPositions = new HashSet<Vector3Int>();
            foreach (RoomCell cell in roomData.cells)
            {
                allPositions.Add(cell.pos);
            }

            int minHorizontal = int.MaxValue;
            int maxHorizontal = int.MinValue;

            foreach (RoomCell cell in roomData.cells)
            {
                int horizontal;
                if (selectedSideMode == 0)
                {
                    horizontal = cell.pos.x;
                }
                else
                {
                    horizontal = cell.pos.y;
                }

                int clampedFloor = Mathf.Clamp(cell.pos.z, 0, NumberOfFloors - 1);
                Vector2Int key = new Vector2Int(horizontal, clampedFloor);

                if (cellCountBySquare.ContainsKey(key))
                {
                    cellCountBySquare[key] = cellCountBySquare[key] + 1;
                }
                else
                {
                    cellCountBySquare[key] = 1;
                }

                minHorizontal = Mathf.Min(minHorizontal, horizontal);
                maxHorizontal = Mathf.Max(maxHorizontal, horizontal);
            }

            int columnCount = maxHorizontal - minHorizontal + 1;
            Rect sideArea = GUILayoutUtility.GetRect(columnCount * SideCellSizeInPixels + 20, NumberOfFloors * SideCellSizeInPixels + 20, GUILayout.ExpandWidth(true));
            Vector2 sideOrigin = new Vector2(sideArea.x + (sideArea.width - columnCount * SideCellSizeInPixels) * 0.5f, sideArea.y + 10);
            EditorGUI.DrawRect(sideArea, new Color(0.15f, 0.15f, 0.15f, 0.4f));

            foreach (KeyValuePair<Vector2Int, int> pair in cellCountBySquare)
            {
                int horizontal = pair.Key.x;
                int squareFloor = pair.Key.y;
                int cellCount = pair.Value;

                Rect squareRect = new Rect(
                    sideOrigin.x + (horizontal - minHorizontal) * SideCellSizeInPixels,
                    sideOrigin.y + (NumberOfFloors - 1 - squareFloor) * SideCellSizeInPixels,
                    SideCellSizeInPixels,
                    SideCellSizeInPixels);
                Rect innerRect = new Rect(squareRect.x + 1, squareRect.y + 1, squareRect.width - 2, squareRect.height - 2);
                EditorGUI.DrawRect(innerRect, new Color(0.35f, 0.38f, 0.42f));

                string label = "F" + squareFloor;
                if (cellCount > 1)
                {
                    label = label + " x" + cellCount;
                }
                GUI.Label(squareRect, label, EditorStyles.centeredGreyMiniLabel);
                
                bool hasCellAbove = false;
                foreach (Vector3Int position in allPositions)
                {
                    int positionHorizontal;
                    if (selectedSideMode == 0)
                    {
                        positionHorizontal = position.x;
                    }
                    else
                    {
                        positionHorizontal = position.y;
                    }

                    if (position.z == squareFloor && positionHorizontal == horizontal && allPositions.Contains(position + Vector3Int.forward))
                    {
                        hasCellAbove = true;
                        break;
                    }
                }

                float barHeight = 2f;
                Color barColor = new Color(0.10f, 0.10f, 0.10f);
                if (hasCellAbove)
                {
                    barHeight = 4f;
                    barColor = new Color(0.40f, 0.80f, 0.40f);
                }
                EditorGUI.DrawRect(new Rect(squareRect.x + 1, squareRect.y, squareRect.width - 2, barHeight), barColor);
            }
        }

        private void DrawEdge(RoomData roomData, Dictionary<Vector3Int, RoomCell> cellsByPosition, RoomCell cell, int direction, Rect edgeRect)
        {
            EditorGUI.DrawRect(edgeRect, GetSocketColor(GetSocket(cell, direction)));
            EditorGUIUtility.AddCursorRect(edgeRect, MouseCursor.Link);

            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.MouseDown) return;
            if (currentEvent.button != 0) return;
            if (!edgeRect.Contains(currentEvent.mousePosition)) return;

            Socket brushSocket = brushSockets[selectedBrushIndex];

            Undo.RecordObject(roomData, "Paint Room Socket");
            SetSocket(cell, direction, brushSocket);
            
            Vector3Int neighbourPosition = cell.pos + directionOffsets[direction];
            if (cellsByPosition.ContainsKey(neighbourPosition))
            {
                RoomCell neighbourCell = cellsByPosition[neighbourPosition];
                int oppositeDirection = (direction + 2) % 4;
                SetSocket(neighbourCell, oppositeDirection, brushSocket);
            }

            EditorUtility.SetDirty(roomData);
            currentEvent.Use();
            Repaint();
        }

        private static void AddCell(RoomData roomData, Dictionary<Vector3Int, RoomCell> cellsByPosition, Vector3Int position)
        {
            Undo.RecordObject(roomData, "Add Room Cell");

            RoomCell newCell = new RoomCell();
            newCell.pos = position;

            for (int direction = 0; direction < 4; direction++)
            {
                Vector3Int neighbourPosition = position + directionOffsets[direction];
                if (cellsByPosition.ContainsKey(neighbourPosition))
                {
                    
                    SetSocket(newCell, direction, Socket.None);
                    SetSocket(cellsByPosition[neighbourPosition], (direction + 2) % 4, Socket.None);
                }
                else
                {
                    SetSocket(newCell, direction, Socket.Wall);
                }
            }

            roomData.cells.Add(newCell);
            EditorUtility.SetDirty(roomData);
        }

        private static void RemoveCell(RoomData roomData, Dictionary<Vector3Int, RoomCell> cellsByPosition, RoomCell cellToRemove)
        {
            Undo.RecordObject(roomData, "Remove Room Cell");
            
            for (int direction = 0; direction < 4; direction++)
            {
                Vector3Int neighbourPosition = cellToRemove.pos + directionOffsets[direction];
                if (cellsByPosition.ContainsKey(neighbourPosition))
                {
                    SetSocket(cellsByPosition[neighbourPosition], (direction + 2) % 4, Socket.Wall);
                }
            }

            roomData.cells.Remove(cellToRemove);
            EditorUtility.SetDirty(roomData);
        }

        private static bool CanAddCellAt(Dictionary<Vector3Int, RoomCell> cellsByPosition, Vector3Int position)
        {
            for (int i = 0; i < directionOffsets.Length; i++)
            {
                if (cellsByPosition.ContainsKey(position + directionOffsets[i])) return true;
            }
            return cellsByPosition.ContainsKey(new Vector3Int(position.x, position.y, 1 - position.z));
        }

        private static Rect GetEdgeRect(Rect cellRect, int direction)
        {
            float t = EdgeThicknessInPixels;
            if (direction == 0)
            {
                return new Rect(cellRect.x + t, cellRect.y, cellRect.width - 2 * t, t);
            }
            if (direction == 1)
            {
                return new Rect(cellRect.xMax - t, cellRect.y + t, t, cellRect.height - 2 * t);
            }
            if (direction == 2)
            {
                return new Rect(cellRect.x + t, cellRect.yMax - t, cellRect.width - 2 * t, t);
            }
            return new Rect(cellRect.x, cellRect.y + t, t, cellRect.height - 2 * t);
        }

        private static Color GetSocketColor(Socket socket)
        {
            if (socket == Socket.Wall) return new Color(0.10f, 0.10f, 0.10f);
            if (socket == Socket.Door) return new Color(0.90f, 0.55f, 0.15f);
            return new Color(0.30f, 0.30f, 0.30f, 0.35f);
        }

        private static Socket GetSocket(RoomCell cell, int direction)
        {
            if (direction == 0) return cell.north;
            if (direction == 1) return cell.east;
            if (direction == 2) return cell.south;
            return cell.west;
        }

        private static void SetSocket(RoomCell cell, int direction, Socket socket)
        {
            if (direction == 0) cell.north = socket;
            else if (direction == 1) cell.east = socket;
            else if (direction == 2) cell.south = socket;
            else cell.west = socket;
        }
    }
}