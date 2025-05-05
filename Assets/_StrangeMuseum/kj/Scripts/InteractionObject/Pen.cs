using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static ItemData;
using UnityEngine.UIElements;

public class Pen : NetworkBehaviour, IInteractable, IUsableItem
{
    public GameObject PenDrinkUI; //에너지 드링크

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

    public void Interact() //에너지 드링크 상호작용 
    {
        Slots slots = SecurityInGameUI.Instance.SlotManager;

        for (int i = 0; i < slots.slotDataList.Count; i++)
        {
            if (!slots.slotDataList[i].IsEmpty && slots.slotDataList[i].itemList == ItemData.ItemList.Pen)
            {
                AddItem(slots, i);
                Debug.Log("처음 얻지 않은 아이템");
                break;
            }

            if (slots.slotDataList[i].IsEmpty && slots.slotDataList[i].itemList == ItemData.ItemList.None)
            {

                AddItem(slots, i);
                Debug.Log("처음 얻은 아이템");
                return;
            }
        }
    }
    private void AddItem(Slots slots, int currentPenSlot)
    {
        itemLayer = currentPenSlot; //처음 얻은 아이템에만 적용

        Slot slot = slots.slotDataList[currentPenSlot].SlotObj.GetComponent<Slot>();

        // 현재 슬롯의 빈 AssignedItem 인덱스를 찾음
        int availableIndex = GetItemEmptyIndex(slot);

        if (availableIndex != -1)
        {
            this.GetComponent<NetworkItem>().CmdPickUpItem(this.gameObject);

            slot.AssignedItem[availableIndex] = this.gameObject;

            if (ItemManager.Instance.inventoryDictionary.ContainsKey(ItemList.Pen) == false) //인벤토리에 박스 아이템이 하나도 없을 떄
            {
                Instantiate(PenDrinkUI, slot.transform, false);

                slots.AddItem(this.gameObject, currentPenSlot);
            }

            ItemManager.Instance.AddItem(ItemData.ItemList.Pen);

            slots.SlotItemCount(ItemData.ItemList.Pen, slot);

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
