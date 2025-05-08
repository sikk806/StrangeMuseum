using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mirror;
using Steamworks;
using System.Linq;
using TMPro;
using UnityEngine.SceneManagement;

public class LobbyController : MonoBehaviour
{
    public static LobbyController Instance;

    public TMP_Text LobbyNameText;

    public GameObject PlayerListViewContent;
    public GameObject PlayerListItemPrefab;
    public GameObject LocalPlayerObject;

    public PlayerLobbyController LocalPlayerLobbyController;

    public Button GameStartButton;
    public TMP_Text ReadyButtonText;

    public ulong CurrentLobbyID;
    public bool PlayerItemCreated = false;

    private List<PlayerListItem> playerListItems = new List<PlayerListItem>();

    // 코드 정리 후에 SMNetworkManger에서 관리하도록 할 예정
    private SMNetworkManager manager;

    private SMNetworkManager Manager
    {
        get
        {
            if (manager)
            {
                return manager;
            }
            return manager = SMNetworkManager.singleton as SMNetworkManager;
        }
    }

    private void Awake()
    {
        if (Instance == null) { Instance = this; }
    }

    public void ReadyPlayer()
    {
        LocalPlayerLobbyController.ChangeReady();   
    }

    public void UpdateButton()
    {
        if (LocalPlayerLobbyController.Ready)
        {
            ReadyButtonText.text = "준비 해제";
        }
        else
        {
            ReadyButtonText.text = "준비";
        }
    }

    public void CheckIfAllReady()
    {
        bool AllReady = false;

        foreach (PlayerLobbyController player in Manager.GamePlayers)
        {
            if (player.Ready)
            {
                AllReady = true;
            }
            else
            {
                AllReady = false;
                break;
            }
        }

        if (AllReady)
        {
            if (LocalPlayerLobbyController.PlayerIdNumber == 1)
            {
                GameStartButton.interactable = true;
            }
            else
            {
                GameStartButton.interactable = false;
            }
        }
        else
        {
            GameStartButton.interactable = false;
        }
    }

    public void ChangeGameScene()
    {
        Debug.Log("Start The Game!!");
    }

    public void UpdateLobbyName()
    {
        CurrentLobbyID = Manager.GetComponent<SteamLobby>().CurrentLobbyID;
        LobbyNameText.text = SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyID), "name");
    }

    public void UpdatePlayerList()
    {
        if (!PlayerItemCreated) { CreateHostPlayerItem(); }
        if (playerListItems.Count < Manager.GamePlayers.Count) { CreateClientPlayerItem(); }
        if (playerListItems.Count > Manager.GamePlayers.Count) { RemovePlayerItem(); }
        if (playerListItems.Count == Manager.GamePlayers.Count) { UpdatePlayerItem(); }
    }

    public void FindLocalPlayer()
    {
        LocalPlayerObject = GameObject.Find("LocalGamePlayer"); // Have To Same String as PlayerLobbyController
        LocalPlayerLobbyController = LocalPlayerObject.GetComponent<PlayerLobbyController>();
    }

    public void CreateHostPlayerItem()
    {
        foreach (PlayerLobbyController player in Manager.GamePlayers)
        {
            GameObject playerItem = Instantiate(PlayerListItemPrefab) as GameObject;
            PlayerListItem playerListItem = playerItem.GetComponent<PlayerListItem>();

            playerListItem.PlayerName = player.PlayerName;
            playerListItem.ConnectionID = player.ConnectionID;
            playerListItem.PlayerSteamID = player.PlayerSteamId;
            playerListItem.Ready = player.Ready;
            playerListItem.SetPlayerValues();

            playerItem.transform.SetParent(PlayerListViewContent.transform);
            playerItem.transform.localScale = Vector3.one;

            playerListItems.Add(playerListItem);
        }

        PlayerItemCreated = true;

    }

    public void CreateClientPlayerItem()
    {
        foreach (PlayerLobbyController player in Manager.GamePlayers)
        {
            if (!playerListItems.Any(b => b.ConnectionID == player.ConnectionID))
            {
                GameObject playerItem = Instantiate(PlayerListItemPrefab) as GameObject;
                PlayerListItem playerListItem = playerItem.GetComponent<PlayerListItem>();

                playerListItem.PlayerName = player.PlayerName;
                playerListItem.ConnectionID = player.ConnectionID;
                playerListItem.PlayerSteamID = player.PlayerSteamId;
                playerListItem.Ready = player.Ready;
                playerListItem.SetPlayerValues();

                playerItem.transform.SetParent(PlayerListViewContent.transform);
                playerItem.transform.localScale = Vector3.one;

                playerListItems.Add(playerListItem);
            }
        }
    }

    public void UpdatePlayerItem()
    {
        foreach (PlayerLobbyController player in Manager.GamePlayers)
        {
            foreach (PlayerListItem playerListItem in playerListItems)
            {
                if (playerListItem.ConnectionID == player.ConnectionID)
                {
                    playerListItem.PlayerName = player.PlayerName;
                    playerListItem.Ready = player.Ready;
                    playerListItem.SetPlayerValues();

                    if (player == LocalPlayerLobbyController)
                    {
                        UpdateButton();
                    }
                }
            }
        }

        CheckIfAllReady();
    }

    public void RemovePlayerItem()
    {
        List<PlayerListItem> playerListItemsToRemove = new List<PlayerListItem>();

        foreach (PlayerListItem playerListItem in playerListItems)
        {
            if (!Manager.GamePlayers.Any(b => b.ConnectionID == playerListItem.ConnectionID))
            {
                playerListItemsToRemove.Add(playerListItem);
            }
        }

        if (playerListItemsToRemove.Count > 0)
        {
            foreach (PlayerListItem playerListItem in playerListItemsToRemove)
            {
                GameObject objectToRemove = playerListItem.gameObject;
                playerListItems.Remove(playerListItem);
                Destroy(objectToRemove);
                objectToRemove = null;
            }
        }
    }

    public void StartGame(string sceneName)
    {
        LocalPlayerLobbyController.CanStartGame(sceneName);
    }
}
