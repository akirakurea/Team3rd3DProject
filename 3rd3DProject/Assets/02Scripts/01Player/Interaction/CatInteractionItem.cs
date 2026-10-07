using System;
using System.Collections;
using UnityEngine;

public enum ItemKind
{
    CarryOnly,
    InventoryPickup
}

[DisallowMultipleComponent]
[RequireComponent(typeof(BoxCollider), typeof(Rigidbody))]
public sealed class CatInteractionItem : MonoBehaviour, IHighlightSource
{
    [Tooltip("표시용 메시를 교체해도 유지되는 아이템 식별자입니다.")]
    public string itemId;
    public ItemKind kind;
    [Tooltip("교체할 메시가 들어 있는 Visual 자식입니다. 충돌과 Rigidbody는 이 루트에 둡니다.")]
    public Transform visualRoot;

    BoxCollider shape;
    Rigidbody body;
    Renderer[] visualRenderers = Array.Empty<Renderer>();

    // 이 상태만 저장해 비활성화·Play 중 재컴파일 뒤에도 충돌 복구를 이어갑니다.
    [Serializable]
    sealed class ReleaseRecovery
    {
        public Vector3 origin;
        public Quaternion rotation;
        public Collider[] colliders;
        public bool[] enabled;
        public bool available;
    }
    [SerializeReference, HideInInspector] ReleaseRecovery releaseRecovery;
    Coroutine releaseRoutine;
    Collider[] releaseOverlaps;

    public BoxCollider Shape => shape ? shape : shape = GetComponent<BoxCollider>();
    public Rigidbody Body => body ? body : body = GetComponent<Rigidbody>();
    public Component HighlightOwner => this;
    public Renderer[] VisualRenderers => visualRenderers;
    public bool IsAvailable { get; set; } = true;
    public int VisualRevision { get; private set; }
    public bool IsHighlightAvailable => isActiveAndEnabled && IsAvailable;
    public bool IsReleasePending => releaseRecovery != null;

    // 들고 있는 동안 Collider가 꺼져 있어도 실제 표시 크기를 반환합니다.
    public Bounds WorldBounds
    {
        get
        {
            var box = Shape;
            Vector3 half = box.size * 0.5f;
            Vector3 x = transform.TransformVector(Vector3.right * half.x);
            Vector3 y = transform.TransformVector(Vector3.up * half.y);
            Vector3 z = transform.TransformVector(Vector3.forward * half.z);
            Vector3 extents = Abs(x) + Abs(y) + Abs(z);
            return new Bounds(transform.TransformPoint(box.center), extents * 2);
        }
    }

    void Awake() => RefreshVisual();

    void OnEnable()
    {
        // 재컴파일 후 Awake가 생략되더라도 표시 목록을 복구합니다.
        if (visualRenderers == null || visualRenderers.Length == 0) RefreshVisual();
        if (releaseRecovery == null) return;
        IsAvailable = false;
        if (!TryCompleteRelease()) StartReleaseRecovery();
    }

    void OnDisable()
    {
        if (releaseRoutine != null) StopCoroutine(releaseRoutine);
        releaseRoutine = null;
    }

    internal void BeginReleaseRecovery(Collider[] colliders, bool[] enabled, bool available)
    {
        releaseRecovery = new ReleaseRecovery
        {
            origin = Body.position, rotation = Body.rotation,
            colliders = colliders, enabled = enabled, available = available
        };
        Body.isKinematic = true;
        Body.useGravity = false;
        IsAvailable = false;
        if (!TryCompleteRelease()) StartReleaseRecovery();
    }

    void StartReleaseRecovery()
    {
        if (releaseRoutine == null && isActiveAndEnabled)
            releaseRoutine = StartCoroutine(RetryRelease());
    }

    IEnumerator RetryRelease()
    {
        var interval = new WaitForSeconds(0.1f);
        // 대기 중인 물건만 검사합니다. 실패 횟수로 충돌을 강제로 켜지 않습니다.
        while (releaseRecovery != null)
        {
            yield return interval;
            if (TryCompleteRelease()) break;
        }
        releaseRoutine = null;
    }

    bool TryCompleteRelease()
    {
        if (releaseRecovery == null || !Body || !Shape) return false;
        if (!TryResolveReleasePosition(out Vector3 position)) return false;
        ReleaseRecovery saved = releaseRecovery;
        releaseRecovery = null;
        Body.position = position;
        Body.rotation = saved.rotation;
        Body.isKinematic = false;
        Body.useGravity = true;
        Body.linearVelocity = Vector3.zero;
        Body.angularVelocity = Vector3.zero;
        Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        for (int i = 0; i < saved.colliders.Length; i++)
            if (saved.colliders[i]) saved.colliders[i].enabled = saved.enabled[i];
        IsAvailable = saved.available;
        return true;
    }

    bool TryResolveReleasePosition(out Vector3 position)
    {
        // 이 Unity 버전은 비활성 BoxCollider의 분리 계산이 false를 반환할 수 있습니다.
        // 같은 동기 호출 안에서만 활성화해 조회하고 물리 프레임 전에 원래 상태로 돌립니다.
        bool enabled = Shape.enabled;
        try
        {
            Shape.enabled = true;
            return TryResolveActiveShape(out position);
        }
        finally { Shape.enabled = enabled; }
    }

    bool TryResolveActiveShape(out Vector3 position)
    {
        const float skin = 0.015f;
        const float maxTravel = 0.35f;
        position = releaseRecovery.origin;
        Quaternion rotation = releaseRecovery.rotation;
        Vector3 scale = transform.lossyScale;
        Vector3 half = Vector3.Scale(Shape.size * 0.5f,
            new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
        Vector3 center = rotation * Vector3.Scale(Shape.center, scale);
        releaseOverlaps ??= new Collider[64];
        float travel = 0f;
        // 16回まで候補だけを修正し、最後の全件確認を通った位置だけ実際に適用します。
        for (int iteration = 0; iteration <= 16; iteration++)
        {
            int count = Physics.OverlapBoxNonAlloc(position + center, half, releaseOverlaps,
                rotation, ~0, QueryTriggerInteraction.Ignore);
            if (count == releaseOverlaps.Length) return false;
            bool penetrated = false;
            Vector3 correction = default;
            for (int i = 0; i < count; i++)
            {
                Collider other = releaseOverlaps[i];
                if (!other || other.isTrigger || other.transform == transform || other.transform.IsChildOf(transform)
                    || Physics.GetIgnoreLayerCollision(gameObject.layer, other.gameObject.layer)
                    || Physics.GetIgnoreCollision(Shape, other)) continue;
                if (!Physics.ComputePenetration(Shape, position, rotation,
                    other, other.transform.position, other.transform.rotation,
                    out Vector3 direction, out float depth) || depth <= 0f) continue;
                penetrated = true;
                correction = direction * (depth + skin);
                break;
            }
            if (!penetrated) return true;
            if (iteration == 16) return false;
            travel += correction.magnitude;
            if (travel > maxTravel) return false;
            position += correction;
        }
        return false;
    }

    void Reset()
    {
        Body.isKinematic = true;
        Body.useGravity = false;
        RefreshVisual();
    }

    /// <summary>Visual을 바꾼 뒤 호출하면 루트 충돌 크기와 표시용 Renderer 목록을 갱신합니다.</summary>
    [ContextMenu("Visual 크기에 맞춰 충돌 갱신")]
    public void RefreshVisual()
    {
        VisualRevision++;
        if (!visualRoot) visualRoot = transform.Find("Visual");
        if (!visualRoot || visualRoot == transform || !visualRoot.IsChildOf(transform))
        {
            visualRenderers = Array.Empty<Renderer>();
            return;
        }

        visualRenderers = visualRoot.GetComponentsInChildren<Renderer>(true);
        bool hasBounds = false;
        Bounds localBounds = default;
        foreach (Renderer renderer in visualRenderers)
        {
            Bounds meshBounds;
            if (renderer is SkinnedMeshRenderer skinned)
                meshBounds = skinned.localBounds;
            else if (renderer.TryGetComponent<MeshFilter>(out var meshFilter) && meshFilter.sharedMesh)
                meshBounds = meshFilter.sharedMesh.bounds;
            else
                continue;

            Matrix4x4 toRoot = transform.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
            for (int corner = 0; corner < 8; corner++)
            {
                Vector3 offset = new Vector3(
                    (corner & 1) == 0 ? -meshBounds.extents.x : meshBounds.extents.x,
                    (corner & 2) == 0 ? -meshBounds.extents.y : meshBounds.extents.y,
                    (corner & 4) == 0 ? -meshBounds.extents.z : meshBounds.extents.z);
                Vector3 point = toRoot.MultiplyPoint3x4(meshBounds.center + offset);
                if (hasBounds) localBounds.Encapsulate(point);
                else
                {
                    localBounds = new Bounds(point, Vector3.zero);
                    hasBounds = true;
                }
            }
        }

        if (!hasBounds) return;
        Shape.center = localBounds.center;
        Shape.size = Vector3.Max(localBounds.size, Vector3.one * 0.01f);
    }

    static Vector3 Abs(Vector3 value) => new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));
}
