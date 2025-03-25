using System;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using BdwLobby = Bdw.Lobby;
using UnityEngine;

public class LobbyListItem : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI lobbyNameText;
    [SerializeField]
    private TextMeshProUGUI playerCountText;

    private Lobby lobby;
    private BdwLobby bdwLobby;
    private LobbyList lobbyList;

    public void SetLobbyListItem(Lobby lobby, LobbyList lobbyList, BdwLobby bdwLobby)
    {
        this.lobby = lobby;
        this.lobbyList = lobbyList;
        this.bdwLobby = bdwLobby;
        lobbyNameText.text = lobby.Name;
        playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
    }

    public async void OnClickJoinBtn()
    {
        try
        {
            (bool success, Lobby lobby) lobbyResult = await JoinLobbyById(lobby.Id);
            if (lobbyResult.success)
            {
                lobbyList.CloseLobbyList();
                bdwLobby.ShowLobby(lobby.Id);
                return;
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    private async Awaitable<(bool success, Lobby lobby)> JoinLobbyById(string lobbyId)
    {
        try
        {
            JoinLobbyByIdOptions options = new()
            {
                Player = GameNetworkManager.Instance.OptionPlayer,
            };

            lobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId, options);
            return (true, lobby);
        }
        catch (Exception e)
        {
            Debug.Log($"JoinLobbyById failed. {e}");
            return (false, null);
        }
    }
}
