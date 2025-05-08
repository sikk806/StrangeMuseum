using UnityEngine;

public class SecurityItemController : MonoBehaviour
{
    public GameObject itemUI;  // 인게임 화면에 표시될 아이템 모델

    private bool isRotating = false;
    private float rotationSpeed = 5f; // 회전 속도

    public Transform itemUIContainer;  // UI에서 3D 오브젝트를 배치할 위치

    private void Start()
    {
        ShowItemInUI(itemUI);
    }

    public void ShowItemInUI(GameObject item)
    {
        item.SetActive(true);  // 아이템을 활성화하여 화면에 표시
        item.transform.position = itemUIContainer.position;  // UI 컨테이너의 위치에 배치
        item.transform.rotation = Quaternion.identity;  // 기본 회전 상태로 설정


    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0)) // 마우스 좌클릭으로 회전 시작
        {
            isRotating = true;
        }

        if (Input.GetMouseButtonUp(0)) // 마우스 버튼을 떼면 회전 멈추기
        {
            isRotating = false;
        }

        if (isRotating)
        {
            // 마우스 이동 방향에 따라 아이템 회전
            float rotationX = Input.GetAxis("Mouse X") * rotationSpeed;
            float rotationY = -Input.GetAxis("Mouse Y") * rotationSpeed;

            // 회전 적용
            itemUI.transform.Rotate(Vector3.up, rotationX, Space.World);  // X축 회전
            itemUI.transform.Rotate(Vector3.right, rotationY, Space.World); // Y축 회전
        }
    }
}
