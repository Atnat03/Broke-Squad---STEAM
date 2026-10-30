using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

namespace Gameplay.LD.ProceduralGeneration
{
    public class RoomVariant
    {
        public RoomData roomData;
        public int rotationIndex;
        public Vector3Int[] cellPositions;
        public HashSet<Vector3Int> cellPositionsSet;
        public Socket[][] cellSockets;
        public Vector3Int pivotOffset;
        public int exitDoorCount;
    }

    public class PlacedRoom
    {
        public int id;
        public RoomVariant variant;
        public Vector3Int origin;
        public Vector3Int PivotCell { get { return origin + variant.pivotOffset; } }
    }

    public class MapResult
    {
        public List<PlacedRoom> rooms;
        public List<DoorConnection> doors;
        public PlacedRoom start;
        public PlacedRoom final;
        public int pathLength;
        public int attempts;
    }
    
    public class OpenDoor
    {
        public Vector3Int cellPosition;
        public int direction;
    }
    
    public class PlacementOption
    {
        public RoomVariant variant;
        public Vector3Int origin;
        public int weight;
    }
    
    public class DoorConnection
    {
        public Vector3Int cellA;
        public Vector3Int cellB;
        public int direction;
        public int roomIdA;
        public int roomIdB;
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
        
        private static readonly Vector3Int[] directionOffsets =
        {
            new Vector3Int(0, -1, 0),
            new Vector3Int(1, 0, 0),
            new Vector3Int(0, 1, 0),
            new Vector3Int(-1, 0, 0)
        };

        struct GridCell
        {
            public int roomId;
            public Socket[] sockets;
        }

        Dictionary<Vector3Int, GridCell> gridCells = new Dictionary<Vector3Int, GridCell>();
        List<PlacedRoom> placedRooms = new List<PlacedRoom>();
        Dictionary<RoomData, int> roomUsedCount = new Dictionary<RoomData, int>();
        Dictionary<RoomData, List<RoomVariant>> variantsCache = new Dictionary<RoomData, List<RoomVariant>>();

        RoomData currentFinalRoomData;
        int nextRoomId;
        int solveCallCount;
        bool timeIsUp;
        Stopwatch stopwatch;
        
        static List<RoomVariant> BuildVariantsOfRoom(RoomData roomData)
        {
            List<RoomVariant> allVariants = new List<RoomVariant>();
            HashSet<string> alreadySeenKeys = new HashSet<string>();

            int cellCount = roomData.cells.Count;
            Vector3Int[] currentPositions = new Vector3Int[cellCount];
            Socket[][] currentSockets = new Socket[cellCount][];

            for (int i = 0; i < cellCount; i++)
            {
                currentPositions[i] = roomData.cells[i].pos;
                currentSockets[i] = roomData.cells[i].ToArray();
            }

            Vector3Int currentPivot = Vector3Int.zero;

            int numberOfRotations = 1;
            if (roomData.allowRotation)
            {
                numberOfRotations = 4;
            }

            for (int rotationIndex = 0; rotationIndex < numberOfRotations; rotationIndex++)
            {
                if (rotationIndex > 0)
                {
                    for (int i = 0; i < cellCount; i++)
                    {
                        Vector3Int oldPosition = currentPositions[i];
                        currentPositions[i] = new Vector3Int(-oldPosition.y, oldPosition.x, oldPosition.z);

                        Socket[] oldSockets = currentSockets[i];
                        Socket[] newSockets = new Socket[4];
                        newSockets[0] = oldSockets[3];
                        newSockets[1] = oldSockets[0];
                        newSockets[2] = oldSockets[1];
                        newSockets[3] = oldSockets[2];
                        currentSockets[i] = newSockets;
                    }
                    currentPivot = new Vector3Int(-currentPivot.y, currentPivot.x, currentPivot.z);
                }
                
                Vector3Int firstCell = currentPositions[0];
                for (int i = 1; i < cellCount; i++)
                {
                    Vector3Int candidate = currentPositions[i];
                    bool isBefore = false;
                    if (candidate.z < firstCell.z)
                    {
                        isBefore = true;
                    }
                    else if (candidate.z == firstCell.z && candidate.y < firstCell.y)
                    {
                        isBefore = true;
                    }
                    else if (candidate.z == firstCell.z && candidate.y == firstCell.y && candidate.x < firstCell.x)
                    {
                        isBefore = true;
                    }
                    if (isBefore)
                    {
                        firstCell = candidate;
                    }
                }

                RoomVariant newVariant = new RoomVariant();
                newVariant.roomData = roomData;
                newVariant.rotationIndex = rotationIndex;
                newVariant.cellPositions = new Vector3Int[cellCount];
                newVariant.cellSockets = new Socket[cellCount][];
                newVariant.pivotOffset = currentPivot - firstCell;
                newVariant.cellPositionsSet = new HashSet<Vector3Int>();

                for (int i = 0; i < cellCount; i++)
                {
                    newVariant.cellPositions[i] = currentPositions[i] - firstCell;
                    newVariant.cellSockets[i] = (Socket[])currentSockets[i].Clone();
                    newVariant.cellPositionsSet.Add(newVariant.cellPositions[i]);
                }
                
                int exitDoors = 0;
                for (int i = 0; i < cellCount; i++)
                {
                    for (int dir = 0; dir < 4; dir++)
                    {
                        bool isDoor = newVariant.cellSockets[i][dir] == Socket.Door;
                        Vector3Int neighbourPosition = newVariant.cellPositions[i] + directionOffsets[dir];
                        bool neighbourIsInsideRoom = newVariant.cellPositionsSet.Contains(neighbourPosition);
                        if (isDoor && !neighbourIsInsideRoom)
                        {
                            exitDoors++;
                        }
                    }
                }
                newVariant.exitDoorCount = exitDoors;
                
                List<int> sortedCellIndexes = new List<int>();
                for (int i = 0; i < cellCount; i++)
                {
                    sortedCellIndexes.Add(i);
                }
                sortedCellIndexes.Sort(delegate (int indexA, int indexB)
                {
                    Vector3Int a = newVariant.cellPositions[indexA];
                    Vector3Int b = newVariant.cellPositions[indexB];
                    if (a.z != b.z) return a.z.CompareTo(b.z);
                    if (a.y != b.y) return a.y.CompareTo(b.y);
                    return a.x.CompareTo(b.x);
                });

                System.Text.StringBuilder keyBuilder = new System.Text.StringBuilder();
                foreach (int cellIndex in sortedCellIndexes)
                {
                    Vector3Int p = newVariant.cellPositions[cellIndex];
                    keyBuilder.Append(p.x).Append(',').Append(p.y).Append(',').Append(p.z).Append(':');
                    for (int dir = 0; dir < 4; dir++)
                    {
                        keyBuilder.Append((int)newVariant.cellSockets[cellIndex][dir]);
                    }
                    keyBuilder.Append(';');
                }

                string variantKey = keyBuilder.ToString();
                if (alreadySeenKeys.Add(variantKey))
                {
                    allVariants.Add(newVariant);
                }
            }

            return allVariants;
        }

        List<RoomVariant> GetVariants(RoomData roomData)
        {
            if (!variantsCache.ContainsKey(roomData))
            {
                variantsCache[roomData] = BuildVariantsOfRoom(roomData);
            }
            return variantsCache[roomData];
        }
        
        bool IsInsideBounds(Vector3Int position)
        {
            if (position.z < 0) return false;
            if (position.z >= floorCount) return false;
            if (Math.Abs(position.x - startOrigin.x) > maxExtent) return false;
            if (Math.Abs(position.y - startOrigin.y) > maxExtent) return false;
            return true;
        }

        bool CanPlaceRoom(RoomVariant variant, Vector3Int origin)
        {
            for (int i = 0; i < variant.cellPositions.Length; i++)
            {
                Vector3Int worldPosition = variant.cellPositions[i] + origin;
                if (!IsInsideBounds(worldPosition)) return false;
                if (gridCells.ContainsKey(worldPosition)) return false;
            }
            
            for (int i = 0; i < variant.cellPositions.Length; i++)
            {
                Vector3Int worldPosition = variant.cellPositions[i] + origin;

                for (int dir = 0; dir < 4; dir++)
                {
                    Vector3Int neighbourWorldPosition = worldPosition + directionOffsets[dir];
                    Vector3Int neighbourLocalPosition = neighbourWorldPosition - origin;

                  
                    if (variant.cellPositionsSet.Contains(neighbourLocalPosition)) continue;

                    bool myselfHasDoor = variant.cellSockets[i][dir] == Socket.Door;

                    if (gridCells.TryGetValue(neighbourWorldPosition, out var neighbourCell))
                    {
                        int oppositeDirection = (dir + 2) % 4;
                        bool neighbourHasDoor = neighbourCell.sockets[oppositeDirection] == Socket.Door;
                        
                        if (myselfHasDoor != neighbourHasDoor) return false;
                    }
                    else
                    {
                        if (myselfHasDoor && !IsInsideBounds(neighbourWorldPosition)) return false;
                    }
                }
            }

            return true;
        }

        PlacedRoom PlaceRoom(RoomVariant variant, Vector3Int origin)
        {
            PlacedRoom newRoom = new PlacedRoom();
            newRoom.id = nextRoomId;
            nextRoomId++;
            newRoom.variant = variant;
            newRoom.origin = origin;

            for (int i = 0; i < variant.cellPositions.Length; i++)
            {
                GridCell newCell = new GridCell();
                newCell.roomId = newRoom.id;
                newCell.sockets = variant.cellSockets[i];
                gridCells[variant.cellPositions[i] + origin] = newCell;
            }

            placedRooms.Add(newRoom);

            if (roomUsedCount.ContainsKey(variant.roomData))
            {
                roomUsedCount[variant.roomData] = roomUsedCount[variant.roomData] + 1;
            }
            else
            {
                roomUsedCount[variant.roomData] = 1;
            }

            return newRoom;
        }
        
        void RemoveRoom(PlacedRoom room)
        {
            for (int i = 0; i < room.variant.cellPositions.Length; i++)
            {
                gridCells.Remove(room.variant.cellPositions[i] + room.origin);
            }
            placedRooms.Remove(room);
            roomUsedCount[room.variant.roomData] = roomUsedCount[room.variant.roomData] - 1;
        }

        List<OpenDoor> FindOpenDoors()
        {
            List<OpenDoor> openDoors = new List<OpenDoor>();

            foreach (KeyValuePair<Vector3Int, GridCell> pair in gridCells)
            {
                for (int dir = 0; dir < 4; dir++)
                {
                    bool hasDoor = pair.Value.sockets[dir] == Socket.Door;
                    Vector3Int neighbourPosition = pair.Key + directionOffsets[dir];
                    bool neighbourIsEmpty = !gridCells.ContainsKey(neighbourPosition);

                    if (hasDoor && neighbourIsEmpty)
                    {
                        OpenDoor openDoor = new OpenDoor();
                        openDoor.cellPosition = pair.Key;
                        openDoor.direction = dir;
                        openDoors.Add(openDoor);
                    }
                }
            }

            return openDoors;
        }

        bool IsFinalRoomPlaced()
        {
            if (!roomUsedCount.TryGetValue(currentFinalRoomData, out var value)) return false;
            return value > 0;
        }

        int[] CountCellsPerFloor()
        {
            int[] cellsPerFloor = new int[floorCount];
            foreach (Vector3Int position in gridCells.Keys)
            {
                cellsPerFloor[position.z]++;
            }
            return cellsPerFloor;
        }
        
        int ComputeWeight(RoomVariant variant, Vector3Int origin, int[] cellsPerFloor, int smallestFloorCount)
        {
            int weight = variant.roomData.weight * 10;

            
            int newOpenDoors = Math.Max(1, variant.exitDoorCount - 1);

            if (placedRooms.Count < targetRooms)
            {
                weight = weight * newOpenDoors;   
            }
            else
            {
                weight = weight / newOpenDoors;  
            }
            
            if (variant.roomData == currentFinalRoomData && !IsFinalRoomPlaced() && placedRooms.Count > targetRooms / 2)
            {
                weight = weight * 5;
            }
            
            for (int i = 0; i < variant.cellPositions.Length; i++)
            {
                int floorIndex = variant.cellPositions[i].z + origin.z;
                if (cellsPerFloor[floorIndex] == smallestFloorCount)
                {
                    weight = weight * 3;
                    break;
                }
            }

            return Math.Max(1, weight);
        }
        
        List<PlacementOption> FindOptionsForDoor(OpenDoor openDoor, List<RoomData> fillerRooms)
        {
            List<PlacementOption> options = new List<PlacementOption>();

            Vector3Int targetCell = openDoor.cellPosition + directionOffsets[openDoor.direction];
            int neededDoorSide = (openDoor.direction + 2) % 4;

            foreach (RoomData roomData in fillerRooms)
            {
                int alreadyUsed = 0;
                if (roomUsedCount.ContainsKey(roomData))
                {
                    alreadyUsed = roomUsedCount[roomData];
                }

                int maxAllowed = roomData.maxCount;
                if (roomData == currentFinalRoomData)
                {
                    maxAllowed = 1;
                }
                if (alreadyUsed >= maxAllowed) continue;

                List<RoomVariant> variants = GetVariants(roomData);
                foreach (RoomVariant variant in variants)
                {
                    for (int i = 0; i < variant.cellPositions.Length; i++)
                    {
                        if (variant.cellSockets[i][neededDoorSide] == Socket.Door)
                        {
                            Vector3Int possibleOrigin = targetCell - variant.cellPositions[i];
                            if (CanPlaceRoom(variant, possibleOrigin))
                            {
                                PlacementOption option = new PlacementOption();
                                option.variant = variant;
                                option.origin = possibleOrigin;
                                options.Add(option);
                            }
                        }
                    }
                }
            }

            return options;
        }

      
        List<DoorConnection> FindDoorConnections()
        {
            List<DoorConnection> connections = new List<DoorConnection>();

            foreach (KeyValuePair<Vector3Int, GridCell> pair in gridCells)
            {
                for (int dir = 1; dir <= 2; dir++)
                {
                    if (pair.Value.sockets[dir] != Socket.Door) continue;

                    Vector3Int neighbourPosition = pair.Key + directionOffsets[dir];
                    if (!gridCells.TryGetValue(neighbourPosition, out var neighbourCell)) continue;

                    if (neighbourCell.roomId == pair.Value.roomId) continue;

                    int oppositeDirection = (dir + 2) % 4;
                    if (neighbourCell.sockets[oppositeDirection] != Socket.Door) continue;

                    DoorConnection connection = new DoorConnection();
                    connection.cellA = pair.Key;
                    connection.cellB = neighbourPosition;
                    connection.direction = dir;
                    connection.roomIdA = pair.Value.roomId;
                    connection.roomIdB = neighbourCell.roomId;
                    connections.Add(connection);
                }
            }

            return connections;
        }
        
        bool Solve(System.Random random, List<RoomData> fillerRooms)
        {
            if (timeIsUp) return false;

            solveCallCount++;
            if (solveCallCount > solverBudget) return false;
            
            if (solveCallCount % 32 == 0 && stopwatch.ElapsedMilliseconds > maxMillis)
            {
                timeIsUp = true;
                return false;
            }

            List<OpenDoor> openDoors = FindOpenDoors();
            
            if (openDoors.Count == 0)
            {
                if (placedRooms.Count < minRooms) return false;
                if (!IsFinalRoomPlaced()) return false;
                return true;
            }
            
            List<PlacementOption> bestOptions = null;
            int randomStartIndex = random.Next(openDoors.Count);

            for (int k = 0; k < openDoors.Count; k++)
            {
                OpenDoor currentDoor = openDoors[(randomStartIndex + k) % openDoors.Count];
                List<PlacementOption> optionsForThisDoor = FindOptionsForDoor(currentDoor, fillerRooms);
                
                if (optionsForThisDoor.Count == 0) return false;

                if (bestOptions == null || optionsForThisDoor.Count < bestOptions.Count)
                {
                    bestOptions = optionsForThisDoor;
                }
                
                if (bestOptions.Count == 1) break;
            }

            int[] cellsPerFloor = CountCellsPerFloor();
            int smallestFloorCount = int.MaxValue;
            for (int i = 0; i < cellsPerFloor.Length; i++)
            {
                if (cellsPerFloor[i] < smallestFloorCount)
                {
                    smallestFloorCount = cellsPerFloor[i];
                }
            }

            foreach (PlacementOption option in bestOptions)
            {
                option.weight = ComputeWeight(option.variant, option.origin, cellsPerFloor, smallestFloorCount);
            }
            
            while (bestOptions.Count > 0)
            {
                int totalWeight = 0;
                for (int i = 0; i < bestOptions.Count; i++)
                {
                    totalWeight += bestOptions[i].weight;
                }

                int roll = random.Next(totalWeight);
                int chosenIndex = 0;
                for (int i = 0; i < bestOptions.Count; i++)
                {
                    roll -= bestOptions[i].weight;
                    if (roll < 0)
                    {
                        chosenIndex = i;
                        break;
                    }
                    chosenIndex = i;
                }

                PlacementOption chosenOption = bestOptions[chosenIndex];
                bestOptions.RemoveAt(chosenIndex);

                PlacedRoom newRoom = PlaceRoom(chosenOption.variant, chosenOption.origin);

                if (Solve(random, fillerRooms))
                {
                    return true;
                }
                
                RemoveRoom(newRoom);

                if (timeIsUp) return false;
                if (solveCallCount > solverBudget) return false;
            }

            return false;
        }
        
        Dictionary<int, int> ComputeRoomDistances(PlacedRoom startRoom)
        {
            Dictionary<int, HashSet<int>> neighbourRooms = new Dictionary<int, HashSet<int>>();

            foreach (KeyValuePair<Vector3Int, GridCell> pair in gridCells)
            {
                for (int dir = 0; dir < 4; dir++)
                {
                    if (pair.Value.sockets[dir] != Socket.Door) continue;

                    Vector3Int neighbourPosition = pair.Key + directionOffsets[dir];
                    if (!gridCells.TryGetValue(neighbourPosition, value: out var neighbourCell)) continue;

                    if (neighbourCell.roomId == pair.Value.roomId) continue;

                    if (!neighbourRooms.ContainsKey(pair.Value.roomId))
                    {
                        neighbourRooms[pair.Value.roomId] = new HashSet<int>();
                    }
                    neighbourRooms[pair.Value.roomId].Add(neighbourCell.roomId);
                }
            }

           
            Dictionary<int, int> distances = new Dictionary<int, int>();
            distances[startRoom.id] = 1;

            Queue<int> queue = new Queue<int>();
            queue.Enqueue(startRoom.id);

            while (queue.Count > 0)
            {
                int currentRoomId = queue.Dequeue();
                if (!neighbourRooms.TryGetValue(currentRoomId, out var room)) continue;

                foreach (int neighbourId in room)
                {
                    if (!distances.ContainsKey(neighbourId))
                    {
                        distances[neighbourId] = distances[currentRoomId] + 1;
                        queue.Enqueue(neighbourId);
                    }
                }
            }

            return distances;
        }

        bool EveryFloorHasEnoughRooms()
        {
            if (minRoomsPerFloor <= 0) return true;

            HashSet<int>[] roomIdsPerFloor = new HashSet<int>[floorCount];
            for (int i = 0; i < floorCount; i++)
            {
                roomIdsPerFloor[i] = new HashSet<int>();
            }

            foreach (KeyValuePair<Vector3Int, GridCell> pair in gridCells)
            {
                roomIdsPerFloor[pair.Key.z].Add(pair.Value.roomId);
            }

            for (int i = 0; i < floorCount; i++)
            {
                if (roomIdsPerFloor[i].Count < minRoomsPerFloor) return false;
            }
            return true;
        }
        
        public MapResult Generate(int seed, IList<RoomData> pool)
        {
            List<RoomData> startRooms = new List<RoomData>();
            List<RoomData> finalRooms = new List<RoomData>();
            List<RoomData> normalRooms = new List<RoomData>();

            foreach (RoomData roomData in pool)
            {
                if (roomData.kind == RoomKind.Start)
                {
                    startRooms.Add(roomData);
                }
                else if (roomData.kind == RoomKind.Final)
                {
                    finalRooms.Add(roomData);
                }
                else
                {
                    normalRooms.Add(roomData);
                }
            }

            if (startRooms.Count == 0 || finalRooms.Count == 0)
            {
                throw new InvalidOperationException("Need a start and final room");
            }

            System.Random random = new System.Random(seed);
            stopwatch = Stopwatch.StartNew();
            timeIsUp = false;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                if (timeIsUp) break;
                if (stopwatch.ElapsedMilliseconds > maxMillis) break;
                
                gridCells.Clear();
                placedRooms.Clear();
                roomUsedCount.Clear();
                nextRoomId = 0;
                solveCallCount = 0;

                RoomData chosenStartData = startRooms[random.Next(startRooms.Count)];
                currentFinalRoomData = finalRooms[random.Next(finalRooms.Count)];

                List<RoomData> fillerRooms = new List<RoomData>(normalRooms);
                fillerRooms.Add(currentFinalRoomData);
                
                RoomVariant startVariant = GetVariants(chosenStartData)[0];
                Vector3Int startCellPosition = new Vector3Int(startOrigin.x, startOrigin.y, 0);
                
                PlacedRoom startRoom = PlaceRoom(startVariant, startCellPosition);
                
                List<OpenDoor> startOpenDoors = FindOpenDoors();
                for (int floor = 0; floor < floorCount; floor++)
                {
                    bool floorHasDoor = false;
                    foreach (OpenDoor openDoor in startOpenDoors)
                    {
                        if (openDoor.cellPosition.z == floor)
                        {
                            floorHasDoor = true;
                            break;
                        }
                    }
                    if (!floorHasDoor)
                    {
                        throw new InvalidOperationException("La room de départ n'a aucune porte à l'étage " + floor);
                    }
                }

                bool solved = Solve(random, fillerRooms);
                if (!solved) continue;

                if (!EveryFloorHasEnoughRooms()) continue;

                PlacedRoom finalRoom = null;
                foreach (PlacedRoom room in placedRooms)
                {
                    if (room.variant.roomData == currentFinalRoomData)
                    {
                        finalRoom = room;
                        break;
                    }
                }
                if (finalRoom == null) continue;

                int cellDistance = ComputeMinManhattanDistance(finalRoom, startRoom);
                if (cellDistance < minCellDistance) continue;

                Dictionary<int, int> roomDistances = ComputeRoomDistances(startRoom);
                if (!roomDistances.ContainsKey(finalRoom.id)) continue;

                int pathLength = roomDistances[finalRoom.id];
                if (pathLength < minRoomDistance) continue;

                if (requireAllReachable && roomDistances.Count != placedRooms.Count) continue;
                
                MapResult result = new MapResult
                {
                    rooms = new List<PlacedRoom>(placedRooms),
                    doors = FindDoorConnections(),
                    start = startRoom,
                    final = finalRoom,
                    pathLength = pathLength,
                    attempts = attempt
                };
                
                return result;
            }

            return null;
        }

        int ComputeMinManhattanDistance(PlacedRoom roomA, PlacedRoom roomB)
        {
            int smallestDistance = int.MaxValue;

            for (int i = 0; i < roomA.variant.cellPositions.Length; i++)
            {
                Vector3Int positionA = roomA.variant.cellPositions[i] + roomA.origin;

                for (int j = 0; j < roomB.variant.cellPositions.Length; j++)
                {
                    Vector3Int positionB = roomB.variant.cellPositions[j] + roomB.origin;

                    int distance = Math.Abs(positionA.x - positionB.x)
                                 + Math.Abs(positionA.y - positionB.y)
                                 + Math.Abs(positionA.z - positionB.z);

                    if (distance < smallestDistance)
                    {
                        smallestDistance = distance;
                    }
                }
            }

            return smallestDistance;
        }
    }
}