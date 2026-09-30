using UnityEngine;

/// <summary>순수 이동 규칙의 결과를 카메라 방향과 Unity 물리에 적용하는 서비스입니다.</summary>
public sealed class CatPlayerLocomotion
{
    readonly Rigidbody body;
    readonly Transform actor;
    readonly ICatStepSolver steps;

    public CatPlayerLocomotion(Rigidbody body, Transform actor, ICatStepSolver steps)
    {
        this.body = body;
        this.actor = actor;
        this.steps = steps;
    }

    public void Tick(CatPlayerInputFrame input, Transform view, float walkSpeed, float runSpeed,
        float acceleration, float turnSpeed, float deltaTime)
        => Apply(input.Intent, view, new CatMovementSettings(walkSpeed, runSpeed, acceleration, turnSpeed), deltaTime);

    public void Apply(CatMovementIntent intent, Transform view, CatMovementSettings settings, float deltaTime)
    {
        Vector3 direction = GetDirection(new Vector2(intent.Horizontal, intent.Forward), view);
        Vector3 target = direction * CatMovementPolicy.SelectSpeed(intent, settings);
        Vector3 velocity = body.linearVelocity;
        Vector3 horizontal = Vector3.MoveTowards(
            new Vector3(velocity.x, 0, velocity.z), target, settings.Acceleration * deltaTime);

        float vertical = velocity.y;
        if (steps.TryGetVerticalSpeed(horizontal, direction, deltaTime, out float stepSpeed))
            vertical = stepSpeed;
        body.linearVelocity = new Vector3(horizontal.x, vertical, horizontal.z);

        if (direction.sqrMagnitude > 0.01f)
            body.MoveRotation(Quaternion.RotateTowards(body.rotation,
                Quaternion.LookRotation(direction), settings.TurnSpeed * deltaTime));
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
