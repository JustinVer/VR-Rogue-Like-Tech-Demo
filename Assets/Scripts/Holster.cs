using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class Holster : MonoBehaviour
{
    Holsterable holsteredItem;
    XRSocketInteractor holsterSocket;

    public void weaponHolstered()
    {
        try
        {
            if (holsteredItem != null)
            {
                holsteredItem.holster = null;
            }
            holsteredItem = holsterSocket.firstInteractableSelected.transform.gameObject.GetComponent<Holsterable>();
            holsteredItem.holster = this;
        }
        catch (System.Exception)
        {
        }
    }

    public void Reholster(Holsterable item)
    {
        if (item == holsteredItem)
        {
            item.transform.position = holsterSocket.attachTransform.position;
        }
    }
}
