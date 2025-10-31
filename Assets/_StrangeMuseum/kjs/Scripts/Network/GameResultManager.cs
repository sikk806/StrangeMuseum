using System;
using UnityEngine;
using Mirror;
using System.Collections;
using Unity.Netcode;

public class GameResultManager : Mirror.NetworkBehaviour
{
    public static GameResultManager Instance;
    public bool IsGamePaused = false;

    public int SecurityCount = 0;
    public int StatueCount = 0;

    private bool gameEnd = false;

    public void SetCharacterCount(int securityNum, int statueNum)
    {
        SecurityCount += securityNum;
        StatueCount += statueNum;
    }

    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        //if (!isServer) return;
        if (SecurityCount == 0) gameEnd = true;
        if (gameEnd == true && StatueCount > 1)
        {
            SecurityCount = -1;
            gameEnd = false;
            StartCoroutine("GameResult");
        }
        //Debug.Log(SecurityCount);
    }

    IEnumerator GameResult()
    {
        IsGamePaused = true;
        yield return null;
    }

}