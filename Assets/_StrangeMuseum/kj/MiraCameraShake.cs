using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MiraCameraShake : NetworkBehaviour
{
    private Camera cam;

    private MiraController miraController;

    private void Awake()
    {
        cam = GetComponent<Camera>();
        miraController = GetComponentInParent<MiraController>();
       

    }

    [SerializeField]
    bool isShake;

    private void Update()
    {
        if (cam.enabled && isShake == false)
        {
            StartCoroutine(ShakeCoroutine(MiraCameraShakeDuration, MiraCameraShakeMagnitude));
            isShake = true;
        }
    }


    [SerializeField]
    float MiraCameraShakeDuration; //카메라 흔들리는 강도

    [SerializeField]
    float MiraCameraShakeMagnitude; //카메라 흔들리는 힘
    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        miraController.State = MiraController.CopyState.Die;

        Vector3 originalPos = cam.transform.localPosition;
        Quaternion originalRot = cam.transform.localRotation;

        // MainCam.transform.GetChild(0).gameObject.SetActive(false); //손과 손전등 비활성화
        // FingerLight.gameObject.SetActive(false);

        float elapsed = 0.0f;

        Debug.Log("카메라 흔들림");


        while (elapsed < duration)
        {
            float offsetX = Random.Range(-1f, 1f) * magnitude;
            float offsetY = Random.Range(-1f, 1f) * magnitude;

            cam.transform.localPosition = originalPos + new Vector3(offsetX, offsetY, 0f);

            elapsed += Time.deltaTime;
            yield return null;
        }
        cam.transform.localPosition = originalPos;
        cam.transform.localRotation = originalRot;
        Debug.Log("카메라 흔들리지 않음");


        yield return new WaitForSeconds(1.0f);

        Camera mainCam = GameObject.FindWithTag("MainCamera").GetComponent<Camera>();

        mainCam.enabled = true;
        cam.enabled = false;
        Debug.Log("미라 사라짐");

        // MainCam.transform.GetChild(0).gameObject.SetActive(true);
        // FingerLight.gameObject.SetActive(true);

        // MainCam.transform.position = originalPos; //> 충돌 전 마지막 카메라 위치로 돌아감

        miraController.CmdRequestDestroy();  // 소유 클라면 서버에 요청

        //SetPlayerState(PlayerState.Idle); //1. 기본 상태 변경 
    }
}