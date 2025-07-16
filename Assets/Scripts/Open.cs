using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class Open : MonoBehaviour
{
    [SerializeField] protected float openAmount = 95f;
    [SerializeField] protected float openDuration = 1f;
    [SerializeField] protected int numRotations = 2;
    public void OpenObject()
    {
        List<GameObject> objects = new List<GameObject>();
        this.gameObject.GetChildGameObjects(objects);
        for (int i = 0; i < objects.Count && i < numRotations; i++)
        {
            float rotateAmount = openAmount;
            if (i % 2 == 1)
            {
                rotateAmount = openAmount * -1f;
            }
            StartCoroutine(RotateObject(objects[i].transform, rotateAmount, openDuration));
        }
    }
    public void CloseObject()
    {
        List<GameObject> objects = new List<GameObject>();
        this.gameObject.GetChildGameObjects(objects);
        for (int i = 0; i < objects.Count && i < numRotations; i++)
        {
            float rotateAmount = openAmount;
            if (i % 2 != 1)
            {
                rotateAmount = openAmount * -1f;
            }
            StartCoroutine(RotateObject(objects[i].transform, rotateAmount, openDuration));
        }
    }
    private IEnumerator RotateObject(Transform doorPart, float angle, float duration)
    {
        Quaternion initialRotation = doorPart.localRotation;
        Quaternion targetRotation = initialRotation * Quaternion.Euler(0, angle, 0);
        float time = 0;

        while (time < duration)
        {
            doorPart.localRotation = Quaternion.Slerp(initialRotation, targetRotation, time / duration);
            time += Time.deltaTime;
            yield return null;
        }

        doorPart.localRotation = targetRotation;
    }

}
