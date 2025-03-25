using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class StatueInGameUI : NetworkBehaviour
{
    private static StatueInGameUI instance;

    public static StatueInGameUI Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<StatueInGameUI>();
                if (instance == null)
                {
                    GameObject obj = new GameObject("Staue_Canvas");
                    instance = obj.AddComponent<StatueInGameUI>();
                }
            }
            return instance;
        }
    }

    StatueInteraction statueInteraction;
    StatueController statueController;

    private void Start()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);

        }

        ulong localClientId = NetworkManager.Singleton.LocalClientId;

        // 모든 조각상 오브젝트를 찾고, 로컬 클라이언트 ID와 비교하여 해당 조각상의 Interaction을 가져옴
        GameObject[] Statues = GameObject.FindGameObjectsWithTag("Statue");
        foreach (var statue in Statues)
        {
            // 각 경비원에 대해 로컬 클라이언트 ID가 일치하는지 확인
            if (statue.GetComponent<NetworkObject>().OwnerClientId == localClientId)
            {
                // 일치하는 경비원 찾으면 그 조각상의 Interaction 객체를 할당
                statueInteraction = statue.GetComponent<StatueInteraction>();
                statueController = statue.GetComponent<StatueController>();

                statueClientId = statue.GetComponent<NetworkObject>().OwnerClientId;
                break; // 하나만 찾으면 됨
            }
        }



    }

    private ulong statueClientId;

    [SerializeField]
    TextMeshProUGUI FreezeTmp;

    private void Update()
    {
        CoverUI();

        if(statueController.playerState.Value == PlayerState.Freeze)
        {
            FreezeTmp.text = "!!! 경비원 손전등 감지 중 !!! ";
        }
        else
        {
            FreezeTmp.text = " ";
        }


    }


    [SerializeField]
    private float CoverCooltime;


    public void CoverUI()
    {
        if (statueInteraction.isCoverUsing.Value == false)
        {
            //Debug.Log("천 씌우지 않음");
            return;
        }

        if(isCoverUI) { Debug.Log("이미 시야 차단 UI 실행 중");  return; }

        Debug.Log("천 씌움");
       

        StartCoroutine(CoverFunc(CoverCooltime));
        statueInteraction.SetIsCoverServerRpc(false); //한번 실행 하고 바로 FALSE. 
    }

    [SerializeField]
    bool isCoverUI;
    public IEnumerator CoverFunc(float Cooltime)
    {
        isCoverUI = true;

        IncreaseAlpha();
        statueInteraction.CoverOnOffServerRpc(true);

        // PlayFearSound(CoverFearSound);

        yield return new WaitForSeconds(Cooltime);
  
        DecreaseAlpha();
        statueInteraction.CoverOnOffServerRpc(false);
        isCoverUI = false;
        statueInteraction.CoverGameObject.GetComponent<Cover>().ResetInteractServerRpc(NetworkManager.Singleton.LocalClientId);
  
    }


    public void IncreaseAlpha()
    {
        Debug.Log("시야 차단");
        StartCoroutine(ChangeAlpha(1f)); // 알파값을 1로 증가
    }

    public void DecreaseAlpha()
    {
        Debug.Log("시야 복구");
        StartCoroutine(ChangeAlpha(0f)); // 알파값을 0으로 감소
    }

    private IEnumerator ChangeAlpha(float targetAlpha)
    {
        Image img = transform.GetChild(1).GetComponent<Image>();
        if (img == null)
        {
            Debug.Log("자식 못 찾음");
            yield break; // 첫 번째 자식에 Image가 없으면 종료

        }

       

        float duration = 0.2f; // 변경에 걸리는 시간
        float elapsed = 0f;
        float startAlpha = img.color.a;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            Color color = img.color;
            img.color = new Color(color.r, color.g, color.b, alpha);
            yield return null;
        }

        // 최종적으로 정확한 값 설정
        Color finalColor = img.color;
        img.color = new Color(finalColor.r, finalColor.g, finalColor.b, targetAlpha);
    }

}
