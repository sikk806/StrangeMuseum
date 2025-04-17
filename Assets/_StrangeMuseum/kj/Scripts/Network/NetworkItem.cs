using Unity.Netcode;
using UnityEngine;

public class NetworkItem : NetworkBehaviour
{
    [SerializeField]
    public bool isPickedUp = false;

    [ServerRpc(RequireOwnership = false)]
    public void PickUpItemServerRpc(NetworkObjectReference objRef)
    {
        if (isPickedUp) return;

        isPickedUp = true;

        PickUpItemClientRpc(objRef);
    }

    [ClientRpc]
    private void PickUpItemClientRpc(NetworkObjectReference objRef)
    {
        if (objRef.TryGet(out NetworkObject obj))
        {
            obj.gameObject.SetActive(false);
        }
    }

    public void DestroyItem(NetworkObject obj)
    {
        if (!IsServer)
            return;

        Destroy(obj.gameObject);
    }
}
