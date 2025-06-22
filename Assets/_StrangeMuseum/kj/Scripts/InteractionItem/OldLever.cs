using Mirror;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEngine.UIElements;

public class OldLever : NetworkBehaviour , IHoldInteractable
{
    private enum HoldState { None, Holding, Reversing }
    private HoldState holdState = HoldState.None;

    public Animator animator;
    public AnimationClip leverClip;

    private PlayableGraph graph;
    private AnimationClipPlayable playable;


    [SerializeField] private float MaxholdTime = 2f;

    public float HoldDuration => MaxholdTime;
    public float CurrentHoldTime { get; set; }

    private bool isHolding; //Hold 여부

    [SerializeField]
    private bool isComplete; //Hold 후 완료 여부

    [SerializeField]
    GameObject MapLight;

    void Start()
    {
        graph = PlayableGraph.Create("LeverGraph");
        var output = AnimationPlayableOutput.Create(graph, "Animation", animator);

        playable = AnimationClipPlayable.Create(graph, leverClip);
        playable.SetSpeed(0); // 멈춰놓기
        playable.SetTime(0);

        output.SetSourcePlayable(playable);
        graph.Play();
    }

    void Update()
    {
        if (holdState == HoldState.Holding)
        {
            // 진행 시간 증가
            CurrentHoldTime += Time.deltaTime;
            if (CurrentHoldTime >= HoldDuration)
            {
                CurrentHoldTime = HoldDuration;
                CompletedHold();
            }
            // 애니메이션 위치 강제 지정

            playable.SetTime((CurrentHoldTime / HoldDuration) * leverClip.length);
            playable.SetSpeed(leverClip.length / HoldDuration); // 속도도 항상 보장
        }
        else if (holdState == HoldState.Reversing)
        {
            // 진행 시간 감소
            CurrentHoldTime -= Time.deltaTime;
            if (CurrentHoldTime <= 0f)
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
        else
        {
            // None 상태일 땐 멈춤
            playable.SetSpeed(0f);
        }

    }

    public bool IsHolding() { return isHolding; }
    public bool IsCompleted() { return isComplete; }
   
    public void StartHolding(float deltaTime) //홀드중
    {
        if (holdState != HoldState.Holding) return;

        CurrentHoldTime += deltaTime;

        if (CurrentHoldTime >= HoldDuration)
        {
            CurrentHoldTime = HoldDuration;

            CompletedHold();
        }

    }

    public void StartHold() //홀드 시작
    {
        if (isComplete) return;

        holdState = HoldState.Holding;

        CurrentHoldTime = 0f;
    }



    public void StopHold() //홀드 멈춤
    {
        if (holdState == HoldState.Holding)
        {
            holdState = HoldState.Reversing;
        }
    }
    public void CompletedHold()
    {
        isComplete = true;
        holdState = HoldState.None;

        MapLight.gameObject.SetActive(true);

        StartCoroutine(CompletedLever());
    }

    IEnumerator CompletedLever()
    {
        yield return new WaitForSeconds(3.0f);

        holdState = HoldState.Reversing;

        MapLight.gameObject.SetActive(false);

        playable.SetSpeed(-(leverClip.length / HoldDuration));
    }
    
}
