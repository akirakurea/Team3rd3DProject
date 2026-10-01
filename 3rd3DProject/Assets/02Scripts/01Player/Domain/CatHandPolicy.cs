/// <summary>엔진과 입력 장치를 모르는 손 사용 규칙. 숫자·참/거짓 값만 받습니다.</summary>
public readonly struct CatHandInput
{
    public readonly bool Enabled, GrabPressed, GrabHeld, ToggleEquipment;
    public CatHandInput(bool enabled, bool grabPressed, bool grabHeld, bool toggleEquipment)
    { Enabled = enabled; GrabPressed = grabPressed; GrabHeld = grabHeld; ToggleEquipment = toggleEquipment; }
}

public readonly struct CatHandState
{
    public readonly bool Carrying, Equipped, PickupBusy, EquipmentReady;
    public CatHandState(bool carrying, bool equipped, bool pickupBusy, bool equipmentReady)
    { Carrying = carrying; Equipped = equipped; PickupBusy = pickupBusy; EquipmentReady = equipmentReady; }
}

public enum CatHandCommand { None, Pickup, Release, Equip, Unequip }

public static class CatHandPolicy
{
    public static CatHandCommand Decide(CatHandInput input, CatHandState state)
    {
        if (!input.Enabled) return state.Carrying ? CatHandCommand.Release : CatHandCommand.None;
        if (input.ToggleEquipment && state.Equipped) return CatHandCommand.Unequip;
        if (input.ToggleEquipment && !state.PickupBusy && state.EquipmentReady) return CatHandCommand.Equip;
        if (state.Carrying && !input.GrabHeld) return CatHandCommand.Release;
        if (input.GrabPressed && input.GrabHeld && !state.Carrying && !state.Equipped && !state.PickupBusy)
            return CatHandCommand.Pickup;
        return CatHandCommand.None;
    }
}

/// <summary>장비 표시 담당과 손 사용 규칙 사이의 작은 약속. Unity 타입이 없습니다.</summary>
public interface ICatEquipmentPort
{
    bool IsEquipped { get; }
    bool IsReady { get; }
    bool TryEquip();
    void Unequip();
}
