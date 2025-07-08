using UnityEngine;

public class FlashLight : MonoBehaviour
{
    public Transform playerTransform; // 따라갈 플레이어 Transform
    public Vector3 offset = new Vector3(0, 2, 0); // 플레이어 기준 위치 오프셋
    public float positionSmoothTime = 0.2f;
    public float rotationSmoothSpeed = 5f;

    private Vector3 velocity = Vector3.zero;


    public Transform flickerTarget;         // 껐다 켰다 할 자식 오브젝트
    public float minFlickerInterval = 0.1f; // 최소 간격
    public float maxFlickerInterval = 0.5f; // 최대 간격
    public bool flickerEnabled = true;      // flicker on/off toggle

    private float timer;
    private float nextFlickerTime;

    void Start()
    {
        ScheduleNextFlicker();
    }

    void Update()
    {
        if (!flickerEnabled || flickerTarget == null) return;

        timer += Time.deltaTime;
        if (timer >= nextFlickerTime)
        {
            // 토글 on/off
            flickerTarget.gameObject.SetActive(!flickerTarget.gameObject.activeSelf);
            timer = 0f;
            ScheduleNextFlicker();
        }
    }

    void ScheduleNextFlicker()
    {
        nextFlickerTime = Random.Range(minFlickerInterval, maxFlickerInterval);
    }

    void LateUpdate()
    {
        // 위치 따라가기 (부드럽게)
        Vector3 targetPosition = playerTransform.position + offset;
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, positionSmoothTime);

        // 회전 따라가기 (상하 포함)
        Quaternion targetRotation = playerTransform.rotation;
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSmoothSpeed * Time.deltaTime);

        // 조명 흔들림 효과
        //Vector3 randomShake = Random.insideUnitSphere * 0.01f;
        //transform.rotation *= Quaternion.Euler(randomShake);
    }
}
