using System;
using UnityEngine;

/// <summary>접지를 Unity 물리로 읽어 점프 규칙에 전달합니다. Rigidbody에는 직접 쓰지 않습니다.</summary>
public sealed class PlayerJump
{
    [Serializable]
    public sealed class Settings
    {
        [Tooltip("승인된 점프 클립과 상태가 연결된 플레이어에서만 켭니다.")]
        public bool enabled;
        [Tooltip("발이 떠오르는 목표 높이. Unity 기본 중력으로 이동합니다.")]
        [Min(0.05f)] public float height = 0.45f;
        [Tooltip("착지 직전에 누른 Space를 기억하는 시간(초). 0이면 점프 입력을 미리 보관하지 않습니다.")]
        [Range(0f, 0.3f)] public float jumpBufferSeconds = 0.15f;
        [Tooltip("이 각도보다 가파른 면은 서 있는 바닥으로 취급하지 않습니다.")]
        [Range(0, 80)] public float maxGroundAngle = 55f;
        public LayerMask groundLayers = ~0;
    }

    const float Skin = 0.01f;
    const float ProbeLift = 0.04f;
    const float GroundDistance = 0.025f;
    readonly Rigidbody body;
    readonly CapsuleCollider capsule;
    readonly Settings settings;
    readonly JumpPolicy policy = new JumpPolicy();
    readonly RaycastHit[] hits = new RaycastHit[24];

    public bool IsGrounded { get; private set; }
    public float LaunchSpeed { get; private set; }
    public JumpFrame Current => policy.Current;

    public PlayerJump(Rigidbody body, CapsuleCollider capsule, Settings settings)
    {
        this.body = body;
        this.capsule = capsule;
        this.settings = settings;
    }

    public JumpFrame Step(bool pressed, float deltaTime, bool inputEnabled = true, bool moving = false)
    {
        if (!body || !capsule || !capsule.enabled || settings == null || !settings.enabled ||
            body.isKinematic || !body.useGravity || Physics.gravity.y >= -0.001f)
        {
            Reset();
            return Current;
        }

        LaunchSpeed = Mathf.Sqrt(2f * Mathf.Max(0.05f, settings.height) * -Physics.gravity.y);
        IsGrounded = HasSupport() && body.linearVelocity.y <= 0.2f;
        return policy.Step(pressed, IsGrounded, body.linearVelocity.y, deltaTime, LaunchSpeed,
            Mathf.Max(0f, settings.jumpBufferSeconds), inputEnabled, moving);
    }

    bool HasSupport()
    {
        // 화면 보간으로 늦어진 Transform 대신 현재 물리 위치를 기준으로 검사합니다.
        Vector3 scale = capsule.transform.lossyScale;
        float radius = capsule.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float height = Mathf.Max(capsule.height * Mathf.Abs(scale.y), radius * 2);
        Vector3 center = body.position + body.rotation * Vector3.Scale(capsule.center, scale);
        Vector3 bottom = center - (body.rotation * Vector3.up) * (height * 0.5f - radius);
        float queryRadius = Mathf.Max(0.005f, radius - Skin);
        int count = Physics.SphereCastNonAlloc(bottom + Vector3.up * ProbeLift, queryRadius,
            Vector3.down, hits, ProbeLift + Skin + GroundDistance, settings.groundLayers,
            QueryTriggerInteraction.Ignore);
        if (count == hits.Length) return false;

        float minNormalY = Mathf.Cos(settings.maxGroundAngle * Mathf.Deg2Rad);
        for (int i = 0; i < count; i++)
        {
            var other = hits[i].collider;
            if (!other || other == capsule || other.attachedRigidbody == body ||
                other.transform.IsChildOf(body.transform) || hits[i].normal.y < minNormalY ||
                Physics.GetIgnoreLayerCollision(capsule.gameObject.layer, other.gameObject.layer) ||
                Physics.GetIgnoreCollision(capsule, other)) continue;
            return true;
        }
        return false;
    }

    public void CancelBufferedInput() => policy.CancelBufferedInput();

    public void Reset()
    {
        policy.Reset();
        IsGrounded = false;
        LaunchSpeed = 0;
    }
}
