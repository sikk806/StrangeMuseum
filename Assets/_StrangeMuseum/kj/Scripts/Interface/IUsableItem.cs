using UnityEngine;

public interface IUsableItem
{
    public void UseServerRpc(ulong id);

    Define.ItemUseType GetItemLayer();
    Define.ItemList GetItemType();
}
