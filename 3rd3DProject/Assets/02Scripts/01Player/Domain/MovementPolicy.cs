/// <summary>장치에서 읽고 길이를 1 이하로 제한한 이동 의도. 엔진의 벡터 타입을 보관하지 않습니다.</summary>
public readonly struct MovementIntent
{
    public float Horizontal { get; }
    public float Forward { get; }
    public bool Sprint { get; }
    public bool IsMoving => Horizontal * Horizontal + Forward * Forward >= 0.01f;

    // 입력 길이 제한은 각 엔진의 기본 벡터 기능으로 처리한 뒤 전달합니다.
    public MovementIntent(float horizontal, float forward, bool sprint)
    {
        Horizontal = horizontal;
        Forward = forward;
        Sprint = sprint;
    }
}

/// <summary>인스펙터와 분리된 이동 설정. 값 형식이므로 매 물리 갱신에 전달해도 힙 할당이 없습니다.</summary>
public readonly struct MovementSettings
{
    public float WalkSpeed { get; }
    public float RunSpeed { get; }
    public float Acceleration { get; }
    public float TurnSpeed { get; }

    public MovementSettings(float walkSpeed, float runSpeed, float acceleration, float turnSpeed)
    {
        WalkSpeed = walkSpeed;
        RunSpeed = runSpeed;
        Acceleration = acceleration;
        TurnSpeed = turnSpeed;
    }
}

/// <summary>Animator의 상태 이름이나 해시와 무관한 이동 상태입니다.</summary>
public enum MovementState
{
    Idle,
    Walk,
    Run
}

/// <summary>이식 가능한 이동 규칙. 물리·카메라 방향·회전 보간은 각 엔진의 기본 기능에 맡깁니다.</summary>
public static class MovementPolicy
{
    public static float SelectSpeed(MovementIntent intent, MovementSettings settings)
        => intent.Sprint ? settings.RunSpeed : settings.WalkSpeed;

    public static MovementState SelectAnimation(MovementIntent intent)
        => !intent.IsMoving ? MovementState.Idle
            : intent.Sprint ? MovementState.Run : MovementState.Walk;
}
