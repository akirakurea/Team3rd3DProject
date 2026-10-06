/// <summary>이동과 카메라가 필요한 조준 상태만 받는 계약. Unity 타입을 사용하지 않습니다.</summary>
public interface ICatAimState
{
    bool IsAiming { get; }
    bool WantsCameraFacing { get; }
}

/// <summary>조준 범위 표시는 장비 내부 대신 이 값만 읽습니다. 각도 단위는 도입니다.</summary>
public interface ICatShotSpreadState
{
    bool IsWeaponEquipped { get; }
    float CurrentSpreadDegrees { get; }
}

/// <summary>단발 입력의 발사 간격과 조준 중 걷기 규칙. 실제 입력·총구·충돌을 다루지 않습니다.</summary>
public sealed class CatCombatPolicy
{
    bool hasFired;
    float nextFireTime;

    /// <summary>새 누름만 받습니다. 버튼 유지 상태를 전달해 자동 발사로 바꾸지 않습니다.</summary>
    public bool TryRequest(bool enabled, bool equipped, bool firePressed, float now, float interval)
    {
        if (!enabled || !equipped || !firePressed || !IsFinite(now) || !IsFinite(interval)) return false;
        if (hasFired && now < nextFireTime) return false;

        nextFireTime = now + (interval > 0f ? interval : 0f);
        hasFired = true;
        return true;
    }

    /// <summary>조준 중 Shift 입력만 제거하므로 멈추면 Idle, 움직이면 Walk가 됩니다.</summary>
    public static CatMovementIntent RestrictMovement(CatMovementIntent intent, bool aiming)
        => aiming ? new CatMovementIntent(intent.Horizontal, intent.Forward, false) : intent;

    public void Reset()
    {
        hasFired = false;
        nextFireTime = 0f;
    }

    static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
}
