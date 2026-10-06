using UnityEngine;

/// <summary>월드에 놓인 아이템 표시용. 줍기는 ItemInteractor가 F키로 처리한다. (콜라이더 필요)</summary>
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private HotbarItemData item;
    public HotbarItemData Item => item;
}