using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Vivox;
using UnityEngine;

public enum PlayerState
{
    Idle,
    Run,
    Jump,
    Die,
    AttackBegin,
    Attack,
    Freeze
}

public class SecurityController : NetworkBehaviour
{
    //public Zone
    [Header("MovementSetting")]
    public NetworkVariable<float> MovementSpeed = new NetworkVariable<float>(5f, NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server); // default : 5f
    public float InitWalkingSpeed;
    public float JumpForce = 3f;
    public float Gravity = 9.8f;

    [Header("\nCameraSetting")]
    public float MouseSensitivity = 2f;
    public Vector3 SecurityCameraPosition = new Vector3(0, 1.36f, 0.15f);

    public void SetPlayerState(PlayerState state) { playerState = state; }
    public PlayerState GetPlayerState() { return playerState; }

    public GameObject CharacterMesh; // 본인 캐릭터 메쉬는 안보이도록 조정
    public Transform playerCamera;

    public GameObject FlashLight;//손전등

    public static Action<SecurityController> OnPlayerSpawn;
    public static Action<SecurityController> OnPlayerDespawn;

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
        InitWalkingSpeed = MovementSpeed.Value;
    }

    private async void Start()
    {
        playerState = PlayerState.Idle;
        transform.Rotate(Vector3.zero);

        if (IsOwner)
        {
            playerCamera = Camera.main.transform;
            playerCamera.GetChild(0).gameObject.SetActive(true);
            FlashLight.gameObject.SetActive(false);
            skinnedMeshRenderer = CharacterMesh.GetComponent<SkinnedMeshRenderer>();
            skinnedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;

            await JoinVivoxChannel(PlayerPrefs.GetString("LobbyName"));
        }

        Cursor.lockState = CursorLockMode.Locked; // scw 추가
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            OnPlayerSpawn?.Invoke(this);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            OnPlayerDespawn?.Invoke(this);
        }
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

        if (playerState == PlayerState.Idle || playerState == PlayerState.Run || playerState == PlayerState.Jump)
        {
            PlayerMovement();
        }
        else if (playerState == PlayerState.Attack)
        {

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
            if (playerState == PlayerState.Jump)
            {
                playerState = PlayerState.Idle;
                if (animator) SetAnimTrigger("Idle");
            }
            // Jump 는 Idle 상태일 때만 가능하도록
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

    void PlayerDie()
    {

    }

    public void SetAnimTrigger(string Value)
    {
        animator.SetTrigger(Value);
    }
}
