using System.Collections.Generic;
using TMPro;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using static UnityEditor.Timeline.Actions.MenuPriority;
using System.Collections;

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

    private SecurityInteraction securityInteraction;

    [SerializeField]
    GameObject BlurLockImage;
    public bool isAddItem; //첫 아이템을 추가했을 때, 일회성임

    public int SelectedIndex { get;  set; } = 0;

    [SerializeField]
    float ItemUsingCooltime;

    public bool isItemCooltime;

    private void Update()
    {

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

            if (slot == slotList[0]) //회전하는 슬롯인 슬롯 리스트의 0번째 슬롯만 사이즈 세팅
            {
                SlotInitSizeSet(slot);
            }


        }
    }

    private void SlotInitSizeSet(Slot slot)
    {
        RectTransform slotRect = slot.GetComponent<RectTransform>();
        RectTransform child0 = slot.transform.GetChild(0).GetComponent<RectTransform>();
        int slotChlidCount = slot.transform.childCount;
        slotRect.sizeDelta = new Vector2(124f, 117f);

        // 자식 0번째 오브젝트만 사이즈 조절
        if (slotChlidCount > 0)
        {

            child0.anchoredPosition = new Vector2(42, -38f); // 위치 이동
            child0.sizeDelta = new Vector2(39f, 37f);
            if (slotChlidCount == 2)
            {
                RectTransform child1 = slot.transform.GetChild(1).GetComponent<RectTransform>();
                child1.sizeDelta = new Vector2(118f, 115f);
            }

        }
    }




    public void UseSelectedItem(uint id)
    {
        
        if(slotList[0] == null) { return;}

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

        if(usableObj != null)
        {
            IUsableItem usableItem = usableObj.GetComponent<IUsableItem>();

            NetworkIdentity itemIdentity = usableObj.GetComponent<NetworkIdentity>();

            Debug.Log("아이템 사용 메서드 실행 ");
            usableItem.UseServerRpc(id); //아이템 기능 메서드 호출 부분

            StartCoroutine(ItemCooltime());

            if (SecurityInGameUI.Instance != null)
            {
                GameObject item = itemIdentity.gameObject; //.gameObject 로 GameObject로 변환

                int itemLayer = usableItem.GetItemlayer();

                SecurityInGameUI.Instance.OnDestroyItemUI(item, itemLayer);

            }

            currentSlot.SlotItemCount(usableItem.GetItemList());

            currentSlot.AssignedItem[usableIndex] = null;

            for (int j = usableIndex; j < currentSlot.AssignedItem.Length - 1; j++)
            {
                currentSlot.AssignedItem[j] = currentSlot.AssignedItem[j + 1];
                currentSlot.AssignedItem[j + 1] = null;
            }
        }

  

    }

    private IEnumerator ItemCooltime()
    {
        isItemCooltime = true;

        BlurLockImage.gameObject.SetActive(true);
        ShowCooltimeLockImage(true);

        yield return new WaitForSeconds(ItemUsingCooltime);

        ShowCooltimeLockImage(false);
        yield return new WaitForSeconds(0.5f);
        BlurLockImage.gameObject.SetActive(false);

        isItemCooltime = false;
    }

    private void ShowCooltimeLockImage(bool isActive)
    {
        RectTransform LockImage = BlurLockImage.transform.GetChild(0).GetComponent<RectTransform>();

        if (isActive)
        {
 
            LockImage.DOShakeAnchorPos(
              duration: 1.5f,       // 흔들리는 시간
              strength: new Vector2(15f, 0f), // 흔들림 세기 (좌우로 10만큼)
              vibrato: 12,          // 진동 횟수
              randomness: 90f,      // 랜덤 정도
              snapping: false,      // 정수 위치로 스냅할지 여부
              fadeOut: true         // 시간이 끝날수록 흔들림 줄어들지 여부
          );
        }
        else
        {
            Sequence unlockSequence = DOTween.Sequence();
            unlockSequence.Append(LockImage.DOShakeAnchorPos(0.5f, new Vector2(10f, 0f)))
                          .AppendInterval(1.0f)
                          .Append(LockImage.DORotate(new Vector3(0f, 0f, 0f), 0.3f)) // 회전
                          .Join(LockImage.GetComponent<CanvasGroup>().DOFade(0f, 0.3f));
                          
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
        RectTransform child0 = slot.transform.GetChild(0).GetComponent<RectTransform>();

        int slotChlidCount = slot.transform.childCount;
        if (i == 0)
        {
            slot.SlotSelectImage();
            slotRect.sizeDelta = new Vector2(118f, 115f);

            // 자식 0번째 오브젝트만 사이즈 조절
            if (slotChlidCount > 0)
            {
              
                child0.anchoredPosition = new Vector2(39f, -39f); // 위치 이동
                child0.sizeDelta = new Vector2(39f, 37f);
                if(slotChlidCount == 2)
                {
                    RectTransform child1 = slot.transform.GetChild(1).GetComponent<RectTransform>();
                    child1.sizeDelta = new Vector2(115f, 115f);
                }

            }
        }
        else
        {
            slot.SlotDefalutImage();
            slotRect.sizeDelta = new Vector2(85f, 85f);

            if (slotChlidCount > 0)
            {
                child0.anchoredPosition = new Vector2(25.1f, -26.8f);
                child0.sizeDelta = new Vector2(34, 31f);
                if (slotChlidCount == 2)
                {
                    RectTransform child1 = slot.transform.GetChild(1).GetComponent<RectTransform>();
                    child1.sizeDelta = new Vector2(75f, 75f);
                }

            }
        }
    }
}
