using UnityEngine;

public interface IHoldInteractable
{
    float HoldDuration { get; } // 필요 유지 시간

    public  float CurrentHoldTime { get; set; }

    public void StartHolding(float time);      //Hold 진행 중
    public void StartHold(); //Hold 시작
    public void StopHold(); //Hold 시작
    public void CompletedHold(); //Hold 초기화

    public bool IsHolding();
    public bool IsCompleted();
}
