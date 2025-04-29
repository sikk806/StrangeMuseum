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

    [SerializeField]
    bool isInteract = false;

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

    public void Interact()
    {

        Slots slots = SecurityInGameUI.Instance.SlotManager;

        for (int i = 0; i < slots.slotDataList.Count; i++)
        {
            // 슬롯 참조
            if (slots.slotDataList[i].IsEmpty)
            {
                itemLayer = i; //빈 슬롯 넘버 저장
            }
            
            Slot slot = slots.slotDataList[i].SlotObj.GetComponent<Slot>();

            // 현재 슬롯의 빈 AssignedItem 인덱스를 찾음
            int availableIndex = GetItemEmptyIndex(slot);

            if (availableIndex != -1)
            {
                this.GetComponent<NetworkItem>().CmdPickUpItem(this.gameObject);

                slot.AssignedItem[availableIndex] = this.gameObject;

                if (ItemManager.Instance.inventoryDictionary.ContainsKey(ItemList.EnergyDrink) == false)
                {
                    Instantiate(EnergyDrinkUI, slot.transform, false);

                    slots.AddItem(this.gameObject, itemLayer);
                }

                ItemManager.Instance.AddItem(ItemData.ItemList.EnergyDrink);
                slot.SlotItemCount(ItemData.ItemList.EnergyDrink);

                break;
            }
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
        NetworkIdentity energyDrinkIdentity = GetComponent<NetworkIdentity>();

        if (NetworkServer.connections.TryGetValue((int)clientId, out NetworkConnectionToClient connection))
        {
            NetworkIdentity playerNetObj = connection.identity;

            if (playerNetObj == null)
            {
                Debug.LogWarning("클라이언트의 PlayerObject (connection.identity)가 null입니다.");
                return;
            }

            bouncerIntercation = playerNetObj.GetComponent<SecurityInteraction>();

            isEnergyDrinkUsing = true;

            bouncerIntercation.EnergyDrinkFunction(playerNetObj, energyDrinkIdentity, itemLayer,EnergyDrinkCooltime, MaxSpeed);

        }
        else
        {
            Debug.LogWarning("이 경비원은 접속하지 않은 유저입니다");
        }
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
