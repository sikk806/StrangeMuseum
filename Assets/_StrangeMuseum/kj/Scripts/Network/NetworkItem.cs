using Mirror;
using Unity.Services.Authentication;
using UnityEngine;

public class NetworkItem : NetworkBehaviour
{
    [SerializeField]
    public bool isPickedUp = false;

    [Command(requiresAuthority = false)]
    public void CmdPickUpItemServerRpc(GameObject objRef)
    {
        Debug.Log("1");

        if (isPickedUp) return;

        Debug.Log("2");

        isPickedUp = true;


        objRef.gameObject.SetActive(false);
        RpcPickUpItemClientRpc(objRef);
    }

    [ClientRpc]
    private void RpcPickUpItemClientRpc(GameObject obj)
    {
        obj.gameObject.SetActive(false);
    }

    public void DestroyItem(GameObject obj)
    {
        if (!isServer)
            return;

        Destroy(obj.gameObject);
    }
}
