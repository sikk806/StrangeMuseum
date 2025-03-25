using System.Collections.Generic;
using System.Text;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    private static GameManager instance;
    public static GameManager Instance
    {
        get
        {
            return instance;
        }
    }

    [HideInInspector]
    public NetworkVariable<Dictionary<ulong, FixedString128Bytes>> PlayerStat =
        new NetworkVariable<Dictionary<ulong, FixedString128Bytes>>(new Dictionary<ulong, FixedString128Bytes>()); // 플레이어 상태 저장

    //[HideInInspector]
    public NetworkVariable<int> SecurityCount = new NetworkVariable<int>(0); // 경비원 수

    //[HideInInspector]
    public NetworkVariable<int> StatueCount = new NetworkVariable<int>(0); // 석상 수        
    public NetworkVariable<bool> IsAllConnected = new NetworkVariable<bool>(false); // 모든 플레이어가 연결되었는지 여부

    public GameResult GameResult;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this; // instance 초기화
        }
        else
        {
            Destroy(gameObject); // 중복 방지
        }
    }

    void Start()
    {
        if (IsServer)
        {
            SpawnDoorsServerRpc();
        }

        SecurityCount.OnValueChanged += OnSecurityCountChanged;
        StatueCount.OnValueChanged += OnStatueCountChanged;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SettingManager.Instance.gameObject.SetActive(
                !SettingManager.Instance.gameObject.activeSelf);
        }

        if (IsServer && !IsAllConnected.Value && PlayerStat.Value.Count == 4)
        {
            Debug.Log("모두 접속!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            IsAllConnected.Value = true;
        }

        Debug.Log("SecurityCount: " + SecurityCount.Value);
        Debug.Log("StatueCount: " + StatueCount.Value);
    }

    [ServerRpc]
    public void SpawnDoorsServerRpc()
    {
        exitObj = Instantiate(exitPrefab);

        if (exitObj.TryGetComponent(out NetworkObject netObj))
        {
            if (NetworkManager.Singleton.IsServer && !netObj.IsSpawned)
            {
                netObj.Spawn();
            }
        }
    }

    [SerializeField]
    private TextMeshPro taskList;

    public List<GameObject> inspectableObjectList = new List<GameObject>(); // 임무 관련 오브젝트를 저장하는 리스트

    [SerializeField]
    private GameObject exitPrefab;
    private GameObject exitObj;

    [SerializeField]
    private AudioClip doorClip;
    [SerializeField]
    private AudioClip taskListCheckClip;
    private string ServerTaskList = "";
    private bool isFirstTaskListUpdate = true;

    public void CheckAllTaskFinish()
    {
        foreach (GameObject obj in inspectableObjectList)
        {
            if (!obj.GetComponent<InspectableObject>().GetIsInspectionComplete()) // 수행 여부 체크
            {
                Debug.Log("아직 모든 임무가 완료되지 않았습니다.");
                return;
            }
        }

        // 모든 임무 수행 완료
        SpawnExitServerRpc();
        Debug.Log("모든 임무가 완료되었습니다.");
    }

    [ServerRpc]
    public void SpawnExitServerRpc()
    {
        int randomNumber = Random.Range(0, exitObj.transform.childCount);
        SpawnExitClientRpc(exitObj.GetComponent<NetworkObject>().NetworkObjectId, randomNumber);
    }

    [ClientRpc]
    public void SpawnExitClientRpc(ulong objId, int index)
    {
        GameObject obj = NetworkManager.Singleton.SpawnManager.SpawnedObjects[objId].gameObject;
        Transform childTransform = obj.transform.GetChild(index);

        childTransform.GetChild(0).gameObject.SetActive(false);
        childTransform.GetChild(1).gameObject.SetActive(true);

        SoundManager.Instance.PlaySfx(doorClip);
    }

    // 클라이언트가 서버에게 taskList 동기화를 요청하는 ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void RequestTaskListServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("클라이언트가 서버에게 taskList 동기화 요청");
        LoadTaskListServerRpc();
    }

    [ServerRpc]
    public void LoadTaskListServerRpc()
    {
        LoadTaskListClientRpc(ServerTaskList);
    }

    [ClientRpc]
    public void LoadTaskListClientRpc(string taskText)
    {
        if (taskList != null)
        {
            taskList.text = taskText;
        }
    }

    // 서버에서 임무 리스트를 갱신하고, 클라이언트들에게 업데이트를 알리는 함수
    [ServerRpc]
    public void UpdateTaskListServerRpc()
    {
        if (taskList == null)
        {
            Debug.Log("GameManager에 taskList가 할당되지 않았습니다.");
            return;
        }

        StringBuilder sb = new StringBuilder();
        int count = 1;

        // 서버에서 inspectableObjectList 갱신
        foreach (GameObject obj in inspectableObjectList)
        {
            string str = count + ". " + obj.GetComponent<InspectableObject>().GetTaskDetails();

            if (obj.GetComponent<InspectableObject>().GetIsInspectionComplete()) // 수행 여부 체크
            {
                str = "<s>" + str + "</s>"; // 수행한 임무는 중간 취소선 삽입
            }

            sb.AppendLine(str);
            count++;
        }

        ServerTaskList = sb.ToString();

        // 클라이언트들에게 동기화
        UpdateTaskListClientRpc(sb.ToString());
    }

    // 클라이언트들에게 임무 목록을 동기화하는 RPC
    [ClientRpc]
    public void UpdateTaskListClientRpc(string taskText)
    {
        ulong clientId = NetworkManager.Singleton.LocalClientId;
        bool isSecurity = false;

        if (GameManager.Instance.PlayerStat.Value.ContainsKey(clientId))
        {
            string role = GameManager.Instance.PlayerStat.Value[clientId].ToString();

            if (role == "Security")
            {
                isSecurity = true;
            }
            else
            {
                isSecurity = false;
            }
        }
        else
        {
            isSecurity = false;
        }

        if (taskList != null)
        {
            taskList.text = taskText;

            if ((!isFirstTaskListUpdate && IsServer) || (!IsServer && IsClient && isSecurity))
            {
                SoundManager.Instance.PlaySfx(taskListCheckClip);
            }
            isFirstTaskListUpdate = false;
        }
    }

    private void OnSecurityCountChanged(int oldValue, int newValue)
    {
        if (IsServer && SecurityCount.Value <= 0 && IsAllConnected.Value)
        {
            Debug.Log(SecurityCount.Value);
            GameResult.ShowPopup(Winner.HeadlessAngel);
        }
    }

    private void OnStatueCountChanged(int oldValue, int newValue)
    {
        if (IsServer && StatueCount.Value <= 0 && IsAllConnected.Value)
        {
            Debug.Log(StatueCount.Value);
            GameResult.ShowPopup(Winner.Security);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdatePlayerCountServerRpc(bool isSecurity, int value)
    {
        if (isSecurity)
        {
            SecurityCount.Value += value;
        }
        else
        {
            StatueCount.Value += value;
        }
    }
}
