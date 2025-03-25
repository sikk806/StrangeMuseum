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

    //[SerializeField]
    //private bool isPickedUp = false;

    //[ServerRpc(RequireOwnership = false)]//RPC 호출 시 소유 여부에 관계없이 호출 가능.
    //public void PickUpItemServerRpc(GameObject go)
    //{
    //    if (isPickedUp) return;

    //    isPickedUp = true;
    //    PickUpItemClientRpc(go);
    //}

    //[ClientRpc]
    //private void PickUpItemClientRpc(GameObject go)
    //{
    //    go.gameObject.SetActive(false); // 모든 클라이언트에서 비활성화
    //}


    //public  void DestroyItem(GameObject obj) //NetworkBehaviour 용 OnDestroy()임. 
    //{
    //    if (IsServer == false)
    //    {
    //        return;
    //    }

    //    Destroy(obj);


    //}
}
