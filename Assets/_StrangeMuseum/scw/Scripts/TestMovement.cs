using UnityEngine;

public class TestMovement : MonoBehaviour
{
    //public Zone
    [Header("MovementSetting")]
    public float MovementSpeed = 5f;
    public float InitWalkingSpeed;
    public float JumpForce = 3f;
    public float Gravity = 9.8f;

    [Header("\nCameraSetting")]
    public float MouseSensitivity = 2f;
    public Vector3 SecurityCameraPosition = new Vector3(0, 1.36f, 0.15f);

    public void SetPlayerState(PlayerState state) { playerState = state; }
    public PlayerState GetPlayerState() { return playerState; }

    public Transform playerCamera;

    // private Zone
    private float moveX = 0, moveZ = 0;
    private float mouseX = 0, mouseY = 0;
    private float pitch = 0, yaw = 0;
    private bool voiceOn = true;

    private string voiceChannelName;
    private Vector3 moveVector;

    private Animator animator;
    private PlayerState playerState;
    private CharacterController characterController;
    private PlayerInteraction playerInteraction; // 임무 오브젝트와 상호작용 기능 추가
    private SkinnedMeshRenderer skinnedMeshRenderer; // CharacterMesh 설정을 위함.

    void Awake()
    {
        // GetComponent Section
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        playerInteraction = GetComponent<PlayerInteraction>(); // scw 추가
        InitWalkingSpeed = MovementSpeed;
    }

    private async void Start()
    {
        playerState = PlayerState.Idle;
        transform.Rotate(Vector3.zero);

        playerCamera = Camera.main.transform;
        playerCamera.GetChild(0).gameObject.SetActive(true);
        skinnedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

        Cursor.lockState = CursorLockMode.Locked; // scw 추가
    }


    // Update is called once per frame
    void Update()
    {
        if (playerState == PlayerState.Idle || playerState == PlayerState.Run || playerState == PlayerState.Jump)
        {
            PlayerMovement();
        }
        else if (playerState == PlayerState.Attack)
        {

        }
    }


    void PlayerMovement()
    {
        //if (!GameManager.Instance.GetCanPlayerMove()) return; // 임무 진행 시 움직임 불가하도록 설정

        // View
        mouseX = Input.GetAxis("Mouse X") * MouseSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);
        playerCamera.Rotate(Vector3.up * mouseX);

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -30f, 30f);

        playerCamera.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        playerCamera.position = transform.position + transform.rotation * SecurityCameraPosition;

        // Move
        moveX = Input.GetAxis("Horizontal");
        moveZ = Input.GetAxis("Vertical");

        if (playerState != PlayerState.Jump)
        {
            if (moveX < 0.1f && moveZ < 0.1f && moveX > -0.1f && moveZ > -0.1f)
            {
                playerState = PlayerState.Idle;
            }
            else
            {
                playerState = PlayerState.Run;
            }
        }

        if (animator)
        {
            animator.SetFloat("ForwardSpeed", moveX);
            animator.SetFloat("RightSpeed", moveZ);
        }

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        moveVector.x = move.x * MovementSpeed;
        moveVector.z = move.z * MovementSpeed;

        // Jump 가능 여부는 Layer / Tag 등으로 구분할 예정
        if (!characterController.isGrounded)
        {
            moveVector.y -= Gravity * Time.deltaTime;
        }
        else
        {
            // Jump에서 착지하는 순간 if문으로 들어가게 됨. > Idle 상태로 바꿈. (isGrounded 와 Jump state의 다른 점은 이전에 점프를 했는지 아닌지를 알 수 있음)
            if (playerState == PlayerState.Jump)
            {
                playerState = PlayerState.Idle;
            }
            // Jump 는 Idle 상태일 때만 가능하도록
            else if (playerState == PlayerState.Idle || playerState == PlayerState.Run)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    moveVector.y = Mathf.Sqrt(JumpForce * Gravity);

                    playerState = PlayerState.Jump;
                }
            }
        }
        characterController.Move(moveVector * Time.deltaTime);
    }
}