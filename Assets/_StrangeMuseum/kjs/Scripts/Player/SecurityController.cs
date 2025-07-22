using System.Collections;
using Mirror;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.SceneManagement;
using static MiraController;

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

    public Camera GetPlayerCamera()
    {
        return cam;
    }

    [SerializeField]
    GameObject SeucrityCamera; //플레이어 전용 카메라 프리펩

    private Camera cam;

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

        MainCam = Instantiate(SeucrityCamera, transform.position, Quaternion.identity);

        cam = MainCam.GetComponent<Camera>();

        Debug.Log("캠 생성");


        skinnedMeshRenderer = CharacterMesh.GetComponent<SkinnedMeshRenderer>();
        skinnedMeshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
    }


    // Update is called once per frame
    protected override void Update()
    {
        // For Network Play
        if (!isLocalPlayer) return;

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


    public void ShakeCamera(float duration = 0.3f, float magnitude = 0.1f)
    {
        if (cam == null) return;

        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        
        MainCam.transform.GetChild(0).gameObject.SetActive(false); //손과 손전등 비활성화

        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;

            MainCam.transform.localPosition = camPos + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }

        MainCam.transform.GetChild(0).gameObject.SetActive(true);

        MainCam.transform.localPosition = originalPos; //> 충돌 전 마지막 카메라 위치로 돌아감

        SetPlayerState(PlayerState.Idle); //1. 기본 상태 변경 

        miraController.CmdRequestDestroy(); //미라 사라짐

        isMiraCollider = false;
    }

    Vector3 originalPos;

    Vector3 camPos;

    bool isMiraCollider;

    MiraController miraController;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Mira") && !isMiraCollider)
        {
            Debug.LogWarning("미라와 충돌 (경비원 스크립트) ");
            miraController = other.GetComponent<MiraController>();

            if (MainCam == null)
            {
                MainCam = FindObjectOfType<Camera>().gameObject; // or GameObject.Find("SecurityCamera").GetComponent<Camera>()
            }

            originalPos = MainCam.transform.localPosition;

            SetPlayerState(PlayerState.Faint); //1. 기절 상태 변경

            Transform mirahead = miraController.transform.GetChild(1);

            if (mirahead != null)
            {
                Debug.Log(mirahead.gameObject.name);

                // 카메라를 머리 기준으로 살짝 앞쪽에 배치 (예: 0.2m 앞, 0.1m 아래)
                Vector3 offset = mirahead.forward * 1.5f + Vector3.down * 0.1f;

                MainCam.transform.position = mirahead.transform.position + offset; //카메라 위치 = 미라 머리 + 미라 머리 앞쪽 및 아래 배치

                camPos = MainCam.transform.position; //campos에 설정한 카메라 위치로 저장

                MainCam.transform.LookAt(mirahead); //카메라를 미라 머리를 바라보게.
            }
            else
            {
                Debug.Log("mirahead null");
            }

            ShakeCamera(1.0f, 0.1f);

            isMiraCollider = true;
        }
    }  

}
