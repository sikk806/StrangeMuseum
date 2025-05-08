using Mirror;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

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

        base.OnStartLocalPlayer();


        // 모든 경비원 오브젝트를 찾고, 로컬 클라이언트 ID와 비교하여 해당 경비원의 Interaction을 가져옴
        GameObject[] bouncers = GameObject.FindGameObjectsWithTag("Statue");

        int statueCount = 0;

        // foreach (var bouncer in bouncers)
        // {
        //     statueCount++;

          //  statueId = (uint)bouncer.GetComponent<PlayerLobbyController>().ConnectionID;

        //     Debug.Log("조각상   " + statueCount + "의 접속 ID" + statueId);

        //     statueInteraction = bouncer.GetComponent<StatueInteraction>();

        // }

    }

    private uint statueId;

    [SerializeField]
    TextMeshProUGUI FreezeTmp;

    private void Update()
    {
        //잠시 주석

        //if(statueController.GetPlayerState() == PlayerState.Freeze)
        //{
        //    FreezeTmp.text = "!!! 경비원 손전등 감지 중 !!! ";
        //}
        //else
        //{
        //    FreezeTmp.text = " ";
        //}


    }

    [SerializeField]
    private float CoverCooltime;


    public void CoverUI(NetworkIdentity BloodCover)
    {
        if (BloodCover.GetComponent<Cover>().isCoverUsing == false)
        {
            return;
        }

        if(isCoverUI) { Debug.Log("이미 시야 차단 UI 실행 중");  return; }

        StartCoroutine(CoverFunc(CoverCooltime,BloodCover));

    }

    [SerializeField]
    bool isCoverUI;
    public IEnumerator CoverFunc(float Cooltime, NetworkIdentity BloodCover)
    {
        isCoverUI = true;

        StartCoroutine(ChangeAlpha(1f)); // 알파값을 1로 증가

        yield return new WaitForSeconds(Cooltime);

        StartCoroutine(ChangeAlpha(0f)); // 알파값을 0으로 감소

        statueInteraction.CoverOnOffServerRpc(false, BloodCover);

        isCoverUI = false;
        BloodCover.GetComponent<Cover>().isCoverUsing = false; //한번 실행 하고 바로 FALSE. 
        BloodCover.GetComponent<Cover>().CoverGameObject.GetComponent<Cover>().ResetInteractServerRpc();
        
    }

    private IEnumerator ChangeAlpha(float targetAlpha)
    {
        UnityEngine.UI.Image img = transform.GetChild(1).GetComponent<UnityEngine.UI.Image>();

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
