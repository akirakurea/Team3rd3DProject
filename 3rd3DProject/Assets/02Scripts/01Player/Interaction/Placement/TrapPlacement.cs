using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TexasJawTrap;

/// <summary>기존 핫바의 덫 한 개를 검증된 바닥 격자에 설치하는 연결부입니다.</summary>
[DefaultExecutionOrder(-100), DisallowMultipleComponent]
public sealed class TrapPlacement : MonoBehaviour, IPrimaryAction, IInteractionAction
{
    [Header("기존 연결")]
    public HotbarManager inventory;
    public HotbarItemData trapItem;
    public Camera view;
    public InteractionController interaction;
    [Header("설치 가능한 실제 바닥")]
    [Tooltip("바닥 Collider만 등록합니다. 매대·벽·기둥은 넣지 않습니다.")]
    public Collider[] floors;
    [Min(.1f)] public float cellSize = 1f;
    [Min(.1f)] public float reach = 2.2f;
    [Min(.1f)] public float holdSeconds = 1f;
    [Tooltip("덫 전체 외형을 포함한 반폭입니다. 감지 Trigger 크기와 다릅니다.")]
    public Vector2 footprintHalfSize = new Vector2(.46f, .27f);
    public Vector3 modelGroundOffset;
    [Min(.1f)] public float clearanceHeight = 1.4f;
    [Header("설치 피드백")]
    public Material previewMaterial;
    public UnityEngine.UI.Image progressFill;
    public TMPro.TMP_Text statusText;

    public PlacementCandidate Candidate { get; private set; }
    public float Progress => Mathf.Clamp01(hold.Elapsed / Mathf.Max(.01f, holdSeconds));
    public int PlacedCount { get; private set; }
    public GameObject LastPlaced { get; private set; }
    public bool ClaimsPrimaryInput => isActiveAndEnabled &&
        (hold.RequiresRelease || (HasSelectedTrap && (!interaction || !interaction.Held)));
    public bool HasSelectedTrap => inventory && trapItem && inventory.SelectedItem == trapItem && trapItem.placePrefab;
    public bool ClaimsInteractionInput => isActiveAndEnabled && placementInput.ClaimsInteractionInput;

    readonly PlacementHold hold = new PlacementHold();
    readonly PlacementInput placementInput = new PlacementInput();
    readonly InputActivationGate activation = new InputActivationGate();
    PlacedTrapLifetime placedTraps;
    PlacementSurface surface;
    PlacementFeedback feedback;
    HotbarItemData startedItem;
    int startedSlot = -1;

    // 덫의 수명은 설치 입력의 활성 여부와 별개로 라운드 종료까지 관리합니다.
    void OnEnable() { BindRound(); }
    void Start() { BindRound(); Initialize(); }
    void BindRound()
    {
        placedTraps ??= new PlacedTrapLifetime(CancelRoundPlacement);
        placedTraps.Bind(GameManager.Instance);
    }
    void CancelRoundPlacement()
    {
        ResetInput();
        LastPlaced = null;
        Candidate = default;
        feedback?.Show(false, default, modelGroundOffset, 0f);
    }
    void Initialize()
    {
        if (surface != null || !trapItem || !trapItem.placePrefab) return;
        surface = new PlacementSurface(transform, floors, cellSize, footprintHalfSize, clearanceHeight, reach);
        feedback = new PlacementFeedback(trapItem.placePrefab, previewMaterial, progressFill, statusText);
    }
    void Update()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;
        BindRound();
        // 결과 전 대기 동안 다른 조작은 유지하지만 새 덫 설치는 허용하지 않습니다.
        bool enabled = GameplayInput.IsEnabled && placedTraps.AllowsPlacement;
        bool acceptsPress = activation.AllowsPress(enabled, Time.frameCount);
        bool fPressed = acceptsPress && keyboard != null && keyboard.fKey.wasPressedThisFrame;
        bool canPlace = HasSelectedTrap && (!interaction || (!interaction.Held && !interaction.IsPickupBusy));
        bool hasTarget = enabled && fPressed && interaction && interaction.registeredInteraction &&
            interaction.registeredInteraction is IInteractionTargetQuery targets && targets.HasInteractionTarget();
        var input = placementInput.Read(enabled,
            acceptsPress && mouse != null && mouse.leftButton.wasPressedThisFrame, mouse != null && mouse.leftButton.isPressed,
            fPressed, keyboard != null && keyboard.fKey.isPressed,
            keyboard != null && keyboard.escapeKey.wasPressedThisFrame, canPlace, hasTarget);
        TickPlacement(enabled, input.Pressed, input.Held, input.Cancel, Time.deltaTime);
    }

    /// <summary>입력과 경과 시간을 받아 실제 카메라·바닥 판정으로 설치합니다.</summary>
    public void TickPlacement(bool enabled, bool pressed, bool held, bool cancel, float deltaTime)
    {
        BindRound();
        Initialize();
        bool available = enabled && placedTraps.AllowsPlacement && surface != null && view && HasSelectedTrap &&
            (!interaction || (!interaction.Held && !interaction.IsPickupBusy));
        if (cancel || !available)
        {
            hold.Cancel(held && (hold.RequiresRelease || HasSelectedTrap));
            startedItem = null; startedSlot = -1;
            feedback?.Show(false, default, modelGroundOffset, 0f);
            return;
        }
        Candidate = surface.FromRay(view.ViewportPointToRay(new Vector3(.5f, .5f, 0f)));
        if (pressed && !hold.RequiresRelease)
        { startedItem = inventory.SelectedItem; startedSlot = inventory.SelectedIndex; }
        bool sameItem = !hold.IsHolding || (inventory.SelectedItem == startedItem && inventory.SelectedIndex == startedSlot);
        bool complete = hold.Tick(true, pressed, held, Candidate.IsValid && sameItem,
            Candidate.Cell.x, Candidate.Cell.y, inventory.SelectedIndex, deltaTime, holdSeconds);
        if (complete) CompletePlacement();
        feedback?.Show(HasSelectedTrap, Candidate, modelGroundOffset, Progress);
    }

    void CompletePlacement()
    {
        if (!placedTraps.AllowsPlacement) return;
        // 1초 사이에 물건·적·슬롯이 바뀌었는지 완료 직전에 다시 검사합니다.
        var final = surface.FromRay(view.ViewportPointToRay(new Vector3(.5f, .5f, 0f)));
        if (!final.IsValid || final.Cell != Candidate.Cell || inventory.SelectedIndex != startedSlot ||
            inventory.GetSlot(startedSlot) != startedItem || startedItem != trapItem) return;
        var placed = Instantiate(trapItem.placePrefab, final.Position - modelGroundOffset, Quaternion.identity);
        if (!inventory.TryRemove(startedSlot, startedItem))
        { placed.SetActive(false); Destroy(placed); return; }
        placedTraps.Register(placed);
        surface.RegisterTrap(placed.GetComponent<ConsumableJawTrap>());
        LastPlaced = placed; PlacedCount++;
    }

    public PlacementCandidate CheckPoint(Vector3 point, bool checkReach = true)
    { Initialize(); return surface != null ? surface.AtPoint(point, checkReach) : default; }
    void ResetInput()
    {
        placementInput.Reset();
        activation.Reset();
        hold.Reset(); startedItem = null; startedSlot = -1;
    }
    void OnApplicationFocus(bool focused)
    {
        if (focused) return;
        ResetInput();
        feedback?.Show(false, default, modelGroundOffset, 0f);
    }
    void OnDestroy() { placedTraps?.Dispose(); }
    void OnDisable()
    {
        ResetInput();
        feedback?.Dispose(); feedback = null; surface = null;
    }
}

/// <summary>이 설치기가 만든 월드 인스턴스만 라운드 수명에 맞춰 정리합니다. 재고·원본 프리팹은 다루지 않습니다.</summary>
internal sealed class PlacedTrapLifetime : System.IDisposable
{
    readonly List<GameObject> instances = new List<GameObject>();
    readonly System.Action onCleared;
    GameManager game;

    // GameManager가 없는 독립 검사/씬에서는 기존 설치 기능을 유지합니다.
    public bool AllowsPlacement => !game || game.State == GameManager.GameState.Playing;

    public PlacedTrapLifetime(System.Action onCleared) { this.onCleared = onCleared; }

    public void Bind(GameManager current)
    {
        if (game == current) return;
        if (game) Clear();
        Unbind();
        game = current;
        if (!game) return;
        game.OnStateChanged += HandleStateChanged;
        game.OnRoundStarted += HandleRoundStarted;
        HandleStateChanged(game.State);
    }

    public void Register(GameObject instance)
    {
        // 인벤토리로 회수되거나 사용 후 소멸한 덫의 참조가 쌓이지 않게 합니다.
        for (int i = instances.Count - 1; i >= 0; i--)
            if (!instances[i]) instances.RemoveAt(i);
        if (instance) instances.Add(instance);
    }

    void HandleStateChanged(GameManager.GameState state)
    {
        if (state == GameManager.GameState.RoundClear || state == GameManager.GameState.RoundResult ||
            state == GameManager.GameState.Win || state == GameManager.GameState.Lose)
            Clear();
    }

    // 씬을 재로딩하지 않는 재시작도 이전 라운드의 설치물을 남기지 않습니다.
    void HandleRoundStarted(int round, int rats) { Clear(); }

    void Clear()
    {
        while (instances.Count > 0)
        {
            int last = instances.Count - 1;
            GameObject instance = instances[last];
            instances.RemoveAt(last);
            if (!instance || !Application.isPlaying || !instance.scene.IsValid()) continue;
            // 승패에서는 시간이 멈추므로 지연 시간을 주지 않고 표시·충돌부터 끕니다.
            instance.SetActive(false);
            UnityEngine.Object.Destroy(instance);
        }
        onCleared?.Invoke();
    }

    void Unbind()
    {
        if (game)
        {
            game.OnStateChanged -= HandleStateChanged;
            game.OnRoundStarted -= HandleRoundStarted;
        }
        game = null;
    }

    public void Dispose() { Unbind(); Clear(); }
}
