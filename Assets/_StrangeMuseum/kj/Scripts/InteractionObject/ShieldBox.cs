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

    public ItemData.ItemList GetItemList() { return ItemData.ItemList.Box;  }

    public ItemData.ItemUseType GetItemType() { return ItemData.ItemUseType.Self; }


    [SerializeField]
    int itemLayer;

    public void Interact()
    {
        // if (!IsOwner) return; 

        for (int i = 0; i < SecurityInGameUI.Instance.SlotData.Count; i++)
        {
            if (SecurityInGameUI.Instance.SlotData[i].IsEmpty)
            {
                NetworkObjectReference objRef = this.gameObject;

                GetComponent<NetworkItem>().PickUpItemServerRpc(objRef); // 서버에 아이템 획득했다고 정보 알림

                itemLayer = i;

                Instantiate(BoxUI, SecurityInGameUI.Instance.SlotData[i].SlotObj.transform, false);

                SecurityInGameUI.Instance.SlotData[i].SlotObj.GetComponent<Slot>().AssignedItem[i] = this.gameObject;

                SecurityInGameUI.Instance.AddItemToSlot(this.gameObject, i);


                SecurityInGameUI.Instance.SlotData[i].IsEmpty = false;

                ItemManager.Instance.AddItem(ItemData.ItemList.Box);

                break;
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
            securityDie.BoxFunction(networkObject);
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
    [SerializeField]
    private float boxInvincibilityTime = 2.0f; //박스 사용 후 무적 시간

    public NetworkVariable<bool> isBoxUsing = new NetworkVariable<bool>
 (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //상호작용 오브젝트 레이 충돌 여부


    public NetworkObjectReference storedBoxRef;

    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsBoxServerRpc(bool value)
    {
        isBoxUsing.Value = value;
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
