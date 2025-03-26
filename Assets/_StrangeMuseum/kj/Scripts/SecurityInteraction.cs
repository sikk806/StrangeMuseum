using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Experimental.GlobalIllumination;
using UnityEngine.UIElements;
using static Define;


public class SecurityInteraction : NetworkBehaviour
{
    public float LayDistance; //상호작용 레이

    public float LightLayDistance; //손전등 레이

    private IInteractable interactableItem; // 상호작용할 수 있는 아이템 저장

    private IUsableItem inusableItem; // 상호작용할 수 있는 아이템 저장

    private SecurityController securityController;

    [SerializeField]
    private AudioClip pickUpSound; // 구속구 공포 효과음

    [SerializeField]
    public GameObject InGameUIPrefab;

    private GameObject uiInstance;


    public bool isLight = false;

    private float LightOnIntensity = 30f; // 초기 intensity 저장

    public GameObject DashVisualEffect; // 이동속도가 빨라졌을 때 이펙트 -JS-

    private Transform playerCamera;

    Light Flashlight; //손전등 자식에 있는 Light컴포넌트

    [SerializeField]
    Light networkLight;

    private ItemList itemType;

    [SerializeField]
    private AudioClip CoverFearSound; // 구속구 공포 효과음

    public NetworkVariable<bool> IsStatue = new NetworkVariable<bool>
        (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isInteracted = new NetworkVariable<bool>
     (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //상호작용 오브젝트 레이 충돌 여부

    public NetworkVariable<bool> isEnergyDrinkUsing = new NetworkVariable<bool>
(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //상호작용 오브젝트 레이 충돌 여부

    public NetworkVariable<bool> isBroken = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
  

    public NetworkVariable<bool> isStatueCollider = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsBrokenServerRpc(bool value)
    {
        isBroken.Value = value;
    }


    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsStatueColliderServerRpc(bool value)
    {
        isStatueCollider.Value = value;
    }


    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsStatueServerRpc(bool value)
    {
        IsStatue.Value = value;
    }

    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsInteractedServerRpc(bool value)
    {
        isInteracted.Value = value;
    }


    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsInEnergyDrinkServerRpc(bool value)
    {
        isEnergyDrinkUsing.Value = value;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();


        if (IsOwner)
        {
            uiInstance = Instantiate(InGameUIPrefab);
            Debug.Log("OnDie += HandleDie 등록");
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsOwner)
        {
            Destroy(uiInstance);
            Debug.Log("OnDie -= HandleDie 해제");
        }

    }


    private void Start()
    {
        if (IsOwner == false)
        {
            return;
        }

        securityController = GetComponent<SecurityController>();

        LightInit();

        if (IsOwner)  // 내가 소유한 클라이언트라면
        {
            networkLight.intensity = 0;
        }
    }


    private void LightInit()
    {
        Transform aTransform = securityController.playerCamera.transform.Find("StylizedHand.Left");

        if (aTransform != null)
        {
            Transform cTransform = aTransform.Find("FlashLight");

            if (cTransform != null)
            {
                Flashlight = cTransform.GetComponentInChildren<Light>();

                Flashlight.intensity = 0;
                isLight = false;
            }
    }
    
}


    private void Update()
    {
        if (IsOwner == false)
        {
            return;
        }
        LightOnOff();

        LightItemRay();

        BouncerInteractionRay();

        BouncerInteracted();

        if (isStatueCollider.Value == true)
        {
            NotifyClientBoxRemoved();
            SetIsStatueColliderServerRpc(false);
        }
    }

  


    private void BouncerInteracted()
    {
        if (interactableItem == null)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) && isInteracted.Value == true && interactableItem != null)
        {
            if (RayItem.gameObject.tag == "Cover")
            {
                PlayFearSound(CoverFearSound);
            }

            interactableItem.Interact(this);
            SetIsInteractedServerRpc(false);
            SecurityInGameUI.Instance.OnInteractionUI(Define.InteractionType.None);

            SoundManager.Instance.PlaySfx(pickUpSound);
        }
    }
    private void BouncerInteractionRay() //상호작용 여부
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, LayDistance, LayerMask.GetMask("Interaction", "Statue")))
        {
            if (hit.collider.CompareTag("HandCuff") || hit.collider.CompareTag("EnergyDrink")
                || hit.collider.CompareTag("Box") || hit.collider.CompareTag("Cover")
                || hit.collider.CompareTag("Pen"))
            {
                interactableItem = hit.collider.GetComponent<IInteractable>();

                bool isInteractedNow = interactableItem != null;
                SetIsInteractedServerRpc(isInteractedNow);
                // SetInteractedServerRpc(isInteractedNow);
                ItemSave(hit.collider.gameObject);
            }
            if (hit.collider.CompareTag("Statue"))
            {
                StatueInterated(true, hit.collider.gameObject);
            }
        }
        else
        {
            interactableItem = null;

            SetIsInteractedServerRpc(false);

            if (isInteracted.Value) // 값이 이미 false라면 다시 호출하지 않음
            {
                SetIsInteractedServerRpc(false);
            }
            StatueInterated(false, null);
        }

    }



    private void LightOnOff()
    {
        if (Input.GetKeyDown(KeyCode.F)) // 'F' 키 입력 시 전환
        {
            if (Flashlight != null)
            {
                isLight = !isLight;
                Flashlight.intensity = isLight ? LightOnIntensity : 0;
                //networkLight.intensity = isLight ? LightOnIntensity : 0;
                //RequestNetworkLightOnOffServerRpc(isLight);
                
                Debug.Log("손전등 토글: " + (isLight ? "켜짐" : "꺼짐"));
            }
        }
    }


    private StatueController currentStatue; // 현재 감지된 조각상 저장

    [SerializeField]
    private bool anyStatueInLight;

    private void LightItemRay()
    {
        Vector3 lightPosition = transform.position + Vector3.up * 0.75f; // 손전등 위치를 조금 위로 올림
        Vector3 lightDirection = transform.forward; // 손전등 방향
        float lightRange = LightLayDistance; // 손전등 최대 거리
        float lightAngle = Flashlight.spotAngle * 0.5f; // Spot Light 반각

        Collider[] hitColliders = Physics.OverlapSphere(lightPosition, lightRange, LayerMask.GetMask("Statue"));

        Debug.DrawRay(lightPosition, lightRange *lightDirection, Color.blue, 0.1f);

        foreach (Collider col in hitColliders)
        {
            if (col.CompareTag("Statue") && isLight) // 손전등이 켜진 상태
            {
                Vector3 toStatue = (col.transform.position - lightPosition).normalized;
                float distanceToStatue = Vector3.Distance(lightPosition, col.transform.position);
                float angle = Vector3.Angle(lightDirection, toStatue); // 손전등 중심축과의 각도 비교


                Debug.DrawRay(lightPosition, toStatue, Color.red, 0.1f);

                if (angle < lightAngle) // 원뿔 범위 내에 있는 경우
                {
                    if (Physics.Raycast(lightPosition, toStatue, out RaycastHit hit, distanceToStatue, LayerMask.GetMask("Default")))
                    {
                        Debug.Log("벽에 가려짐 - Idle");
                        col.GetComponent<StatueController>().SetPlayerStateServerRpc(PlayerState.Idle);
                    }
                    else
                    {
                        Debug.Log("벽에 가려지지 않음 - Freeze");
                        col.GetComponent<StatueController>().SetPlayerStateServerRpc(PlayerState.Freeze);
                    }
                 
                }
                else
                {
                    Debug.Log("Idle (각도 벗어남)");
                    col.GetComponent<StatueController>().SetPlayerStateServerRpc(PlayerState.Idle);
                }
            }
            else if (col.CompareTag("Statue") && !isLight) // 손전등이 꺼진 상태
            {
                Debug.Log("손전등 꺼서 Idle");
                col.GetComponent<StatueController>().SetPlayerStateServerRpc(PlayerState.Idle);
            }
        }
    }


    public GameObject RayItem;

    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ 바라본 조각상 객체 저장 @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
    public GameObject RayStaute;


    [ServerRpc(RequireOwnership = false)]
    public void RayStatueServerRpc(NetworkObjectReference coverRef)
    {
        if (coverRef.TryGet(out NetworkObject networkObject))
        {
            RayStaute = networkObject.gameObject;
            RayStatueClientRpc(coverRef); // 서버에서 클라이언트로 전달
        }
    }


    public void StatueInterated(bool value, GameObject statue)
    {
        if (statue == null) { SetIsStatueServerRpc(false); return; }


        SetIsStatueServerRpc(true);

        if (statue != null)
        {
            if (statue.TryGetComponent(out NetworkObject networkObject))
            {

                Debug.Log("RayStatueServerRpc 호출");
                RayStatueServerRpc(networkObject);
            }
        }

    }

    [ClientRpc]
    public void RayStatueClientRpc(NetworkObjectReference boxRef)
    {
        if (!boxRef.TryGet(out NetworkObject networkObject))
        {
            return;
        }

        RayStaute = networkObject.gameObject;
    }

    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@

    private void ItemSave(GameObject obj = null)
    {
        RayItem = obj;
    }

    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@ 1. 에너지 드링크 @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
    public void EnergyDrinkInteracted(EnergyDrink energyDrink, float cooltime, float maxSpeed, int itemLayer) // 상호작용 
    {

        //if (IsServer == false) { return; }
        //if (IsOwner == false) { Debug.Log("오너가 아니므로 리턴"); return; }

        if (isEnergyDrinkUsing.Value == true)
        {
            Debug.Log("에너지 드링크 기능 적용중");
            return;
        }


        StartCoroutine(EnergyDrinkFunc(energyDrink, cooltime, maxSpeed));
    }


    private IEnumerator EnergyDrinkFunc(EnergyDrink energyDrink, float cooltime, float maxSpeed)
    {

        SetIsInEnergyDrinkServerRpc(true);




        float halfCooldown = cooltime / 2f; // 감소 & 회복을 위한 절반 시간

        float elapsedTime = 0f;

        while (elapsedTime < halfCooldown)
        {
            elapsedTime += Time.deltaTime;
            securityController.MovementSpeed.Value =
                Mathf.Lerp(securityController.MovementSpeed.Value, maxSpeed, elapsedTime / halfCooldown);



            yield return null;
        }

        yield return new WaitForSeconds(halfCooldown);

        elapsedTime = 0f;
        while (elapsedTime < halfCooldown)
        {
            elapsedTime += Time.deltaTime;
            securityController.MovementSpeed.Value =
                Mathf.Lerp(securityController.MovementSpeed.Value, securityController.InitWalkingSpeed, elapsedTime / halfCooldown);

            yield return null;
        }


        energyDrink.ResetEnergyDrinkServerRpc(NetworkManager.Singleton.LocalClientId);

        SetIsInEnergyDrinkServerRpc(false);
    }

    // 이속 버프 잠시 보류

    public void EnergyDrinkRushEffect(float cooltime)
    {
        StartCoroutine(RushEffectOn(cooltime));
    }
    private IEnumerator RushEffectOn(float cooltime)
    {

        float elapsedTime = 0f;
        playerCamera = Camera.main.transform;

        GameObject go = Instantiate(DashVisualEffect);
        while (elapsedTime < cooltime)
        {
            elapsedTime += Time.deltaTime;
            go.transform.position = playerCamera.transform.position + playerCamera.transform.forward;
            go.transform.LookAt(playerCamera);
            yield return null;
        }
        Destroy(go);
    }

    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@ 2. 박스 @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@

    public NetworkVariable<bool> isBoxUsing = new NetworkVariable<bool>
(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //상호작용 오브젝트 레이 충돌 여부


    public NetworkObjectReference storedBoxRef;

    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsBoxServerRpc(bool value)
    {
        isBoxUsing.Value = value;
    }

    [ServerRpc(RequireOwnership = false)]
    public void BoxServerRpc(NetworkObjectReference boxRef)
    {
        if (boxRef.TryGet(out NetworkObject networkObject))
        {
            storedBoxRef = boxRef; // 원본을 저장하지 않고, 네트워크 참조를 저장
            BoxClientRpc(boxRef);  // 서버에서 클라이언트로 전달
        }
    }


    [SerializeField]
    private float boxInvincibilityTime; //박스 사용 후 무적 시간

    public void BoxInteracted(GameObject box)
    {
        if (IsServer == false) { return; }

        SetIsBoxServerRpc(true);

        if (box.TryGetComponent(out NetworkObject networkObject))
        {
            BoxServerRpc(networkObject);
        }

    }

    [ClientRpc]
    public void BoxClientRpc(NetworkObjectReference boxRef)
    {
        storedBoxRef = boxRef; // 클라이언트도 네트워크 참조를 저장

        if (!boxRef.TryGet(out NetworkObject networkObject))
        {
            return;
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void BoxOnOffServerRpc(bool value)
    {

        transform.GetChild(2).gameObject.SetActive(value);

        BoxOnOffClientRpc(value);
    }

    IEnumerator DelayBoxing()
    {
        yield return new WaitForSeconds(boxInvincibilityTime);
        SetIsBoxServerRpc(false);
    }


    [ClientRpc]
    private void BoxOnOffClientRpc(bool value)
    {

        transform.GetChild(2).gameObject.SetActive(value);


        // 박스를 사용하지 않았을 때 이하 코드를 실행

    }

    [SerializeField]
    private GameObject storedBox;

    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@ 3. 손전등 활성화/비활성화
    public void CameraOff()
    {
        securityController.playerCamera.GetChild(0).gameObject.SetActive(false);
        //securityController.playerCamera.GetChild(1).gameObject.SetActive(true);
    }

    public void CameraOn()
    {
        securityController.playerCamera.GetChild(0).gameObject.SetActive(true);
        //securityController.playerCamera.GetChild(1).gameObject.SetActive(false);
    }

    //3. 
    public void PlayFearSound(AudioClip audio)
    {
        SoundManager.Instance.PlaySfx(audio);
        // audioSource.PlayOneShot(audio);
    }


    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("OnTriggerOn");
        if (other.gameObject.CompareTag("Statue") && isStatueCollider.Value == false)
        {
            SetIsStatueColliderServerRpc(true);

            // Debug.Log("------------->IsOwner: " + IsOwner);

            if (IsOwner || IsServer)
            {
                Debug.Log("----------------------------In1");
                GameManager.Instance.UpdatePlayerCountServerRpc(true, -1);
                GameManager.Instance.UpdatePlayerCountServerRpc(false, 1);
                GameManager.Instance.PlayerStat.Value[OwnerClientId] = "Statue";
            }
        }
    }




    private void NotifyClientBoxRemoved()
    {
        Debug.Log($"ClientRpc 실행됨 | IsOwner: {IsOwner}, OwnerClientId: {OwnerClientId}, LocalClientId: {NetworkManager.Singleton.LocalClientId}");

        if (isBoxUsing.Value == true)
        {
            Debug.Log("경비원 박스 입고 있었음");

            if (storedBoxRef.TryGet(out NetworkObject networkObject))
            {
                Debug.Log("결국 경비원 박스 벗음");
                BoxOnOffServerRpc(false);
                SetIsBrokenServerRpc(false);

                networkObject.GetComponent<Box>().ResetInteractServerRpc(NetworkManager.Singleton.LocalClientId);
                StartCoroutine(DelayBoxing());
                return;
            }
        }

        if (isBoxUsing.Value == false)
        {
            Debug.Log("사망 관련 if문 진입 직전 isOwner 판별");
            if (IsOwner)
            {
                Debug.Log("IsOwner 이고, 경비원 박스 입지 않으므로 경비원 죽음"); //여기까지는 호출 잘 됨
                SecurityDieServerRpc();
            }

        }
    }

    private void HandleDie(SecurityInteraction interaction)
    {
        // 사망 후 수행할 행동 처리
        Debug.Log("경비원이 사망했습니다.");

        SetIsBrokenServerRpc(true);



        VivoxController.Instance.StopAllCoroutines();

        Debug.Log("경비원 수 : " + GameManager.Instance.SecurityCount.Value);
        Debug.Log("조각상 수 : " + GameManager.Instance.StatueCount.Value);

    }

    [ServerRpc(RequireOwnership = false)]
    private void SecurityDieServerRpc()
    {


        if (OnDie != null)
        {
            OnDie?.Invoke(this);  // OnDie 델리게이트 호출
        }
        else
        {
            Debug.LogWarning("OnDie null 이므로 델리게이트 호출 x");
        }

    }

    [SerializeField]
    private GameObject bloodPrefab; // 피 프리팹
    [SerializeField]
    private GameObject[] fragmentPrefabs; // 부서진 조각 프리팹 리스트

    public Action<SecurityInteraction> OnDie;

    private float explosionForce = 5f; // 튀는 힘
    private float explosionRadius = 3f; // 폭발 반경

    [ServerRpc(RequireOwnership = false)]
    public void SpawnBloodServerRpc()
    {
        if (!IsServer) return;

        Debug.Log("SpawnBloodServerRpc() - IsServer = true");

        float bloodOffset = 0.05f;
        Vector3 bloodPosition = transform.position + Vector3.up * bloodOffset;

        GameObject blood = Instantiate(bloodPrefab, bloodPosition, Quaternion.identity);
        blood.GetComponent<NetworkObject>().Spawn();

        SpawnBloodClientRpc(bloodPosition);
    }

    // 클라이언트에서 피 생성 결과 반영
    [ClientRpc]
    void SpawnBloodClientRpc(Vector3 bloodPosition)
    {
        if (IsServer) return;

        Debug.Log("SpawnBloodClientRpc() - IsServer = false");

        GameObject blood = Instantiate(bloodPrefab, bloodPosition, Quaternion.identity);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SpawnFragmentServerRpc()
    {
        if (!IsServer) return;

        Debug.Log("SpawnFragmentServerRpc - IsServer = true");

        // 여러 조각 생성
        for (int i = 0; i < 20; i++)
        {
            if (fragmentPrefabs.Length > 0)
            {
                // 랜덤한 조각 선택
                GameObject fragment = Instantiate(
                    fragmentPrefabs[UnityEngine.Random.Range(0, fragmentPrefabs.Length)],
                    transform.position,
                    UnityEngine.Random.rotation
                );

                fragment.GetComponent<NetworkObject>().Spawn();

                Rigidbody rb = fragment.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 0, ForceMode.Impulse);
                }

                Destroy(fragment, 10f);
            }
        }

        SpawnFragmentClientRpc();
    }

    [ClientRpc]
    private void SpawnFragmentClientRpc()
    {
        if (IsServer) return;

        Debug.Log("SpawnFragmentClientRpc - IsServer = false");

        // 클라이언트에서 조각 생성
        for (int i = 0; i < 20; i++)
        {
            if (fragmentPrefabs.Length > 0)
            {
                GameObject fragment = Instantiate(
                    fragmentPrefabs[UnityEngine.Random.Range(0, fragmentPrefabs.Length)],
                    transform.position,
                    UnityEngine.Random.rotation
                );

                Rigidbody rb = fragment.GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 0, ForceMode.Impulse);
                }

                Destroy(fragment, 10f);
            }
        }
    }
}