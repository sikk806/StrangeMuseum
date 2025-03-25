using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Unity.Services.Lobbies.Models
{
    // User Custom enums
    public enum EPlayerOptionKey { PlayerName }
    public enum ELobbyOptionKey { IsGameStarted, JoinCode }
}

public class GameNetworkManager : MonoBehaviour
{
    public static GameNetworkManager Instance;

    private readonly string GeneratingNicknameUrl = "https://www.rivestsoft.com/nickname/getRandomNickname.ajax";

    public Player OptionPlayer { get; private set; }
    public string CurPlayerId { get; private set; }

    private Button loginButton;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(this);
        }
        else
        {
            Destroy(gameObject);
            GameObject.Find("GameNetworkManager")
                .GetComponent<GameNetworkManager>().SetLoginButtonEvent();
        }
    }

    public void SetLoginButtonEvent()
    {
        loginButton = GameObject.Find("GameStartButton").GetComponent<Button>();
        loginButton.onClick.RemoveAllListeners();
        loginButton.onClick.AddListener(LoginGuest);
    }

    public void OnClickLoginButton()
    {
        LoginGuest();
    }

    private async void LoginGuest()
    {
        (bool success, string playerId) loginResult = await TryLoginGuest();
        if (loginResult.success)
        {
            StartCoroutine("InitializeUserProfileCo");
            return;
        }
    }

    private async Awaitable<(bool success, string playerId)> TryLoginGuest()
    {
        try
        {
            await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
            CurPlayerId = AuthenticationService.Instance.PlayerId;
            return (true, CurPlayerId);
        }
        catch (Exception e)
        {
            Debug.Log($"SignIn failed. {e}");
            return (false, string.Empty);
        }
    }

    private IEnumerator InitializeUserProfileCo()
    {
        WWWForm dataForm = new WWWForm();
        dataForm.AddField("lang", "ko");

        using (UnityWebRequest request = UnityWebRequest.Post(GeneratingNicknameUrl, dataForm))
        {
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.Log("Failed to generate random nickname");
                yield break;
            }

            Dictionary<string, string> nicknameDic =
                JsonConvert.DeserializeObject<Dictionary<string, string>>(request.downloadHandler.text);

            Dictionary<EPlayerOptionKey, string> playerDic = new()
            {
                 { EPlayerOptionKey.PlayerName, nicknameDic["data"] },
            };

            SetOptionCurPlayer(playerDic);
            Debug.Log($"SignIn success! PlayerId:{CurPlayerId}, PlayerName:{nicknameDic["data"]}");

            SceneManager.LoadScene("Lobby");
        }
    }

    public void SetOptionCurPlayer(Dictionary<EPlayerOptionKey, string> playerDic)
    {
        Dictionary<string, PlayerDataObject> optionPlayerDic = new();
        foreach (var (key, value) in playerDic)
        {
            optionPlayerDic[key.ToString()] = new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, value);
        }
        OptionPlayer = new Player(id: CurPlayerId, data: optionPlayerDic);
    }
}
