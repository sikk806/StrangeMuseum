using Mirror;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
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

    [SyncVar]
    public bool isInteracted;
    [SyncVar]
    public bool IsStatue;
    

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (isOwned)  // 내가 소유한 클라이언트라면
        {
            uiInstance = Instantiate(InGameUIPrefab);

        }
    }
    public override void OnStopLocalPlayer()
    {
        base.OnStopLocalPlayer();

        if (isOwned)
        {
            Destroy(uiInstance);

        }

    }


    void Start()
    {
       
        if (isOwned == false)
        {
            return;
        }


        if (isOwned)  // 내가 소유한 클라이언트라면
        {
            networkLight.intensity = 0;

           // uiInstance = Instantiate(InGameUIPrefab);
            Debug.Log("OnDie += HandleDie 등록");
        }
    }

    void Update()
    {
        if (isOwned == false)
        {
            return;
        }

        BouncerInteractionRay();

        BouncerInteracted();

    }

    private void BouncerInteracted()
    {
        if (interactableItem == null) { return; }      

        if (Input.GetMouseButtonDown(0) && isInteracted == true && interactableItem != null)
        {
            SaveRayItem.GetComponent<Collider>().enabled = false; //false하지 않으면 좌클릭 할 때마다 아이템이 인 게임 화면 중앙으로 이동함. 이동은 1번만.

            interactableItem.Interact();



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

                isInteracted = true;

                ItemSave(hit.collider.gameObject);
            }
            if (hit.collider.CompareTag("Statue"))
            {
                IsStatue = true;
                StatueSave(hit.collider.gameObject);
            }
        }
        else
        {
            interactableItem = null;
            isInteracted = false;
            IsStatue = false;

            if (isInteracted) // 값이 이미 false라면 다시 호출하지 않음
            {
                isInteracted = false;
            }
            StatueSave(null);
        }

    }
    private void ItemSave(GameObject obj = null)
    {
        SaveRayItem = obj;
    }

    private void StatueSave(GameObject obj = null)
    {

        SaveRayStaute = obj;
    }



    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@ 1. 에너지 드링크  동기화 과정@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
    #region 에너지드링크 동기화 부분
    [Command(requiresAuthority = false)]
    public void EnergyDrinkFunction(NetworkIdentity playerNetIdentity, NetworkIdentity energyDrinkIdentity,
        int itemLayer, float cooltime, float maxSpeed)
    {
        //1. 호출한 클라이언트만 대상으로 TargetRpc 호출 - UI 삭제 부분
        NetworkConnectionToClient targetConn = playerNetIdentity.connectionToClient;
        TargetEnergyDrinkUI(targetConn, energyDrinkIdentity.netId, itemLayer);

        StartCoroutine(EnergyDrinkCoroutine(playerNetIdentity, energyDrinkIdentity, cooltime, maxSpeed, itemLayer));

    }

    [TargetRpc]
    private void TargetEnergyDrinkUI(NetworkConnection target, uint energyDrinkNetId, int itemLayer)
    {
        try
        {
            if (NetworkClient.spawned.TryGetValue(energyDrinkNetId, out var energyDrinkIdentity))
            {

                //if (SecurityInGameUI.Instance != null)
                //{
                //    GameObject energyDrink = energyDrinkIdentity.gameObject; //.gameObject 로 GameObject로 변환
                //    SecurityInGameUI.Instance.OnDestroyItemUI(energyDrink, itemLayer);
                //}
                //else
                //{
                //    Debug.LogWarning("SecurityInGameUI.Instance가 null입니다.");
                //}

                Debug.Log("해당 클라이언트에서 에너지 드링크 UI 활성화 완료");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TargetSetSecurityBodyActiveAndUI] Rpc 오류: {ex.Message}\n{ex.StackTrace}");
        }

    }
    private IEnumerator EnergyDrinkCoroutine(NetworkIdentity playerNetIdentity, NetworkIdentity energyDrinkIdentity, float cooltime, float maxSpeed,int itemLayer)
    {


        SecurityController seucurityController = playerNetIdentity.GetComponent<SecurityController>();

        float halfCooldown = cooltime / 2f;
        float elapsed = 0f;

        // 속도 증가
        while (elapsed < halfCooldown)
        {
            elapsed += Time.deltaTime;
            seucurityController.MovementSpeed = Mathf.Lerp(seucurityController.InitWalkingSpeed, maxSpeed, elapsed / halfCooldown);
            yield return null;
        }

        yield return new WaitForSeconds(halfCooldown);

        // 속도 복원
        elapsed = 0f;
        while (elapsed < halfCooldown)
        {
            elapsed += Time.deltaTime;
            seucurityController.MovementSpeed = Mathf.Lerp(maxSpeed, seucurityController.InitWalkingSpeed, elapsed / halfCooldown);
            yield return null;
        }

        // 마무리
        EnergyDrink energyDrink = energyDrinkIdentity.GetComponent<EnergyDrink>();
        energyDrink.ResetEnergyDrinkServerRpc();
    }
    #endregion

    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@ 2. 박스 동기화 과정 @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@

    #region 박스 동기화 부분

    [Command(requiresAuthority = false)]
    public void BoxFunction(NetworkIdentity playerNetIdentity, NetworkIdentity boxIdentity, int itemLayer)
    {
        //1. 호출한 클라이언트만 대상으로 TargetRpc 호출 -  - UI 삭제 및 박스 활성화 부분
        NetworkConnectionToClient targetConn = playerNetIdentity.connectionToClient;
        TargetSetSecurityBodyActiveAndUI(playerNetIdentity.netId, boxIdentity.netId, itemLayer);

        //2. 모든 클라이언트 시각화
        BoxClientRpc(playerNetIdentity);

    }
    [ClientRpc]
    void BoxClientRpc(NetworkIdentity body)
    {
        GameObject securityBody = body.transform.GetChild(2).gameObject;
        securityBody.SetActive(true); // 서버에서도 활성화 
    }

    [TargetRpc]
    void TargetSetSecurityBodyActiveAndUI(uint playerNetId, uint boxNetId, int itemLayer)
    {
        try
        {
            if (NetworkClient.spawned.TryGetValue(playerNetId, out var playerIdentity) &&
                NetworkClient.spawned.TryGetValue(boxNetId, out var boxIdentity))
            {
                if (playerIdentity.transform.childCount > 2)
                {
                    GameObject securityBody = playerIdentity.transform.GetChild(2).gameObject;
                    securityBody.SetActive(true);
                }

                //if (SecurityInGameUI.Instance != null)
                //{
                //    GameObject box = boxIdentity.gameObject; //.gameObject 로 GameObject로 변환
                //    SecurityInGameUI.Instance.OnDestroyItemUI(box, itemLayer);
                //}
                //else
                //{
                //    Debug.LogWarning("SecurityInGameUI.Instance가 null입니다.");
                //}

                Debug.Log("해당 클라이언트에서 박스 UI 및 오브젝트 활성화 완료");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TargetSetSecurityBodyActiveAndUI] Rpc 오류: {ex.Message}\n{ex.StackTrace}");
        }
    }
    #endregion

    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@ 3. 피 묻은 천 동기화 과정 @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@

    #region 피 묻은 천 동기화 부분 - 조각상 피 묻은 천 씌움

    [Command(requiresAuthority = false)]
    public void BloodCoverFunction(NetworkIdentity playerNetIdentity, NetworkIdentity bloodCoverIdentity, int itemLayer)
    {

        NetworkConnectionToClient targetConn = playerNetIdentity.connectionToClient;
        TargetStatueBloodCover(targetConn, bloodCoverIdentity, playerNetIdentity.netId, bloodCoverIdentity.netId, itemLayer);
    }

    [TargetRpc] 
    void TargetStatueBloodCover(NetworkConnection target, NetworkIdentity bloodCoverIdentity, uint playerNetId, uint bloocCoverNetId, int itemLayer)
    {
        try
        {
            if (!NetworkClient.spawned.TryGetValue(playerNetId, out var playerIdentity))
            {
                Debug.LogError("playerNetId로 찾은 NetworkIdentity가 없습니다.");
                return;
            }

            if (!NetworkClient.spawned.TryGetValue(bloocCoverNetId, out var bloodIdentity))
            {
                Debug.LogError("bloocCoverNetId로 찾은 NetworkIdentity가 없습니다.");
                return;
            }

            var cover = bloodCoverIdentity.GetComponent<Cover>();
            if (cover == null)
            {
                Debug.LogError("bloodCoverIdentity에 Cover 컴포넌트가 없습니다.");
                return;
            }
            cover.isCoverUsing = true;

            var statue = SaveRayStaute.GetComponent<StatueInteraction>();
            if (statue == null)
            {
                Debug.LogError("SaveRayStaute에 StatueInteraction 컴포넌트가 없습니다.");
                return;
            }

            statue.CoverOnOffServerRpc(true, bloodCoverIdentity);

            //if (SecurityInGameUI.Instance != null)
            //{
            //    GameObject bloodCover = bloodIdentity.gameObject;
            //    SecurityInGameUI.Instance.OnDestroyItemUI(bloodCover, itemLayer);
            //    Debug.Log("Cover UI 삭제");
            //}

            Debug.Log("해당 클라이언트에서 박스 UI 및 오브젝트 활성화 완료");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TargetSetSecurityBodyActiveAndUI] Rpc 오류: {ex.Message}\n{ex.StackTrace}");
        }
    }

    #endregion

    //@@@@@@@@@@@@@@@@@@@@@@@@@@@@ 4. 구속구 동기화 과정 @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
    #region 구속구 동기화 부분
    [Command(requiresAuthority = false)]
    public void HandCuffFunction(NetworkIdentity playerNetIdentity, NetworkIdentity handCuffIdentity, int itemLayer,float minMoveSpeed, float minRushSpeed,
        float handCuffCooltime)
    {

        NetworkConnectionToClient targetConn = playerNetIdentity.connectionToClient;
        TargetStatueHandCuff(targetConn, handCuffIdentity, playerNetIdentity.netId, handCuffIdentity.netId, itemLayer,minMoveSpeed,minRushSpeed,handCuffCooltime);

       // SaveRayStaute.GetComponent<StatueInteraction>().HandCuffInteracted(handCuffIdentity, minMoveSpeed, minRushSpeed, handCuffCooltime);
    }


    [TargetRpc]
    void TargetStatueHandCuff(NetworkConnection target, NetworkIdentity handCUffIdentity, uint playerNetId, uint handCuffNetId, 
        int itemLayer, float minMoveSpeed,float minRushSpeed, float handCuffCooltime)
    {
        try
        {
            if (NetworkClient.spawned.TryGetValue(playerNetId, out var playerIdentity) &&
                NetworkClient.spawned.TryGetValue(handCuffNetId, out var handCuffIdentity))
            {
                handCUffIdentity.GetComponent<HandCuff>().isHandCuffUsing = true;

                SaveRayStaute.GetComponent<StatueInteraction>().HandCuffInteracted(handCUffIdentity, minMoveSpeed, minRushSpeed, handCuffCooltime);

                //if (SecurityInGameUI.Instance != null)
                //{
                //    GameObject HandCuff = handCUffIdentity.gameObject; //.gameObject 로 GameObject로 변환
                //    SecurityInGameUI.Instance.OnDestroyItemUI(HandCuff, itemLayer);

                //    Debug.Log("Cover UI 삭제");
                //}

                Debug.Log("해당 클라이언트에서 박스 UI 및 오브젝트 활성화 완료");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[TargetSetSecurityBodyActiveAndUI] Rpc 오류: {ex.Message}\n{ex.StackTrace}");
        }
    }

    #endregion

    bool RayStatue()
    {
        if(IsStatue && SaveRayStaute != null)
        {
            return true;
        }

        return false;
    }
    public void PlayFearSound(AudioClip audio)
    {
        SoundManager.Instance.PlaySfx(audio);
        // audioSource.PlayOneShot(audio);
    }

}