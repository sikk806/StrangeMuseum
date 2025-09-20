using Mirror;
using Mirror.Examples.Common;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class StatueRush : NetworkBehaviour, IStatueUsingSkill
{
    public string SkillName => "Rush";

    public bool isUsing { get;  set; } = false; //

    [Header("AttackSetting")]
    public float RushSpeed = 50f;  // default : 50f

    public float InitRushSpeed;

    public float RushDuration = 0.2f;

    public GameObject DashVisualEffect;

    protected GameObject playerCamera;

    StatueController statueController;

    void Start()
    {
        if (!isOwned) return;

        statueController = GetComponent<StatueController>();

        playerCamera = statueController.GetPlayerCamera();

    }

    public void UseSkill()
    {
        if(isUsing == false)
        {
            isUsing = true;

            Debug.Log("돌진 스킬 사용 후 쿨타임 상태" + isUsing);
            StartCoroutine(RushAttack());

        }

    }
     
    IEnumerator RushAttack()
    {
        float elapseTime = 0;
        Vector3 rushDirection = transform.forward;
        CharacterController characterController = GetComponent<CharacterController>();


        yield return new WaitForSeconds(0.1f);

        GameObject go = ResourceManager.Instance.Instantiate("DashEffect", null, transform);
        go.transform.position = playerCamera.transform.position + playerCamera.transform.forward;
        go.transform.LookAt(playerCamera.transform);
        while (elapseTime < RushDuration)
        {
            characterController.Move(rushDirection * RushSpeed * Time.deltaTime);
            elapseTime += Time.deltaTime;

            playerCamera.transform.position = transform.position + transform.rotation * statueController.StatueCameraPosition;
            go.transform.position = playerCamera.transform.position + playerCamera.transform.forward + playerCamera.transform.up * 0.1f;
            yield return null;
        }

        ResourceManager.Instance.Destroy(go);

    }





}
