using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyPlayerListItem : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI playerNameText;
    [SerializeField]
    private TextMeshProUGUI playerTypeText;

    public Player Player { get; private set; }

    public void SetLobbyPlayerListItem(Player player, string playerType)
    {
        Player = player;
        playerNameText.text = player.Data["PlayerName"].Value;
        playerTypeText.text = playerType;
    }
}
