using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum Winner
{
    Security,
    HeadlessAngel
}

public class GameResult : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI resultText;
    [SerializeField]
    private TextMeshProUGUI messageText;

    private readonly string securityWinResult = "경비원 승리";
    private readonly string securityWinMessage = "조형물의 위협을 피해 무사히 탈출하였습니다.";
    private readonly string headlessAngelWinResult = "조형물 승리";
    private readonly string headlessAngelWinMessage = "경비원의 탈출을 성공적으로 저지하였습니다.";

    public void ShowPopup(Winner winner)
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        switch (winner)
        {
            case Winner.Security:
                resultText.text = securityWinResult;
                messageText.text = securityWinMessage;
                break;

            case Winner.HeadlessAngel:
                resultText.text = headlessAngelWinResult;
                messageText.text = headlessAngelWinMessage;
                break;
        }

        Time.timeScale = 0f;
        gameObject.SetActive(true);
    }

    public void OnClickLobbyBtn()
    {
        Time.timeScale = 1;
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("Lobby");
    }

    public void OnClickQuitBtn()
    {
        NetworkManager.Singleton.Shutdown();
        Application.Quit();
    }
}
