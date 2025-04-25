using Mirror;
using Unity.Services.Authentication;
using UnityEngine;

public class NetworkItem : NetworkBehaviour
{
    [SerializeField]
    public bool isPickedUp = false;

    [Command(requiresAuthority = false)]
    public void CmdPickUpItem(uint netId)
    {
        if (isPickedUp) return;
        isPickedUp = true;

        NetworkIdentity objIdentity = NetworkServer.spawned[netId];
        if (objIdentity != null)
        {
            objIdentity.gameObject.SetActive(false);
            RpcPickUpItem(netId);
        }
    }

    [ClientRpc]
    private void RpcPickUpItem(uint netId)
    {
        if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity objIdentity))
        {
            objIdentity.gameObject.SetActive(false);
        }
    }

    public void DestroyItem(GameObject obj)
    {
        if (!isServer)
            return;

        Destroy(obj.gameObject);
    }
}
