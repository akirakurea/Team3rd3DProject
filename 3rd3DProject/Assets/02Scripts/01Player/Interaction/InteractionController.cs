using UnityEngine;

/// <summary>등록 대상의 F 상호작용과 기존 손 조작이 공유하는 최소 상태입니다.</summary>
public interface IRegisteredInteraction
{
    GameObject HeldObject { get; }
    bool ConsumesHandInput { get; }
    IHighlightSource HoveredHighlight { get; }
    bool TryStowHeld();
    bool AcceptsTrigger(Collider collider);
}

/// <summary>화면 중심 조준 → 거리·가림 검사 → 모델 강조 및 아이템별 동작 연결.</summary>
[DefaultExecutionOrder(10000)]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "CatInteractionController")]
public sealed class InteractionController : MonoBehaviour
{
    [Header("연결")]
    public Camera view;
    public InventoryPickupEffect pickup;
    [Tooltip("등록 물체의 F 획득과 손 점유 상태를 공유하는 기존 ItemInteractor입니다.")]
    public MonoBehaviour registeredInteraction;
    [Tooltip("양손을 사용하는 장비. 장착 중에는 아이템을 집을 수 없습니다.")]
    public MonoBehaviour equipment;
    public Material highlightFill, highlightEdge, highlightHalo;
    [Header("상호작용")]
    [Min(.2f)] public float reach = 2.2f;
    public LayerMask solidLayers = ~0;
    public CatInteractionItem Target { get; private set; }
    public IHighlightSource HoveredHighlight { get; private set; }
    public CatInteractionItem Held => carrier?.Held;
    IRegisteredInteraction Registered => registeredInteraction && registeredInteraction.isActiveAndEnabled
        ? registeredInteraction as IRegisteredInteraction : null;
    public bool IsInteractionEnabled => isActiveAndEnabled && input.Enabled && Time.timeScale > 0f;
    public bool IsPickupBusy => Pickup != null && Pickup.IsBusy;
    public bool HasFreeHands
    {
        get
        {
            var pickupPort = Pickup;
            var weapon = Equipment;
            return Held == null && (Registered == null || !Registered.HeldObject) && (pickupPort == null || !pickupPort.IsBusy) &&
                (weapon == null || !weapon.IsEquipped);
        }
    }

    IEquipmentPort Equipment => equipment != null && equipment.isActiveAndEnabled
        ? equipment as IEquipmentPort : null;
    IPickupPort Pickup
    {
        get
        {
            // Unity 컴포넌트가 파괴되었으면 인스펙터에 연결된 기본 구현으로 돌아갑니다.
            if (pickupSource != null && (!(pickupSource is Object sourceObject) || sourceObject != null))
                return pickupSource;
            return pickup != null ? pickup : null;
        }
    }

    IPickupPort pickupSource;
    ItemCarrier carrier;
    ItemHighlight highlight;
    readonly RaycastHit[] hits = new RaycastHit[64];
    IInteractionInputSource inputSource = new InteractionInput();
    HandInput input;
    [Tooltip("물건 중심을 중앙 조준선에 두는 거리입니다. 플레이어보다 앞쪽으로 적용합니다.")]
    [Min(.3f)] public float holdDistance = .8f;
    bool queryOverflow;
    void OnEnable()
    {
        if (view == null) view = Camera.main;
        inputSource ??= new InteractionInput();
        carrier = new ItemCarrier(transform, view);
        highlight = new ItemHighlight(view, highlightFill, highlightEdge, highlightHalo);
        ResetInput();
    }

    public void SetInputSource(IInteractionInputSource source)
    {
        inputSource = source ?? new InteractionInput();
        ResetInput();
    }

    /// <summary>다른 수집 연출을 연결합니다. 수집 중 교체는 거부하며 null이면 인스펙터 연결로 돌아갑니다.</summary>
    public bool TrySetPickupSource(IPickupPort source)
    {
        if (Pickup != null && Pickup.IsBusy) return false;
        pickupSource = source;
        ClearTarget();
        return true;
    }

    void Update() => input = inputSource.Read();

    void LateUpdate()
    {
        carrier.Tick(Time.deltaTime, holdDistance);
        RefreshTarget();
        ApplyHandCommand();
        // 장착이 실패해도 버튼을 뗀 물건을 계속 들지는 않습니다. 포커스 상실도 동일합니다.
        if (Held != null && (!input.Enabled || !input.GrabHeld)) ReleaseHeld();
        highlight.SetTarget(Registered?.HoveredHighlight ?? HoveredHighlight);
        highlight.Draw();
    }

    void ApplyHandCommand()
    {
        // 동시 입력의 이중 실행을 막습니다. 등록 대상 F 동작을 먼저 처리합니다.
        if (Registered != null && Registered.ConsumesHandInput) return;
        var weapon = Equipment;
        var pickupPort = Pickup;
        var state = new HandState(Held != null, weapon != null && weapon.IsEquipped,
            pickupPort != null && pickupPort.IsBusy, weapon != null && weapon.IsReady);
        switch (HandPolicy.Decide(input, state))
        {
            case HandCommand.Pickup:
                if (Registered == null || !Registered.HeldObject) TryPickup();
                break;
            case HandCommand.Release:
                ReleaseHeld();
                break;
            case HandCommand.Equip:
            case HandCommand.Unequip:
                TryToggleEquipment();
                break;
        }
    }

    public void RefreshTarget()
    {
        Target = null;
        HoveredHighlight = null;
        if (!isActiveAndEnabled || view == null || !HasFreeHands || !input.Enabled) return;
        var ray = view.ViewportPointToRay(new Vector3(.5f, .5f, 0));
        float distance = Vector3.Distance(view.transform.position, transform.position) + reach + 1;
        if (!Nearest(ray.origin, ray.direction, distance, out var hit)) return;
        var source = FindHighlightSource(hit.collider);
        if (source == null || source.HighlightOwner == null || !source.IsHighlightAvailable) return;
        Vector3 origin = transform.position + Vector3.up * .65f;
        if (Vector3.Distance(origin, hit.point) > reach) return;
        Vector3 delta = hit.point - origin;
        bool obstructed = Nearest(origin, delta.normalized, delta.magnitude + .015f, out var obstruction);
        if (queryOverflow || (obstructed && !ReferenceEquals(FindHighlightSource(obstruction.collider), source))) return;
        HoveredHighlight = source;
        Target = source as CatInteractionItem;
    }
    /// <summary>손 점유와 무관한 중앙 포인터 검사. 실제 F 대상은 등록 목록으로 판별합니다.</summary>
    public bool TryGetInteractionHit(float range, out RaycastHit hit)
    {
        hit = default;
        if (!IsInteractionEnabled || !view || float.IsNaN(range) || float.IsInfinity(range) || range <= 0f) return false;
        var ray = view.ViewportPointToRay(new Vector3(.5f, .5f, 0));
        if (!Nearest(ray.origin, ray.direction, Vector3.Distance(view.transform.position, transform.position) + range + 1f, out hit, true)) return false;
        Vector3 origin = transform.position + Vector3.up * .65f;
        Vector3 delta = hit.point - origin;
        if (delta.sqrMagnitude > range * range) return false;
        return true;
    }

    public bool IsRegisteredTargetVisible(RaycastHit hit, Transform targetRoot)
    {
        if (!targetRoot || !hit.collider || !hit.collider.transform.IsChildOf(targetRoot)) return false;
        Vector3 origin = transform.position + Vector3.up * .65f;
        Vector3 delta = hit.point - origin;
        RaycastHit obstruction = default;
        bool blocked = delta.magnitude > .025f && Nearest(origin, delta.normalized, delta.magnitude, out obstruction);
        return !queryOverflow && (!blocked || obstruction.collider.transform.IsChildOf(targetRoot));
    }

    static IHighlightSource FindHighlightSource(Collider collider)
    {
        // 기존 아이템의 예약·비활성 상태가 별도 강조 컴포넌트로 우회되지 않게 합니다.
        var item = collider.GetComponentInParent<CatInteractionItem>();
        if (item != null) return item;
        return collider.GetComponentInParent<IHighlightSource>();
    }
    bool Nearest(Vector3 origin, Vector3 direction, float distance, out RaycastHit result, bool registeredTriggers = false)
    {
        result = default; float closest = float.PositiveInfinity;
        int count = Physics.RaycastNonAlloc(origin, direction, hits, distance, solidLayers, registeredTriggers ? QueryTriggerInteraction.Collide : QueryTriggerInteraction.Ignore);
        // 버퍼가 넘치면 벽을 건너뛰는 대신 이번 프레임 선택을 거부합니다.
        queryOverflow = count == hits.Length;
        if (queryOverflow) return false;
        for (int i = 0; i < count; i++)
        {
            var h = hits[i];
            // 덫의 기존 Trigger를 변경하지 않습니다. 등록된 대상만 F 선택 후보로 허용합니다.
            if (h.collider.isTrigger && (!registeredTriggers || Registered == null || !Registered.AcceptsTrigger(h.collider))) continue;
            if (h.collider.transform.IsChildOf(transform) || (Held != null && h.collider.transform.IsChildOf(Held.transform))) continue;
            if (Registered != null && Registered.HeldObject && h.collider.transform.IsChildOf(Registered.HeldObject.transform)) continue;
            if (h.distance < closest) { closest = h.distance; result = h; }
        }
        return closest < float.PositiveInfinity;
    }
    /// <summary>빈손일 때만 집습니다. 누름 유지와 해제는 손 사용 정책에서 판단합니다.</summary>
    public bool TryPickup()
    {
        if (!isActiveAndEnabled || !input.Enabled || !HasFreeHands) return false;
        if (Target == null || !Target.IsHighlightAvailable) return false;
        var pickupPort = Pickup;
        bool success = Target.kind == ItemKind.CarryOnly
            ? carrier.TryGrab(Target)
            : pickupPort != null && pickupPort.TryCollect(Target, view);
        if (success) ClearTarget();
        return success;
    }
    /// <summary>키 1 토글의 공용 진입점. 다른 장비 구현도 작은 포트 계약으로 연결합니다.</summary>
    public bool TryToggleEquipment()
    {
        if (!isActiveAndEnabled || !input.Enabled) return false;
        var weapon = Equipment;
        if (weapon == null) return false;
        if (weapon.IsEquipped)
        {
            weapon.Unequip();
            return true;
        }
        var pickupPort = Pickup;
        if (!weapon.IsReady || (pickupPort != null && pickupPort.IsBusy)) return false;
        if (Registered != null && Registered.HeldObject && !Registered.TryStowHeld()) return false;
        if (Held != null && !carrier.TryDrop()) return false;
        ClearTarget();
        return weapon.TryEquip();
    }
    public void ReleaseHeld()
    {
        carrier?.Release();
        ClearTarget();
    }

    // 집기·놓기·장착 후 선택과 강조가 함께 해제되도록 한곳에서 처리합니다.
    void ClearTarget()
    {
        Target = null;
        HoveredHighlight = null;
        highlight?.SetTarget(null);
    }

    void ResetInput()
    {
        input = default;
        if (inputSource is IResettableInputSource resettableInput) resettableInput.Reset();
        ClearTarget();
    }

    void OnApplicationFocus(bool focused)
    {
        if (focused) return;
        ResetInput();
        ReleaseHeld();
    }

    void OnDisable()
    {
        ResetInput();
        Equipment?.Unequip();
        carrier?.Dispose();
        carrier = null;
        highlight?.Dispose();
        highlight = null;
    }
}
