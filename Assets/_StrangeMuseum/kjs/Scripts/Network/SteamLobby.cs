using UnityEngine;
using Mirror;
using Steamworks;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance;
    public GameObject HostButton;
    public ulong CurrentLobbyID;

    private SMNetworkManager networkManager;

    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> gameLobbyJoinRequested;
    protected Callback<LobbyEnter_t> lobbyEntered;

    private const string HostAddressKey = "HostAddress";

    private bool isSteamAvailable; // 혼자 작업할 때 필요한 것.

    private void Start()
    {
        networkManager = GetComponent<SMNetworkManager>();
        if (!Instance) { Instance = this; }

        if (!SteamManager.Initialized)
        {
            // Steam을 사용ㅇ하지 않는 경우 TelepathyTransport로 넘겨주기 위함
            var fallback = FindObjectOfType<TelepathyTransport>();
            if (fallback != null)
                Transport.active = fallback;
        }

        isSteamAvailable = SteamManager.Initialized;

        if (!isSteamAvailable)
        {
            Debug.Log("Steam is NOT initialized. Running in LOCAL TEST MODE.");
            return; // Steam 콜백 등록 생략
        }

        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
    }

    public void HostLobby()
    {
        HostButton.SetActive(false);

        if (!isSteamAvailable)
        {
            Debug.Log("Local host started (no Steam)");
            networkManager.networkAddress = "localhost";
            networkManager.StartHost();
            return;
        }

        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypeFriendsOnly, networkManager.maxConnections);
    }

    public void ConnectAsClient()
    {
        if (!isSteamAvailable)
        {
            Debug.Log("Local host started (no Steam)");
            networkManager.networkAddress = "localhost";
            networkManager.StartClient();
            return;
        }
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        // Check able to create Lobby. If Not show the hostButton Again
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            HostButton.SetActive(true);
            return;
        }

        networkManager.StartHost();

        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey, SteamUser.GetSteamID().ToString());
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CurrentLobbyID = callback.m_ulSteamIDLobby;
        if (NetworkServer.active) return;

        string hostAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey);

        networkManager.networkAddress = hostAddress;
        networkManager.StartClient();

    }
}
