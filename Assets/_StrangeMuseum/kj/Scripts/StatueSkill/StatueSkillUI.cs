using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UI;

public class StatueSkillUI : MonoBehaviour
{
    public string SkillName; //스킬 이름

    [SerializeField]
    private float SkillMaxCooltime; //해당 스킬 재사용 대기 시간 

    [SerializeField]
    private TextMeshProUGUI TextCooldownTime; //재사용 대기시간 텍스트

    [SerializeField]
    private UnityEngine.UI.Image ImageCooldownTime; //재사용 대기시간의 이미지

    private float currentCooldownTime; //현재 재사용 대기 시간

    private bool isCooldown; //현재 쿨타임이 적용중인지 여부


    private IStatueUsingSkill statueUsingSkill;

    private void Awake()
    {
        
        SetCooldownIs(false); //재사용 대기시간 쿨타임 UI 비활성화
    }

    private void SetCooldownIs(bool isActive) //재사용 대기시간 관련 UI 활성화 및 비활성화
    {
        isCooldown = isActive;
        TextCooldownTime.enabled = isActive;
        ImageCooldownTime.enabled = isActive;
    }

    public void UseSkill(IStatueUsingSkill statueUsingSkill) //외부 클래스에서 스킬 사용할 때 호출하는 메서드
    {
        if(this.statueUsingSkill == null)
        {
            this.statueUsingSkill = statueUsingSkill;

        }
        //이미 스킬을 사용해 재사용 대기 시간이 남아있으면 종료
        if (isCooldown) //현재 재사용 대기 시간이 적용 중임 (쿨타임 아직 안 끝남)
        {
            return;
        }
        // 이 줄 부터 스킬 사용 진입

        StartCoroutine(OnCooldownTime(SkillMaxCooltime)); //재사용 대기 시간을 처리하는 코루틴 호출

    }

    private IEnumerator OnCooldownTime(float maxCooldownTime) //재사용 대기 시간을 처리하는 코루틴
    {
        currentCooldownTime = maxCooldownTime; //현재 재사용 대기 시간을 해당 스킬 재사용 대기시간으로 설정.

        SetCooldownIs(true); //재사용 대기시간 관련 UI 활성화 

        while (currentCooldownTime > 0)
        {
            currentCooldownTime -= Time.deltaTime; //쿨타임은 시간 단위로 감소

            //Image UI의 fillAmount를 조절해 채워지는 이미지 모양 설정
            ImageCooldownTime.fillAmount = currentCooldownTime / maxCooldownTime; //남은 시간 / 해당 스킬 재사용 대기 시간 으로 계산. currentCoolTime이 0이되면 보이질 않음

            //TextUI의 현재 남은 재사용 대기 시간 표시
            TextCooldownTime.text = currentCooldownTime.ToString("F1"); //소수점 1번째까지만 출력

            yield return null;
        }

        statueUsingSkill.isUsing = false;

        Debug.Log("쿨타임 진행 후 쿨타임 상태" + statueUsingSkill.isUsing);
        SetCooldownIs(false);
    }
}
