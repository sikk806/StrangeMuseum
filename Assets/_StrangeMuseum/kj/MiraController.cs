using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using static MiraController;
using static UnityEditor.VersionControl.Message;

public class MiraController : NetworkBehaviour
{
    public enum CopyState 
    {
        Idle,
        Walk,
        Stop,
        Follow,
        Die
    }


    public List<Transform> WayPoint;

    public NavMeshAgent agent;

    private int nextIndex;

    [SerializeField]
    bool isPath = false;

    public bool isDie;

    [SyncVar]
    [SerializeField]
    CopyState copystate;

    GameObject targetSecurity;

    [SerializeField]
    LayerMask securityLayer;

    [SerializeField]
    float sphereRadius = 1.5f; // 반경 조절 가능


    [SerializeField]
    Material outLine;


    public CopyState State
    {
        get
        {
            return copystate;
        }

        set
        {
            copystate = value;

            switch (copystate)
            {
                case CopyState.Idle:
                    break;
                case CopyState.Walk:
                    isPath = true;
                    break;
                case CopyState.Stop:
                    agent.isStopped = true;
                    break;
                case CopyState.Follow:
                    break;
                case CopyState.Die:
                    agent.ResetPath();
                    agent.isStopped = true;
                    agent.enabled = false;
                    OutlineRemoveMaterial(targetSecurity);
                    break;

            }
        }
    }
    void Start()
    {
      
        agent = GetComponent<NavMeshAgent>();
        agent.autoBraking = false; //균일한 속도로 이동
        

        var group = GameObject.Find("AIPatrolPoint");

        if (group != null)
        {
            WayPoint = group.GetComponentsInChildren<Transform>().ToList();
            WayPoint.RemoveAt(0);

            copystate = CopyState.Idle;
        }
    }

    // Update is called once per frame
    void Update()
    {

        switch (copystate)
        {
            case CopyState.Idle:
                if (!isIdleRunning)
                    StartCoroutine(UpdateIdle());
                break;
            case CopyState.Walk:
                UpdateWalk();
                break;
            case CopyState.Stop:
                agent.isStopped = true;
                break;
            case CopyState.Follow:
                if (copystate != CopyState.Die) // Die 상태일 경우 실행하지 않음
                    UpdateFollow();
                break;
            //case CopyState.Die:         
            //    if(isDie == false)
            //    {
            //        Debug.Log(" 마테리얼 제거 ");
            //        OutlineRemoveMaterial(targetSecurity);
            //        isDie = true;
            //        //StartCoroutine(DelayDestroy());
            //    }
            //    break;
        }
    }

    [Command(requiresAuthority = false)]
    public void CmdRequestDestroy()
    {
        NetworkServer.Destroy(this.gameObject);
        StartCoroutine(DelayDestroy());
    }
    IEnumerator DelayDestroy()
    {
        yield return null; // 다음 프레임까지 기다림
        ResourceManager.Instance.Destroy(this.gameObject);
    }

    private bool isIdleRunning = false;
    IEnumerator UpdateIdle()
    {
        if (isIdleRunning) yield break; // 중복 실행 방지
        isIdleRunning = true;

        agent.isStopped = true;
        yield return new WaitForSeconds(2.0f);

        isIdleRunning = false;

        State = CopyState.Walk;
        agent.isStopped = false;
    }

    private void UpdateWalk()
    {
        if (agent.isPathStale) { return; } //최단 경로 계산 끝나지 않았다면 return

        if (agent.velocity.sqrMagnitude >= 0.2f * 0.2f && agent.remainingDistance <= 0.5f) //현재 속도가 0.04 보다 크면서, 현재 지점이 0.5 보다 작을경우 => 목적지와 가까워짐
        {
            Debug.Log("목적지 도착 후 Idle 상태");
            State = CopyState.Idle;

            int previousIndex = nextIndex;

            nextIndex = Random.Range(0, WayPoint.Count);

            if (WayPoint.Count > 1 && nextIndex == previousIndex)
            {
                nextIndex = (nextIndex - 1 + WayPoint.Count) % WayPoint.Count;
            }
        }

        Debug.Log($"Setting destination: {WayPoint[nextIndex].position}");
        agent.destination = WayPoint[nextIndex].position;
        agent.isStopped = false;

        RaySecurity(10f);
    }


    private void RaySecurity(float rayDistance)
    {
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 direction = transform.forward;

        Ray ray = new Ray(origin, direction);
        RaycastHit hit;

        if (Physics.SphereCast(ray, sphereRadius, out hit, rayDistance, securityLayer))
        {
            if (hit.collider.CompareTag("Bouncer"))
            {
                StopAllCoroutines(); // Idle 대기 중이면 중단

                //@@@@@@@@@@@@@@ 소리 지르는 사운드 추가 @@@@@@@@@@@@@@@@

                agent.isStopped = false;

                targetSecurity = hit.collider.gameObject;

                if(isServer)
                {
                    //AttackSetClientRpc();

                }

                OutlineAddMaterial(targetSecurity);

                State = CopyState.Follow;

                // 기타 로직


            }

        }

    }

    //[ClientRpc]
    //private void AttackSetClientRpc()
    //{
    //    Debug.Log(" ClientRpc 미라 컨트롤러 활성화");
    //    AttackCollier.gameObject.SetActive(true); //공격 콜라이더 On
    //}

    [SerializeField]
    GameObject AttackCollier;
    private void UpdateFollow()
    {
        if (copystate == CopyState.Die)
        {
            return;
        }

        if(targetSecurity != null)
        {
            agent.speed = 6f; // 추격 속도
            agent.SetDestination(targetSecurity.transform.position);
        }
    }


    #region 죽은 뒤 이펙트 생성 - 보류 (주석 처리)
    //[SerializeField]
    //GameObject FireEffect;

    //[Command(requiresAuthority = false)]
    //private void CopyStatueDie()
    //{

    //    SpawnBreakbleLock();

    //}

    //[ClientRpc]
    //private void SpawnBreakbleLock()
    //{
    //    UpdateDie();
    //}


    //private void UpdateDie()
    //{
    //    // 랜덤한 조각 

    //    GameObject fragment = ResourceManager.Instance.Instantiate("vfx_Flames_01", null, transform);

    //    if (isServer)
    //    {
    //        NetworkServer.Spawn(fragment);
    //    }

    //   // Destroy(fragment, 5f); //10초 뒤 제거
    //}

    //IEnumerator DelayFlamesEffectDestroy(GameObject flamesEffect)
    //{
    //    yield return new WaitForSeconds(3.0f);

    //    ResourceManager.Instance.Destroy(flamesEffect);
    //    ResourceManager.Instance.Destroy(this.gameObject);
    //}
    #endregion


    private void OutlineAddMaterial(GameObject security)
    {
        if(security == null) 
        {
            return;
        }

        Transform securityMesh = security.transform.GetChild(1);
        SkinnedMeshRenderer smr = securityMesh.GetComponent<SkinnedMeshRenderer>();

        if (smr != null)
        {
            Material[] mats = smr.materials; //1. 복사본을 생성하였지만

            if (!mats.Contains(outLine))
            {
                Material[] newMats = new Material[mats.Length + 1];

                for (int i = 0; i < mats.Length; i++)
                {
                    newMats[i] = mats[i];
                }
                newMats[mats.Length] = outLine;

                smr.materials = newMats; //2. 새 배열을 원본에 명시적으로 할당했기 때문에 가능
            }
        }

    }

    public void OutlineRemoveMaterial(GameObject security)
    {
        if (security == null)
        {
            Debug.Log("Security는 null");
            return;
        }

        Transform child = security.transform.GetChild(1); // 두 번째 자식
        SkinnedMeshRenderer smr = child.GetComponent<SkinnedMeshRenderer>();

        if (smr != null)
        {
           // Material[] mats = smr.materials; //원본을 수정해야 되는데 이것은 복사본이므로 원본에 영향을 가지 않아 제거 못함
            Material[] mats = smr.sharedMaterials;

            Debug.Log("마테리얼 제거 준비");

            // outLine 마테리얼이 포함되어 있다면 제거
            if (mats.Contains(outLine))
            {
                Debug.Log("마테리얼 제거");
                List<Material> matList = new List<Material>(mats);
                matList.Remove(outLine);
                smr.materials = matList.ToArray();
            }
        }
        foreach (var mat in smr.materials)
        {
            Debug.Log($"머티리얼 이름: {mat.name}");
        }
    }

    bool isCollider;
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Bouncer") && !isCollider)
        {
            if(isOwned) //미라를 스폰한 조각상 시점
            {
                transform.LookAt(other.transform.position);

                State = CopyState.Die;

                Debug.LogWarning("경비원과 충돌 - isOwned = true (미라 스크립트) ");

                isCollider = true;

            }
            //else //조각상이 아닌 시점
            //{
            //    Transform mirahead = transform.GetChild(1);
            //    other.GetComponent<SecurityController>().TestMira(this, mirahead);
            //    Debug.LogWarning("경비원과 충돌 - isOwned = false (미라 스크립트) ");
            //}
                          

        }
    }

    private void OnDrawGizmos()
    {


        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 direction = transform.forward;

        // 감지 범위 시각화: 전방 구체 캐스트
        Gizmos.color = Color.cyan;

        // 방향 벡터 끝점 계산
        Vector3 endPoint = origin + direction.normalized * 10f;

        // 선 그리기 (방향 표시)
        Gizmos.DrawLine(origin, endPoint);

        // 구체 감지 범위 시각화 (시작점 + 끝점에 구 그리기)
        Gizmos.DrawWireSphere(origin, sphereRadius);
        Gizmos.DrawWireSphere(endPoint, sphereRadius);
    }
}
