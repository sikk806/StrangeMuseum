using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    private static UIManager instance;
    public static UIManager Instance
    {
        get
        {
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this; // instance 초기화
        }
        else
        {
            Destroy(gameObject); // 중복 방지
        }
    }

    [SerializeField]
    private GameObject keyGuideUI; // 하단에 입력 키를 알려주는 UI

    [SerializeField]
    private GameObject inspectionGaugeUI; // 공통 임무를 진행할때 등장하는 게이지 UI

    [SerializeField]
    private GameObject blackPanel; // 화면이 잠깐 꺼지는 효과를 만들기 위한 패널

    [SerializeField]
    private Image flashImage; // 조각상이 플래시에 노출되었을때 피드백을 화면에 나타내기 위한 이미지

    [SerializeField]
    private AudioClip audioClip;

    private Coroutine runningCoroutine;

    public void CallGameManagerInspectionGaugeUI(string objectName, float currentValue, float maxValue, bool isInProgress) // 공통 임무 수행 관련 게이지 UI를 그리는 함수 호출
    {
        bool isNeedGuide = (currentValue / maxValue < 1f) ? true : false;
        keyGuideUI.SetActive(isNeedGuide);
        inspectionGaugeUI.SetActive(true);

        inspectionGaugeUI.GetComponent<InspectionGauge>().SetInspectionGauge(objectName, currentValue, maxValue, isInProgress);
    }

    public void CloseInspectionObjectUI() // 공통 임무 수행 시 등장하는 UI를 끄는 함수
    {
        keyGuideUI.SetActive(false);
        inspectionGaugeUI.SetActive(false);
    }

    public void BlackOutEffect(ulong clientId) // 화면이 잠깐 꺼지는 효과를 주는 함수
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        StartCoroutine(ShowUIForSeconds(0.2f)); // 입력값만큼 꺼짐
        SoundManager.Instance.PlaySfx(audioClip);
    }

    IEnumerator ShowUIForSeconds(float sec)
    {
        blackPanel.SetActive(true);
        yield return new WaitForSeconds(sec);
        blackPanel.SetActive(false);
    }

    public void WhiteOutEffect(bool isFlashed)
    {
        if (runningCoroutine != null)
        {
            StopCoroutine(runningCoroutine);
        }

        if (isFlashed)
        {
            runningCoroutine = StartCoroutine(FlashEffectOn());
        }
        else
        {
            runningCoroutine = StartCoroutine(FlashEffectOff());
        }
    }

    IEnumerator FlashEffectOn()
    {
        float duration = 0.5f;
        float flashTime = 0;

        while (flashTime < duration)
        {
            float alpha = Mathf.Lerp(0, 1, flashTime / duration);
            flashImage.color = new Color(1, 1, 1, alpha);
            flashTime += Time.deltaTime;
            yield return null;
        }
        flashImage.color = new Color(1, 1, 1, 1);
    }

    IEnumerator FlashEffectOff()
    {
        float duration = 0.5f;
        float flashTime = 0;

        while (flashTime < duration)
        {
            float alpha = Mathf.Lerp(1, 0, flashTime / duration);
            flashImage.color = new Color(1, 1, 1, alpha);
            flashTime += Time.deltaTime;
            yield return null;
        }
        flashImage.color = new Color(1, 1, 1, 0);
    }
}

