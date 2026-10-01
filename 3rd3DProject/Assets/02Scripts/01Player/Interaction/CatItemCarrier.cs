using System;
using UnityEngine;

// 입력과 씬 구성에 의존하지 않고, 들기·안전한 이동·내려놓기만 담당합니다.
public sealed class CatItemCarrier : IDisposable
{
    const float Skin = 0.015f;
    const float HoldHeight = 0.75f;
    const float HoldDistance = 0.8f;
    const float DropDistance = 3f;
    readonly Transform actor;
    readonly Camera view;
    readonly RaycastHit[] hits = new RaycastHit[64];
    readonly Collider[] overlaps = new Collider[64];
    CatInteractionItem held;
    Rigidbody heldBody;
    Collider[] heldColliders;
    bool[] colliderWasEnabled;
    bool wasKinematic;
    bool usedGravity;
    bool wasAvailable;
    CollisionDetectionMode collisionMode;
    Vector3 linearVelocity;
    Vector3 angularVelocity;
    Vector3 lastSafePosition;
    Vector3 centerOffset;
    Vector3 halfExtents;
    Vector3 liftedPosition;
    Quaternion heldRotation;
    int pickupStage;
    bool disposed;

    public CatInteractionItem Held => held;

    public CatItemCarrier(Transform actor, Camera view)
    {
        this.actor = actor;
        this.view = view;
    }

    public bool TryGrab(CatInteractionItem item)
    {
        if (!held && heldColliders != null) Restore();
        if (disposed || !actor || held || !item || !item.isActiveAndEnabled ||
            !item.IsAvailable || item.kind != CatItemKind.CarryOnly) return false;
        item.RefreshVisual();
        if (!item.Shape || !item.Body) return false;

        held = item;
        heldBody = item.Body;
        heldRotation = item.transform.rotation;
        lastSafePosition = item.transform.position;
        liftedPosition = lastSafePosition + Vector3.up * 0.08f;
        pickupStage = 1;
        Vector3 scale = item.transform.lossyScale;
        halfExtents = Vector3.Scale(item.Shape.size * 0.5f,
            new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        centerOffset = item.transform.TransformPoint(item.Shape.center) - lastSafePosition;
        if (!ClearSpace(lastSafePosition, true))
        {
            ClearCache();
            return false;
        }

        heldColliders = item.GetComponentsInChildren<Collider>(true);
        colliderWasEnabled = new bool[heldColliders.Length];
        wasKinematic = heldBody.isKinematic;
        usedGravity = heldBody.useGravity;
        wasAvailable = item.IsAvailable;
        collisionMode = heldBody.collisionDetectionMode;
        linearVelocity = wasKinematic ? Vector3.zero : heldBody.linearVelocity;
        angularVelocity = wasKinematic ? Vector3.zero : heldBody.angularVelocity;
        if (!wasKinematic)
        {
            heldBody.linearVelocity = Vector3.zero;
            heldBody.angularVelocity = Vector3.zero;
        }
        heldBody.collisionDetectionMode = CollisionDetectionMode.Discrete;
        heldBody.isKinematic = true;
        heldBody.useGravity = false;
        for (int i = 0; i < heldColliders.Length; i++)
        {
            colliderWasEnabled[i] = heldColliders[i].enabled;
            heldColliders[i].enabled = false;
        }
        item.IsAvailable = false;
        return true;
    }

    public void Tick(float dt, float holdDistance = HoldDistance)
    {
        if (disposed) return;
        if (!held || !heldBody)
        {
            if (heldColliders != null) Restore();
            return;
        }
        if (!actor) return;

        Vector3 target = HoldPoint(holdDistance) - centerOffset;
        // 선반에서 바로 대각선으로 당기면 턱에 걸리므로 먼저 들고, 수평으로 꺼냅니다.
        if (pickupStage == 1) target = liftedPosition;
        else if (pickupStage == 2) target.y = Mathf.Max(target.y, liftedPosition.y);
        Vector3 desired = Vector3.Lerp(lastSafePosition, target, 1 - Mathf.Exp(-16 * Mathf.Max(0, dt)));
        Vector3 delta = desired - lastSafePosition;
        float distance = delta.magnitude;
        if (distance <= 0.0001f)
        {
            AdvancePickup(target);
            return;
        }
        if (!TrySweep(lastSafePosition, delta / distance, distance, out var hit, out bool blocked)) return;
        if (blocked) desired = lastSafePosition + delta / distance * Mathf.Max(0, hit.distance - Skin);
        if (!ClearSpace(desired, true)) return;
        MoveTo(desired);
        AdvancePickup(target);
    }

    public bool TryDrop()
    {
        if (disposed || !held || !heldBody || !actor) return false;
        Vector3 forward = Forward();
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        for (int candidate = 0; candidate < 5; candidate++)
        {
            Vector3 start = lastSafePosition;
            if (candidate > 0)
            {
                float sideways = candidate <= 2 ? (candidate == 1 ? 0.6f : -0.6f) : 0;
                float ahead = candidate == 3 ? 1.15f : candidate == 4 ? 0.45f : HoldDistance;
                start = actor.position + Vector3.up * HoldHeight + forward * ahead + right * sideways - centerOffset;
            }
            if (!ClearPath(lastSafePosition, start) || !ClearSpace(start, true)) continue;
            if (!TrySweep(start, Vector3.down, DropDistance, out var ground, out bool supported) ||
                !supported || ground.normal.y < 0.55f) continue;

            Vector3 landing = start + Vector3.down * Mathf.Max(0, ground.distance - Skin);
            // 복원한 충돌체가 플레이어를 밀지 않도록 최종 착지는 플레이어도 검사합니다.
            if (!ClearSpace(landing, false)) continue;
            MoveTo(landing);
            Restore();
            return true;
        }
        return false;
    }

    /// <summary>보유를 즉시 끝냅니다. 안전한 착지가 불가능하면 아이템이 직접 분리·물리 복구를 맡습니다.</summary>
    public void Release()
    {
        if (!held || !heldBody) return;
        if (TryDrop()) return;
        var releasedItem = held;
        var savedColliders = heldColliders;
        var savedFlags = colliderWasEnabled;
        bool savedAvailability = wasAvailable;
        // 물리 복구를 기다리더라도 손은 즉시 비우고 카메라 추적을 끝냅니다.
        ClearCache();
        releasedItem.BeginReleaseRecovery(savedColliders, savedFlags, savedAvailability);
    }

    Vector3 HoldPoint(float distance)
    {
        if (!view) return actor.position + Vector3.up * HoldHeight + Forward() * distance;
        var ray = view.ViewportPointToRay(new Vector3(.5f, .5f, 0));
        float actorDepth = Vector3.Dot(actor.position + Vector3.up * HoldHeight - ray.origin, ray.direction);
        return ray.GetPoint(Mathf.Max(.4f, actorDepth + Mathf.Max(.3f, distance)));
    }

    public void Dispose()
    {
        if (disposed) return;
        // 물리 복구 대기는 아이템이 소유하므로 플레이어가 비활성화되어도 유실되지 않습니다.
        Release();
        Restore();
        disposed = true;
    }

    Vector3 Forward()
    {
        Vector3 direction = view ? view.transform.forward : actor.forward;
        direction.y = 0;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = actor.forward;
            direction.y = 0;
        }
        return direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;
    }

    Vector3 QueryExtents => Vector3.Max(halfExtents - Vector3.one * (Skin * 0.25f), Vector3.one * 0.002f);

    bool IsBlocking(Collider other, bool ignoreActor)
    {
        if (!other || !held || other.isTrigger || other.transform == held.transform ||
            other.transform.IsChildOf(held.transform)) return false;
        if (ignoreActor && actor && (other.transform == actor || other.transform.IsChildOf(actor))) return false;
        return !Physics.GetIgnoreLayerCollision(held.gameObject.layer, other.gameObject.layer) &&
            !Physics.GetIgnoreCollision(held.Shape, other);
    }

    bool ClearSpace(Vector3 position, bool ignoreActor)
    {
        int count = Physics.OverlapBoxNonAlloc(position + centerOffset, QueryExtents, overlaps,
            heldRotation, ~0, QueryTriggerInteraction.Ignore);
        if (count == overlaps.Length) return false;
        for (int i = 0; i < count; i++)
            if (IsBlocking(overlaps[i], ignoreActor)) return false;
        return true;
    }

    // 버퍼가 가득 차면 누락된 벽이 있을 수 있으므로 이동을 허용하지 않습니다.
    bool TrySweep(Vector3 position, Vector3 direction, float distance, out RaycastHit nearest, out bool blocked)
    {
        nearest = default;
        blocked = false;
        int count = Physics.BoxCastNonAlloc(position + centerOffset, QueryExtents, direction, hits,
            heldRotation, distance, ~0, QueryTriggerInteraction.Ignore);
        if (count == hits.Length) return false;
        float best = float.PositiveInfinity;
        for (int i = 0; i < count; i++)
        {
            if (hits[i].distance >= best || !IsBlocking(hits[i].collider, true)) continue;
            nearest = hits[i];
            best = hits[i].distance;
            blocked = true;
        }
        return true;
    }

    bool ClearPath(Vector3 from, Vector3 to)
    {
        Vector3 delta = to - from;
        float distance = delta.magnitude;
        if (distance <= 0.0001f) return true;
        return TrySweep(from, delta / distance, distance, out _, out bool blocked) && !blocked;
    }

    void MoveTo(Vector3 position)
    {
        lastSafePosition = position;
        heldBody.position = position;
        heldBody.rotation = heldRotation;
    }

    void AdvancePickup(Vector3 target)
    {
        if (pickupStage == 0 || (lastSafePosition - target).sqrMagnitude > 0.0001f) return;
        pickupStage = pickupStage == 1 ? 2 : 0;
    }

    void Restore()
    {
        if (heldBody)
        {
            heldBody.position = lastSafePosition;
            heldBody.rotation = heldRotation;
            heldBody.isKinematic = wasKinematic;
            heldBody.useGravity = usedGravity;
            heldBody.collisionDetectionMode = collisionMode;
            if (!wasKinematic)
            {
                heldBody.linearVelocity = linearVelocity;
                heldBody.angularVelocity = angularVelocity;
            }
        }
        if (heldColliders != null)
            for (int i = 0; i < heldColliders.Length; i++)
                if (heldColliders[i]) heldColliders[i].enabled = colliderWasEnabled[i];
        if (held) held.IsAvailable = wasAvailable;
        ClearCache();
    }

    void ClearCache()
    {
        held = null;
        heldBody = null;
        heldColliders = null;
        colliderWasEnabled = null;
        pickupStage = 0;
    }
}
