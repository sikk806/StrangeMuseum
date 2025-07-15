using System.Collections;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public GameObject GetPlayerCamera()
    {
        if (cam == null)
        {
            Debug.LogWarning("cam is null in GetPlayerCamera(), 재시도");

            cam = GameObject.FindWithTag("MainCamera"); // 또는 다른 로직으로 복구
            return cam;
        }

        return cam;
    }

    [SerializeField]
    GameObject SeucrityCamera;

    private GameObject cam;

    protected override void Awake()
    {
        base.Awake();

        if (!isOwned) return;

        Cursor.lockState = CursorLockMode.Locked; // 커서 숨기기

        SceneManager.sceneLoaded += InitPlayerPosition;

        DontDestroyOnLoad(gameObject);

        Debug.Log("Start()");
        
    }

    private void Start()
    {
        if (!isOwned) return;

        cam = Instantiate(SeucrityCamera, transform.position, Quaternion.identity);

        // 자신의 스킨은 볼 수 없도록. (그림자만 존재하도록)
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
        cam.transform.Rotate(Vector3.up * mouseX);

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -30f, 30f);

        cam.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        cam.transform.position = transform.position + transform.rotation * SecurityCameraPosition;
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





   

}
