using System.Collections;
using System.Net.NetworkInformation;
using Unity.Netcode;
using Unity.Services.Vivox;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static Define;

public class StatueInteraction : NetworkBehaviour
{
    public NetworkVariable<bool> isHandCuffUsing = new NetworkVariable<bool>
(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //상호작용 오브젝트 레이 충돌 여부

    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsHandCuffServerRpc(bool value)
    {
        isHandCuffUsing.Value = value;
    }


    private StatueController statueController;
    private StatueAttack statueAttack;

    [SerializeField]
    private AudioSource audioSource; // 오디오 소스 컴포넌트

    [SerializeField]
    private AudioClip HandCuffFearSound; // 구속구 공포 효과음
    [SerializeField]
    private AudioClip CoverFearSound; // 피 묻은 천 공포 효과음




    //서버에서 이동 속도 관리 (네트워크 변수로 동기화)
    // 모든 플레이어가 같은 값을 보장받음 (서버 권한 유지)
    // 치트 방지 가능(클라이언트가 속도 조작 불가능)

    private void Start()
    {
        statueController = GetComponent<StatueController>();
        statueAttack = GetComponent<StatueAttack>();
    }

    [SerializeField]
    GameObject InGameUIPrefab;


    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner == false) { Debug.Log("오너 아님 X"); return; }

        Instantiate(InGameUIPrefab);

    }


    // @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@  1. 구속구 @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@.
    public void HandCuffInteracted(HandCuff handcuff, float minMoveSpeed, float minRushSpeed,
        float handCuffCooltime, ulong clientid) // 상호작용 
    {
        if (IsServer == false) { return; }
        if (IsClient == false) { return; }

        if (isHandCuffUsing.Value == true)
        {
            Debug.Log("구속구 기능 진행중임. 아직 사용불가");
            return;
        }

        StartCoroutine(HandStuffFunc(handcuff, minMoveSpeed, minRushSpeed, handCuffCooltime));

        handcuff.HandActiveClientRpc(clientid);
    }


    private IEnumerator HandStuffFunc(HandCuff handcuff, float minMoveSpeed, float minRushSpeed, float handCuffCooltime)
    {
        SetIsHandCuffServerRpc(true);

       

        //감소 시간 - handCuffCooltime / 2 = 1.5초
        float elapsedTime = 0f;
        while (elapsedTime < handCuffCooltime)
        {
            elapsedTime += Time.deltaTime;

            statueController.MovementSpeed.Value = Mathf.Lerp(statueController.MovementSpeed.Value, minMoveSpeed, elapsedTime / handCuffCooltime); //
            statueAttack.RushSpeed.Value = Mathf.Lerp(statueAttack.RushSpeed.Value, minRushSpeed, elapsedTime / handCuffCooltime); //1.5

            yield return null;
        }

        yield return new WaitForSeconds(handCuffCooltime);

        // 증가 시간 - handCuffCooltime / 2 = 1.5초
        elapsedTime = 0f;
        while (elapsedTime < handCuffCooltime)
        {
            elapsedTime += Time.deltaTime;

            statueController.MovementSpeed.Value = Mathf.Lerp(minMoveSpeed, statueController.InitMovementSpeed, elapsedTime / handCuffCooltime);
            statueAttack.RushSpeed.Value = Mathf.Lerp(minRushSpeed, statueAttack.InitRushSpeed, elapsedTime / handCuffCooltime);

            yield return null;
        }

        Debug.Log("속도 정상 복구, 구속 효과 종료");

        handcuff.ResetInteractServerRpc(NetworkManager.Singleton.LocalClientId);

        SetIsHandCuffServerRpc(false);
    }

    
    public void PlayFearSound(AudioClip audio)
    {
        SoundManager.Instance.PlaySfx(audio);
        // audioSource.PlayOneShot(audio);
    }


    // @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@  2. 피 묻은천 @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@.

    public NetworkVariable<bool> isCoverUsing = new NetworkVariable<bool>
(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server); //상호작용 오브젝트 레이 충돌 여부

    [ServerRpc(RequireOwnership = false)] // 클라이언트도 요청할 수 있도록 설정
    public void SetIsCoverServerRpc(bool value)
    {
        isCoverUsing.Value = value;
     
    }

    [ServerRpc(RequireOwnership = false)]
    public void CoverServerRpc(NetworkObjectReference coverRef)
    {
        if (coverRef.TryGet(out NetworkObject networkObject))
        {
            CoverGameObject = networkObject.gameObject;
            CoverClientRpc(coverRef); // 서버에서 클라이언트로 전달
        }
    }
    [ServerRpc(RequireOwnership = false)]
    public void CoverOnOffServerRpc(bool value)
    {
        transform.GetChild(4).gameObject.SetActive(value);

        CoverOnOffClientRpc(value);
    }

    [ClientRpc]
    public void CoverOnOffClientRpc(bool value)
    {
        transform.GetChild(4).gameObject.SetActive(value);
    }

    public void CoverInteracted(bool value, GameObject cover)
    {
        Debug.Log("CoverInteracted 메서드 진입");

        SetIsCoverServerRpc(value);
        

        if (cover.TryGetComponent(out NetworkObject networkObject))
        {
            CoverServerRpc(networkObject);
            
        }
    }

    [ClientRpc]
    public void CoverClientRpc(NetworkObjectReference boxRef)
    {
        if (!boxRef.TryGet(out NetworkObject networkObject))
        {
            Debug.LogError("Failed to get NetworkObject from boxRef on client.");
            return;
        }

        CoverGameObject = networkObject.gameObject;
    }

    public GameObject CoverGameObject;


    // @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@  3. 던지는 볼펜  @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@.

    public IEnumerator ThrowPenFuca(float voiceUsingTime) //voiceUsingTime -> 보이스 사용 가능 시간
    {
        if (IsOwner == false) { yield return null; }


        // 조각상 보이스 챗 비활성화 
        VivoxService.Instance.MuteOutputDevice();
        yield return new WaitForSeconds(voiceUsingTime);
        // 조각상 보이스 챗 활성화
        VivoxService.Instance.UnmuteOutputDevice();
    }

}

