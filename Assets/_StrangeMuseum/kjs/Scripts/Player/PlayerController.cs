using System;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;


public enum PlayerState
{
    Idle,
    Run,
    Jump,
    Die,
    Attack,
    Freeze
}

[RequireComponent(typeof(NetworkAnimator))]
public class PlayerController : NetworkBehaviour
{
    //public Zone
    [Header("MovementSetting")]
    public float MovementSpeed = 5f;// default : 5f
    public float InitWalkingSpeed;// default : 5f

    public float JumpForce = 3f;
    public float Gravity = 9.8f;

    public void SetPlayerState(PlayerState state) { playerState = state; }
    public PlayerState GetPlayerState() { return playerState; }

    public Transform GetPlayerCamera() { return playerCamera; }

    // 이 부분은 플레이어가 스폰 됐을 때 사용할 것으로 예상. 필요 없다면 과감히 지울 것.
    // public static Action<SecurityController> OnPlayerSpawn;
    // public static Action<SecurityController> OnPlayerDespawn;

    // private Zone
    private float moveX = 0, moveZ = 0; // 이동 변수 (X : AD | Z : WS)

    private Vector3 moveVector;

    private Animator animator;
    private CharacterController characterController;
    //private PlayerInteraction playerInteraction; // 임무 오브젝트와 상호작용 기능 추가 > 합칠것

    // 상속을 위한 변수들
    protected PlayerState playerState;
    protected Transform playerCamera;

    protected virtual void Awake()
    {
        SceneManager.sceneLoaded += SettingCamera;
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        // PlayerInteraction 합쳐야 함.
    }

    protected virtual void Start()
    {
        // Player 기본 상태 세팅
        playerState = PlayerState.Idle;
        transform.Rotate(Vector3.zero);

        // Player 카메라 가져오기.
        playerCamera = Camera.main.transform;
        playerCamera.GetChild(0).gameObject.SetActive(true);

        InitWalkingSpeed = MovementSpeed;

        Debug.Log("커서 ");
        Cursor.lockState = CursorLockMode.Locked; // 커서 숨기기
    }

    // Update is called once per frame
    protected virtual void Update()
    {
        // 원래 PlayerMovement 있었던 자리.
    }

    protected virtual void PlayerMovement()
    {
        // 카메라는 경비원과 석상이 다르기 때문에 자식 클래스에서 진행
        // Move (키보드)
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

    private void SettingCamera(Scene scene, LoadSceneMode mode)
    {
        // Player 카메라 가져오기.
        playerCamera = Camera.main.transform;
        playerCamera.GetChild(0).gameObject.SetActive(true);

    }

    public void SetAnimTrigger(string Value)
    {
        animator.SetTrigger(Value);
    }
}
