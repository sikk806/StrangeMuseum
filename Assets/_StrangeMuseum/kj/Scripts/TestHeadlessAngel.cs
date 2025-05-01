using Mirror;
using UnityEngine;

public class TestHeadlessAngel : NetworkBehaviour
{
    // Rush Speed 적용시키는 버전으로 업데이트 예정
    [SyncVar]
    public float RushSpeed;
    [SyncVar]
    // RushSpeed와 함께 조절
    public float initRushSpeed;

    // Rush Speed 적용시키는 버전으로 업데이트 예정
    [SyncVar]
    public float MoveSpeed;
    [SyncVar]
    // RushSpeed와 함께 조절
    public float initMoveSpeed;
    void Start()
    {
        initRushSpeed = RushSpeed;
        initMoveSpeed = MoveSpeed;
    }
}
