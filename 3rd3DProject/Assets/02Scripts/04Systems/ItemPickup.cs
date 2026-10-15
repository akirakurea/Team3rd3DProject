using UnityEngine;

/// <summary>월드에 놓인 아이템 표시용. 줍기는 ItemInteractor가 F키로 처리한다. (콜라이더 필요)</summary>
public class ItemPickup : MonoBehaviour
{
    [SerializeField] private HotbarItemData item;
    [SerializeField] private MonoBehaviour pickupRule;
    public HotbarItemData Item => item;
    public bool CanPickup => !pickupRule || !(pickupRule is IItemPickupRule rule) || rule.CanPickup;
    public bool ConsumeWorldInstance => pickupRule && pickupRule is IItemPickupConsumption consumption && consumption.ConsumeWorldInstance;
    public void Configure(HotbarItemData data, MonoBehaviour rule = null) { item = data; pickupRule = rule; }
}

/// <summary>사용 중인 덫처럼 현재 획득하면 안 되는 아이템의 조건입니다.</summary>
public interface IItemPickupRule { bool CanPickup { get; } }

/// <summary>데이터만 보관하고 다음 설치 때 새로 만드는 물건의 월드 인스턴스 정리 여부입니다.</summary>
public interface IItemPickupConsumption { bool ConsumeWorldInstance { get; } }
