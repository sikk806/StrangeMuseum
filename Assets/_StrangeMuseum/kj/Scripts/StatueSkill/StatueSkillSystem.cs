using DG.Tweening;
using Mirror;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;
using static MiraController;

// 조각상 스킬과 관련된 클래스
public class StatueSkillSystem : NetworkBehaviour
{
    [SerializeField]
    private StatueSkillUI[] statueSkills;

    private Dictionary<KeyCode, IStatueUsingSkill> keyToSkillName;

    [SerializeField]
    GameObject InGameUIPrefab;

    public override void OnStartLocalPlayer()
    {
        base.OnStartLocalPlayer();

        if (!isOwned) { return; }

        GameObject uiCanvas = Instantiate(InGameUIPrefab);

        StatueSkillUI[] StatueSkillUI = uiCanvas.GetComponentsInChildren<StatueSkillUI>();
        List<StatueSkillUI> skills = new List<StatueSkillUI>();

        foreach (var skillUI in StatueSkillUI)
        {
            skills.Add(skillUI);
        }

        statueSkills = skills.ToArray();

        Debug.Log($"조각상 UI 호출 완료. 총 스킬 수: {statueSkills.Length}");

        InfoSkillKeyList();
    }
    private void InfoSkillKeyList() //키에 맞는 스킬 컴포넌트 갖고오기.
    {
        keyToSkillName = new Dictionary<KeyCode, IStatueUsingSkill>
        {
            { KeyCode.LeftShift, GetComponent<StatueRush>() },
            { KeyCode.Q, GetComponent<StatueMiraRecall>() }
        };
    }

    private void Update()
    {
        if (!isOwned) return;

        if (!Input.anyKey) //키를 누르지 않는다면 return;
        {
            return;
        }

        foreach (var kvp in keyToSkillName) 
        {
            if (Input.GetKeyDown(kvp.Key)) // 스킬 키를 눌렀을 때
            {
                kvp.Value?.UseSkill();

                UseSkillByName(kvp.Value);
            }
        }
    }

    private void UseSkillByName(IStatueUsingSkill statueUsingSkill)
    {
        string skillName = statueUsingSkill.SkillName;

        foreach (var skill in statueSkills)
        {
            //전달 받은 스킬 이름과 StatueSkill 컴포넌트에 설정한 스킬이름을 대소문자 구분 없이 비교하여 같다면 해당 스킬 쿨타임 동작
            if (skillName.Equals(skill.SkillName, System.StringComparison.OrdinalIgnoreCase))
            {
                Debug.Log($"Skill : [{skillName}] 사용");
                skill.UseSkill(statueUsingSkill);
                return;
            }
        }

    
    }

 

}
