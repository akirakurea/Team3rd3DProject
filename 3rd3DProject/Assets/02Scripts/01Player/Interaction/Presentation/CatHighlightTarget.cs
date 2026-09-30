using System;
using UnityEngine;

/// <summary>들기·획득 기능 없이 기존 노란 강조만 적용하는 대상입니다.</summary>
[DisallowMultipleComponent]
[AddComponentMenu("Cat Player/Interaction/Highlight Target")]
public sealed class CatHighlightTarget : MonoBehaviour, ICatHighlightSource
{
    [Tooltip("비워 두면 이 오브젝트와 모든 자식의 메시를 강조합니다. 조준하려면 이 루트 또는 자식에 활성화된 비트리거 Collider가 필요합니다. 기본 Cube 등은 Collider가 이미 있습니다. 외형 교체 후 강조 Renderer 갱신을 실행하세요.")]
    public Transform visualRoot;

    Renderer[] visualRenderers = Array.Empty<Renderer>();

    public Component HighlightOwner => this;
    public Renderer[] VisualRenderers => visualRenderers;
    public int VisualRevision { get; private set; }
    public bool IsHighlightAvailable => isActiveAndEnabled;

    void Reset() => RefreshRenderers();
    void OnEnable() => RefreshRenderers();

    /// <summary>외형을 교체한 뒤 호출합니다. 충돌체나 Rigidbody는 만들거나 변경하지 않습니다.</summary>
    [ContextMenu("강조 Renderer 갱신")]
    public void RefreshRenderers()
    {
        VisualRevision++;
        Transform root = visualRoot ? visualRoot : transform;
        visualRenderers = root == transform || root.IsChildOf(transform)
            ? root.GetComponentsInChildren<Renderer>(true)
            : Array.Empty<Renderer>();
    }
}
