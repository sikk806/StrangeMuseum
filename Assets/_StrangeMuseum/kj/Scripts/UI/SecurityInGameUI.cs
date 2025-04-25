using System.Collections.Generic;
using TMPro;
using Unity.Burst.CompilerServices;
using Mirror;
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


    [Header("UI References")]
    [SerializeField] 
    private GameObject InteractionUI;
    [SerializeField] 
    private Sprite UsingIcon;
    [SerializeField] 
    private Sprite PickUpIcon;
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
      
        InteractionUI.SetActive(false);
        ulong id = netId;

        Debug.Log($" NetId : {netId}");
       

        // 모든 경비원 오브젝트를 찾고, 로컬 클라이언트 ID와 비교하여 해당 경비원의 Interaction을 가져옴
        GameObject[] bouncers = GameObject.FindGameObjectsWithTag("Bouncer");

        foreach (var bouncer in bouncers)
        {

            // 각 경비원에 대해 로컬 클라이언트 ID가 일치하는지 확인
            if (bouncer.GetComponent<PlayerLobbyController>().ConnectionID == (int)id)
            {
                // 일치하는 경비원 찾으면 그 경비원의 Interaction 객체를 할당
                //if (GameManager.Instance.PlayerStat.Value[OwnerClientId] == "Statue")
                //{
                //    Destroy(this.gameObject);
                //}

                playerId = (uint)bouncer.GetComponent<PlayerLobbyController>().ConnectionID;

                securityInteraction = bouncer.GetComponent<SecurityInteraction>();
                Interaction = bouncer.GetComponent<PlayerInteraction>();
                break; // 하나만 찾으면 됨
            }
        }

        SlotManager.SlotSet(); //슬롯들 초기화

    }

    uint playerId;

    private void Update() 
    {
        SecurityInteractionUI();

        //ItemSlotUpdate();


        if (Input.GetKeyDown(KeyCode.I))
        {
            if(ItemManager.Instance.inventoryDictionary.Count > 0)
            {
                foreach (var item in ItemManager.Instance.inventoryDictionary)
                {
                    int itemCount = item.Value;
                    ItemData.ItemList items = item.Key;

                    Debug.Log("아이템 : " + items + " 갯수 : " + itemCount);

                }
            }
        }


        if (Input.GetKeyDown(KeyCode.E) && Interaction.isMissionProgress == false)
        {       
            SlotManager.UseSelectedItem(playerId);
        }
    }

    private void SecurityInteractionUI()
    {
        //1. 비어 있을 때 와 비어 있지 않을 때 
        // 2. 아이템을 쳐다 볼 때와 쳐다보지 않을 때 

        if (securityInteraction.isInteracted == true) //아이템 쳐다 볼 때
        {
            if(securityInteraction.SaveRayItem != null)
            {
                ItemList currentItem = SlotManager.slotDataList[SlotManager.SelectedIndex].itemList;
                ItemUseType currentItemType = SlotManager.slotDataList[SlotManager.SelectedIndex].itemUseType;

                if (SlotManager.slotDataList[SlotManager.SelectedIndex].IsEmpty == false) //슬롯에 뭔가 있음
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
                if(SlotManager.slotDataList[SlotManager.SelectedIndex].IsEmpty == false) //슬롯에 뭔가 있음
                {
                    ItemList currentItem = SlotManager.slotDataList[SlotManager.SelectedIndex].itemList;
                    ItemUseType currentItemType = SlotManager.slotDataList[SlotManager.SelectedIndex].itemUseType;

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
          
        }

    }  // 슬롯안에 아이템 여부에 따라 경비원이 아이템을 바라봤을 때

    private void ItemSlotUpdate(ItemUseType itemType)
    {
        switch (itemType)
        {
            case ItemUseType.Self: //자기 자신에게 사용하는 아이템을 든 상태 . 박스, 에너지 드링크, 볼펜
                if (securityInteraction.isInteracted == true) //다른 아이템 바라볼 경우
                {
                    OnInteractionUI(InteractionType.PickUp); //픽업 ui 보여주고
                }
                else //그냥 기존 아이템을 상태일 경우
                {
                    OnInteractionUI(InteractionType.Self); 
                }
                break;
            case ItemUseType.Target: //상대에게 사용하는 아이템을 든 상태 . 피 묻은 천, 구속구

                if (securityInteraction.isInteracted == true) //다른 아이템 바라볼 경우
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
                    text.text = " 줍기";
                    break;
                case InteractionType.Self:
                    uiImage.sprite = UsingIcon; // 아이템 줍기 아이콘
                    text.text = "자신에게 사용";
                    break;
                case InteractionType.Target:

                    uiImage.sprite = UsingIcon; // 아이템 줍기 아이콘
                    text.text = "대상에게 사용";
                    break;
            }
        }



    }

    public void OnItemNameUI(ItemData.ItemList itemKey = ItemData.ItemList.None)
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

    public void OnItemExplainUI(ItemData.ItemList itemKey = ItemData.ItemList.None)
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

        Transform itemUITransform = slotTransform.GetChild(2);

        // 자식 객체가 이미 삭제되었는지 확인
        if (itemUITransform != null && itemUITransform.gameObject != null)
        {
            IUsableItem usableItem = item.GetComponent<IUsableItem>();

            ItemData.ItemList itemList = usableItem.GetItemList(); // 아이템의 종류 확인
            ItemData.ItemUseType itemType = usableItem.GetItemType(); // 아이템의 종류 확인

            ItemManager.Instance.RemoveItem(itemList);

            if (InventoryCheck(itemList)) //해당 아이템이 하나도 존재하지 않으면
            {
                SlotManager.slotDataList[slotIndex].itemList = itemList;
                SlotManager.slotDataList[slotIndex].itemUseType = itemType;
                SlotManager.slotDataList[slotIndex].IsEmpty = false; // 빈 슬롯 됨

            }
            else
            {
                Destroy(itemUITransform.gameObject); // 해당 자식 객체 삭제

                SlotManager.slotDataList[slotIndex].itemList = ItemList.None;
                SlotManager.slotDataList[slotIndex].itemUseType = ItemUseType.None;
                SlotManager.slotDataList[slotIndex].IsEmpty = true; // 빈 슬롯 됨
            }

            SlotManager.slotDataList[slotIndex].SlotObj.GetComponent<Slot>().SlotItemCount(itemList);
        }
    

    }




    private bool InventoryCheck(ItemData.ItemList itemList)
    {
        return ItemManager.Instance.inventoryDictionary.ContainsKey(itemList);
    }
}