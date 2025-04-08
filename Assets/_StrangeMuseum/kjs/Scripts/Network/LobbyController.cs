using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Steamworks;

public class LobbyController : MonoBehaviour
{
    public static LobbyController Instance;

    public Text LobbyNameText;

    public GameObject PlayerListViewContent;
    public GameObject PlayerListItemPrefab;
    public GameObject LocalPlayerObject;

    public PlayerLobbyController LocalPlayerLobbyController;

    public ulong CurrentLobbyID;
    public bool PlayerItemCreated = false;

    private List<PlayerListItem> playerListItems = new List<PlayerListItem>();

    // 코드 정리 후에 SMNetworkManger에서 관리하도록 할 예정
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

    private void Awake()
    {
        if(Instance == null) { Instance = this; }
    }

    public void UpdateLobbyName()
    {
        CurrentLobbyID = Manager.GetComponent<SteamLobby>().CurrentLobbyID;
        LobbyNameText.text = SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyID), "name");
    }

    public void UpdatePlayerList()
    {
        if(!PlayerItemCreated) { CreateHostPlayerItem(); }
        if(playerListItems.Count < Manager.GamePlayers.Count) { CreateClientPlayerItem(); }
        if(playerListItems.Count > Manager.GamePlayers.Count) { RemovePlayerItem(); }
        if(playerListItems.Count == Manager.GamePlayers.Count) { UpdatePlayerItem(); }
    }

    public void FindLocalPlayer()
    {
        LocalPlayerObject = GameObject.Find("LocalGamePlayer");
        LocalPlayerLobbyController = LocalPlayerObject.GetComponent<PlayerLobbyController>();
    }

    public void CreateHostPlayerItem()
    {
        foreach(PlayerLobbyController player in Manager.GamePlayers)
        {
            GameObject playerItem = Instantiate(PlayerListItemPrefab) as GameObject;
            PlayerListItem playerListItem = playerItem.GetComponent<PlayerListItem>();

            playerListItem.PlayerName = player.PlayerName;
            playerListItem.ConnectionID = player.ConnectionID;
            playerListItem.PlayerSteamID = player.PlayerSteamId;
            playerListItem.SetPlayerValues();

            playerItem.transform.SetParent(PlayerListViewContent.transform);
            playerItem.transform.localScale = Vector3.one;

            playerListItems.Add(playerListItem);
        }

        PlayerItemCreated = true;
        
    }

    public void CreateClientPlayerItem()
    {

    }

    public void UpdatePlayerItem()
    {

    }

    public void RemovePlayerItem()
    {

    }
}
