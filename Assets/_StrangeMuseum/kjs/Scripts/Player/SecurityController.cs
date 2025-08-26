using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using static MiraController;
using static UnityEditor.VersionControl.Message;

[RequireComponent(typeof(NetworkAnimator))]
public class SecurityController : PlayerController
{
    // public Zone
    [Header("\nCameraSetting")]
    public float MouseSensitivity = 2f;
    public Vector3 SecurityCameraPosition = new Vector3(0, 1.36f, 0.15f);

    public GameObject CharacterMesh; // 본인 캐릭터 메쉬는 안보이도록 조정 : SecurityController

    // private Zone
    private float mouseX = 0, mouseY = 0;
    private float pitch = 0, yaw = 0;

    private SkinnedMeshRenderer skinnedMeshRenderer; // CharacterMesh 설정을 위함.

    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private GameObject MainCam;

    protected override void Awake()
    {
       
        base.Awake();

        if (!isOwned) return;


        SceneManager.sceneLoaded += InitPlayerPosition;

        DontDestroyOnLoad(gameObject);

      
        
    }


    protected override void Start()
    {
        if (!isOwned) return;

        MainCam = GameObject.FindWithTag("MainCamera");

        MouseLockedHover mousehover = MainCam.AddComponent<MouseLockedHover>();
        mousehover.interactableLayer = (1 << 6) | (1 << 26);

        PhysicsRaycaster physicsRaycaster = MainCam.AddComponent<PhysicsRaycaster>();
        physicsRaycaster.eventMask = (1 << 3) | (1 << 6) | (1 << 26);
        foreach (Transform child in MainCam.transform)
        {
            child.gameObject.SetActive(true);
        }
        Debug.Log("캠 생성");


        skinnedMeshRenderer = CharacterMesh.GetComponent<SkinnedMeshRenderer>();
        skinnedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
    }


    // Update is called once per frame
    protected override void Update()
    {
        // For Network Play
        if (!isLocalPlayer) return;
        if (GameResultManager.Instance.IsGamePaused == true) return;

        if (playerState == PlayerState.Faint) { return; }

        Debug.Log("경비원 이동 로직");

         base.Update();


        if (playerState == PlayerState.Idle || playerState == PlayerState.Run || playerState == PlayerState.Jump)
        {
            PlayerMovement();
        }
        else if (playerState == PlayerState.Attack)
        {

        }
    }


    protected override void PlayerMovement()
    {
        base.PlayerMovement();

        // View
        mouseX = Input.GetAxis("Mouse X") * MouseSensitivity;
        mouseY = Input.GetAxis("Mouse Y") * MouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);
        MainCam.transform.Rotate(Vector3.up * mouseX);

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -30f, 30f);

        MainCam.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        MainCam.transform.position = transform.position + transform.rotation * SecurityCameraPosition;
    }

    protected void InitPlayerPosition(Scene scene, LoadSceneMode mode)
    {
        if(scene.name == "PlayScene")
        {
            StartCoroutine("SetPlayerPosition");
        }
    }

    IEnumerator SetPlayerPosition()
    {
        yield return new WaitUntil(() => NetworkClient.ready);

        transform.position = new Vector3(-21f, 2f, 41f);
    }


    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Mira"))
        {
            if (!isOwned) { return; }

            Debug.LogWarning("미라와 충돌 - isOwned = true (경비원 스크립트) ");

            CmdSetPlayerState(PlayerState.Faint);

            Camera miraHeadCam = other.GetComponentInChildren<Camera>();

            if (miraHeadCam != null)
            {
                miraHeadCam.enabled = true;

            }

            if (MainCam == null)
            {
                MainCam = GameObject.FindWithTag("MainCamera");
            }

            MainCam.GetComponent<Camera>().enabled = false;

        }
    }
}
