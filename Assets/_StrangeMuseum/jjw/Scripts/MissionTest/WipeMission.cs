using UnityEngine;
using UnityEngine.EventSystems;

public class WipeMission : MonoBehaviour
{
    public GameObject MiniGameCanvas;
    public ScratchCard.ScratchCardMaskUGUI scratchCard;
    public Transform player;
    public float interactionRange = 5f;
    public float cancelRange = 2f;
    private bool isMissionActive = false;
    private bool isCompleted = false;

    private CanvasGroup canvasGroup;

    void Start()
    {
        scratchCard.OnWipeComplete += CompleteMission;
        canvasGroup = MiniGameCanvas.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = MiniGameCanvas.AddComponent<CanvasGroup>();
        }
        canvasGroup.blocksRaycasts = false;
    }

    void Update()
    {
        if (isCompleted) return;
        Debug.Log("미션 활성 상태: " + isMissionActive);

        float distance = Vector3.Distance(player.position, transform.position);

        if (EventSystem.current.IsPointerOverGameObject()) return;

        if (!isMissionActive && distance <= interactionRange && Input.GetMouseButtonDown(0))
        {
            StartMission();
        }

        if (isMissionActive && (Input.GetKeyDown(KeyCode.Escape) || distance > cancelRange))
        {
            CancelMission();
        }
    }

    void StartMission()
    {
        if (isCompleted)
        {
            Debug.Log("이미 완료된 미션이므로 실행되지 않음.");
            return;
        }
        isMissionActive = true;
        MiniGameCanvas.SetActive(true);
        canvasGroup.blocksRaycasts = true; // UI 클릭 가능
        Debug.Log("미니게임 시작");
    }

    void CancelMission()
    {
        isMissionActive = false;
        MiniGameCanvas.SetActive(false);
        canvasGroup.blocksRaycasts = false;
        Debug.Log("미션 취소됨");
    }

    void CompleteMission()
    {
        if (isCompleted) return;

        Debug.Log("미션 완료!");
        isMissionActive = false;
        isCompleted = true;
        MiniGameCanvas.SetActive(false);
        scratchCard.enabled = false;
        canvasGroup.blocksRaycasts = false;
    }
}