using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using BdwLobby = Bdw.Lobby;
using UnityEngine;
using Unity.Services.Authentication;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class LobbyList : MonoBehaviour
{
    [SerializeField]
    private LobbyListItem lobbyListItemPrefab;
    [SerializeField]
    private BdwLobby bdwLobby;
    [SerializeField]
    private Transform lobbyListContainer;
    [SerializeField]
    private TextMeshProUGUI playerIdText;

    private void Awake()
    {
        try
        {
            playerIdText.text =
                $"닉네임: {GameNetworkManager.Instance.OptionPlayer.Data["PlayerName"].Value}";

            RefreshLobbyList();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    public void OnClickRefreshLobbyListBtn()
    {
        RefreshLobbyList();
    }

    public void OnClickBackBtn()
    {
        AuthenticationService.Instance.SignOut(true);
        Destroy(SettingManager.Instance.gameObject);
        Destroy(NetworkManager.Singleton.gameObject);
        SceneManager.LoadScene("MainScene");
    }

    private async void RefreshLobbyList()
    {
        QueryLobbiesOptions options = new QueryLobbiesOptions();
        options.Count = 5;

        options.Filters = new List<QueryFilter>()
        {
            new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
        };

        options.Order = new List<QueryOrder>()
        {
            new QueryOrder(false, QueryOrder.FieldOptions.Created)
        };

        try
        {
            (bool success, List<Lobby> lobbies) lobbyResult = await GetListLobbies(options);
            if (lobbyResult.success)
            {
                int childCount = lobbyListContainer.childCount;
                for (int i = 0; i < childCount; i++)
                {
                    Destroy(lobbyListContainer.GetChild(i).gameObject);
                }

                foreach (Lobby lobby in lobbyResult.lobbies)
                {
                    Instantiate(lobbyListItemPrefab, lobbyListContainer).SetLobbyListItem(lobby, this, bdwLobby);
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    private async Awaitable<(bool success, List<Lobby> lobbies)> GetListLobbies(QueryLobbiesOptions options)
    {
        try
        {
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(options);
            List<Lobby> lobbies = queryResponse.Results;
            return (true, lobbies);
        }
        catch (Exception e)
        {
            Debug.Log($"GetListLobbies failed. {e}");
            return (false, null);
        }
    }

    public void ShowLobbyList()
    {
        gameObject.SetActive(true);
    }

    public void CloseLobbyList()
    {
        gameObject.SetActive(false);
    }
}
