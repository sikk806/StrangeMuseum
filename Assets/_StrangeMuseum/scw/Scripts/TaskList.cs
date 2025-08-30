using System.Collections;
using Mirror;
using TMPro;
using UnityEngine;

public class TaskList : MonoBehaviour
{
    /*
    * 이 스크립트는 Tab키를 통해 업무 일지를  펼칠 수 있는 기능을 담은 스크립트입니다.
    */

    private Animator taskListAnimator;
    private bool prevTabPressed = false;
    private uint clientId;

    [SerializeField]
    private AudioClip audioClip;

    [SerializeField]
    private TextMeshPro text;

    void Start()
    {
        taskListAnimator = GetComponent<Animator>();
        clientId = NetworkClient.localPlayer.netId;

        GameManager.Instance.taskList = text;

        StartCoroutine(WaitForPlayerStat());
    }

    void Update()
    {
        bool isTabPressed = Input.GetKey(KeyCode.Tab);

        if (isTabPressed != prevTabPressed)
        {
            Debug.Log("탭키 누름");

            // 상태 변경 시 사운드 출력
            //SoundManager.Instance.PlaySfx(audioClip);

            // 이전 상태 변경
            prevTabPressed = isTabPressed;

            // 애니메이터 상태 변경
            taskListAnimator.SetBool("IsOpen", isTabPressed);
        }
    }

    IEnumerator WaitForPlayerStat()
    {
        while (!GameManager.Instance.PlayerStat.ContainsKey(clientId))
        {
            Debug.Log("PlayerStat에 clientId가 추가될 때까지 대기 중");
            yield return new WaitForSeconds(0.5f);
        }

        Debug.Log("PlayerStat에 추가 완료");
    }
}