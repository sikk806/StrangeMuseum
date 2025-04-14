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
    [SerializeField] private GameObject InteractionUI;
    [SerializeField] private Sprite UsingIcon;
    [SerializeField] private Sprite PickUpIcon;
    [SerializeField] private TextMeshProUGUI ItemExplain;
    public TextMeshProUGUI itemObjectName;

    [Header("Slot Management")]
    public Slots SlotManager;


    private IUsableItem usableItem;

    ItemData.ItemUseType itemType;
    ItemData.ItemList itemList;

    PlayerInteraction Interaction;
    SecurityInteraction bouncerInteraction;

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

        SlotManager.SlotSet(); //슬롯들 초기화

    }

    private void Update()
    {
        SecurityInteractionUI();

        ItemSlotUpdate();


        if (Input.GetKeyDown(KeyCode.I))
        {
            Debug.Log("아이템 itemDictionary 에 저장된 현재 갯수" + ItemManager.Instance.inventoryDictionary.Count);
        }

        if (Input.GetKeyDown(KeyCode.E) && Interaction.isMissionProgress.Value == false)
        {
            Debug.Log("경비원이 아이템을 사용했을 때 경비원의 id : " + NetworkManager.Singleton.LocalClientId);
            SlotManager.UseSelectedItem();
        }

        // 숫자 키(1~4)로 아이템 선택

        for (int i = 0; i < SlotManager.slotDataList.Count; i++)
        {
            if (Input.GetKeyDown((KeyCode)(49 + i))) // KeyCode.Alpha1 == 49
            {
                OnItemNameUI(ItemList.None); //아이템 이름 지웠다가 다시 업데이트
                OnInteractionUI(InteractionType.None); //아이템 이름 지웠다가 다시 업데이트

                SlotManager.SelectSlot(i);
            }
            else
            {
                SlotManager.slotDataList[SlotManager.SelectedIndex].SlotObj.GetComponent<Slot>().SlotSelectImage();
            }
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
            if (SlotManager.slotDataList[SlotManager.SelectedIndex].IsEmpty) //슬롯이 비어 있을 때
            {
                OnInteractionUI(InteractionType.None); //아무것도  안 뜸

                OnItemNameUI();
            }
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

        //Self 타입인지 Target 타입인지 아이템 사용 방식 가져옴
        itemType = SlotManager.slotDataList[SlotManager.SelectedIndex].itemUseType;
        itemList = SlotManager.slotDataList[SlotManager.SelectedIndex].itemList;

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

        Transform slotTransform = SlotManager.slotDataList[slotIndex].SlotObj.transform;

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

                SlotManager.slotDataList[slotIndex].IsEmpty = true; // 빈 슬롯 됨
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