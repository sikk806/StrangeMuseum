using Mirror;
using Unity.VisualScripting;
using UnityEngine;

public class StatueController : PlayerController
{
    private SMNetworkManager manager;

    private SMNetworkManager Manager
    {
        get
        {
            if (manager)
            {
                return manager;
            }
            return manager = SMNetworkManager.singleton as SMNetworkManager;
        }
    }



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

    public GameObject GetPlayerCamera() { return cam; }

    // 상속을 위한 변수들

    [SerializeField]
    GameObject StauteCamera;

    GameObject cam;
    public override void OnStartLocalPlayer()
    {
        if (!isOwned) return;

        base.OnStartLocalPlayer();

        initRushSpeed = RushSpeed;
    }
    protected override void Start()
    {
        if (!isOwned) return;

        base.Start();
        cam = GameObject.FindWithTag("MainCamera");
        //cam = Instantiate(StauteCamera, transform.position, Quaternion.identity);
        foreach (Transform child in cam.transform)
        {
            child.gameObject.SetActive(false);
        }
        
    }

    // Update is called once per frame
    protected override void Update()
    {
        // For Network Play
        if (!isLocalPlayer) return;
        if (GameResultManager.Instance.IsGamePaused == true) return;
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
        cam.transform.Rotate(Vector3.up * mouseX);

        yaw += mouseX;

        cam.transform.localRotation = Quaternion.Euler(20f, yaw, 0f);
        cam.transform.position = transform.position + transform.rotation * StatueCameraPosition;
    }


    [Command]
    private void CmdRequestReplace(NetworkIdentity target)
    {
        NetworkConnectionToClient conn = target.connectionToClient;
        if (conn == null) return;

        // 현재 PlayerController
        GameObject oldPlayerObject = target.gameObject;
        PlayerController oldPlayerController = oldPlayerObject.GetComponent<PlayerController>();

        Vector3 spawnPosition = oldPlayerObject.transform.position;
        Quaternion spawnRotation = oldPlayerObject.transform.rotation;

        // New StatueController
        GameObject newPlayerObject = Instantiate(Manager.GetStatuePrefab(), spawnPosition, spawnRotation);
        PlayerController newPlayerController = newPlayerObject.GetComponent<PlayerController>();

        newPlayerController.ConnectionID = oldPlayerController.ConnectionID;
        newPlayerController.PlayerIdNumber = oldPlayerController.PlayerIdNumber;
        newPlayerController.PlayerSteamId = oldPlayerController.PlayerSteamId;
        newPlayerController.PlayerName = oldPlayerController.PlayerName;

        NetworkServer.ReplacePlayerForConnection(conn, newPlayerObject, true);

        ///NetworkServer.Destroy(oldPlayerController.gameObject);
        GameResultManager.Instance.SetCharacterCount(-1, 1);
    }



    private void OnTriggerEnter(Collider other) //조각상 경비원 충돌처리
    {
        if (!isOwned) return;
        if (other.GetComponent<SecurityController>() != null)
        {
            NetworkIdentity networkIdentity = other.GetComponent<NetworkIdentity>();
            CmdRequestReplace(networkIdentity);
            Debug.Log("HIT!!!!!!!");
        }
    }
}
