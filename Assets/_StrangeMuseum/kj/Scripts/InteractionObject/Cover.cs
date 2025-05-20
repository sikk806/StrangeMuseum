using Mirror;
using UnityEngine;
using static ItemData;
using UnityEngine.UIElements;
using System.Collections;


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

    public int GetItemlayer()
    {
        return itemLayer;
    }
    [SyncVar]
    public bool isCoverUsing;



    [SerializeField]
    private GameObject CoverInStatue;

   
    
    Slot slot;

    public void Interact() //구속구 상호작용 
    {

        CoverInStatue.GetComponent<CoverInStatue>().PlayMoveEffect();

        Slots slots = SecurityInGameUI.Instance.SlotManager;

        ItemData.ItemList itemType = ItemData.ItemList.Cover;

        if (slots.itemSlotIndex.TryGetValue(itemType, out Slot slot))
        {
            Debug.Log("중복 아이템 슬롯 번호" + slot);
            AddItem(slots, slot);
            return;
        }

        // 수갑이 없는 상태이므로 빈 슬롯 탐색
        for (int i = 0; i < slots.slotList.Count; i++)
        {
            var data = slots.slotList[i];

            Debug.Log("처음 습득 한 아이템 슬롯 번호" + i);
            if (data.SlotData.IsEmpty && data.SlotData.itemList == ItemData.ItemList.None)
            {
                slots.itemSlotIndex[itemType] = data; // 슬롯 인덱스 기억
                Debug.Log(" 슬롯 인덱스 기억" + slots.itemSlotIndex[itemType]);
                AddItem(slots, slots.itemSlotIndex[itemType]);
                return;
            }
        }

    }

 
    private void AddItem(Slots slots, Slot ItemSlot)
    {

        itemLayer = ItemSlot.SlotData.OriginalIndex;

        // Slot slot = slots.slotList[itemLayer].GetComponent<Slot>();

        int availableIndex = GetItemEmptyIndex(ItemSlot);

        if (availableIndex != -1)
        {
            this.GetComponent<NetworkItem>().CmdPickUpItem(this.gameObject);

            ItemSlot.AssignedItem[availableIndex] = this.gameObject;
            // UI가 없을 때만 생성
            if (!ItemManager.Instance.inventoryDictionary.ContainsKey(ItemList.Cover))
            {
                Debug.Log("슬롯 오브젝트 이름 2 ---- " + ItemSlot.gameObject);
                Instantiate(CoverUI, ItemSlot.transform, false);
                slots.AddItem(this.gameObject, ItemSlot);
            }

            ItemManager.Instance.AddItem(ItemData.ItemList.Cover);
            ItemSlot.SlotItemCount(ItemData.ItemList.Cover);

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