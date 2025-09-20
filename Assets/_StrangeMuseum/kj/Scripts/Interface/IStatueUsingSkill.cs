using UnityEngine;

public interface IStatueUsingSkill
{
    bool isUsing { get; set; } 

    string SkillName { get; }
    void UseSkill();
}
