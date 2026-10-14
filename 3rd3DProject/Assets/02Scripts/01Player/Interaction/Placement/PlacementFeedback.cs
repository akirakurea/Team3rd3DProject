using System;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 설치할 위치와 남은 시간을 보여 줍니다. 원본 덫의 메시만 복사하므로
/// 미리보기에서 덫 스크립트나 충돌 감지가 실행되지 않습니다.
/// </summary>
public sealed class PlacementFeedback : IDisposable
{
    static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly Color AllowedColor = new Color(.16f, 1f, .36f, .38f);
    static readonly Color BlockedColor = new Color(1f, .18f, .12f, .38f);

    readonly GameObject preview;
    readonly Material material;
    readonly UnityEngine.UI.Image progressFill;
    readonly TMP_Text statusText;
    readonly bool hasBaseColor;
    readonly bool hasColor;
    bool disposed;
    bool? lastValidity;
    int lastStatusKey = int.MinValue;

    public PlacementFeedback(GameObject sourcePrefab, Material previewMaterial,
        UnityEngine.UI.Image progressFill, TMP_Text statusText)
    {
        this.progressFill = progressFill;
        this.statusText = statusText;
        if (progressFill)
        {
            progressFill.raycastTarget = false;
            progressFill.fillAmount = 0f;
            progressFill.enabled = false;
        }
        if (statusText)
        {
            statusText.raycastTarget = false;
            statusText.enabled = false;
        }

        if (!sourcePrefab || !previewMaterial) return;
        material = new Material(previewMaterial)
        {
            name = "Trap Placement Preview (runtime)",
            hideFlags = HideFlags.DontSave
        };
        hasBaseColor = material.HasProperty(BaseColorId);
        hasColor = material.HasProperty(ColorId);

        // 프리팹 전체를 Instantiate하지 않습니다. Transform과 렌더러만 생성합니다.
        preview = new GameObject("Trap Placement Preview")
        {
            hideFlags = HideFlags.DontSave,
            layer = 2 // Ignore Raycast. 미리보기에는 Collider도 만들지 않습니다.
        };
        preview.SetActive(false);
        preview.transform.localScale = sourcePrefab.transform.localScale;
        CopyVisual(sourcePrefab.transform, preview.transform);
    }

    public void Show(bool visible, PlacementCandidate candidate, Vector3 rootOffset, float progress)
    {
        if (disposed) return;
        bool showPreview = visible && candidate.HasSurface;
        if (preview)
        {
            if (preview.activeSelf != showPreview) preview.SetActive(showPreview);
            if (showPreview)
            {
                preview.transform.SetPositionAndRotation(candidate.Position - rootOffset, Quaternion.identity);
                SetValidityColor(candidate.IsValid);
            }
        }

        bool showProgress = showPreview && candidate.IsValid;
        float amount = showProgress && !float.IsNaN(progress) ? Mathf.Clamp01(progress) : 0f;
        if (progressFill)
        {
            progressFill.enabled = showProgress;
            progressFill.fillAmount = amount;
        }
        if (!statusText) return;
        statusText.enabled = visible;
        if (!visible) return;

        // 진행률이 같은 동안에는 문자열을 다시 만들지 않습니다.
        int percent = Mathf.FloorToInt(amount * 100f);
        int key = !candidate.HasSurface ? -1 : !candidate.IsValid
            ? -10 - (int)candidate.Failure : percent;
        if (key == lastStatusKey) return;
        lastStatusKey = key;
        if (!candidate.HasSurface)
            statusText.text = "설치할 바닥을 조준하세요.";
        else if (!candidate.IsValid)
            statusText.text = FailureMessage(candidate.Failure);
        else if (percent == 0)
            statusText.text = "좌클릭 또는 F를 1초간 누르면 덫을 설치합니다.";
        else
            statusText.text = "덫 설치 중... " + percent + "%";
    }

    void CopyVisual(Transform source, Transform destination)
    {
        if (source.TryGetComponent<MeshFilter>(out var meshFilter) && meshFilter.sharedMesh &&
            source.TryGetComponent<MeshRenderer>(out var sourceRenderer) && sourceRenderer.enabled)
        {
            destination.gameObject.AddComponent<MeshFilter>().sharedMesh = meshFilter.sharedMesh;
            var renderer = destination.gameObject.AddComponent<MeshRenderer>();
            var materials = new Material[meshFilter.sharedMesh.subMeshCount];
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        }

        for (int i = 0; i < source.childCount; i++)
        {
            Transform child = source.GetChild(i);
            var copy = new GameObject(child.name)
            {
                hideFlags = HideFlags.DontSave,
                layer = 2
            };
            copy.transform.SetParent(destination, false);
            copy.transform.localPosition = child.localPosition;
            copy.transform.localRotation = child.localRotation;
            copy.transform.localScale = child.localScale;
            copy.SetActive(child.gameObject.activeSelf);
            CopyVisual(child, copy.transform);
        }
    }

    void SetValidityColor(bool valid)
    {
        if (!material || lastValidity == valid) return;
        lastValidity = valid;
        Color color = valid ? AllowedColor : BlockedColor;
        if (hasBaseColor) material.SetColor(BaseColorId, color);
        if (hasColor) material.SetColor(ColorId, color);
    }

    static string FailureMessage(PlacementFailure failure)
    {
        switch (failure)
        {
            case PlacementFailure.TooFar: return "설치할 위치가 너무 멉니다.";
            case PlacementFailure.Blocked: return "장애물이 있어 설치할 수 없습니다.";
            case PlacementFailure.Uneven: return "바닥이 고르지 않아 설치할 수 없습니다.";
            case PlacementFailure.Occupied: return "이미 덫이 놓인 위치입니다.";
            case PlacementFailure.NoFloor: return "설치할 바닥을 조준하세요.";
            default: return "이 위치에는 설치할 수 없습니다.";
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (progressFill)
        {
            progressFill.fillAmount = 0f;
            progressFill.enabled = false;
        }
        if (statusText) statusText.enabled = false;
        Release(preview);
        Release(material);
    }

    static void Release(UnityEngine.Object value)
    {
        if (!value) return;
        if (Application.isPlaying) UnityEngine.Object.Destroy(value);
        else UnityEngine.Object.DestroyImmediate(value);
    }
}
