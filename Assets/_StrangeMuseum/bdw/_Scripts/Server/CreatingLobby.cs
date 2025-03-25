using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using BdwLobby = Bdw.Lobby;
using UnityEngine;
using System;
using System.Collections.Generic;

public class CreatingLobby : MonoBehaviour
{
    [SerializeField]
    private BdwLobby bdwLobby;
    [SerializeField]
    private TMP_InputField gameNameField;

    public Lobby CurLobby { get; private set; }

    public async void CreateLobby()
    {
        try
        {
            Dictionary<ELobbyOptionKey, string> dataDic = new()
            {
                {ELobbyOptionKey.IsGameStarted,  "False"},
                {ELobbyOptionKey.JoinCode, "" }
            };

            (bool success, Lobby lobby) lobbyResult = await CreateLobby(dataDic);
            CloseCreatingLobby();
            bdwLobby.ShowLobby(lobbyResult.lobby.Id);
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    // <summary> isPublic is true, password is 8 ~ 64 characters. </summary>
    private async Awaitable<(bool success, Lobby lobby)> CreateLobby(Dictionary<ELobbyOptionKey, string> dataDic)
    {
        try
        {
            CreateLobbyOptions options = new CreateLobbyOptions()
            {
                IsPrivate = false,
                Player = GameNetworkManager.Instance.OptionPlayer,
                Data = GetLobbyOptionDic(dataDic)
            };

            CurLobby = await LobbyService.Instance.CreateLobbyAsync(gameNameField.text, 6, options);
            return (true, CurLobby);
        }
        catch (Exception e)
        {
            Debug.Log($"CreateLobby failed. {e}");
            return (false, null);
        }
    }

    private Dictionary<string, DataObject> GetLobbyOptionDic(Dictionary<ELobbyOptionKey, string> dataDic)
    {
        Dictionary<string, DataObject> resultDic = new();
        foreach (var (key, value) in dataDic)
        {
            resultDic[key.ToString()] = new DataObject(DataObject.VisibilityOptions.Public, value);
        }
        return resultDic;
    }

    public void ShowCreatingLobby()
    {
        gameObject.SetActive(true);
    }

    public void CloseCreatingLobby()
    {
        gameObject.SetActive(false);
        gameNameField.text = "";
    }
}
