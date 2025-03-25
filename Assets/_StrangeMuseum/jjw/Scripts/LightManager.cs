using System.Collections;
using UnityEngine;

public class LightManager : MonoBehaviour
{
    // 라이트 받아오기
    public Light ScreenLight;
   
    // 깜빡임 멈추는 확률
    private float flickeringPause = 0.5f;
    // 깜빡임 멈추는 시간 설정


    // 조형물 찾아오기
    public TitleManager TitleManager;

    void Start()
    {
        StartCoroutine(FlickerLight());
    }

    void Update()
    {
        if(TitleManager.IsSculptureCheck())
        {
            TitleManager.RespawnRandomSculpture();
        }
    }


    IEnumerator FlickerLight()
    {
        while(true)
        {
            // 깜빡이는거 가끔 멈추기
            if(Random.value < flickeringPause)
            {
                float randomPauseTime = Random.Range (3f, 7f); // 랜덤 정지
                yield return new WaitForSeconds(randomPauseTime);
            }

            // 깜빡이는 간격 늘리기
            float flickerDelay = Random.Range(1f, 3f);
            yield return new WaitForSeconds(flickerDelay);

            // 불 ON/OFF
            ScreenLight.enabled = !ScreenLight.enabled;
            Debug.Log($"[라이트] 상태 변경: {ScreenLight.enabled}");

            // 불이 꺼질 때 조형물 하나씩 없어지기
            if (!ScreenLight.enabled)
            {
                // 불이 꺼지면 활성화
                TitleManager.VisibleSculpture();
            }
            else if(ScreenLight.enabled)
            {
                // 불이 켜지면 비활성화
                TitleManager.BlindSculpture();
            }
                // 불이 꺼졌을 때 조형물이 활성화된 후 일정 시간이 지나면 다시 불이 켜짐
                yield return new WaitForSeconds(TitleManager.VisibleTime); // 조형물이 나타나는 시간 동안 대기 후 불 켜기
        }
    }
}
