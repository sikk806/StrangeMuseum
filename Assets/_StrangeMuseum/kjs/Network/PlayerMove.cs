using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

public class PlayerMove : NetworkBehaviour
{
    public float Speed = 0.01f;
    public GameObject PlayerModel;

    private void Start()
    {
        PlayerModel.SetActive(false);
    }

    private void Update()
    {
        if (SceneManager.GetActiveScene().name == "NetworkTest")
        {
            if (PlayerModel.activeSelf == false)
            {
                SetPosition();
                PlayerModel.SetActive(true);
            }

            if(isLocalPlayer)
            {
                Movement();
            }
        }
    }

    public void SetPosition()
    {
        transform.position = new Vector3(0, 0, 0);
    }

    public void Movement()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 playerMovement = new Vector3(h, 0f, v);

        transform.position += playerMovement * Speed;
    }
}
