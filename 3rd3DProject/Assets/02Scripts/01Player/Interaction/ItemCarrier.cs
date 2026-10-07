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


/// <summary>등록된 월드 원본을 복제하거나 재부모화하지 않고 운반합니다.</summary>
public sealed class RegisteredItemCarrier : IDisposable
{
    const float Skin = .015f;
    const float HoldHeight = .75f;
    readonly Transform actor;
    readonly Camera view;
    readonly RaycastHit[] hits = new RaycastHit[64];
    readonly Collider[] overlaps = new Collider[64];
    GameObject held;
    Transform originalParent;
    Quaternion originalRotation;
    Vector3 centerOffset, halfExtents, lastSafePosition;
    BodyState[] bodies;
    Collider[] colliders;
    bool[] colliderEnabled;
    CatInteractionItem[] items;
    bool[] itemAvailable;
    bool disposed;

    struct BodyState
    {
        public Rigidbody Body;
        public bool Kinematic, Gravity;
        public CollisionDetectionMode CollisionMode;
        public Vector3 Velocity, AngularVelocity;
    }

    public GameObject Held => held;

    public RegisteredItemCarrier(Transform actor, Camera view)
    {
        this.actor = actor;
        this.view = view;
    }

    public bool TryBegin(GameObject root)
    {
        if (disposed || held || colliders != null || !actor || !root || !root.activeInHierarchy) return false;
        Transform target = root.transform;
        // 플레이어 본체나 플레이어가 포함된 부모를 실수로 운반하지 않습니다.
        if (target == actor || target.IsChildOf(actor) || actor.IsChildOf(target)) return false;

        var foundColliders = root.GetComponentsInChildren<Collider>(true);
        var foundBodies = root.GetComponentsInChildren<Rigidbody>(true);
        var foundItems = root.GetComponentsInChildren<CatInteractionItem>(true);
        var foundRenderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = default;
        bool hasBounds = false;
        foreach (var collider in foundColliders)
        {
            var attachedBody = collider.attachedRigidbody;
            if (attachedBody && attachedBody.transform != target && !attachedBody.transform.IsChildOf(target))
                return false; // 다른 등록 루트의 복합 충돌체 일부만 떼어 움직이지 않습니다.
            if (collider.enabled && collider.gameObject.activeInHierarchy && !collider.isTrigger)
                IncludeBounds(collider.bounds, ref bounds, ref hasBounds);
        }
        foreach (var renderer in foundRenderers)
            if (renderer.enabled && renderer.gameObject.activeInHierarchy)
                IncludeBounds(renderer.bounds, ref bounds, ref hasBounds);
        foreach (var item in foundItems)
            if (item.isActiveAndEnabled && !item.IsAvailable) return false;
        if (!hasBounds || !Finite(bounds.center) || !Finite(bounds.extents) || bounds.size.sqrMagnitude < .000001f)
            return false;

        // 성공 전 검사는 캐시에만 기록하며, 원본 Transform과 물리 상태를 건드리지 않습니다.
        held = root;
        originalParent = target.parent;
        originalRotation = target.rotation;
        lastSafePosition = target.position;
        centerOffset = bounds.center - lastSafePosition;
        halfExtents = bounds.extents;
        colliders = foundColliders;
        colliderEnabled = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++) colliderEnabled[i] = colliders[i].enabled;
        if (!ClearSpace(lastSafePosition, true))
        {
            ClearCache();
            return false;
        }
        bodies = new BodyState[foundBodies.Length];
        for (int i = 0; i < bodies.Length; i++)
        {
            var body = foundBodies[i];
            bodies[i] = new BodyState
            {
                Body = body, Kinematic = body.isKinematic, Gravity = body.useGravity,
                CollisionMode = body.collisionDetectionMode,
                Velocity = body.isKinematic ? Vector3.zero : body.linearVelocity,
                AngularVelocity = body.isKinematic ? Vector3.zero : body.angularVelocity
            };
        }
        items = foundItems;
        itemAvailable = new bool[items.Length];
        for (int i = 0; i < items.Length; i++) itemAvailable[i] = items[i].IsAvailable;
        try
        {
            foreach (var state in bodies)
            {
                var body = state.Body;
                if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                body.collisionDetectionMode = CollisionDetectionMode.Discrete;
                body.isKinematic = true;
                body.useGravity = false;
            }
            foreach (var collider in colliders) collider.enabled = false;
            foreach (var item in items) item.IsAvailable = false;
            return true;
        }
        catch
        {
            // 시작 도중 실패하면 속도까지 원래 상태로 돌려 실패를 원자적으로 처리합니다.
            Restore(false);
            return false;
        }
    }

    public void Tick(float dt, float holdDistance = .8f)
    {
        if (disposed) return;
        if (!held || !held.activeInHierarchy)
        {
            Restore(true);
            return;
        }
        if (!actor || !PoseUnchanged() || !Finite(dt) || dt <= 0 || !Finite(holdDistance)) return;
        if (!ClearSpace(lastSafePosition, true)) return;
        Vector3 target = HoldPoint(holdDistance) - centerOffset;
        Vector3 desired = Vector3.Lerp(lastSafePosition, target, 1 - Mathf.Exp(-16 * dt));
        Vector3 delta = desired - lastSafePosition;
        float distance = delta.magnitude;
        if (distance < .0001f) return;
        Vector3 direction = delta / distance;
        int count = Physics.BoxCastNonAlloc(lastSafePosition + centerOffset, QueryExtents,
            direction, hits, Quaternion.identity, distance, ~0, QueryTriggerInteraction.Ignore);
        if (count == hits.Length) return; // 결과가 잘렸다면 보이지 않은 벽을 통과하지 않습니다.
        float allowedDistance = distance;
        for (int i = 0; i < count; i++)
            if (IsBlocking(hits[i].collider, true))
                allowedDistance = Mathf.Min(allowedDistance, Mathf.Max(0, hits[i].distance - Skin));
        desired = lastSafePosition + direction * allowedDistance;
        if (!ClearSpace(desired, true)) return;
        MoveTo(desired);
    }

    /// <summary>현재 위치에서 기존 Rigidbody의 중력으로 놓습니다.</summary>
    public bool TryDrop() => TryDrop(null);

    /// <summary>슬롯 변경이 성공했을 때만 손을 비우고 현재 위치에서 놓습니다.</summary>
    public bool TryDrop(Func<bool> commit)
    {
        if (disposed || !held || !held.activeInHierarchy) return false;
        if (commit != null && !commit()) return false;
        Restore(true, true);
        return true;
    }

    /// <summary>월드 원본의 위치는 유지하고 비활성화하여 인벤토리에서 보관합니다.</summary>
    public void Store()
    {
        if (disposed) return;
        GameObject stored = held;
        Restore(true);
        if (stored) stored.SetActive(false);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        // 정리 시에도 원본 삭제·복제·원래 위치로의 순간 이동을 하지 않습니다.
        Restore(true);
    }

    static void IncludeBounds(Bounds candidate, ref Bounds bounds, ref bool hasBounds)
    {
        if (!hasBounds) { bounds = candidate; hasBounds = true; }
        else bounds.Encapsulate(candidate);
    }

    static bool Finite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    static bool Finite(Vector3 value) => Finite(value.x) && Finite(value.y) && Finite(value.z);
    Vector3 QueryExtents => Vector3.Max(halfExtents - Vector3.one * (Skin * .25f), Vector3.one * .002f);

    bool PoseUnchanged() => held.transform.parent == originalParent &&
        Quaternion.Angle(held.transform.rotation, originalRotation) < .01f;

    Vector3 HoldPoint(float distance)
    {
        float reach = Mathf.Max(.3f, distance);
        if (!view) return actor.position + Vector3.up * HoldHeight + actor.forward * reach;
        var ray = view.ViewportPointToRay(new Vector3(.5f, .5f));
        float actorDepth = Vector3.Dot(actor.position + Vector3.up * HoldHeight - ray.origin, ray.direction);
        return ray.GetPoint(Mathf.Max(.4f, actorDepth + reach));
    }

    bool IsBlocking(Collider other, bool ignoreActor)
    {
        if (!other || !held || other.isTrigger || other.transform == held.transform ||
            other.transform.IsChildOf(held.transform)) return false;
        if (ignoreActor && actor && (other.transform == actor || other.transform.IsChildOf(actor))) return false;
        bool hadSolidCollider = false;
        for (int i = 0; i < colliders.Length; i++)
        {
            var own = colliders[i];
            if (!own || !colliderEnabled[i] || own.isTrigger || !own.gameObject.activeInHierarchy) continue;
            hadSolidCollider = true;
            if (!Physics.GetIgnoreLayerCollision(own.gameObject.layer, other.gameObject.layer) &&
                !Physics.GetIgnoreCollision(own, other)) return true;
        }
        return !hadSolidCollider && !Physics.GetIgnoreLayerCollision(held.layer, other.gameObject.layer);
    }

    bool ClearSpace(Vector3 position, bool ignoreActor)
    {
        // 시작 때의 월드 경계와 회전을 유지합니다. 모델·Collider 모양은 수정하지 않습니다.
        int count = Physics.OverlapBoxNonAlloc(position + centerOffset, QueryExtents, overlaps,
            Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        if (count == overlaps.Length) return false;
        for (int i = 0; i < count; i++) if (IsBlocking(overlaps[i], ignoreActor)) return false;
        return true;
    }

    void MoveTo(Vector3 position)
    {
        held.transform.position = position;
        // 보간을 사용하는 하위 Rigidbody도 같은 원본 계층의 현재 위치에 동기화합니다.
        foreach (var state in bodies)
            if (state.Body) state.Body.position = state.Body.transform.position;
        lastSafePosition = position;
    }

    void Restore(bool clearVelocity, bool drop = false)
    {
        // 먼저 캐시를 분리해 반복 Dispose나 원본의 비활성화 콜백에도 한 번만 복원합니다.
        var savedBodies = bodies;
        var savedColliders = colliders;
        var savedColliderEnabled = colliderEnabled;
        var savedItems = items;
        var savedItemAvailable = itemAvailable;
        ClearCache();
        if (savedColliders != null)
            for (int i = 0; i < savedColliders.Length; i++)
                if (savedColliders[i]) savedColliders[i].enabled = savedColliderEnabled[i];
        if (savedBodies != null)
            foreach (var state in savedBodies)
            {
                var body = state.Body;
                if (!body) continue;
                body.isKinematic = !drop && state.Kinematic;
                body.useGravity = drop || state.Gravity;
                body.collisionDetectionMode = state.CollisionMode;
                if (!body.isKinematic)
                {
                    body.linearVelocity = drop || clearVelocity ? Vector3.zero : state.Velocity;
                    body.angularVelocity = drop || clearVelocity ? Vector3.zero : state.AngularVelocity;
                    if (drop) body.WakeUp();
                }
            }
        if (savedItems != null)
            for (int i = 0; i < savedItems.Length; i++)
                if (savedItems[i]) savedItems[i].IsAvailable = savedItemAvailable[i];
        if (savedColliders != null || savedBodies != null) Physics.SyncTransforms();
    }

    void ClearCache()
    {
        held = null;
        originalParent = null;
        bodies = null;
        colliders = null;
        colliderEnabled = null;
        items = null;
        itemAvailable = null;
    }
}
