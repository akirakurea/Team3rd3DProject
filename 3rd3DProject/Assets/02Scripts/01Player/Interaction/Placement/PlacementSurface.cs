using System.Collections.Generic;
using TexasJawTrap;
using UnityEngine;

public enum PlacementFailure { None, NoFloor, TooFar, Blocked, Uneven, Occupied }

public struct PlacementCandidate
{
    public bool HasSurface;
    public bool IsValid;
    public Vector2Int Cell;
    // 덫의 피벗이 아니라 설치 면적의 중심입니다. 바닥에서 2cm 올립니다.
    public Vector3 Position;
    public PlacementFailure Failure;
}

/// <summary>등록된 실제 바닥 위에서 격자 위치, 지지면과 설치 공간을 검사합니다.</summary>
public sealed class PlacementSurface
{
    const float SurfaceOffset = .02f;
    const float EdgeMargin = .03f;
    const float HeightTolerance = .08f;
    const float MinimumUp = .9659258f; // 최대 경사 15도
    const int QueryCapacity = 128;

    readonly Transform player;
    readonly Collider[] floors;
    readonly float cellSize;
    readonly Vector2 footprint;
    readonly float clearanceHeight;
    readonly float range;
    readonly RaycastHit[] rayHits = new RaycastHit[QueryCapacity];
    readonly Collider[] overlaps = new Collider[QueryCapacity];
    readonly List<ConsumableJawTrap> knownTraps = new List<ConsumableJawTrap>(32);
    readonly List<Renderer> trapRenderers = new List<Renderer>(64);

    public PlacementSurface(Transform player, Collider[] floors, float cellSize,
        Vector2 footprintHalfSize, float clearanceHeight = 1.4f, float range = 2.2f)
    {
        this.player = player;
        this.floors = floors != null ? (Collider[])floors.Clone() : System.Array.Empty<Collider>();
        this.cellSize = Mathf.Max(.1f, cellSize);
        footprint = new Vector2(Mathf.Max(.05f, Mathf.Abs(footprintHalfSize.x)),
            Mathf.Max(.05f, Mathf.Abs(footprintHalfSize.y)));
        this.clearanceHeight = Mathf.Max(.1f, clearanceHeight);
        this.range = Mathf.Max(.1f, range);

        // 생성 시 한 번만 찾습니다. 이후 생성한 덫은 RegisterTrap으로 알립니다.
        var existing = Object.FindObjectsByType<ConsumableJawTrap>(FindObjectsInactive.Exclude);
        for (int i = 0; i < existing.Length; i++) RegisterTrap(existing[i]);
    }

    /// <summary>감지 Trigger가 꺼진 사용 중 덫도 보이는 동안 설치 공간을 차지합니다.</summary>
    public void RegisterTrap(ConsumableJawTrap trap)
    {
        if (!trap || !trap.gameObject.activeInHierarchy) return;
        bool registered = false;
        for (int i = knownTraps.Count - 1; i >= 0; i--)
        {
            var previous = knownTraps[i];
            if (!previous || !previous.gameObject.activeInHierarchy) knownTraps.RemoveAt(i);
            else if (previous == trap) registered = true;
        }
        // 용량 증가는 새 덫 등록에만 생깁니다. 줍거나 사라진 덫은 목록에서 뺍니다.
        if (!registered) knownTraps.Add(trap);
    }

    public PlacementCandidate FromRay(Ray ray)
    {
        if (!Finite(ray.origin) || !Finite(ray.direction) || ray.direction.sqrMagnitude < .001f)
            return Failed(Vector3.zero, PlacementFailure.NoFloor);

        float cameraDistance = player ? Vector3.Distance(ray.origin, player.position) : 0f;
        float distance = Mathf.Max(8f, cameraDistance + range + clearanceHeight + 2f);
        int count = Physics.RaycastNonAlloc(ray.origin, ray.direction.normalized, rayHits,
            distance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        if (count == rayHits.Length) return Failed(ray.origin, PlacementFailure.Blocked);

        bool found = false;
        RaycastHit nearest = default;
        for (int i = 0; i < count; i++)
        {
            Collider collider = rayHits[i].collider;
            if (!collider || IsPlayer(collider)) continue;
            if (collider.isTrigger && !collider.GetComponentInParent<ConsumableJawTrap>()) continue;
            if (!found || rayHits[i].distance < nearest.distance)
            { nearest = rayHits[i]; found = true; }
        }
        if (!found) return Failed(ray.GetPoint(distance), PlacementFailure.NoFloor);

        PlacementCandidate candidate = AtPoint(nearest.point);
        if (!IsFloor(nearest.collider))
        {
            // 벽을 관통해 뒤의 바닥을 선택하지 않습니다. 아래 바닥은 빨간 예고에만 씁니다.
            candidate.IsValid = false;
            candidate.Failure = nearest.collider.GetComponentInParent<ConsumableJawTrap>()
                ? PlacementFailure.Occupied : PlacementFailure.Blocked;
        }
        return candidate;
    }

    public PlacementCandidate AtPoint(Vector3 point, bool checkReach = true)
    {
        if (!Finite(point)) return Failed(Vector3.zero, PlacementFailure.NoFloor);
        PlacementCandidate candidate = Failed(point, PlacementFailure.NoFloor);
        Vector3 center = candidate.Position;
        if (!SampleFloor(center, out RaycastHit middle, out PlacementFailure failure))
        { candidate.Failure = failure; return candidate; }

        candidate.HasSurface = true;
        center.y = middle.point.y;
        candidate.Position = center + Vector3.up * SurfaceOffset;
        if (failure != PlacementFailure.None)
        { candidate.Failure = failure; return candidate; }
        failure = InspectSupport(center, middle);
        if (failure != PlacementFailure.None)
        { candidate.Failure = failure; return candidate; }

        // 몸 중심에서 닿을 수 없는 반대편이나 멀리 떨어진 칸에는 설치하지 않습니다.
        if (checkReach)
        {
            if (!player)
            { candidate.Failure = PlacementFailure.TooFar; return candidate; }
            Vector3 origin = player.position + Vector3.up * .65f;
            Vector3 delta = candidate.Position + Vector3.up * .05f - origin;
            if (delta.sqrMagnitude > range * range)
            { candidate.Failure = PlacementFailure.TooFar; return candidate; }
            if (Obstructed(origin, delta))
            { candidate.Failure = PlacementFailure.Blocked; return candidate; }
        }

        failure = InspectSpace(candidate.Position);
        candidate.Failure = failure;
        candidate.IsValid = failure == PlacementFailure.None;
        return candidate;
    }

    PlacementCandidate Failed(Vector3 point, PlacementFailure failure)
    {
        var cell = new Vector2Int(Mathf.RoundToInt(point.x / cellSize), Mathf.RoundToInt(point.z / cellSize));
        return new PlacementCandidate
        {
            Cell = cell,
            Position = new Vector3(cell.x * cellSize, point.y, cell.y * cellSize),
            Failure = failure
        };
    }

    bool SampleFloor(Vector3 point, out RaycastHit floorHit, out PlacementFailure failure)
    {
        floorHit = default;
        failure = PlacementFailure.NoFloor;
        float lift = Mathf.Max(clearanceHeight, .5f);
        Ray ray = new Ray(point + Vector3.up * lift, Vector3.down);
        float distance = lift + Mathf.Max(range, 3f);
        bool found = false;
        for (int i = 0; i < floors.Length; i++)
        {
            Collider floor = floors[i];
            if (!floor || !floor.enabled || !floor.gameObject.activeInHierarchy || floor.isTrigger) continue;
            if (floor.Raycast(ray, out RaycastHit hit, distance) && (!found || hit.distance < floorHit.distance))
            { floorHit = hit; found = true; }
        }
        if (!found) return false;
        if (floorHit.normal.y < MinimumUp)
        { failure = PlacementFailure.Uneven; return true; }

        // 등록 바닥이 아래에 있어도 그 위의 매대나 지붕을 건너뛰지 않습니다.
        int count = Physics.RaycastNonAlloc(ray.origin, ray.direction, rayHits,
            floorHit.distance + .005f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (count == rayHits.Length)
        { failure = PlacementFailure.Blocked; return true; }
        for (int i = 0; i < count; i++)
        {
            Collider collider = rayHits[i].collider;
            if (!collider || IsPlayer(collider) || IsFloor(collider)) continue;
            if (rayHits[i].distance <= floorHit.distance + .005f)
            { failure = PlacementFailure.Blocked; return true; }
        }
        failure = PlacementFailure.None;
        return true;
    }

    PlacementFailure InspectSupport(Vector3 center, RaycastHit middle)
    {
        if (middle.normal.y < MinimumUp) return PlacementFailure.Uneven;
        // 중앙과 네 모서리 모두 같은 높이의 실제 바닥에 지지되어야 합니다.
        float low = middle.point.y, high = middle.point.y;
        for (int corner = 0; corner < 4; corner++)
        {
            Vector3 sample = center + new Vector3((corner & 1) == 0 ? -footprint.x : footprint.x,
                0f, (corner & 2) == 0 ? -footprint.y : footprint.y);
            if (!SampleFloor(sample, out RaycastHit hit, out PlacementFailure failure)) return failure;
            if (failure != PlacementFailure.None) return failure;
            low = Mathf.Min(low, hit.point.y);
            high = Mathf.Max(high, hit.point.y);
            if (high - low > HeightTolerance) return PlacementFailure.Uneven;
        }
        return PlacementFailure.None;
    }

    bool Obstructed(Vector3 origin, Vector3 delta)
    {
        float distance = delta.magnitude;
        if (distance < .001f) return false;
        int count = Physics.RaycastNonAlloc(origin, delta / distance, rayHits, distance,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (count == rayHits.Length) return true;
        for (int i = 0; i < count; i++)
            if (rayHits[i].collider && !IsPlayer(rayHits[i].collider)) return true;
        return false;
    }

    PlacementFailure InspectSpace(Vector3 position)
    {
        Vector3 half = new Vector3(footprint.x + EdgeMargin, clearanceHeight * .5f, footprint.y + EdgeMargin);
        Vector3 center = position + Vector3.up * half.y;
        int count = Physics.OverlapBoxNonAlloc(center, half, overlaps, Quaternion.identity,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        if (count == overlaps.Length) return PlacementFailure.Blocked;
        for (int i = 0; i < count; i++)
        {
            Collider collider = overlaps[i];
            if (!collider || IsPlayer(collider) || IsFloor(collider)) continue;
            var trap = collider.GetComponentInParent<ConsumableJawTrap>();
            if (trap) { RegisterTrap(trap); return PlacementFailure.Occupied; }
            if (!collider.isTrigger) return PlacementFailure.Blocked;
        }

        // 턱이 Trigger보다 넓으므로 인접 덫의 작은 감지 영역도 넉넉히 찾습니다.
        float padding = Mathf.Max(cellSize, footprint.magnitude * 2f);
        Vector3 search = half + new Vector3(padding, .25f, padding);
        count = Physics.OverlapBoxNonAlloc(center, search, overlaps, Quaternion.identity,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide);
        if (count == overlaps.Length) return PlacementFailure.Blocked;
        for (int i = 0; i < count; i++)
        {
            Collider collider = overlaps[i];
            if (!collider || IsPlayer(collider)) continue;
            var trap = collider.GetComponentInParent<ConsumableJawTrap>();
            if (trap) RegisterTrap(trap);
        }
        Bounds space = new Bounds(center, half * 2f);
        for (int i = knownTraps.Count - 1; i >= 0; i--)
        {
            var trap = knownTraps[i];
            if (!trap || !trap.gameObject.activeInHierarchy)
            { knownTraps.RemoveAt(i); continue; }
            trapRenderers.Clear();
            trap.GetComponentsInChildren(false, trapRenderers);
            for (int rendererIndex = 0; rendererIndex < trapRenderers.Count; rendererIndex++)
            {
                Renderer renderer = trapRenderers[rendererIndex];
                if (renderer && renderer.enabled && space.Intersects(renderer.bounds))
                    return PlacementFailure.Occupied;
            }
        }
        return PlacementFailure.None;
    }

    bool IsFloor(Collider collider)
    {
        for (int i = 0; i < floors.Length; i++) if (floors[i] == collider) return true;
        return false;
    }

    bool IsPlayer(Collider collider) => player && collider.transform.IsChildOf(player);
    static bool Finite(Vector3 value) => !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
        !float.IsNaN(value.y) && !float.IsInfinity(value.y) && !float.IsNaN(value.z) && !float.IsInfinity(value.z);
}
