using System;

/// <summary>같은 칸·아이템을 1초 유지해야 설치합니다. Unity와 관계없는 시간·취소 규칙입니다.</summary>
public sealed class PlacementHold
{
    public float Elapsed { get; private set; }
    public bool IsHolding { get; private set; }
    public bool RequiresRelease { get; private set; }
    int x, z, slot;

    public bool Tick(bool enabled, bool pressed, bool held, bool valid,
        int cellX, int cellZ, int selectedSlot, float deltaTime, float duration)
    {
        if (!held) { Reset(); return false; }
        if (!enabled) { Cancel(true); return false; }
        if (pressed && !RequiresRelease)
        {
            RequiresRelease = true;
            if (!valid) return false;
            x = cellX; z = cellZ; slot = selectedSlot;
            Elapsed = 0f; IsHolding = true;
            return false;
        }
        if (!IsHolding) return false;
        if (!valid || x != cellX || z != cellZ || slot != selectedSlot)
        { Cancel(true); return false; }
        Elapsed = Math.Min(Math.Max(.01f, duration), Elapsed + Math.Max(0f, deltaTime));
        if (Elapsed < Math.Max(.01f, duration)) return false;
        IsHolding = false;
        return true;
    }
    public void Cancel(bool held) { Elapsed = 0f; IsHolding = false; RequiresRelease = held; }
    public void Reset() { Elapsed = 0f; IsHolding = false; RequiresRelease = false; }
}
