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

    NavMeshAgent agent;

    private int nextIndex;

    [SerializeField]
    bool isPath = false;

    [SerializeField]
    CopyState copystate;

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
                UpdateFollow();
                break;
            case CopyState.Die:         
                if(isDie == false)
                {
                    CopyStatueDie();
                    isDie = true;
                }
                break;
        }
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

        RaySecurity();
    }

    GameObject targetSecurity;

    [SerializeField]
    LayerMask securityLayer;

    [SerializeField]
    float sphereRadius = 1.5f; // 반경 조절 가능
    private void RaySecurity()
    {
        float rayDistance = 10f;
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 direction = transform.forward;

        Ray ray = new Ray(origin, direction);
        RaycastHit hit;

        if (Physics.SphereCast(ray, sphereRadius, out hit, rayDistance, securityLayer))
        {
            if (hit.collider.CompareTag("Bouncer"))
            {
                StopAllCoroutines(); // Idle 대기 중이면 중단

                agent.isStopped = false;

                targetSecurity  = hit.collider.gameObject;

                OutlineAddMaterial(targetSecurity);
                 
                State = CopyState.Follow;
           
                // 기타 로직
            }
        }

    }

    [SerializeField]
    Material outLine;

    private void UpdateFollow()
    {


        agent.speed = 6f; // 추격 속도
        agent.SetDestination(targetSecurity.transform.position);

    }

    [SerializeField]
    GameObject FireEffect;

    private float explosionForce = 5f; // 튀는 힘
    private float explosionRadius = 3f; // 폭발 반경

    [Command(requiresAuthority = false)]
    private void CopyStatueDie()
    {

        SpawnBreakbleLock();

    }

    [ClientRpc]
    private void SpawnBreakbleLock()
    {
        UpdateDie();
    }


    private void UpdateDie()
    {
        // 랜덤한 조각 

        GameObject fragment = ResourceManager.Instance.Instantiate("vfx_Flames_01", null, transform);

        if (isServer)
        {
            NetworkServer.Spawn(fragment);
        }

       // Destroy(fragment, 5f); //10초 뒤 제거
    }

    IEnumerator DelayFlamesEffectDestroy(GameObject flamesEffect)
    {
        yield return new WaitForSeconds(3.0f);

        ResourceManager.Instance.Destroy(flamesEffect);
        ResourceManager.Instance.Destroy(this.gameObject);
    }

    private void OutlineAddMaterial(GameObject security)
    {
        if(security == null) { return; }

        Transform securityMesh = security.transform.GetChild(1);
        SkinnedMeshRenderer smr = securityMesh.GetComponent<SkinnedMeshRenderer>();

        if (smr != null)
        {
            Material[] mats = smr.materials;

            if (!mats.Contains(outLine))
            {
                Material[] newMats = new Material[mats.Length + 1];

                for (int i = 0; i < mats.Length; i++)
                {
                    newMats[i] = mats[i];
                }
                newMats[mats.Length] = outLine;

                smr.materials = newMats;
            }
        }

    }

    private void OutlineRemoveMaterial(GameObject security)
    {
        if (security == null) return;

        Transform child = security.transform.GetChild(1); // 두 번째 자식
        SkinnedMeshRenderer smr = child.GetComponent<SkinnedMeshRenderer>();

        if (smr != null)
        {
            Material[] mats = smr.materials;

            // outLine 마테리얼이 포함되어 있다면 제거
            if (mats.Contains(outLine))
            {
                Debug.Log("마테리얼 제거");
                List<Material> matList = new List<Material>(mats);
                matList.Remove(outLine);
                smr.materials = matList.ToArray();
            }
        }
    }

    [SerializeField]
    private bool isDie;
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Bouncer") && isDie == false)
        {
            OutlineRemoveMaterial(other.gameObject);
            State = CopyState.Die;
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
