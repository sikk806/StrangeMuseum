using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Define;


[System.Serializable]
public class SlotData
{
    public bool IsEmpty; //슬롯 비어있는지 확인
    public GameObject SlotObj;
    public ItemLayer itemLayer = ItemLayer.None; // 기본값: 자기 자신에게 사용
    public ItemType itemType = ItemType.None; // 기본값: 자기 자신에게 사용
}    
public class Slot : MonoBehaviour
{
    public SlotData slotData = new SlotData();



    public int Number;

    private void Start()
    {
       

        Number = int.Parse(gameObject.name.Substring(gameObject.name.IndexOf("_") + 1));
      
    }

    private void Update()
    {

        if(transform.childCount <= 0)
        {
            SecurityInGameUI.Instance.SlotData[Number].IsEmpty = true;
        }
    }

    [SerializeField]
    Sprite selectImage;

    [SerializeField]
    Sprite defalutImage;
    public void SlotSelectImage()
    {
        this.GetComponent<Image>().sprite = selectImage;
    }

    public void SlotDefalutImage()
    {
        this.GetComponent<Image>().sprite = defalutImage;
    }

    public GameObject[] AssignedItem; // 해당 슬롯에 할당된 아이템 오브젝트


  
}
