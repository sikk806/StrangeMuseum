using Mirror;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class OldLever : NetworkBehaviour
{
    public Animator animator;
    public AnimationClip leverClip;

    private PlayableGraph graph;
    private AnimationClipPlayable playable;
    private float frameRate = 30f; // 애니메이션 FPS
    private int currentFrame = 0;

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



    public void OldLeverFunction()
    {
        StepFrame();
    }
    void StepFrame()
    {
        currentFrame++;

        float newTime = currentFrame / frameRate;

        if (newTime > leverClip.length)
        {
            newTime = leverClip.length;
            // currentFrame--; // 끝나면 멈추게 하거나 루프 가능
        }

        playable.SetTime(newTime);
        playable.SetTime(newTime); // 두 번 호출해 정확하게 적용되도록 함
    }

    void OnDestroy()
    {
        graph.Destroy();
    }
}
