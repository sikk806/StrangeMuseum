using UnityEngine;

public class MissionAdmin : MonoBehaviour
{
    // 여기서 해줄거 팝업 관리
    public GameObject NoticeCanvas;    // 안내 팝업
    public GameObject CompleteCanvas;  // 완료 팝업
    public Transform PlayerTransform;  // 플레이어 위치
    public float visibleDistance = 5f; // 안내 팝업 표시 거리
    public float cancelDistance = 2f;  // 미션 취소 거리
    private MissionAct currentMission;

    private void Start()
    {
        NoticeCanvas.SetActive(false);
        CompleteCanvas.SetActive(false);
    }

    private void Update()
    {
        if (currentMission == null) return;

        float distance = Vector3.Distance(PlayerTransform.position, transform.position);

        if (currentMission.IsComplete)
        {
            NoticeCanvas.SetActive(false);
            CompleteCanvas.SetActive(true);
            UpdateCanvasPosition(CompleteCanvas);
            return;
        }

        if (distance <= visibleDistance)
        {
            NoticeCanvas.SetActive(true);
            UpdateCanvasPosition(NoticeCanvas);
        }
        else
        {
            NoticeCanvas.SetActive(false);
        }

        if (currentMission.IsActive && distance > cancelDistance)
        {
            currentMission.CancelMission();
        }
    }

    private void UpdateCanvasPosition(GameObject canvas)
    {
        canvas.transform.position = transform.position + Vector3.up * 1f;
        canvas.transform.LookAt(Camera.main.transform);
        canvas.transform.Rotate(0, 180, 0);
    }

    public void SetMission(MissionAct mission)
    {
        currentMission = mission;
    }
}
