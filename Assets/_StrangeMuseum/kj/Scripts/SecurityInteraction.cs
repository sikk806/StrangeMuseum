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

//PlayerController - SecurityInteraction - SecurityItemController 

//네트워크 관련 아이템 변수들은 각 아이템 스크립트에서 관리 -> 시도 해보기

public class SecurityInteraction : NetworkBehaviour
{
    public float LayDistance; //상호작용 레이

    public float LightLayDistance; //손전등 레이

    private IInteractable interactableItem; // 상호작용할 수 있는 아이템 저장

    private IUsableItem iusableItem; // 상호작용할 수 있는 아이템 저장

    private TestMoveController testMoveController; //임시

    [SerializeField]
    private AudioClip pickUpSound; // 구속구 공포 효과음

    [SerializeField]
    public GameObject InGameUIPrefab;

    private GameObject uiInstance;


    public bool isLight = false;

    private float LightOnIntensity = 30f; // 초기 intensity 저장

    public GameObject DashVisualEffect; // 이동속도가 빨라졌을 때 이펙트 -JS-

    [SerializeField]
    Light networkLight;

    [SerializeField]
    private AudioClip CoverFearSound; // 구속구 공포 효과음

    public GameObject SaveRayItem; //바라본 아이템 저장 

    public GameObject SaveRayStaute; //바라본 조각상 저장

    public NetworkVariable<bool> IsStatue = new NetworkVariable<bool>
        (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<bool> isInteracted = new NetworkVariable<bool>
     (false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //상호작용 오브젝트 레이 충돌 여부


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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)  // 내가 소유한 클라이언트라면
        {
            uiInstance = Instantiate(InGameUIPrefab);
        }
    }
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (IsOwner)
        {
            Destroy(uiInstance);

        }

    }


    private void Start()
    {
       
        if (IsOwner == false)
        {
            return;
        }

        testMoveController = GetComponent<TestMoveController>();

        if (IsOwner)  // 내가 소유한 클라이언트라면
        {
            networkLight.intensity = 0;

            uiInstance = Instantiate(InGameUIPrefab);
            Debug.Log("OnDie += HandleDie 등록");
        }
    }

    private void Update()
    {
        if (IsOwner == false)
        {
            return;
        }

        BouncerInteractionRay();

        BouncerInteracted();

    }

    private void BouncerInteracted()
    {
        if (interactableItem == null) { return; }      

        if (Input.GetMouseButtonDown(0) && isInteracted.Value == true && interactableItem != null)
        {
            SaveRayItem.GetComponent<Collider>().enabled = false; //false하지 않으면 좌클릭 할 때마다 아이템이 인 게임 화면 중앙으로 이동함. 이동은 1번만.

            interactableItem.Interact();

            SetIsInteractedServerRpc(false);

            SecurityInGameUI.Instance.OnInteractionUI(InteractionType.None);

            SoundManager.Instance.PlaySfx(pickUpSound);

            if (SaveRayItem.gameObject.tag == "Cover")
            {
                PlayFearSound(CoverFearSound);
            }    
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
                iusableItem = hit.collider.GetComponent<IUsableItem>();

                ItemData.ItemList rayItem = iusableItem.GetItemList();

                if(ItemManager.Instance.inventoryDictionary.ContainsKey(rayItem))
                {
                    if (ItemManager.Instance.CountCurrentItem(rayItem))
                    {
                        Debug.LogWarning($"해당 {rayItem}은 소지 개수를 초과 했으므로 상호작용 불가능");
                        return;
                    }
                }


                bool isInteractedNow = interactableItem != null;

                SetIsInteractedServerRpc(isInteractedNow);
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
    private void ItemSave(GameObject obj = null)
    {
        SaveRayItem = obj;
    }

    #region 손전등 Light 여부에 따른 조각상 행동 제한
    //private void LightItemRay()
    //{
    //    Vector3 lightPosition = transform.position + Vector3.up * 0.75f; // 손전등 위치를 조금 위로 올림
    //    Vector3 lightDirection = transform.forward; // 손전등 방향
    //    float lightRange = LightLayDistance; // 손전등 최대 거리
    //    float lightAngle = Flashlight.spotAngle * 0.5f; // Spot Light 반각

    //    Collider[] hitColliders = Physics.OverlapSphere(lightPosition, lightRange, LayerMask.GetMask("Statue"));

    //    Debug.DrawRay(lightPosition, lightRange * lightDirection, Color.blue, 0.1f);

    //    foreach (Collider col in hitColliders)
    //    {
    //        if (col.CompareTag("Statue") && isLight) // 손전등이 켜진 상태
    //        {
    //            Vector3 toStatue = (col.transform.position - lightPosition).normalized;
    //            float distanceToStatue = Vector3.Distance(lightPosition, col.transform.position);
    //            float angle = Vector3.Angle(lightDirection, toStatue); // 손전등 중심축과의 각도 비교


    //            Debug.DrawRay(lightPosition, toStatue, Color.red, 0.1f);

    //            if (angle < lightAngle) // 원뿔 범위 내에 있는 경우
    //            {
    //                if (Physics.Raycast(lightPosition, toStatue, out RaycastHit hit, distanceToStatue, LayerMask.GetMask("Default")))
    //                {
    //                    Debug.Log("벽에 가려짐 - Idle");
    //                    col.GetComponent<StatueController>().SetPlayerState(PlayerState.Idle);
    //                }
    //                else
    //                {
    //                    Debug.Log("벽에 가려지지 않음 - Freeze");
    //                    col.GetComponent<StatueController>().SetPlayerState(PlayerState.Freeze);
    //                }

    //            }
    //            else
    //            {
    //                Debug.Log("Idle (각도 벗어남)");
    //                col.GetComponent<StatueController>().SetPlayerState(PlayerState.Idle);
    //            }
    //        }
    //        else if (col.CompareTag("Statue") && !isLight) // 손전등이 꺼진 상태
    //        {
    //            Debug.Log("손전등 꺼서 Idle");
    //            col.GetComponent<StatueController>().SetPlayerState(PlayerState.Idle);
    //        }
    //    }
    //}
    #endregion

    [ServerRpc(RequireOwnership = false)]
    public void RayStatueServerRpc(NetworkObjectReference coverRef)
    {
        if (coverRef.TryGet(out NetworkObject networkObject))
        {
            SaveRayStaute = networkObject.gameObject;
            RayStatueClientRpc(coverRef); // 서버에서 클라이언트로 전달
        }
    }

    [ClientRpc]
    public void RayStatueClientRpc(NetworkObjectReference boxRef)
    {
        if (!boxRef.TryGet(out NetworkObject networkObject))
        {
            return;
        }

        SaveRayStaute = networkObject.gameObject;
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

  

    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@ 1. 에너지 드링크 @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@

    public void EnergyDrinkFunction(EnergyDrink energyDrink, float cooltime, float maxSpeed)
    {
        StartCoroutine(EnergyDrinkFunc(energyDrink, cooltime, maxSpeed));
    }

    public IEnumerator EnergyDrinkFunc(EnergyDrink energyDrink, float cooltime, float maxSpeed)
    {     
        float halfCooldown = cooltime / 2f; // 감소 & 회복을 위한 절반 시간

        float elapsedTime = 0f;

        while (elapsedTime < halfCooldown)
        {
            elapsedTime += Time.deltaTime;
            testMoveController.MovementSpeed =
                Mathf.Lerp(testMoveController.MovementSpeed, maxSpeed, elapsedTime / halfCooldown);



            yield return null;
        }

        yield return new WaitForSeconds(halfCooldown);

        elapsedTime = 0f;
        while (elapsedTime < halfCooldown)
        {
            elapsedTime += Time.deltaTime;
            testMoveController.MovementSpeed =
            Mathf.Lerp(testMoveController.MovementSpeed, testMoveController.InitWalkingSpeed, elapsedTime / halfCooldown);

            yield return null;
        }


        energyDrink.ResetEnergyDrinkServerRpc(NetworkManager.Singleton.LocalClientId);

 
    }

    public void PlayFearSound(AudioClip audio)
    {
        SoundManager.Instance.PlaySfx(audio);
        // audioSource.PlayOneShot(audio);
    }

}