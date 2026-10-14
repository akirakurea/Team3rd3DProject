using UnityEngine;
using TexasJawTrap;

/// <summary>덫의 기존 동작을 F 인벤토리와 기존 Enemy 기절 API에 연결합니다.</summary>
[DisallowMultipleComponent, RequireComponent(typeof(ConsumableJawTrap), typeof(ItemPickup))]
public sealed class TrapPickup : MonoBehaviour, IItemPickupRule, IItemPickupConsumption
{
    ConsumableJawTrap trap;
    public bool CanPickup => trap && !trap.IsUsed;
    public bool ConsumeWorldInstance => true;
    void Awake() { trap = GetComponent<ConsumableJawTrap>(); }
    void OnEnable()
    {
        if (!trap) trap = GetComponent<ConsumableJawTrap>();
        trap.onTriggered.AddListener(HandleTriggered);
    }
    void OnDisable() { if (trap) trap.onTriggered.RemoveListener(HandleTriggered); }
    void HandleTriggered(GameObject actor)
    {
        if (actor && actor.TryGetComponent<ThiefController>(out var thief))
            thief.TakeStun(Mathf.Max(.01f, trap.removeDelay));
    }
}
