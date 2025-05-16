using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static ItemData;


[System.Serializable]
public class SlotData
{
    public bool IsEmpty; //슬롯 비어있는지 확인

    public GameObject SlotObj;
    public int OriginalIndex; // <- 추가
    public ItemData.ItemUseType itemUseType = ItemData.ItemUseType.None; // 기본값: 자기 자신에게 사용
    public ItemData.ItemList itemList = ItemData.ItemList.None; // 기본값: 자기 자신에게 사용
}    
public class Slot : MonoBehaviour
{
    public int SlotNumber;
    public SlotData SlotData; // 해당 슬롯의 데이터만 연결

    [SerializeField]
    Sprite selectImage;

    [SerializeField]
    Sprite defalutImage;

    [SerializeField]
     public TextMeshProUGUI ItemCountText; //중복 표시 텍스트

    public GameObject[] AssignedItem; // 해당 슬롯에 할당된 아이템 오브젝트

    private void Update()
    {
        if (transform.childCount <= 0 && SlotData != null)
        {
            SlotData.IsEmpty = true;
        }
    }

    public void SlotItemCount(ItemData.ItemList item,bool isAdd)
    {
        int count = 0;

        foreach (GameObject obj in AssignedItem)
        {
            if (obj != null)
            {
                if(isAdd && obj.GetComponent<IUsableItem>().GetItemList() == item)
                {
                    count++;
                    Debug.Log(item + "의 " + count + "만큼 증가");
                }
                if(isAdd == false)
                {
                    count--;
                    Debug.Log(item + "의 " + count + "만큼 감소");
                }
               
            }
        }

        ItemCountText.text = count.ToString();
    }


    public void SlotSelectImage()
    {
        this.GetComponent<Image>().sprite = selectImage;
    }

    public void SlotDefalutImage()
    {
        this.GetComponent<Image>().sprite = defalutImage;
    }



}
