using System;
using System.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class BreakableObject : NetworkBehaviour
{
    [SerializeField]
    private GameObject bloodPrefab; // 피 프리팹
    [SerializeField]
    private GameObject[] fragmentPrefabs; // 부서진 조각 프리팹 리스트

    public Action<BreakableObject> OnDie;

    private float explosionForce = 5f; // 튀는 힘
    private float explosionRadius = 3f; // 폭발 반경

  

    // 석상에게 맞았을 때
    //void OnTriggerEnter(Collider other)
    //{
    //    if (other.tag == "Statue")
    //    {
    //        if (GetComponent<SecurityInteraction>().isBoxUsing.Value == true) //경비원에게 박스를 씌웠을 경우
    //        {
    //            return; //아래 죽음 처리 실행 X
    //        }

    //        if (!isBroken.Value) // 한 번만 실행되도록 방지
    //        {
    //            BreakObject();
    //            SetIsBrokenServerRpc(true);
    //        }
    //    }
    //}

    public void BreakObject()
    {
        // 델리게이트 실행
        OnDie?.Invoke(this);
        Debug.Log("BreakObject-OnDieInvoke Test");
        //if (isBroken.Value) return;

        //Debug.Log("BO(Line 27) - Security is Broken...");
        //BrokenValueServerRpc(true);

        // 피 웅덩이 생성
        SpawnBloodServerRpc();

        //// 시체 토막 생성
        SpawnFragmentServerRpc();

        //// PlayerRespawnManager에 있는 함수 호출 (델리게이트)
        //Debug.Log("BO(Line 37) - OnDie Invoke");
        //if (OnDie == null) Debug.Log("BO(Line 38) - OnDie is Null!!");
        //OnDie.Invoke(this);
    }


    // 피 웅덩이 생성
    [ServerRpc(RequireOwnership = false)]
    private void SpawnBloodServerRpc()
    {
        Debug.Log("SpawnBloodServerRpc() - IsServer ? ");

        if (!IsServer) return;

        Debug.Log("SpawnBloodServerRpc() - IsServer = true");
        float bloodOffset = 0.05f; // 바닥과의 거리
        RaycastHit hit;

        Vector3 bloodPosition = transform.position + Vector3.up * bloodOffset; // 피를 생성할 위치
        GameObject blood = Instantiate(bloodPrefab, bloodPosition, Quaternion.identity);
        //blood.transform.up = hit.normal; // 바닥 기울기에 맞게 정렬
                                         blood.GetComponent<NetworkObject>().Spawn();

        SpawnBloodClientRpc();
    }
    [ClientRpc]
    void SpawnBloodClientRpc()
    {
        Debug.Log("SpawnBloodClientRpc() - IsServer ? ");

        if (IsServer) return;

        Debug.Log("SpawnBloodClientRpc() - IsServer = false");
        float bloodOffset = 0.05f; // 바닥과의 거리
        RaycastHit hit;

        Vector3 bloodPosition = transform.position + Vector3.up * bloodOffset; // 피를 생성할 위치
        GameObject blood = Instantiate(bloodPrefab, bloodPosition, Quaternion.identity);
        //blood.transform.up = hit.normal; // 바닥 기울기에 맞게 정렬
    }
    // 시체 토막 생성
    [ServerRpc(RequireOwnership = false)]
    private void SpawnFragmentServerRpc()
    {
        Debug.Log("SpawnFragmentServerRpc - IsServer ?");

        if (!IsServer) return;

        Debug.Log("SpawnFragmentServerRpc - IsServer = true");
        // 여러 조각 생성
        for (int i = 0; i < 20; i++)
        {
            if (fragmentPrefabs.Length > 0)
            {
                // 랜덤한 조각 선택
                GameObject fragment = Instantiate(
                    fragmentPrefabs[Random.Range(0, fragmentPrefabs.Length)],
                    transform.position,
                    Random.rotation
                );

                fragment.GetComponent<NetworkObject>().Spawn();
                Rigidbody rb = fragment.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // 랜덤 방향으로 튀기기
                    Vector3 explosionDir = Random.insideUnitSphere; // 모든 방향 랜덤
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 0, ForceMode.Impulse);
                }

                Destroy(fragment, 10f); // 일정 시간 후 조각 삭제
            }
        }
        SpawnFragmentClientRpc();
    }
    [ClientRpc]
    private void SpawnFragmentClientRpc()
    {
        Debug.Log("SpawnFragmentClientRpc - Server ??");

        if (IsServer) return;

        Debug.Log("SpawnFragmentClientRpc - Server = false");

        for (int i = 0; i < 20; i++)
        {
            if (fragmentPrefabs.Length > 0)
            {
                // 랜덤한 조각 선택
                GameObject fragment = Instantiate(
                    fragmentPrefabs[Random.Range(0, fragmentPrefabs.Length)],
                    transform.position,
                    Random.rotation
                );

                Rigidbody rb = fragment.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    // 랜덤 방향으로 튀기기
                    Vector3 explosionDir = Random.insideUnitSphere; // 모든 방향 랜덤
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 0, ForceMode.Impulse);
                }

                Destroy(fragment, 10f); // 일정 시간 후 조각 삭제
            }
        }
    }


}