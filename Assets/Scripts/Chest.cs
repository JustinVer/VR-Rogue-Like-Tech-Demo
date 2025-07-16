using System.Collections.Generic;
using UnityEngine;

public class Chest : MonoBehaviour
{

    public Animator chestAnim;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float sideOffset = 0.27f;
    [SerializeField] private int powerUpCount = 2;
    private List<GameObject> pickups = new List<GameObject>();

    private void Start()
    {
        trigger();
    }
    public void trigger()
    {
        if (GeneralReferences.Instance.allPowerUps != null && GeneralReferences.Instance.allPowerUps.Length >= 1)
        {
            for (int i = 0; i < powerUpCount; i++)
            {
                GameObject powerUp = Instantiate(GeneralReferences.Instance.allPowerUps[Random.Range(0, GeneralReferences.Instance.allPowerUps.Length)]);
                pickups.Add(powerUp);
                powerUp.GetComponent<UpgradePickup>().parent = this.gameObject;
                powerUp.transform.parent = transform;
                Vector3 sideOffsetCalc = this.transform.right * (((powerUpCount / 2f) - 0.5f) - i) * sideOffset;
                powerUp.transform.position = transform.position + offset + sideOffsetCalc;
                powerUp.transform.rotation = this.transform.rotation;
            }
        }
        chestAnim.SetTrigger("open");
    }

    public void destroyPickups()
    {
        while (pickups.Count != 0)
        {
            GameObject pickup = pickups[0];
            pickups.RemoveAt(0);
            Destroy(pickup);
        }
    }
}
