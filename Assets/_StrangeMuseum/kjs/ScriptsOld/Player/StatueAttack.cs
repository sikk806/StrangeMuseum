using Mirror;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static CopyStatueCotroller;

// 석상 돌진 스킬과 관련된 클래스
public class StatueAttack : NetworkBehaviour
{
    [Header("AttackSetting")]
    //public float RushSpeed = 50f;  // default : 50f

    // public float InitRushSpeed;
    public float RushDuration = 0.2f;

    public GameObject DashVisualEffect;

    private bool isRush = false;
    Transform playerCamera;

    StatueController playerController;

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

    private void Start()
    {
        //initRushSpeed = RushSpeed;
        //playerCamera = Camera.main.transform;

        playerController = GetComponent<StatueController>();

        playerCamera = playerController.GetPlayerCamera();

        //playerCamera = playerController.GetPlayerCamera();


    }

    // Update is called once per frame

    [SerializeField]
    [SyncVar]
    bool isCopyStatue;

    private void Update()
    {

        if (!isOwned) return;
        //if(GetComponent<StatueController>().playerState.Value == PlayerState.Freeze) { return; }

        if (Input.GetKeyDown(KeyCode.LeftShift) && !isRush)
        {
            // Move, View Fix

            //GetComponent<StatueController>().SetPlayerStateServerRpc(PlayerState.Attack);
            StartCoroutine(Attack());

            isRush = true;

            Debug.Log("Statue Rush - Attack");
        }

        if(Input.GetKeyDown(KeyCode.Q) && isCopyStatue == false)
        {
            CopyStatueFunction();
        }
    }

    private void CopyStatueFunction()
    {
        CopyStatueCmd();
    }

    [SerializeField]
    GameObject CopyStatue;

    [Command(requiresAuthority = false)]
    private void CopyStatueCmd()
    {

        GameObject copyStatue = Instantiate(CopyStatue, transform.position, Quaternion.identity);

        NetworkServer.Spawn(copyStatue,connectionToClient);
        isCopyStatue = true;
    }

 

    private void OnTriggerEnter(Collider other)
    {
        if (!isOwned) return;
        if (other.GetComponent<SecurityController>() != null && isRush)
        {
            NetworkIdentity networkIdentity = other.GetComponent<NetworkIdentity>();
            CmdRequestReplace(networkIdentity);
            Debug.Log("HIT!!!!!!!");
        }
    }



    IEnumerator Attack()
    {
        float elapseTime = 0;
        Vector3 rushDirection = transform.forward;
        CharacterController characterController = GetComponent<CharacterController>();


        yield return new WaitForSeconds(0.1f);
        GameObject go = Instantiate(DashVisualEffect);
        go.transform.position = playerCamera.transform.position + playerCamera.transform.forward;
        go.transform.LookAt(playerCamera);
        while (elapseTime < RushDuration)
        {
            characterController.Move(rushDirection * GetComponent<StatueController>().RushSpeed * Time.deltaTime);
            elapseTime += Time.deltaTime;

            playerCamera.position = transform.position + transform.rotation * GetComponent<StatueController>().StatueCameraPosition;
            go.transform.position = playerCamera.transform.position + playerCamera.transform.forward + playerCamera.transform.up * 0.1f;
            yield return null;
        }
        Destroy(go);
        isRush = false;

        //GetComponent<StatueController>().SetPlayerStateServerRpc(PlayerState.Idle);
    }

    [Command]
    private void CmdRequestReplace(NetworkIdentity target)
    {
        NetworkConnectionToClient conn = target.connectionToClient;

        // 현재 PlayerController
        PlayerController playerController = target.GetComponent<PlayerController>();

        // 바뀔 PlayerController
        GameObject playerObj = null;
        playerObj = Instantiate(Manager.GetStatuePrefab());
        PlayerController newPlayerController = playerObj.GetComponent<PlayerController>();

        NetworkServer.ReplacePlayerForConnection(conn, playerObj, ReplacePlayerOptions.KeepAuthority);

        newPlayerController.ConnectionID = playerController.ConnectionID;
        newPlayerController.PlayerIdNumber = playerController.PlayerIdNumber;
        newPlayerController.PlayerSteamId = playerController.PlayerSteamId;
        newPlayerController.PlayerName = playerController.PlayerName;

        NetworkServer.Destroy(playerController.gameObject);
        GameResultManager.Instance.SetCharacterCount(-1, 1);
    }

}
