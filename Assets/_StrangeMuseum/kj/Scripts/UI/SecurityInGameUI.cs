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
    [SerializeField]
    GameObject ItemExplainImage;
    [SerializeField]
    GameObject ItemNameImage;
    [SerializeField]
    GameObject BackKeyImage;

    [SerializeField]
    GameObject KeyImages;
    [Header("Slot Management")]
    public Slots SlotManager;

    [Header("UI/Item")]
    [SerializeField]
    private GameObject CurrentItemView; //현재 보고 있는 아이템
    public bool isItemFirstView; // 아이템을 처음으로 봤는지에 대한 여부.

    [SerializeField]
    private bool isItemView; // 아이템을 처음과 상관 없이 보고 있는지에 대한 여부 => ItemExplainImage 활성화를 위함

    [SerializeField]
    public bool isItemExplainView; //아이템 설명 보고 있는지에 대한 여부

  




    private IUsableItem usableItem;

    ItemData.ItemUseType itemType;
    ItemData.ItemList itemList;

    PlayerInteraction Interaction;
    SecurityInteraction bouncerInteraction;


    TestMoveController testMoveController;
    

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

        OnItemViewResetUI();
    }

    private void Start()
    {
      
        InteractionUI.SetActive(false);

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
                testMoveController = bouncer.GetComponent<TestMoveController>();
                break; // 하나만 찾으면 됨
            }
        }

        SlotManager.SlotSet(); //슬롯들 초기화

    }

    private void Update()
    {
        SecurityInteractionUI();

        ItemSlotUpdate();


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

        if (CurrentItemView != null)
        {
            if (isItemFirstView == false)
            {
                if (CurrentItemView.GetComponent<ItemController>().IsItemView)
                {
                    if (testMoveController.GetPlayerState() == TestPlayerState.Run)
                    {
                        OnItemViewCloseUI(CurrentItemView);
                    }
                }

            }
        }


        if (isItemView)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                OnItemExplainUI(CurrentItemView.GetComponent<IUsableItem>().GetItemList());
                isItemView = false; 
            }
        }

        if(isItemExplainView)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CurrentItemView.GetComponent<ItemController>().RotateSpeedSet();

                testMoveController.SetPlayerState(TestPlayerState.Idle);

                OnItemViewCloseUI(CurrentItemView);

                isItemFirstView = false;
                isItemExplainView = false;
            }
        }





        if (Input.GetKeyDown(KeyCode.E) && Interaction.isMissionProgress.Value == false)
        {
            Debug.Log("경비원이 아이템을 사용했을 때 경비원의 id : " + NetworkManager.Singleton.LocalClientId);
            SlotManager.UseSelectedItem();
        }
    }

    private void SecurityInteractionUI()
    {
        if (bouncerInteraction.isInteracted.Value == true)
        {
            if (SlotManager.slotDataList[SlotManager.SelectedIndex].IsEmpty)
            {
                if (bouncerInteraction.RayItem != null)
                {
                    OnInteractionUI(InteractionType.PickUp);

                }
            }
            else //비어  있지 않다면
            {
                if (bouncerInteraction.RayItem != null)
                {
                    OnInteractionUI(InteractionType.PickUp);
                }
            }
        }
        else
        {
            OnInteractionUI(InteractionType.None); //아무것도  안 뜸

        }
    } // 슬롯안에 아이템 여부에 따라 경비원이 아이템을 바라봤을 때

    private void ItemSlotUpdate()
    {
        if (bouncerInteraction.isInteracted.Value == true &&
            SlotManager.slotDataList[SlotManager.SelectedIndex].IsEmpty == false)
        {
            OnItemExplainUI(ItemData.ItemList.None);
            return; //아이템 바라본 상태에서, 슬롯이 비어있지 않을 때 바라본 아이템을 우선으로
        }
        if (SlotManager.slotDataList[SlotManager.SelectedIndex].IsEmpty)
        {
            OnItemExplainUI(ItemData.ItemList.None);
            return;
        }

        switch (itemType)
        {
            case ItemUseType.Self: //자기 자신에게 사용하는 아이템을 든 상태 . 박스, 에너지 드링크, 볼펜
                if (bouncerInteraction.isInteracted.Value == true) //다른 아이템 바라볼 경우
                {
                    OnInteractionUI(InteractionType.PickUp); //픽업 ui 보여주고
                }
                else //그냥 기존 아이템을 상태일 경우
                {
                    OnInteractionUI(InteractionType.Self); //사용법 ui 보여주고
                }
                break;
            case ItemUseType.Target: //상대에게 사용하는 아이템을 든 상태 . 피 묻은 천, 구속구

                if (bouncerInteraction.isInteracted.Value == true) //다른 아이템 바라볼 경우
                {
                    OnInteractionUI(InteractionType.PickUp); //픽업 ui 보여주고
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
                }
                break;
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
                    text.text = " 줍기";
                    break;
                case InteractionType.Self:
                    uiImage.sprite = UsingIcon; // 아이템 줍기 아이콘
                    text.text = ": 자신에게 사용";
                    break;
                case InteractionType.Target:

                    uiImage.sprite = UsingIcon; // 아이템 줍기 아이콘
                    text.text = ": 대상에게 사용";
                    break;
            }
        }



    }

    public void OnItemNameUI(ItemData.ItemList itemKey = ItemData.ItemList.None)
    {
        if (Interaction.isMissionProgress.Value == true)
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
                    itemObjectName.text = value.itemName;
                    break;
                case ItemData.ItemList.EnergyDrink:
                    itemObjectName.text = value.itemName;
                    break;
                case ItemData.ItemList.Box:
                    itemObjectName.text = value.itemName;
                    break;
                case ItemData.ItemList.Cover:
                    itemObjectName.text = value.itemName;
                    break;
                case ItemData.ItemList.Pen:
                    itemObjectName.text = value.itemName;
                    break;
                case ItemData.ItemList.None:
                    itemObjectName.text = " ";
                    break;
            }
        }
        else
        {
            // 아이템이 딕셔너리에 없는 경우
            ItemExplain.text = " ";
        }
    }

    public void OnItemExplainUI(ItemData.ItemList itemKey = ItemData.ItemList.None)
    {
        if(itemKey == ItemList.None)
        {
            return;
        }

        if (Interaction.isMissionProgress.Value == true)
        {
            ItemExplain.text = " ";
            return;
        }

        CurrentItemView.GetComponent<ItemController>().RotateSpeed = 0.0f; //해당 아이템 회전 못하게

        isItemExplainView = true;

        ItemExplainImage.gameObject.SetActive(true);

        KeyImages.gameObject.SetActive(false);

        BackKeyImage.gameObject.SetActive(false);

    
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

    public void OnDestroyItemUI(GameObject item, int slotIndex)
    {

        Transform slotTransform = SlotManager.slotDataList[slotIndex].SlotObj.transform;

        Transform itemUITransform = slotTransform.GetChild(2);

        // 자식 객체가 이미 삭제되었는지 확인
        if (itemUITransform != null && itemUITransform.gameObject != null)
        {
            IUsableItem usableItem = item.GetComponent<IUsableItem>();

            ItemData.ItemList itemList = usableItem.GetItemList(); // 아이템의 종류 확인

            if (InventoryCheck(itemList))
            {
                ItemManager.Instance.RemoveItem(itemList);

                if (InventoryCheck(itemList) == false) //해당 아이템이 하나도 존재하지 않으면
                {
                    Destroy(itemUITransform.gameObject); // 해당 자식 객체 삭제

                    SlotManager.slotDataList[slotIndex].itemList = ItemList.None;
                    SlotManager.slotDataList[slotIndex].itemUseType = ItemUseType.None;
                    SlotManager.slotDataList[slotIndex].IsEmpty = true; // 빈 슬롯 됨
             
                }

              
                SlotManager.slotDataList[slotIndex].SlotObj.GetComponent<Slot>().SlotItemCount(itemList);
            }
            else
            {
                Debug.LogWarning($"딕셔너리에 {itemList} 키가 존재하지 않습니다.");
            }
        }
    

    }

    public void OnItemViewUI(GameObject item)
    {
        CurrentItemView = item; //현재 아이템 저장.

        isItemView = true;

        testMoveController.SetPlayerState(TestPlayerState.Freeze); //행동 정지

        ItemController itemController = CurrentItemView.GetComponent<ItemController>();
        itemController.ViewCreateItem(CurrentItemView); //아이템 이동

        ItemList currentItem = CurrentItemView.GetComponent<IUsableItem>().GetItemList();
        
        OnItemNameUI(currentItem); //아이템 설명

        if (isItemFirstView == false) { BackKeyImage.gameObject.SetActive(true); } //처음 본 아이템이 아니라면 UI표시

        ItemNameImage.gameObject.SetActive(true);
        KeyImages.gameObject.SetActive(true);

       

      
    }

    private void OnItemViewCloseUI(GameObject item = null)
    {
        Debug.Log("아이템 비활성화");

        NetworkObjectReference objRef = item;
        item.GetComponent<NetworkItem>().PickUpItemServerRpc(objRef);

        OnItemViewResetUI();
    }

    private void OnItemViewResetUI()
    {
        

        ItemNameImage.gameObject.SetActive(false);

        ItemExplainImage.gameObject.SetActive(false);

        BackKeyImage.gameObject.SetActive(false);

        KeyImages.gameObject.SetActive(false);

        CurrentItemView = null;

    }

    private bool InventoryCheck(ItemData.ItemList itemList)
    {
        return ItemManager.Instance.inventoryDictionary.ContainsKey(itemList);
    }
}