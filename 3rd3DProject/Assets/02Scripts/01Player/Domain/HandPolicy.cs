/// <summary>엔진과 입력 장치를 모르는 손 사용 규칙. 숫자·참/거짓 값만 받습니다.</summary>
public readonly struct HandInput
{
    public readonly bool Enabled, GrabPressed, GrabHeld, ToggleEquipment;
    public HandInput(bool enabled, bool grabPressed, bool grabHeld, bool toggleEquipment)
    { Enabled = enabled; GrabPressed = grabPressed; GrabHeld = grabHeld; ToggleEquipment = toggleEquipment; }
}

public readonly struct HandState
{
    public readonly bool Carrying, Equipped, PickupBusy, EquipmentReady;
    public HandState(bool carrying, bool equipped, bool pickupBusy, bool equipmentReady)
    { Carrying = carrying; Equipped = equipped; PickupBusy = pickupBusy; EquipmentReady = equipmentReady; }
}

public enum HandCommand { None, Pickup, Release, Equip, Unequip }

public static class HandPolicy
{
    public static HandCommand Decide(HandInput input, HandState state)
    {
        if (!input.Enabled) return state.Carrying ? HandCommand.Release : HandCommand.None;
        if (input.ToggleEquipment && state.Equipped) return HandCommand.Unequip;
        if (input.ToggleEquipment && !state.PickupBusy && state.EquipmentReady) return HandCommand.Equip;
        if (state.Carrying && !input.GrabHeld) return HandCommand.Release;
        if (input.GrabPressed && input.GrabHeld && !state.Carrying && !state.Equipped && !state.PickupBusy)
            return HandCommand.Pickup;
        return HandCommand.None;
    }
}

/// <summary>장비 표시 담당과 손 사용 규칙 사이의 작은 약속. Unity 타입이 없습니다.</summary>
public interface IEquipmentPort
{
    bool IsEquipped { get; }
    bool IsReady { get; }
    bool TryEquip();
    void Unequip();
}
