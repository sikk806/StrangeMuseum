using System.Collections;
using System.Net.NetworkInformation;
using Mirror;
using Unity.Services.Vivox;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;

public class StatueInteraction : PlayerController
{
 

    private StatueController statueController;

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
    }



    // @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@  1. 구속구 @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@.
    [Command(requiresAuthority = false)]
    public void HandCuffInteracted(NetworkIdentity handCuffIdentity, float minMoveSpeed, float minRushSpeed,
        float handCuffCooltime) // 상호작용 
    {

        StartCoroutine(HandStuffFunc(handCuffIdentity, minMoveSpeed, minRushSpeed, handCuffCooltime));
    }


    private IEnumerator HandStuffFunc(NetworkIdentity handCuffIdentity, float minMoveSpeed, float minRushSpeed, float handCuffCooltime)
    {
        StatueController stauteController = GetComponent<StatueController>();
        handCuffIdentity.GetComponent<HandCuff>().isHandCuffUsing = true;

        //감소 시간 - handCuffCooltime / 2 = 1.5초
        float elapsedTime = 0f;
        while (elapsedTime < handCuffCooltime)
        {
            elapsedTime += Time.deltaTime;

            stauteController.MovementSpeed = Mathf.Lerp(stauteController.MovementSpeed, minMoveSpeed, elapsedTime / handCuffCooltime); //
            stauteController.RushSpeed = Mathf.Lerp(stauteController.initRushSpeed, minRushSpeed, elapsedTime / handCuffCooltime); //1.5

            yield return null;
        }

        yield return new WaitForSeconds(handCuffCooltime);

        // 증가 시간 - handCuffCooltime / 2 = 1.5초
        elapsedTime = 0f;
        while (elapsedTime < handCuffCooltime)
        {
            elapsedTime += Time.deltaTime;

            stauteController.MovementSpeed = Mathf.Lerp(minMoveSpeed, stauteController.InitWalkingSpeed, elapsedTime / handCuffCooltime);
            stauteController.RushSpeed = Mathf.Lerp(minRushSpeed, stauteController.initRushSpeed, elapsedTime / handCuffCooltime);

            yield return null;
        }

        Debug.Log("속도 정상 복구, 구속 효과 종료");

        handCuffIdentity.GetComponent<HandCuff>().isHandCuffUsing = false;

        handCuffIdentity.GetComponent<HandCuff>().ResetInteractServerRpc();


    }

    
    public void PlayFearSound(AudioClip audio)
    {
        SoundManager.Instance.PlaySfx(audio);
        // audioSource.PlayOneShot(audio);
    }

    [Command(requiresAuthority = false)]
    public void CoverOnOffServerRpc(NetworkIdentity BloodCover , bool value)
    {
        transform.GetChild(4).gameObject.SetActive(value);

        Debug.Log("CoverUI 호출");

        NetworkIdentity statue = this.GetComponent<NetworkIdentity>();

        StatueInGameUI.Instance.CoverUI(BloodCover, statue);

        CoverOnOffClientRpc(value);
    }

    [ClientRpc]
    public void CoverOnOffClientRpc(bool value)
    {
        transform.GetChild(4).gameObject.SetActive(value);
    }



    // @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@  3. 던지는 볼펜  @@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@.

    //public IEnumerator ThrowPenFuca(float voiceUsingTime) //voiceUsingTime -> 보이스 사용 가능 시간
    //{
    //    if (IsOwner == false) { yield return null; }


    //    // 조각상 보이스 챗 비활성화 
    //    VivoxService.Instance.MuteOutputDevice();
    //    yield return new WaitForSeconds(voiceUsingTime);
    //    // 조각상 보이스 챗 활성화
    //    VivoxService.Instance.UnmuteOutputDevice();
    //}

}

