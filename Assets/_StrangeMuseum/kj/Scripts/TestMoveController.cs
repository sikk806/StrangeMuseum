using System.Threading;
using Unity.Android.Gradle.Manifest;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Playables;

public enum TestPlayerState
{
    Idle,
    Run,
    Jump,
    Die,
    Attack,
    Freeze,
    ItemView,
}

public class TestMoveController : NetworkBehaviour
{
    [Header("MovementSetting")]
    public float MovementSpeed = 5f;// default : 5f
    public float InitWalkingSpeed = 5f;// default : 5f
    public float JumpForce = 3f ;
    public float Gravity = 9.8f;

    public void SetPlayerState(TestPlayerState state) { playerState = state; }
    public TestPlayerState GetPlayerState() { return playerState; }

    public Transform GetPlayerCamera() { return playerCamera; }

    // �� �κ��� �÷��̾ ���� ���� �� ����� ������ ����. �ʿ� ���ٸ� ������ ���� ��.
    // public static Action<SecurityController> OnPlayerSpawn;
    // public static Action<SecurityController> OnPlayerDespawn;

    // private Zone
    private float moveX = 0, moveZ = 0; // �̵� ���� (X : AD | Z : WS)

    private Vector3 moveVector;

    private Animator animator;
    private CharacterController characterController;
    //private PlayerInteraction playerInteraction; // �ӹ� ������Ʈ�� ��ȣ�ۿ� ��� �߰� > ��ĥ��

    // ����� ���� ������
    public TestPlayerState playerState;
    private Transform playerCamera;

    [Header("\nCameraSetting")]
    public float MouseSensitivity = 2f;
    public Vector3 SecurityCameraPosition = new Vector3(0, 1.36f, 0.15f);

    public GameObject CharacterMesh; // ���� ĳ���� �޽��� �Ⱥ��̵��� ���� : SecurityController

    // private Zone
    private float mouseX = 0, mouseY = 0;
    private float pitch = 0, yaw = 0;

    void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();

        InitWalkingSpeed = MovementSpeed;
        // PlayerInteraction ���ľ� ��.
    }

    void Start()
    {

        if (IsOwner == false) { return; }

        playerState = TestPlayerState.Idle;
        transform.Rotate(Vector3.zero);

        // Player ī�޶� ��������.
        playerCamera = Camera.main.transform;
        playerCamera.GetChild(0).gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked; // Ŀ�� �����
    }

    // Update is called once per frame
    void Update()
    {
        if(IsOwner == false) { return; }

        Debug.Log("현재 상태" + playerState);

        // Freeze 상태일 때 이동 입력이 들어오면 Idle로 전환

        if (SecurityInGameUI.Instance.isItemFirstView == true) { return; }

        if (playerState == TestPlayerState.Freeze )
        {

            float h = Input.GetAxisRaw("Horizontal");
            float v = Input.GetAxisRaw("Vertical");

            if (IsMoveInputDetected())
            {
                Debug.Log("행동 정지 후 해제");
                SetPlayerState(TestPlayerState.Idle);
                return;
            }

            // Freeze 상태에서는 이동/ 마우스 회전 안 함
            return;
        }

        PlayerMovement();

        MouseMove();
    }

    private bool IsMoveInputDetected()
    {
        return Mathf.Abs(Input.GetAxisRaw("Horizontal")) >= 0.1f || Mathf.Abs(Input.GetAxisRaw("Vertical")) >= 0.1f;
    }
    void PlayerMovement()
    {
        // ī�޶�� ������ ������ �ٸ��� ������ �ڽ� Ŭ�������� ����
        // Move (Ű����)
        moveX = Input.GetAxis("Horizontal");
        moveZ = Input.GetAxis("Vertical");

        if (playerState != TestPlayerState.Jump)
        {
            if (moveX < 0.1f && moveZ < 0.1f && moveX > -0.1f && moveZ > -0.1f)
            {
                playerState = TestPlayerState.Idle;
            }
            else
            {
                playerState = TestPlayerState.Run;
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

        // Jump ���� ���δ� Layer / Tag ������ ������ ����
        if (!characterController.isGrounded)
        {
            moveVector.y -= Gravity * Time.deltaTime;
        }
        else
        {
            // Jump���� �����ϴ� ���� if������ ���� ��. > Idle ���·� �ٲ�. (isGrounded �� Jump state�� �ٸ� ���� ������ ������ �ߴ��� �ƴ����� �� �� ����)
            if (playerState == TestPlayerState.Jump)
            {
                playerState = TestPlayerState.Idle;
                if (animator) SetAnimTrigger("Idle");
            }
            // Jump �� Idle ������ ���� �����ϵ���
            else if (playerState == TestPlayerState.Idle || playerState == TestPlayerState.Run)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    moveVector.y = Mathf.Sqrt(JumpForce * Gravity);

                    playerState = TestPlayerState.Jump;
                    if (animator) SetAnimTrigger("Jump");
                }
            }
        }
        characterController.Move(moveVector * Time.deltaTime);
    }

    void MouseMove()
    {
        mouseX = Input.GetAxis("Mouse X") * MouseSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);
        playerCamera.Rotate(Vector3.up * mouseX);

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -30f, 30f);

        playerCamera.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        playerCamera.position = transform.position + transform.rotation * SecurityCameraPosition;
    }

    public void SetAnimTrigger(string Value)
    {
        animator.SetTrigger(Value);
    }
}
