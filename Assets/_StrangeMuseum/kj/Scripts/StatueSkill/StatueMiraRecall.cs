using Mirror;
using System.Reflection;
using UnityEngine;

public class StatueMiraRecall : NetworkBehaviour, IStatueUsingSkill
{
    public string SkillName => "MiraRecall";
    public bool isUsing { get;  set; } = false;

    public void UseSkill()
    {
        if(isUsing == false)
        {
            isUsing = true;

            CopyStatueCmd();
        }
    }

    [SerializeField]
    GameObject CopyStatue;

    [Command(requiresAuthority = false)]
    private void CopyStatueCmd()
    {
        GameObject Mira = ResourceManager.Instance.Instantiate("Mira", null, transform);

        NetworkServer.Spawn(Mira, connectionToClient);
    }


}
