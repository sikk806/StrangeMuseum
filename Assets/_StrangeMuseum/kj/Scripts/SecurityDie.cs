using System;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SecurityDie : NetworkBehaviour
{
    //경비원 사망 여부
    [SyncVar]
    public bool isSecurityDie;

    //조각상과 충돌 여부
    [SyncVar]
    public bool isStatueCollider;


    [SerializeField]
    private GameObject bloodPrefab; // 피 프리팹
    [SerializeField]
    private GameObject[] fragmentPrefabs; // 부서진 조각 프리팹 리스트

    public Action<SecurityDie> OnDie;

    private float explosionForce = 5f; // 튀는 힘
    private float explosionRadius = 3f; // 폭발 반경

    [SerializeField]
    NetworkIdentity currentSecurityBox; //경비원이 현재 입은 박스

    [ClientRpc]
    public void BoxSetClientRpc(uint boxNetId)
    {
        if (NetworkClient.spawned.TryGetValue(boxNetId, out var boxIdentity))
        {
            currentSecurityBox = boxIdentity;
            Debug.Log($"Box 이름: {currentSecurityBox.name} / 활성 상태: {currentSecurityBox.gameObject.activeSelf}");
        }
    }


    public void NotifyClientBoxRemoved()
    {
        NetworkIdentity playerIdentity = this.GetComponent<NetworkIdentity>();

        this.GetComponent<SecurityInteraction>().BoxFunction(playerIdentity, currentSecurityBox, false);

        StartCoroutine(DelayBoxing()); //무적 시간
    }



    IEnumerator DelayBoxing()
    {
        yield return new WaitForSeconds(2.0f);
        currentSecurityBox.GetComponent<ShieldBox>().isBoxUsing = false;
        Debug.Log("isBoxUsing = false");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Statue"))
        {
            if (currentSecurityBox.GetComponent<ShieldBox>().isBoxUsing)
            {
                NotifyClientBoxRemoved();
            }
            else if(currentSecurityBox.GetComponent<ShieldBox>().isBoxUsing == false)
            {
                //if(! isOwned == false)
                DieFunctionServerRPc();
                currentSecurityBox.GetComponent<ShieldBox>().ResetInteractServerRpc(); //박스 기능 초기화(박스 벗겨짐)
            }
        }
    }

    [Command(requiresAuthority = false)]
    private void DieFunctionServerRPc()
    {
        SpawnBloodClientRpc(); //피 웅덩이

        SpawnFragmentClientRpc(); //시체 조각

        //if (OnDie != null)
        //{
        //    OnDie?.Invoke(this);  // OnDie 델리게이트 호출
        //}
        //else
        //{
        //    Debug.LogWarning("OnDie null 이므로 델리게이트 호출 x");
        //}

    }


    #region 피 웅덩이
    [ClientRpc]
    private void SpawnBloodClientRpc()
    {
        SpawnBlood();
    }

    private void SpawnBlood(Vector3 bloodPosition = default)
    {
        // 피 위치가 지정되지 않으면 기본 위치로 설정
        if (bloodPosition == default)
        {
            bloodPosition = transform.position + Vector3.up * 0.05f;
        }

        GameObject blood = Instantiate(bloodPrefab, bloodPosition, Quaternion.identity);

        if (isServer)
        {
            NetworkServer.Spawn(blood);
        }
   
        Destroy(blood, 5f); //5초 뒤 제거
    }
    #endregion

    #region 시체 조각
    [ClientRpc]
    private void SpawnFragmentClientRpc()
    {
        SpawnFragments(); // 클라이언트에서 조각 생성
    }

    private void SpawnFragments()
    {
        for (int i = 0; i < 20; i++)
        {
            if (fragmentPrefabs.Length > 0)
            {
                // 랜덤한 조각 
                GameObject fragment = Instantiate(
                    fragmentPrefabs[UnityEngine.Random.Range(0, fragmentPrefabs.Length)],
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

                Destroy(fragment, 10f); //10초 뒤 제거
            }
        }
    }
    #endregion
}
