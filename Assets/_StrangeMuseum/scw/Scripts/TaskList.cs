using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TaskList : MonoBehaviour
{
    /*
        * 이 스크립트는 Tab키를 통해 업무 일지를  펼칠 수 있는 기능을 담은 스크립트입니다.
        */

    private Animator taskListAnimator;
    private bool isSecurity = false;
    private bool prevTabPressed = false;
    private ulong clientId;

    [SerializeField]
    private AudioClip audioClip;

    void Start()
    {
        taskListAnimator = GetComponent<Animator>();
        clientId = NetworkManager.Singleton.LocalClientId;

        StartCoroutine(WaitForPlayerStat());
    }

    void Update()
    {
        if (GameManager.Instance.PlayerStat.Value.ContainsKey(clientId))
        {
            string role = GameManager.Instance.PlayerStat.Value[clientId].ToString();

            if (role == "Security")
            {
                isSecurity = true;
            }
            else
            {
                isSecurity = false;
            }
        }
        else
        {
            isSecurity = false;
        }

        if (!isSecurity) return;

        bool isTabPressed = Input.GetKey(KeyCode.Tab);

        if (isTabPressed != prevTabPressed)
        {
            // 상태 변경 시 사운드 출력
            SoundManager.Instance.PlaySfx(audioClip);

            // 이전 상태 변경
            prevTabPressed = isTabPressed;

            // 애니메이터 상태 변경
            taskListAnimator.SetBool("IsOpen", isTabPressed);
        }
    }

    IEnumerator WaitForPlayerStat()
    {
        while (!GameManager.Instance.PlayerStat.Value.ContainsKey(clientId))
        {
            Debug.Log("PlayerStat에 clientId가 추가될 때까지 대기 중");
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("PlayerStat에 추가 완료");
    }
}