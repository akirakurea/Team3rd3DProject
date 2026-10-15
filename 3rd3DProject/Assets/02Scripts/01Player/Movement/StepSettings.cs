using System;
using UnityEngine;

[Serializable]
public sealed class StepSettings
{
    [Tooltip("넘을 수 있는 최대 턱 높이. 0이면 보정을 끕니다.")]
    [Min(0)] public float maxHeight = 0.28f;
    [Tooltip("충돌체 앞쪽에서 미리 검사하는 거리입니다.")]
    [Min(0.02f)] public float forwardReach = 0.12f;
    [Tooltip("턱을 오르는 초당 최대 높이입니다.")]
    [Min(0.1f)] public float riseSpeed = 1.8f;
    [Tooltip("착지할 수 있는 표면의 최대 경사각입니다.")]
    [Range(0, 60)] public float maxLandingSlope = 45f;
    [Tooltip("턱·바닥·공간 검사에 포함할 레이어입니다.")]
    public LayerMask solidLayers = Physics.DefaultRaycastLayers;
}
