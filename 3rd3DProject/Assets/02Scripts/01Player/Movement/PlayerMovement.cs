using UnityEngine;

/// <summary>순수 이동 규칙의 결과를 카메라 방향과 Unity 물리에 적용하는 서비스입니다.</summary>
public sealed class PlayerMovement
{
    readonly Rigidbody body;
    readonly Transform actor;
    readonly IStepSolver steps;

    public PlayerMovement(Rigidbody body, Transform actor, IStepSolver steps)
    {
        this.body = body;
        this.actor = actor;
        this.steps = steps;
    }

    public void Apply(MovementIntent intent, Transform view, MovementSettings settings, float deltaTime, bool faceView = false, JumpFrame jump = default, float launchSpeed = 0)
    {
        Vector3 direction = GetDirection(new Vector2(intent.Horizontal, intent.Forward), view);
        Vector3 target = direction * MovementPolicy.SelectSpeed(intent, settings);
        Vector3 velocity = body.linearVelocity;
        Vector3 horizontal = Vector3.MoveTowards(
            new Vector3(velocity.x, 0, velocity.z), target, settings.Acceleration * deltaTime);

        float vertical = velocity.y;
        if (jump.Active)
        {
            // 점프 중 턱 상승 목표를 없애고 Unity 중력이 만든 속도는 그대로 둡니다.
            steps.Reset();
            if (jump.TakeOff) vertical = launchSpeed;
        }
        else if (steps.TryGetVerticalSpeed(horizontal, direction, deltaTime, out float stepSpeed))
            vertical = stepSpeed;
        body.linearVelocity = new Vector3(horizontal.x, vertical, horizontal.z);

        Vector3 facing = faceView && view ? Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized : direction;
        if (facing.sqrMagnitude > 0.01f)
            body.MoveRotation(Quaternion.RotateTowards(body.rotation,
                Quaternion.LookRotation(facing), settings.TurnSpeed * deltaTime));
    }

    Vector3 GetDirection(Vector2 move, Transform view)
    {
        Vector3 forward = view ? Vector3.ProjectOnPlane(view.forward, Vector3.up).normalized : Vector3.forward;
        if (forward.sqrMagnitude < 0.01f) forward = actor.forward;
        return forward * move.y + Vector3.Cross(Vector3.up, forward) * move.x;
    }

    public void Reset()
    {
        if (!steps.Reset() || !body) return;
        Vector3 velocity = body.linearVelocity;
        body.linearVelocity = new Vector3(velocity.x, Mathf.Min(velocity.y, 0), velocity.z);
    }
}
