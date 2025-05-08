using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static ItemData;
using UnityEngine.UIElements;

public class Pen : NetworkBehaviour, IInteractable, IUsableItem
{
    public GameObject PenUI; //에너지 드링크

    private SecurityInteraction bouncerIntercation;

    [SerializeField]
    int itemLayer;

    Slot slot;

    public ItemData.ItemList GetItemList()
    {
        return ItemData.ItemList.Pen;
    }

    public ItemData.ItemUseType GetItemType()
    {
        return ItemData.ItemUseType.Self;
    }

    public void Interact() //구속구 상호작용 
    {
        Slots slots = SecurityInGameUI.Instance.SlotManager;

        ItemData.ItemList itemType = ItemData.ItemList.Pen;

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
                AddItem(slots, slots.itemSlotIndex[itemType], i);
                return;
            }
        }

    }

    private void AddItem(Slots slots, Slot ItemSlot, int itemLayer)
    {

        this.itemLayer = itemLayer;

        // Slot slot = slots.slotList[itemLayer].GetComponent<Slot>();

        int availableIndex = GetItemEmptyIndex(ItemSlot);

        if (availableIndex != -1)
        {
            this.GetComponent<NetworkItem>().CmdPickUpItem(this.gameObject);

            ItemSlot.AssignedItem[availableIndex] = this.gameObject;



            // UI가 없을 때만 생성
            if (!ItemManager.Instance.inventoryDictionary.ContainsKey(ItemList.Pen))
            {
                Debug.Log("슬롯 오브젝트 이름 2 ---- " + ItemSlot.gameObject);
                Instantiate(PenUI, ItemSlot.transform, false);
                slots.AddItem(this.gameObject, ItemSlot);
            }

            ItemManager.Instance.AddItem(ItemData.ItemList.Pen);
            ItemSlot.SlotItemCount(ItemData.ItemList.Pen);


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


    [ServerRpc(RequireOwnership = false)]
    public void UseServerRpc(uint ClientId)
    {
        // Server-side action
        ProjectileLancuher securityLancuher = GameObject.FindGameObjectWithTag("Bouncer").GetComponent<ProjectileLancuher>();

        if (securityLancuher == null)
        {
            return;
        }

        securityLancuher.Attack(this);

        PenInteractedClientRpc(ClientId);

        ResetPenServerRpc();

    }

    [ClientRpc]
    public void PenInteractedClientRpc(uint targetClientId)
    {


        //if (NetworkManager.Singleton.LocalClientId != targetClientId)
        //    return;


        SecurityInGameUI.Instance.OnDestroyItemUI(this.gameObject, itemLayer);


    }

    [ServerRpc(RequireOwnership = false)] ////RPC 호출 시 소유 여부에 관계없이 호출 가능.
    public void ResetPenServerRpc()
    {
        itemLayer = 0;


        NetworkObjectReference objRef = this.gameObject;

      //  GetComponent<NetworkItem>().DestroyItem(objRef); // 서버에 아이템 획득 요청
    }
}
