using UnityEngine;

public interface IStepSolver
{
    bool TryGetVerticalSpeed(Vector3 planarVelocity, Vector3 inputDirection, float deltaTime, out float verticalSpeed);
    // 진행 중인 상승을 취소한 경우 true입니다. 호출자가 남은 상승 속도를 제거합니다.
    bool Reset();
}

// 물리 월드의 턱을 검사하기만 합니다. 실제 Rigidbody 쓰기는 Locomotion에서만 합니다.
public sealed class StepSolver : IStepSolver
{
    const float Skin = 0.015f;
    const float GroundTolerance = 0.065f;
    readonly Rigidbody body;
    readonly CapsuleCollider capsule;
    readonly StepSettings settings;
    readonly RaycastHit[] hits = new RaycastHit[24];
    readonly Collider[] overlaps = new Collider[24];
    float targetFootHeight;
    float remainingTime;
    Vector3 stepDirection;
    bool stepping;

    public StepSolver(Rigidbody body, CapsuleCollider capsule, StepSettings settings)
    {
        this.body = body;
        this.capsule = capsule;
        this.settings = settings;
    }

    public bool TryGetVerticalSpeed(Vector3 planarVelocity, Vector3 inputDirection, float dt, out float verticalSpeed)
    {
        verticalSpeed = 0;
        if (settings == null || settings.maxHeight <= 0 || dt <= 0 || capsule.direction != 1 ||
            inputDirection.sqrMagnitude < 0.01f || planarVelocity.sqrMagnitude < 0.01f)
        {
            bool wasStepping = stepping;
            stepping = false;
            verticalSpeed = Mathf.Min(body.linearVelocity.y, 0);
            return wasStepping;
        }

        // 화면 보간으로 늦어진 Transform 대신 물리 위치로 검사합니다.
        Vector3 scale = capsule.transform.lossyScale;
        float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(capsule.height * Mathf.Abs(scale.y), radius * 2);
        Vector3 center = body.position + body.rotation * Vector3.Scale(capsule.center, scale);
        Vector3 foot = center - Vector3.up * (height * 0.5f);
        Vector3 bottom = foot + Vector3.up * radius;
        Vector3 top = foot + Vector3.up * (height - radius);
        Vector3 direction = inputDirection.normalized;
        float queryRadius = Mathf.Max(0.01f, radius - Skin);
        float minNormalY = Mathf.Cos(settings.maxLandingSlope * Mathf.Deg2Rad);

        if (stepping)
        {
            remainingTime -= dt;
            float rise = targetFootHeight - foot.y;
            if (remainingTime <= 0 || Vector3.Dot(direction, stepDirection) < 0.5f ||
                !ClearSweep(bottom, top, queryRadius, Vector3.up, Mathf.Max(0, rise)))
            {
                stepping = false;
                verticalSpeed = Mathf.Min(body.linearVelocity.y, 0);
                return true;
            }
            if (rise <= 0.005f &&
                Ray(foot + Vector3.up * GroundTolerance, Vector3.down, GroundTolerance * 2, out var support) &&
                support.normal.y >= minNormalY && support.point.y >= targetFootHeight - Skin * 2)
            {
                stepping = false;
                return true; // 완료 시 남은 상승 속도를 제거해 튀어 오르지 않게 합니다.
            }
            // 중심이 턱 위에 도달할 때까지 높이를 잠깐 유지해 모서리로 다시 내려앉지 않게 합니다.
            verticalSpeed = RiseVelocity(Mathf.Max(0, rise), dt);
            return true;
        }

        if (body.linearVelocity.y > 0.3f || body.linearVelocity.y < -1f) return false;
        if (!Ray(foot + Vector3.up * GroundTolerance, Vector3.down, GroundTolerance * 2, out var ground) ||
            ground.normal.y < minNormalY || Mathf.Abs(ground.point.y - foot.y) > GroundTolerance) return false;

        float reach = radius + settings.forwardReach + planarVelocity.magnitude * dt;
        // 허용 높이 안의 전방 장애물이 있을 때만 무거운 공간 검사를 진행합니다.
        if (!Ray(foot + Vector3.up * (Skin * 2), direction, reach, out var obstacle) ||
            obstacle.normal.y >= minNormalY) return false;
        Vector3 landingProbe = obstacle.point + direction * Mathf.Max(Skin * 3, radius * 0.5f);
        landingProbe.y = ground.point.y + settings.maxHeight + Skin;
        if (!Ray(landingProbe, Vector3.down, settings.maxHeight + Skin * 2, out var landing) ||
            landing.normal.y < minNormalY) return false;
        float stepHeight = landing.point.y - ground.point.y;
        float lift = landing.point.y + Skin - foot.y;
        if (stepHeight <= Skin || stepHeight > settings.maxHeight || lift <= 0) return false;

        Vector3 up = Vector3.up * lift;
        float advance = Vector3.Dot(landingProbe - foot, direction);
        if (!ClearSweep(bottom, top, queryRadius, Vector3.up, lift) ||
            !ClearSweep(bottom + up, top + up, queryRadius, direction, advance) ||
            !ClearSpace(bottom + up + direction * advance, top + up + direction * advance, queryRadius)) return false;

        targetFootHeight = foot.y + lift;
        remainingTime = lift / Mathf.Max(0.1f, settings.riseSpeed) + advance / Mathf.Max(0.5f, planarVelocity.magnitude) + 0.15f;
        stepDirection = direction;
        stepping = true;
        verticalSpeed = RiseVelocity(lift, dt);
        return true;
    }

    float RiseVelocity(float rise, float dt)
    {
        // 다음 물리 스텝의 중력을 상쇄하면서 높이 변화량을 제한합니다.
        float gravity = body.useGravity ? Mathf.Max(0, -Physics.gravity.y) * dt : 0;
        return Mathf.Min(settings.riseSpeed, rise / dt) + gravity;
    }

    public bool Reset()
    {
        bool wasStepping = stepping;
        stepping = false;
        remainingTime = 0;
        return wasStepping;
    }

    bool IsSolid(Collider other)
    {
        return other && other != capsule && other.attachedRigidbody != body &&
            !Physics.GetIgnoreLayerCollision(capsule.gameObject.layer, other.gameObject.layer) &&
            !Physics.GetIgnoreCollision(capsule, other);
    }

    bool Ray(Vector3 origin, Vector3 direction, float distance, out RaycastHit nearest)
    {
        nearest = default;
        int count = Physics.RaycastNonAlloc(origin, direction, hits, distance, settings.solidLayers, QueryTriggerInteraction.Ignore);
        if (count == hits.Length) return false;
        float best = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].distance < best && IsSolid(hits[i].collider))
            {
                nearest = hits[i];
                best = hits[i].distance;
            }
        }
        return best < float.PositiveInfinity;
    }

    bool ClearSweep(Vector3 bottom, Vector3 top, float radius, Vector3 direction, float distance)
    {
        if (distance <= 0) return true;
        int count = Physics.CapsuleCastNonAlloc(bottom, top, radius, direction, hits, distance, settings.solidLayers, QueryTriggerInteraction.Ignore);
        if (count == hits.Length) return false;
        for (int i = 0; i < count; i++) if (IsSolid(hits[i].collider)) return false;
        return true;
    }

    bool ClearSpace(Vector3 bottom, Vector3 top, float radius)
    {
        int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius, overlaps, settings.solidLayers, QueryTriggerInteraction.Ignore);
        if (count == overlaps.Length) return false;
        for (int i = 0; i < count; i++) if (IsSolid(overlaps[i])) return false;
        return true;
    }
}
