using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ClickMission : MonoBehaviour
{
    public int ClickNum = 10; // 목표 클릭 수
    private int curNum = 0;
    private bool isMissionActive = false;
    private bool isComplete = false;

    public GameObject MiniGameCanvas;
    public GameObject NoticeCanvas;
    public TMP_Text ClickCounterText;
    public Button ClickButton;

    public Transform player; // 플레이어 (인스펙터에서 할당)
    public float interactionDistance = 1.5f; //  미션 시작 거리
    public float cancelDistance = 1f; //  일정 거리 벗어나면 취소

    private void Start()
    {
        MiniGameCanvas.SetActive(false);
        NoticeCanvas.SetActive(false);
        ClickButton.onClick.AddListener(OnClick);
    }

    private void Update()
    {
        if (isComplete || player == null) return;

        float distance = Vector3.Distance(player.position, transform.position);
        Debug.Log("거리: " + distance + " / 취소 기준: " + cancelDistance);

        //  안내 팝업 ON/OFF
        NoticeCanvas.SetActive(distance < interactionDistance);

        //  왼쪽 클릭 시 미션 시작
        if (Input.GetMouseButtonDown(0) && distance < interactionDistance && !isMissionActive && !isComplete)
        {
            StartMission();
        }

        //  ESC 키 or 일정 거리 초과 시 미션 취소
        if (isMissionActive && (Input.GetKeyDown(KeyCode.Escape) || distance > cancelDistance))
        {
            CancelMission();
            Debug.Log("미션 취소됨: 거리 초과");
            MiniGameCanvas.SetActive(false);
        }

    }

    private void StartMission()
    {
        MiniGameCanvas.SetActive(true);
        isMissionActive = true;
        UpdateClickCount();
    }

    private void OnClick()
    {
        if (!isMissionActive) return;

        curNum++;
        UpdateClickCount();

        if (curNum >= ClickNum)
        {
            CompleteMission();
        }
    }

    private void UpdateClickCount()
    {
        ClickCounterText.text = $"Click Count: {curNum} / {ClickNum}";
    }

    private void CancelMission()
    {
        isMissionActive = false;
        curNum = 0;  // 진행도 초기화
        UpdateClickCount(); 
        MiniGameCanvas.SetActive(false);
        Debug.Log("미션 취소됨");
    }

    private void CompleteMission()
    {
        isMissionActive = false;
        isComplete = true;
        MiniGameCanvas.SetActive(false);
        NoticeCanvas.SetActive(false);
    }
}
