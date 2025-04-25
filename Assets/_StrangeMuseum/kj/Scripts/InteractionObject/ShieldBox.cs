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

    [SyncVar]
    public NetworkIdentity storedBoxRef;

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

                uint netId = GetComponent<NetworkIdentity>().netId;
                GetComponent<NetworkItem>().CmdPickUpItem(netId);


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


    [Command(requiresAuthority = false)]
    public void UseServerRpc(uint clientId)
    {
        Debug.Log("UseServerRpc 실행 " + (int)clientId);

        if (NetworkServer.connections.ContainsKey((int)clientId))
        {
            Debug.Log("0");

            NetworkConnectionToClient connection = NetworkServer.connections[(int)clientId];
            NetworkIdentity playerNetObj = connection.identity;  // 클라이언트의 PlayerObject

            if (playerNetObj != null)
            {
                bouncerIntercation = playerNetObj.GetComponent<SecurityInteraction>();
                securityDie = playerNetObj.GetComponent<SecurityDie>();

                Debug.Log("1");

                if (bouncerIntercation != null)
                {
                    Debug.Log("2");

                    if (isBoxUsing == true) return;


                    GameObject securityBody = bouncerIntercation.transform.GetChild(2).gameObject;
                    if (securityBody != null)
                    {
                        Debug.Log("3");
                        securityBody.SetActive(true);

                        if (SecurityInGameUI.Instance != null)
                        {
                            SecurityInGameUI.Instance.OnDestroyItemUI(this.gameObject, itemLayer);
                        }
                    }

                    NetworkIdentity objRef = GetComponent<NetworkIdentity>();

                    BoxInteracted(objRef);
                    BoxActiveClientRpc(true, clientId);
                }
            }
        }
    }

    [ClientRpc]
    private void BoxActiveClientRpc(bool isActive, uint clientId)
    {
      //  if (NetworkManager.Singleton.LocalClientId != clientId) return; // 해당 클라이언트에서만 실행

        if (NetworkServer.connections.ContainsKey((int)clientId))
        {
            NetworkConnectionToClient connection = NetworkServer.connections[(int)clientId];
            NetworkIdentity playerNetObj = connection.identity;  // 클라이언트의 PlayerObject

            if (playerNetObj != null)
            {
              

                bouncerIntercation = playerNetObj.GetComponent<SecurityInteraction>();

                securityDie = playerNetObj.GetComponent<SecurityDie>();

                if (bouncerIntercation != null)
                {
                    GameObject securityBody = bouncerIntercation.transform.GetChild(2).gameObject;

                    if (isActive)
                    {
                        if (SecurityInGameUI.Instance != null)
                        {
                            SecurityInGameUI.Instance.OnDestroyItemUI(this.gameObject, itemLayer);
                        }
                    }

                    securityBody.SetActive(isActive);
                }
            }
        }
    }

    public void BoxInteracted(NetworkIdentity box)
    {
        if (isServer == false) { return; }

        isBoxUsing = true; //박스 사용 true

        if (box.TryGetComponent(out NetworkIdentity networkObject))
        {
            BoxServerRpc(networkObject); //박스 객체 저장
            securityDie.BoxSet(networkObject);
        }

    }

    public void NotifyClientBoxRemoved()
    {
        if (isBoxUsing == true)
        {
            Debug.Log("경비원 박스 입고 있었음");

            if (NetworkClient.spawned.TryGetValue(netId,out NetworkIdentity networkObject))
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
    public void BoxServerRpc(NetworkIdentity boxRef)
    {
        if (boxRef == null) return;

        storedBoxRef = boxRef;  // 서버에서 저장
        BoxClientRpc(boxRef.netId);   // netId만 전달
    }

    [ClientRpc]
    public void BoxClientRpc(uint netId)
    {
        if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity boxRef))
        {
            storedBoxRef = boxRef; // 클라이언트에서도 저장
            Debug.Log($"클라이언트에서 받은 박스 오브젝트: {boxRef.name}");
        }
        else
        {
            Debug.LogWarning($"netId {netId}를 가진 오브젝트를 찾을 수 없습니다.");
        }
    }


    [Command(requiresAuthority = false)]
    private void BoxOffServerRpc()
    {
        if (!isServer) return;

        BoxOff(); // 서버에서 BoxOff 처리

        BoxOffClientRpc(); // 클라이언트에게 BoxOff 작업을 전달
    }

    [ClientRpc]
    private void BoxOffClientRpc()
    {
        if (isServer) return;

        BoxOff(); // 클라이언트에서 BoxOff 처리
    }

    private void BoxOff()
    {
        GameObject securityBody = bouncerIntercation.transform.GetChild(2).gameObject;
        securityBody.SetActive(false);
    }

    [Command(requiresAuthority = false)]
    public void ResetInteractServerRpc()
    {
        securityDie = null;
        itemLayer = 0;
        bouncerIntercation = null;

        BoxOffServerRpc();

        GetComponent<NetworkItem>().DestroyItem(this.gameObject);
    }


}
