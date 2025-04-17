using System.Collections;
using Unity.Netcode;
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

    public NetworkVariable<bool> isBoxUsing = new NetworkVariable<bool>
 (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //상호작용 오브젝트 레이 충돌 여부


    public NetworkObjectReference storedBoxRef;

    public ItemData.ItemList GetItemList() { return ItemData.ItemList.Box;  }

    public ItemData.ItemUseType GetItemType() { return ItemData.ItemUseType.Self; }


    [SerializeField]
    int itemLayer;


    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsBoxServerRpc(bool value)
    {
        isBoxUsing.Value = value;
    }

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

                //슬롯에 아이템 추가하는 부분 및 슬롯 상태 부분
                slots.slotDataList[i].SlotObj.GetComponent<Slot>().AssignedItem[i] = this.gameObject;

                //UI 표시
                if (ItemManager.Instance.inventoryDictionary.ContainsKey(ItemList.Box) == false) //인벤토리에 박스 아이템이 하나도 없을 떄
                {
                    SecurityInGameUI.Instance.isItemFirstView = true;

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


    public void ItemView(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
        {
            NetworkObject playerNetObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if (playerNetObj != null)
            {
                if (SecurityInGameUI.Instance != null)
                {
                    SecurityInGameUI.Instance.OnItemViewUI(this.gameObject);
                }

            }
        }
    }

 
    [ServerRpc(RequireOwnership = false)]
    public void UseServerRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
        {
            NetworkObject playerNetObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
            if (playerNetObj != null)
            {
                bouncerIntercation = playerNetObj.GetComponent<SecurityInteraction>();
                securityDie = playerNetObj.GetComponent<SecurityDie>();

                if (bouncerIntercation != null)
                {
                    if (isBoxUsing.Value == true) return;

                    GameObject securityBody = bouncerIntercation.transform.GetChild(2).gameObject;
                    if (securityBody != null)
                    {
                        securityBody.SetActive(true);
                    }

                    NetworkObjectReference objRef = this.gameObject;

                    BoxInteracted(objRef);
                    BoxActiveClientRpc(true, clientId);
                }
            }
        }
    }

    [ClientRpc]
    private void BoxActiveClientRpc(bool isActive, ulong clientId)
    {
      //  if (NetworkManager.Singleton.LocalClientId != clientId) return; // 해당 클라이언트에서만 실행

        if (NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
        {
            NetworkObject playerNetObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
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

    public void BoxInteracted(GameObject box)
    {
        if (IsServer == false) { return; }

        SetIsBoxServerRpc(true); //박스 사용 true

        if (box.TryGetComponent(out NetworkObject networkObject))
        {
            BoxServerRpc(networkObject); //박스 객체 저장
            securityDie.BoxSet(networkObject);
        }

    }

    public void NotifyClientBoxRemoved()
    {
        if (isBoxUsing.Value == true)
        {
            Debug.Log("경비원 박스 입고 있었음");

            if (storedBoxRef.TryGet(out NetworkObject networkObject))
            {
                Debug.Log("결국 경비원 박스 벗음");
                StartCoroutine(DelayBoxing()); //무적 시간

                ResetInteractServerRpc(NetworkManager.Singleton.LocalClientId); //박스 기능 초기화(박스 벗겨짐)

                return;
            }
        }

        if (isBoxUsing.Value == false)
        {
            if (IsOwner)
            {
                Debug.Log("IsOwner 이고, 경비원 박스 입지 않으므로 경비원 죽음"); //여기까지는 호출 잘 됨
                GetComponent<SecurityDie>().SecurityDieServerRpc(); //죽음 기능 다른 스크립트에서 처리 하고 나서 ㄱㄱ
            }

        }
    }

    IEnumerator DelayBoxing()
    {
        yield return new WaitForSeconds(boxInvincibilityTime);
        SetIsBoxServerRpc(false);
    }


    [ServerRpc(RequireOwnership = false)]
    public void BoxServerRpc(NetworkObjectReference boxRef)
    {
        if (boxRef.TryGet(out NetworkObject networkObject))
        {
            storedBoxRef = boxRef; // 원본을 저장하지 않고, 네트워크 참조를 저장
            BoxClientRpc(boxRef);  // 서버에서 클라이언트로 전달
        }
    }

    [ClientRpc]
    public void BoxClientRpc(NetworkObjectReference boxRef)
    {
        storedBoxRef = boxRef; // 클라이언트도 네트워크 참조를 저장

        if (!boxRef.TryGet(out NetworkObject networkObject))
        {
            return;
        }
    }

 
    [ServerRpc(RequireOwnership = false)]
    private void BoxOffServerRpc()
    {
        if (!IsServer) return;

        BoxOff(); // 서버에서 BoxOff 처리

        BoxOffClientRpc(); // 클라이언트에게 BoxOff 작업을 전달
    }

    [ClientRpc]
    private void BoxOffClientRpc()
    {
        if (IsServer) return;

        BoxOff(); // 클라이언트에서 BoxOff 처리
    }

    private void BoxOff()
    {
        GameObject securityBody = bouncerIntercation.transform.GetChild(2).gameObject;
        securityBody.SetActive(false);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetInteractServerRpc(ulong clientId)
    {
        securityDie = null;
        itemLayer = 0;
        bouncerIntercation = null;

        BoxOffServerRpc();

        NetworkObjectReference objRef = this.gameObject;
        GetComponent<NetworkItem>().DestroyItem(objRef);
    }


}
