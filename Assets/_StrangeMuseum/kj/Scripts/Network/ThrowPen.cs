using UnityEngine;

public class ThrowPen : MonoBehaviour
{

    [SerializeField]
    float voiceUsingCoolTime;

    private void OnTriggerEnter(Collider other)
    {

        //if(other.CompareTag("Statue"))
        //{
        //    Debug.Log(this.gameObject.name + "와 조각상 충돌");
        //    StatueInteraction statueInteraction = other.GetComponent<StatueInteraction>();
        //    statueInteraction.ThrowPenFuca();
        //}
    }


    private void OnCollisionEnter(Collision collision)
    {

        if (collision.gameObject.tag == "Statue")
        {
            Debug.Log(this.gameObject.name + "와 조각상 충돌");
            StatueInteraction statueInteraction = collision.gameObject.GetComponent<StatueInteraction>();
            StartCoroutine(statueInteraction.ThrowPenFuca(voiceUsingCoolTime));
        }
    }
}
