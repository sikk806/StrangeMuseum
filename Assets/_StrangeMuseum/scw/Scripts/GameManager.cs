using System.Collections.Generic;
using System.Text;
using TMPro;
using Mirror;
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

    public class PlayerStatDictionary : SyncDictionary<uint, string> { }
    public PlayerStatDictionary PlayerStat = new PlayerStatDictionary();
    [SyncVar] public int SecurityCount = 0; // 경비원 수
    [SyncVar] public int StatueCount = 0; // 석상 수
    [SyncVar] public bool IsAllConnected = false; // 모든 플레이어가 연결되었는지 여부

    // public GameResult GameResult;

    [SerializeField]
    public TextMeshPro taskList;

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

    public override void OnStartServer()
    {
        base.OnStartServer();
        CmdSpawnDoors();
    }

    private void Update()
    {
        if (isServer && !IsAllConnected && PlayerStat.Count == 4)
        {
            Debug.Log("모두 접속!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
            IsAllConnected = true;
        }

        //if (Input.GetKeyDown(KeyCode.Escape))
        //{
        //    SettingManager.Instance.gameObject.SetActive(
        //        !SettingManager.Instance.gameObject.activeSelf);
        //}
    }

    [Server]
    public void CmdSpawnDoors()
    {
        exitObj = Instantiate(exitPrefab);
        NetworkServer.Spawn(exitObj);
    }

    [Command(requiresAuthority = false)]
    public void CmdSpawnExit()
    {
        if (exitObj == null) return;

        int randomNumber = Random.Range(0, exitObj.transform.childCount);
        uint netId = exitObj.GetComponent<NetworkIdentity>().netId;

        RpcSpawnExit(netId, randomNumber);
    }

    [ClientRpc]
    public void RpcSpawnExit(uint netId, int index)
    {
        if (NetworkClient.spawned.TryGetValue(netId, out NetworkIdentity identity))
        {
            Transform childTransform = identity.transform.GetChild(index);
            childTransform.GetChild(0).gameObject.SetActive(false);
            childTransform.GetChild(1).gameObject.SetActive(true);
            //SoundManager.Instance.PlaySfx(doorClip);
        }
    }

    public void UpdateTaskList()
    {
        if (isServer) ServerUpdateTaskList();
        else CmdUpdateTaskList();
    }

    [Server]
    public void ServerUpdateTaskList()
    {
        string text = BuildTaskListString();
        RpcUpdateTaskList(text);
    }

    [Command(requiresAuthority = false)]
    public void CmdUpdateTaskList()
    {
        ServerUpdateTaskList();
    }

    public string BuildTaskListString()
    {
        StringBuilder sb = new StringBuilder();
        int count = 1;

        foreach (GameObject obj in inspectableObjectList)
        {
            string str = count + ". " + obj.GetComponent<InspectableObject>().GetTaskDetails();
            if (obj.GetComponent<InspectableObject>().GetIsInspectionComplete())
            {
                str = "<s>" + str + "</s>";
            }
            sb.AppendLine(str);
            count++;
        }

        return sb.ToString();
    }

    [ClientRpc]
    public void RpcUpdateTaskList(string text)
    {
        if (taskList != null)
        {
            taskList.text = text;
        }
    }

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
        CmdSpawnExit();
        Debug.Log("모든 임무가 완료되었습니다.");
    }

    /*
    [ClientRpc]
    public void RpcUpdateTaskList(string taskText)
    {
        bool isSecurity = false;
        uint localNetId = NetworkClient.localPlayer.netId;

        if (PlayerStat.ContainsKey(localNetId))
        {
            isSecurity = PlayerStat[localNetId] == "Security";
        }

        if (taskList != null)
        {
            taskList.text = taskText;

            if ((!isFirstTaskListUpdate && isServer) || (!isServer && isClient && isSecurity))
            {
                SoundManager.Instance.PlaySfx(taskListCheckClip);
            }
            isFirstTaskListUpdate = false;
        }
    }
     */

    [Command(requiresAuthority = false)]
    public void CmdUpdatePlayerCount(bool isSecurity, int value)
    {
        if (isSecurity) SecurityCount += value;
        else StatueCount += value;
    }
}
