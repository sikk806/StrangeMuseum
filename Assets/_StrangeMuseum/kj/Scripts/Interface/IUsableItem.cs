using UnityEngine;

public interface IUsableItem
{
    public void UseServerRpc(uint id);

    public ItemData.ItemList GetItemList();
    public ItemData.ItemUseType GetItemType();
}
