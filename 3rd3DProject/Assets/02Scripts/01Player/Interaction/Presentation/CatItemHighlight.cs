using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>원본 재질을 건드리지 않고 조준 중인 모델만 덧그립니다.</summary>
public sealed class CatItemHighlight : System.IDisposable
{
    readonly Material fill, edge, halo;
    readonly Camera view;
    ICatHighlightSource target;
    int revision;
    struct Part { public Mesh source, smooth; public Transform transform; public Renderer renderer; public SkinnedMeshRenderer skinned; public bool ownsSource; }
    readonly List<Part> parts = new List<Part>();
    public CatItemHighlight(Camera camera, Material fillMaterial, Material edgeMaterial, Material haloMaterial)
    { view = camera; fill = fillMaterial; edge = edgeMaterial; halo = haloMaterial; }
    public void SetTarget(ICatHighlightSource value)
    {
        // 인터페이스 참조에는 Unity의 파괴된 Object == null 비교가 적용되지 않습니다.
        if (value != null && (value.HighlightOwner == null || !value.IsHighlightAvailable)) value = null;
        if (ReferenceEquals(target, value) && (value == null || revision == value.VisualRevision)) return;
        Release(); target = value;
        if (target == null) return;
        revision = target.VisualRevision;
        foreach (var renderer in target.VisualRenderers)
        {
            if (renderer == null) continue;
            var filter = renderer.GetComponent<MeshFilter>();
            if (renderer is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
            {
                var baked = new Mesh { name = "Highlight baked mesh" }; skinned.BakeMesh(baked);
                parts.Add(new Part { source = baked, transform = renderer.transform, renderer = renderer, skinned = skinned, ownsSource = true });
                continue;
            }
            if (filter == null || filter.sharedMesh == null) continue;
            var original = filter.sharedMesh;
            Mesh smooth = null;
            if (original.isReadable)
            {
                smooth = Object.Instantiate(original); smooth.name = original.name + " Highlight normals";
                var vertices = original.vertices; var normals = original.normals;
                if (normals.Length != vertices.Length) { smooth.RecalculateNormals(); normals = smooth.normals; }
                var sums = new Dictionary<Vector3, Vector3>();
                for (int i = 0; i < vertices.Length; i++) { sums.TryGetValue(vertices[i], out var n); sums[vertices[i]] = n + normals[i]; }
                for (int i = 0; i < vertices.Length; i++) normals[i] = sums[vertices[i]].normalized;
                smooth.normals = normals;
            }
            parts.Add(new Part { source = original, smooth = smooth, transform = renderer.transform, renderer = renderer });
        }
    }
    public void Draw()
    {
        if (target == null || target.HighlightOwner == null || !target.IsHighlightAvailable)
        {
            SetTarget(null);
            return;
        }
        foreach (var part in parts)
        {
            if (part.renderer == null || !part.renderer.enabled || !part.renderer.gameObject.activeInHierarchy) continue;
            if (part.skinned != null) part.skinned.BakeMesh(part.source);
            DrawPart(part, fill, part.source);
            DrawPart(part, halo, part.smooth != null ? part.smooth : part.source);
            DrawPart(part, edge, part.smooth != null ? part.smooth : part.source);
        }
    }
    void DrawPart(Part part, Material material, Mesh mesh)
    {
        if (material == null) return;
        Bounds bounds = part.renderer.bounds; bounds.Expand(.04f);
        var parameters = new RenderParams(material) { camera = view, worldBounds = bounds, shadowCastingMode = ShadowCastingMode.Off, receiveShadows = false };
        for (int i = 0; i < mesh.subMeshCount; i++) Graphics.RenderMesh(parameters, mesh, i, part.transform.localToWorldMatrix);
    }
    void Release() { foreach (var p in parts) { if (p.smooth != null) Object.Destroy(p.smooth); if (p.ownsSource && p.source != null) Object.Destroy(p.source); } parts.Clear(); }
    public void Dispose() { Release(); target = null; }
}
