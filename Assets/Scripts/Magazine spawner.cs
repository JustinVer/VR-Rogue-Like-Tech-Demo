using UnityEngine;

public class Magazinespawner : MonoBehaviour
{
    public static Magazinespawner instance { private set; get; }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(this);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }

    public int isHoldingGun = 0;
    private Magazine.AmmoType ammoType = 0;
    private bool isMagazine = false;
    private GameObject currentMag;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] GameObject[] magazinePrefabs;

    private void Update()
    {
        if (isHoldingGun > 0 && !isMagazine)
        {
            spawnMag();
        }
        else if (isMagazine && isHoldingGun == 0)
        {
            isMagazine = false;
            Destroy(currentMag);
        }
    }

    private void spawnMag()
    {
        if (((int)ammoType) < magazinePrefabs.Length)
        {
            currentMag = Instantiate(magazinePrefabs[((int)ammoType)], spawnPoint);
            Magazine magScript = currentMag.GetComponent<Magazine>();
            magScript.magazinespawner = this;
            currentMag.transform.position = spawnPoint.position;
            isMagazine = true;
            currentMag.GetComponent<Rigidbody>().isKinematic = true;
        }
    }

    public void magGrabed()
    {
        currentMag.GetComponent<Rigidbody>().isKinematic = false;
        isMagazine = false;
        currentMag.transform.parent = null;
    }
}
