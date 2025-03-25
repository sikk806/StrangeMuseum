using UnityEngine;

public interface IUsableItem
{
    public void UseServerRpc(ulong id);

    Define.ItemLayer GetItemLayer();
    Define.ItemType GetItemType();
}
