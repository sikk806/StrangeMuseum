using UnityEngine;

public class Exit : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Bouncer"))
        {
            Debug.Log("경비원 탈출 성공");
           // GameManager.Instance.GameResult.ShowPopup(Winner.Security);
        }
    }
}
