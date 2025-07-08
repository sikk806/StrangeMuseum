using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

public class PipeRotate : MonoBehaviour
{
    private bool isRotating = false;
    private float rotateSpeed = 180f;
    public float accumulatedRotation = 0f;
    public float goalRotation;

    public void StartRotation()
    {
        if (!isRotating)
        {
            accumulatedRotation = 90f;
            StartCoroutine(RotateSmoothly());
        }
        else
        {
            accumulatedRotation += 90f;
        }
    }

    private IEnumerator RotateSmoothly()
    {
        isRotating = true;

        while (accumulatedRotation > 0f)
        {
            float step = rotateSpeed * Time.deltaTime;
            float angle = Mathf.Min(step, accumulatedRotation);
            transform.Rotate(Vector3.forward, angle);
            accumulatedRotation -= angle;
            yield return null;
        }

        isRotating = false;

        // 회전 끝났다고 매니저에 직접 알림
        if (PipeRotateManager.Instance != null)
        {
            PipeRotateManager.Instance.OnRotationComplete(this);
        }
    }

    public bool IsInCorrectRotation()
    {
        float z = transform.eulerAngles.z;
        float snapped = Mathf.Round(z / 90f) * 90f;
        return Mathf.Abs(z - snapped) < 1f;
    }
}
