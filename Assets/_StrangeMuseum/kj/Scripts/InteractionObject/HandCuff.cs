using TMPro;
using Unity.Netcode;
using UnityEngine;
using static Define;

public class HandCuff : NetworkBehaviour, IInteractable, IUsableItem //구속구
{
    //1. 조각상 이동속도 일정시간 동안 낮추기 
    //2. 


    public GameObject HandcuffUI; //구속구 UI

    private SecurityInteraction bouncerIntercation;



    public ItemLayer GetItemLayer()
    {
        return ItemLayer.Target; // 자기 자신에게 사용되는 아이템
    }
    public ItemType GetItemType()
    {
        return ItemType.HandCuff; // 자기 자신에게 사용되는 아이템
    }
    public void Interact(SecurityInteraction bouncer) //구속구 상호작용 
    {
       
        bouncerIntercation = bouncer.GetComponent<SecurityInteraction>();

        for (int i = 0; i < SecurityInGameUI.Instance.SlotData.Count; i++)
        {
            if (SecurityInGameUI.Instance.SlotData[i].IsEmpty)
            {
                NetworkObjectReference objRef = this.gameObject;

                GetComponent<NetworkItem>().PickUpItemServerRpc(objRef); // 서버에 아이템 획득했다고 정보 알림

                itemLayer = i;

                Instantiate(HandcuffUI, SecurityInGameUI.Instance.SlotData[i].SlotObj.transform, false);

                SecurityInGameUI.Instance.SlotData[i].IsEmpty = false;


                SecurityInGameUI.Instance.SlotData[i].SlotObj.GetComponent<Slot>().AssignedItem[i] = this.gameObject;

                SecurityInGameUI.Instance.AddItemToSlot(this.gameObject, i);

                break;
            }
        }
       
    }

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


    [ServerRpc(RequireOwnership = false)]
    public void UseServerRpc(ulong id)
    {
        if (IsServer == false) { return; }

        Debug.Log("서버에서 구속구 기능 호출");
        HandCuffInteractedServerRpc(id);


    }

    [ServerRpc(RequireOwnership = false)]
    public void HandCuffInteractedServerRpc(ulong ClientId)
    {
        Debug.Log("서버에서 구속구 사용 처리");
        GameObject[] bouncers = GameObject.FindGameObjectsWithTag("Bouncer");

        // 아이템 사용한 경비원 찾기
        foreach (var bouncer in bouncers)
        {
            NetworkObject netObj = bouncer.GetComponent<NetworkObject>();
            if (netObj != null && netObj.OwnerClientId == ClientId)
            {
                Debug.Log(netObj.OwnerClientId);
                bouncerIntercation = bouncer.GetComponent<SecurityInteraction>();

                if (bouncerIntercation.IsStatue.Value)
                {
                    Debug.Log("조각상 확인");
                    if (bouncerIntercation.RayStaute != null)
                    {
                        Debug.Log("조각상 CoverInteracted 호출 ");

                        if(bouncerIntercation.RayStaute.GetComponent<StatueInteraction>().isHandCuffUsing.Value == false)
                        {
                            bouncerIntercation.RayStaute.GetComponent<StatueInteraction>().HandCuffInteracted(this, minMoveSpeed, minRushSpeed, handCuffCooltime, ClientId);
                            bouncerIntercation.RayStaute.GetComponent<StatueInteraction>().PlayFearSound(HandCuffFearSound);
                            HandActiveClientRpc(ClientId);
                        }

                        
                    }
                    else
                    {
                        Debug.Log("조각상 확인 불가 ");
                    }
                }


                break;
            }
        }

    }

    [ClientRpc]
    public void HandActiveClientRpc(ulong ClientId)
    {
  

        if (NetworkManager.Singleton.LocalClientId != ClientId)
            return;

        Debug.Log("클라이언트에서 아이템 UI 제거");
        SecurityInGameUI.Instance.OnDestroyItemUI(itemLayer);
        SecurityInGameUI.Instance.RemoveItemLayer(itemLayer);
    }



    [ServerRpc(RequireOwnership = false)] //RPC 호출 시 소유 여부에 관계없이 호출 가능.
    public void ResetInteractServerRpc(ulong ClientId)
    {
        Debug.Log("서버에서 구속구 리셋");

        itemLayer = 0;

        bouncerIntercation = null;

        NetworkObjectReference objRef = this.gameObject;

        GetComponent<NetworkItem>().DestroyItem(objRef); // 서버에 아이템 획득 요청
    }

}
