using System.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerRespawnManager : NetworkBehaviour
{
    [SerializeField] NetworkObject statuePrefab;

    // 스폰 시 델리게이트 추가

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        SecurityController.OnPlayerSpawn += HandlePlayerSpawn;
        SecurityController.OnPlayerDespawn += HandlePlayerDespawn;

        SecurityController[] securities = FindObjectsByType<SecurityController>(FindObjectsSortMode.None);
        foreach (SecurityController security in securities)
        {
            HandlePlayerSpawn(security);
        }

    }

    // 디스폰 시 델리게이트 해제
    public override void OnNetworkDespawn()
    {
        if (!IsServer) return;

        SecurityController.OnPlayerSpawn -= HandlePlayerSpawn;
        SecurityController.OnPlayerDespawn -= HandlePlayerDespawn;
    }

    private void HandlePlayerSpawn(SecurityController security)
    {
        Debug.Log("PRM(Line 35) - PlayerSpawnDelegate");
        if (!security.TryGetComponent<SecurityInteraction>(out var securityInteraction))
        {
            Debug.Log("PRM(Line 40) - BreakableObject is Null");
            return;
        }
        securityInteraction.OnDie += HandlePlayerDie;
    }

    private void HandlePlayerDespawn(SecurityController security)
    {
        security.GetComponent<SecurityInteraction>().OnDie -= HandlePlayerDie;
    }

    // 플레이어가 죽었을 때 처리할 것들. (Destroy)
    private void HandlePlayerDie(SecurityInteraction sender)
    {
        Debug.Log("PRM(Line 54) - Time To Respawn as Statue");

        SecurityController security = sender.GetComponent<SecurityController>();
        if (security == null)
        {
            Debug.Log("PRM(Line 59) - There is No SeucrityController. Check Again..");
            return;
        }

        sender.SpawnBloodServerRpc();
        sender.SpawnFragmentServerRpc();

        StartCoroutine(RespawnPlayerRoutine(sender.OwnerClientId, sender.transform.position));

        // 기존 오브젝트 제거 후 로컬에서도 제거.
        sender.GetComponent<NetworkObject>().Despawn();
        Destroy(security.gameObject);
       
    }

    // 플레이어가 죽고 죽은 위치에서 석상으로 부활
    IEnumerator RespawnPlayerRoutine(ulong ownerClientId, Vector3 spawnPosition)
    {
        Debug.Log("PRM(Line 73) - Respawn. Wait 1 second.");
        yield return new WaitForSeconds(1.0f);

        Debug.Log("PRM(Line 76) - Respawn Player...");

        NetworkObject playerObj = Instantiate(statuePrefab, spawnPosition, Quaternion.identity);
        playerObj.SpawnAsPlayerObject(ownerClientId);
    }
}   
