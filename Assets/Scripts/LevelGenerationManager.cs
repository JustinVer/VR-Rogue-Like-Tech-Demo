using System.Collections;
using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class LevelGenerationManager : MonoBehaviour
{
    [Header("Room Generation Settings")]
    [SerializeField, Range(60, 300)] private int _minRoomSize = 60;
    [SerializeField, Range(60, 300)] private int _maxRoomSize = 300;
    [SerializeField, Range(0.01f, 0.3f)] private float _minEnemyPercentage = 0.01f;
    [SerializeField, Range(0.01f, 0.3f)] private float _maxEnemyPercentage = 0.22f;

    [Header("Prefabs")]
    [SerializeField] private GameObject _floorPrefab1;
    [SerializeField] private GameObject _floorPrefab2;
    [SerializeField] private GameObject _wallPrefab;
    [SerializeField] private GameObject _doorPrefab;
    [SerializeField] private GameObject _hallwayFloorPrefab;
    [SerializeField] private GameObject _lootChestPrefab;

    [Header("Enemy Configuration")]
    [SerializeField] private List<EnemyData> _enemyTypes;

    [Header("Navigation")]
    [SerializeField] private NavMeshSurface[] _navMeshSurfaces;

    [Header("Spawn Locations")]
    [SerializeField] private Vector3[] _spawnPositions;
    [SerializeField] private Quaternion[] _spawnRotations;

    // Constants
    private const int TILE_SIZE = 2;
    private const int MAX_PLACEMENT_ATTEMPTS = 100;
    private const int MAX_HALLWAY_LENGTH = 4;

    // Runtime references
    private GameObject _currentRoomParent;

    private void Start()
    {
        StartCoroutine(GenerateRoomsOverTime());
    }

    /// <summary>
    /// Generates rooms sequentially at specified spawn points with delays
    /// </summary>
    private IEnumerator GenerateRoomsOverTime()
    {
        for (int i = 0; i < _spawnPositions.Length && i < _spawnRotations.Length; i++)
        {
            GenerateRoom(_spawnPositions[i], _spawnRotations[i]);
            yield return null; // Brief pause between rooms
        }
    }

    /// <summary>
    /// Main room generation pipeline
    /// </summary>
    /// <param name="position">World position for room root</param>
    /// <param name="rotation">World rotation for room root</param>
    public void GenerateRoom(Vector3 position, Quaternion rotation)
    {
        // Create room container object
        _currentRoomParent = new GameObject("GeneratedRoom");
        _currentRoomParent.transform.position = position;
        _currentRoomParent.transform.rotation = rotation;

        // Determine room dimensions
        int roomSize = Random.Range(_minRoomSize, _maxRoomSize);

        // Generate floor layout (returns tilemap and anchor point)
        (int[][] layout, Vector2Int anchor) = GenerateFloorLayout(roomSize);

        // Generation sequence
        PlaceFloorTiles(layout, anchor);
        SpawnDoors(layout, anchor);
        BuildPerimeterWalls(layout, anchor);
        PlaceLootChests(layout, anchor);
        StartCoroutine(BakeNavMeshAsync());
        SpawnEnemies(layout, roomSize, anchor);
    }

    #region Floor Generation
    /// <summary>
    /// Creates procedural floor layout using BFS with connectivity constraints
    /// </summary>
    /// <param name="targetSize">Approximate number of floor tiles</param>
    /// <returns>Tile grid and anchor point (center reference)</returns>
    private (int[][], Vector2Int) GenerateFloorLayout(int targetSize)
    {
        int gridSize = Mathf.RoundToInt(Mathf.Sqrt(targetSize)) + 3;
        int[][] grid = new int[gridSize][];
        for (int i = 0; i < gridSize; i++)
            grid[i] = new int[gridSize];

        // Initial floor block (2x2)
        Vector2Int anchor = new Vector2Int(gridSize / 2 - 1, gridSize / 2 - 1);
        grid[anchor.x][anchor.y] = 1;
        grid[anchor.x + 1][anchor.y] = 1;
        grid[anchor.x][anchor.y + 1] = 1;
        grid[anchor.x + 1][anchor.y + 1] = 1;

        int placedTiles = 4;
        Queue<Vector2Int> expansionQueue = new Queue<Vector2Int>();
        expansionQueue.Enqueue(anchor);
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        visited.Add(anchor);

        Vector2Int[] directions =
        {
            Vector2Int.up,
            Vector2Int.right,
            Vector2Int.down,
            Vector2Int.left
        };

        int expansionAttempts = 0;
        while (placedTiles < targetSize && expansionAttempts++ < 1000)
        {
            if (expansionQueue.Count == 0)
                expansionQueue.Enqueue(visited.GetEnumerator().Current);

            Vector2Int currentPos = expansionQueue.Dequeue();
            int directionOffset = Random.Range(0, directions.Length);

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int dir = directions[(i + directionOffset) % directions.Length];
                Vector2Int neighbor = currentPos + dir;

                // Skip out-of-bounds or visited tiles
                if (!IsInGridBounds(grid, neighbor) || visited.Contains(neighbor))
                    continue;

                // Calculate neighbor connectivity
                int connectedNeighbors = CountConnectedNeighbors(grid, neighbor);
                float connectionProbability = CalculateConnectionBias(connectedNeighbors);

                // Attempt to place tile
                if (Random.value < connectionProbability)
                {
                    grid[neighbor.x][neighbor.y] = 1;
                    placedTiles++;
                    expansionQueue.Enqueue(neighbor);
                }
                visited.Add(neighbor);
            }
        }
        return (grid, anchor);
    }

    private bool IsInGridBounds(int[][] grid, Vector2Int position)
    {
        return position.x >= 0 &&
               position.y >= 0 &&
               position.x < grid.Length &&
               position.y < grid[0].Length;
    }

    private int CountConnectedNeighbors(int[][] grid, Vector2Int position)
    {
        int count = 0;
        Vector2Int[] dirs = Vector2IntHelper.GetCardinalDirections();
        foreach (Vector2Int dir in dirs)
        {
            Vector2Int neighbor = position + dir;
            if (IsInGridBounds(grid, neighbor) && grid[neighbor.x][neighbor.y] == 1)
                count++;
        }
        return count;
    }

    private float CalculateConnectionBias(int connectedNeighbors)
    {
        return 0.3f + (0.3f * connectedNeighbors);
    }
    #endregion

    #region Object Placement
    /// <summary>
    /// Instantiates floor tiles based on generated layout
    /// </summary>
    private void PlaceFloorTiles(int[][] layout, Vector2Int anchor)
    {
        for (int x = 0; x < layout.Length; x++)
        {
            for (int y = 0; y < layout[0].Length; y++)
            {
                if (layout[x][y] == 1)
                {
                    Vector3 localPosition = GetLocalPositionFromGrid(x, y, anchor);
                    GameObject floorPrefab = (x + y) % 2 == 0 ?
                        _floorPrefab1 : _floorPrefab2;
                    Instantiate(floorPrefab,
                               localPosition,
                               Quaternion.identity,
                               _currentRoomParent.transform);
                }
            }
        }
    }

    /// <summary>
    /// Places walls around floor tiles that border empty space
    /// </summary>
    private void BuildPerimeterWalls(int[][] layout, Vector2Int anchor)
    {
        Vector2Int[] directions = Vector2IntHelper.GetCardinalDirections();

        for (int x = 0; x < layout.Length; x++)
        {
            for (int y = 0; y < layout[0].Length; y++)
            {
                if (layout[x][y] != 1) continue;

                foreach (Vector2Int dir in directions)
                {
                    Vector2Int neighborPos = new Vector2Int(x + dir.x, y + dir.y);
                    bool shouldPlaceWall =
                        !IsInGridBounds(layout, neighborPos) ||
                        layout[neighborPos.x][neighborPos.y] == 0;

                    if (shouldPlaceWall)
                    {
                        Vector3 basePosition = GetLocalPositionFromGrid(neighborPos, anchor);
                        PlaceWallStack(basePosition);
                    }
                }
            }
        }
    }

    private void PlaceWallStack(Vector3 basePosition)
    {
        for (int height = 0; height < 3; height++)
        {
            Vector3 wallPos = basePosition + Vector3.up * (height * TILE_SIZE);
            if (!IsPositionOccupied(wallPos))
            {
                Instantiate(_wallPrefab,
                           wallPos,
                           Quaternion.identity,
                           _currentRoomParent.transform);
            }
        }
    }

    /// <summary>
    /// Places loot chests in valid floor positions
    /// </summary>
    private void PlaceLootChests(int[][] layout, Vector2Int anchor)
    {
        int chestCount = Random.Range(1, 5);
        int placedChests = 0;
        int placementAttempts = 0;

        while (placedChests < chestCount && placementAttempts++ < MAX_PLACEMENT_ATTEMPTS)
        {
            int randomX = Random.Range(0, layout.Length);
            int randomY = Random.Range(0, layout[0].Length);

            if (layout[randomX][randomY] != 1)
                continue;

            Vector3 chestPosition = GetLocalPositionFromGrid(randomX, randomY, anchor);
            if (!IsPositionOccupied(chestPosition + Vector3.up * 2))
            {
                Instantiate(_lootChestPrefab,
                           chestPosition,
                           Quaternion.identity,
                           _currentRoomParent.transform);
                placedChests++;
            }
        }
    }

    /// <summary>
    /// Spawns enemies with footprint validation
    /// </summary>
    private void SpawnEnemies(int[][] layout, int roomSize, Vector2Int anchor)
    {
        int maxEnemies = CalculateMaxEnemies(roomSize);
        int placedEnemies = 0;
        int placementAttempts = 0;
        System.Random random = new System.Random();

        while (placedEnemies < maxEnemies && placementAttempts++ < MAX_PLACEMENT_ATTEMPTS * 5)
        {
            // Select random enemy type
            EnemyData enemy = _enemyTypes[random.Next(_enemyTypes.Count)];

            // Find valid position for enemy footprint
            int x = random.Next(layout.Length - enemy.TileWidth);
            int y = random.Next(layout[0].Length - enemy.TileHeight);

            // Check footprint validity
            if (!IsFootprintValid(layout, x, y, enemy.TileWidth, enemy.TileHeight))
                continue;

            // Calculate spawn position (center of footprint)
            Vector3 spawnPos = GetLocalPositionFromGrid(
                x + enemy.TileWidth / 2,
                y + enemy.TileHeight / 2,
                anchor
            );

            // Check vertical clearance
            if (!IsPositionOccupied(spawnPos + Vector3.up * 2))
            {
                Instantiate(enemy.Prefab,
                           spawnPos,
                           Quaternion.identity,
                           _currentRoomParent.transform);
                placedEnemies += enemy.TileWidth * enemy.TileHeight;
            }
        }
    }

    private int CalculateMaxEnemies(int roomSize)
    {
        return Mathf.RoundToInt(roomSize * Random.Range(_minEnemyPercentage, _maxEnemyPercentage));
    }

    private bool IsFootprintValid(int[][] layout, int startX, int startY, int width, int height)
    {
        for (int x = startX; x < startX + width; x++)
        {
            for (int y = startY; y < startY + height; y++)
            {
                if (!IsInGridBounds(layout, new Vector2Int(x, y)) ||
                    layout[x][y] != 1)
                {
                    return false;
                }
            }
        }
        return true;
    }
    #endregion

    #region Door & Hallway System
    /// <summary>
    /// Identifies valid door positions and spawns doors and connecting hallways
    /// </summary>
    private void SpawnDoors(int[][] layout, Vector2Int anchor)
    {
        List<DoorCandidate> candidates = new List<DoorCandidate>();
        Vector2Int[] directions = Vector2IntHelper.GetCardinalDirections();

        // Identify potential door locations
        for (int x = 0; x < layout.Length; x++)
        {
            for (int y = 0; y < layout[0].Length; y++)
            {
                if (layout[x][y] != 1) continue;

                foreach (Vector2Int dir in directions)
                {
                    // Calculate perpendicular for double-door placement
                    Vector2Int perpendicular = Vector2IntHelper.GetPerpendicular(dir);
                    Vector2Int doorTile1 = new Vector2Int(x, y);
                    Vector2Int doorTile2 = new Vector2Int(x + perpendicular.x, y + perpendicular.y);

                    // Validate both tiles are floor
                    if (!IsInGridBounds(layout, doorTile2) || layout[doorTile2.x][doorTile2.y] != 1)
                        continue;

                    // Check front clearance (where hallway would start)
                    bool hasFrontClearance = CheckFrontClearance(layout, doorTile1, doorTile2, dir);
                    // Check room connection behind door
                    bool hasRoomBehind = CheckRoomConnection(layout, doorTile1, doorTile2, dir);

                    if (hasFrontClearance && hasRoomBehind)
                    {
                        candidates.Add(new DoorCandidate(doorTile1, dir));
                    }
                }
            }
        }

        // Shuffle candidates for random selection
        ShuffleList(candidates);
        int doorsToPlace = Mathf.Min(Random.Range(2, 4), candidates.Count);
        int placedDoors = 0;

        foreach (DoorCandidate candidate in candidates)
        {
            if (placedDoors >= doorsToPlace) break;

            Vector2Int perpendicular = Vector2IntHelper.GetPerpendicular(candidate.Direction);
            Vector2Int tile1 = candidate.TilePosition;
            Vector2Int tile2 = new Vector2Int(tile1.x + perpendicular.x, tile1.y + perpendicular.y);

            // Calculate door position (center between two tiles)
            Vector3 doorBasePos = GetLocalPositionFromGrid(tile1, anchor);
            Vector3 doorCenterLocal = (doorBasePos + GetLocalPositionFromGrid(tile2, anchor)) / 2f;

            // Orient door outward from room
            Vector3 doorForward = new Vector3(candidate.Direction.x, 0, candidate.Direction.y);
            Vector3 worldPos = _currentRoomParent.transform.TransformPoint(doorCenterLocal);
            worldPos += doorForward * (TILE_SIZE / 2f); // Offset to room exterior

            if (!IsPositionOccupied(worldPos + Vector3.up * 2))
            {
                // Create door instance
                Instantiate(_doorPrefab,
                           worldPos,
                           Quaternion.LookRotation(doorForward),
                           _currentRoomParent.transform);

                // Build connecting hallway
                BuildHallway(worldPos, doorForward);
                placedDoors++;
            }
        }
    }

    private bool CheckFrontClearance(int[][] layout, Vector2Int tile1, Vector2Int tile2, Vector2Int dir)
    {
        Vector2Int frontTile1 = tile1 + dir;
        Vector2Int frontTile2 = tile2 + dir;

        bool tile1Clear = !IsInGridBounds(layout, frontTile1) || layout[frontTile1.x][frontTile1.y] == 0;
        bool tile2Clear = !IsInGridBounds(layout, frontTile2) || layout[frontTile2.x][frontTile2.y] == 0;

        return tile1Clear && tile2Clear;
    }

    private bool CheckRoomConnection(int[][] layout, Vector2Int tile1, Vector2Int tile2, Vector2Int dir)
    {
        Vector2Int backTile1 = tile1 - dir;
        Vector2Int backTile2 = tile2 - dir;

        bool tile1InRoom = IsInGridBounds(layout, backTile1) && layout[backTile1.x][backTile1.y] == 1;
        bool tile2InRoom = IsInGridBounds(layout, backTile2) && layout[backTile2.x][backTile2.y] == 1;

        return tile1InRoom && tile2InRoom;
    }

    /// <summary>
    /// Creates hallway tiles extending outward from a door
    /// </summary>
    private void BuildHallway(Vector3 doorWorldPosition, Vector3 direction)
    {
        for (int length = 1; length <= MAX_HALLWAY_LENGTH; length++)
        {
            for (int segment = 0; segment < 2; segment++) // Double-wide hallway
            {
                // Calculate hallway tile position
                Vector3 hallwayLocalPos = doorWorldPosition +
                                         direction * (length * TILE_SIZE) -
                                         new Vector3(TILE_SIZE / 2f, 0, TILE_SIZE / 2f) +
                                         new Vector3(segment * TILE_SIZE, 0, 0);

                if (!IsPositionOccupied(hallwayLocalPos))
                {
                    Instantiate(_hallwayFloorPrefab,
                               hallwayLocalPos,
                               Quaternion.identity,
                               _currentRoomParent.transform);
                }
            }
        }
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Converts grid coordinates to local position relative to anchor
    /// </summary>
    private Vector3 GetLocalPositionFromGrid(int x, int y, Vector2Int anchor)
    {
        float xOffset = (x - anchor.x) * TILE_SIZE;
        float zOffset = (y - anchor.y) * TILE_SIZE;
        return new Vector3(xOffset, 0, zOffset);
    }

    private Vector3 GetLocalPositionFromGrid(Vector2Int gridPos, Vector2Int anchor)
    {
        return GetLocalPositionFromGrid(gridPos.x, gridPos.y, anchor);
    }

    /// <summary>
    /// Checks if position is occupied using physics overlap
    /// </summary>
    private bool IsPositionOccupied(Vector3 worldPosition)
    {
        Vector3 halfExtents = new Vector3(TILE_SIZE * 0.45f, TILE_SIZE * 0.45f, TILE_SIZE * 0.45f);
        return Physics.CheckBox(worldPosition, halfExtents, Quaternion.identity, ~0);
    }

    /// <summary>
    /// Coroutine for asynchronous navmesh baking
    /// </summary>
    private IEnumerator BakeNavMeshAsync()
    {
        yield return null; // Allow frame completion before baking
        if (_navMeshSurfaces != null)
        {
            foreach (NavMeshSurface surface in _navMeshSurfaces)
                surface.BuildNavMesh();
        }
    }

    private void ShuffleList<T>(IList<T> list)
    {
        System.Random rng = new System.Random();
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T value = list[k];
            list[k] = list[n];
            list[n] = value;
        }
    }
    #endregion

    #region Data Structures
    [System.Serializable]
    public class EnemyData
    {
        [SerializeField] private GameObject _prefab;
        [SerializeField, Range(1, 3)] private int _tileWidth = 1;
        [SerializeField, Range(1, 3)] private int _tileHeight = 1;

        public GameObject Prefab => _prefab;
        public int TileWidth => _tileWidth;
        public int TileHeight => _tileHeight;
    }

    private class DoorCandidate
    {
        public Vector2Int TilePosition { get; }
        public Vector2Int Direction { get; }

        public DoorCandidate(Vector2Int tilePosition, Vector2Int direction)
        {
            TilePosition = tilePosition;
            Direction = direction;
        }
    }
    #endregion

    #region Helper Classes
    private static class Vector2IntHelper
    {
        public static Vector2Int[] GetCardinalDirections() =>
            new Vector2Int[] { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public static Vector2Int GetPerpendicular(Vector2Int direction)
        {
            return new Vector2Int(-direction.y, direction.x);
        }
    }
    #endregion
}