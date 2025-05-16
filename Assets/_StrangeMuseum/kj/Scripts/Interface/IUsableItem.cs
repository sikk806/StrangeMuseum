using UnityEngine;

public interface IUsableItem
{
    public void UseServerRpc(uint id); //아이템 사용
    public int GetItemEmptyIndex(Slot slot); //같은 아이템들을 저장할 수 있는 오브젝트 배열 중 빈 인덱스 가져오는 메서드

    public int GetItemlayer();


    public ItemData.ItemList GetItemList(); //아이템 이름 가져오기
    public ItemData.ItemUseType GetItemType(); //아이템 타입 가져오기
}
