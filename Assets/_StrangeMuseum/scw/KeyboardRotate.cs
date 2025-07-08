using UnityEngine;

public class KeyboardRotateZ : MonoBehaviour
{
    public float rotateSpeed = 90f; // 초당 90도 회전
    Rigidbody rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal != 0f)
        {
            Quaternion delta = Quaternion.Euler(0, 0, -horizontal * rotateSpeed * Time.fixedDeltaTime);
            rb.MoveRotation(rb.rotation * delta);
        }
    }
}