using Unity.Services.Lobbies.Models;
using UnityEngine;

public class UpDownHand : MonoBehaviour
{
    [SerializeField]
    Transform cameraTransform;

    private Vector3 OriginPosition;
    private Vector3 lastPosition;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        OriginPosition = transform.localPosition;
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 speedVector = lastPosition - transform.position;
        float speed = speedVector.magnitude;

        if (speed > 0.01f && speedVector.y < 0.01f && speedVector.y > -0.01f)
        {
            Vector3 offset = cameraTransform.up * Mathf.Sin(Time.time * 10f) * 0.01f;
            transform.localPosition = OriginPosition + offset;
        }
        lastPosition = transform.position;
    }
}
