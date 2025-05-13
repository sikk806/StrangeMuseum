using System.Collections;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class CoverInStatue : MonoBehaviour
{
    Quaternion originalLocalRotation;
    Vector3 originalLocalPosition;

    [SerializeField]
    float statueDuration; //지속시간. 

    [SerializeField]
    float statueStandDuration; //조각상 서 있을 때 지속시간. 
    private void Awake()
    {
        originalLocalRotation = transform.localRotation; // 자기 자신 기준으로

        originalLocalPosition = transform.localPosition;

    }

    public void PlayMoveEffect()
    {
        StartCoroutine(ChangeStatueRotation());
    }

    IEnumerator ChangeStatuePostion()
    {

        float elapsedTime = 0f;

        while(elapsedTime < statueDuration)
        {
            float tFactor = elapsedTime / statueDuration;

            float newY = Mathf.Lerp(originalLocalPosition.y, 0.7f, tFactor);
            transform.localPosition = new Vector3(originalLocalPosition.x, newY, originalLocalPosition.z);
            Debug.Log($"originalPosition.y: {originalLocalPosition.y}, transform.position.y: {transform.localPosition.y}");
            elapsedTime += Time.deltaTime;
            yield return null;
        }
  

    }
    IEnumerator ChangeStatueRotation()
    {

        Quaternion targetRotation = Quaternion.Euler(0f, -90f, 0f);

        float elapsedTime = 0f;

        bool hasStartedMoveY = false;

        while (elapsedTime < statueDuration)
        {

            float tFactor = elapsedTime / statueDuration;

            transform.localRotation = Quaternion.Slerp(originalLocalRotation, targetRotation, tFactor);

            if (!hasStartedMoveY && elapsedTime >= statueDuration * 0.5f)
            {
                hasStartedMoveY = true;
                StartCoroutine(ChangeStatuePostion());
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 목표 상태 보정
        this.transform.localRotation = targetRotation;
     

        yield return new WaitForSeconds(statueStandDuration);

        elapsedTime = 0f;
        while (elapsedTime < statueDuration)
        {
            float tFactor = elapsedTime / statueDuration;

            this.transform.localRotation = Quaternion.Slerp(targetRotation, originalLocalRotation, tFactor);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        this.transform.localRotation = originalLocalRotation;
        transform.localPosition = originalLocalPosition;
    }



}
