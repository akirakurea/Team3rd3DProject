using UnityEngine;

/// <summary>
/// 핫바에 들어갈 아이템/도구 한 종류의 데이터.
/// Project 창에서 우클릭 > Create > Game > Hotbar Item 으로 에셋을 만든다.
/// 실제 사용 효과(쿨타임, 범위 등)는 고양이 컨트롤러 쪽에서 itemId로 분기해서 처리한다.
/// </summary>
[CreateAssetMenu(fileName = "Item_", menuName = "Game/Hotbar Item")]
public class HotbarItemData : ScriptableObject
{
    public string itemId = "trap";          // 코드에서 분기할 때 쓰는 식별자
    public string displayName = "덫";
    
    [TextArea] public string description;
    public Sprite icon;
    public GameObject placePrefab;   // 추가: F키로 설치될 오브젝트
}