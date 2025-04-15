using Unity.Netcode;
using UnityEngine;
using static ItemData;
using UnityEngine.UIElements;

public class EnergyDrink : NetworkBehaviour, IInteractable, IUsableItem
{
    public GameObject EnergyDrinkUI; //에너지 드링크

    private SecurityInteraction bouncerIntercation;

    public ItemData.ItemList GetItemList()
    {
        return ItemData.ItemList.EnergyDrink;
    }

    public ItemData.ItemUseType GetItemType()
    {
        return ItemData.ItemUseType.Self;
    }

    Slot slot;

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

                if (ItemManager.Instance.inventoryDictionary.ContainsKey(ItemList.EnergyDrink) == false) //인벤토리에 박스 아이템이 하나도 없을 떄
                {
                    Instantiate(EnergyDrinkUI, slots.slotDataList[i].SlotObj.transform, false);

                    itemLayer = i;

                    slots.AddItem(this.gameObject, itemLayer);
                }

                ItemManager.Instance.AddItem(ItemData.ItemList.EnergyDrink);

                slots.slotDataList[itemLayer].SlotObj.GetComponent<Slot>().SlotItemCount(ItemData.ItemList.EnergyDrink);
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

    public NetworkVariable<bool> isEnergyDrinkUsing = new NetworkVariable<bool>
(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //상호작용 오브젝트 레이 충돌 여부


    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsInEnergyDrinkServerRpc(bool value)
    {
        isEnergyDrinkUsing.Value = value;
    }

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

        var allSecurityInteractions = FindObjectsOfType<SecurityInteraction>();

        SecurityInteraction targetBouncer = null;
        foreach (var security in allSecurityInteractions)
        {

            if (security.OwnerClientId == ClientId)  // 해당 클라이언트의 Bouncer인지 확인
            {
                targetBouncer = security;
                break;
            }
        }

        if (targetBouncer != null && isEnergyDrinkUsing.Value == false)
        {
            if (isEnergyDrinkUsing.Value == true)
            {
                Debug.Log("에너지 드링크 기능 적용중");
                return;
            }


            SetIsInEnergyDrinkServerRpc(true);

            targetBouncer.EnergyDrinkFunction(this,EnergyDrinkCooltime, MaxSpeed);

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

        SecurityInGameUI.Instance.OnDestroyItemUI(this.gameObject, itemLayer);


    }

    [ServerRpc(RequireOwnership = false)] ////RPC 호출 시 소유 여부에 관계없이 호출 가능.
    public void ResetEnergyDrinkServerRpc(ulong clientId)
    {
        SetIsInEnergyDrinkServerRpc(false);

        itemLayer = 0;
        bouncerIntercation = null;

        NetworkObjectReference objRef = this.gameObject;

        GetComponent<NetworkItem>().DestroyItem(objRef); // 서버에 아이템 획득 요청
    }
}
