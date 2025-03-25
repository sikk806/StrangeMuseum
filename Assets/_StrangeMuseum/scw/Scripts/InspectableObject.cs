using NUnit.Framework;
using System.Collections;
using System.Drawing;
using Unity.Netcode;
using UnityEngine;

public class InspectableObject : NetworkBehaviour
{
    /*
     * 이 스크립트는 공통 임무를 진행하면서 상호작용하는 오브젝트(ex. 정수기, 장식품 등)에 적용되는 스크립트입니다.
     */
    
    public InspectableObjectData inspectableObjectData;

    [SerializeField]
    private AudioClip audioClip;

    private float inspectedTime = 0; // 점검을 진행한 시간
    private bool isInspectionComplete = false; // 오브젝트 점검이 완료되었는지 체크하는 bool형 변수
    private bool isExecutedOneTime = false;
    private GameObject player;
    Vector3 direction;


    public void ProceedInspectedTime(ulong playerId, Vector3 playerPosition) // 점검 진행도를 증가시키는 함수
    {
        inspectedTime += Time.deltaTime;

        UIManager.Instance.CallGameManagerInspectionGaugeUI(inspectableObjectData.objectName, inspectedTime, inspectableObjectData.inspectionTimeRequired, true);

        if (inspectedTime > inspectableObjectData.inspectionTimeRequired && !isExecutedOneTime) // 점검 진행 시간이 필요 시간보다 커지면(점검이 완료되면) 함수 호출
        {
            // 다회 수행되는 것을 방지하기 위한 bool형 변수
            isExecutedOneTime = true;
            RequestCompleteInspectionServerRpc(playerId, playerPosition);
        }
    }

    public void RedrawGaugeUI() // GameManager에 있는 UI 그리는 함수 호출
    {
        UIManager.Instance.CallGameManagerInspectionGaugeUI(inspectableObjectData.objectName, inspectedTime, inspectableObjectData.inspectionTimeRequired, false);
    }

    public bool GetIsInspectionComplete() // 점검 완료 유무를 반환하는 함수
    { 
        return isInspectionComplete; 
    }

    // 클라이언트가 서버에게 임무 완료 Feedback을 요청하는 ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void RequestCompleteInspectionServerRpc(ulong playerId, Vector3 playerPosition, ServerRpcParams rpcParams = default)
    {
        Debug.Log("ID: " + playerId + " 클라이언트가 서버에게 임무 완료 Feedback 요청");
        CompleteInspectionServerRpc(playerId, playerPosition);
    }

    [ServerRpc]
    public void CompleteInspectionServerRpc(ulong playerId, Vector3 targetPos)
    {
        CompleteInspectionClientRpc(playerId, inspectableObjectData.inspectionTimeRequired, targetPos);
        GameManager.Instance.UpdateTaskListServerRpc(); // 점검표에서 해당 임무 지워질 수 있도록 수정하는 코드

        // 모든 임무가 완료되었는지 체크하는 기능 추가 필요
        GameManager.Instance.CheckAllTaskFinish();
    }

    [ClientRpc]
    public void CompleteInspectionClientRpc(ulong playerId, float inspectionTimeRequired, Vector3 targetPos) 
    {
        Debug.Log(inspectableObjectData.objectName + "의 점검이 완료되었습니다.");
        isInspectionComplete = true;
        inspectedTime = inspectionTimeRequired;
        ActivateHorrorEffect(playerId, targetPos);
    }

    private void ActivateHorrorEffect(ulong playerId,Vector3 targetPos) // 각 임무 완료시 특정한 임무 오브젝트에게 일정 확률로 공포적인 이펙트를 주는 함수
    {
        //if (Random.Range(0, 1f) > 0.0f) return; // 확률 삽입

        switch (inspectableObjectData.objectName)
        {
            case "디플로도쿠스 화석":
                SetActiveForSelectedChildren(playerId, 2, 0);
                break;
            case "대왕 딱정벌레":
                SetActiveForSelectedChildren(playerId, 0, -1);
                break;
            case "모아이 석상":
                SetActiveForSelectedChildren(playerId, 2, -1);
                direction = targetPos - transform.position;
                direction.y = 0f;
                transform.rotation = Quaternion.LookRotation(direction);
                break;
            case "소녀 미라":
                SetActiveForSelectedChildren(playerId, 1, 0);
                break;
            case "이집트 석관":
                SetActiveForSelectedChildren(playerId, 1, 0);
                break;
            case "미라":
                SetActiveForSelectedChildren(playerId, 2, 1);
                break;
            case "동양풍 거울":
                SetActiveForSelectedChildren(playerId, 6, -1);
                break;
            case "흉상":
                SetActiveForSelectedChildren(playerId, 6, -1);
                break;
            case "???":
                SetActiveForSelectedChildren(playerId, 1, -1);
                transform.GetChild(0).GetChild(1).GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                break;
            case "안내판":
                SetActiveForSelectedChildren(playerId, 0, -1);
                break;
            case "우편물":
                SetActiveForSelectedChildren(playerId, 1, 0);
                direction = targetPos - this.transform.GetChild(1).GetChild(0).transform.position;
                direction.y = 0f;
                this.transform.GetChild(1).GetChild(0).transform.rotation = Quaternion.LookRotation(direction);
                break;
            case "맨홀":
                SetActiveForSelectedChildren(playerId, 0, -1);
                break;
            case "중세 롱소드":
                SetActiveForSelectedChildren(playerId, 0, -1);
                break;
            case "그림":
                SetActiveForSelectedChildren(playerId, 2, 1);
                break;
            case "쓰레기통":
                SetActiveForSelectedChildren(playerId, 2, 0);
                break;
            case "정수기":
                SetActiveForSelectedChildren(playerId, 3, -1);
                break;
        }
    }

    private void SetActiveForSelectedChildren(ulong playerId, int trueIndex, int falseIndex) // 입력한 정수번째의 자식 오브젝트를 켜거나 끄는 함수
    {
        UIManager.Instance.BlackOutEffect(playerId); // 블랙아웃 이펙트

        if (audioClip != null)
        {
            StartCoroutine(PlayHorrorSound(1f, playerId));
        }

        if (trueIndex != -1) this.transform.GetChild(trueIndex).gameObject.SetActive(true);
        if (falseIndex != -1) this.transform.GetChild(falseIndex).gameObject.SetActive(false);
    }

    private IEnumerator PlayHorrorSound(float delay, ulong playerId)
    {
        if (NetworkManager.Singleton.LocalClientId == playerId)
        {
            yield return new WaitForSeconds(delay);
            SoundManager.Instance.PlaySfx(audioClip);
        }
    }

    public string GetTaskDetails() // 스크립터블 오브젝트에 달린 임무 내용 반환
    {
        return inspectableObjectData.taskDetail;
    }
}