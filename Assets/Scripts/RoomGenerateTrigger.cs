// HallwayTrigger.cs
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RoomGenerateTrigger : MonoBehaviour
{
    // This will be set by the LevelGenerationManager when the hallway is created.
    public Transform roomSpawnPoint;
    private bool hasBeenTriggered = false;
    public GameObject door;
    public GameObject closeDoor;

    private void Awake()
    {
        // Ensure the collider is set to be a trigger.
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        // Check if the player entered the trigger and it hasn't been used yet.
        // IMPORTANT: Make sure your player GameObject has the "Player" tag!
        if (!hasBeenTriggered && other.CompareTag("Player"))
        {
            hasBeenTriggered = true;
            Debug.Log("Player entered hallway trigger. Requesting new room.");

            if (GameManager.Instance != null && roomSpawnPoint != null)
            {
                // Tell the GameManager to create a new room at our designated spawn point.
                GameManager.Instance.RequestNewRoom(roomSpawnPoint);
            }

            try
            {
                closeDoor.GetComponent<Open>().CloseObject();
            }
            catch (System.Exception)
            { }

            // We destroy the trigger itself so it can't be used again.
            Destroy(roomSpawnPoint.gameObject);
            try
            {
                StartCoroutine(openDoor(door.GetComponent<Open>()));
                Debug.Log("After door courtine");
            }
            catch (System.Exception)
            { Debug.Log("error opening door"); }
            this.gameObject.GetComponent<Collider>().enabled = false;
        }
    }

    private IEnumerator openDoor(Open door)
    {
        Debug.Log("waiting to open door");
        yield return new WaitForSeconds(1f);
        Debug.Log("before door call");
        door.OpenObject();
        Debug.Log("after door call");
        Destroy(this.gameObject);
    }
}