using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class StatueController : PlayerController
{
    // Rush Speed 적용시키는 버전으로 업데이트 예정
    [SyncVar]
    public float RushSpeed = 5f;

    // RushSpeed와 함께 조절
    [SyncVar]
    public float initRushSpeed;

    [Header("\nCameraSetting")]
    public float MouseSensitivity = 2f; // 마우스 감도 조절
    public Vector3 StatueCameraPosition = new Vector3(0f, 1.85f, -0.6f); // Camera Default Position

    // private Zone
    private float mouseX = 0;
    private float yaw = 0;
    private bool voiceOn = true;
    private bool canMove = true;

    //private PlayerState playerState;

    protected override void Start()
    {
        if(!isOwned) return;

        base.Start();

        initRushSpeed = RushSpeed;
    }

    // Update is called once per frame
    protected override void Update()
    {
        // For Network Play
        if (!isLocalPlayer) return;
        if (playerState == PlayerState.Idle || playerState == PlayerState.Run || playerState == PlayerState.Jump)
        {
            PlayerMovement();

            if (!canMove)
            {
                canMove = true;
                UIManager.Instance.WhiteOutEffect(false);
            }
        }
        // Freeze 로직 개선 필요
        // else if (playerState == PlayerState.Freeze)
        // {
        //     moveVector.x = 0;
        //     moveVector.z = 0;

        //     if (!characterController.isGrounded)
        //     {
        //         moveVector.y -= Gravity * Time.deltaTime; // 지면에 닿을 때까지 중력 적용
        //     }
        //     else
        //     {
        //         moveVector.y = 0;
        //     }

        //     characterController.Move(moveVector * Time.deltaTime);
        //     return;

        //     if (canMove)
        //     {
        //         canMove = false;
        //         UIManager.Instance.WhiteOutEffect(true);
        //     }
        // }
    }

    protected override void PlayerMovement()
    {
        // View
        if (!isLocalPlayer) return;

        base.PlayerMovement();

        mouseX = Input.GetAxis("Mouse X") * MouseSensitivity; // RightLeft

        transform.Rotate(Vector3.up * mouseX);
        playerCamera.Rotate(Vector3.up * mouseX);

        yaw += mouseX;

        playerCamera.localRotation = Quaternion.Euler(20f, yaw, 0f);
        playerCamera.position = transform.position + transform.rotation * StatueCameraPosition;
    }
}
