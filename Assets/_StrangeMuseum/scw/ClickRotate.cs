using UnityEngine;

public class ClickRotate : MonoBehaviour
{
    private Transform rotatingTarget;
    private Quaternion targetRotation;
    private bool isRotating = false;
    private float rotateSpeed = 5f;

    // 회전 누적 각도 추적
    private float accumulatedRotation = 0f;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                rotatingTarget = hit.transform;

                // 회전 중이라면 현재 목표 각도로 기준 업데이트
                if (isRotating)
                {
                    // 현재 도달 중이던 목표 각도를 기준으로 누적
                    accumulatedRotation += 90f;
                }
                else
                {
                    // 현재 회전 상태에서 누적 시작
                    accumulatedRotation = Mathf.Round(rotatingTarget.eulerAngles.z / 90f) * 90f + 90f;
                }

                // 다음 목표 회전 지정 (Z축 기준)
                targetRotation = Quaternion.Euler(0f, 0f, accumulatedRotation % 360f);
                isRotating = true;
            }
        }

        if (isRotating && rotatingTarget != null)
        {
            rotatingTarget.rotation = Quaternion.Lerp(
                rotatingTarget.rotation,
                targetRotation,
                Time.deltaTime * rotateSpeed
            );

            if (Quaternion.Angle(rotatingTarget.rotation, targetRotation) < 0.1f)
            {
                rotatingTarget.rotation = targetRotation;
                isRotating = false;
            }
        }
    }
}
