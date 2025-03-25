using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInteraction : NetworkBehaviour
{
    /*
     * 이 스크립트는 경비원과 오브젝트간 상호작용에 대한 기능을 담은 스크립트입니다.
     */


    public NetworkVariable<bool> isMissionProgress = new NetworkVariable<bool>
        (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //미션 진행 중인지?(진행률을 보여주고 있는지?)


    private SecurityController playerController;
    private float interactionDistance = 1.5f; // 플레이어 눈(카메라)으로부터 상호작용이 가능한 거리

    void Awake()
    {
        playerController = GetComponent<SecurityController>();
    }

    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsProgressServerRpc(bool value) //네트워크 bool 변수 여부 설정 메서드
    {
        isMissionProgress.Value = value;
    }

    void Update()
    {
        if(!IsOwner) return;
        if(!playerController.playerCamera) return;

        RaycastHit hit;
        if (Physics.Raycast(playerController.playerCamera.position, playerController.playerCamera.forward, out hit, interactionDistance))
        {
            if (hit.collider.CompareTag("InspectableObject"))
            {
                HandleInspectableObject(hit.collider.gameObject);

                SetIsProgressServerRpc(true); // 미션 진행중

            }
            else
            {

                UIManager.Instance.CloseInspectionObjectUI();


            }
        }
        else
        {
            UIManager.Instance.CloseInspectionObjectUI();

            SetIsProgressServerRpc(false); // 미션 진행 X
        }
    }

    private void HandleInspectableObject(GameObject obj) // 공통 임무를 진행하면서 상호작용하는 오브젝트(ex. 정수기, 장식품 등)와의 기능을 담은 함수
    {
        InspectableObject inspectableObject = obj.GetComponent<InspectableObject>();

        inspectableObject.RedrawGaugeUI(); // 진행률 UI 그리기


        if (inspectableObject.GetIsInspectionComplete()) return; // 점검이 이미 완료되었다면 return

        if (Input.GetMouseButtonDown(0))
        {
            //GameManager.Instance.SetCanPlayerMove(false); // 플레이어 움직임 lock
        }

        if (Input.GetMouseButton(0)) // 유지
        {
            Vector3 playerPosition = transform.position;
            ulong myId = NetworkManager.Singleton.LocalClientId;
            inspectableObject.ProceedInspectedTime(myId, playerPosition);
        }

        if (Input.GetMouseButtonUp(0))
        {
            //GameManager.Instance.SetCanPlayerMove(true); // 플레이어 움직임 lock 해제
        }
    }
}