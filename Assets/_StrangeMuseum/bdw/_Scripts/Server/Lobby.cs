using Unity.Services.Lobbies;
using UgsLobby = Unity.Services.Lobbies.Models.Lobby;
using UnityEngine;
using TMPro;
using System;
using Unity.Services.CloudCode.GeneratedBindings;
using Unity.Services.CloudCode;
using System.Collections.Concurrent;
using Unity.Services.Lobbies.Models;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Relay.Models;
using Unity.Services.Relay;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Bdw
{
    public class Lobby : MonoBehaviour
    {
        [SerializeField]
        private LobbyList lobbyList;
        [SerializeField]
        private LobbyPlayerListItem lobbyPlayerListItemPrefab;
        [SerializeField]
        private Transform lobbyPlayerListContainer;
        [SerializeField]
        private TextMeshProUGUI lobbyNameText;
        [SerializeField]
        private Button playButton;
        [SerializeField]
        private TextMeshProUGUI playerCountText;

        private UgsLobby curLobby;
        private LobbyEventCallbacks lobbyEvents;
        public ConcurrentQueue<string> createdLobbyIds = new();

        private List<LobbyPlayerListItem> lobbyPlayerList;
        private ILobbyEvents lobbyEventSubscription;

        private float heartbeatTimer;
        private float heartbeatTimerMax = 15;
        private bool isProcessingGame = false;
        private string joinCode;

        private void Update()
        {
            HandleLobbyHeartbeat();

            if (bool.Parse(curLobby.Data["IsGameStarted"].Value) && !isProcessingGame)
            {
                isProcessingGame = true;
                PlayerPrefs.SetString("LobbyName", curLobby.Name);
                if (curLobby.HostId == GameNetworkManager.Instance.CurPlayerId)
                {
                    CreateGame();
                }
                else
                {
                    JoinGame();
                }
            }

            if (curLobby.HostId == GameNetworkManager.Instance.CurPlayerId)
            {
                playButton.gameObject.SetActive(true);
                //임시 테스트로 2, 추후에는 방 만들 떄의 최디 인원으로 변경
                if (curLobby.Players.Count > 1)
                {
                    playButton.interactable = true;
                }
                else
                {
                    playButton.interactable = false;
                }
            }
            else
            {
                playButton.gameObject.SetActive(false);
            }

            playerCountText.text = $"참가자 수: {curLobby.Players.Count}/{curLobby.MaxPlayers}";
        }

        private async void CreateGame()
        {
            try
            {
                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(6);
                joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                    allocation.RelayServer.IpV4, (ushort)allocation.RelayServer.Port, allocation.AllocationIdBytes,
                    allocation.Key, allocation.ConnectionData);

                NetworkManager.Singleton.StartHost();

                curLobby.Data["JoinCode"] = new DataObject(DataObject.VisibilityOptions.Public, joinCode);
                UpdateLobbyOptions options = new UpdateLobbyOptions()
                {
                    Data = curLobby.Data
                };

                await LobbyService.Instance.UpdateLobbyAsync(curLobby.Id, options);

                Debug.Log($"Started Relay Server with {joinCode}");
                NetworkManager.Singleton.SceneManager.LoadScene("PlayScene", LoadSceneMode.Single);
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        private async void JoinGame()
        {
            try
            {
                joinCode = curLobby.Data["JoinCode"].Value;
                JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                    joinAllocation.RelayServer.IpV4, (ushort)joinAllocation.RelayServer.Port, joinAllocation.AllocationIdBytes,
                    joinAllocation.Key, joinAllocation.ConnectionData, joinAllocation.HostConnectionData);

                NetworkManager.Singleton.StartClient();

                Debug.Log($"Joined Relay with {joinCode}");
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }

        private async void HandleLobbyHeartbeat()
        {
            if (curLobby != null && curLobby.HostId == GameNetworkManager.Instance.CurPlayerId)
            {
                heartbeatTimer -= Time.deltaTime;
                if (heartbeatTimer < 0)
                {
                    heartbeatTimer = heartbeatTimerMax;
                    await LobbyService.Instance.SendHeartbeatPingAsync(curLobby.Id);
                }
            }
        }

        public async void ShowLobby(string lobbyId)
        {
            try
            {
                curLobby = await LobbyService.Instance.GetLobbyAsync(lobbyId);
                await SubscribeLobbyEvent(lobbyId);
                createdLobbyIds.Enqueue(curLobby.Id);
                lobbyNameText.text = curLobby.Name;

                var binding = new PlayerDataManagerBindings(CloudCodeService.Instance);

                foreach (Transform item in lobbyPlayerListContainer)
                {
                    Destroy(item.gameObject);
                }

                lobbyPlayerList = new();
                foreach (Player player in curLobby.Players)
                {
                    string playerType = curLobby.HostId == player.Id ? "방장" : "플레이어";
                    LobbyPlayerListItem item = Instantiate(lobbyPlayerListItemPrefab, lobbyPlayerListContainer);
                    item.SetLobbyPlayerListItem(player, playerType);
                    lobbyPlayerList.Add(item);
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }

            gameObject.SetActive(true);
        }

        public async void OnClickBackBtn()
        {
            await lobbyEventSubscription.UnsubscribeAsync();
            await LobbyService.Instance.RemovePlayerAsync(curLobby.Id, GameNetworkManager.Instance.CurPlayerId);
            CloseLobby();
            lobbyList.ShowLobbyList();
        }

        private void CloseLobby()
        {
            gameObject.SetActive(false);
        }

        private async Awaitable<bool> SubscribeLobbyEvent(string lobbyId)
        {
            try
            {
                lobbyEvents = new LobbyEventCallbacks();
                lobbyEvents.LobbyChanged += OnLobbyChanged;
                lobbyEvents.LobbyEventConnectionStateChanged += OnLobbyEventConnectionStateChanged;
                lobbyEventSubscription = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyId, lobbyEvents);
                Debug.Log($"SubscribeLobbyEvent success");
                return true;
            }
            catch (Exception e)
            {
                Debug.Log($"SubscribeLobbyEvent failed. {e}");
                return false;
            }
        }

        private void OnLobbyChanged(ILobbyChanges changes)
        {
            if (changes.LobbyDeleted)
            {
                Debug.Log($"Lobby deleted");
                curLobby = null;
                lobbyEvents = new LobbyEventCallbacks();
            }
            else
            {
                Debug.Log($"Lobby changed");

                if (changes.PlayerJoined.Value != null)
                {
                    foreach (LobbyPlayerJoined player in changes.PlayerJoined.Value)
                    {
                        string playerType = curLobby.HostId == player.Player.Id ? "방장" : "플레이어";
                        LobbyPlayerListItem item = Instantiate(lobbyPlayerListItemPrefab, lobbyPlayerListContainer);
                        item.SetLobbyPlayerListItem(player.Player, playerType);
                        lobbyPlayerList.Add(item);
                    }
                }

                if (changes.PlayerLeft.Value != null)
                {
                    foreach (int playerNumber in changes.PlayerLeft.Value)
                    {
                        Destroy(lobbyPlayerListContainer.GetChild(playerNumber).gameObject);

                        if (curLobby.HostId == lobbyPlayerList[playerNumber].Player.Id)
                        {
                            LobbyPlayerListItem newHost = lobbyPlayerList.FirstOrDefault(
                                item => item.Player.Id == changes.HostId.Value);
                            newHost.SetLobbyPlayerListItem(newHost.Player, "방장");
                        }

                        lobbyPlayerList.RemoveAt(playerNumber);
                    }
                }
            }

            changes.ApplyToLobby(curLobby);
        }

        private void OnLobbyEventConnectionStateChanged(LobbyEventConnectionState state)
        {
            Debug.Log($"LobbyEventConnectionStateChanged: {state}");
        }

        public void OnClickStartButton()
        {
            if (curLobby.HostId == GameNetworkManager.Instance.CurPlayerId)
            {
                playButton.interactable = false;
                curLobby.Data["IsGameStarted"] = new DataObject(DataObject.VisibilityOptions.Public, "True");
            }
        }
    }
}
