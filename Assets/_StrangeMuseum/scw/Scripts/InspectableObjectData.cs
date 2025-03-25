using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Inspectable Object", menuName = "Scriptable Object/Inspectable Object")]
public class InspectableObjectData : ScriptableObject
{
    /*
     * 이 스크립트는 공통 임무를 진행하면서 상호작용하는 오브젝트(ex. 정수기, 장식품 등)가
     * 개별적인 이름, 점검 시간 등을 가질 수 있도록 하는 ScriptableObject 스크립트입니다.
     */

    public string objectName; // 오브젝트 이름
    public string taskDetail; // 업무 내용
    public float inspectionTimeRequired; // 점검에 필요한 시간

    public List<FloatListWrapper> spawnDatas = new List<FloatListWrapper>();
}

[System.Serializable]
public class FloatListWrapper
{
    public List<float> spawnData = new List<float>();  // 내부 리스트
}