using System;
using UnityEngine;

// 입력과 씬 구성에 의존하지 않고, 들기·안전한 이동·내려놓기만 담당합니다.
public sealed class ItemCarrier : IDisposable
{
    const float Skin = 0.015f;
    const float HoldHeight = 0.75f;
    const float HoldDistance = 0.8f;
    readonly Transform actor;
    readonly Camera view;
    readonly RaycastHit[] hits = new RaycastHit[64];
    readonly Collider[] overlaps = new Collider[64];
    CatInteractionItem held;
    Rigidbody heldBody;
    Collider[] heldColliders;
    bool[] colliderWasEnabled;
    bool wasAvailable;
    CollisionDetectionMode collisionMode;
    Vector3 lastSafePosition;
    Vector3 centerOffset;
    Vector3 halfExtents;
    Vector3 liftedPosition;
    Quaternion heldRotation;
    // 선반에서 위로 든 뒤 수평으로 꺼내고, 이후 조준점을 따라갑니다.
    enum PickupStage { Following, Lifting, ClearingShelf }
    PickupStage pickupStage;
    bool disposed;

    public CatInteractionItem Held => held;

    public ItemCarrier(Transform actor, Camera view)
    {
        this.actor = actor;
        this.view = view;
    }

    public bool TryGrab(CatInteractionItem item)
    {
        if (!held && heldColliders != null) Restore();
        if (disposed || !actor || held || !item || !item.isActiveAndEnabled ||
            !item.IsAvailable || item.kind != ItemKind.CarryOnly) return false;
        item.RefreshVisual();
        if (!item.Shape || !item.Body) return false;

        held = item;
        heldBody = item.Body;
        heldRotation = item.transform.rotation;
        lastSafePosition = item.transform.position;
        liftedPosition = lastSafePosition + Vector3.up * 0.08f;
        pickupStage = PickupStage.Lifting;
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
        wasAvailable = item.IsAvailable;
        collisionMode = heldBody.collisionDetectionMode;
        if (!heldBody.isKinematic)
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
        if (pickupStage == PickupStage.Lifting) target = liftedPosition;
        else if (pickupStage == PickupStage.ClearingShelf) target.y = Mathf.Max(target.y, liftedPosition.y);
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
        if (disposed || !held || !heldBody) return false;
        Restore();
        return true;
    }

    /// <summary>현재 위치에서 손을 비우고 기존 Rigidbody의 중력으로 놓습니다.</summary>
    public void Release() => TryDrop();

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
        // 플레이어가 비활성화되어도 물건은 현재 위치에서 낙하합니다.
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

    void MoveTo(Vector3 position)
    {
        lastSafePosition = position;
        heldBody.position = position;
        heldBody.rotation = heldRotation;
    }

    void AdvancePickup(Vector3 target)
    {
        if (pickupStage == PickupStage.Following || (lastSafePosition - target).sqrMagnitude > 0.0001f) return;
        pickupStage = pickupStage == PickupStage.Lifting ? PickupStage.ClearingShelf : PickupStage.Following;
    }

    void Restore()
    {
        var released = held;
        var body = heldBody;
        var colliders = heldColliders;
        var enabled = colliderWasEnabled;
        bool available = wasAvailable;
        ClearCache();
        if (colliders != null)
            for (int i = 0; i < colliders.Length; i++)
                if (colliders[i]) colliders[i].enabled = enabled[i];
        if (body)
        {
            // 위치·회전은 그대로 두며, 집기 전 속도를 다시 적용하지 않습니다.
            body.isKinematic = false;
            body.useGravity = true;
            body.collisionDetectionMode = collisionMode;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            body.WakeUp();
        }
        if (released) released.IsAvailable = available;
    }

    void ClearCache()
    {
        held = null;
        heldBody = null;
        heldColliders = null;
        colliderWasEnabled = null;
        pickupStage = PickupStage.Following;
    }
}


