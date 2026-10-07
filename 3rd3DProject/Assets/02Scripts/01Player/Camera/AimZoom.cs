using UnityEngine;
using Unity.Cinemachine;

/// <summary>조준 상태를 Cinemachine 렌즈에 연결합니다. 실제 Main Camera는 Brain이 출력합니다.</summary>
[DefaultExecutionOrder(-50)]
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineCamera))]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "CatAimZoom")]
public sealed class AimZoom : MonoBehaviour
{
    [Tooltip("IAimState를 제공하는 플레이어 전투 컴포넌트를 연결합니다.")]
    public MonoBehaviour aimSource;
    [Tooltip("조준 시 시야각입니다. 작을수록 확대되며 기본 시야각보다 넓어지지 않습니다.")]
    [Range(1f, 179f)] public float aimedFieldOfView = 40f;
    [Tooltip("확대·복귀가 부드러워지는 시간입니다. 0이면 즉시 전환합니다.")]
    [Min(0f)] public float transitionSeconds = .16f;

    CinemachineCamera virtualCamera;
    float originalFieldOfView;
    float zoomVelocity;
    bool hasOriginalLens;

    public float CurrentFieldOfView => virtualCamera != null ? virtualCamera.Lens.FieldOfView : 0f;

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        virtualCamera = GetComponent<CinemachineCamera>();
        originalFieldOfView = virtualCamera.Lens.FieldOfView;
        hasOriginalLens = true;
        zoomVelocity = 0f;
    }

    void Update()
    {
        if (!Application.isPlaying || !hasOriginalLens || !virtualCamera) return;
        bool aiming = aimSource && aimSource.isActiveAndEnabled && aimSource is IAimState state && state.IsAiming;
        float target = aiming ? Mathf.Min(originalFieldOfView, Mathf.Clamp(aimedFieldOfView, 1f, 179f)) : originalFieldOfView;
        float next = transitionSeconds <= 0f ? target : Mathf.SmoothDamp(
            virtualCamera.Lens.FieldOfView, target, ref zoomVelocity, transitionSeconds,
            Mathf.Infinity, Time.unscaledDeltaTime);
        virtualCamera.Lens.FieldOfView = next;
    }

    void OnDisable()
    {
        if (Application.isPlaying && hasOriginalLens && virtualCamera)
            virtualCamera.Lens.FieldOfView = originalFieldOfView;
        zoomVelocity = 0f;
        hasOriginalLens = false;
    }
}
