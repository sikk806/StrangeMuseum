using UnityEngine;
using Mirror;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Steamworks;

public class SMNetworkManager : NetworkManager
{
    [SerializeField] private PlayerLobbyController GamePlayerPrefab;
    [SerializeField] private GameObject securityPrefab;
    [SerializeField] private GameObject statuePrefab;

    public List<PlayerLobbyController> GamePlayers { get; } = new List<PlayerLobbyController>(); // Info of Players

    protected void Start()
    {
        //SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name == "NetworkTest")
        {
            GameObject GamePlayerInstance = Instantiate(securityPrefab);

            PlayerController playerController = GamePlayerInstance.GetComponent<PlayerController>();

            playerController.transform.position = Vector3.zero;

            playerController.ConnectionID = conn.connectionId;
            playerController.PlayerIdNumber = GamePlayers.Count + 1;
            //GamePlayerInstance.PlayerSteamId = (ulong)SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)SteamLobby.Instance.CurrentLobbyID, GamePlayers.Count);
            if (SteamManager.Initialized && SteamLobby.Instance != null)
            {
                playerController.PlayerSteamId = (ulong)SteamMatchmaking.GetLobbyMemberByIndex(
                    (CSteamID)SteamLobby.Instance.CurrentLobbyID, GamePlayers.Count);
            }
            else
            {
                Debug.Log("Steam not initialized or SteamLobby missing. Assigning dummy SteamID.");
                playerController.PlayerSteamId = 0; // fallback or dummy ID
            }

            NetworkServer.AddPlayerForConnection(conn, GamePlayerInstance.gameObject);
        }
    }

    public void StartGame(string sceneName)
    {
        ServerChangeScene(sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "NetworkTest")
        {
           foreach(var player in GamePlayers)
           {
               NetworkServer.DestroyPlayerForConnection(player.connectionToClient);
               GameObject playerObj = null;

                int randomPrefab = Random.Range(0, 2);
                if(randomPrefab == 0)
                {
                    playerObj = Instantiate(statuePrefab);
                }
                else if(randomPrefab == 1)
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
}
