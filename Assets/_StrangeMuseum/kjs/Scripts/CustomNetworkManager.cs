using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.SceneManagement;
 
public class CustomNetworkManager : NetworkManager
{
    [SerializeField]
    //private PlayerObjectController GamePlayerPrefab;
 
    //public List<PlayerObjectController> GamePlayers { get; } = new List<PlayerObjectController>();
 
    //서버에 사람 추가될 때
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        //base.OnServerAddPlayer(conn);
        if(SceneManager.GetActiveScene().name == "Lobby")
        {
            // PlayerController GamePlayerInstance = Instantiate(GamePlayerPrefab);            
            // GamePlayerInstance.ConnectionID = conn.connectionId;
            // GamePlayerInstance.PlayerIdNumber = GamePlayers.Count + 1;
            // GamePlayerInstance.PlayerSteamID = (ulong)SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)SteamLobby.Instance.CurrentLobbyID, GamePlayers.Count);
 
            //NetworkServer.AddPlayerForConnection(conn, GamePlayerInstance.gameObject);
        }
    }
 
    //로비와 게임이 씬이 같지 않을 경우라면 사용
    public void StartGame()
    {                
        //ServerChangeScene(Constant.SCENE_GAME);
    }
 
    //클라이언트가 연결 끊겼을 때
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
    }
 
    //서버가 연결 끊겼을 때
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        base.OnServerDisconnect(conn);
    }
}
