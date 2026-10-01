using System;

/// <summary>엔진과 무관하게 이동 중 자유 회전과 정지 중 수평 회전 범위를 계산합니다.</summary>
public sealed class CatOrbitPolicy
{
    bool initialized;
    bool wasMoving;
    float stationaryCenter;

    public readonly struct Constraint
    {
        public float Value { get; }
        public float Minimum { get; }
        public float Maximum { get; }
        public float Center { get; }
        public bool Wrap { get; }

        public Constraint(float value, float minimum, float maximum, float center, bool wrap)
        {
            Value = value;
            Minimum = minimum;
            Maximum = maximum;
            Center = center;
            Wrap = wrap;
        }
    }

    public Constraint Evaluate(bool moving, float currentYaw, float stationaryHalfAngle)
    {
        if (moving)
        {
            initialized = true;
            wasMoving = true;
            return new Constraint(Normalize(currentYaw), -180f, 180f, 0f, true);
        }

        // 멈춘 순간의 카메라 방향을 보관합니다. 정지 중에는 기준을 갱신하지 않습니다.
        if (!initialized || wasMoving)
            stationaryCenter = Normalize(currentYaw);
        initialized = true;
        wasMoving = false;

        float halfAngle = Math.Max(0f, Math.Min(180f, stationaryHalfAngle));
        float offset = Normalize(currentYaw - stationaryCenter);
        float value = stationaryCenter + Math.Max(-halfAngle, Math.Min(halfAngle, offset));
        return new Constraint(value, stationaryCenter - halfAngle, stationaryCenter + halfAngle,
            stationaryCenter, false);
    }

    public void Reset()
    {
        initialized = false;
        wasMoving = false;
        stationaryCenter = 0f;
    }

    static float Normalize(float angle)
    {
        float value = (angle + 180f) % 360f;
        if (value < 0f) value += 360f;
        return value - 180f;
    }
}
