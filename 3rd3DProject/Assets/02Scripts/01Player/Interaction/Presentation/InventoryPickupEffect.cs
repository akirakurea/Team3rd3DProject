using UnityEngine;
using UnityEngine.Events;

/// <summary>손 사용 담당이 수집 연출에 요구하는 최소 계약입니다. 인벤토리 저장 방식은 포함하지 않습니다.</summary>
public interface IPickupPort
{
    bool IsBusy { get; }
    bool TryCollect(CatInteractionItem item, Camera view);
}

[DisallowMultipleComponent]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "CatInventoryPickupPresenter")]
public sealed class InventoryPickupEffect : MonoBehaviour, IPickupPort
{
    [Header("인벤토리 UI 연결")]
    [Tooltip("실제 인벤토리 UI의 도착 지점. 연결되지 않았거나 비활성 상태이면 수집하지 않습니다.")]
    public RectTransform inventoryTarget;
    [Tooltip("아이템이 UI에 도착한 뒤 한 번만 아이템 ID를 전달합니다. 인벤토리 저장은 연결한 수신자가 처리합니다.")]
    [SerializeField] UnityEvent<string> collected = new UnityEvent<string>();
    [Header("수집 연출")]
    [SerializeField, Min(0.05f)] float duration = 0.55f;
    [SerializeField, Min(0f)] float arcHeight = 100f;
    [SerializeField, Range(0.02f, 0.5f)] float finalScale = 0.12f;
    [SerializeField, Range(64, 512)] int iconResolution = 256;

    public UnityEvent<string> Collected => collected;
    public bool IsBusy => activeItem != null || iconTexture != null;

    Canvas overlay;
    RectTransform overlayRect;
    UnityEngine.UI.RawImage icon;
    RenderTexture iconTexture;
    CatInteractionItem activeItem;
    RectTransform activeTarget;
    Camera sourceCamera;
    Vector2 startScreenPosition;
    Vector2 startPixelSize;
    float elapsed;
    string activeItemId;
    bool previousAvailability;
    RendererState[] rendererStates;
    ColliderState[] colliderStates;
    BodyState[] bodyStates;

    struct RendererState
    {
        public Renderer renderer;
        public bool enabled;
    }

    struct ColliderState
    {
        public Collider collider;
        public bool enabled;
    }

    struct BodyState
    {
        public Rigidbody body;
        public bool isKinematic;
        public bool detectCollisions;
        public Vector3 linearVelocity;
        public Vector3 angularVelocity;
    }

    /// <summary>UI를 생성한 쪽에서 실제 도착 지점을 등록합니다. null은 등록 해제입니다.</summary>
    public void RegisterTarget(RectTransform target)
    {
        if (activeItem != null && activeTarget != target) CancelCollection();
        inventoryTarget = target;
    }

    /// <summary>유효한 UI와 촬영 가능한 외형이 있을 때만 아이템을 예약하고 연출을 시작합니다.</summary>
    public bool TryCollect(CatInteractionItem item, Camera view)
    {
        if (!isActiveAndEnabled || IsBusy || item == null || !item.isActiveAndEnabled
            || !item.IsAvailable || view == null
            || !TryGetTargetScreenPoint(inventoryTarget, view, out _)) return false;

        Bounds bounds = item.WorldBounds;
        Vector3 screenPosition = view.WorldToScreenPoint(bounds.center);
        if (screenPosition.z <= view.nearClipPlane) return false;
        Renderer[] renderers = item.VisualRenderers;
        // 촬영 실패 시 임시 텍스처 정리는 촬영 도구가 책임집니다.
        if (!ItemIconCapture.TryCapture(renderers, bounds, view.transform.rotation,
            iconResolution, out RenderTexture texture))
            return false;

        EnsureOverlay();
        activeItem = item;
        activeItemId = item.itemId;
        activeTarget = inventoryTarget;
        sourceCamera = view;
        previousAvailability = item.IsAvailable;
        item.IsAvailable = false;
        SaveAndHideItem(item, renderers);
        iconTexture = texture;
        icon.texture = iconTexture;
        elapsed = 0f;
        startScreenPosition = screenPosition;
        float pixelSize = GetScreenSize(view, bounds);
        startPixelSize = Vector2.one * pixelSize;
        overlay.targetDisplay = view.targetDisplay;
        overlay.gameObject.SetActive(true);
        icon.gameObject.SetActive(true);
        UpdateIcon(0f, startScreenPosition);
        return true;
    }

    void LateUpdate()
    {
        if (activeItem == null)
        {
            // 외부에서 아이템을 파괴했을 때 남은 임시 UI도 해제합니다.
            if (iconTexture != null) CancelCollection();
            return;
        }
        if (!activeItem.isActiveAndEnabled || activeTarget != inventoryTarget
            || sourceCamera == null
            || !TryGetTargetScreenPoint(activeTarget, sourceCamera, out Vector2 targetPoint))
        {
            CancelCollection();
            return;
        }

        elapsed += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(elapsed / Mathf.Max(0.05f, duration));
        UpdateIcon(t, targetPoint);
        if (t >= 1f) CompleteCollection();
    }

    void UpdateIcon(float t, Vector2 targetPoint)
    {
        float eased = t * t * (3f - 2f * t);
        Vector2 point = Vector2.LerpUnclamped(startScreenPosition, targetPoint, eased);
        point.y += Mathf.Sin(t * Mathf.PI) * arcHeight;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(overlayRect, point, null, out Vector2 local);
        icon.rectTransform.anchoredPosition = local;
        icon.rectTransform.sizeDelta = startPixelSize / Mathf.Max(0.001f, overlay.scaleFactor);
        icon.rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, finalScale, eased);
    }

    static bool TryGetTargetScreenPoint(RectTransform target, Camera view, out Vector2 point)
    {
        point = default;
        if (target == null || !target.gameObject.activeInHierarchy
            || target.rect.width <= 0f || target.rect.height <= 0f) return false;

        Canvas canvas = target.GetComponentInParent<Canvas>();
        if (canvas == null || !canvas.isActiveAndEnabled) return false;
        Canvas rootCanvas = canvas.rootCanvas;
        if (rootCanvas == null || !rootCanvas.isActiveAndEnabled
            || rootCanvas.targetDisplay != view.targetDisplay) return false;

        Camera uiCamera = null;
        if (rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = rootCanvas.worldCamera;
            if (uiCamera == null && rootCanvas.renderMode == RenderMode.WorldSpace) uiCamera = view;
            if (uiCamera == null || !uiCamera.isActiveAndEnabled) return false;
        }
        Vector3 worldCenter = target.TransformPoint(target.rect.center);
        if (uiCamera != null && uiCamera.WorldToScreenPoint(worldCenter).z <= uiCamera.nearClipPlane)
            return false;
        point = RectTransformUtility.WorldToScreenPoint(uiCamera, worldCenter);
        return true;
    }

    void EnsureOverlay()
    {
        if (overlay != null) return;
        var canvasObject = new GameObject("CatPickupOverlay", typeof(RectTransform), typeof(Canvas),
            typeof(UnityEngine.UI.CanvasScaler), typeof(CanvasGroup));
        canvasObject.hideFlags = HideFlags.DontSave;
        overlay = canvasObject.GetComponent<Canvas>();
        overlay.renderMode = RenderMode.ScreenSpaceOverlay;
        overlay.sortingOrder = 32760;
        overlayRect = canvasObject.GetComponent<RectTransform>();
        var scaler = canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        var group = canvasObject.GetComponent<CanvasGroup>();
        group.interactable = false;
        group.blocksRaycasts = false;

        var iconObject = new GameObject("CollectedItemIcon", typeof(RectTransform),
            typeof(UnityEngine.UI.RawImage));
        iconObject.transform.SetParent(overlayRect, false);
        icon = iconObject.GetComponent<UnityEngine.UI.RawImage>();
        icon.raycastTarget = false;
        icon.color = Color.white;
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        icon.rectTransform.sizeDelta = new Vector2(96f, 96f);
        canvasObject.SetActive(false);
    }

    static float GetScreenSize(Camera view, Bounds bounds)
    {
        Vector2 minimum = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
        Vector2 maximum = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + new Vector3(
                (i & 1) == 0 ? -bounds.extents.x : bounds.extents.x,
                (i & 2) == 0 ? -bounds.extents.y : bounds.extents.y,
                (i & 4) == 0 ? -bounds.extents.z : bounds.extents.z);
            Vector3 projected = view.WorldToScreenPoint(corner);
            if (projected.z <= view.nearClipPlane) continue;
            minimum = Vector2.Min(minimum, projected);
            maximum = Vector2.Max(maximum, projected);
        }
        float size = Mathf.Max(maximum.x - minimum.x, maximum.y - minimum.y) * 1.12f;
        return Mathf.Clamp(size, 32f, 240f);
    }

    void SaveAndHideItem(CatInteractionItem item, Renderer[] renderers)
    {
        rendererStates = new RendererState[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            rendererStates[i] = new RendererState { renderer = renderer, enabled = renderer != null && renderer.enabled };
            if (renderer != null) renderer.enabled = false;
        }
        Collider[] colliders = item.GetComponentsInChildren<Collider>(true);
        colliderStates = new ColliderState[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            colliderStates[i] = new ColliderState { collider = colliders[i], enabled = colliders[i].enabled };
            colliders[i].enabled = false;
        }
        Rigidbody[] bodies = item.GetComponentsInChildren<Rigidbody>(true);
        bodyStates = new BodyState[bodies.Length];
        for (int i = 0; i < bodies.Length; i++)
        {
            Rigidbody body = bodies[i];
            bodyStates[i] = new BodyState
            {
                body = body,
                isKinematic = body.isKinematic,
                detectCollisions = body.detectCollisions,
                linearVelocity = body.linearVelocity,
                angularVelocity = body.angularVelocity
            };
            body.isKinematic = true;
            body.detectCollisions = false;
        }
    }

    void RestoreItemState()
    {
        if (rendererStates != null)
            foreach (RendererState state in rendererStates)
                if (state.renderer != null) state.renderer.enabled = state.enabled;
        if (colliderStates != null)
            foreach (ColliderState state in colliderStates)
                if (state.collider != null) state.collider.enabled = state.enabled;
        if (bodyStates != null)
            foreach (BodyState state in bodyStates)
            {
                if (state.body == null) continue;
                state.body.isKinematic = state.isKinematic;
                state.body.detectCollisions = state.detectCollisions;
                if (!state.isKinematic)
                {
                    state.body.linearVelocity = state.linearVelocity;
                    state.body.angularVelocity = state.angularVelocity;
                }
            }
        rendererStates = null;
        colliderStates = null;
        bodyStates = null;
    }

    void CompleteCollection()
    {
        CatInteractionItem item = activeItem;
        string itemId = activeItemId;
        // 비활성화 콜백이나 이벤트에서 다시 진입해도 완료가 중복되지 않게 먼저 예약을 해제합니다.
        activeItem = null;
        activeTarget = null;
        item.gameObject.SetActive(false);
        RestoreItemState();
        ClearPresentation();
        collected?.Invoke(itemId);
    }

    void CancelCollection()
    {
        if (activeItem != null) activeItem.IsAvailable = previousAvailability;
        activeItem = null;
        activeTarget = null;
        RestoreItemState();
        ClearPresentation();
    }

    void ClearPresentation()
    {
        if (icon != null) icon.texture = null;
        if (overlay != null) overlay.gameObject.SetActive(false);
        ItemIconCapture.Release(iconTexture);
        iconTexture = null;
        sourceCamera = null;
        activeItemId = null;
    }

    void OnDisable() => CancelCollection();

    void OnDestroy()
    {
        CancelCollection();
        if (overlay != null) Destroy(overlay.gameObject);
    }
}
