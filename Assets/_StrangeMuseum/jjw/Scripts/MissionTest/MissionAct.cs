using UnityEngine;

public abstract class MissionAct : MonoBehaviour
{
    public bool IsActive { get; private set; }
    public bool IsComplete { get; private set; }

    public void StartMission()
    {
        if (IsComplete) return;

        IsActive = true;
        OnStart();
    }

    public void CancelMission()
    {
        if (!IsActive) return;

        IsActive = false;
        OnCancel();
    }

    public void CompleteMission()
    {
        if (!IsActive) return;

        IsActive = false;
        IsComplete = true;
        OnComplete();
    }

    protected abstract void OnStart();
    protected abstract void OnCancel();
    protected abstract void OnComplete();
}
