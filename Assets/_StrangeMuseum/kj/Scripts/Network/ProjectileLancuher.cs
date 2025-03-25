using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class ProjectileLancuher : NetworkBehaviour
{
    //클라이언트나 서버에서 이 스크립트를 사용할 수 있으므로 구분해야함

    [SerializeField]
    GameObject serverProjectilePrefab;

    [SerializeField]
    Transform ThrowPos;

    [SerializeField]
    float throwForce = 10f; // 던지는 힘

    Pen pen;
    public void Attack(Pen pen)
    {
        this.pen = pen;
       


        if (IsServer)
        {
            OnThrowEvent();
        }
        else if (IsClient)
        {
            // 클라이언트에서는 서버에게 발사 요청을 보냄
          

            RequestFireServerRpc();
        }
    }
    [ServerRpc(RequireOwnership = false)]
    void RequestFireServerRpc() // 클라이언트에서 서버에게 발사 요청을 보냄
    {
       // FireServerRpc();
        OnThrowEvent();
    }


    public void OnThrowEvent()
    {
        GetComponent<Animator>().SetTrigger("Throw");
       
    }


    [ServerRpc(RequireOwnership = false)]
    void FireServerRpc() // 서버에서 호출되는 메서드
    {

        Debug.Log("서버에서 펜 던짐");

       

        // 서버에서 던질 펜 오브젝트 생성
        GameObject thrownPen = Instantiate(serverProjectilePrefab, ThrowPos.transform.position, transform.rotation);

        // 펜 오브젝트에 네트워크 오브젝트 할당
        NetworkObject penServerNetworkObject = thrownPen.GetComponent<NetworkObject>();
        penServerNetworkObject.Spawn(); // 네트워크 동기화

        // 펜 던지기
        Rigidbody rb = thrownPen.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 던질 방향 설정
            Vector3 throwDirection = transform.forward; // 던지는 방향
            rb.linearVelocity = throwDirection * throwForce; // 던지기
        }

        FireClientRpc(penServerNetworkObject.NetworkObjectId);
    }


    [ClientRpc]
    void FireClientRpc(ulong penNetworkObjectId) // 클라이언트에서 호출되는 메서드
    {
        // 클라이언트에서 이미 오너일 경우 반복을 방지
        GetComponent<SecurityInteraction>().CameraOff();

        NetworkObject penNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[penNetworkObjectId];


        // 클라이언트에서 던질 펜 오브젝트 생성
        Rigidbody rb = penNetworkObject.GetComponent<Rigidbody>();
        if (rb != null)
        {
            // 던질 방향 설정
            Vector3 throwDirection = transform.forward; // 던지는 방향
       
            rb.linearVelocity = throwDirection * throwForce; // 던지기
        }

        Debug.Log("펜 던짐 (클라이언트)");

        StartCoroutine(DelayThrow());
    }

    [SerializeField]
    float delay;
    IEnumerator DelayThrow()
    {
        yield return new WaitForSeconds(delay);
        // 클라이언트에게 펜 던지기 이벤트 전달
        GetComponent<SecurityInteraction>().CameraOn();
    }
}
