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

    [SerializeField]
    GameObject SeucrityCamera;

    public GameObject cam;

    protected override void Start()
    {
        if(!isOwned) return;
        DontDestroyOnLoad(gameObject);

        Debug.Log("Start()");

        base.Start();

        cam = Instantiate(SeucrityCamera, transform.position, Quaternion.identity);

        SceneManager.sceneLoaded += InitPlayerPosition;

        
        Debug.Log("SetDelegate");

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
