using Mirror;
using UnityEngine;
using static ItemData;
using UnityEngine.UIElements;


public class Cover : NetworkBehaviour, IInteractable, IUsableItem
{

    public GameObject CoverUI; // 구속구 UI
    private SecurityInteraction bouncerInteraction;
    private StatueInGameUI statueIngameUI;


    [SerializeField]
    private float CoverCooltime;
    [SerializeField]
    private int itemLayer;

    public ItemData.ItemList GetItemList() { return ItemData.ItemList.Cover; }

    public ItemData.ItemUseType GetItemType() { return ItemData.ItemUseType.Target; }

    [SyncVar]
    public bool isCoverUsing;


    public GameObject CoverGameObject;

    Slot slot;
    public void Interact()
    {
        this.gameObject.tag = "Untagged";
        this.gameObject.layer = 0; // Default

        Slots slots = SecurityInGameUI.Instance.SlotManager;

        // 1. 빈 슬롯에 아이템 추가 (처음 얻는 경우)
        for (int i = 0; i < slots.slotDataList.Count; i++)
        {
            if (!slots.slotDataList[i].IsEmpty && slots.slotDataList[i].itemList == ItemData.ItemList.Cover)
            {
                AddItem(slots, i);
                Debug.Log("처음 얻지 않은 아이템");
                break;
            }

            if (slots.slotDataList[i].IsEmpty && slots.slotDataList[i].itemList == ItemData.ItemList.None)
            {
                itemLayer = i; //처음 얻은 아이템에만 적용

                AddItem(slots, i);
                Debug.Log("처음 얻은 아이템");
                return;
            }
        }

        // 3. 처리할 수 있는 슬롯이 없을 경우
        Debug.Log("아이템을 추가할 수 있는 슬롯이 없습니다.");
    }

    private void AddItem(Slots slots,int currentCoverSlot)
    {
        Slot slot = slots.slotDataList[currentCoverSlot].SlotObj.GetComponent<Slot>();

        // 현재 슬롯의 빈 AssignedItem 인덱스를 찾음
        int availableIndex = GetItemEmptyIndex(slot);

        if (availableIndex != -1)
        {        
            this.GetComponent<NetworkItem>().CmdPickUpItem(this.gameObject);

            slot.AssignedItem[availableIndex] = this.gameObject;

            if (ItemManager.Instance.inventoryDictionary.ContainsKey(ItemList.Cover) == false) //인벤토리에 박스 아이템이 하나도 없을 떄
            {
                Instantiate(CoverUI, slot.transform, false);

                slots.AddItem(this.gameObject, currentCoverSlot);
            }

            ItemManager.Instance.AddItem(ItemData.ItemList.Cover);

            slots.SlotItemCount(ItemData.ItemList.Cover, slot);
        }
    }
    public int GetItemEmptyIndex(Slot slot)
    {
        for (int i = 0; i < slot.AssignedItem.Length; i++)
        {
            if (slot.AssignedItem[i] == null)
            {
                return i;
            }
        }
        return -1; // 모든 인덱스가 차있으면 -1
    }

    [Command(requiresAuthority = false)]
    public void UseServerRpc(uint clientId)
    {
        NetworkIdentity bloodCoverIdentity = GetComponent<NetworkIdentity>();

        if (NetworkServer.connections.TryGetValue((int)clientId, out NetworkConnectionToClient connection))
        {
            NetworkIdentity playerNetObj = connection.identity;

            if (playerNetObj == null)
            {
                Debug.LogWarning("클라이언트의 PlayerObject (connection.identity)가 null입니다.");
                return;
            }

            bouncerInteraction = playerNetObj.GetComponent<SecurityInteraction>();

            bouncerInteraction.BloodCoverFunction(playerNetObj, bloodCoverIdentity, itemLayer);

        }
        else
        {
            Debug.LogWarning("이 경비원은 접속하지 않은 유저입니다");
        }

    }


 

    [Command(requiresAuthority = false)]
    public void ResetInteractServerRpc()
    {
        itemLayer = 0;

        bouncerInteraction = null;


        GetComponent<NetworkItem>().DestroyItem(this.gameObject); // 서버에 아이템 삭제 요청
    }

}