using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ItemController : MonoBehaviour //각 아이템에 부착
{

    private GameObject currentItem;

    [SerializeField]
    public float RotateSpeed = 3f;
    private Vector3 targetPosition;

    public bool IsItemView = false;


    [SerializeField]
    private float ItemHeight = 0.5f; //아이템이 화면 기준 어디로 가게 할지 높이.


    private float InitSpeed;

    private float yaw = 0f;
    private float pitch = 0f;

    private void Start()
    {
        InitSpeed = RotateSpeed;
    }


    void Update()
    {
        if (currentItem != null)
        {
            if (Input.GetMouseButton(0) && IsItemView)
            {
                float mouseX = Input.GetAxis("Mouse X");
                float mouseY = Input.GetAxis("Mouse Y");

                transform.Rotate(Vector3.up, -mouseX * RotateSpeed, Space.World); 
                transform.Rotate(Vector3.right, mouseY * RotateSpeed, Space.Self); 
            }

            else if(IsItemView == false)
            {
                currentItem.transform.position = Vector3.Lerp(currentItem.transform.position, targetPosition, Time.deltaTime * 5f);

                if (Vector3.Distance(currentItem.transform.position, targetPosition) < 0.01f)
                {
                    IsItemView = true;
                }
            }

        }
    }

    public void RotateSpeedSet()
    {
        RotateSpeed = InitSpeed;
    }
    public void ViewCreateItem(GameObject go)
    {
        currentItem = go;

        Camera cam = Camera.main;
        if (cam != null)
        {
            currentItem.transform.position = cam.transform.position + cam.transform.up * ItemHeight;
            currentItem.transform.rotation = Quaternion.identity;

            // 목표 위치: 화면 정중앙 기준, z=2f는 카메라 앞으로의 거리
            Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 2f);
            targetPosition = cam.ScreenToWorldPoint(screenCenter);

          
        }
    }
}