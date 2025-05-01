using TMPro;
using UnityEngine;
using static ItemData;
using UnityEngine.UIElements;
using Mirror;

public class HandCuff : NetworkBehaviour, IInteractable, IUsableItem //구속구
{
    //1. 조각상 이동속도 일정시간 동안 낮추기 
    //2. 

    [SerializeField]
    float handCuffCooltime;

    [SerializeField]
    float minMoveSpeed;

    [SerializeField]
    float minRushSpeed;

    [SerializeField]
    int itemLayer;


    [SerializeField]
    private AudioClip HandCuffFearSound; // 구속구 공포 효과음

    public GameObject HandcuffUI; //구속구 UI

    private SecurityInteraction bouncerInteraction;

    [SyncVar]
    public bool isHandCuffUsing;



    public ItemData.ItemList GetItemList() { return ItemData.ItemList.HandCuff; }
   
    public ItemData.ItemUseType GetItemType() { return ItemData.ItemUseType.Target; }

    Slot slot;

    public void Interact() //구속구 상호작용 
    {
        Slots slots = SecurityInGameUI.Instance.SlotManager;

        for (int i = 0; i < slots.slotDataList.Count; i++)
        {
            if (!slots.slotDataList[i].IsEmpty && slots.slotDataList[i].itemList == ItemData.ItemList.HandCuff)
            {
                AddItem(slots, i);
                Debug.Log("처음 얻지 않은 아이템");
                break;
            }

            if (slots.slotDataList[i].IsEmpty && slots.slotDataList[i].itemList == ItemData.ItemList.None)
            {
                itemLayer = i;
                AddItem(slots, i);
                Debug.Log("처음 얻은 아이템");
                return;
            }
        }
       
    }

    private void AddItem(Slots slots, int currentHandCuffSlot)
    {
        Slot slot = slots.slotDataList[currentHandCuffSlot].SlotObj.GetComponent<Slot>();

        // 현재 슬롯의 빈 AssignedItem 인덱스를 찾음
        int availableIndex = GetItemEmptyIndex(slot);

        if (availableIndex != -1)
        {
            this.GetComponent<NetworkItem>().CmdPickUpItem(this.gameObject);

            slot.AssignedItem[availableIndex] = this.gameObject;

            //UI 표시
            if (ItemManager.Instance.inventoryDictionary.ContainsKey(ItemList.HandCuff) == false) //인벤토리에 박스 아이템이 하나도 없을 떄
            {
                Instantiate(HandcuffUI, slot.transform, false);

                slots.AddItem(this.gameObject, currentHandCuffSlot);
            }

            ItemManager.Instance.AddItem(ItemData.ItemList.HandCuff);

            slots.SlotItemCount(ItemData.ItemList.HandCuff, slot);

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
        NetworkIdentity handCuffIdentity = GetComponent<NetworkIdentity>();

        if (NetworkServer.connections.TryGetValue((int)clientId, out NetworkConnectionToClient connection))
        {
            NetworkIdentity playerNetObj = connection.identity;

            if (playerNetObj == null)
            {
                Debug.LogWarning("클라이언트의 PlayerObject (connection.identity)가 null입니다.");
                return;
            }

            bouncerInteraction = playerNetObj.GetComponent<SecurityInteraction>();

            bouncerInteraction.HandCuffFunction(playerNetObj, handCuffIdentity, itemLayer,minMoveSpeed,minRushSpeed,handCuffCooltime);

        }
        else
        {
            Debug.LogWarning("이 경비원은 접속하지 않은 유저입니다");
        }


    }


    [Command(requiresAuthority = false)]
    public void ResetInteractServerRpc()
    {
        Debug.Log("서버에서 구속구 리셋");

        itemLayer = 0;

        bouncerInteraction = null;



       GetComponent<NetworkItem>().DestroyItem(this.gameObject); // 서버에 아이템 획득 요청
    }

}
