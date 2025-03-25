using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class InspectableObjectSpawner : NetworkBehaviour
{
    /*
     * 이 스크립트는 임무 관련 오브젝트를 Spawn하는 스크립트입니다.
     */

    // A형 임무를 위해 맵에 배치된 오브젝트 리스트
    [SerializeField] 
    private List<GameObject> mapObjectsForTaskTypeA = new List<GameObject>();

    // A형 임무를 위해 생성되는 오브젝트 리스트
    [SerializeField] 
    private List<GameObject> replacementObjectsForTaskTypeA = new List<GameObject>();

    // B형 임무를 위해 맵에 배치된 오브젝트 리스트
    [SerializeField] 
    private List<GameObject> mapObjectsForTaskTypeB = new List<GameObject>();

    // B형 임무를 위해 생성되는 오브젝트 리스트
    [SerializeField] 
    private List<GameObject> replacementObjectsForTaskTypeB = new List<GameObject>();

    // C형 임무를 위해 생성되는 오브젝트 리스트
    [SerializeField] 
    private List<GameObject> newObjectsForTaskTypeC = new List<GameObject>();

    private List<ulong> objIdList = new List<ulong>();
    private List<int> indexList = new List<int>();

    void Start()
    {
        if (IsServer)
        {
            Debug.Log("이 플레이어가 Host(서버)입니다.");
            GenerateTaskServerRpc();
        }
        else
        {
            Debug.Log("이 플레이어는 클라이언트입니다.");
            GameManager.Instance.RequestTaskListServerRpc();
            RequestDisableObjectServerRpc();
        }
    }

    [ServerRpc]
    private void GenerateTaskServerRpc()
    {
        SpawnObjectForTaskTypeA();
        SpawnObjectForTaskTypeB();
        SpawnObjectForTaskTypeC();
        GameManager.Instance.UpdateTaskListServerRpc();
    }

    void SpawnObjectForTaskTypeA() // A형 임무를 생성하는 함수
    {
        /*
         * A형 임무는 맵에 1개만 배치되어있는 오브젝트를 대체하여 생성됨 (ex. 화석, 파라오 등)
         * 가까이 있는 오브젝트끼리 임무가 같이 나오지 않도록 제한
         */

        // 특정 조합을 저장하는 HashSet
        HashSet<(int, int)> restrictedCombinations = new HashSet<(int, int)>
        {
            (1, 2), (2, 1),
            (1, 3), (3, 1),
            (2, 3), (3, 2),
            (4, 5), (5, 4)
        };

        // 첫 번째 번호 뽑기
        int firstIndex = Random.Range(0, mapObjectsForTaskTypeA.Count);

        /*
         * 두 번째 번호 뽑기
         * HashSet을 이용해 특정 조합의 인덱스를 제외할 수 있음
         */

        List<int> possibleIndexes = new List<int>();

        for (int i = 0; i < mapObjectsForTaskTypeA.Count; i++)
        {
            if (i != firstIndex && !restrictedCombinations.Contains((firstIndex, i)))
            {
                possibleIndexes.Add(i);
            }
        }

        // 가능한 조합 중 두 번째 번호 뽑기
        int secondIndex = possibleIndexes[Random.Range(0, possibleIndexes.Count)];

        // A 임무 오브젝트 소환
        for (int i = 0; i < mapObjectsForTaskTypeA.Count; i++)
        {
            GameObject obj;

            if (i == firstIndex || i == secondIndex)
            {
                obj = replacementObjectsForTaskTypeA[i];
            }
            else
            {
                obj = mapObjectsForTaskTypeA[i];
            }

            // 새로운 인스턴스 생성
            GameObject spawnObj = Instantiate(obj, obj.transform.position, obj.transform.rotation);
            SpawnObjectToNetwork(spawnObj);

            if (i == firstIndex || i == secondIndex)
            {
                // GameManager 내 임무 리스트에 추가
                GameManager.Instance.inspectableObjectList.Add(spawnObj);
            }
        }
    }

    void SpawnObjectForTaskTypeB() // B형 임무를 생성하는 함수
    {
        /*
         * B형 임무는 맵에 여러개 배치되어있는 오브젝트 중 1개를 대체하여 생성됨 (ex. 안내판, 진열장 등)
         */

        // 2개의 숫자 뽑기
        HashSet<int> uniqueNumbers = new HashSet<int>();

        while (uniqueNumbers.Count < 2)
        {
            int randomNumber = Random.Range(0, mapObjectsForTaskTypeB.Count);
            uniqueNumbers.Add(randomNumber);
        }

        // HashSet은 순서가 없으므로 리스트로 변환하여 사용 가능
        List<int> randomTwoNumbers = new List<int>(uniqueNumbers);

        for (int i = 0; i < mapObjectsForTaskTypeB.Count; i++)
        {
            if (i == randomTwoNumbers[0] || i == randomTwoNumbers[1])
            {
                int randomNumber = Random.Range(0, mapObjectsForTaskTypeB[i].transform.childCount);

                // 오브젝트를 먼저 생성
                GameObject obj = Instantiate(mapObjectsForTaskTypeB[i]);

                Transform objTransform = obj.transform.GetChild(randomNumber);

                SpawnObjectToNetwork(obj);

                objIdList.Add(obj.GetComponent<NetworkObject>().NetworkObjectId);
                indexList.Add(randomNumber);

                // 프리팹을 먼저 생성 (부모 포함)
                GameObject spawnObj = Instantiate(replacementObjectsForTaskTypeB[i], objTransform.position, objTransform.rotation);

                // 안내판의 경우 rotation 필요
                if (spawnObj.GetComponent<InspectableObject>().inspectableObjectData.objectName == "안내판")
                {
                    Vector3 newRotation = spawnObj.transform.rotation.eulerAngles;
                    newRotation.x += 180f; newRotation.z += 180f;
                    spawnObj.transform.rotation = Quaternion.Euler(newRotation);
                }

                SpawnObjectToNetwork(spawnObj);

                GameManager.Instance.inspectableObjectList.Add(spawnObj);
            }
            else
            {
                // 오브젝트를 먼저 생성
                GameObject obj = Instantiate(mapObjectsForTaskTypeB[i]);
                SpawnObjectToNetwork(obj);
            }
        }

        DisableObjectServerRpc();
    }

    // 클라이언트가 서버에게 비활성화한 오브젝트 동기화를 요청하는 ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void RequestDisableObjectServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("클라이언트가 서버에게 비활성화한 오브젝트 동기화 요청");
        DisableObjectServerRpc();
    }

    [ServerRpc]
    public void DisableObjectServerRpc()
    {
        for (int i = 0; i< objIdList.Count; i++)
        {
            DisableObjectClientRpc(objIdList[i], indexList[i]);
        }
    }

    [ClientRpc]
    public void DisableObjectClientRpc(ulong objId, int index)
    {
        GameObject obj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objId].gameObject;
        Transform childTransform = obj.transform.GetChild(index);
        childTransform.gameObject.SetActive(false);
    }

    void SpawnObjectForTaskTypeC() // C형 임무를 생성하는 함수
    {
        /*
         * C형 임무는 맵에 존재하지 않은 오브젝트 1개가 생성됨 (ex. 정수기, 쓰레기통 등)
         */

        for (int i = 0; i < 4; i++)
        {
            int dice1 = Random.Range(0, newObjectsForTaskTypeC.Count);
            int dice2 = Random.Range(0, newObjectsForTaskTypeC[dice1].GetComponent<InspectableObject>().inspectableObjectData.spawnDatas.Count);

            float positionX = newObjectsForTaskTypeC[dice1].GetComponent<InspectableObject>().inspectableObjectData.spawnDatas[dice2].spawnData[0];
            float positionY = newObjectsForTaskTypeC[dice1].GetComponent<InspectableObject>().inspectableObjectData.spawnDatas[dice2].spawnData[1];
            float positionZ = newObjectsForTaskTypeC[dice1].GetComponent<InspectableObject>().inspectableObjectData.spawnDatas[dice2].spawnData[2];

            float rotationX = newObjectsForTaskTypeC[dice1].GetComponent<InspectableObject>().inspectableObjectData.spawnDatas[dice2].spawnData[3];
            float rotationY = newObjectsForTaskTypeC[dice1].GetComponent<InspectableObject>().inspectableObjectData.spawnDatas[dice2].spawnData[4];
            float rotationZ = newObjectsForTaskTypeC[dice1].GetComponent<InspectableObject>().inspectableObjectData.spawnDatas[dice2].spawnData[5];

            GameObject obj = Instantiate(newObjectsForTaskTypeC[dice1], new Vector3(positionX, positionY, positionZ), Quaternion.Euler(rotationX, rotationY, rotationZ));
            SpawnObjectToNetwork(obj);

            GameManager.Instance.inspectableObjectList.Add(obj);

            newObjectsForTaskTypeC.RemoveAt(dice1);
        }
    }

    private void SpawnObjectToNetwork(GameObject obj)
    {
        // 네트워크 오브젝트 생성 후 Spawn()
        if (obj.TryGetComponent(out NetworkObject netObj))
        {
            // 서버에서만 Spawn() 호출
            if (NetworkManager.Singleton.IsServer && !netObj.IsSpawned)
            {
                netObj.Spawn();
            }
        }
    }
}