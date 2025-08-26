using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.AI.Navigation;
using Unity.VisualScripting;
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
    Material SecurityOutLine;


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

    [SyncVar]
    private NetworkIdentity statueOwner;

    public override void OnStartServer()
    {
        base.OnStartServer();
        statueOwner = connectionToClient.identity;
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
        if (copystate == CopyState.Die)
        {
            if (agent.enabled && agent.isOnNavMesh)
            {
              
                Debug.Log("Update - Die");

                agent.ResetPath();
                agent.isStopped = true;

                agent.velocity = Vector3.zero; // 남은 속도 제거
                agent.enabled = false; // 완전 차단
            }
            return; 
        }

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

        var player = targetSecurity.GetComponent<SecurityController>();


        CmdRequestOutlineRemove();


        if (isServer)
        {
            player.SetPlayerStateServer(PlayerState.Idle); // 서버 직접 변경
        }
        else if (isOwned)
        {
            player.CmdSetPlayerState(PlayerState.Idle); // 클라이언트 → 서버
        }


        //NetworkServer.Destroy(this.gameObject);
       StartCoroutine(DelayDestroy());
    }

    IEnumerator DelayDestroy()
    {
        yield return new WaitForSeconds(1.0f); // 다음 프레임까지 기다림
        Debug.Log("미라 Net 사라짐");
        NetworkServer.Destroy(this.gameObject);
        yield return null; // 다음 프레임까지 기다림
        Debug.Log("미라 Scene 사라짐");
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

        RaySecurity();
    }

    [SerializeField]
    float RayWidth = 5f;
    [SerializeField]
    float RayHeight = 3f;
    [SerializeField]
    float RayDistance = 3f;

    private void RaySecurity()
    {
        if (!isServer) return;

        Vector3 center = transform.position + transform.forward * RayDistance * 0.5f + Vector3.up * 1.0f;
        Vector3 halfExtents = new Vector3(RayWidth * 0.5f, RayHeight * 0.5f, RayDistance * 0.5f);

        // forward 방향을 반영
        Quaternion orientation = Quaternion.LookRotation(transform.forward, Vector3.up);

        Collider[] hits = Physics.OverlapBox(center, halfExtents, orientation, securityLayer);

        foreach (var hit in hits)
        {
            if (hit.CompareTag("Bouncer"))
            {
                StopAllCoroutines(); // Idle 중단
                agent.isStopped = false;

                targetSecurity = hit.gameObject;
                State = CopyState.Follow;

                Debug.Log($"감지됨: {hit.name}");
                break;
            }
        }

    }


    [SerializeField]
    float alertDistance = 3f; // 원하는 범위

    bool isFollow;

    private void UpdateFollow()
    {
        if (copystate == CopyState.Die)
        {
            return;
        }

        if(isServer && isFollow == false)
        {
            CmdRequestOutlineAdd();
            isFollow = true;
        }


        if (targetSecurity != null)
        {
            Debug.Log("경비원 쫒아가는 중 + Follow상태");

            agent.isStopped = false;
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


    [Command(requiresAuthority = false)]
    public void CmdRequestOutlineAdd()
    {
        if (targetSecurity == null) return;

        if (connectionToClient != null)
            TargetOutlineAdd(statueOwner.connectionToClient, targetSecurity);
    
    }

    [Command(requiresAuthority = false)]
    public void CmdRequestOutlineRemove()
    {
        if (targetSecurity == null) return;

        if (connectionToClient != null)
            TargetOutlineRemove(statueOwner.connectionToClient, targetSecurity);
    }

    private Material outlineInstance;

    // 실제로 경비원 소유 클라이언트에서만 실행됨
    [TargetRpc]
    private void TargetOutlineAdd(NetworkConnection target, GameObject securityObj)
    {

        SkinnedMeshRenderer smr = securityObj.transform.GetChild(1).GetComponent<SkinnedMeshRenderer>();
        if (smr != null)
        {
            // 한 번만 인스턴스화
            outlineInstance ??= Instantiate(SecurityOutLine);

            List<Material> matList = new List<Material>(smr.sharedMaterials);

            if (!matList.Contains(outlineInstance))
            {
                Debug.Log("TargetOutlineAdd");
                matList.Add(outlineInstance);
                smr.materials = matList.ToArray();
            }
        }
    }

    [TargetRpc]
    private void TargetOutlineRemove(NetworkConnection target, GameObject securityObj)
    {
        

        SkinnedMeshRenderer smr = securityObj.transform.GetChild(1).GetComponent<SkinnedMeshRenderer>();
        if (smr != null && outlineInstance != null)
        {
            List<Material> matList = new List<Material>(smr.sharedMaterials);

            // 중복된 인스턴스 모두 제거
            int removedCount = matList.RemoveAll(m => m == outlineInstance);
            if (removedCount > 0)
            {
                Debug.Log("TargetOutlineRemove - Removed " + removedCount);
                smr.materials = matList.ToArray();
            }
        }
    }


    private void OnDrawGizmosSelected()
    {
      

        Vector3 center = transform.position + transform.forward * RayDistance * 0.5f + Vector3.up;
        Vector3 halfExtents = new Vector3(RayWidth * 0.5f, RayHeight * 0.5f, RayDistance * 0.5f);

        Quaternion orientation = Quaternion.LookRotation(transform.forward, Vector3.up);

        Gizmos.color = Color.yellow;
        Gizmos.matrix = Matrix4x4.TRS(center, orientation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, halfExtents * 2);
    }
}
