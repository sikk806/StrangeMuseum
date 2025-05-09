using TMPro;
using UnityEngine;
using static ItemData;
using UnityEngine.UIElements;
using Mirror;

public class HandCuff : NetworkBehaviour, IInteractable, IUsableItem //구속구
{
    //1. 조각상 이동속도 일정시간 동안 낮추기 
    //2. 

    [SerializeField]
    float handCuffCooltime;
    [SerializeField]
    float minMoveSpeed;
    [SerializeField]
    float minRushSpeed;
    [SerializeField]
    int itemLayer = -1;

    [SerializeField]
    private AudioClip HandCuffFearSound; // 구속구 공포 효과음

    public GameObject HandcuffUI; //구속구 UI

    private SecurityInteraction bouncerInteraction;

    [SyncVar]
    public bool isHandCuffUsing;



    public ItemData.ItemList GetItemList() { return ItemData.ItemList.HandCuff; }
   
    public ItemData.ItemUseType GetItemType() { return ItemData.ItemUseType.Target; }

    Slot slot;

    public void Interact() //구속구 상호작용 
    {
        Slots slots = SecurityInGameUI.Instance.SlotManager;

        ItemData.ItemList itemType = ItemData.ItemList.HandCuff;

        if (slots.itemSlotIndex.TryGetValue(itemType, out Slot slot))
        {
            Debug.Log("중복 아이템 슬롯 번호" + slot);
            AddItem(slots, slot, itemLayer);
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
                AddItem(slots, slots.itemSlotIndex[itemType] , i);
                return;
            }
        }

    }

    private void AddItem(Slots slots, Slot ItemSlot , int itemLayer)
    {

        this.itemLayer = itemLayer;

       // Slot slot = slots.slotList[itemLayer].GetComponent<Slot>();

        int availableIndex = GetItemEmptyIndex(ItemSlot);

        if (availableIndex != -1)
        {
            this.GetComponent<NetworkItem>().CmdPickUpItem(this.gameObject);

            ItemSlot.AssignedItem[availableIndex] = this.gameObject;

            // UI가 없을 때만 생성
            if (!ItemManager.Instance.inventoryDictionary.ContainsKey(ItemList.HandCuff))
            {
                Debug.Log("슬롯 오브젝트 이름 2 ---- " + ItemSlot.gameObject);
                Instantiate(HandcuffUI, ItemSlot.transform, false);
                slots.AddItem(this.gameObject, ItemSlot);
            }

            ItemManager.Instance.AddItem(ItemData.ItemList.HandCuff);
            ItemSlot.SlotItemCount(ItemData.ItemList.HandCuff);

           
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
        NetworkIdentity handCuffIdentity = GetComponent<NetworkIdentity>();

        if (NetworkServer.connections.TryGetValue((int)clientId, out NetworkConnectionToClient connection))
        {
            NetworkIdentity playerNetObj = connection.identity;

            if (playerNetObj == null)
            {
                Debug.LogWarning("클라이언트의 PlayerObject (connection.identity)가 null입니다.");
                return;
            }

            bouncerInteraction = playerNetObj.GetComponent<SecurityInteraction>();

            bouncerInteraction.HandCuffFunction(playerNetObj, handCuffIdentity, itemLayer,minMoveSpeed,minRushSpeed,handCuffCooltime);

        }
        else
        {
            Debug.LogWarning("이 경비원은 접속하지 않은 유저입니다");
        }


    }


    [Command(requiresAuthority = false)]
    public void ResetInteractServerRpc()
    {
        Debug.Log("서버에서 구속구 리셋");

        itemLayer = 0;

        bouncerInteraction = null;



       GetComponent<NetworkItem>().DestroyItem(this.gameObject); // 서버에 아이템 획득 요청
    }

}
