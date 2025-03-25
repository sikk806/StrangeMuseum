using UnityEngine;

public class Exit : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Bouncer"))
        {
            GameManager.Instance.GameResult.ShowPopup(Winner.Security);
        }
    }
}
