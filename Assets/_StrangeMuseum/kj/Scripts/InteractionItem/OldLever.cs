using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UIElements;

public class OldLever : NetworkBehaviour , IHoldInteractable 
{
    private enum HoldState { None, Holding, Reversing }

    [Header("애니메이션 관련")]
    public Animator animator;
    public AnimationClip leverClip;
    private PlayableGraph graph;
    private AnimationClipPlayable playable;

    [Header("오래된 레버 기능 관련")]
    [SyncVar]
    [SerializeField]
    private HoldState holdState = HoldState.None;

    [SyncVar]
    [SerializeField] private float MaxholdTime = 2f;

    [SyncVar]
    private float CurrentHoldTime = 2f;

    public float HoldDuration => MaxholdTime;
    public float HoldTime => CurrentHoldTime;

    [SerializeField]
    GameObject MapLight; //맵 밝기

    [SerializeField]
    GameObject SparkEffect; //스파크 이펙트

    [SerializeField]
    GameObject SparkEffectPos; //스파크 이펙트

    [SyncVar]
    private bool isHolding; //Hold 여부
    public bool IsCompleted() { return isComplete; }
    [SyncVar]
    private bool isComplete; //Hold 후 완료 여부

    private MissionProgressBarUI progressBarUI;

    public ObjectData.ObjectList GetObjectList()
    {
        return ObjectData.ObjectList.OldLever;
    }

    GameObject SparkEffectPrefab;
    void Start()
    {
        if(SecurityInGameUI.Instance != null)
        {
            progressBarUI = SecurityInGameUI.Instance.GetComponent<MissionProgressBarUI>();
        }
      

        AnimSet();

        SparkEffectPrefab = Instantiate(SparkEffect, SparkEffectPos.transform.position, Quaternion.identity);
        SparkEffectPrefab.transform.SetParent(transform);
        SparkEffectPrefab.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
    }

    //public override void OnStartServer()
    //{
    //    Debug.Log("server = 스파크 이펙트 생성");

    //    base.OnStartServer();

    //    CreateSparkEffect();
    //}

    //private void CreateSparkEffect()
    //{
    //    SparkEffectPrefab = Instantiate(SparkEffect, SparkEffectPos.transform.position, Quaternion.identity);
    //    SparkEffectPrefab.transform.SetParent(transform);
    //    SparkEffectPrefab.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);

    //    NetworkServer.Spawn(SparkEffectPrefab);

    //    RpcAssignSparkEffect(SparkEffectPrefab.GetComponent<NetworkIdentity>());
    //}

    //[ClientRpc]
    //void RpcAssignSparkEffect(NetworkIdentity sparkNetId)
    //{
    //    SparkEffectPrefab = sparkNetId.gameObject;
    //}


    [SerializeField]
    float radius = 5f;


    public bool IsSoundInStatue()
    {
        Vector3 center = transform.position;  // 레버 위치

        Collider[] hitColliders = Physics.OverlapSphere(center, radius,LayerMask.GetMask("Statue"));

        foreach (Collider collider in hitColliders)
        {
            Debug.Log("근처에 있는 오브젝트: " + collider.name);

            return true;
        }

        return false;
    }
    private void AnimSet()
    {
        graph = PlayableGraph.Create("LeverGraph");
        var output = AnimationPlayableOutput.Create(graph, "Animation", animator);

        playable = AnimationClipPlayable.Create(graph, leverClip);
        playable.SetSpeed(0); // 멈춰놓기
        playable.SetTime(0);

        output.SetSourcePlayable(playable);
        graph.Play();

    }
    public void StartHold() //홀드 시작
    {
        if (isComplete) return;

        holdState = HoldState.Holding;

        CurrentHoldTime = 0f;
    }

    public void StartHolding(float deltaTime) //홀드중
    {
        if (holdState != HoldState.Holding) return;

        CurrentHoldTime += deltaTime;

        if (CurrentHoldTime >= HoldDuration)
        {
            CurrentHoldTime = HoldDuration;

            if(isComplete == false)
            {
                isComplete = true;

                SecurityInGameUI.Instance.OnObjectInteractionUnview();

                progressBarUI.Hide();

                CmdNotifyHoldCompleted();
            }
           

        }
        // 애니메이션 위치 강제 지정

        playable.SetTime((CurrentHoldTime / HoldDuration) * leverClip.length);
        playable.SetSpeed(leverClip.length / HoldDuration); // 속도도 항상 보장

    }

    public void StopHold() //홀드 멈춤
    {
        if (holdState == HoldState.Holding)
        {
            holdState = HoldState.Reversing;

            CurrentHoldTime -= Time.deltaTime;

            if (CurrentHoldTime <= 0f )
            {
                CurrentHoldTime = 0f;
                holdState = HoldState.None;
                playable.SetSpeed(0f);
                playable.SetTime(0f);
                return;
            }
            playable.SetTime((CurrentHoldTime / HoldDuration) * leverClip.length);
            playable.SetSpeed(-leverClip.length / HoldDuration);
        }
    }
    [Command(requiresAuthority = false)]
    private void CmdNotifyHoldCompleted()
    {
        CompletedHold();
    }

    public void CompletedHold()
    {

        if (isServer)
        {
            StartCoroutine(CompletedLever());

            SparkEffectPrefab.gameObject.SetActive(false);       
        }
           

    }

    [ClientRpc]
    private void LeverOnOClientRpc()
    {
        //동기화 목록 - 1. 맵 밝기 2. 조각상 상태 3. 레버 애니메이션 역재생 4. 스파크 이펙트

        MapLight.SetActive(true);

        SparkEffectPrefab.gameObject.SetActive(false);


    }
    [ClientRpc]
    private void LeverOffClientRpc()
    {
        //동기화 목록 - 1. 맵 밝기 2. 조각상 상태 3. 레버 애니메이션 역재생 4. 스파크 이펙트

        MapLight.SetActive(false);


        playable.SetTime((CurrentHoldTime / HoldDuration) * leverClip.length);
        playable.SetSpeed(-leverClip.length / HoldDuration);

    }


    IEnumerator CompletedLever() //맵 밝기 낮추는 코루틴
    {

        LeverOnOClientRpc();

        StatueMoveStop(PlayerState.Freeze);

        Debug.Log("불 키고, 스파크 이펙트 삭제");

        yield return new WaitForSeconds(5.0f); //5초 쿨타임 설정

        Debug.Log("불 끄고, 레버 애니메이션 역 재생");

        LeverOffClientRpc();

        StatueMoveStop(PlayerState.Idle);


    }

    private void StatueMoveStop(PlayerState state)
    {
        GameObject[] statues = GameObject.FindGameObjectsWithTag("Statue");

        foreach(GameObject statue in statues)
        {
            StatueController statueController = statue.GetComponent<StatueController>();

            if(statueController != null)
            {
                statueController.SetPlayerState(state);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        // Gizmo 색깔 설정 (반투명 빨간색)
        Gizmos.color = new Color(1, 0, 0, 0.5f);
        // 현재 오브젝트 위치를 중심으로 반경 radius 만큼 와이어 구 그리기
        Gizmos.DrawWireSphere(transform.position, radius);
    }

}
