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
            if (slots.slotDataList[i].IsEmpty)
            {
                NetworkObjectReference objRef = this.gameObject;

                GetComponent<NetworkItem>().PickUpItemServerRpc(objRef); // 서버에 아이템 획득했다고 정보 알림

                slots.slotDataList[i].SlotObj.GetComponent<Slot>().AssignedItem[i] = this.gameObject;

                if (ItemManager.Instance.inventoryDictionary.ContainsKey(ItemList.Pen) == false) //인벤토리에 박스 아이템이 하나도 없을 떄
                {
                    Instantiate(PenDrinkUI, slots.slotDataList[i].SlotObj.transform, false);

                    itemLayer = i;

                    slots.AddItem(this.gameObject, itemLayer);
                }

                ItemManager.Instance.AddItem(ItemData.ItemList.Pen);


                slots.slotDataList[itemLayer].SlotObj.GetComponent<Slot>().SlotItemCount(ItemData.ItemList.Pen);



                break;
            }
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void UseServerRpc(ulong ClientId)
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
    public void PenInteractedClientRpc(ulong targetClientId)
    {


        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;


        SecurityInGameUI.Instance.OnDestroyItemUI(this.gameObject, itemLayer);


    }

    [ServerRpc(RequireOwnership = false)] ////RPC 호출 시 소유 여부에 관계없이 호출 가능.
    public void ResetPenServerRpc()
    {
        itemLayer = 0;


        NetworkObjectReference objRef = this.gameObject;

        GetComponent<NetworkItem>().DestroyItem(objRef); // 서버에 아이템 획득 요청
    }
}
