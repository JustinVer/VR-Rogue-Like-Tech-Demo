using System.Collections;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Holster : MonoBehaviour
{
    Holsterable holsteredItem;
    [SerializeField] XRSocketInteractor holsterSocket;
    [SerializeField] float moveSpeed = 5f;
    private Coroutine moveRoutine;

    public void weaponHolstered()
    {
        try
        {
            if (holsteredItem != null)
            {
                holsteredItem.holster = null;
            }
            holsteredItem = holsterSocket.firstInteractableSelected.transform.gameObject.GetComponent<Holsterable>();
            if (holsteredItem == null)
            {
                holsteredItem = holsterSocket.firstInteractableSelected.transform.gameObject.GetComponentInChildren<Holsterable>();
            }
            holsteredItem.holster = this;

        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
        }
    }

    public void Reholster(Holsterable item)
    {

        if (item == holsteredItem)
        {
            if (moveRoutine != null)
                StopCoroutine(moveRoutine);

            moveRoutine = StartCoroutine(MoveToHolster(item.gameObject));
        }
    }

    private IEnumerator MoveToHolster(GameObject item)
    {
        float elapsed = 0f;
        Vector3 startPos = item.transform.position;
        Quaternion startRot = item.transform.rotation;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.detectCollisions = false;
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
        while (elapsed + Time.deltaTime < moveSpeed)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveSpeed);

            // Update target each frame in case the holster moves
            Vector3 targetPos = holsterSocket.transform.position;
            Quaternion targetRot = holsterSocket.transform.rotation;

            item.transform.position = Vector3.Lerp(startPos, targetPos, t);
            item.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            yield return new WaitForEndOfFrame();
        }
        if (rb != null)
        {
            rb.detectCollisions = true;
            rb.isKinematic = false;
        }

        // Snap to final position/rotation to finish
        item.transform.position = holsterSocket.transform.position;
        item.transform.rotation = holsterSocket.transform.rotation;
        holsteredItem.resetGroundTimer();


    }

    public void ReholsterObject(GameObject item)
    {
        if (item == holsteredItem.gameObject)
        {
            if (moveRoutine != null)
                StopCoroutine(moveRoutine);

            moveRoutine = StartCoroutine(MoveToHolster(item));
        }
    }
}
