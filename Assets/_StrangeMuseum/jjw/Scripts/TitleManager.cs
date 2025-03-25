using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TitleManager : MonoBehaviour
{
    //옵션창 받아오기
    public SettingManager settingPopup;

    //연출을 위해 없어지는 오브젝트 받아오기
    public List<GameObject> Sculptures;
    public List<GameObject> HideSculptures;


    // 조형물 원래 위치 저장
    Dictionary<GameObject, Vector3> originPos = new Dictionary<GameObject, Vector3>();

    // 카메라 앞에 생성된 조형물
    GameObject visibleTransSculpture;
    // 몇초 정도 보여주고 없어지는지
    public float VisibleTime = 2f;
    // 활성화 가능 여부 체크
    bool isSpawn = false;

    [SerializeField]
    private LightManager lightManager;

    public AudioClip TitleBgm;
    public AudioClip ButtonSfx;

    void Start()
    {
        SoundManager.Instance.PlayBgm(TitleBgm);
        // 배경에 배치 된 조형물 위치 저장
        foreach (GameObject sculpture in Sculptures)
        {
            originPos[sculpture] = sculpture.transform.position;
        }
    }

    public bool IsSculptureCheck()
    {
        // 비활성화 된 조형물 체크
        foreach (GameObject sculpture in Sculptures)
        {
            if (sculpture.activeSelf) return false;
        }

        return true;
    }

    public void BlindSculpture()
    {
        //조형물 비활성화 해주기
        foreach (GameObject sculpture in Sculptures)
        {
            if (sculpture.activeSelf)
            {
                sculpture.SetActive(false);
                return;
            }
        }
    }

    public void RespawnRandomSculpture()
    {
        // 뒤에 배치 됐었던 조형물들 다시 생성해주는 거
        List<GameObject> hiddenSculpture = Sculptures.FindAll(s => !s.activeSelf);
        if (hiddenSculpture.Count == 0) return;

        int index = Random.Range(0, hiddenSculpture.Count);
        GameObject sculpture = hiddenSculpture[index];

        sculpture.transform.position = originPos[sculpture];
        sculpture.SetActive(true);
    }

    public void VisibleSculpture()
    {
        if (isSpawn) return; // 실행중이면 리턴
        isSpawn = true;

        // 카메라 앞에서 갑툭튀하는 조형물들 생성해주는 거
        List<GameObject> hideSculpture = HideSculptures.FindAll(s => !s.activeSelf);

        if (hideSculpture.Count == 0)
        {
            isSpawn = false;
            return;
        }

        int index = Random.Range(0, hideSculpture.Count);
        GameObject selectSculpture = hideSculpture[index];
        float randomDelay = Random.Range(5f, 7f);
        StartCoroutine(DelaySpawn(selectSculpture, randomDelay));

    }

    IEnumerator DelaySpawn(GameObject sculpture, float delay)
    {
        yield return new WaitForSeconds(delay);

        if (!lightManager.ScreenLight.enabled)
        {
            sculpture.SetActive(true);
            StartCoroutine(HideSculpture(sculpture, VisibleTime));
        }

        else
        {
            Debug.Log("Cancel");
        }

        isSpawn = false;
    }


    IEnumerator HideSculpture(GameObject sculpture, float delay)
    {
        Debug.Log($"[코루틴 시작] {sculpture.name} -> {delay}초 후 비활성화 예정");
        yield return new WaitForSeconds(delay);
        sculpture.SetActive(false);
        Debug.Log($"[비활성화 완료] {sculpture.name}");

        isSpawn = false;
    }


    // 이하 버튼들 // 

    public void OnClickGameStart()
    {
        // 나중에 씬 추가하기
        SceneManager.LoadScene("");
    }

    public void OnClickExitGame()
    {
        //게임종료
        SoundManager.Instance.PlaySfx(ButtonSfx);
        Application.Quit();
    }
    public void OnClickOption()
    {
        // 옵션창 관련
        SoundManager.Instance.PlaySfx(ButtonSfx);
        settingPopup.gameObject.SetActive(true);
    }
}
