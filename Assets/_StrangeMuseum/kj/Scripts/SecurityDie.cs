using System;
using Unity.Netcode;
using UnityEngine;

public class SecurityDie : NetworkBehaviour
{
    //경비원 사망 여부
    public NetworkVariable<bool> isSecurityDie = 
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    //조각상과 충돌 여부
    public NetworkVariable<bool> isStatueCollider = 
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsBrokenServerRpc(bool value)
    {
        isSecurityDie.Value = value;
    }


    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsStatueColliderServerRpc(bool value)
    {
        isStatueCollider.Value = value;
    }

    NetworkObjectReference box;

    [SerializeField]
    GameObject currentBox;
    public void BoxFunction(NetworkObjectReference box)
    {
        if (!IsOwner) { return; }

        if (box.TryGet(out NetworkObject networkObject))
        {
            currentBox = box;
        }
    }
    private void Update()
    {
        if (!IsOwner) { return; } 
       
        if (isStatueCollider.Value == true)
        {
            currentBox.GetComponent<ShieldBox>().NotifyClientBoxRemoved();
            SetIsStatueColliderServerRpc(false);
        }
    }
    private void OnTriggerEnter(Collider other)
    {

        if (other.gameObject.CompareTag("Statue") && isStatueCollider.Value == false)
        {
            SetIsStatueColliderServerRpc(true);

            if (IsOwner || IsServer)
            {
                Debug.Log("----------------------------In1");
                GameManager.Instance.UpdatePlayerCountServerRpc(true, -1);
                GameManager.Instance.UpdatePlayerCountServerRpc(false, 1);
                GameManager.Instance.PlayerStat.Value[OwnerClientId] = "Statue";
            }
        }
    }

    private void HandleDie()
    {
        SetIsBrokenServerRpc(true);
    }

    [ServerRpc(RequireOwnership = false)]
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

    [ServerRpc(RequireOwnership = false)]
    public void SpawnBloodServerRpc()
    {
        if (!IsServer) return;

        Debug.Log("SpawnBloodServerRpc() - IsServer = true");

        SpawnBlood();  // 서버에서 피 생성

    }

    [ClientRpc]
    void SpawnBloodClientRpc(Vector3 bloodPosition)
    {
        if (IsServer) return;

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
        NetworkObject networkObject = blood.GetComponent<NetworkObject>();
        if (networkObject != null)
        {
            networkObject.Spawn();
        }

        // 클라이언트에서는 SpawnBloodClientRpc() 호출, 서버에서는 로컬로 처리
        if (IsServer)
        {
            SpawnBloodClientRpc(bloodPosition);
        }

        // 피 객체는 일정 시간이 지나면 제거
        Destroy(blood, 5f);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnFragmentServerRpc()
    {
        if (!IsServer) return;

        Debug.Log("SpawnFragmentServerRpc - IsServer = true");

        SpawnFragments(); // 서버에서 조각 생성
        SpawnFragmentClientRpc();
    }

    [ClientRpc]
    private void SpawnFragmentClientRpc()
    {
        if (IsServer) return;

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

                // 네트워크 객체로 Spawn
                NetworkObject networkObject = fragment.GetComponent<NetworkObject>();
                if (networkObject != null)
                {
                    networkObject.Spawn();
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
