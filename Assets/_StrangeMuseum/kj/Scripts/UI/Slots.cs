using System.Collections.Generic;
using TMPro;
using Mirror;
using UnityEngine;
using UnityEngine.UI;

public class Slots : NetworkBehaviour //슬롯들의 부모 오브젝트(Slots)
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
                SecurityInGameUI.Instance.OnItemNameUI(ItemData.ItemList.None);

                SelectSlot(i);

            }
            else
            {
                slotDataList[SelectedIndex].SlotObj.GetComponent<Slot>().SlotSelectImage();
            }
        }

        //if (Input.GetKeyDown(KeyCode.LeftAlt))
        //{
        //    RotateSlotsUp();
        //    Debug.Log("퀵슬롯 회전 (ALT)");
        //}


    }

    public void SlotSet() //SecurityInGameUI.cs에서 Start문에서 호출
    {
        for (int i = 0; i < maxSlot; i ++)
        {
            Transform slotParentChild = this.transform.GetChild(i);

            SlotData data = new SlotData
            {
                IsEmpty = true,
                SlotObj = slotParentChild.gameObject
            };

            slotDataList.Add(data);

            Slot slot = slotParentChild.gameObject.GetComponent<Slot>();
            slot.SlotData = data;

            slotList.Add(slot);

            slotParentChild.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = (i + 1).ToString();


        }
        SelectSlot(0); //처음엔 0번
    }
    public void AddItem(GameObject item, int slotIndex)
    {
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

    private SecurityInteraction securityInteraction;


    public void SlotItemCount(ItemData.ItemList item, Slot slot) //슬롯에 들어 있는 아이템 개수
    {

        TextMeshProUGUI slotItemCount = slot.transform.GetChild(1).GetComponentInChildren<TextMeshProUGUI>();

        if (ItemManager.Instance.inventoryDictionary.TryGetValue(item, out int count))
        {
            Debug.Log($"현재 {item}의 개수는 {count} 입니다");

            slotItemCount.text = count.ToString();
        }
        else
        {
            slotItemCount.text = "";
        }

    }

    public void UseSelectedItem(uint id)
    {


        SlotData data = GetSelectedData();

        if (data.IsEmpty) return;


        Slot currentSlot = slotDataList[SelectedIndex].SlotObj.GetComponent<Slot>();

        // 사용 가능한 첫 번째 아이템 찾기
        GameObject usableObj = null;
        int usableIndex = -1;

        for (int i = 0; i < currentSlot.AssignedItem.Length; i++)
        {
            if (currentSlot.AssignedItem[i] != null)
            {
                usableObj = currentSlot.AssignedItem[i];
                usableIndex = i;
                break;
            }
        }

        IUsableItem usableItem = usableObj.GetComponent<IUsableItem>();


        if (usableItem != null)
        {
            Debug.Log("사용 메서드 실행 " + id);
            usableItem.UseServerRpc(id); //아이템 기능 메서드 호출 부분
            currentSlot.AssignedItem[usableIndex] = null;

            for (int j = usableIndex; j < currentSlot.AssignedItem.Length - 1; j++)
            {
                currentSlot.AssignedItem[j] = currentSlot.AssignedItem[j + 1];
                currentSlot.AssignedItem[j + 1] = null;
            }
        }

    }

    public void RotateSlotsUp()
    {
        if (slotDataList.Count < 2) return;

        // 슬롯 데이터 회전 (맨 뒤를 앞으로)
        SlotData lastData = slotDataList[slotDataList.Count - 1];
        slotDataList.RemoveAt(slotDataList.Count - 1);
        slotDataList.Insert(0, lastData);

        Slot lastSlot = slotList[slotList.Count - 1];
        slotList.RemoveAt(slotList.Count - 1);
        slotList.Insert(0, lastSlot);

        // UI 순서 조정: 슬롯들이 서로 위치 바꾸도록
        for (int i = 0; i < slotList.Count; i++)
        {
            slotList[i].transform.SetSiblingIndex(i);
            slotList[i].GetComponentInChildren<TextMeshProUGUI>().text = (i + 1).ToString(); // 표시도 정리
        }

        // 선택 슬롯을 항상 0번으로 유지
        SelectSlot(0);
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
