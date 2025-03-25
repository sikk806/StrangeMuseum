using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using static Define;


public class Box : NetworkBehaviour, IInteractable, IUsableItem
{
    public ItemLayer GetItemLayer()
    {
        return ItemLayer.Self; // 자기 자신에게 사용되는 아이템
    }
    public ItemType GetItemType()
    {
        return ItemType.Box; // 자기 자신에게 사용되는 아이템
    }

    public GameObject BoxUI; //구속구 UI

    private SecurityInteraction bouncerIntercation;

    [SerializeField]
    int itemLayer;

    public void Interact(SecurityInteraction bouncer)
    {
        // if (!IsOwner) return; 


        bouncerIntercation = bouncer.GetComponent<SecurityInteraction>();

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

                if (bouncerIntercation != null)
                {
                    if (bouncerIntercation.isBoxUsing.Value == true) return;

                    GameObject securityBody = bouncerIntercation.transform.GetChild(2).gameObject;
                    if (securityBody != null)
                    {
                        securityBody.SetActive(true);
                    }

                    NetworkObjectReference objRef = this.gameObject;
                    bouncerIntercation.BoxInteracted(objRef);
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

                if (bouncerIntercation != null)
                {
                    GameObject securityBody = bouncerIntercation.transform.GetChild(2).gameObject;

                    if (isActive)
                    {
                        if (SecurityInGameUI.Instance != null)
                        {
                            SecurityInGameUI.Instance.OnDestroyItemUI(itemLayer);
                            SecurityInGameUI.Instance.RemoveItemLayer(itemLayer);
                        }
                    }

                    securityBody.SetActive(isActive);
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ResetInteractServerRpc(ulong clientId)
    {
      
        itemLayer = 0;
        bouncerIntercation = null;

        NetworkObjectReference objRef = this.gameObject;
        GetComponent<NetworkItem>().DestroyItem(objRef);
    }
}
