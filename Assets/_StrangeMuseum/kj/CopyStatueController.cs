using Mirror;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;
using static CopyStatueCotroller;
using static UnityEditor.VersionControl.Message;

public class CopyStatueCotroller : NetworkBehaviour
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
            group.GetComponentsInChildren<Transform>(WayPoint);
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

    IEnumerator UpdateIdle()
    {
        agent.isStopped = true;
        yield return new WaitForSeconds(2.0f);
        State = CopyState.Walk;
        agent.isStopped = false;
    }

    private void UpdateWalk()
    {
        if (agent.isPathStale) { return; } //최단 경로 계산 끝나지 않았다면 return

        if (agent.velocity.sqrMagnitude >= 0.2f * 0.2f && agent.remainingDistance <= 0.5f) //현재 속도가 0.04 보다 크면서, 현재 지점이 0.5 보다 작을경우 => 목적지와 가까워짐
        {
            State = CopyState.Idle;

            int previousIndex = nextIndex;

            nextIndex = Random.Range(0, WayPoint.Count);

            if (nextIndex == previousIndex)
            {
                nextIndex = (nextIndex - 1) % WayPoint.Count;
            }
        }

        agent.destination = WayPoint[nextIndex].position;
        agent.isStopped = false;

        RaySecurity();
    }

    [SerializeField]
    GameObject targetSecurity;
    private void RaySecurity()
    {
        float rayDistance = 10f;
        Vector3 origin = transform.position + Vector3.up * 1.0f;
        Vector3 direction = transform.forward;

        Ray ray = new Ray(origin, direction);
        RaycastHit hit;

        Vector3 endPoint = origin + direction * rayDistance;

        if (Physics.Raycast(ray, out hit, rayDistance))
        {
            endPoint = hit.point;

            if (hit.collider.TryGetComponent<SecurityController>(out var security))
            {
                StopAllCoroutines(); // Idle 대기 중이면 중단

                agent.isStopped = false;

                targetSecurity = hit.collider.gameObject;

                State = CopyState.Follow;
                // 기타 로직
            }
        }

    }

    private void UpdateFollow()
    {
        agent.speed = 6f; // 추격 속도
        agent.SetDestination(targetSecurity.transform.position);

    }

    [SerializeField]
    GameObject[] BreakbleRocks;

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
        for (int i = 0; i < 5; i++)
        {
            if (BreakbleRocks.Length > 0)
            {
                // 랜덤한 조각 
                GameObject fragment = Instantiate(
                    BreakbleRocks[UnityEngine.Random.Range(0, BreakbleRocks.Length)],
                    transform.position,
                    UnityEngine.Random.rotation
                );

                if (isServer)
                {
                    NetworkServer.Spawn(fragment);
                }

                Rigidbody rb = fragment.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 0, ForceMode.Impulse);
                }

                Destroy(fragment, 5f); //10초 뒤 제거
            }
        }
    }

    [SerializeField]
    private bool isDie;
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Bouncer") && isDie == false)
        {
            State = CopyState.Die;
        }
    }
}
