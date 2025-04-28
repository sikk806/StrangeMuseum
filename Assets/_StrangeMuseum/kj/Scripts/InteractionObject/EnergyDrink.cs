using Mirror;
using UnityEngine;
using static ItemData;
using UnityEngine.UIElements;

public class EnergyDrink : NetworkBehaviour, IInteractable, IUsableItem
{
    public GameObject EnergyDrinkUI; //에너지 드링크

    private SecurityInteraction bouncerIntercation;

    [SerializeField]
    float EnergyDrinkCooltime;

    [SerializeField]
    float MaxSpeed;

    [SerializeField]
    int itemLayer;

    [SyncVar]
    public bool isEnergyDrinkUsing;

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
                this.GetComponent<NetworkItem>().CmdPickUpItem(this.gameObject);

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
 

    [Command(requiresAuthority = false)]
    public void UseServerRpc(uint ClientId)
    {
        if (!isServer) return;

        Debug.Log($"에너지 드링크 사용 요청 - ClientId: {ClientId}");
        EnergyDrinkInteractedServerRpc(ClientId);
    }

    [Command(requiresAuthority = false)]
    public void EnergyDrinkInteractedServerRpc(uint ClientId)
    {

        Debug.Log($"서버에서 에너지 드링크 사용 처리 - ClientId: {ClientId}");

        var allSecurityInteractions = FindObjectsOfType<SecurityInteraction>();

        SecurityInteraction targetBouncer = null;

        foreach (var security in allSecurityInteractions)
        {

            if (NetworkServer.connections.ContainsKey((int)ClientId))
            {
                targetBouncer = security;
                break;
            }
        }

        if (targetBouncer != null && isEnergyDrinkUsing == false)
        {
            if (isEnergyDrinkUsing == true)
            {
                Debug.Log("에너지 드링크 기능 적용중");
                return;
            }


            isEnergyDrinkUsing = true;

            targetBouncer.EnergyDrinkFunction(this, EnergyDrinkCooltime, MaxSpeed);

            EnergyDrinkInteractedClientRpc(ClientId);
        }
        else
        {
            Debug.LogError("에너지 드링크 사용 중");
        }
    }
    [ClientRpc]
    public void EnergyDrinkInteractedClientRpc(uint targetClientId)
    {

        if (!NetworkServer.connections.ContainsKey((int)targetClientId))
        {
            Debug.Log("클라이언트 ID 에너지 드링크 부분 맞지 않음");
            return;
        }

         SecurityInGameUI.Instance.OnDestroyItemUI(this.gameObject, itemLayer);


    }

    [Command(requiresAuthority = false)]
    public void ResetEnergyDrinkServerRpc()
    {
        isEnergyDrinkUsing = false;

        itemLayer = 0;
        bouncerIntercation = null;

        GetComponent<NetworkItem>().DestroyItem(this.gameObject); // 서버에 아이템 획득 요청
    }
}
