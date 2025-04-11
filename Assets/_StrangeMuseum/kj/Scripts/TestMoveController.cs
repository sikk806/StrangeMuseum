using Mirror;
using Mirror.Examples.Common;
using System.Threading;
using Unity.Android.Gradle.Manifest;
using UnityEngine;
using UnityEngine.Playables;

public class TestMoveController : NetworkBehaviour
{
    [Header("MovementSetting")]
    public float MovementSpeed = 5f;// default : 5f
    public float InitWalkingSpeed = 5f;// default : 5f
    public float JumpForce = 3f;
    public float Gravity = 9.8f;

    public void SetPlayerState(PlayerState state) { playerState = state; }
    public PlayerState GetPlayerState() { return playerState; }

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
    private PlayerState playerState;
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
        playerState = PlayerState.Idle;
        transform.Rotate(Vector3.zero);

        // Player ī�޶� ��������.
        playerCamera = Camera.main.transform;
        playerCamera.GetChild(0).gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked; // Ŀ�� �����
    }

    // Update is called once per frame
    void Update()
    {
    
        PlayerMovement();

        MouseMove();
    }

    void PlayerMovement()
    {
        // ī�޶�� ������ ������ �ٸ��� ������ �ڽ� Ŭ�������� ����
        // Move (Ű����)
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

        // Jump ���� ���δ� Layer / Tag ������ ������ ����
        if (!characterController.isGrounded)
        {
            moveVector.y -= Gravity * Time.deltaTime;
        }
        else
        {
            // Jump���� �����ϴ� ���� if������ ���� ��. > Idle ���·� �ٲ�. (isGrounded �� Jump state�� �ٸ� ���� ������ ������ �ߴ��� �ƴ����� �� �� ����)
            if (playerState == PlayerState.Jump)
            {
                playerState = PlayerState.Idle;
                if (animator) SetAnimTrigger("Idle");
            }
            // Jump �� Idle ������ ���� �����ϵ���
            else if (playerState == PlayerState.Idle || playerState == PlayerState.Run)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    moveVector.y = Mathf.Sqrt(JumpForce * Gravity);

                    playerState = PlayerState.Jump;
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
