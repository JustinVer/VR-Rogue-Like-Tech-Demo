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

    public List<EnemyData> enemyTypes;
    public NavMeshSurface[] navMeshSurface;
    public Vector3[] spawnPositions;
    public Quaternion[] spawnRotations;

    private const int TILE_SIZE = 2;
    private GameObject roomParent;

    private void Start()
    {
        StartCoroutine(GenerateRoomsOverTime());
    }

    private IEnumerator GenerateRoomsOverTime()
    {
        for (int i = 0; i < spawnPositions.Length && i < spawnRotations.Length; i++)
        {
            GenerateARoom(spawnPositions[i], spawnRotations[i]);
            yield return null;
        }
    }

    public void GenerateARoom(Vector3 position, Quaternion rotation)
    {
        roomParent = new GameObject("GeneratedRoom");
        roomParent.transform.position = position;
        roomParent.transform.rotation = rotation;

        int roomSize = Random.Range(minRoomSize, maxRoomSize);
        (int[][] layout, Vector2Int anchor) = GetNewFloor(roomSize);

        // STEP 1: Floor
        GenerateFloor(layout, anchor);

        // STEP 2: Doors & Hallways before any walls or obstacles exist
        SpawnDoors(layout, anchor);

        // STEP 5: Walls AFTER all physics-based things are placed
        BuildWallsAround(layout, anchor);

        // STEP 4: Loot
        SpawnLootChests(layout, anchor);

        // STEP 6: Bake navmesh
        StartCoroutine(BuildNavMesh());

        // STEP 3: Enemies
        SpawnEnemies(layout, roomSize, anchor);

    }

    private IEnumerator BuildNavMesh()
    {
        yield return null;
        if (navMeshSurface != null)
        {
            foreach (NavMeshSurface navMesh in navMeshSurface)
                navMesh.BuildNavMesh();
        }
    }

    private (int[][], Vector2Int) GetNewFloor(int roomSize)
    {
        int size = (int)(Mathf.Sqrt(roomSize) + 5);
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

    private void GenerateFloor(int[][] layout, Vector2Int anchor)
    {
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
                }
            }
        }
    }

    private void SpawnEnemies(int[][] layout, int roomSize, Vector2Int anchor)
    {
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
                Vector3 worldPos = roomParent.transform.TransformPoint(localPos);
                if (!IsPositionBlocked(worldPos + new Vector3(0, 2, 0)))
                {
                    Instantiate(enemy.prefab, worldPos, Quaternion.identity, roomParent.transform);
                    spawned += enemy.tileWidth * enemy.tileHeight;
                }
            }

            tries++;
        }
    }

    private void SpawnLootChests(int[][] layout, Vector2Int anchor)
    {
        int count = Random.Range(1, 5);
        int spawned = 0;
        int tries = 0;

        while (spawned < count && tries < 100)
        {
            int x = Random.Range(0, layout.Length);
            int y = Random.Range(0, layout[0].Length);

            if (layout[x][y] != 1)
            {
                tries++;
                continue;
            }

            Vector3 localPos = GetLocalOffset(x, y, anchor);
            Vector3 worldPos = roomParent.transform.TransformPoint(localPos);

            if (!IsPositionBlocked(worldPos + new Vector3(0, 2, 0)))
            {
                Instantiate(lootChestPrefab, worldPos, Quaternion.identity, roomParent.transform);
                spawned++;
            }

            tries++;
        }
    }

    private void SpawnDoors(int[][] layout, Vector2Int anchor)
    {
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

        System.Random rng = new();
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int swap = rng.Next(i + 1);
            (candidates[i], candidates[swap]) = (candidates[swap], candidates[i]);
        }

        int doorCount = Mathf.Min(Random.Range(2, 5), candidates.Count);
        int placed = 0;

        foreach (var (x, y, dir) in candidates)
        {
            if (placed >= doorCount) break;

            Vector2Int perp = new Vector2Int(-dir.y, dir.x);
            Vector3 door1 = GetLocalOffset(x, y, anchor);
            Vector3 door2 = GetLocalOffset(x + perp.x, y + perp.y, anchor);
            Vector3 centerLocal = (door1 + door2) / 2f;

            // FIXED: Place door fully outside the wall
            Vector3 outward = new Vector3(dir.x, 0, dir.y) * (TILE_SIZE / 2f); // Changed from TILE_SIZE/2f

            Vector3 localDoorPos = centerLocal + outward;
            Vector3 worldDoorPos = roomParent.transform.TransformPoint(localDoorPos);
            Quaternion rot = Quaternion.LookRotation(roomParent.transform.TransformDirection(new Vector3(dir.x, 0, dir.y)));

            // FIXED: Check if hallway can be built before placing door
            if (CanBuildHallway(localDoorPos, dir))
            {
                Instantiate(doorPrefab, worldDoorPos, rot, roomParent.transform);
                BuildHallway(localDoorPos, dir);
                placed++;
            }
        }
    }

    private bool CanBuildHallway(Vector3 doorLocalPos, Vector2Int dir)
    {
        for (int d = 1; d <= 4; d++)
        {
            for (int dx = 0; dx < 2; dx++)
            {
                Vector3 offset = new Vector3(
                    dir.x == 0 ? dx * TILE_SIZE : 0,
                    0,
                    dir.y == 0 ? dx * TILE_SIZE : 0
                );

                Vector3 stepDir = new Vector3(dir.x, 0, dir.y) * TILE_SIZE * d;
                Vector3 local = doorLocalPos + stepDir + offset - new Vector3(TILE_SIZE, 0, TILE_SIZE) / 2f;
                Vector3 world = roomParent.transform.TransformPoint(local);

                // Check full volume of hallway section (floor + height)
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
        // Check a larger volume that encompasses the floor and potential walls
        float checkSize = TILE_SIZE * 0.9f; // Increased from 0.45f
        Vector3 halfExtents = new Vector3(checkSize, TILE_SIZE * 1.5f, checkSize); // Check vertically too
        Vector3 checkCenter = worldPosition + Vector3.up * (TILE_SIZE * 1.5f); // Center of the check volume

        return Physics.CheckBox(checkCenter, halfExtents, Quaternion.identity, ~0);
    }

    private bool IsPositionBlocked(Vector3 worldPosition, float checkScale = 0.45f)
    {
        float checkSize = TILE_SIZE * checkScale;
        Vector3 halfExtents = new Vector3(checkSize, checkSize, checkSize);
        return Physics.CheckBox(worldPosition, halfExtents, Quaternion.identity, ~0);
    }

    private void BuildHallway(Vector3 doorLocalPos, Vector2Int dir)
    {
        for (int d = 1; d <= 4; d++)
        {
            for (int dx = 0; dx < 2; dx++)
            {
                Vector3 offset = new Vector3(
                    dir.x == 0 ? dx * TILE_SIZE : 0,
                    0,
                    dir.y == 0 ? dx * TILE_SIZE : 0
                );

                Vector3 stepDir = new Vector3(dir.x, 0, dir.y) * TILE_SIZE * d;
                Vector3 local = doorLocalPos + stepDir + offset - new Vector3(TILE_SIZE, 0, TILE_SIZE) / 2f;
                Vector3 world = roomParent.transform.TransformPoint(local);

                // Only place if not blocked (redundant check but safe)
                if (!IsPositionBlocked(world))
                {
                    Instantiate(hallwayFloorPrefab, world, Quaternion.identity, roomParent.transform);

                    if ((dx == 1 && (dir.x >= 0.01 || dir.y >= 0.01)) || (dx == 0 && (dir.x <= -0.01 || dir.y <= -0.01)))
                    {
                        BuildWallSegement(local + new Vector3(dir.y, 0, dir.x) * TILE_SIZE);
                    }
                    else
                    {
                        BuildWallSegement(local + new Vector3(dir.y * -1, 0, dir.x * -1) * TILE_SIZE);
                    }
                }
            }
        }
    }

    private void BuildWallsAround(int[][] layout, Vector2Int anchor)
    {
        Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

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
                    BuildWallSegement(anchor, nx, ny);
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