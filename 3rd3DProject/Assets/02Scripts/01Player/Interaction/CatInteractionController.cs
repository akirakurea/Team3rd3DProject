using UnityEngine;

/// <summary>화면 중심 조준 → 거리·가림 검사 → 모델 강조 및 아이템별 동작 연결.</summary>
[DefaultExecutionOrder(10000)]
public sealed class CatInteractionController : MonoBehaviour
{
    [Header("연결")]
    public Camera view;
    public CatInventoryPickupPresenter pickup;
    [Tooltip("양손을 사용하는 장비. 장착 중에는 아이템을 집을 수 없습니다.")]
    public MonoBehaviour equipment;
    ICatEquipmentPort Equipment => equipment != null && equipment.isActiveAndEnabled ? equipment as ICatEquipmentPort : null;
    public Material highlightFill, highlightEdge, highlightHalo;
    [Header("상호작용")]
    [Min(.2f)] public float reach = 2.2f;
    public LayerMask solidLayers = ~0;
    public CatInteractionItem Target { get; private set; }
    public ICatHighlightSource HoveredHighlight { get; private set; }
    public CatInteractionItem Held => carrier?.Held;
    public bool HasFreeHands => Held == null && (pickup == null || !pickup.IsBusy) &&
        (Equipment == null || !Equipment.IsEquipped);
    CatItemCarrier carrier;
    CatItemHighlight highlight;
    readonly RaycastHit[] hits = new RaycastHit[64];
    ICatInteractionInputSource inputSource = new CatInteractionInput();
    CatHandInput input;
    [Tooltip("물건 중심을 중앙 조준선에 두는 거리입니다. 플레이어보다 앞쪽으로 적용합니다.")]
    [Min(.3f)] public float holdDistance = .8f;
    bool queryOverflow;
    void OnEnable()
    {
        if (view == null) view = Camera.main;
        carrier = new CatItemCarrier(transform, view);
        highlight = new CatItemHighlight(view, highlightFill, highlightEdge, highlightHalo);
    }
    public void SetInputSource(ICatInteractionInputSource source) => inputSource = source ?? new CatInteractionInput();
    void Update() => input = inputSource.Read();
    void LateUpdate()
    {
        carrier.Tick(Time.deltaTime, holdDistance);
        RefreshTarget();
        var weapon = Equipment;
        var state = new CatHandState(Held != null, weapon != null && weapon.IsEquipped,
            pickup != null && pickup.IsBusy, weapon != null && weapon.IsReady);
        switch (CatHandPolicy.Decide(input, state))
        {
            case CatHandCommand.Pickup: TryPickup(); break;
            case CatHandCommand.Release: ReleaseHeld(); break;
            case CatHandCommand.Equip:
            case CatHandCommand.Unequip: TryToggleEquipment(); break;
        }
        // 장착이 실패해도 버튼을 뗀 물건을 계속 들지는 않습니다. 포커스 상실도 동일합니다.
        if (Held != null && (!input.Enabled || !input.GrabHeld)) ReleaseHeld();
        highlight.SetTarget(HoveredHighlight); highlight.Draw();
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
    static ICatHighlightSource FindHighlightSource(Collider collider)
    {
        // 기존 아이템의 예약·비활성 상태가 별도 강조 컴포넌트로 우회되지 않게 합니다.
        var item = collider.GetComponentInParent<CatInteractionItem>();
        if (item != null) return item;
        return collider.GetComponentInParent<CatHighlightTarget>();
    }
    bool Nearest(Vector3 origin, Vector3 direction, float distance, out RaycastHit result)
    {
        result = default; float closest = float.PositiveInfinity;
        int count = Physics.RaycastNonAlloc(origin, direction, hits, distance, solidLayers, QueryTriggerInteraction.Ignore);
        // 버퍼가 넘치면 벽을 건너뛰는 대신 이번 프레임 선택을 거부합니다.
        queryOverflow = count == hits.Length;
        if (queryOverflow) return false;
        for (int i = 0; i < count; i++)
        {
            var h = hits[i];
            if (h.collider.transform.IsChildOf(transform) || (Held != null && h.collider.transform.IsChildOf(Held.transform))) continue;
            if (h.distance < closest) { closest = h.distance; result = h; }
        }
        return closest < float.PositiveInfinity;
    }
    /// <summary>빈손일 때만 집습니다. 누름 유지와 해제는 손 사용 정책에서 판단합니다.</summary>
    public bool TryPickup()
    {
        if (!isActiveAndEnabled || !input.Enabled || !HasFreeHands) return false;
        if (Target == null || !Target.IsHighlightAvailable) return false;
        bool success = Target.kind == CatItemKind.CarryOnly ? carrier.TryGrab(Target) : pickup != null && pickup.TryCollect(Target, view);
        if (success) { Target = null; HoveredHighlight = null; highlight.SetTarget(null); }
        return success;
    }
    /// <summary>키 1 토글의 공용 진입점. 다른 장비 구현도 작은 포트 계약으로 연결합니다.</summary>
    public bool TryToggleEquipment()
    {
        if (!isActiveAndEnabled || !input.Enabled) return false;
        var weapon = Equipment;
        if (weapon == null) return false;
        if (weapon.IsEquipped) { weapon.Unequip(); return true; }
        if (!weapon.IsReady || (pickup != null && pickup.IsBusy)) return false;
        if (Held != null && !carrier.TryDrop()) return false;
        Target = null; HoveredHighlight = null; highlight?.SetTarget(null);
        return weapon.TryEquip();
    }
    public void ReleaseHeld()
    {
        carrier?.Release();
        Target = null; HoveredHighlight = null; highlight?.SetTarget(null);
    }
    void OnDisable()
    {
        input = default; Target = null; HoveredHighlight = null;
        Equipment?.Unequip();
        carrier?.Dispose(); carrier = null;
        highlight?.Dispose(); highlight = null;
    }
}
