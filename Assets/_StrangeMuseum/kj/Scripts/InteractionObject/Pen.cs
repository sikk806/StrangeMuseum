using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static Define;

public class Pen : NetworkBehaviour, IInteractable, IUsableItem
{
    public GameObject PenDrinkUI; //에너지 드링크

    private SecurityInteraction bouncerIntercation;

    public ItemUseType GetItemLayer()
    {
        return ItemUseType.Self; 
    }
    public ItemList GetItemType()
    {
        return ItemList.Pen; // 자기 자신에게 사용되는 아이템
    }

    [SerializeField]
    int itemLayer;


    public void Interact(SecurityInteraction bouncer) //에너지 드링크 상호작용 
    {

        bouncerIntercation = bouncer.GetComponent<SecurityInteraction>();

        for (int i = 0; i < SecurityInGameUI.Instance.SlotData.Count; i++)
        {
            if (SecurityInGameUI.Instance.SlotData[i].IsEmpty)
            {
                NetworkObjectReference objRef = this.gameObject;

                GetComponent<NetworkItem>().PickUpItemServerRpc(objRef); // 서버에 아이템 획득했다고 정보 알림

                itemLayer = i;

                Instantiate(PenDrinkUI, SecurityInGameUI.Instance.SlotData[i].SlotObj.transform, false);

                SecurityInGameUI.Instance.SlotData[i].SlotObj.GetComponent<Slot>().AssignedItem[i] = this.gameObject;

                SecurityInGameUI.Instance.AddItemToSlot(this.gameObject, i);


                SecurityInGameUI.Instance.SlotData[i].IsEmpty = false;

                this.gameObject.SetActive(false);

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


        SecurityInGameUI.Instance.OnDestroyItemUI(itemLayer);
        SecurityInGameUI.Instance.RemoveItemLayer(itemLayer);

      
    }

    [ServerRpc(RequireOwnership = false)] ////RPC 호출 시 소유 여부에 관계없이 호출 가능.
    public void ResetPenServerRpc()
    {
        itemLayer = 0;

        GetComponent<NetworkItem>().isPickedUp = false;

        NetworkObjectReference objRef = this.gameObject;

        GetComponent<NetworkItem>().DestroyItem(objRef); // 서버에 아이템 획득 요청
    }
}
