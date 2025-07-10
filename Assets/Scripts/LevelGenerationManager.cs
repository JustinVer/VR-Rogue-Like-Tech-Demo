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
            yield return null; // Wait one frame between rooms
        }
    }

    public void GenerateARoom(Vector3 position, Quaternion rotation)
    {
        roomParent = new GameObject("GeneratedRoom");
        roomParent.transform.position = position;
        roomParent.transform.rotation = rotation;

        int roomSize = Random.Range(minRoomSize, maxRoomSize);
        (int[][] layout, Vector2Int anchor) = GetNewFloor(roomSize);
        GenerateFloor(layout, position, anchor);
        StartCoroutine(BuildNavMeshAndSpawn(layout, roomSize, position, anchor));
    }

    private IEnumerator BuildNavMeshAndSpawn(int[][] layout, int roomSize, Vector3 origin, Vector2Int anchor)
    {
        yield return null;
        if (navMeshSurface != null)
        {
            foreach (NavMeshSurface navMesh in navMeshSurface)
            {
                navMesh.BuildNavMesh();
            }
        }
        SpawnEnemies(layout, roomSize, origin, anchor);
    }

    private (int[][], Vector2Int) GetNewFloor(int roomSize)
    {
        int squareLength = (int)(Mathf.Sqrt(roomSize) + 5);
        int[][] layout = new int[squareLength][];
        for (int i = 0; i < squareLength; i++)
        {
            layout[i] = new int[squareLength];
        }

        // Anchor is bottom-center (where hallway meets room)
        Vector2Int anchor = new Vector2Int(squareLength / 2 - 1, 0);

        // Reserve 2x2 entrance area at anchor
        layout[anchor.x][anchor.y] = 1;
        layout[anchor.x + 1][anchor.y] = 1;
        layout[anchor.x][anchor.y + 1] = 1;
        layout[anchor.x + 1][anchor.y + 1] = 1;

        int tilesPlaced = 4;
        Queue<Vector2Int> toCheck = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        toCheck.Enqueue(anchor);
        visited.Add(anchor);

        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

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

                if (neighbor.x < 0 || neighbor.y < 0 || neighbor.x >= squareLength || neighbor.y >= squareLength)
                    continue;

                if (visited.Contains(neighbor)) continue;

                int neighborConnections = 0;
                foreach (var d in directions)
                {
                    Vector2Int check = neighbor + d;
                    if (check.x >= 0 && check.y >= 0 && check.x < squareLength && check.y < squareLength)
                    {
                        if (layout[check.x][check.y] == 1) neighborConnections++;
                    }
                }

                float connectionBias = 0.3f + 0.3f * neighborConnections;
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

            if (toCheck.Count == 0) toCheck.Enqueue(anchor); // restart if stuck
        }

        return (layout, anchor);
    }

    private void GenerateFloor(int[][] layout, Vector3 origin, Vector2Int anchor)
    {
        int rows = layout.Length;
        int cols = layout[0].Length;

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                if (layout[x][y] == 1)
                {
                    Vector3 spawnPos = origin + GetRotatedOffset(roomParent.transform, x, y, anchor);
                    if (IsPositionBlocked(spawnPos)) continue;

                    GameObject prefab = (x + y) % 2 == 0 ? floorPrefab1 : floorPrefab2;
                    Instantiate(prefab, spawnPos, Quaternion.identity, roomParent.transform);
                }
            }
        }

        BuildWallsAround(layout, origin, anchor);
    }

    private void BuildWallsAround(int[][] layout, Vector3 origin, Vector2Int anchor)
    {
        int rows = layout.Length;
        int cols = layout[0].Length;
        Vector2Int[] directions = new Vector2Int[]
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
        };

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                if (layout[x][y] != 1) continue;

                foreach (var dir in directions)
                {
                    int nx = x + dir.x;
                    int ny = y + dir.y;
                    bool needsWall = nx < 0 || ny < 0 || nx >= rows || ny >= cols || layout[nx][ny] == 0;

                    if (needsWall)
                    {
                        for (int h = 0; h < 3; h++)
                        {
                            Vector3 wallBase = origin + GetRotatedOffset(roomParent.transform, x + dir.x, y + dir.y, anchor);
                            Vector3 wallPos = wallBase + roomParent.transform.up * (h * TILE_SIZE);
                            if (IsPositionBlocked(wallPos)) break;

                            Instantiate(wallPrefab, wallPos, Quaternion.identity, roomParent.transform);
                        }
                    }
                }
            }
        }
    }

    private void SpawnEnemies(int[][] layout, int roomSize, Vector3 origin, Vector2Int anchor)
    {
        int rows = layout.Length;
        int cols = layout[0].Length;
        int maxEnemies = (int)(roomSize * (((Random.value * (1 - minEnemyPercentage)) * (maxEnemyPercentage - minEnemyPercentage)) + minEnemyPercentage));
        int tries = 0;
        int spawned = 0;
        System.Random rng = new System.Random();

        while (spawned < maxEnemies && tries < maxEnemies * 10)
        {
            EnemyData enemy = enemyTypes[rng.Next(enemyTypes.Count)];
            int x = rng.Next(rows - enemy.tileWidth);
            int y = rng.Next(cols - enemy.tileHeight);
            bool canPlace = true;

            for (int i = 0; i < enemy.tileWidth; i++)
            {
                for (int j = 0; j < enemy.tileHeight; j++)
                {
                    if (layout[x + i][y + j] != 1)
                    {
                        canPlace = false;
                        break;
                    }
                }
                if (!canPlace) break;
            }

            if (canPlace)
            {
                Vector3 spawnOffset = GetRotatedOffset(roomParent.transform, x + enemy.tileWidth / 2, y + enemy.tileHeight / 2, anchor);
                Vector3 spawnPos = origin + spawnOffset;

                Instantiate(enemy.prefab, spawnPos, Quaternion.identity, roomParent.transform);
                spawned += (enemy.tileWidth * enemy.tileHeight);
            }

            tries++;
        }
    }

    private Vector3 GetRotatedOffset(Transform roomTransform, int x, int y, Vector2Int anchor)
    {
        Vector3 anchorOffset =
            roomTransform.right * (anchor.x * TILE_SIZE) +
            roomTransform.forward * (anchor.y * TILE_SIZE);

        return roomTransform.right * (x * TILE_SIZE) +
               roomTransform.forward * (y * TILE_SIZE) -
               anchorOffset;
    }

    // Checks if the given position is blocked by anything in the physics world
    private bool IsPositionBlocked(Vector3 position)
    {
        float checkSize = TILE_SIZE * 0.45f;
        Vector3 halfExtents = new Vector3(checkSize, checkSize, checkSize);
        return Physics.CheckBox(position, halfExtents, Quaternion.identity, ~0); // ~0 = all layers
    }

    [System.Serializable]
    public class EnemyData
    {
        public GameObject prefab;
        public int tileWidth = 1;
        public int tileHeight = 1;
    }
}
