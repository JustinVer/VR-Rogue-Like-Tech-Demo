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

    private GameObject roomParent;
    private const int TILE_SIZE = 2;

    private void Start()
    {
        GenerateARoom(new Vector3(0, 50, 5), this.transform.rotation);
        GenerateARoom(new Vector3(30, 50, 50), this.transform.rotation);
        GenerateARoom(new Vector3(60, 50, 50), this.transform.rotation);
    }
    public void GenerateARoom(Vector3 position, Quaternion rotation)
    {
        roomParent = new GameObject("GeneratedRoom");
        roomParent.transform.position = position;
        roomParent.transform.rotation = rotation;

        int roomSize = Random.Range(minRoomSize, maxRoomSize);
        int[][] floorLayout = getNewFloor(roomSize);
        GenerateFloor(floorLayout, position);

        StartCoroutine(BuildNavMeshAndSpawn(floorLayout, roomSize, position));
    }

    private IEnumerator BuildNavMeshAndSpawn(int[][] layout, int roomSize, Vector3 origin)
    {
        yield return null;
        if (navMeshSurface != null)
        {
            foreach (NavMeshSurface navMesh in navMeshSurface)
            {
                navMesh.BuildNavMesh();
            }

        }
        SpawnEnemies(layout, roomSize, origin);
    }

    private int[][] getNewFloor(int roomSize)
    {
        int squareLength = (int)(Mathf.Sqrt(roomSize) + 5);
        Debug.Log("Square length " + squareLength + " " + roomSize);
        int[][] layout = new int[squareLength][];
        for (int i = 0; i < squareLength; i++)
        {
            layout[i] = new int[squareLength];
        }

        Vector2Int bottomCenter = new Vector2Int(squareLength / 2, 0);

        // Reserve 2x2 starting area at bottom center
        layout[bottomCenter.x][bottomCenter.y] = 1;
        layout[bottomCenter.x + 1][bottomCenter.y] = 1;
        layout[bottomCenter.x][bottomCenter.y + 1] = 1;
        layout[bottomCenter.x + 1][bottomCenter.y + 1] = 1;

        int tilesPlaced = 4;
        Queue<Vector2Int> toCheck = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        while (tilesPlaced < roomSize)
        {
            toCheck.Enqueue(bottomCenter);
            visited.Add(bottomCenter);

            Vector2Int[] directions = new Vector2Int[]
            {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
            };

            while (tilesPlaced < roomSize && toCheck.Count > 0)
            {
                Vector2Int current = toCheck.Dequeue();

                int offset = Random.Range(0, directions.Length);
                for (int i = 0; i < directions.Length; i++)
                {
                    Vector2Int neighbor = current + (directions[(i + offset) % 4]);
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
            }
        }

        return layout;
    }

    private void GenerateFloor(int[][] layout, Vector3 origin)
    {
        int rows = layout.Length;
        int cols = layout[0].Length;

        for (int x = 0; x < rows; x++)
        {
            for (int y = 0; y < cols; y++)
            {
                if (layout[x][y] == 1)
                {
                    Vector3 spawnPos = origin + new Vector3(x * TILE_SIZE, 0, y * TILE_SIZE);
                    if ((x + y) % 2 == 0)
                    {
                        Instantiate(floorPrefab1, spawnPos, Quaternion.identity, roomParent.transform);
                    }
                    else
                    {
                        Instantiate(floorPrefab2, spawnPos, Quaternion.identity, roomParent.transform);
                    }
                }
            }
        }

        BuildWallsAround(layout, origin);
    }

    private void BuildWallsAround(int[][] layout, Vector3 origin)
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
                    if (nx >= 0 && ny >= 0 && nx < rows && ny < cols)
                    {
                        if (layout[nx][ny] == 0)
                        {
                            for (int h = 0; h < 3; h++)
                            {
                                Vector3 wallPos = origin + new Vector3((x + dir.x) * TILE_SIZE, h * TILE_SIZE, (y + dir.y) * TILE_SIZE);
                                Instantiate(wallPrefab, wallPos, Quaternion.identity, roomParent.transform);
                            }
                        }
                    }
                    else
                    {
                        for (int h = 0; h < 3; h++)
                        {
                            Vector3 wallPos = origin + new Vector3((x + dir.x) * TILE_SIZE, h * TILE_SIZE, (y + dir.y) * TILE_SIZE);
                            Instantiate(wallPrefab, wallPos, Quaternion.identity, roomParent.transform);
                        }
                    }
                }
            }
        }
    }

    private void SpawnEnemies(int[][] layout, int roomSize, Vector3 origin)
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
                Vector3 spawnPos = origin + new Vector3((x + enemy.tileWidth / 2f) * TILE_SIZE, 0, (y + enemy.tileHeight / 2f) * TILE_SIZE);
                Instantiate(enemy.prefab, spawnPos, Quaternion.identity, roomParent.transform);
                spawned += (enemy.tileWidth * enemy.tileHeight);
            }

            tries++;
        }
    }
    [System.Serializable]
    public class EnemyData
    {
        public GameObject prefab;
        public int tileWidth = 1;
        public int tileHeight = 1;
    }
}
