using System;
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

    // 이전 저장 상태를 읽기 위해 형식과 필드명을 유지합니다.
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
        if (releaseRecovery != null) TryCompleteRelease();
    }

    internal void BeginReleaseRecovery(Collider[] colliders, bool[] enabled, bool available)
    {
        releaseRecovery = new ReleaseRecovery
        {
            origin = Body.position, rotation = Body.rotation,
            colliders = colliders, enabled = enabled, available = available
        };
        TryCompleteRelease();
    }

    bool TryCompleteRelease()
    {
        if (releaseRecovery == null || !Body) return false;
        ReleaseRecovery saved = releaseRecovery;
        releaseRecovery = null;
        if (saved.colliders != null && saved.enabled != null)
            for (int i = 0; i < Mathf.Min(saved.colliders.Length, saved.enabled.Length); i++)
                if (saved.colliders[i]) saved.colliders[i].enabled = saved.enabled[i];
        // 예전 대기 상태도 위치를 바꾸지 않고 즉시 물리 시뮬레이션으로 넘깁니다.
        Body.isKinematic = false;
        Body.useGravity = true;
        Body.linearVelocity = Vector3.zero;
        Body.angularVelocity = Vector3.zero;
        Body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        Body.WakeUp();
        IsAvailable = saved.available;
        return true;
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
