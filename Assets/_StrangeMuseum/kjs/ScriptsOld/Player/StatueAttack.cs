using Mirror;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

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

    PlayerController playerController;

    private  void Start()
    {
        //initRushSpeed = RushSpeed;
        //playerCamera = Camera.main.transform;

        playerController = GetComponentInParent<StatueController>();
        playerCamera = playerController.GetPlayerCamera();

  
    }

    // Update is called once per frame
    private  void Update()
    {
   
        if(!isOwned) return;
        //if(GetComponent<StatueController>().playerState.Value == PlayerState.Freeze) { return; }

        if (Input.GetKeyDown(KeyCode.LeftShift) && !isRush)
        {
            // Move, View Fix
            
            //GetComponent<StatueController>().SetPlayerStateServerRpc(PlayerState.Attack);
            StartCoroutine(Attack());

            isRush = true;

            Debug.Log("Statue Rush - Attack");
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


}
