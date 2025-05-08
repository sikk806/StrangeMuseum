using System.Collections.Generic;
using TMPro;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class Slots : NetworkBehaviour //슬롯들의 부모 오브젝트(Slots)
{

    [SerializeField]
    private GameObject SlotPrefab; //슬롯 프리펩

    [SerializeField]
    int maxSlot; //슬롯 최대 개수

    public List<SlotData> slotDataList = new List<SlotData>();

    public List<Slot> slotList = new List<Slot>();

    private List<Vector3> initialPositions = new List<Vector3>();

    public Dictionary<ItemData.ItemList, Slot> itemSlotIndex = new Dictionary<ItemData.ItemList, Slot>();

    public int SelectedIndex { get;  set; } = 0;


    private void Update()
    {

        //for (int i = 0; i < slotDataList.Count; i++)
        //{
        //    if (Input.GetKeyDown(KeyCode.Alpha1 + i)) // KeyCode.Alpha1 == 49
        //    {
        //        SecurityInGameUI.Instance.OnItemExplainUI(ItemData.ItemList.None); //아이템 이름 지웠다가 다시 업데이트
        //        SecurityInGameUI.Instance.OnInteractionUI(InteractionType.None); //아이템 이름 지웠다가 다시 업데이트
        //        SecurityInGameUI.Instance.OnItemNameUI(ItemData.ItemList.None);

        //        SelectSlot(i);

        //    }
        //    else
        //    {
        //        slotDataList[SelectedIndex].SlotObj.GetComponent<Slot>().SlotSelectImage();
        //    }
        //}

        // ALT키로 슬롯 회전
        if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
        {
            RotateSlotsUp();
        }


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
            slot.SlotData.OriginalIndex = i;
           // slotParentChild.gameObject.GetComponentInChildren<TextMeshProUGUI>().text = (i + 1).ToString();

            initialPositions.Add(slotParentChild.position);  // 위치 저장

            Slot slotLists = slotList[i];
            RectTransform slotRect = slot.GetComponent<RectTransform>();
            SlotSizeSet(slot, slotRect, i);
        }
       // SelectSlot(0); //처음엔 0번
    }
    public void AddItem(GameObject item, Slot slot)
    {
        IUsableItem usableItem = item.GetComponent<IUsableItem>();

        if (usableItem != null)
        {
            ItemData.ItemList list = usableItem.GetItemList();
            ItemData.ItemUseType type = usableItem.GetItemType();

            if (ItemManager.Instance.itemDictionary.ContainsKey(list))
            {
                slot.SlotData.itemList = list;
                slot.SlotData.itemUseType = type;
                slot.SlotData.IsEmpty = false; 

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


    public void UseSelectedItem(uint id)
    {
       

        SlotData data = GetSelectedData();

        if (data.IsEmpty) return;


        Slot currentSlot = slotList[0].GetComponent<Slot>();

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
       // if (slotDataList.Count < 2) return;

        // === 순서 회전 ===
        //SlotData lastData = slotDataList[slotDataList.Count - 1];
        //slotDataList.RemoveAt(slotDataList.Count - 1);
        //slotDataList.Insert(0, lastData);

        Slot lastSlot = slotList[slotList.Count - 1];
        slotList.RemoveAt(slotList.Count - 1);
        slotList.Insert(0, lastSlot);

        // === 위치 이동 애니메이션 ===
        for (int i = 0; i < slotList.Count; i++)
        {
            Slot slot = slotList[i];
            slot.transform.DOMove(initialPositions[i], 0.3f);

            RectTransform slotRect = slot.GetComponent<RectTransform>();

            SlotSizeSet(slot, slotRect, i);
        }

        // 선택 슬롯 유지
        //  SelectSlot(0);
    }

    private void SlotSizeSet(Slot slot , RectTransform slotRect, int i)
    {

        if (i == 0)
        {
            slotRect.sizeDelta = new Vector2(124f, 117f);

            // 자식 0번째 오브젝트만 사이즈 조절
            if (slot.transform.childCount > 0)
            {
                RectTransform child0 = slot.transform.GetChild(0).GetComponent<RectTransform>();
                child0.anchoredPosition = new Vector2(42, -38f); // 위치 이동
                child0.sizeDelta = new Vector2(39f, 37f);
            }
        }
        else
        {
            slotRect.sizeDelta = new Vector2(85f, 85f);

            if (slot.transform.childCount > 0)
            {
                RectTransform child0 = slot.transform.GetChild(0).GetComponent<RectTransform>();
                child0.anchoredPosition = new Vector2(25f, -26f);
                child0.sizeDelta = new Vector2(26f, 25f);
            }
        }
    }
}
