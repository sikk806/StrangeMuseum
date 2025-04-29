using TMPro;
using Unity.Netcode;
using UnityEngine;
using static ItemData;
using UnityEngine.UIElements;

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

    private SecurityInteraction bouncerIntercation;

    public NetworkVariable<bool> isHandCuffUsing = new NetworkVariable<bool>
(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //상호작용 오브젝트 레이 충돌 여부

    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsHandCuffServerRpc(bool value)
    {
        isHandCuffUsing.Value = value;
    }


    public ItemData.ItemList GetItemList() { return ItemData.ItemList.HandCuff; }
   
    public ItemData.ItemUseType GetItemType() { return ItemData.ItemUseType.Target; }

    Slot slot;

    public void Interact() //구속구 상호작용 
    {
        Slots slots = SecurityInGameUI.Instance.SlotManager;

        for (int i = 0; i < slots.slotDataList.Count; i++)
        {
            if (slots.slotDataList[i].IsEmpty)
            {
                itemLayer = i; //빈 슬롯 넘버 저장
            }

            Slot slot = slots.slotDataList[i].SlotObj.GetComponent<Slot>();

            // 현재 슬롯의 빈 AssignedItem 인덱스를 찾음
            int availableIndex = GetItemEmptyIndex(slot);

            if(availableIndex != -1)
            {
                this.GetComponent<NetworkItem>().CmdPickUpItem(this.gameObject);

                slot.AssignedItem[availableIndex] = this.gameObject;

                //UI 표시
                if (ItemManager.Instance.inventoryDictionary.ContainsKey(ItemList.HandCuff) == false) //인벤토리에 박스 아이템이 하나도 없을 떄
                {
                    Instantiate(HandcuffUI, slot.transform, false);

                    slots.AddItem(this.gameObject, itemLayer);
                }

                ItemManager.Instance.AddItem(ItemData.ItemList.HandCuff);
                slots.slotDataList[itemLayer].SlotObj.GetComponent<Slot>().SlotItemCount(ItemData.ItemList.HandCuff);

                break;
            }

         
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

    [ServerRpc(RequireOwnership = false)]
    public void UseServerRpc(uint id)
    {
        if (IsServer == false) { return; }

        Debug.Log("서버에서 구속구 기능 호출");
        HandCuffInteractedServerRpc(id);


    }

    [ServerRpc(RequireOwnership = false)]
    public void HandCuffInteractedServerRpc(uint ClientId)
    {
        //Debug.Log("서버에서 구속구 사용 처리");
        //GameObject[] bouncers = GameObject.FindGameObjectsWithTag("Bouncer");

        //// 아이템 사용한 경비원 찾기
        //foreach (var bouncer in bouncers)
        //{
        //    NetworkObject netObj = bouncer.GetComponent<NetworkObject>();
        //    if (netObj != null && netObj.OwnerClientId == ClientId)
        //    {
        //        Debug.Log(netObj.OwnerClientId);
        //        bouncerIntercation = bouncer.GetComponent<SecurityInteraction>();

        //        if (bouncerIntercation.IsStatue.Value)
        //        {
        //            Debug.Log("조각상 확인");
        //            if (bouncerIntercation.SaveRayStaute != null)
        //            {
        //                Debug.Log("조각상 CoverInteracted 호출 ");

        //                if(isHandCuffUsing.Value == false)
        //                {
        //                    bouncerIntercation.SaveRayStaute.GetComponent<StatueInteraction>().HandCuffInteracted(this, minMoveSpeed, minRushSpeed, handCuffCooltime, ClientId);
        //                    bouncerIntercation.SaveRayStaute.GetComponent<StatueInteraction>().PlayFearSound(HandCuffFearSound);
        //                    HandActiveClientRpc(ClientId);
        //                }

                        
        //            }
        //            else
        //            {
        //                Debug.Log("조각상 확인 불가 ");
        //            }
        //        }


        //        break;
        //    }
        //}

    }

    [ClientRpc]
    public void HandActiveClientRpc(ulong ClientId)
    {
  

        if (NetworkManager.Singleton.LocalClientId != ClientId)
            return;

        Debug.Log("클라이언트에서 아이템 UI 제거");
        SecurityInGameUI.Instance.OnDestroyItemUI(this.gameObject,itemLayer);
    }



    [ServerRpc(RequireOwnership = false)] //RPC 호출 시 소유 여부에 관계없이 호출 가능.
    public void ResetInteractServerRpc(ulong ClientId)
    {
        Debug.Log("서버에서 구속구 리셋");

        itemLayer = 0;

        bouncerIntercation = null;

        NetworkObjectReference objRef = this.gameObject;

       // GetComponent<NetworkItem>().DestroyItem(objRef); // 서버에 아이템 획득 요청
    }

}
