using Mirror;
using Steamworks;
using UnityEngine;

public class PlayerLobbyController : NetworkBehaviour
{
    [SyncVar] public int ConnectionID;
    [SyncVar] public int PlayerIdNumber;
    [SyncVar] public ulong PlayerSteamId;
    [SyncVar(hook = nameof(PlayerNameUpdate))] public string PlayerName;
    [SyncVar(hook = nameof(PlayerReadyUpdate))] public bool Ready;

    private SMNetworkManager manager;

    private SMNetworkManager Manager
    {
        get
        {
            if(manager)
            {
                return manager;
            }
            return manager = SMNetworkManager.singleton as SMNetworkManager;
        }
    }

    private void Start()
    {
        Debug.Log($" ConnectionID : {ConnectionID}");

        DontDestroyOnLoad(this.gameObject);
    }

    private void PlayerReadyUpdate(bool oldValue, bool newValue)
    {
        if(isServer)
        {
            this.Ready = newValue;
        }
        if(isClient)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    [Command]
    private void CmdSetPlayerReady()
    {
        this.PlayerReadyUpdate(this.Ready, !this.Ready);
    }

    public void ChangeReady()
    {
        if(isOwned)
        {
            CmdSetPlayerReady();
        }
    }

    public override void OnStartAuthority()
    {
        CmdSetPlayerName(SteamFriends.GetPersonaName().ToString());
        gameObject.name = "LocalGamePlayer";
       // LobbyController.Instance.FindLocalPlayer();
      //  LobbyController.Instance.UpdateLobbyName();
    }

    public override void OnStartClient()
    {
        Manager.GamePlayers.Add(this);
     //   LobbyController.Instance.UpdateLobbyName();
        //LobbyController.Instance.UpdatePlayerList();
    }

    public override void OnStopClient()
    {
        Manager.GamePlayers.Remove(this);
        LobbyController.Instance.UpdatePlayerList();
    }

    [Command]
    private void CmdSetPlayerName(string PlayerName)
    {
        this.PlayerNameUpdate(this.PlayerName, PlayerName);
    }

    public void PlayerNameUpdate(string OldValue, string NewValue)
    {
        if(isServer)
        {
            this.PlayerName = NewValue;
        }
        if(isClient)
        {
            //LobbyController.Instance.UpdatePlayerList();
        }
    }

    public void CanStartGame(string sceneName)
    {
        if(isOwned)
        {
            CmdCanStartGame(sceneName);
        }
    }

    [Command]
    public void CmdCanStartGame(string sceneName)
    {
        manager.StartGame(sceneName);
    }
}
