using UnityEngine;

public class Chest : MonoBehaviour
{

    public Animator chestAnim;

    public void trigger()
    {
        if (GeneralReferences.Instance.allPowerUps != null)
        {
            GameObject powerUp = Instantiate(GeneralReferences.Instance.allPowerUps[Random.Range(0, GeneralReferences.Instance.allPowerUps.Length)]);
            powerUp.transform.parent = transform;
            powerUp.transform.position = transform.position;
            powerUp.transform.rotation = this.transform.rotation;
        }
        chestAnim.SetTrigger("open");
    }
}
