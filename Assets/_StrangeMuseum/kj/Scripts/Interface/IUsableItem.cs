using UnityEngine;

public interface IUsableItem
{
    public void UseServerRpc(ulong id);
    public void ItemView(ulong id);


    public ItemData.ItemList GetItemList();
    public ItemData.ItemUseType GetItemType();
}
