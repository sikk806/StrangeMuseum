using Unity.Services.Vivox;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

public class StatueController : NetworkBehaviour
{
    //public Zone
    [Header("MovementSetting")]
    public NetworkVariable<float> MovementSpeed = new NetworkVariable<float>(5f, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);  // default : 5f

    public float InitMovementSpeed;
    public float JumpForce = 3f;
    public float Gravity = 9.8f;

    // Rush Speed 적용시키는 버전으로 업데이트 예정
    public float RushSpeed;

    // RushSpeed와 함께 조절
    public float initRushSpeed;

    [Header("\nCameraSetting")]
    public float MouseSensitivity = 2f; // 마우스 감도 조절
    public Vector3 StatueCameraPosition = new Vector3(0f, 1.85f, -0.6f); // Camera Default Position

    Transform playerCamera;
    public Transform GetPlayerCamera() { return playerCamera; }

    public void SetPlayerState(PlayerState state) { if(IsServer) SetPlayerStateServerRpc(state); }
    public PlayerState GetPlayerState() { return playerState.Value; }

    // private Zone
    private float moveX = 0, moveZ = 0;
    private float mouseX = 0;
    private float yaw = 0;
    private bool voiceOn = true;
    private bool canMove = true;

    private Vector3 moveVector;

    private Animator animator;
    //private PlayerState playerState;

    public NetworkVariable<PlayerState> playerState = new NetworkVariable<PlayerState>(PlayerState.Idle, NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);
    private CharacterController characterController;

    void Awake()
    {
        // GetComponent Section
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
    }

    private async void Start()
    {
        transform.Rotate(Vector3.zero);

        InitMovementSpeed = MovementSpeed.Value;
        initRushSpeed = RushSpeed;

        if (IsOwner)
        {
            SetPlayerStateServerRpc(PlayerState.Idle);
            playerCamera = Camera.main.transform;
            playerCamera.GetChild(0).gameObject.SetActive(false);

            // 채널 이름 넘기는 방법 고민해야함.
            await JoinVivoxChannel(PlayerPrefs.GetString("LobbyName"));
        }

        Cursor.lockState = CursorLockMode.Locked; // scw 추가
    }


    private async Task JoinVivoxChannel(string channelName)
    {
        await VivoxController.Instance.JoinVoiceChannel(channelName, gameObject);

        Debug.Log($"ID : {OwnerClientId} / S_Controller :  Vivox 채널 참가 완료!");
        Debug.Log($"ID : {OwnerClientId} / S_Controller :  Number Of Active Channels: " + VivoxService.Instance.ActiveChannels.Count);
        voiceOn = true;
    }

    // Update is called once per frame
    void Update()
    {
        // For Network Play
        if (!IsOwner) return;
        if (playerState.Value == PlayerState.Idle || playerState.Value == PlayerState.Run || playerState.Value == PlayerState.Jump)
        {
            PlayerMovement();

            if (!canMove)
            {
                canMove = true;
                UIManager.Instance.WhiteOutEffect(false);
            }
        }
        else if (playerState.Value == PlayerState.Freeze)
        {
            moveVector.x = 0;
            moveVector.z = 0;

            if (!characterController.isGrounded)
            {
                moveVector.y -= Gravity * Time.deltaTime; // 지면에 닿을 때까지 중력 적용
            }
            else
            {
                moveVector.y = 0;
            }

            characterController.Move(moveVector * Time.deltaTime);
            return;

            if (canMove)
            {
                canMove = false;
                UIManager.Instance.WhiteOutEffect(true);
            }
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            ToggleMicrophone();
        }
    }

    private void ToggleMicrophone()
    {
        voiceOn = VivoxController.Instance.ToggleMicrophone(voiceOn);
    }

    void PlayerMovement()
    {
        // View
        if (!IsOwner) return;
        mouseX = Input.GetAxis("Mouse X") * MouseSensitivity; // RightLeft

        transform.Rotate(Vector3.up * mouseX);
        playerCamera.Rotate(Vector3.up * mouseX);

        yaw += mouseX;

        // Move
        moveX = Input.GetAxis("Horizontal");
        moveZ = Input.GetAxis("Vertical");

        if (playerState.Value != PlayerState.Jump)
        {
            if (moveX < 0.1f && moveZ < 0.1f && moveX > -0.1f && moveZ > -0.1f)
            {
                if(IsServer)
                    SetPlayerStateServerRpc(PlayerState.Idle);
            }
            else
            {
                if(IsServer)
                    SetPlayerStateServerRpc(PlayerState.Run);
            }
        }

        if (animator)
        {
            animator.SetFloat("ForwardSpeed", moveX);
            animator.SetFloat("RightSpeed", moveZ);
        }

        Vector3 move = transform.right * moveX + transform.forward * moveZ;
        moveVector.x = move.x * MovementSpeed.Value;
        moveVector.z = move.z * MovementSpeed.Value;

        // Jump 가능 여부는 Layer / Tag 등으로 구분할 예정
        if (!characterController.isGrounded)
        {
            moveVector.y -= Gravity * Time.deltaTime;
        }
        else
        {
            // Jump에서 착지하는 순간 if문으로 들어가게 됨. > Idle 상태로 바꿈. (isGrounded 와 Jump state의 다른 점은 이전에 점프를 했는지 아닌지를 알 수 있음)
            if (playerState.Value == PlayerState.Jump)
            {
                SetPlayerStateServerRpc(PlayerState.Idle);
                if (animator) SetAnimTrigger("Idle");
            }
            // Jump 는 Idle 상태일 때만 가능하도록
            else if (playerState.Value == PlayerState.Idle || playerState.Value == PlayerState.Run)
            {
                if (Input.GetKeyDown(KeyCode.Space))
                {
                    moveVector.y = Mathf.Sqrt(JumpForce * Gravity);

                    SetPlayerStateServerRpc(PlayerState.Jump);
                    if (animator) SetAnimTrigger("Jump");
                }
            }
        }

        characterController.Move(moveVector * Time.deltaTime);

        playerCamera.localRotation = Quaternion.Euler(20f, yaw, 0f);
        playerCamera.position = transform.position + transform.rotation * StatueCameraPosition;
    }

    public void SetAnimTrigger(string Value)
    {
        animator.SetTrigger(Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetPlayerStateServerRpc(PlayerState state)
    {
        playerState.Value = state;
    }
}
