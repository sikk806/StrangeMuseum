using UnityEngine;

public class Define 
{
    //임시 - 정식님이 역할부여 관련 코드 만들면 그때 ㄱㄱ
    public enum RoleType
    {
        None,
        Security,
        Statue
    }

    public enum InteractionType
    {
        None,       // 비활성화
        PickUp,     // 아이템 줍기
        Used,   // 아이템 사용
        Self,   // 자기 자신에게 사용하는 아이템
        Target  // 상대에게 사용하는 아이템
    }

    public enum ItemUseType
    {
        None,
        Self,   // 자기 자신에게 사용하는 아이템
        Target  // 상대에게 사용하는 아이템
    }


    public enum ItemList
    {
        None,
        HandCuff,
        EnergyDrink,
        Box,
        Cover,
        Pen,
    }
}
