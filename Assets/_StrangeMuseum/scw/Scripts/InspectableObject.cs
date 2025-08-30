using System.Collections;
using UnityEngine;
using Mirror;

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


    public void ProceedInspectedTime(uint playerId, Vector3 playerPosition) // 점검 진행도를 증가시키는 함수
    {
        inspectedTime += Time.deltaTime;

        UIManager.Instance.CallGameManagerInspectionGaugeUI(inspectableObjectData.objectName, inspectedTime, inspectableObjectData.inspectionTimeRequired, true);

        if (inspectedTime > inspectableObjectData.inspectionTimeRequired && !isExecutedOneTime) // 점검 진행 시간이 필요 시간보다 커지면(점검이 완료되면) 함수 호출
        {
            // 다회 수행되는 것을 방지하기 위한 bool형 변수
            isExecutedOneTime = true;
            CmdRequestCompleteInspection(playerId, playerPosition);
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
    [Command(requiresAuthority = false)]
    public void CmdRequestCompleteInspection(uint playerId, Vector3 playerPosition)
    {
        Debug.Log("ID: " + playerId + " 클라이언트가 서버에게 임무 완료 Feedback 요청");
        ServerCompleteInspection(playerId, playerPosition);
    }

    [Server]
    public void ServerCompleteInspection(uint playerId, Vector3 targetPos)
    {
        // 서버 오브젝트 상태 먼저 반영
        isInspectionComplete = true;
        inspectedTime = inspectableObjectData.inspectionTimeRequired;

        RpcCompleteInspection(playerId, inspectableObjectData.inspectionTimeRequired, targetPos);
      
        GameManager.Instance.UpdateTaskList(); // 점검표에서 해당 임무 지워질 수 있도록 수정하는 코드
        
        // 모든 임무가 완료되었는지 체크하는 기능 추가 필요
        GameManager.Instance.CheckAllTaskFinish();
    }

    [ClientRpc]
    public void RpcCompleteInspection(ulong playerId, float inspectionTimeRequired, Vector3 targetPos) 
    {
        Debug.Log(inspectableObjectData.objectName + "의 점검이 완료되었습니다.");
        isInspectionComplete = true;
        inspectedTime = inspectionTimeRequired;
        ActivateHorrorEffect(playerId, targetPos);
    }

    private void ActivateHorrorEffect(ulong playerId,Vector3 targetPos) // 각 임무 완료시 특정한 임무 오브젝트에게 일정 확률로 공포적인 이펙트를 주는 함수
    {
        //if (Random.Range(0, 1f) > 0.0f) return; // 확률 삽입

        SetActiveForSelectedChildren(playerId);

        switch (inspectableObjectData.objectName)
        {
            case "모아이 석상":
                direction = targetPos - transform.position;
                direction.y = 0f;
                transform.rotation = Quaternion.LookRotation(direction);
                break;
            case "???":
                transform.GetChild(0).GetChild(0).GetChild(1).GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                break;
            case "우편물":
                direction = targetPos - this.transform.GetChild(2).GetChild(0).transform.position;
                direction.y = 0f;
                this.transform.GetChild(2).GetChild(0).transform.rotation = Quaternion.LookRotation(direction);
                break;
        }
    }

    private void SetActiveForSelectedChildren(ulong playerId) // 입력한 정수번째의 자식 오브젝트를 켜거나 끄는 함수
    {
        UIManager.Instance.BlackOutEffect(playerId); // 블랙아웃 이펙트

        if (audioClip != null)
        {
            StartCoroutine(PlayHorrorSound(1f, playerId));
        }

        this.transform.GetChild(1).gameObject.SetActive(false);
        this.transform.GetChild(2).gameObject.SetActive(true);
    }

    private IEnumerator PlayHorrorSound(float delay, ulong playerId)
    {
        if (NetworkClient.localPlayer != null && NetworkClient.localPlayer.netId == playerId)
        {
            yield return new WaitForSeconds(delay);
            //SoundManager.Instance.PlaySfx(audioClip);
        }
    }

    public string GetTaskDetails() // 스크립터블 오브젝트에 달린 임무 내용 반환
    {
        return inspectableObjectData.taskDetail;
    }
}