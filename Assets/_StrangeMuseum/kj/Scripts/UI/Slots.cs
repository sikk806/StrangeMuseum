using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class Slots : MonoBehaviour //슬롯들의 부모 오브젝트(Slots)
{

    [SerializeField]
    private GameObject SlotPrefab; //슬롯 프리펩

    [SerializeField]
    int maxSlot; //슬롯 최대 개수

    public List<SlotData> slotDataList = new List<SlotData>();

    private List<Slot> slotList = new List<Slot>();

    public int SelectedIndex { get;  set; } = 0;

    public void SlotSet()
    {
        for(int i = 0; i < maxSlot; i ++)
        {
            GameObject slotPrefab = Instantiate(SlotPrefab, this.transform, false);
            slotPrefab.name = "Slot_" + i;


            SlotData data = new SlotData
            {
                IsEmpty = true,
                SlotObj = slotPrefab
            };

            slotDataList.Add(data);

            Slot slot = slotPrefab.GetComponent<Slot>();
            slot.Data = data;

            slotPrefab.GetComponentInChildren<TextMeshProUGUI>().text = (i + 1).ToString();
        }

        SelectSlot(0); //처음엔 0번
    }
    public void AddItem(GameObject item, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= slotDataList.Count) return;

        IUsableItem usableItem = item.GetComponent<IUsableItem>();

        if (usableItem != null)
        {
            ItemData.ItemList list = usableItem.GetItemList();
            ItemData.ItemUseType type = usableItem.GetItemType();

            if (ItemManager.Instance.itemDictionary.ContainsKey(list))
            {
                SlotData slotData = slotDataList[slotIndex];

                slotData.itemList = list;
                slotData.itemUseType = type;
                slotData.IsEmpty = false;

                SelectSlot(slotIndex);
            }

        }
    }


    public void SelectSlot(int index)
    {
        if (index < 0 || index >= slotList.Count) return;

        slotList[SelectedIndex].SlotDefalutImage();

        SelectedIndex = index;

        slotList[SelectedIndex].SlotSelectImage();
    }


    public SlotData GetSelectedData()
    {
        return slotDataList[SelectedIndex];
    }

    public void UseSelectedItem()
    {
        SlotData data = GetSelectedData();

        if (data.IsEmpty) return;

        Slot currentSlot = slotDataList[SelectedIndex].SlotObj.GetComponent<Slot>();

        IUsableItem usableItem = currentSlot.AssignedItem[SelectedIndex].GetComponent<IUsableItem>();

        if (usableItem != null)
        {
            usableItem.UseServerRpc(NetworkManager.Singleton.LocalClientId); //아이템 기능 메서드 호출 부분

            slotDataList[SelectedIndex].IsEmpty = true;

        }
      
    }

    public void SwapFirstTwo()
    {
        if (slotDataList.Count < 2) return;
        var a = slotDataList[0];
        var b = slotDataList[1];
        (a.itemList, b.itemList) = (b.itemList, a.itemList);
        (a.itemUseType, b.itemUseType) = (b.itemUseType, a.itemUseType);
        (a.IsEmpty, b.IsEmpty) = (b.IsEmpty, a.IsEmpty);
    }

}
