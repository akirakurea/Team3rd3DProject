using UnityEngine;
using Unity.Cinemachine;

/// <summary>순수 회전 규칙을 Cinemachine의 수평 축에 연결합니다. 세로 축은 변경하지 않습니다.</summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
[RequireComponent(typeof(CinemachineOrbitalFollow))]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "CatCameraOrbitLimit")]
public sealed class CameraOrbitLimit : MonoBehaviour
{
    [Tooltip("IMotionState를 제공하는 컴포넌트입니다. 현재는 Cat_Player의 PlayerController를 연결합니다.")]
    public MonoBehaviour motionSource;
    [Tooltip("멈춘 순간의 카메라 방향에서 좌우로 각각 회전할 수 있는 각도입니다.")]
    [Range(0f, 180f)] public float stationaryHalfAngle = 60f;

    readonly OrbitPolicy policy = new OrbitPolicy();
    CinemachineOrbitalFollow orbit;
    Vector2 originalRange;
    float originalCenter;
    bool originalWrap;
    InputAxis.RecenteringSettings originalRecentering;
    bool hasOriginalSettings;

    void OnEnable()
    {
        if (!Application.isPlaying) return;
        orbit = GetComponent<CinemachineOrbitalFollow>();
        var axis = orbit.HorizontalAxis;
        originalRange = axis.Range;
        originalCenter = axis.Center;
        originalWrap = axis.Wrap;
        originalRecentering = axis.Recentering;
        hasOriginalSettings = true;
        policy.Reset();
    }

    void Update()
    {
        if (!orbit || !motionSource || !(motionSource is IMotionState motion)) return;

        // 기본 입력 Update가 끝난 뒤 제한을 적용하고, Brain의 LateUpdate가 결과를 출력합니다.
        var limit = policy.Evaluate(motionSource.isActiveAndEnabled && motion.IsMoving,
            orbit.HorizontalAxis.Value, stationaryHalfAngle);
        ref var axis = ref orbit.HorizontalAxis;
        axis.Range = new Vector2(limit.Minimum, limit.Maximum);
        axis.Center = limit.Center;
        axis.Wrap = limit.Wrap;
        axis.Value = limit.Value;
        axis.Recentering.Enabled = false;
    }

    void OnDisable()
    {
        if (orbit && hasOriginalSettings)
        {
            ref var axis = ref orbit.HorizontalAxis;
            axis.Range = originalRange;
            axis.Center = originalCenter;
            axis.Wrap = originalWrap;
            axis.Recentering = originalRecentering;
            axis.Value = axis.ClampValue(axis.Value);
        }
        hasOriginalSettings = false;
        policy.Reset();
    }
}
