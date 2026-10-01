using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// 원본 렌더러와 공유 머티리얼은 변경하지 않고, 메시만 한 번 촬영합니다.
internal static class CatItemIconCapture
{
    const int PreviewLayer = 31;
    static readonly Vector3 PreviewPosition = new Vector3(10000f, 10000f, 10000f);

    public static bool TryCapture(Renderer[] sources, Bounds bounds, Quaternion rotation,
        int resolution, out RenderTexture texture)
    {
        texture = null;
        if (sources == null || sources.Length == 0) return false;

        var root = new GameObject("CatItemIconPreview") { hideFlags = HideFlags.HideAndDontSave };
        root.transform.position = PreviewPosition;
        var bakedMeshes = new List<Mesh>();
        RenderTexture previousTarget = RenderTexture.active;
        Camera camera = null;
        bool captured = false;
        try
        {
            int meshCount = 0;
            foreach (Renderer source in sources)
            {
                if (source == null || !source.enabled || !source.gameObject.activeInHierarchy
                    || source.forceRenderingOff) continue;

                Mesh mesh = null;
                if (source is SkinnedMeshRenderer skinned && skinned.sharedMesh != null)
                {
                    mesh = new Mesh { name = "CatItemIconBakedMesh", hideFlags = HideFlags.HideAndDontSave };
                    bakedMeshes.Add(mesh);
                    skinned.BakeMesh(mesh);
                }
                else if (source is MeshRenderer && source.TryGetComponent(out MeshFilter filter))
                {
                    mesh = filter.sharedMesh;
                }
                Material[] materials = source.sharedMaterials;
                if (mesh == null || materials.Length == 0) continue;

                var clone = new GameObject("PreviewMesh", typeof(MeshFilter), typeof(MeshRenderer))
                {
                    layer = PreviewLayer,
                    hideFlags = HideFlags.HideAndDontSave
                };
                clone.transform.SetParent(root.transform, false);
                clone.transform.localPosition = source.transform.position - bounds.center;
                clone.transform.rotation = source.transform.rotation;
                clone.transform.localScale = source.transform.lossyScale;
                clone.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = clone.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = materials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
                var properties = new MaterialPropertyBlock();
                source.GetPropertyBlock(properties);
                renderer.SetPropertyBlock(properties);
                for (int i = 0; i < materials.Length; i++)
                {
                    source.GetPropertyBlock(properties, i);
                    renderer.SetPropertyBlock(properties, i);
                }
                meshCount++;
            }
            if (meshCount == 0) return false;

            var cameraObject = new GameObject("PreviewCamera", typeof(Camera))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            cameraObject.transform.SetParent(root.transform, false);
            camera = cameraObject.GetComponent<Camera>();
            camera.enabled = false;
            camera.cullingMask = 1 << PreviewLayer;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.orthographic = true;
            camera.aspect = 1f;
            camera.allowHDR = false;
            camera.allowMSAA = false;
            camera.useOcclusionCulling = false;
            camera.transform.rotation = rotation;
            float radius = Mathf.Max(0.025f, bounds.extents.magnitude);
            camera.transform.position = PreviewPosition - camera.transform.forward * (radius * 3f);
            camera.nearClipPlane = Mathf.Max(0.001f, radius);
            camera.farClipPlane = radius * 5f;
            camera.orthographicSize = GetHalfSize(bounds.extents, rotation) * 1.12f;

            var lightObject = new GameObject("PreviewLight", typeof(Light))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.rotation = rotation * Quaternion.Euler(25f, -25f, 0f);
            var light = lightObject.GetComponent<Light>();
            light.type = LightType.Directional;
            light.cullingMask = 1 << PreviewLayer;
            light.shadows = LightShadows.None;
            light.intensity = 1.2f;

            texture = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32)
            {
                name = "CatItemPickupIcon",
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            if (!texture.Create()) return false;

            if (GraphicsSettings.currentRenderPipeline == null)
            {
                camera.targetTexture = texture;
                camera.Render();
            }
            else
            {
                var request = new RenderPipeline.StandardRequest { destination = texture };
                if (!RenderPipeline.SupportsRenderRequest(camera, request)) return false;
                RenderPipeline.SubmitRenderRequest(camera, request);
            }
            captured = true;
            return true;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"아이템 아이콘 촬영 실패: {exception.GetType().Name}");
            return false;
        }
        finally
        {
            // 지연 Destroy 이전에도 메인 카메라나 물리 루프에 프리뷰가 남지 않습니다.
            root.SetActive(false);
            if (camera != null) camera.targetTexture = null;
            RenderTexture.active = previousTarget;
            if (!captured)
            {
                Release(texture);
                texture = null;
            }
            Object.Destroy(root);
            foreach (Mesh mesh in bakedMeshes) Object.Destroy(mesh);
        }
    }

    static float GetHalfSize(Vector3 extents, Quaternion rotation)
    {
        Quaternion inverse = Quaternion.Inverse(rotation);
        float halfSize = 0.025f;
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = inverse * new Vector3(
                (i & 1) == 0 ? -extents.x : extents.x,
                (i & 2) == 0 ? -extents.y : extents.y,
                (i & 4) == 0 ? -extents.z : extents.z);
            halfSize = Mathf.Max(halfSize, Mathf.Abs(corner.x), Mathf.Abs(corner.y));
        }
        return halfSize;
    }

    public static void Release(RenderTexture texture)
    {
        if (texture == null) return;
        texture.Release();
        Object.Destroy(texture);
    }
}
