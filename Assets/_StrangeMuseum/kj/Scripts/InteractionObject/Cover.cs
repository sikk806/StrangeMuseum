using Unity.Netcode;
using UnityEngine;
using static Define;


public class Cover : NetworkBehaviour, IInteractable, IUsableItem
{

    public GameObject CoverUI; // 구속구 UI
    private SecurityInteraction bouncerInteraction;

    [SerializeField]
    private float CoverCooltime;
    [SerializeField]
    private int itemLayer;

    public ItemUseType GetItemLayer() => ItemUseType.Target;
    public ItemList GetItemType() => ItemList.Cover;


    public void Interact(SecurityInteraction bouncer) // 구속구 상호작용
    {
        this.gameObject.tag = "Untagged";
        this.gameObject.layer = 0; //Defalut

        bouncerInteraction = bouncer.GetComponent<SecurityInteraction>();

        for (int i = 0; i < SecurityInGameUI.Instance.SlotData.Count; i++)
        {
            if (SecurityInGameUI.Instance.SlotData[i].IsEmpty)
            {

                //Transform cover = transform.GetChild(2);
                NetworkObjectReference objRef = this.gameObject;

                GetComponent<NetworkItem>().PickUpItemServerRpc(objRef); // 서버에 아이템 획득 요청
                itemLayer = i;

                Instantiate(CoverUI, SecurityInGameUI.Instance.SlotData[i].SlotObj.transform, false);

                SecurityInGameUI.Instance.SlotData[i].IsEmpty = false;


                SecurityInGameUI.Instance.SlotData[i].SlotObj.GetComponent<Slot>().AssignedItem[i] = this.gameObject;

                SecurityInGameUI.Instance.AddItemToSlot(this.gameObject, i);

                break;
            }
        }

    }


   


    [ServerRpc(RequireOwnership = false)]
    public void UseServerRpc(ulong clientId)
    {
        // Bouncer 리스트 가져오기
        GameObject[] bouncers = GameObject.FindGameObjectsWithTag("Bouncer");

        // 아이템 사용한 경비원 찾기
        foreach (var bouncer in bouncers)
        {
            NetworkObject netObj = bouncer.GetComponent<NetworkObject>();
            if (netObj != null && netObj.OwnerClientId == clientId)
            {
                Debug.Log(netObj.OwnerClientId);
                bouncerInteraction = bouncer.GetComponent<SecurityInteraction>();

                if (bouncerInteraction.IsStatue.Value)
                {
                    Debug.Log("조각상 확인");
                    if (bouncerInteraction.RayStaute != null)
                    {
                        Debug.Log("조각상 CoverInteracted 호출 ");
                        bouncerInteraction.RayStaute.GetComponent<StatueInteraction>().CoverInteracted(true, this.gameObject);
                       
                        // 🚀 ClientRpc 호출
                        CoverActiveClientRpc(true, bouncerInteraction.RayStaute.GetComponent<NetworkObject>().NetworkObjectId);
                    }
                    else
                    {
                        Debug.Log("조각상 확인 불가 ");
                    }
                }

               
                break;
            }
        }

        // 조각상 감지 로직
     

    }


    [ClientRpc]
    private void CoverActiveClientRpc(bool isActive, ulong statueId)
    {
        if (SecurityInGameUI.Instance != null)
        {
            SecurityInGameUI.Instance.OnDestroyItemUI(itemLayer);
            SecurityInGameUI.Instance.RemoveItemLayer(itemLayer);
        }

    }


    [ServerRpc(RequireOwnership = false)]
    public void ResetInteractServerRpc(ulong statueId)
    {
        Debug.Log("리셋 커버");

        itemLayer = 0;

        bouncerInteraction = null;

        CoverActiveClientRpc(false, statueId); // 모든 Statue 비활성화


        NetworkObjectReference objRef = this.gameObject;

        GetComponent<NetworkItem>().DestroyItem(objRef); // 서버에 아이템 삭제 요청
    }

}