using UnityEngine;
using UnityEngine.Rendering;

/// <summary>현재 산탄 퍼짐 각도를 카메라 시야각에 맞는 속 빈 원으로 표시합니다.</summary>
[DefaultExecutionOrder(11010)]
[DisallowMultipleComponent]
public sealed class CatShotSpreadRing : MonoBehaviour
{
    public Camera view;
    [Tooltip("ICatShotSpreadState를 제공하는 전투 컴포넌트입니다.")]
    public MonoBehaviour stateSource;
    [Tooltip("Cat Player/Shot Spread Ring 셰이더를 쓰는 표시용 재질입니다.")]
    public Material ringMaterial;
    [Min(.2f)] public float lineWidthPixels = 1.4f;
    [Min(0f)] public float outlineWidthPixels = .6f;
    public Color ringColor = new Color(1f, 1f, 1f, .8f);
    public Color outlineColor = new Color(.05f, .05f, .05f, .65f);

    public float RadiusPixels { get; private set; }
    public bool IsVisible { get; private set; }
    Mesh quad;
    MaterialPropertyBlock properties;
    static readonly int ColorId = Shader.PropertyToID("_Color");
    static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    static readonly int RadiusId = Shader.PropertyToID("_Radius");
    static readonly int HalfWidthId = Shader.PropertyToID("_HalfWidth");
    static readonly int BorderId = Shader.PropertyToID("_BorderWidth");

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        properties ??= new MaterialPropertyBlock();
        quad = new Mesh { name = "Cat Shot Spread Ring (runtime)", hideFlags = HideFlags.HideAndDontSave };
        quad.vertices = new[] { new Vector3(-.5f, -.5f), new Vector3(.5f, -.5f), new Vector3(.5f, .5f), new Vector3(-.5f, .5f) };
        quad.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
        quad.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        quad.RecalculateBounds();
        quad.UploadMeshData(true);
    }

    void LateUpdate()
    {
        RadiusPixels = 0f;
        IsVisible = false;
        if (!quad || !view || !ringMaterial || !stateSource || !stateSource.isActiveAndEnabled ||
            Cursor.lockState != CursorLockMode.Locked || !(stateSource is ICatShotSpreadState state) || !state.IsWeaponEquipped) return;
        float angle = state.CurrentSpreadDegrees;
        if (float.IsNaN(angle) || float.IsInfinity(angle) || angle <= 0f || angle >= 89f) return;

        float depth = Mathf.Max(view.nearClipPlane + .08f, .2f);
        float worldHeight = view.orthographic ? 2f * view.orthographicSize
            : 2f * depth * Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f);
        float worldPerPixel = worldHeight / Mathf.Max(1, view.pixelHeight);
        if (worldPerPixel <= 0f || float.IsNaN(worldPerPixel) || float.IsInfinity(worldPerPixel)) return;
        float radius = depth * Mathf.Tan(angle * Mathf.Deg2Rad);
        RadiusPixels = radius / worldPerPixel;
        float halfWidth = Mathf.Max(.2f, lineWidthPixels) * .5f;
        float border = Mathf.Max(0f, outlineWidthPixels);
        // 테두리와 부드러운 가장자리만큼 여백을 두고 원 중심선은 실제 퍼짐 반경에 맞춥니다.
        float halfExtent = radius + (halfWidth + border + 2f) * worldPerPixel;
        properties.SetColor(ColorId, ringColor);
        properties.SetColor(OutlineColorId, outlineColor);
        properties.SetFloat(RadiusId, radius / halfExtent);
        properties.SetFloat(HalfWidthId, halfWidth * worldPerPixel / halfExtent);
        properties.SetFloat(BorderId, border * worldPerPixel / halfExtent);
        Transform cameraTransform = view.transform;
        var matrix = Matrix4x4.TRS(cameraTransform.position + cameraTransform.forward * depth,
            cameraTransform.rotation, new Vector3(halfExtent * 2f, halfExtent * 2f, 1f));
        Graphics.DrawMesh(quad, matrix, ringMaterial, gameObject.layer, view, 0, properties,
            ShadowCastingMode.Off, false, null, LightProbeUsage.Off);
        IsVisible = true;
    }

    void OnDisable()
    {
        RadiusPixels = 0f;
        IsVisible = false;
        if (quad) Destroy(quad);
        quad = null;
    }
}
