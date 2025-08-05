using Mirror;
using UnityEngine;

public class MiraAttackCollider : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        //if (other.gameObject.CompareTag("Bouncer"))
        //{
        //    if(isOwned == false)
        //    {
        //        Transform mirahead = transform.GetChild(1);
        //        other.GetComponent<SecurityController>().TestMira(this, mirahead);
        //        Debug.LogWarning("경비원과 충돌 - isOwned = false (미라 공격 콜라이더 스크립트) ");
        //    }
        //}
    }
}