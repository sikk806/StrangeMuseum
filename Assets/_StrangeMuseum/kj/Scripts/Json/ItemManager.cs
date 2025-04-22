using Newtonsoft.Json;
using NUnit.Framework;
using System.Collections.Generic;
using System.IO;
using Unity.Netcode;
using Unity.Services.CloudSave.Models;
using UnityEngine;


public enum InteractionType //유동적으로 바뀌기 때문에 ItemData에 추가하지 않음.
{
    None, PickUp, Used, Self, Target
}

[System.Serializable]
public class ItemData
{
    public enum ItemUseType //아이템 타입. 자신 or 상대에게 사용하는지
    {
        None, Self, Target
    }

    public enum ItemList //아이템 목록 - 경비원 
    {
        None, HandCuff, EnergyDrink, Box, Cover, Pen
    }

    public string ItemName; //아이템 이름
    public string ItemExplain; //아이템 기능 설명
    public int ItemMaxCount; //아이템 개수

    public ItemData(string itemName,string itemExplain , int itemCount)
    {
        this.ItemName = itemName;
        this.ItemExplain = itemExplain;
        this.ItemMaxCount = itemCount;
    }


}


public class ItemManager : MonoBehaviour
{
    private static ItemManager instance;
    public static ItemManager Instance
    {
        get
        {
            if (instance == null)
            {
                Debug.LogError("ItemManager 인스턴스가 null입니다. Initialize() 메서드를 호출해야 합니다.");
            }
            return instance;
        }
    }

    private void Awake()
    {
        if(instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Dictionary<ItemData.ItemList, ItemData> itemDictionary = new Dictionary<ItemData.ItemList, ItemData>();

    public Dictionary<ItemData.ItemList, int> inventoryDictionary = new Dictionary<ItemData.ItemList, int>();

    private string filePath;

    public ItemData ItemData;

    void Start()
    {
        filePath = Application.dataPath + "/_StrangeMuseum/Json/ItemFile.json";

        itemDictionary = new Dictionary<ItemData.ItemList, ItemData>
            {
                 { ItemData.ItemList.HandCuff, new ItemData("구속구", "조각상에게 사용 시, 조각상의 이동속도 및 돌진속도 감소",2) },
                 { ItemData.ItemList.EnergyDrink, new ItemData("에너지 드링크", "사용 시, 이동속도 증가",3) },
                 { ItemData.ItemList.Box, new ItemData("박스", "사용 시, 조각상의 공격으로 부터 1회 방어",2) },
                 { ItemData.ItemList.Cover, new ItemData("피 묻은 천", "조각상에게 사용 시, 조각상의 시야 기능 제한",2) },
                 { ItemData.ItemList.Pen, new ItemData("만년필", "조각상 적중 시, 조각상의 보이스 챗 기능 제한",4) }
            };

        SaveItemData();

        inventoryDictionary.Clear(); //인벤토리 초기화

    }

    public void SaveItemData()
    {
        string itemData = JsonConvert.SerializeObject(itemDictionary, Formatting.Indented); //Json 파일로 변환, Formatting.Indented 사용 시 잘 들여쓰기 됨

        File.WriteAllText(filePath, itemData); //읽을 수 있게
    }

    public void LoadItemData()
    {
        string itemData = File.ReadAllText(filePath);

        itemDictionary = JsonConvert.DeserializeObject<Dictionary<ItemData.ItemList, ItemData>>(itemData);


       
    }

    public void AddItem(ItemData.ItemList item)
    {
        if (!inventoryDictionary.ContainsKey(item)) 
        {
            inventoryDictionary.Add(item, 1);
        }
        else //아이템 중복 처리
        {
            int currentCount = inventoryDictionary[item];
            int maxCount = itemDictionary[item].ItemMaxCount;

            if (currentCount < maxCount)
            {
                inventoryDictionary[item]++;
                Debug.Log($"{item}을(를) 인벤토리에 추가했습니다. (현재 수량: {inventoryDictionary[item]})");
            }
            else
            {
                Debug.Log($"{item}의 최대 보유 수량({maxCount})에 도달했습니다. 더 이상 추가할 수 없습니다.");
                return;
            }
        }

        SaveItemData();
    }


    public void RemoveItem(ItemData.ItemList item)
    {
        if (inventoryDictionary.ContainsKey(item))
        {
            inventoryDictionary[item]--;

            Debug.Log($"{item} 1개 소모");

            if (inventoryDictionary[item] <= 0)
            {
                inventoryDictionary.Remove(item);
                itemDictionary.Remove(item); // 이 부분도 제거
                Debug.Log($"아이템 {item}이 모두 소모되어 삭제되었습니다.");
            }
            else
            {
                Debug.Log($"아이템 {item}의 남은 개수: {inventoryDictionary[item]}");
            }

            SaveItemData(); 
        }
    }

    public bool CountCurrentItem(ItemData.ItemList item)
    {
        if (inventoryDictionary[item] == itemDictionary[item].ItemMaxCount)
        {
            return true;
        }

        return false;
      
    }

}
