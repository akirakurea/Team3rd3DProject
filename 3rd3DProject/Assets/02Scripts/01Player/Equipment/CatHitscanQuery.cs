using UnityEngine;

public enum CatShotHitKind { Miss, Hit, Blocked, QueryOverflow }

/// <summary>한 발의 결과입니다. Origin은 항상 실제 총구, Direction은 퍼짐까지 반영한 발사 방향입니다.</summary>
public readonly struct CatShotHit
{
    public readonly CatShotHitKind Kind;
    public readonly Collider Collider;
    public readonly Vector3 Origin, Point, Normal, Direction;
    public readonly float Distance;

    public CatShotHit(CatShotHitKind kind, Collider collider, Vector3 origin, Vector3 point,
        Vector3 normal, Vector3 direction, float distance)
    {
        Kind = kind;
        Collider = collider;
        Origin = origin;
        Point = point;
        Normal = normal;
        Direction = direction;
        Distance = distance;
    }
}

/// <summary>
/// 조준점과 총구의 충돌 검사만 담당합니다. 입력·피해량·연출·게임오브젝트 생성은 담당하지 않습니다.
/// 인스턴스마다 검사 버퍼를 재사용하며, 몸과 총을 모두 포함하는 플레이어 루트를 owner로 전달합니다.
/// 호출자는 손과 총구의 최종 자세를 적용한 뒤 Cast를 호출합니다.
/// </summary>
public sealed class CatHitscanQuery
{
    const int BufferCapacity = 64;
    const float MinimumRadius = 0.001f;
    const float MinimumDirectionSquared = 0.00000001f;

    readonly Transform owner;
    readonly RaycastHit[] hits = new RaycastHit[BufferCapacity];
    readonly Collider[] overlaps = new Collider[BufferCapacity];

    public CatHitscanQuery(Transform owner) => this.owner = owner;

    /// <summary>
    /// 화면 중앙에서 가장 가까운 외부 충돌점을 구합니다. 아무것도 없으면 range 끝점입니다.
    /// true는 계산 성공이며 명중 여부가 아닙니다. 입력 이상·버퍼 포화에서는 false입니다.
    /// </summary>
    public bool TryGetAimPoint(Camera view, float range, int mask, out Vector3 point)
    {
        point = Vector3.zero;
        if (owner == null || view == null || !ValidRange(range)) return false;
        Ray ray = view.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (!Finite(ray.origin) || !TryDirection(ray.direction, out Vector3 direction)) return false;
        Vector3 end = ray.origin + direction * range;
        if (!Finite(end)) return false;

        int count = Physics.RaycastNonAlloc(ray.origin, direction, hits, range, mask,
            QueryTriggerInteraction.Ignore);
        // NonAlloc 결과는 정렬되어 있지 않고 포화 시 최근접 충돌이 빠질 수도 있습니다.
        if (count >= hits.Length) return false;
        point = TryNearest(count, out RaycastHit nearest) ? nearest.point : end;
        return true;
    }

    /// <summary>
    /// 총구의 앞(+Z)으로 발사합니다. spreadDegrees.x는 좌우, y는 상하 각도이며 난수는 호출자가 정합니다.
    /// guardOrigin은 캐릭터 가슴 안쪽입니다. 가슴과 총구 사이 벽 및 총구의 벽 겹침도 차단합니다.
    /// Blocked는 발사 경로 확보 실패, QueryOverflow는 검사 버퍼 포화로 인한 안전 차단입니다.
    /// </summary>
    public CatShotHit Cast(Vector3 guardOrigin, Transform muzzle, float range, int mask,
        Vector2 spreadDegrees, float muzzleRadius = 0.025f)
    {
        Vector3 origin = muzzle != null && Finite(muzzle.position) ? muzzle.position : Vector3.zero;
        Vector3 direction = Vector3.zero;
        if (owner == null || muzzle == null || !Finite(muzzle.position) || !Finite(guardOrigin) ||
            !ValidRange(range) || !Finite(muzzleRadius) || muzzleRadius < 0f ||
            !Finite(spreadDegrees.x) || !Finite(spreadDegrees.y) ||
            Mathf.Abs(spreadDegrees.x) >= 90f || Mathf.Abs(spreadDegrees.y) >= 90f)
            return Stopped(CatShotHitKind.Blocked, origin, direction);

        Vector3 spreadForward = muzzle.rotation *
            (Quaternion.Euler(-spreadDegrees.y, spreadDegrees.x, 0f) * Vector3.forward);
        if (!TryDirection(spreadForward, out direction))
            return Stopped(CatShotHitKind.Blocked, origin, Vector3.zero);

        Vector3 end = origin + direction * range;
        Vector3 guardDelta = origin - guardOrigin;
        float guardDistance = guardDelta.magnitude;
        if (!Finite(end) || !Finite(guardDistance))
            return Stopped(CatShotHitKind.Blocked, origin, direction);

        // Raycast/SphereCast는 시작점이 충돌체 내부일 때 놓칠 수 있으므로 양 끝의 겹침을 별도로 검사합니다.
        float radius = Mathf.Max(MinimumRadius, muzzleRadius);
        if (TryOverlap(guardOrigin, radius, mask, origin, direction, out CatShotHit obstruction))
            return obstruction;

        if (guardDistance > MinimumRadius)
        {
            int count = Physics.SphereCastNonAlloc(guardOrigin, radius, guardDelta / guardDistance,
                hits, guardDistance, mask, QueryTriggerInteraction.Ignore);
            if (count >= hits.Length) return Stopped(CatShotHitKind.QueryOverflow, origin, direction);
            if (TryNearest(count, out RaycastHit guardHit))
                return Result(CatShotHitKind.Blocked, guardHit.collider, origin, guardHit.point,
                    guardHit.normal, direction);
        }

        if (TryOverlap(origin, radius, mask, origin, direction, out obstruction)) return obstruction;

        int shotCount = Physics.RaycastNonAlloc(origin, direction, hits, range, mask,
            QueryTriggerInteraction.Ignore);
        if (shotCount >= hits.Length) return Stopped(CatShotHitKind.QueryOverflow, origin, direction);
        if (TryNearest(shotCount, out RaycastHit shotHit))
            return Result(CatShotHitKind.Hit, shotHit.collider, origin, shotHit.point,
                shotHit.normal, direction);
        return new CatShotHit(CatShotHitKind.Miss, null, origin, end, Vector3.zero, direction, range);
    }

    bool TryOverlap(Vector3 center, float radius, int mask, Vector3 origin, Vector3 direction,
        out CatShotHit obstruction)
    {
        int count = Physics.OverlapSphereNonAlloc(center, radius, overlaps, mask,
            QueryTriggerInteraction.Ignore);
        if (count >= overlaps.Length)
        {
            obstruction = Stopped(CatShotHitKind.QueryOverflow, origin, direction);
            return true;
        }

        Collider nearest = null;
        Vector3 nearestPoint = center;
        float nearestSquared = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            Collider candidate = overlaps[i];
            if (IsSelfOrMissing(candidate)) continue;
            Vector3 point = candidate.ClosestPoint(center);
            float squared = (point - center).sqrMagnitude;
            if (!Finite(point) || !Finite(squared))
            {
                obstruction = Stopped(CatShotHitKind.Blocked, origin, direction);
                return true;
            }
            if (squared >= nearestSquared) continue;
            nearest = candidate;
            nearestPoint = point;
            nearestSquared = squared;
        }
        if (nearest == null)
        {
            obstruction = default;
            return false;
        }

        // 충돌체 안에서는 ClosestPoint가 중심점을 반환하므로 법선을 발사 반대 방향으로 보완합니다.
        Vector3 normal = TryDirection(center - nearestPoint, out Vector3 surfaceNormal)
            ? surfaceNormal : -direction;
        obstruction = Result(CatShotHitKind.Blocked, nearest, origin, nearestPoint, normal, direction);
        return true;
    }

    bool TryNearest(int count, out RaycastHit nearest)
    {
        nearest = default;
        float distance = float.PositiveInfinity;
        bool found = false;
        for (int i = 0; i < count; i++)
        {
            RaycastHit candidate = hits[i];
            if (IsSelfOrMissing(candidate.collider) || !Finite(candidate.distance) ||
                !Finite(candidate.point) || candidate.distance < 0f || candidate.distance >= distance) continue;
            nearest = candidate;
            distance = candidate.distance;
            found = true;
        }
        return found;
    }

    bool IsSelfOrMissing(Collider candidate) => candidate == null ||
        candidate.transform == owner || candidate.transform.IsChildOf(owner);

    static CatShotHit Result(CatShotHitKind kind, Collider collider, Vector3 origin, Vector3 point,
        Vector3 normal, Vector3 direction) => new CatShotHit(kind, collider, origin, point, normal,
            direction, Vector3.Distance(origin, point));

    // 잘못된 입력과 포화 시에는 먼 목표를 맞혔다고 판단하지 않도록 총구에서 종료합니다.
    static CatShotHit Stopped(CatShotHitKind kind, Vector3 origin, Vector3 direction) =>
        new CatShotHit(kind, null, origin, origin, Vector3.zero, direction, 0f);

    static bool TryDirection(Vector3 value, out Vector3 direction)
    {
        direction = Vector3.zero;
        float squared = value.sqrMagnitude;
        if (!Finite(value) || !Finite(squared) || squared < MinimumDirectionSquared) return false;
        direction = value / Mathf.Sqrt(squared);
        return true;
    }

    static bool ValidRange(float value) => Finite(value) && value > 0f;
    static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
}
