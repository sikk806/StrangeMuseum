using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Steamworks;
using Unity.VisualScripting;
using System.Collections;

public class SMNetworkManager : NetworkManager
{
    [SerializeField] private PlayerLobbyController GamePlayerPrefab;
    [SerializeField] private GameObject securityPrefab;
    [SerializeField] private GameObject statuePrefab;

    [SerializeField] public Vector3[] SecurityPos = new Vector3[2];
    [SerializeField] public Vector3[] StatuePos = new Vector3[2];

    private int[] checkSecPos = new int[2];
    private int[] checkStaPos = new int[2];

    public List<PlayerLobbyController> GamePlayers { get; } = new List<PlayerLobbyController>(); // Info of Players

    [SerializeField]
    private bool forceLocalMode = true; // 스팀 사용 아닐 떄 켜기.

    public GameObject GetStatuePrefab() { return statuePrefab; }

    public override void Awake()
    {
        base.Awake();

        if (!spawnPrefabs.Contains(securityPrefab))
            spawnPrefabs.Add(securityPrefab);

        if (!spawnPrefabs.Contains(statuePrefab))
            spawnPrefabs.Add(statuePrefab);
    }

    public override void Start()
    {

    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name == "NetworkTest")
        {
            PlayerLobbyController GamePlayerInstance = Instantiate(GamePlayerPrefab);
            GamePlayerInstance.transform.position = Vector3.zero;

            GamePlayerInstance.ConnectionID = conn.connectionId;
            GamePlayerInstance.PlayerIdNumber = GamePlayers.Count + 1;

            if (SteamManager.Initialized && SteamLobby.Instance != null)
            {
                GamePlayerInstance.PlayerSteamId = (ulong)SteamMatchmaking.GetLobbyMemberByIndex(
                    (CSteamID)SteamLobby.Instance.CurrentLobbyID, GamePlayers.Count);
            }
            else
            {
                Debug.Log("Steam not initialized or SteamLobby missing. Assigning dummy SteamID.");
                GamePlayerInstance.PlayerSteamId = 0; // fallback or dummy ID
            }
            NetworkServer.AddPlayerForConnection(conn, GamePlayerInstance.gameObject);

            if (forceLocalMode)
            {
                GameObject playerObj = null;

                int randomPrefab = Random.Range(0, 2);
                if (randomPrefab == 0)
                {
                    if (GameResultManager.Instance.StatueCount == 1)
                    {
                        playerObj = Instantiate(securityPrefab);
                        GameResultManager.Instance.SecurityCount += 1;
                        int num = Random.Range(0, 2);
                        while (checkSecPos[num] == 1)
                        {
                            num = Random.Range(0, 2);
                        }
                        playerObj.transform.position = SecurityPos[num];
                    }
                    else
                    {
                        playerObj = Instantiate(statuePrefab);
                        GameResultManager.Instance.StatueCount += 1;
                        int num = Random.Range(0, 2);
                        while (checkStaPos[num] == 1)
                        {
                            num = Random.Range(0, 2);
                        }
                        playerObj.transform.position = StatuePos[num];
                    }
                }
                else
                {
                    if (GameResultManager.Instance.SecurityCount == 1)
                    {
                        playerObj = Instantiate(statuePrefab);
                        GameResultManager.Instance.StatueCount += 1;
                        int num = Random.Range(0, 2);
                        while (checkStaPos[num] == 1)
                        {
                            num = Random.Range(0, 2);
                        }
                        playerObj.transform.position = StatuePos[num];
                    }
                    else
                    {
                        playerObj = Instantiate(securityPrefab);
                        GameResultManager.Instance.SecurityCount += 1;
                        int num = Random.Range(0, 2);
                        while (checkSecPos[num] == 1)
                        {
                            num = Random.Range(0, 2);
                        }
                        playerObj.transform.position = SecurityPos[num];
                    }
                }

                if (GamePlayerInstance.connectionToClient == null)
                {
                    Debug.LogWarning($"[WaitLoad] player.connectionToClient is null for player ID {GamePlayerInstance.PlayerIdNumber}");
                    return;
                }

                NetworkServer.ReplacePlayerForConnection(GamePlayerInstance.connectionToClient, playerObj, ReplacePlayerOptions.KeepAuthority);

                PlayerController playerController = playerObj.GetComponent<PlayerController>();

                playerController.ConnectionID = GamePlayerInstance.ConnectionID;
                playerController.PlayerIdNumber = GamePlayerInstance.PlayerIdNumber;
                playerController.PlayerSteamId = GamePlayerInstance.PlayerSteamId;
                playerController.PlayerName = GamePlayerInstance.PlayerName;
            }

            //GamePlayers.Add(GamePlayerInstance);
        }
    }

    public void StartGame(string sceneName)
    {
        ServerChangeScene(sceneName);
    }
}
