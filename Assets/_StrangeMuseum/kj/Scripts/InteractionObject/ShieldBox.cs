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

    //   public NetworkVariable<bool> isBoxUsing = new NetworkVariable<bool>
    //(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //상호작용 오브젝트 레이 충돌 여부

  

    public ItemData.ItemList GetItemList() { return ItemData.ItemList.Box;  }

    public ItemData.ItemUseType GetItemType() { return ItemData.ItemUseType.Self; }


    [SerializeField]
    int itemLayer;


    Slots slots;

    [SerializeField]
    bool isInteract = false;

    public void Interact()
    {
        if(isInteract) { return;  } //이미 해당 아이템을 상호작용 하였는데, 좌클릭을 할 경우 습득하는 경우가 있으므로 방지.

        Slots slots = SecurityInGameUI.Instance.SlotManager;

        for (int i = 0; i < slots.slotDataList.Count; i++)
        {
            if (slots.slotDataList[i].IsEmpty)
            {
                isInteract = true;

                this.GetComponent<NetworkItem>().CmdPickUpItem(this.gameObject);

                //슬롯에 아이템 추가하는 부분 및 슬롯 상태 부분
                slots.slotDataList[i].SlotObj.GetComponent<Slot>().AssignedItem[i] = this.gameObject;

                //UI 표시
                if (ItemManager.Instance.inventoryDictionary.ContainsKey(ItemList.Box) == false) //인벤토리에 박스 아이템이 하나도 없을 떄
                {

                    Instantiate(BoxUI, slots.slotDataList[i].SlotObj.transform, false);

                    itemLayer = i;

                    slots.AddItem(this.gameObject, itemLayer);

                }

                //ItemData에 Add하는 부분
                ItemManager.Instance.AddItem(ItemData.ItemList.Box);
                slots.slotDataList[itemLayer].SlotObj.GetComponent<Slot>().SlotItemCount(ItemData.ItemList.Box);

                break; 
            }
        }

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
