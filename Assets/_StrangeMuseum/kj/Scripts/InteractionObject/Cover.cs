using Unity.Netcode;
using UnityEngine;


public class Cover : NetworkBehaviour, IInteractable, IUsableItem
{

    public GameObject CoverUI; // 구속구 UI
    private SecurityInteraction bouncerInteraction;
    private StatueInGameUI statueIngameUI;


    [SerializeField]
    private float CoverCooltime;
    [SerializeField]
    private int itemLayer;

    public ItemData.ItemList GetItemList() { return ItemData.ItemList.Cover; }

    public ItemData.ItemUseType GetItemType() { return ItemData.ItemUseType.Target; }

    public NetworkVariable<bool> isCoverUsing = new NetworkVariable<bool>
(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //상호작용 오브젝트 레이 충돌 여부

    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsCoverServerRpc(bool value)
    {
        isCoverUsing.Value = value;

    }

    [ServerRpc(RequireOwnership = false)]
    public void CoverServerRpc(NetworkObjectReference coverRef)
    {
        if (coverRef.TryGet(out NetworkObject networkObject))
        {
            CoverGameObject = networkObject.gameObject;
            CoverClientRpc(coverRef); // 서버에서 클라이언트로 전달
        }
    }

    [ClientRpc]
    public void CoverClientRpc(NetworkObjectReference boxRef)
    {
        if (!boxRef.TryGet(out NetworkObject networkObject))
        {
            Debug.LogError("Failed to get NetworkObject from boxRef on client.");
            return;
        }

        CoverGameObject = networkObject.gameObject;
    }

    public GameObject CoverGameObject;

    Slot slot;
    public void Interact() // 구속구 상호작용
    {
        this.gameObject.tag = "Untagged";
        this.gameObject.layer = 0; //Defalut

        Slots slots = SecurityInGameUI.Instance.SlotManager;

        for (int i = 0; i < slots.slotDataList.Count; i++)
        {
            if (slots.slotDataList[i].IsEmpty)
            {
                NetworkObjectReference objRef = this.gameObject;

                GetComponent<NetworkItem>().PickUpItemServerRpc(objRef); // 서버에 아이템 획득 요청
                itemLayer = i;

                Instantiate(CoverUI, slots.slotDataList[i].SlotObj.transform, false);


                slots.slotDataList[i].SlotObj.GetComponent<Slot>().AssignedItem[i] = this.gameObject;

                slots.AddItem(this.gameObject, i);


                slots.slotDataList[i].IsEmpty = false;

                ItemManager.Instance.AddItem(ItemData.ItemList.Cover);
                break;
            }
        }

    }


   


    [ServerRpc(RequireOwnership = false)]
    public void UseServerRpc(ulong clientId)
    {
        // Bouncer 리스트 가져오기
        GameObject[] bouncers = GameObject.FindGameObjectsWithTag("Statue");

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
                        // 🚀 ClientRpc 호출
                        CoverActiveClientRpc(true, bouncerInteraction.RayStaute.GetComponent<NetworkObject>().NetworkObjectId);

                        StatueInGameUI.Instance.CoverSet(this.gameObject);

                    }
                    else
                    {
                        Debug.Log("조각상 확인 불가 ");
                    }
                }
                else
                {
                    Debug.Log("에임 미스");
                }

                break;
            }
        }

    }


    [ClientRpc]
    private void CoverActiveClientRpc(bool isActive, ulong statueId)
    {
        if (SecurityInGameUI.Instance != null)
        {
            SecurityInGameUI.Instance.OnDestroyItemUI(this.gameObject, itemLayer);

            StatueInGameUI.Instance.CoverSet(this.gameObject);
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