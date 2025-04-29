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
