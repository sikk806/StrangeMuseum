using Unity.Netcode;
using UnityEngine;
using static Define;

public class EnergyDrink : NetworkBehaviour, IInteractable, IUsableItem
{
    public GameObject EnergyDrinkUI; //에너지 드링크

    private SecurityInteraction bouncerIntercation;

    public ItemLayer GetItemLayer()
    {
        return ItemLayer.Self; // 자기 자신에게 사용되는 아이템
    }
    public ItemType GetItemType()
    {
        return ItemType.EnergyDrink; // 자기 자신에게 사용되는 아이템
    }

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

                Instantiate(EnergyDrinkUI, SecurityInGameUI.Instance.SlotData[i].SlotObj.transform, false);

                SecurityInGameUI.Instance.SlotData[i].SlotObj.GetComponent<Slot>().AssignedItem[i] = this.gameObject;

                SecurityInGameUI.Instance.AddItemToSlot(this.gameObject, i);


                SecurityInGameUI.Instance.SlotData[i].IsEmpty = false;

                this.gameObject.SetActive(false);

                break;
            }
        }
    }

    [SerializeField]
    float EnergyDrinkCooltime;

    [SerializeField]
    float MaxSpeed;

    [SerializeField]
    int itemLayer;

    [ServerRpc(RequireOwnership = false)]
    public void UseServerRpc(ulong ClientId)
    {
        if (!IsServer) return;

        Debug.Log($"에너지 드링크 사용 요청 - ClientId: {ClientId}");
        EnergyDrinkInteractedServerRpc(ClientId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void EnergyDrinkInteractedServerRpc(ulong ClientId)
    {

        Debug.Log($"서버에서 에너지 드링크 사용 처리 - ClientId: {ClientId}");

        // Bouncer(보안요원) 캐릭터의 속도 증가 효과 적용

        // 현재 게임에 존재하는 모든 SecurityInteraction(Bouncer) 객체 찾기
        var allSecurityInteractions = FindObjectsOfType<SecurityInteraction>();


            // 해당 ClientId를 소유한 Bouncer 찾기
        SecurityInteraction targetBouncer = null;
        foreach (var security in allSecurityInteractions)
        {

            if (security.OwnerClientId == ClientId)  // 해당 클라이언트의 Bouncer인지 확인
            {
                targetBouncer = security;
                break;
            }
        }

        if (targetBouncer != null && targetBouncer.isEnergyDrinkUsing.Value == false)
        {
            targetBouncer.EnergyDrinkInteracted(this, EnergyDrinkCooltime, MaxSpeed, itemLayer);

            EnergyDrinkInteractedClientRpc(ClientId);
        }
        else
        {
            Debug.LogError("에너지 드링크 사용 중");
        }
    }
    [ClientRpc]
    public void EnergyDrinkInteractedClientRpc(ulong targetClientId)
    {
        Debug.Log("클라이언트에서 에너지 드링크 효과 적용");

        if (NetworkManager.Singleton.LocalClientId != targetClientId)
            return;

        SecurityInGameUI.Instance.OnDestroyItemUI(itemLayer);
        SecurityInGameUI.Instance.RemoveItemLayer(itemLayer);


    }

    [ServerRpc(RequireOwnership = false)] ////RPC 호출 시 소유 여부에 관계없이 호출 가능.
    public void ResetEnergyDrinkServerRpc(ulong clientId)
    {
        itemLayer = 0;
        bouncerIntercation = null;

        NetworkObjectReference objRef = this.gameObject;

        GetComponent<NetworkItem>().DestroyItem(objRef); // 서버에 아이템 획득 요청
    }
}
