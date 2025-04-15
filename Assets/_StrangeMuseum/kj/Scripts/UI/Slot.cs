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

    public ItemData.ItemUseType itemUseType = ItemData.ItemUseType.None; // 기본값: 자기 자신에게 사용
    public ItemData.ItemList itemList = ItemData.ItemList.None; // 기본값: 자기 자신에게 사용
}    
public class Slot : MonoBehaviour
{
    public int Number;

    public SlotData SlotData; // 해당 슬롯의 데이터만 연결

    [SerializeField]
    Sprite selectImage;

    [SerializeField]
    Sprite defalutImage;

    [SerializeField]
    TextMeshProUGUI ItemCountText; //중복 표시 텍스트

    public GameObject[] AssignedItem; // 해당 슬롯에 할당된 아이템 오브젝트

    private void Start()
    {

        Number = int.Parse(gameObject.name.Substring(gameObject.name.IndexOf("_") + 1));     



    }

    private void Update()
    {
        if (transform.childCount <= 0 && SlotData != null)
        {
            SlotData.IsEmpty = true;
        }
    }

    public void SlotItemCount(ItemData.ItemList item) //슬롯에 들어 있는 아이템 개수
    {
        if (ItemManager.Instance.inventoryDictionary.TryGetValue(item, out int count))
        {
            Debug.Log($"현재 {item}의 개수는 {count} 입니다");
            ItemCountText.text = count.ToString();
        }
        else
        {
            ItemCountText.text = "";
        }

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
