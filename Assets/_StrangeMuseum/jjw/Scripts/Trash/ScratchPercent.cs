using ScratchCard;
using UnityEngine;
using UnityEngine.EventSystems;

public class ScratchPercent : MonoBehaviour
{
    [SerializeField] private ScratchCardMask scratchCardMask;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (scratchCardMask == null) return;

        float progress = scratchCardMask.GetRevealProgress();
        Debug.Log($"현재 긁힌 진행도: {progress * 100:F1}%");

        if (scratchCardMask.IsRevealed)
        {
            Debug.Log("스크래치 완료!");
        }
    }
}
