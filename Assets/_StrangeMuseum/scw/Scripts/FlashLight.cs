using UnityEngine;

public class FlashLight : MonoBehaviour
{
    public Transform playerTransform; // 따라갈 플레이어 Transform
    public Vector3 offset = new Vector3(0, 2, 0); // 플레이어 기준 위치 오프셋
    public float positionSmoothTime = 0.2f;
    public float rotationSmoothSpeed = 5f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        // 위치 따라가기 (부드럽게)
        Vector3 targetPosition = playerTransform.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, positionSmoothTime);

        // 회전 따라가기 (상하 포함)
        Quaternion targetRotation = playerTransform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);

        // 조명 흔들림 효과
        Vector3 randomShake = Random.insideUnitSphere * 0.01f;
        transform.rotation *= Quaternion.Euler(randomShake);
    }
}
