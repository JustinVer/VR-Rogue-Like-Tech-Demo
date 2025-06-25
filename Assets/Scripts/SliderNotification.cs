using UnityEngine;

public class SliderNotification : MonoBehaviour
{
    [SerializeField] CrossbowShooter gunScript;
    [SerializeField] Collider sliderCollider;
    private bool collided = false;

    private void OnTriggerEnter(UnityEngine.Collider other)
    {
        if (other == sliderCollider)
        {
            collided = true;
        }
    }

    public void sliderGrabed()
    {
        collided = false;
        gunScript.enabled = false;
    }

    public void sliderReleased()
    {
        gunScript.enabled = true;
        if (collided)
        {
            gunScript.sliderPulled();
        }
    }
}
