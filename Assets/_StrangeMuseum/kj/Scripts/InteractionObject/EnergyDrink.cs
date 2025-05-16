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

    public int GetItemlayer()
    {
        return itemLayer;
    }
    Slot slot;

    public void Interact() //구속구 상호작용 
    {
        Slots slots = SecurityInGameUI.Instance.SlotManager;

        ItemData.ItemList itemType = ItemData.ItemList.EnergyDrink;

        if (slots.itemSlotIndex.TryGetValue(itemType, out Slot slot))
        {
            Debug.Log("중복 아이템 슬롯 번호" + slot);
            AddItem(slots, slot);
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
                AddItem(slots, slots.itemSlotIndex[itemType]);
                return;
            }
        }

    }

    private void AddItem(Slots slots, Slot ItemSlot)
    {
        itemLayer = ItemSlot.SlotData.OriginalIndex;

        // Slot slot = slots.slotList[itemLayer].GetComponent<Slot>();

        int availableIndex = GetItemEmptyIndex(ItemSlot);

        if (availableIndex != -1)
        {
            this.GetComponent<NetworkItem>().CmdPickUpItem(this.gameObject);

            ItemSlot.AssignedItem[availableIndex] = this.gameObject;



            // UI가 없을 때만 생성
            if (!ItemManager.Instance.inventoryDictionary.ContainsKey(ItemList.EnergyDrink))
            {
                Debug.Log("슬롯 오브젝트 이름 2 ---- " + ItemSlot.gameObject);
                Instantiate(EnergyDrinkUI, ItemSlot.transform, false);
                slots.AddItem(this.gameObject, ItemSlot);
            }

            ItemManager.Instance.AddItem(ItemData.ItemList.EnergyDrink);
            ItemSlot.SlotItemCount(ItemData.ItemList.EnergyDrink,true);


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
