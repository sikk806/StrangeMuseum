using UnityEngine;

public class MoveTest : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 2f;

    private float rotationX = 0f;
    private bool isMiniGameActive = false; // 미니게임 실행 여부
    private static bool isAnyMissionActive = false; // 다른 미션이 실행 중인지 확인

    void Start()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

    }

    void Update()
    {
        // 이동 처리
        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        transform.position += move * moveSpeed * Time.deltaTime;

        // 미니게임 중이 아닐 때만 마우스 회전 허용
        if (!isMiniGameActive)
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            transform.Rotate(Vector3.up * mouseX);
            Camera.main.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        }
    }

    // 미니게임 시작
    public bool StartMiniGame()
    {
        if (isAnyMissionActive) return false; // 다른 미션이 실행 중이면 시작 불가

        isMiniGameActive = true;
        isAnyMissionActive = true;
        return true;
    }

    // 미니게임 종료
    public void EndMiniGame()
    {
        isMiniGameActive = false;
        isAnyMissionActive = false;
    }
}
