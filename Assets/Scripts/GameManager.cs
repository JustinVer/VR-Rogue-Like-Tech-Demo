// GameManager.cs
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public LevelGenerationManager levelGenerator;
    public Transform firstRoomPosition;

    [SerializeField] private GameObject activeRoom1;
    private GameObject activeRoom2;
    private HashSet<Transform> activeSpawnPoints = new HashSet<Transform>();

    public Dictionary<GameObject, List<GameObject>> roomEnemies = new();
    public Dictionary<GameObject, List<GameObject>> roomDoors = new();
    public Dictionary<GameObject, List<GameObject>> roomChests = new();

    private void Awake()
    {
        // A simple singleton pattern to make the instance easily accessible.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Start()
    {
        if (levelGenerator == null)
        {
            Debug.LogError("LevelGenerationManager reference is not set in the GameManager!", this);
            return;
        }

        // Generate the very first room at the world origin.
        GenerateInitialRoom();
    }

    private void GenerateInitialRoom()
    {
        // We create a temporary spawn point for the first room.
        RequestNewRoom(firstRoomPosition);
    }

    /// <summary>
    /// Public method called by hallway triggers to request a new room.
    /// </summary>
    public void RequestNewRoom(Transform spawnPoint)
    {
        // Prevent generating multiple rooms from the same hallway exit.
        if (activeSpawnPoints.Contains(spawnPoint))
        {
            return;
        }

        Debug.Log($"New room requested at {spawnPoint.position}");
        activeSpawnPoints.Add(spawnPoint);

        if (activeRoom2 != null)
        {
            Destroy(activeRoom2);
        }
        activeRoom2 = activeRoom1;

        // Tell the level generator to create a room and give us the parent GameObject.
        activeRoom1 = levelGenerator.GenerateARoom(spawnPoint.position, spawnPoint.rotation);
    }

    public void OnEnemyDefeated(GameObject enemy)
    {
        // Find which room the enemy belongs to
        foreach (var entry in roomEnemies)
        {
            if (entry.Value.Contains(enemy))
            {
                entry.Value.Remove(enemy);

                // If all enemies in the room are defeated, open the door
                if (entry.Value.Count == 0)
                {
                    OpenDoors(entry.Key);
                    OpenChests(entry.Key);
                }
                break;
            }
        }
    }

    private void OpenDoors(GameObject room)
    {
        if (roomDoors.ContainsKey(room))
        {
            foreach (var door in roomDoors[room])
            {
                door.GetComponent<Open>().OpenObject();
            }
        }
    }
    private void OpenChests(GameObject room)
    {
        if (roomChests.ContainsKey(room))
        {
            foreach (var chest in roomChests[room])
            {
                chest.GetComponent<Chest>().trigger();
            }
        }
    }
}