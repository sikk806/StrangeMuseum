using UnityEngine;

public class LookAtPlayer : MonoBehaviour
{
    public Transform player;

    private void Update()
    {
        if (player != null)
        {
            transform.LookAt(player);
            transform.Rotate(0, 180, 0);
        }
    }
}

