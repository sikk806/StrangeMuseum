using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class Slots : MonoBehaviour //슬롯들의 부모 오브젝트(Slots)
{

    [SerializeField]
    private GameObject SlotPrefab; //슬롯 프리펩

    [SerializeField]
    int maxSlot; //슬롯 최대 개수

    public List<SlotData> slotDataList = new List<SlotData>();

    public List<Slot> slotList = new List<Slot>();

    public int SelectedIndex { get;  set; } = 0;

    private void Update()
    {

        for (int i = 0; i < slotDataList.Count; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i)) // KeyCode.Alpha1 == 49
            {
                SecurityInGameUI.Instance.OnItemExplainUI(ItemData.ItemList.None); //아이템 이름 지웠다가 다시 업데이트
                SecurityInGameUI.Instance.OnInteractionUI(InteractionType.None); //아이템 이름 지웠다가 다시 업데이트


                SelectSlot(i);

            }
            else
            {
                slotDataList[SelectedIndex].SlotObj.GetComponent<Slot>().SlotSelectImage();
            }
        }

        if (SelectedIndex == 0 && Input.GetKeyDown(KeyCode.LeftAlt))
        {
            if (slotDataList.Count > 1 && !slotDataList[1].IsEmpty)
            {
                SwapFirstTwo();
                Debug.Log("2번 슬롯의 아이템이 1번 슬롯으로 이동했습니다.");
            }
        }
    }

    public void SlotSet() //SecurityInGameUI.cs에서 Start문에서 호출
    {
        for (int i = 0; i < maxSlot; i ++)
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
            slot.SlotData = data;

            slotList.Add(slot);

            slotPrefab.GetComponentInChildren<TextMeshProUGUI>().text = (i + 1).ToString();


        }
        SelectSlot(0); //처음엔 0번
    }
    public void AddItem(GameObject item, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex > slotDataList.Count) return;

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
        slotList[SelectedIndex].GetComponent<Slot>().SlotDefalutImage();

        //slotDataList[SelectedIndex].SlotObj.GetComponent<Slot>().SlotDefalutImage();

        SelectedIndex = index;

        slotList[SelectedIndex].GetComponent<Slot>().SlotSelectImage();

       // slotDataList[SelectedIndex].SlotObj.GetComponent<Slot>().SlotSelectImage();
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

        SlotData firstSlotData = slotDataList[0];
        SlotData secondSlotData = slotDataList[1];

        //1. 데이터 교체
        (firstSlotData.itemList, secondSlotData.itemList) = (secondSlotData.itemList, firstSlotData.itemList);
        (firstSlotData.itemUseType, secondSlotData.itemUseType) = (secondSlotData.itemUseType, firstSlotData.itemUseType);
        (firstSlotData.IsEmpty, secondSlotData.IsEmpty) = (secondSlotData.IsEmpty, firstSlotData.IsEmpty);

        //2. 이미지 교체
        Transform firstTransform = firstSlotData.SlotObj.transform.GetChild(1);
        Transform secondTransform = secondSlotData.SlotObj.transform.GetChild(1);

        Image firstSprite = firstTransform.GetComponent<Image>();
        Image secondSprite = secondTransform.GetComponent<Image>();

        Sprite tempSprite = firstSprite.sprite;
        firstSprite.sprite = secondSprite.sprite;
        secondSprite.sprite = tempSprite;
    }

}
