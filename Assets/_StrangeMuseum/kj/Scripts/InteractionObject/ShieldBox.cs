using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;
using static ItemData;

public class ShieldBox : NetworkBehaviour, IInteractable, IUsableItem
{
    public GameObject BoxUI; //구속구 UI

    private SecurityInteraction bouncerIntercation;
    private SecurityDie securityDie;

    [SerializeField]
    private float boxInvincibilityTime = 2.0f; //박스 사용 후 무적 시간

    [SyncVar]
    public bool isBoxUsing;

    public ItemData.ItemList GetItemList() { return ItemData.ItemList.Box;  }

    public ItemData.ItemUseType GetItemType() { return ItemData.ItemUseType.Self; }

    public int GetItemlayer()
    {
        return itemLayer;
    }

    [SerializeField]
    int itemLayer;

    Slots slots;

    public void Interact() //구속구 상호작용 
    {
        Slots slots = SecurityInGameUI.Instance.SlotManager;

        ItemData.ItemList itemType = ItemData.ItemList.Box;

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
            if (!ItemManager.Instance.inventoryDictionary.ContainsKey(ItemList.Box))
            {
                Debug.Log("슬롯 오브젝트 이름 2 ---- " + ItemSlot.gameObject);
                Instantiate(BoxUI, ItemSlot.transform, false);
                slots.AddItem(this.gameObject, ItemSlot);
            }

            ItemManager.Instance.AddItem(ItemData.ItemList.Box);
            ItemSlot.SlotItemCount(ItemData.ItemList.Box);

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


    void Update()
    {
       
        if (Input.GetKeyDown(KeyCode.F1)) // 예를 들어 F1 키를 눌렀을 때
        {
            Debug.Log("현재 서버에 연결된 클라이언트들:");

            foreach (var connection in NetworkServer.connections)
            {
                // connection.Key는 클라이언트의 ID, connection.Value는 NetworkConnectionToClient
                Debug.Log($"Client ID: {connection.Key}, Connection Info: {connection.Value}");
                RpcSendConnectionInfo(connection.Key);
            }
        }

    }

    [ClientRpc]
    private void RpcSendConnectionInfo(int clientId)
    {
        Debug.Log("클라이언트로 전달된 서버 연결 정보: " + clientId);
    }

    [Command(requiresAuthority = false)]
    public void UseServerRpc(uint clientId)
    {
        Debug.Log("박스를 사용한 경비원의 아이디는 " + clientId + " 입니다");

        NetworkIdentity boxNetworkIdentity = GetComponent<NetworkIdentity>();

        if (NetworkServer.connections.TryGetValue((int)clientId, out NetworkConnectionToClient connection))
        {
            NetworkIdentity playerNetObj = connection.identity;

            if (playerNetObj == null)
            {
                Debug.LogWarning("클라이언트의 PlayerObject (connection.identity)가 null입니다.");
                return;
            }

            bouncerIntercation = playerNetObj.GetComponent<SecurityInteraction>();
            securityDie = playerNetObj.GetComponent<SecurityDie>();

            isBoxUsing = true;

            bouncerIntercation.BoxFunction(playerNetObj, boxNetworkIdentity, itemLayer);

            securityDie.BoxSet(boxNetworkIdentity);

         
        }
        else
        {
            Debug.LogWarning("이 경비원은 접속하지 않은 유저입니다");
        }
    }


    public void NotifyClientBoxRemoved()
    {
        if (isBoxUsing == true)
        {
            Debug.Log("경비원 박스 입고 있었음");

            if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity networkObject))
            {
                Debug.Log("결국 경비원 박스 벗음");
                StartCoroutine(DelayBoxing()); //무적 시간

                ResetInteractServerRpc(); //박스 기능 초기화(박스 벗겨짐)

                return;
            }
        }

        if (isBoxUsing == false)
        {
            if (isOwned)
            {
                Debug.Log("IsOwner 이고, 경비원 박스 입지 않으므로 경비원 죽음"); //여기까지는 호출 잘 됨
                GetComponent<SecurityDie>().SecurityDieServerRpc(); //죽음 기능 다른 스크립트에서 처리 하고 나서 ㄱㄱ
            }

        }
    }

    IEnumerator DelayBoxing()
    {
        yield return new WaitForSeconds(boxInvincibilityTime);
        isBoxUsing = false;
    }


    [Command(requiresAuthority = false)]
    public void ResetInteractServerRpc()
    {
        securityDie = null;
        itemLayer = 0;
        bouncerIntercation = null;

        GetComponent<NetworkItem>().DestroyItem(this.gameObject);
    }


}
