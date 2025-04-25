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
        if (isPickedUp) return;
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
