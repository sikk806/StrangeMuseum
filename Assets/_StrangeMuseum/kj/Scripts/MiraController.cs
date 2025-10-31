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
using static UnityEditor.PlayerSettings;
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

    [SyncVar]
    [SerializeField]
    GameObject targetSecurity;

    [SerializeField]
    LayerMask securityLayer;

    [SerializeField]
    float sphereRadius = 1.5f; // 반경 조절 가능


    [SerializeField]
    Material SecurityOutLine;


    [SyncVar]
    private NetworkIdentity statueOwner;
    [SerializeField]
    float maxLifeTime = 5;
    private bool isIdleRunning = false;

    [SerializeField]
    float RayWidth = 5f;
    [SerializeField]
    float RayHeight = 3f;
    [SerializeField]
    float RayDistance = 3f;

    [SerializeField]
    float alertDistance = 3f; // 원하는 범위

    bool isFollow;

    private Material outlineInstance;

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
                    isFollow = false;

                    if (agent != null && agent.enabled && agent.isOnNavMesh)
                    {
                        agent.ResetPath();
                        agent.isStopped = true;

                        agent.Warp(agent.transform.position); // 내부 velocity, remainingDistance 모두 초기화
                        agent.SetDestination(agent.transform.position);
                    }

                    var col = GetComponent<Collider>();
                    if (col) col.enabled = false;
                    break;

            }
        }
    }

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

    void Update()
    {
        switch (copystate)
        {
            case CopyState.Idle:
                if (!isIdleRunning)
                    StartCoroutine(UpdateIdle());
                break;
            case CopyState.Walk:
                if (copystate == CopyState.Walk) // 자기 상태일 때만 실행
                    UpdateWalk();
                break;
            case CopyState.Stop:
                agent.isStopped = true;
                break;
            case CopyState.Follow:
                if (agent.hasPath) agent.ResetPath(); // 이전 Walk 경로 완전 제거
                UpdateFollow();
                break;
        }
    }

    IEnumerator MiraLife()
    {
        float currentLifeTime = 0;

        while (currentLifeTime < maxLifeTime)
        {
            currentLifeTime += Time.deltaTime;
            yield return null;
        }
        // 현재 상태를 미리 저장
        if(isServer)
        {
            Debug.Log("미라 생존시간에 의한 죽음");
            State = CopyState.Die;

            if(targetSecurity == null)
            {
                if(isServer)
                {
                    Debug.Log("targetSecurity/isServer- null");
                }
                else if(isClient)
                {
                    Debug.Log("targetSecurity/isClient- null");
                }
            }

            StartCoroutine(MiraDie());

        }

    }


    [Command(requiresAuthority = false)]
    public void CmdRequestDestroy()
    {
        if(targetSecurity != null)
        {
            State = CopyState.Die;

            var player = targetSecurity.GetComponent<SecurityController>();


            if (isServer)
            {
                player.SetPlayerStateServer(PlayerState.Idle); // 서버 직접 변경
            }
            else if (isOwned)
            {
                player.CmdSetPlayerState(PlayerState.Idle); // 클라이언트 -> 서버
            }


            StartCoroutine(MiraDie());

        }
       
    }
    IEnumerator MiraDie()
    {
        State = CopyState.Die;

        GameObject fragment = ResourceManager.Instance.Instantiate("vfx_Flames_01", null, transform);

        NetworkServer.Spawn(fragment);

        yield return new WaitForSeconds(0.05f); //패킷 전송 여유

        if (isServer)
        {
            TargetOutlineRemove(statueOwner.connectionToClient, targetSecurity);
        }

        yield return new WaitForSeconds(1.0f);

        NetworkServer.Destroy(fragment);
        NetworkServer.Destroy(this.gameObject);
        yield return null; // 다음 프레임까지 기다림
     
        ResourceManager.Instance.Destroy(this.gameObject);
    }

    IEnumerator UpdateIdle()
    {
        if (isIdleRunning) yield break; // 중복 실행 방지
        isIdleRunning = true;

        agent.isStopped = true;
        yield return new WaitForSeconds(2.0f);

        isIdleRunning = false;

        State = CopyState.Walk;
        agent.isStopped = false;

        StartCoroutine(MiraLife());
    }

    private void UpdateWalk()
    {
        if (agent.isPathStale) { return; } //최단 경로 계산 끝나지 않았다면 return

        if (agent.velocity.sqrMagnitude >= 0.2f * 0.2f && agent.remainingDistance <= 0.5f) //현재 속도가 0.04 보다 크면서, 현재 지점이 0.5 보다 작을경우 => 목적지와 가까워짐
        {
            State = CopyState.Idle;

            int previousIndex = nextIndex;

            nextIndex = Random.Range(0, WayPoint.Count);

            if (WayPoint.Count > 1 && nextIndex == previousIndex)
            {
                nextIndex = (nextIndex - 1 + WayPoint.Count) % WayPoint.Count;
            }
        }

        agent.destination = WayPoint[nextIndex].position;
        agent.isStopped = false;

        RaySecurity();
    }


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
                //StopAllCoroutines(); // Idle 중단

                agent.isStopped = false;

                agent.ResetPath();   //  Walk 경로 완전 제거

                if(isServer)
                {
                    Debug.Log("targetSecurity 저장");

                    targetSecurity = hit.gameObject;

                    TargetOutlineAdd(statueOwner.connectionToClient, targetSecurity);

                    State = CopyState.Follow;
                }
                break;
            }
        }

    }
    private void UpdateFollow()
    {
        if (State == CopyState.Die)
        {
            return;
        }


        if (targetSecurity != null && State != CopyState.Die)
        {

            agent.ResetPath();

            agent.isStopped = false;

            agent.speed = 6f; // 추격 속도
            agent.SetDestination(targetSecurity.transform.position);
      
        }
    }



    // 실제로 경비원 소유 클라이언트에서만 실행됨
    [TargetRpc]
    private void TargetOutlineAdd(NetworkConnection target, GameObject securityObj)
    {

        if (securityObj == null)
        {
            Debug.Log("타겟(경비원)이 없으므로 OutLine Add 불가 후 return");
            return;
        }

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

        if (securityObj == null)
        {
            Debug.Log("타겟(경비원)이 없으므로 OutLine Add 불가 후 return");
            return;
        }

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
