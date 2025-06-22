using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionProgressBarUI  : MonoBehaviour
{
    [SerializeField] private GameObject root;
    [SerializeField] private Image fillImage;
    [SerializeField] private TextMeshProUGUI progressTmp;

    public void Show()
    {
        root.SetActive(true);
        fillImage.fillAmount = 0f;
    }

    public void UpdateProgress(float ratio)
    {
        fillImage.fillAmount = Mathf.Clamp01(ratio);

        progressTmp.text = $"{(fillImage.fillAmount * 100f).ToString("F0")} %";

        if(fillImage.fillAmount == 1.0f)
        {
            CompletedProgress();
        }
    }


    public void CompletedProgress()
    {
        progressTmp.text = $"100 %";

        Invoke("Hide", 1.0f);

    }

    public void Hide()
    {
        root.SetActive(false);
    }
}
