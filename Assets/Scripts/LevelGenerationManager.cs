using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class LevelGenerationManager : MonoBehaviour
{
    public int minRoomSize = 60;
    public int maxRoomSize = 300;
    public float minEnemyPercentage = 0.01f;
    public float maxEnemyPercentage = 0.22f;

    public GameObject floorPrefab1;
    public GameObject floorPrefab2;
    public GameObject wallPrefab;
    public GameObject doorPrefab;
    public GameObject hallwayFloorPrefab;
    public GameObject lootChestPrefab;
    public GameObject hallwayTriggerPrefab;
    public GameObject KillZonePrefab;

    public List<EnemyData> enemyTypes;
    public NavMeshSurface[] navMeshSurface;

    private const int TILE_SIZE = 2;
    private GameObject roomParent;
    private bool successfulGeneration = false;

    [Header("Generation Performance")]
    [SerializeField] private int tilesPerFrame = 10; // How many tiles to process per frame
    [SerializeField] private float maxTimePerFrame = 0.008f; // Max time in seconds per frame (8ms for 120fps target)

    private int doorsPlaced = 0;
    private List<Vector2Int> doorPositions = new List<Vector2Int>();


    public GameObject GenerateARoom(Vector3 position, Quaternion rotation)
    {
        Debug.Log("New room generating");
        // Create and return the parent immediately
        GameObject roomParentObject = new GameObject("GeneratedRoom");
        roomParentObject.transform.position = position;
        roomParentObject.transform.rotation = rotation;

        // Start the async generation
        StartCoroutine(GenerateRoomContentAsync(roomParentObject, position, rotation));

        // Return immediately so calling code has the reference
        return roomParentObject;
    }

    private IEnumerator GenerateRoomContentAsync(GameObject roomParentObject, Vector3 position, Quaternion rotation)
    {
        const int MAX_REGENERATION_ATTEMPTS = 10;
        int regenerationAttempt = 0;

        // Use the passed-in parent instead of creating a new one
        roomParent = roomParentObject;

        successfulGeneration = false;
        while (successfulGeneration == false && regenerationAttempt < MAX_REGENERATION_ATTEMPTS)
        {
            // Clear children if this is a retry
            if (regenerationAttempt > 0)
            {
                foreach (Transform child in roomParent.transform)
                {
                    Destroy(child.gameObject);
                }
                yield return null; // Wait a frame for destruction
            }

            int roomSize = Random.Range(minRoomSize, maxRoomSize);
            (int[][] layout, Vector2Int anchor) = GetNewFloor(roomSize);

            // STEP 1: Floor - Now as coroutine
            yield return StartCoroutine(GenerateFloor(layout, anchor));

            // STEP 2: Doors & Hallways - Now as coroutine
            doorsPlaced = 0;
            yield return StartCoroutine(SpawnDoors(layout, anchor));

            // Check if we successfully placed at least one door
            if (doorsPlaced > 0)
            {
                try
                {
                    GameObject zillzone = GameObject.Instantiate(KillZonePrefab, roomParent.transform);
                    DPSZone dpsScript = zillzone.GetComponent<DPSZone>();
                    dpsScript.DPS = 99999f;
                    BoxCollider collider = zillzone.GetComponent<BoxCollider>();
                    collider.center = zillzone.transform.position + (roomParentObject.transform.forward * layout.Length / 2f) - new Vector3(0, 10, 0);
                    collider.size = new Vector3(layout.Length * 3.5f, 10, layout.Length * 3.5f);
                }
                catch (System.Exception)
                { }

                Debug.Log("Doors placed more than 0");
                successfulGeneration = true;

                // Build walls - Now as coroutine
                yield return StartCoroutine(BuildWallsAround(layout, anchor));

                // Spawn loot chests - Now as coroutine
                SpawnLootChests(layout, anchor);

                Debug.Log("Nav mesh started rebaking");
                yield return StartCoroutine(BuildNavMeshAndEnemies(layout, roomSize, anchor, roomParent));
                Debug.Log("nav mesh finished baking");
            }
            else
            {
                regenerationAttempt++;
                Debug.LogWarning($"Failed to place doors, regenerating room (attempt {regenerationAttempt}/{MAX_REGENERATION_ATTEMPTS})");
            }
        }

        if (!successfulGeneration)
        {
            Debug.LogError("Failed to generate room with valid door placement after maximum attempts");
            // Optionally destroy the room if generation failed completely
            // Destroy(roomParentObject);
        }

        roomParent = null;
    }

    public void RebakeAllNavMeshes()
    {
        Debug.Log("Nav Mesh rebake");
        if (navMeshSurface != null)
        {
            foreach (NavMeshSurface navMesh in navMeshSurface)
            {

                if (navMesh != null)
                {
                    Debug.Log("Nav Mesh rebake single");
                    navMesh.BuildNavMesh();
                }
            }
        }
    }

    private IEnumerator BuildNavMeshAndEnemies(int[][] layout, int roomSize, Vector2Int anchor, GameObject roomParent)
    {
        yield return null;
        Debug.Log("Before nav mesh in method");
        RebakeAllNavMeshes();
        Debug.Log("Finished nav mesh in mehtod");
        yield return null;
        SpawnEnemies(layout, roomSize, anchor, roomParent);
        Debug.Log("Finished placing enemies");
    }

    private (int[][], Vector2Int) GetNewFloor(int roomSize)
    {
        int size = (int)(Mathf.Sqrt(roomSize) + 10);
        int[][] layout = new int[size][];
        for (int i = 0; i < size; i++) layout[i] = new int[size];

        Vector2Int anchor = new Vector2Int(size / 2 - 1, 0);
        layout[anchor.x][anchor.y] = 1;
        layout[anchor.x + 1][anchor.y] = 1;
        layout[anchor.x][anchor.y + 1] = 1;
        layout[anchor.x + 1][anchor.y + 1] = 1;

        int tilesPlaced = 4;
        Queue<Vector2Int> toCheck = new();
        HashSet<Vector2Int> visited = new();
        toCheck.Enqueue(anchor);
        visited.Add(anchor);

        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        int attempts = 0;

        while (tilesPlaced < roomSize && attempts < 500)
        {
            attempts++;
            Vector2Int current = toCheck.Dequeue();
            int offset = Random.Range(0, directions.Length);

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int dir = directions[(i + offset) % 4];
                Vector2Int neighbor = current + dir;

                if (neighbor.x < 0 || neighbor.y < 0 || neighbor.x >= size || neighbor.y >= size)
                    continue;

                if (visited.Contains(neighbor)) continue;

                int connections = 0;
                foreach (var d in directions)
                {
                    Vector2Int check = neighbor + d;
                    if (check.x >= 0 && check.y >= 0 && check.x < size && check.y < size)
                        if (layout[check.x][check.y] == 1) connections++;
                }

                float connectionBias = 0.3f + 0.3f * connections;
                if (Random.value < connectionBias)
                {
                    if (layout[neighbor.x][neighbor.y] == 0)
                    {
                        layout[neighbor.x][neighbor.y] = 1;
                        tilesPlaced++;
                    }
                    toCheck.Enqueue(neighbor);
                }
                visited.Add(neighbor);
            }

            if (toCheck.Count == 0) toCheck.Enqueue(anchor);
        }

        return (layout, anchor);
    }

    private IEnumerator GenerateFloor(int[][] layout, Vector2Int anchor)
    {
        float startTime = Time.realtimeSinceStartup;
        int tilesProcessed = 0;

        for (int x = 0; x < layout.Length; x++)
        {
            for (int y = 0; y < layout[0].Length; y++)
            {
                if (layout[x][y] == 1)
                {
                    Vector3 localPos = GetLocalOffset(x, y, anchor);
                    Vector3 worldPos = roomParent.transform.TransformPoint(localPos);
                    GameObject prefab = (x + y) % 2 == 0 ? floorPrefab1 : floorPrefab2;
                    Instantiate(prefab, worldPos, Quaternion.identity, roomParent.transform);

                    tilesProcessed++;

                    // Yield based on tiles processed or time elapsed
                    if (tilesProcessed >= tilesPerFrame ||
                        (Time.realtimeSinceStartup - startTime) > maxTimePerFrame)
                    {
                        yield return null; // Wait for next frame
                        startTime = Time.realtimeSinceStartup;
                        tilesProcessed = 0;
                    }
                }
            }
        }
    }

    private void SpawnEnemies(int[][] layout, int roomSize, Vector2Int anchor, GameObject roomParent)
    {
        List<GameObject> enemies = new();
        int maxEnemies = (int)(roomSize * Mathf.Lerp(minEnemyPercentage, maxEnemyPercentage, Random.value));
        int spawned = 0;
        int tries = 0;
        System.Random rng = new();

        while (spawned < maxEnemies && tries < maxEnemies * 10)
        {

            EnemyData enemy = enemyTypes[rng.Next(enemyTypes.Count)];
            int x = rng.Next(layout.Length - enemy.tileWidth);
            int y = rng.Next(layout[0].Length - enemy.tileHeight);
            bool canPlace = true;

            for (int i = 0; i < enemy.tileWidth && canPlace; i++)
                for (int j = 0; j < enemy.tileHeight; j++)
                    if (layout[x + i][y + j] != 1) canPlace = false;

            if (canPlace)
            {
                Vector3 localPos = GetLocalOffset(x + enemy.tileWidth / 2, y + enemy.tileHeight / 2, anchor);
                Vector3 worldPos = roomParent.transform.TransformPoint(localPos) + new Vector3(0, 1, 0);
                if (!IsPositionBlocked(worldPos + new Vector3(0, 1.5f, 0)))
                {
                    GameObject enemyInstance = Instantiate(enemy.prefab, worldPos, Quaternion.identity, roomParent.transform);
                    enemies.Add(enemyInstance);
                    spawned += enemy.tileWidth * enemy.tileHeight;
                }
            }

            tries++;
        }
        GameManager.Instance.roomEnemies[roomParent] = enemies;
    }

    private void SpawnLootChests(int[][] layout, Vector2Int anchor)
    {
        int count = Random.Range(1, 4);
        int spawned = 0;
        int tries = 0;

        while (spawned < count && tries < 5555)
        {
            int x = Random.Range(0, layout.Length);
            int y = Random.Range(0, layout[0].Length);

            if (layout[x][y] != 1)
            {
                tries++;
                continue;
            }

            // Check if position has exactly 1 adjacent wall
            int adjacentWalls = CountAdjacentWalls(layout, x, y);
            if (adjacentWalls != 1)
            {
                tries++;
                continue;
            }

            // Check if this position is too close to any door
            if (IsNearDoorPosition(x, y))
            {
                tries++;
                continue;
            }

            Vector3 localPos = GetLocalOffset(x, y, anchor);
            Vector3 worldPos = roomParent.transform.TransformPoint(localPos) + new Vector3(0, 1, 0);

            if (!IsPositionBlocked(worldPos + new Vector3(0, 1, 0)))
            {
                // Find the nearest wall direction
                Vector2Int nearestWallDir = FindNearestWallDirection(layout, x, y);

                // Calculate rotation to face away from the wall
                Quaternion rotation = Quaternion.identity;
                if (nearestWallDir != Vector2Int.zero)
                {
                    // Convert grid direction to world direction
                    Vector3 awayFromWall = new Vector3(-nearestWallDir.x, 0, -nearestWallDir.y);
                    Vector3 worldDirection = roomParent.transform.TransformDirection(awayFromWall);
                    rotation = Quaternion.LookRotation(worldDirection);
                }

                Instantiate(lootChestPrefab, worldPos, rotation, roomParent.transform);
                spawned++;
            }

            tries++;
        }
    }

    // Add this helper method to check if a position is near a door
    private bool IsNearDoorPosition(int x, int y)
    {
        // Check against all door positions
        foreach (var doorPos in doorPositions)
        {
            // Calculate distance to door position
            int distX = Mathf.Abs(x - doorPos.x);
            int distY = Mathf.Abs(y - doorPos.y);

            // If within 2 tiles of a door position, it's too close
            if (distX <= 2 && distY <= 2)
            {
                return true;
            }
        }

        return false;
    }


    // Add this helper method to count adjacent walls
    private int CountAdjacentWalls(int[][] layout, int x, int y)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        int wallCount = 0;

        foreach (var dir in directions)
        {
            int checkX = x + dir.x;
            int checkY = y + dir.y;

            // Count as a wall if it's out of bounds or empty space
            if (checkX < 0 || checkY < 0 || checkX >= layout.Length || checkY >= layout[0].Length || layout[checkX][checkY] == 0)
            {
                wallCount++;
            }
        }

        return wallCount;
    }

    // Keep the existing FindNearestWallDirection method
    private Vector2Int FindNearestWallDirection(int[][] layout, int x, int y)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        int minDistance = int.MaxValue;
        Vector2Int nearestWallDir = Vector2Int.zero;

        foreach (var dir in directions)
        {
            int distance = 0;
            int checkX = x;
            int checkY = y;

            // Keep checking in this direction until we hit a wall or edge
            while (true)
            {
                checkX += dir.x;
                checkY += dir.y;
                distance++;

                // Check if we've hit the edge of the layout or an empty space (wall)
                if (checkX < 0 || checkY < 0 || checkX >= layout.Length || checkY >= layout[0].Length || layout[checkX][checkY] == 0)
                {
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestWallDir = dir;
                    }
                    break;
                }

                // Safety check to prevent infinite loops
                if (distance > layout.Length + layout[0].Length)
                {
                    break;
                }
            }
        }

        return nearestWallDir;
    }

    private IEnumerator SpawnDoors(int[][] layout, Vector2Int anchor)
    {
        doorPositions.Clear(); // Clear previous door positions

        List<GameObject> doors = new();
        List<(int x, int y, Vector2Int dir)> candidates = new();
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        for (int x = 0; x < layout.Length; x++)
        {
            for (int y = 0; y < layout[0].Length; y++)
            {
                foreach (var dir in directions)
                {
                    Vector2Int perp = new Vector2Int(-dir.y, dir.x);
                    int x1 = x;
                    int y1 = y;
                    int x2 = x + perp.x;
                    int y2 = y + perp.y;

                    if (x2 < 0 || y2 < 0 || x2 >= layout.Length || y2 >= layout[0].Length) continue;
                    if (layout[x1][y1] != 1 || layout[x2][y2] != 1) continue;

                    int fx1 = x1 + dir.x;
                    int fy1 = y1 + dir.y;
                    int fx2 = x2 + dir.x;
                    int fy2 = y2 + dir.y;

                    bool frontClear =
                        (fx1 < 0 || fy1 < 0 || fx1 >= layout.Length || fy1 >= layout[0].Length || layout[fx1][fy1] == 0) &&
                        (fx2 < 0 || fy2 < 0 || fx2 >= layout.Length || fy2 >= layout[0].Length || layout[fx2][fy2] == 0);

                    if (!frontClear) continue;

                    int bx1 = x1 - dir.x;
                    int by1 = y1 - dir.y;
                    int bx2 = x2 - dir.x;
                    int by2 = y2 - dir.y;

                    bool backFloor =
                        bx1 >= 0 && by1 >= 0 && bx1 < layout.Length && by1 < layout[0].Length &&
                        bx2 >= 0 && by2 >= 0 && bx2 < layout.Length && by2 < layout[0].Length &&
                        layout[bx1][by1] == 1 && layout[bx2][by2] == 1;

                    if (!backFloor) continue;

                    candidates.Add((x1, y1, dir));
                }
            }
        }

        Debug.Log("FIXED door candidates: " + candidates.Count);
        yield return null;

        System.Random rng = new();
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int swap = rng.Next(i + 1);
            (candidates[i], candidates[swap]) = (candidates[swap], candidates[i]);
        }

        int doorCount = Mathf.Min(Random.Range(2, 4), candidates.Count);
        int placed = 0;

        foreach (var (x, y, dir) in candidates)
        {
            if (placed >= doorCount) break;

            Vector2Int perp = new Vector2Int(-dir.y, dir.x);
            Vector3 door1 = GetLocalOffset(x, y, anchor);
            Vector3 door2 = GetLocalOffset(x + perp.x, y + perp.y, anchor);
            Vector3 centerLocal = (door1 + door2) / 2f;

            Vector3 outward = new Vector3(dir.x, 0, dir.y) * (TILE_SIZE / 2f);

            Vector3 localDoorPos = centerLocal + outward;
            Vector3 worldDoorPos = roomParent.transform.TransformPoint(localDoorPos);
            Quaternion rot = Quaternion.LookRotation(roomParent.transform.TransformDirection(new Vector3(dir.x, 0, dir.y)));

            // Pass layout to check if hallway can be built
            if (CanBuildHallway(localDoorPos, dir, layout, anchor))
            {
                GameObject doorInstance = Instantiate(doorPrefab, worldDoorPos, rot, roomParent.transform);
                doors.Add(doorInstance);

                // Store door positions for chest placement
                doorPositions.Add(new Vector2Int(x, y));
                doorPositions.Add(new Vector2Int(x + perp.x, y + perp.y));

                // Also store positions behind the door (inside the room)
                doorPositions.Add(new Vector2Int(x - dir.x, y - dir.y));
                doorPositions.Add(new Vector2Int(x + perp.x - dir.x, y + perp.y - dir.y));

                BuildHallway(localDoorPos, dir, layout, anchor, doorInstance);
                placed++;
                doorsPlaced++;
                yield return null;
            }
        }
        GameManager.Instance.roomDoors[roomParent] = doors;
    }

    private int GetHallwayLength(int startX, int startY, Vector2Int dir, int[][] layout)
    {
        int minLength = 4;
        int maxPossibleLength = 0;

        // Calculate distance to edge of layout in the given direction
        if (dir.x > 0) // Right
            maxPossibleLength = layout.Length - startX - 1;
        else if (dir.x < 0) // Left
            maxPossibleLength = startX;
        else if (dir.y > 0) // Up
            maxPossibleLength = layout[0].Length - startY - 1;
        else if (dir.y < 0) // Down
            maxPossibleLength = startY;

        return Mathf.Max(minLength, maxPossibleLength);
    }

    private bool CanBuildHallway(Vector3 doorLocalPos, Vector2Int dir, int[][] layout, Vector2Int anchor)
    {
        // First, determine the hallway length
        Vector3 doorGridPos = doorLocalPos / TILE_SIZE + new Vector3(anchor.x, 0, anchor.y);
        int startX = Mathf.RoundToInt(doorGridPos.x + dir.x);
        int startY = Mathf.RoundToInt(doorGridPos.z + dir.y);

        int hallwayLength = GetHallwayLength(startX, startY, dir, layout);

        for (int d = 1; d <= hallwayLength; d++)
        {
            for (int dx = 0; dx < 2; dx++)
            {
                // Calculate grid position for this hallway tile
                int gridX = startX + dir.x * (d - 1);
                int gridY = startY + dir.y * (d - 1);

                if (dir.x == 0) // Moving in Y direction
                    gridX += dx;
                else // Moving in X direction
                    gridY += dx;

                // Check if this position is within bounds
                if (gridX >= 0 && gridY >= 0 && gridX < layout.Length && gridY < layout[0].Length)
                {
                    // CRITICAL: Make sure we're not building inside the room
                    if (layout[gridX][gridY] == 1)
                    {
                        return false; // This would be inside the room!
                    }
                }

                // Also check world collision
                Vector3 offset = new Vector3(
                    dir.x == 0 ? dx * TILE_SIZE : 0,
                    0,
                    dir.y == 0 ? dx * TILE_SIZE : 0
                );

                Vector3 stepDir = new Vector3(dir.x, 0, dir.y) * TILE_SIZE * d;
                Vector3 local = doorLocalPos + stepDir + offset - new Vector3(TILE_SIZE, 0, TILE_SIZE) / 2f;
                Vector3 world = roomParent.transform.TransformPoint(local);

                if (IsHallwaySectionBlocked(world))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private bool IsHallwaySectionBlocked(Vector3 worldPosition)
    {
        float checkSize = TILE_SIZE * 0.9f;
        Vector3 halfExtents = new Vector3(checkSize, TILE_SIZE * 1.5f, checkSize);
        Vector3 checkCenter = worldPosition + Vector3.up * (TILE_SIZE * 1.5f);

        return Physics.CheckBox(checkCenter, halfExtents, Quaternion.identity, ~0);
    }

    private void BuildHallway(Vector3 doorLocalPos, Vector2Int dir, int[][] layout, Vector2Int anchor, GameObject firstDoor)
    {
        // Calculate hallway length
        Vector3 doorGridPos = doorLocalPos / TILE_SIZE + new Vector3(anchor.x, 0, anchor.y);
        int startX = Mathf.RoundToInt(doorGridPos.x + dir.x);
        int startY = Mathf.RoundToInt(doorGridPos.z + dir.y);

        int hallwayLength = GetHallwayLength(startX, startY, dir, layout);

        // First, place floor tiles under the door itself
        for (int dx = 0; dx < 2; dx++)
        {
            Vector3 offset = new Vector3(
                dir.x == 0 ? dx * TILE_SIZE : 0,
                0,
                dir.y == 0 ? dx * TILE_SIZE : 0
            );

            Vector3 doorFloorLocal = doorLocalPos + offset - new Vector3(TILE_SIZE, 0, TILE_SIZE) / 2f;
            Vector3 doorFloorWorld = roomParent.transform.TransformPoint(doorFloorLocal);

            // Always place floor under the door position
            if (!IsPositionBlocked(doorFloorWorld))
            {
                Instantiate(hallwayFloorPrefab, doorFloorWorld, Quaternion.identity, roomParent.transform);

                // Build walls on the sides of the hallway
                if ((dx == 1 && (dir.x >= 0.01 || dir.y >= 0.01)) || (dx == 0 && (dir.x <= -0.01 || dir.y <= -0.01)))
                {
                    BuildWallSegement(doorFloorLocal + new Vector3(dir.y, 0, dir.x) * TILE_SIZE);
                }
                else
                {
                    BuildWallSegement(doorFloorLocal + new Vector3(dir.y * -1, 0, dir.x * -1) * TILE_SIZE);
                }
            }
        }

        RoomGenerateTrigger trigger = null;
        // Then continue with the rest of the hallway
        for (int d = 1; d <= hallwayLength; d++)
        {
            for (int dx = 0; dx < 2; dx++)
            {
                // Calculate grid position
                int gridX = startX + dir.x * (d - 1);
                int gridY = startY + dir.y * (d - 1);

                if (dir.x == 0) // Moving in Y direction
                    gridX += dx;
                else // Moving in X direction
                    gridY += dx;

                // Only build if outside the room (or at edge of layout)
                bool isOutsideRoom = gridX < 0 || gridY < 0 ||
                                    gridX >= layout.Length || gridY >= layout[0].Length ||
                                    layout[gridX][gridY] == 0;

                if (!isOutsideRoom)
                {
                    continue; // Skip this tile, it's inside the room
                }

                Vector3 offset = new Vector3(
                    dir.x == 0 ? dx * TILE_SIZE : 0,
                    0,
                    dir.y == 0 ? dx * TILE_SIZE : 0
                );

                Vector3 stepDir = new Vector3(dir.x, 0, dir.y) * TILE_SIZE * d;
                Vector3 local = doorLocalPos + stepDir + offset - new Vector3(TILE_SIZE, 0, TILE_SIZE) / 2f;
                Vector3 world = roomParent.transform.TransformPoint(local);

                if (!IsPositionBlocked(world))
                {
                    Instantiate(hallwayFloorPrefab, world, Quaternion.identity, roomParent.transform);

                    // Build walls on the sides of the hallway
                    if ((dx == 1 && (dir.x >= 0.01 || dir.y >= 0.01)) || (dx == 0 && (dir.x <= -0.01 || dir.y <= -0.01)))
                    {
                        BuildWallSegement(local + new Vector3(dir.y, 0, dir.x) * TILE_SIZE);
                    }
                    else
                    {
                        BuildWallSegement(local + new Vector3(dir.y * -1, 0, dir.x * -1) * TILE_SIZE);
                    }

                    if (d == hallwayLength && dx == 0)
                    {
                        Vector3 nextRoomPosition = roomParent.transform.TransformPoint(local + new Vector3(dir.x, 0, dir.y) * TILE_SIZE);
                        Quaternion nextRoomRotation = Quaternion.LookRotation(roomParent.transform.TransformDirection(new Vector3(dir.x, 0, dir.y)));
                        GameObject nextRoomSpawnObject = new GameObject("nextRoomSpawnPoint");
                        nextRoomSpawnObject.transform.position = nextRoomPosition;
                        nextRoomSpawnObject.transform.rotation = nextRoomRotation;
                        nextRoomSpawnObject.transform.SetParent(roomParent.transform, true);

                        Vector3 hallwayStartWorldPos = roomParent.transform.TransformPoint(doorLocalPos);
                        Vector3 triggerPos = Vector3.Lerp(hallwayStartWorldPos, roomParent.transform.TransformPoint(doorLocalPos + stepDir), 0.6f);
                        GameObject triggerObject = Instantiate(hallwayTriggerPrefab, triggerPos, nextRoomRotation, roomParent.transform);
                        triggerObject.name = "Hallway_Trigger";

                        // 3. Link the trigger to the new spawn point.
                        trigger = triggerObject.GetComponent<RoomGenerateTrigger>();
                        if (trigger != null)
                        {
                            trigger.roomSpawnPoint = nextRoomSpawnObject.transform;
                            trigger.closeDoor = firstDoor;
                        }
                        else
                        {
                            Debug.LogError("hallwayTriggerPrefab is missing the HallwayTrigger script!", hallwayTriggerPrefab);
                        }
                    }
                    else if (d == hallwayLength && dx == 1)
                    {
                        Quaternion rot = Quaternion.LookRotation(roomParent.transform.TransformDirection(new Vector3(dir.x, 0, dir.y)));
                        GameObject door = Instantiate(doorPrefab, roomParent.transform.TransformPoint(doorLocalPos + new Vector3(dir.x, 0, dir.y) * TILE_SIZE * (d + 0.95f)), rot, roomParent.transform);
                        trigger.door = door;
                    }
                }
            }
        }
    }

    private IEnumerator BuildWallsAround(int[][] layout, Vector2Int anchor)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        float startTime = Time.realtimeSinceStartup;
        int wallsProcessed = 0;

        for (int x = 0; x < layout.Length; x++)
        {
            for (int y = 0; y < layout[0].Length; y++)
            {
                if (layout[x][y] != 1) continue;

                foreach (var dir in directions)
                {
                    int nx = x + dir.x;
                    int ny = y + dir.y;
                    bool needsWall = nx < 0 || ny < 0 || nx >= layout.Length || ny >= layout[0].Length || layout[nx][ny] == 0;
                    if (!needsWall) continue;

                    // Build wall segment as coroutine
                    BuildWallSegement(anchor, nx, ny);

                    wallsProcessed++;

                    // Yield based on walls processed or time elapsed
                    if (wallsProcessed >= tilesPerFrame ||
                        (Time.realtimeSinceStartup - startTime) > maxTimePerFrame)
                    {
                        yield return null; // Wait for next frame
                        startTime = Time.realtimeSinceStartup;
                        wallsProcessed = 0;
                    }
                }
            }
        }
    }

    private void BuildWallSegement(Vector2Int anchor, int nx, int ny)
    {
        for (int h = 0; h < 3; h++)
        {
            Vector3 baseLocal = GetLocalOffset(nx, ny, anchor) + Vector3.up * h * TILE_SIZE;
            Vector3 wallWorld = roomParent.transform.TransformPoint(baseLocal);
            if (IsPositionBlocked(wallWorld)) break;

            Instantiate(wallPrefab, wallWorld, Quaternion.identity, roomParent.transform);
        }
    }

    private void BuildWallSegement(Vector3 localPosition)
    {
        for (int h = 0; h < 3; h++)
        {
            Vector3 baseLocal = localPosition + Vector3.up * h * TILE_SIZE;
            Vector3 wallWorld = roomParent.transform.TransformPoint(baseLocal);
            if (IsPositionBlocked(wallWorld)) break;

            Instantiate(wallPrefab, wallWorld, Quaternion.identity, roomParent.transform);
        }
    }

    private Vector3 GetLocalOffset(int x, int y, Vector2Int anchor)
    {
        return new Vector3((x - anchor.x) * TILE_SIZE, 0, (y - anchor.y) * TILE_SIZE);
    }

    private bool IsPositionBlocked(Vector3 worldPosition)
    {
        float checkSize = TILE_SIZE * 0.45f;
        Vector3 halfExtents = new Vector3(checkSize, checkSize, checkSize);
        return Physics.CheckBox(worldPosition, halfExtents, Quaternion.identity, ~0);
    }

    [System.Serializable]
    public class EnemyData
    {
        public GameObject prefab;
        public int tileWidth = 1;
        public int tileHeight = 1;
    }
}