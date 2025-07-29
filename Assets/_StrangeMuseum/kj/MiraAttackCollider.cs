using UnityEngine;
using static MiraController;

public class MiraAttackCollider : MonoBehaviour
{
    bool isCollider;

    private void OnTriggerEnter(Collider other)
    {
        if(other.gameObject.CompareTag("Bouncer") && !isCollider)
        {
            Debug.LogWarning("경비원과 충돌 (미라 공격 콜라이더 스크립트) ");

            GetComponentInParent<MiraController>().State = CopyState.Die;  

            isCollider = true;
        }
    }
}