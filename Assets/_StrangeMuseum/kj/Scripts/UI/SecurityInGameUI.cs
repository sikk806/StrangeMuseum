using TMPro;
using Mirror;
using UnityEngine;
using UnityEngine.UI;
using static ItemData;
using System.Collections.Generic;
using static ObjectData;

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


    [Header("UI References")]
    [SerializeField] 
    private GameObject InteractionUI;
    [SerializeField] 
    private Sprite UsingIcon;
    [SerializeField] 
    private Sprite PickUpIcon;
    [SerializeField]
    private Sprite HoldIcon;
    [SerializeField] 
    private TextMeshProUGUI ItemExplain;
    public TextMeshProUGUI itemObjectName;


    [Header("Slot Management")]
    public Slots SlotManager;


  



    private IUsableItem usableItem;

    [SerializeField]
    ItemData.ItemUseType itemType;
    [SerializeField]
    ItemData.ItemList itemList;

    PlayerInteraction Interaction;
    SecurityInteraction securityInteraction;
 

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
        base.OnStartLocalPlayer();

      

        InteractionUI.SetActive(false);

        GameObject[] bouncers = GameObject.FindGameObjectsWithTag("Bouncer");

        int SecuriyCount = 0;

        foreach (var bouncer in bouncers)
        {
            SecuriyCount++;

            playerId = (uint)bouncer.GetComponent<PlayerController>().ConnectionID;

            Debug.Log("경비원   " + SecuriyCount + "의 접속 ID" + playerId);

            securityInteraction = bouncer.GetComponent<SecurityInteraction>();

            Interaction = bouncer.GetComponent<PlayerInteraction>();
        }

        SlotManager.SlotSet(); //슬롯들 초기화

    }

    public uint playerId;

    private void Update() 
    {
        SecurityInteractionUI();

        if (Input.GetKeyDown(KeyCode.E) && Interaction.isMissionProgress == false)
        {
            if(SlotManager.isItemCooltime == false)
            {
                SlotManager.UseSelectedItem(playerId);
            }
            else
            {
                Debug.Log("아이템 사용 쿨타임 아직 안 끝남");
            }
           
        }
    }

    private void SecurityInteractionUI()
    {
        //오브젝트 관련

        if(securityInteraction.isObjectInteracted == true)
        {
            if(securityInteraction.SaveRayObject.GetComponent<IHoldInteractable>().IsCompleted()) 
            {
                OnObjectInteractionUnview();

                return; 
            } 
            //오브젝트 상호작용 후 오브젝트의 기능 정상 작동 되면 UI 보여주지 않음

            if (securityInteraction.SaveRayObject != null)
            {
                ObjectData.ObjectList objectList = securityInteraction.SaveRayObject.GetComponent<IHoldInteractable>().GetObjectList();

                OnInteractionUI(InteractionType.Hold);

                OnObjectNameUI(objectList);

                OnObjectExplainUI(objectList);
            }
        }
        else
        {
            OnObjectInteractionUnview();
        }


        if(securityInteraction.isObjectInteracted == true) { return; } //오브젝트와 상호작용 중이라면 아이템 상호작용 관련 UI 작동 X

        // 아이템 관련
        //1. 비어 있을 때 와 비어 있지 않을 때 
        // 2. 아이템을 쳐다 볼 때와 쳐다보지 않을 때 

        if (securityInteraction.isItemInteracted == true) //아이템 쳐다 볼 때
        {
            if(securityInteraction.SaveRayItem != null)
            {
                ItemList currentItem = SlotManager.slotList[0].SlotData.itemList;
                ItemUseType currentItemType = SlotManager.slotList[0].SlotData.itemUseType;

                if (SlotManager.slotList[0].SlotData.IsEmpty == false) //슬롯에 뭔가 있음
                {
                    currentItem = securityInteraction.SaveRayItem.GetComponent<IUsableItem>().GetItemList();

                    OnItemNameUI(currentItem); //바라본 아이템 이름 출력

                    OnItemExplainUI(ItemList.None); //바라본 아이템 설명 x

                    OnInteractionUI(InteractionType.PickUp); // 줍기 아이콘 활성화
                }
                else //슬롯이 텅 빔
                {
                    if(currentItem == ItemList.None) 
                    {
                        currentItem = securityInteraction.SaveRayItem.GetComponent<IUsableItem>().GetItemList();
                        
                        OnItemNameUI(currentItem);
                        OnInteractionUI(InteractionType.PickUp); // 줍기 아이콘 활성화
                        return;
                    }
                    OnItemNameUI(currentItem); //바라본 아이템 이름 출력

                    OnInteractionUI(InteractionType.PickUp); // 줍기 아이콘 활성화
                }

            }
           
        }
        else //아이템 쳐다보지 않을 때
        {
            if(securityInteraction.SaveRayItem != null )
            {
                if(SlotManager.slotList[0].SlotData.IsEmpty == false) //슬롯에 뭔가 있음
                {
                    ItemList currentItem = SlotManager.slotList[0].SlotData.itemList;
                    ItemUseType currentItemType = SlotManager.slotList[0].SlotData.itemUseType;

                    OnItemNameUI(currentItem);

                    OnItemExplainUI(currentItem);

                    ItemSlotUpdate(currentItemType);
                }
                else //슬롯이 텅 빔
                {
                    OnItemNameUI(ItemList.None);

                    OnItemExplainUI(ItemList.None);

                    OnInteractionUI(InteractionType.None);
                }
               
            }
            else
            {

                Debug.Log("아이템 상호작용 UI - X");

                OnItemNameUI(ItemList.None);

                OnItemExplainUI(ItemList.None);

                OnInteractionUI(InteractionType.None);
            }
          
        }

    }  // 슬롯안에 아이템 여부에 따라 경비원이 아이템을 바라봤을 때

    private void ItemSlotUpdate(ItemUseType itemType)
    {
        switch (itemType)
        {
            case ItemUseType.Self: //자기 자신에게 사용하는 아이템을 든 상태 . 박스, 에너지 드링크, 볼펜
                if (securityInteraction.isItemInteracted == true) //다른 아이템 바라볼 경우
                {
                    OnInteractionUI(InteractionType.PickUp); //픽업 ui 보여주고
                }
                else //그냥 기존 아이템을 상태일 경우
                {
                    OnInteractionUI(InteractionType.Self); 
                }
                break;
            case ItemUseType.Target: //상대에게 사용하는 아이템을 든 상태 . 피 묻은 천, 구속구

                if (securityInteraction.isItemInteracted == true) //다른 아이템 바라볼 경우
                {
                    OnInteractionUI(InteractionType.PickUp); //픽업 ui 보여주고
                }
                else //그냥 기존 아이템을 상태일 경우
                {
                    if (securityInteraction.IsStatue)
                    {
                        OnInteractionUI(InteractionType.Target); //사용법 ui 보여주고
                    }
                    else
                    {
                        OnInteractionUI(InteractionType.None); //사용법 ui 보여주고
                    }
                }
            break;

            case ItemUseType.None:
                OnInteractionUI(InteractionType.None);
            break;
        }


    }
    public void OnInteractionUI(InteractionType type = InteractionType.None)
    {

        if (type == InteractionType.None || Interaction.isMissionProgress == true)
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
                    text.text = "상호작용";
                    break;
                case InteractionType.Self:
                    uiImage.sprite = UsingIcon; // 아이템 줍기 아이콘
                    text.text = "자신에게 사용";
                    break;
                case InteractionType.Target:
                    uiImage.sprite = UsingIcon; // 아이템 줍기 아이콘
                    text.text = "대상에게 사용";
                    break;
                case InteractionType.Hold:
                    uiImage.sprite = PickUpIcon; // 아이템 줍기 아이콘
                    text.text = "상호작용";
                    break;
            }
        }



    }

    public void OnObjectInteractionUnview()
    {

        Debug.Log("오브젝트 UI 가리기");

        OnInteractionUI(InteractionType.None);

        OnObjectNameUI(ObjectData.ObjectList.None);

        OnObjectExplainUI(ObjectData.ObjectList.None);
    }

    private void OnObjectNameUI(ObjectData.ObjectList objectKey = ObjectData.ObjectList.None)
    {
        if(ItemManager.Instance.ObjectDictionary.TryGetValue(objectKey,out var value))
        {
            switch(objectKey)
            {
                case ObjectData.ObjectList.OldLever:
                    itemObjectName.text = value.ObjectName;
                    break;
                case ObjectData.ObjectList.None:
                    itemObjectName.text = "";
                    break;
            }
        }
    }
    private void OnObjectExplainUI(ObjectData.ObjectList objectKey = ObjectData.ObjectList.None)
    {
        if (ItemManager.Instance.ObjectDictionary.TryGetValue(objectKey, out var value))
        {
            switch (objectKey)
            {
                case ObjectData.ObjectList.OldLever:
                    ItemExplain.text = value.ObjectExplain;
                    break;
                case ObjectData.ObjectList.None:
                    ItemExplain.text = "";
                    break;
            }
        }
    }


    private void OnItemNameUI(ItemData.ItemList itemKey = ItemData.ItemList.None)
    {
        if (Interaction.isMissionProgress == true )
        {
            itemObjectName.text = " ";
            return;
        }

        if (ItemManager.Instance.itemDictionary.TryGetValue(itemKey, out var value))
        {
            // itemKey에 맞는 설명을 switch 문을 사용하여 설정
            switch (itemKey)
            {
                case ItemData.ItemList.HandCuff:
                    itemObjectName.text = value.ItemName;
                    break;
                case ItemData.ItemList.EnergyDrink:
                    itemObjectName.text = value.ItemName;
                    break;
                case ItemData.ItemList.Box:
                    itemObjectName.text = value.ItemName;
                    break;
                case ItemData.ItemList.Cover:
                    itemObjectName.text = value.ItemName;
                    break;
                case ItemData.ItemList.Pen:
                    itemObjectName.text = value.ItemName;
                    break;
                case ItemData.ItemList.None:
                    itemObjectName.text = " ";
                    break;
            }
        }
        else
        {
            // 아이템이 딕셔너리에 없는 경우
            itemObjectName.text = " ";
        }
    }

    private void OnItemExplainUI(ItemData.ItemList itemKey = ItemData.ItemList.None)
    {

        if (Interaction.isMissionProgress == true)
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
                    ItemExplain.text = value.ItemExplain;
                    break;
                case ItemData.ItemList.EnergyDrink:
                    ItemExplain.text = value.ItemExplain;
                    break;
                case ItemData.ItemList.Box:
                    ItemExplain.text = value.ItemExplain;
                    break;
                case ItemData.ItemList.Cover:
                    ItemExplain.text = value.ItemExplain;
                    break;
                case ItemData.ItemList.Pen:
                    ItemExplain.text = value.ItemExplain;
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

    public void OnDestroyItemUI(GameObject item, int slotIndex)
    {
        Transform slotTransform = SlotManager.slotDataList[slotIndex].SlotObj.transform;

        Transform itemUITransform = slotTransform.GetChild(1);

        // 자식 객체가 이미 삭제되었는지 확인
        if (itemUITransform != null && itemUITransform.gameObject != null)
        {
            IUsableItem usableItem = item.GetComponent<IUsableItem>();

            ItemData.ItemList itemList = usableItem.GetItemList(); // 아이템의 종류 확인
            ItemData.ItemUseType itemType = usableItem.GetItemType(); // 아이템의 종류 확인

            ItemManager.Instance.RemoveItem(itemList);

            if (InventoryCheck(itemList)) //해당 아이템이 존재한다면
            {
                SlotManager.slotDataList[slotIndex].itemList = itemList;
                SlotManager.slotDataList[slotIndex].itemUseType = itemType;
                SlotManager.slotDataList[slotIndex].IsEmpty = false; // 빈 슬롯 됨

            }
            else //존재하지 않는다면
            {
                Destroy(itemUITransform.gameObject); // 해당 자식 객체 삭제

                SlotManager.slotDataList[slotIndex].itemList = ItemList.None;
                SlotManager.slotDataList[slotIndex].itemUseType = ItemUseType.None;
                SlotManager.slotDataList[slotIndex].IsEmpty = true; // 빈 슬롯 됨

                SlotManager.itemSlotIndex.Remove(itemList); // 슬롯 인덱스 해제
            }

        }
    

    }




    private bool InventoryCheck(ItemData.ItemList itemList)
    {
        return ItemManager.Instance.inventoryDictionary.ContainsKey(itemList);
    }
}