using UnityEngine;

/// <summary>상호작용 동작과 관계없이 강조할 모델과 사용 가능 여부를 제공합니다.</summary>
public interface IHighlightSource
{
    Component HighlightOwner { get; }
    Renderer[] VisualRenderers { get; }
    int VisualRevision { get; }
    bool IsHighlightAvailable { get; }
}
