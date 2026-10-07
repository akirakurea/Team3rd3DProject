using System;
using System.Collections.Generic;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>F 획득에 필요한 저장 계약. UI와 실제 저장 구현은 교체할 수 있습니다.</summary>
public interface IItemInventory
{
    bool HasSpace { get; }
    HotbarItemData GetSlot(int index);
    bool TryAdd(HotbarItemData item, out int index);
    bool TryRemove(int index, HotbarItemData expected);
}

public enum RegisteredTargetKind { Item, Enemy }

/// <summary>대상에 새 물리 컴포넌트를 붙이지 않고 플레이어에서 권한을 등록합니다.</summary>
[Serializable]
public sealed class InteractionRegistration : IHighlightSource
{
    public GameObject target;
    public RegisteredTargetKind kind;
    [Tooltip("기존 아이템 데이터가 있으면 연결합니다. 비워 두면 현재 실행 중에만 이름으로 데이터를 만듭니다.")]
    public HotbarItemData item;
    public string displayName;
    [NonSerialized] internal bool reserved;
    [NonSerialized] internal HotbarItemData runtimeItem;
    [NonSerialized] Renderer[] renderers;
    public Component HighlightOwner => target ? target.transform : null;
    public Renderer[] VisualRenderers => renderers ??= target ? target.GetComponentsInChildren<Renderer>(true) : Array.Empty<Renderer>();
    public int VisualRevision => 0;
    public bool IsHighlightAvailable => target && target.activeInHierarchy && !reserved &&
        (!target.TryGetComponent<CatInteractionItem>(out var legacy) || legacy.IsAvailable);

    internal HotbarItemData Data()
    {
        if (item) return item;
        if (!runtimeItem)
        {
            runtimeItem = ScriptableObject.CreateInstance<HotbarItemData>();
            runtimeItem.name = target.name + " (runtime item)";
            runtimeItem.itemId = target.name;
            runtimeItem.displayName = string.IsNullOrWhiteSpace(displayName) ? target.name : displayName;
            // 월드 인스턴스를 프리팹으로 잘못 저장하지 않습니다. 등록 원본을 재사용합니다.
        }
        return runtimeItem;
    }
}

/// <summary>기존 F 상호작용의 진입점. 중앙 대상 선택, 인벤토리 저장과 포획을 연결합니다.</summary>
[DefaultExecutionOrder(9990), DisallowMultipleComponent]
public class ItemInteractor : MonoBehaviour, IRegisteredInteraction
{
    [Header("연결")]
    public InteractionController interaction;
    public HotbarManager inventory;
    [Header("F 상호작용")]
    [SerializeField, Min(.1f)] private float pickupRange = 2.2f;
    // 이전 씬의 직렬화 값은 유지합니다. 설치 위치를 강제로 옮기는 데 사용하지 않습니다.
    [SerializeField, HideInInspector] private float placeDistance = 1f;
    [Tooltip("기존 ItemPickup을 붙이고 아이템 데이터를 연결한 물체도 등록 대상으로 사용합니다.")]
    public bool useItemPickupRegistration = true;
    public InteractionRegistration[] targets = Array.Empty<InteractionRegistration>();
    // 기존 연결 계약을 보존합니다. F 획득은 손을 점유하지 않습니다.
    public GameObject HeldObject => null;
    public bool ConsumesHandInput => pressedThisFrame;
    public IHighlightSource HoveredHighlight => Target;
    public InteractionRegistration Target { get; private set; }
    public string LastResult { get; private set; }
    public float PickupRange { get => pickupRange; set => pickupRange = Mathf.Max(.1f, value); }
    public event Action<GameObject> EnemyCaptured;

    sealed class HotbarStore : IItemInventory
    {
        readonly HotbarManager manager;
        public HotbarStore(HotbarManager manager) { this.manager = manager; }
        public bool HasSpace => manager && manager.HasSpace;
        public HotbarItemData GetSlot(int index) => manager ? manager.GetSlot(index) : null;
        public bool TryAdd(HotbarItemData item, out int index)
        { index = -1; return manager && manager.TryAdd(item, out index); }
        public bool TryRemove(int index, HotbarItemData expected) => manager && manager.TryRemove(index, expected);
    }

    readonly Dictionary<GameObject, InteractionRegistration> registrations = new Dictionary<GameObject, InteractionRegistration>();
    readonly HashSet<GameObject> captured = new HashSet<GameObject>();
    readonly InputActivationGate activation = new InputActivationGate();
    IItemInventory store;
    IItemInventory customStore;
    HotbarManager boundInventory;
    bool pressedThisFrame;
    bool executing;

    void OnEnable()
    {
        if (!interaction) interaction = GetComponent<InteractionController>();
        activation.Reset();
        RebuildRegistrations();
        EnsureConnections();
    }

    void EnsureConnections()
    {
        if (!interaction) return;
        if (customStore != null) { store = customStore; return; }
        if (!inventory) inventory = HotbarManager.Instance;
        if (boundInventory != inventory || store == null)
        {
            boundInventory = inventory;
            store = inventory ? new HotbarStore(inventory) : null;
        }
    }

    /// <summary>다른 인벤토리 저장 구현을 연결합니다. 획득 처리 중 교체는 거부합니다.</summary>
    public bool TrySetInventory(IItemInventory value)
    {
        if (executing) return false;
        customStore = value; store = null; EnsureConnections(); return true;
    }

    public void RebuildRegistrations()
    {
        registrations.Clear();
        foreach (var entry in targets)
            Register(entry);
    }

    /// <summary>런타임에 생성한 덫·물체·쥐도 명시적으로 등록할 수 있습니다.</summary>
    public bool Register(InteractionRegistration entry)
    {
        if (entry == null || !entry.target || entry.target.transform.IsChildOf(transform) ||
            transform.IsChildOf(entry.target.transform) || registrations.ContainsKey(entry.target)) return false;
        registrations.Add(entry.target, entry); return true;
    }

    public bool Unregister(GameObject target)
    {
        if (!target) return false;
        if (!registrations.TryGetValue(target, out var entry) || entry.reserved) return false;
        return registrations.Remove(target);
    }

    void Update()
    {
        bool enabled = Application.isFocused && Cursor.lockState == CursorLockMode.Locked && Time.timeScale > 0f;
        pressedThisFrame = activation.AllowsPress(enabled, Time.frameCount) && FPressed();
    }

    void LateUpdate()
    {
        EnsureConnections();
        RefreshTarget();
        if (pressedThisFrame) TryInteract();
    }

    public bool AcceptsTrigger(Collider collider)
    {
        if (!collider) return false;
        for (Transform node = collider.transform; node; node = node.parent)
        {
            if (registrations.TryGetValue(node.gameObject, out var entry))
                return entry.IsHighlightAvailable && !captured.Contains(entry.target);
            if (useItemPickupRegistration && node.TryGetComponent<ItemPickup>(out var pickup) && pickup.Item) return true;
        }
        return false;
    }

    public void RefreshTarget()
    {
        Target = null;
        if (!interaction || !interaction.TryGetInteractionHit(pickupRange, out var hit)) return;
        for (Transform node = hit.collider.transform; node; node = node.parent)
        {
            if (registrations.TryGetValue(node.gameObject, out var entry))
            {
                if (entry.IsHighlightAvailable && !captured.Contains(entry.target) && interaction.IsRegisteredTargetVisible(hit, entry.target.transform)) Target = entry;
                return;
            }
            // 기존 ItemPickup 데이터도 등록으로 취급하여 원래 ItemInteractor 사용처를 유지합니다.
            if (useItemPickupRegistration && node.TryGetComponent<ItemPickup>(out var pickup) && pickup.Item)
            {
                var discovered = new InteractionRegistration { target = node.gameObject, item = pickup.Item };
                if (Register(discovered) && interaction.IsRegisteredTargetVisible(hit, discovered.target.transform)) Target = discovered;
                return;
            }
        }
    }

    public bool TryInteract()
    {
        if (executing || !isActiveAndEnabled) return false;
        EnsureConnections(); RefreshTarget();
        var command = RegisteredInteractionPolicy.Decide(interaction && interaction.IsInteractionEnabled,
            interaction && interaction.IsPickupBusy, Target != null, Target != null && Target.kind == RegisteredTargetKind.Enemy,
            store != null, store != null && store.HasSpace);
        executing = true;
        try
        {
            switch (command)
            {
                case RegisteredCommand.Capture: return Capture(Target);
                case RegisteredCommand.Store: return Collect(Target);
                default: LastResult = Target != null ? "획득 불가: 저장 연결·빈칸 또는 현재 상태를 확인하세요." : "대상 없음"; return false;
            }
        }
        finally { executing = false; Target = null; }
    }

    bool Collect(InteractionRegistration entry)
    {
        if (entry == null || !entry.IsHighlightAvailable || store == null) return false;
        entry.reserved = true;
        var data = entry.Data();
        if (!store.TryAdd(data, out int index)) { entry.reserved = false; return false; }
        // 새 물건만 보관합니다. 빈손·현재 장비·좌클릭으로 든 물건은 변경하지 않습니다.
        entry.target.SetActive(false);
        LastResult = "인벤토리에 보관: " + data.displayName + " / 칸 " + (index + 1);
        return true;
    }

    bool Capture(InteractionRegistration entry)
    {
        if (entry == null || !entry.IsHighlightAvailable || !captured.Add(entry.target)) return false;
        entry.reserved = true;
        GameObject target = entry.target;
        // 기존 쥐의 보물 정리 경로만 재사용합니다. 기절 제한·점수·라운드 카운트는 호출하지 않습니다.
        var thief = target.GetComponentInChildren<ThiefController>();
        if (thief) thief.GetCaptured();
        if (target) target.SetActive(false);
        LastResult = "쥐 포획 (카운트 미구현)";
        EnemyCaptured?.Invoke(target);
        return true;
    }

    // 기존 외부 호출과 직렬화 연결을 보존합니다. F로 직접 든 물건은 없습니다.
    public bool TryDropHeld() => false;
    public bool TryStowHeld() => true;

    void OnDisable()
    {
        pressedThisFrame = false; activation.Reset(); Target = null;
    }

    void OnDestroy()
    {
        foreach (var entry in registrations.Values)
            if (entry.runtimeItem && !entry.reserved) Destroy(entry.runtimeItem);
    }

    static bool FPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }
}
