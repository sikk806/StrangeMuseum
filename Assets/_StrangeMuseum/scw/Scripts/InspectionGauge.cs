using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InspectionGauge : MonoBehaviour
{
    /*
     * 이 스크립트는 공통 임무를 진행하면서 오브젝트(ex. 정수기, 장식품 등)와 상호작용할 때
     * 등장하는 게이지 UI의 기능이 담긴 스크립트입니다.
     */

    [SerializeField]
    private TextMeshProUGUI gaugeText;
    [SerializeField]
    private Image gaugeImage;

    public void SetInspectionGauge(string objectName, float currentValue, float maxValue, bool isInProgress) // 게이지를 수정하는 함수
    {
        float progress = currentValue / maxValue;

        // 게이지 UI 투명도 설정
        float alpha = isInProgress ? 1f : 0.3f;
        gaugeImage.color = new Color(gaugeImage.color.r, gaugeImage.color.g, gaugeImage.color.b, alpha);

        if (isInProgress) // 진행 중인 임무 게이지 UI
        {
            gaugeText.text = objectName + "을(를) 점검하는 중...";
        }
        else // 진행 중이지 않거나 완료된 임무 게이지 UI
        {
            if (progress >= 1f)
            {
                gaugeText.text = "점검 완료";
            }
            else
            {
                gaugeText.text = "마우스를 홀드하여 " + objectName + " 점검";
            }
        }

        gaugeImage.fillAmount = progress;
    }
}
