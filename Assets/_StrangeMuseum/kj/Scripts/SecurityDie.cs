using System;
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

    [Command(requiresAuthority = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsBrokenServerRpc(bool value)
    {
        isSecurityDie = value;
    }


    [Command(requiresAuthority = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsStatueColliderServerRpc(bool value)
    {
        isStatueCollider = value;
    }

    [SerializeField]
    NetworkIdentity currentSecurityBox; //경비원이 현재 입은 박스
    public void BoxSet(NetworkIdentity box)
    {
        if (!isOwned) { return; }

        if (box.TryGetComponent(out NetworkIdentity networkObject))
        {
            currentSecurityBox = box;
        }
    }
    private void Update()
    {
        if (!isOwned) { return; } 
       
        if (isStatueCollider == true)
        {
            currentSecurityBox.GetComponent<ShieldBox>().NotifyClientBoxRemoved();
            SetIsStatueColliderServerRpc(false);
        }
    }
    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.CompareTag("Statue") && isStatueCollider == false)
        {
            SetIsStatueColliderServerRpc(true);

            if (isOwned || isServer)
            {
                Debug.Log("----------------------------In1");
                GameManager.Instance.UpdatePlayerCountServerRpc(true, -1);
                GameManager.Instance.UpdatePlayerCountServerRpc(false, 1);
              
                //GameManager.Instance.PlayerStat.Value[OwnerClientId] = "Statue"; //정식님이 역할 배정 구현 후 얘기 나누기.
            }
        }
    }

    private void HandleDie()
    {
        SetIsBrokenServerRpc(true);
    }

    [Command(requiresAuthority = false)]
    public void SecurityDieServerRpc()
    {
        if (OnDie != null)
        {
            OnDie?.Invoke(this);  // OnDie 델리게이트 호출

            SpawnBloodServerRpc(); //피 웅덩이

            SpawnFragmentServerRpc(); //시체 조각
        }
        else
        {
            Debug.LogWarning("OnDie null 이므로 델리게이트 호출 x");
        }

    }


    [SerializeField]
    private GameObject bloodPrefab; // 피 프리팹
    [SerializeField]
    private GameObject[] fragmentPrefabs; // 부서진 조각 프리팹 리스트

    public Action<SecurityDie> OnDie;

    private float explosionForce = 5f; // 튀는 힘
    private float explosionRadius = 3f; // 폭발 반경

    [Command(requiresAuthority = false)]
    public void SpawnBloodServerRpc()
    {
        if (!isServer) return;

        Debug.Log("SpawnBloodServerRpc() - IsServer = true");

        SpawnBlood();  // 서버에서 피 생성

    }

    [ClientRpc]
    void SpawnBloodClientRpc(Vector3 bloodPosition)
    {
        if (isServer) return;

        Debug.Log("SpawnBloodClientRpc() - IsServer = false");

        SpawnBlood(bloodPosition);  // 클라이언트에서 피 생성
    }

    private void SpawnBlood(Vector3 bloodPosition = default)
    {
        // 피 위치가 지정되지 않으면 기본 위치로 설정
        if (bloodPosition == default)
        {
            bloodPosition = transform.position + Vector3.up * 0.05f;
        }

        GameObject blood = Instantiate(bloodPrefab, bloodPosition, Quaternion.identity);

        // 네트워크에서 피 객체를 Spawn

        // 클라이언트에서는 SpawnBloodClientRpc() 호출, 서버에서는 로컬로 처리
        if (isServer)
        {
            NetworkServer.Spawn(blood);
            SpawnBloodClientRpc(bloodPosition);
        }

        // 피 객체는 일정 시간이 지나면 제거
        Destroy(blood, 5f);
    }

    [Command(requiresAuthority = false)]
    public void SpawnFragmentServerRpc()
    {
        if (!isServer) return;

        Debug.Log("SpawnFragmentServerRpc - IsServer = true");

        SpawnFragments(); // 서버에서 조각 생성
        SpawnFragmentClientRpc();
    }

    [ClientRpc]
    private void SpawnFragmentClientRpc()
    {
        if (isServer) return;

        Debug.Log("SpawnFragmentClientRpc - IsServer = false");

        SpawnFragments(); // 클라이언트에서 조각 생성
    }

    private void SpawnFragments()
    {
        for (int i = 0; i < 20; i++)
        {
            if (fragmentPrefabs.Length > 0)
            {
                // 랜덤한 조각 선택
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

                Destroy(fragment, 10f);
            }
        }
    }
}
