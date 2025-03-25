using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TestMultiManager : NetworkBehaviour
{
    public GameObject Bouncer;
    public GameObject Statue;

    public List<Transform> SecuritySpawnPoint = new List<Transform>();
    public List<Transform> StatueSpawnPoint = new List<Transform>();

    private int bouncerPoint = 0;
    private int statuePoint = -1;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        if (IsServer)
        {
            OnClientConnected(OwnerClientId);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (IsServer)
        {
            string clientStat = GameManager.Instance.PlayerStat.Value[clientId].ToString();
            switch (clientStat)
            {
                case "Security":
                    GameManager.Instance.SecurityCount.Value--;
                    break;

                case "Statue":
                    GameManager.Instance.StatueCount.Value--;
                    break;
            }

            GameManager.Instance.PlayerStat.Value.Remove(clientId);
        }
    }

    void OnClientConnected(ulong clientId)
    {
        Debug.Log("Connecting...");
        if (IsServer)
        {
            Debug.Log($"Connected! My Client ID: {clientId}");
            SpawnPlayerForClient(clientId);
        }
        // if (!IsServer) return;
        // SpawnPlayerForClient(clientId);
    }

    void SpawnPlayerForClient(ulong clientId)
    {
        GameObject playerPrefab = new GameObject();
        // int selectCharacter = Random.Range(0, 2); // 0 : Security

        int selectCharacter = 0;

        if (selectCharacter == 0)
        {
            if (GameManager.Instance.SecurityCount.Value > 1)
            {
                playerPrefab = Statue;

                if (IsServer)
                {
                    GameManager.Instance.StatueCount.Value++;
                    GameManager.Instance.PlayerStat.Value[clientId] = "Statue";
                    int ranPos = Random.Range(0, StatueSpawnPoint.Count);
                    while (ranPos == statuePoint)
                    {
                        ranPos = Random.Range(0, StatueSpawnPoint.Count);
                    }
                    GameObject newPlayer = Instantiate(playerPrefab, StatueSpawnPoint[ranPos].position, Quaternion.identity);
                    newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
                }
            }
            else
            {
                playerPrefab = Bouncer;

                if (IsServer)
                {
                    GameManager.Instance.SecurityCount.Value++;
                    GameManager.Instance.PlayerStat.Value[clientId] = "Security";
                    GameObject newPlayer = Instantiate(playerPrefab, SecuritySpawnPoint[bouncerPoint++].position, Quaternion.identity);
                    newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
                }
            }
        }
        else if (selectCharacter == 1)
        {
            if (GameManager.Instance.StatueCount.Value > 4)
            {
                playerPrefab = Bouncer;

                if (IsServer)
                {
                    GameManager.Instance.SecurityCount.Value++;
                    GameManager.Instance.PlayerStat.Value[clientId] = "Security";
                    GameObject newPlayer = Instantiate(playerPrefab, SecuritySpawnPoint[bouncerPoint++].position, Quaternion.identity);
                    newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
                }
            }
            else
            {
                playerPrefab = Statue;

                if (IsServer)
                {
                    GameManager.Instance.StatueCount.Value++;
                    GameManager.Instance.PlayerStat.Value[clientId] = "Statue";
                    int ranPos = Random.Range(0, StatueSpawnPoint.Count);
                    while (ranPos == statuePoint)
                    {
                        ranPos = Random.Range(0, StatueSpawnPoint.Count);
                    }
                    GameObject newPlayer = Instantiate(playerPrefab, StatueSpawnPoint[ranPos].position, Quaternion.identity);
                    newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
                }
            }
        }

    }

    void PlayerDie(ulong clientId)
    {
        GameObject playerPrefab = Bouncer;
        GameObject revivePlayer = Instantiate(playerPrefab);
        revivePlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    }

    public void OnHostButton()
    {
        NetworkManager.Singleton.StartHost();
    }

    public void OnClientButton()
    {
        NetworkManager.Singleton.StartClient();
    }
}
