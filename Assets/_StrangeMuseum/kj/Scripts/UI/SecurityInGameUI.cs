using System.Collections.Generic;
using TMPro;
using Unity.Burst.CompilerServices;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using static ItemData;

public class SecurityInGameUI : NetworkBehaviour
{
    private static SecurityInGameUI instance;

    public static SecurityInGameUI Instance
    {
        get
        {
            return instance;
        }
    }

    public List<SlotData> SlotData = new List<SlotData>();
    public int maxSlot = 5;

    public GameObject SlotPrefab;
    public GameObject SlotPrefabParent;



    [SerializeField]
    GameObject InteractionUI;

    [SerializeField]
    Sprite UsingIcon;

    [SerializeField]
    Sprite PickUpIcon;
    private IUsableItem usableItem;

    public int selectedSlot = 0;

    PlayerInteraction Interaction;
    SecurityInteraction bouncerInteraction;



    [SerializeField]
    TextMeshProUGUI ItemExplain;

    public TextMeshProUGUI itemObjectName;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);

        }
    }

    private void Start()
    {
        //ItemManager.Instance.Initialize();

        SlotSet();

        InteractionUI.SetActive(false);


       // Interaction = GameObject.FindGameObjectWithTag("Bouncer").GetComponent<PlayerInteraction>();


        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        // 모든 경비원 오브젝트를 찾고, 로컬 클라이언트 ID와 비교하여 해당 경비원의 Interaction을 가져옴
        GameObject[] bouncers = GameObject.FindGameObjectsWithTag("Bouncer");
        foreach (var bouncer in bouncers)
        {
            // 각 경비원에 대해 로컬 클라이언트 ID가 일치하는지 확인
            if (bouncer.GetComponent<NetworkObject>().OwnerClientId == localClientId)
            {
                // 일치하는 경비원 찾으면 그 경비원의 Interaction 객체를 할당
                if (GameManager.Instance.PlayerStat.Value[OwnerClientId] == "Statue")
                {
                    Destroy(this.gameObject);
                }

                bouncerInteraction = bouncer.GetComponent<SecurityInteraction>();
                Interaction = bouncer.GetComponent<PlayerInteraction>();
                break; // 하나만 찾으면 됨
            }
        }

      
    }

    private void SlotSet()
    {

        for (int i = 0; i < maxSlot; i++)
        {
            GameObject go = Instantiate(SlotPrefab, SlotPrefabParent.transform, false);
            go.name = "Slot_" + i;
            SlotData slot = new SlotData();
            slot.IsEmpty = true;
            slot.SlotObj = go;
            SlotData.Add(slot);

            go.GetComponentInChildren<TextMeshProUGUI>().text = (i + 1).ToString(); //아이템 슬롯 키 ui 표시 . ex 1~5
        }

    }

    private void Update()
    {
        ItemSlotUpdate();

        SecurityInteractionUI();

        if(Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("아이템 itemDictionary 에 저장된 현재 갯수" + ItemManager.Instance.inventoryDictionary.Count);
        }

        if (Input.GetKeyDown(KeyCode.E) && Interaction.isMissionProgress.Value == false)
        {
            Debug.Log("경비원이 아이템을 사용했을 때 경비원의 id : " + NetworkManager.Singleton.LocalClientId);
            UsingItem();
        }

        // 숫자 키(1~4)로 아이템 선택
        for (int i = 0; i < SlotData.Count; i++)
        {
            if (Input.GetKeyDown((KeyCode)(49 + i))) // KeyCode.Alpha1 == 49
            {
                OnItemNameUI(ItemList.None); //아이템 이름 지웠다가 다시 업데이트
                OnInteractionUI(InteractionType.None); //아이템 이름 지웠다가 다시 업데이트

                OnSlotUpdateUI(i);
            }
            else
            {
                SlotData[selectedSlot].SlotObj.GetComponent<Slot>().SlotSelectImage();
            }
        }

    
    }

    private void SecurityInteractionUI()
    {
        if (bouncerInteraction.isInteracted.Value == true)
        {
            if (SlotData[selectedSlot].IsEmpty)
            {
                if (bouncerInteraction.RayItem != null)
                {
                    ItemList currentItem = bouncerInteraction.RayItem.GetComponent<IUsableItem>().GetItemList();
                    OnItemNameUI(currentItem, bouncerInteraction.RayItem.gameObject);
                    OnInteractionUI(InteractionType.PickUp);

                }

            }
            else //비어  있지 않다면
            {
                if (bouncerInteraction.RayItem != null)
                {
                    ItemList currentItem = bouncerInteraction.RayItem.GetComponent<IUsableItem>().GetItemList();
                    OnItemNameUI(currentItem, bouncerInteraction.RayItem.gameObject);

                    OnInteractionUI(InteractionType.PickUp);

                }
            }
        }
        else
        {

            if (SlotData[selectedSlot].IsEmpty) //슬롯이 비어 있을 때
            {
                OnInteractionUI(InteractionType.None); //아무것도  안 뜸

                OnItemNameUI();
            }
        }
    } // 슬롯안에 아이템 여부에 따라 경비원이 아이템을 바라봤을 때

    public void AddItemToSlot(GameObject item, int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotData.Count) return;

        IUsableItem usableItem = item.GetComponent<IUsableItem>();

        if (usableItem != null)
        {
            ItemData.ItemList itemList = usableItem.GetItemList(); // 아이템의 종류 확인
            ItemData.ItemUseType itemType = usableItem.GetItemType(); // 아이템의 종류 확인

           // ItemManager.Instance.AddItem(itemList);


            if (ItemManager.Instance.itemDictionary.ContainsKey(itemList))
            {
                
                // 해당 아이템이 ItemManager에 존재하는지 확인하고, 아이템 정보를 가져옵니다.
                SlotData[slotIndex].itemUseType = itemType; // itemLayer에 해당 아이템 사용 타입 저장
                SlotData[slotIndex].itemList = itemList; // 아이템 사용 대상 미리 저장
            }
        }

        SlotData[selectedSlot].SlotObj.GetComponent<Slot>().SlotDefalutImage(); // 이전 슬롯 초기화
        //현재 슬롯을 아이템 추가된 슬롯으로 변경
        selectedSlot = slotIndex;
        OnSlotUpdateUI(selectedSlot); // UI 업데이트
    }

    private void OnSlotUpdateUI(int slotIndex) // 선택된 슬롯 or 선택하지 않는 슬롯 ui 업데이트 
    {

        if (selectedSlot != -1)
        {
            SlotData[selectedSlot].SlotObj.GetComponent<Slot>().SlotDefalutImage(); // 이전 슬롯 초기화
        }

        selectedSlot = slotIndex;
        SlotData[selectedSlot].SlotObj.GetComponent<Slot>().SlotSelectImage(); // 새로운 슬롯 강조

    }

    ItemData.ItemUseType itemType;
    ItemData.ItemList itemList;

    private void ItemSlotUpdate()
    {
        if (bouncerInteraction.isInteracted.Value == true && SlotData[selectedSlot].IsEmpty == false)
        {
            OnItemExplainUI(ItemData.ItemList.None);
            return; //아이템 바라본 상태에서, 슬롯이 비어있지 않을 때 바라본 아이템을 우선으로
        }
        if (SlotData[selectedSlot].IsEmpty)
        {
            OnItemExplainUI(ItemData.ItemList.None);
            return;
        }

        //Self 타입인지 Target 타입인지 아이템 사용 방식 가져옴
        itemType = SlotData[selectedSlot].itemUseType;
        itemList =  SlotData[selectedSlot].itemList;

        switch (itemType)
        {
            case ItemUseType.None:
                OnInteractionUI(InteractionType.None);
                OnItemExplainUI(ItemList.None);
                break;

            case ItemUseType.Self: //자기 자신에게 사용하는 아이템을 든 상태 . 박스, 에너지 드링크, 볼펜


                if (bouncerInteraction.isInteracted.Value == true) //다른 아이템 바라볼 경우
                {
                    OnItemNameUI(itemList); //이름 보여주고
                    OnInteractionUI(InteractionType.PickUp); //픽업 ui 보여주고
                    OnItemExplainUI(ItemList.None);  //해당 아이템은 설명하지 않음. 
                }
                else //그냥 기존 아이템을 상태일 경우
                {
                    OnInteractionUI(InteractionType.Self); //사용법 ui 보여주고

                    OnItemExplainUI(itemList); //아이템 설명

                    OnItemNameUI(ItemList.None); //아이템 이름 안보여주고
                }


                break;
            case ItemUseType.Target: //상대에게 사용하는 아이템을 든 상태 . 피 묻은 천, 구속구

                if (bouncerInteraction.isInteracted.Value == true) //다른 아이템 바라볼 경우
                {
                    OnItemNameUI(itemList); //이름 보여주고
                    OnInteractionUI(InteractionType.PickUp); //픽업 ui 보여주고
                    OnItemExplainUI(ItemList.None);  //해당 아이템은 설명하지 않음. 
                }
                else //그냥 기존 아이템을 상태일 경우
                {
                    if (bouncerInteraction.IsStatue.Value)
                    {
                        OnInteractionUI(InteractionType.Target); //사용법 ui 보여주고
                    }
                    else
                    {
                        OnInteractionUI(InteractionType.None); //사용법 ui 보여주고
                    }

                    OnItemExplainUI(itemList); //아이템 설명
                    OnItemNameUI(ItemList.None); //아이템 이름 안보여주고

                }

                break;
        }


    }



    public void UsingItem() //아이템 사용 부분.
    {
        if (SlotData[selectedSlot].IsEmpty)
        {
            return;
        }

        Slot slotComponent = SlotData[selectedSlot].SlotObj.GetComponent<Slot>();

        if (slotComponent.AssignedItem != null && slotComponent.AssignedItem.Length > 0)
        {
            usableItem = slotComponent.AssignedItem[selectedSlot].GetComponent<IUsableItem>();

            if (usableItem != null)
            {
                usableItem.UseServerRpc(NetworkManager.Singleton.LocalClientId); //아이템 기능 메서드 호출 부분

                if (slotComponent.AssignedItem.Length == 0)
                {
                    SlotData[selectedSlot].IsEmpty = true;
                }
            }
        }
    }


    public void OnInteractionUI(InteractionType type = InteractionType.None)
    {

        if (type == InteractionType.None || Interaction.isMissionProgress.Value == true)
        {
            InteractionUI.gameObject.SetActive(false);
            return;
        }
        else
        {
            InteractionUI.gameObject.SetActive(true);

            Image uiImage = InteractionUI.GetComponent<Image>();
            TextMeshProUGUI text = InteractionUI.GetComponentInChildren<TextMeshProUGUI>();
            switch (type)
            {
                case InteractionType.PickUp:
                    uiImage.sprite = PickUpIcon; // 아이템 줍기 아이콘
                    text.text = ": Pick Up";
                    break;
                case InteractionType.Self:
                    uiImage.sprite = UsingIcon; // 아이템 줍기 아이콘
                    text.text = ": Self to Using";
                    break;
                case InteractionType.Target:

                    uiImage.sprite = UsingIcon; // 아이템 줍기 아이콘
                    text.text = ": Target to Using";
                    break;
            }
        }



    }

    public void OnItemExplainUI(ItemData.ItemList itemKey = ItemData.ItemList.None)
    {
        if (Interaction.isMissionProgress.Value == true)
        {
            ItemExplain.text = " ";
            return;
        }


        if (ItemManager.Instance.itemDictionary.TryGetValue(itemKey, out var value))
        {
            // itemKey에 맞는 설명을 switch 문을 사용하여 설정
            switch (itemKey)
            {
                case ItemData.ItemList.HandCuff:
                    ItemExplain.text = value.itemExplain;
                    break;
                case ItemData.ItemList.EnergyDrink:
                    ItemExplain.text = value.itemExplain;
                    break;
                case ItemData.ItemList.Box:
                    ItemExplain.text = value.itemExplain;
                    break;
                case ItemData.ItemList.Cover:
                    ItemExplain.text = value.itemExplain;
                    break;
                case ItemData.ItemList.Pen:
                    ItemExplain.text = value.itemExplain;
                    break;
                case ItemData.ItemList.None:
                    ItemExplain.text = " ";
                    break;
            }
        }
        else
        {
            // 아이템이 딕셔너리에 없는 경우
            ItemExplain.text = " ";
        }
    }

    public void OnItemNameUI(ItemData.ItemList itemKey = ItemData.ItemList.None, GameObject obj = null)
    {

        if (Interaction.isMissionProgress.Value == true)
        {
            itemObjectName.text = " ";
            return;
        }

        if(ItemManager.Instance.itemDictionary.TryGetValue(itemKey,out var value))
        {
            switch (itemKey)
            {
                case ItemList.HandCuff:
                    itemObjectName.text = value.itemName;
                    break;
                case ItemList.EnergyDrink:
                    itemObjectName.text = value.itemName;
                    break;
                case ItemList.Box:
                    itemObjectName.text = value.itemName;
                    break;
                case ItemList.Cover:
                    itemObjectName.text = value.itemName;
                    break;
                case ItemList.Pen:
                    itemObjectName.text = value.itemName;
                    break;
                case ItemList.None:
                    itemObjectName.text = " ";
                    break;
            }
        }
        else
        {
            itemObjectName.text = " ";
        }
      
    }



    public void OnDestroyItemUI(GameObject item, int slotIndex)
    {


        Transform slotTransform = SlotData[slotIndex].SlotObj.transform;

        // 자식이 존재하는지 확인
        if (slotTransform.childCount > 1)  // 자식이 2개 이상일 때만 1번 인덱스를 사용할 수 있음
        {

            Transform itemUITransform = slotTransform.GetChild(1);

            // 자식 객체가 이미 삭제되었는지 확인
            if (itemUITransform != null && itemUITransform.gameObject != null)
            {
                IUsableItem usableItem = item.GetComponent<IUsableItem>();

                ItemData.ItemList itemList = usableItem.GetItemList(); // 아이템의 종류 확인

                ItemManager.Instance.RemoveItem(itemList);

                Destroy(itemUITransform.gameObject); // 해당 자식 객체 삭제

                SlotData[slotIndex].IsEmpty = true; // 빈 슬롯 됨
            }
            else
            {
                Debug.Log($"Slot {slotIndex}의 자식이 이미 삭제되었습니다.");
            }
        }
        else
        {
            Debug.Log($"Slot {slotIndex}에 충분한 자식이 존재하지 않습니다.");
        }
    }
}