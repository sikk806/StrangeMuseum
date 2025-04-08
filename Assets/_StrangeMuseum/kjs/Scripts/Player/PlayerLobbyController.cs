using Mirror;
using UnityEngine;

public class PlayerLobbyController : NetworkBehaviour
{
    [SyncVar] public int ConnectionID;
    [SyncVar] public int PlayerIdNumber;
    [SyncVar] public ulong PlayerSteamId;
    [SyncVar(hook = nameof(PlayerNameUpdate))] public string PlayerName;

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

    public void PlayerNameUpdate(string OldValue, string NewValue)
    {

    }
}
