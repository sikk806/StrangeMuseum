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

    public List<PlayerLobbyController> GamePlayers { get; } = new List<PlayerLobbyController>(); // Info of Players

    [SerializeField]
    private bool forceLocalMode = true; // 스팀 사용 아닐 떄 켜기.

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
        //SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 스팀에 로그인 되어있는 경우에만 사용 가능.
    // 스팀에 로그아웃 되어있는 경우에 테스트 할 것 구현 필요
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name == "NetworkTest")
        {
            PlayerLobbyController GamePlayerInstance = Instantiate(GamePlayerPrefab);
            GamePlayerInstance.transform.position = Vector3.zero;

            GamePlayerInstance.ConnectionID = conn.connectionId;
            GamePlayerInstance.PlayerIdNumber = GamePlayers.Count + 1;
            //GamePlayerInstance.PlayerSteamId = (ulong)SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)SteamLobby.Instance.CurrentLobbyID, GamePlayers.Count);

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

                int randomPrefab = Random.Range(0, 1);
                if (GamePlayerInstance.PlayerIdNumber-1 == 0)
                {
                    playerObj = Instantiate(statuePrefab);
                }
                else if (GamePlayerInstance.PlayerIdNumber-1 == 1)
                {
                    playerObj = Instantiate(securityPrefab);
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

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Enter The Scene Loaded Function");
        if (scene.name == "NetworkTest")
        {
            StartCoroutine(WaitLoad(mode));
            return;
            foreach (var player in GamePlayers)
            {
                Debug.Log("Player : " + player.name);
                NetworkServer.DestroyPlayerForConnection(player.connectionToClient);
                GameObject playerObj = null;

                int randomPrefab = Random.Range(1, 2);
                if (randomPrefab == 0)
                {
                    playerObj = Instantiate(statuePrefab);
                }
                else if (randomPrefab == 1)
                {
                    playerObj = Instantiate(securityPrefab);
                }
                NetworkServer.AddPlayerForConnection(player.connectionToClient, playerObj);

                PlayerController playerController = playerObj.GetComponent<PlayerController>();

                playerController.ConnectionID = player.ConnectionID;
                playerController.PlayerIdNumber = player.PlayerIdNumber;
                playerController.PlayerSteamId = player.PlayerSteamId;
                playerController.PlayerName = player.PlayerName;
            }
        }
    }

    IEnumerator WaitLoad(LoadSceneMode mode)
    {
        yield return new WaitForSeconds(1.0f);

        int i = 0;
        foreach (var player in GamePlayers)
        {
            Debug.Log("i : " + i);
            Debug.Log("Player : " + player.PlayerIdNumber);
            GameObject playerObj = null;

            int randomPrefab = Random.Range(0, 1);
            if (randomPrefab == 0)
            {
                playerObj = Instantiate(statuePrefab);
            }
            else if (randomPrefab == 1)
            {
                playerObj = Instantiate(securityPrefab);
            }

            if (player.connectionToClient == null)
            {
                Debug.LogWarning($"[WaitLoad] player.connectionToClient is null for player ID {player.PlayerIdNumber}");
                continue;
            }

            NetworkServer.ReplacePlayerForConnection(player.connectionToClient, playerObj, ReplacePlayerOptions.KeepAuthority);

            PlayerController playerController = playerObj.GetComponent<PlayerController>();

            playerController.ConnectionID = player.ConnectionID;
            playerController.PlayerIdNumber = player.PlayerIdNumber;
            playerController.PlayerSteamId = player.PlayerSteamId;
            playerController.PlayerName = player.PlayerName;
        }
        Debug.Log("GamePlayers : " + GamePlayers.Count);
    }
}
