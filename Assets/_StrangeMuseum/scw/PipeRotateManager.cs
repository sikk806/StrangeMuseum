using System.Collections.Generic;
using UnityEngine;

public class PipeRotateManager : MonoBehaviour
{
    public static PipeRotateManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }

    [SerializeField] private List<GameObject> pipeList = new List<GameObject>();

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit))
            {
                PipeRotate clickRotate = hit.transform.GetComponent<PipeRotate>();
                if (clickRotate != null)
                {
                    clickRotate.StartRotation();
                }
            }
        }
    }

    // 회전이 끝났을 때 오브젝트가 직접 호출
    public void OnRotationComplete(PipeRotate rotator)
    {
        Debug.Log($"{rotator.name} 회전 완료!");

        foreach (GameObject pipe in pipeList)
        {
            PipeRotate pipeRotate = pipe.GetComponent<PipeRotate>();
            
            Debug.Log(pipeRotate.goalRotation + ", " + pipe.transform.eulerAngles.z);

            if (Mathf.Abs(Mathf.DeltaAngle(pipeRotate.goalRotation, pipe.transform.eulerAngles.z)) > 1f)
            {
                Debug.Log($"{pipe.name} 각도가 다름");
                return;
            }
        }

        Debug.Log($"미션 완료");
    }
}
