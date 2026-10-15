using System;
using UnityEngine;

/// <summary>이동·점프 정보를 Animator 매개변수에 전달합니다. 전환 조건과 시간은 Controller에서 편집합니다.</summary>
public sealed class PlayerAnimation
{
    [Serializable]
    public sealed class Settings
    {
        [Tooltip("Animator의 대기 상태 이름")] public string idle = "Idle";
        [Tooltip("Animator의 걷기 상태 이름")] public string walk = "Walk";
        [Tooltip("Animator의 달리기 상태 이름")] public string run = "Run";
        [Min(0)] public float transitionSeconds = 0.12f;
        [Tooltip("승인된 제자리 점프 클립의 상태 이름")] public string jump = "Jump";
        [Tooltip("클립의 재생 위치를 전달할 Animator float 매개변수")] public string jumpTime = "JumpTime";
    }

    static readonly int Movement = Animator.StringToHash("Movement");
    static readonly int JumpActive = Animator.StringToHash("JumpActive");
    readonly Animator animator;
    readonly bool usesStateGraph;
    readonly int idle, walk, run, jump, jumpTime;
    public bool SupportsJump { get; }
    readonly float transitionSeconds;
    int currentState;
    bool hasState;

    public PlayerAnimation(Animator animator, Settings settings)
    {
        this.animator = animator;
        settings ??= new Settings();
        idle = Animator.StringToHash(settings.idle);
        walk = Animator.StringToHash(settings.walk);
        run = Animator.StringToHash(settings.run);
        jump = Animator.StringToHash(settings.jump);
        jumpTime = Animator.StringToHash(settings.jumpTime);
        bool hasTimeParameter = false;
        bool hasMovement = false, hasJumpActive = false;
        foreach (var parameter in animator.parameters)
        {
            if (parameter.nameHash == jumpTime && parameter.type == AnimatorControllerParameterType.Float)
                hasTimeParameter = true;
            if (parameter.nameHash == Movement && parameter.type == AnimatorControllerParameterType.Int)
                hasMovement = true;
            if (parameter.nameHash == JumpActive && parameter.type == AnimatorControllerParameterType.Bool)
                hasJumpActive = true;
        }
        usesStateGraph = hasMovement && hasJumpActive;
        SupportsJump = usesStateGraph && animator.HasState(0, jump) && hasTimeParameter;
        transitionSeconds = Mathf.Max(0, settings.transitionSeconds);
        animator.applyRootMotion = false;
    }

    public void Apply(MovementIntent intent, JumpFrame jumpFrame = default)
    {
        if (usesStateGraph)
        {
            animator.SetInteger(Movement, (int)MovementPolicy.SelectAnimation(intent));
            animator.SetBool(JumpActive, jumpFrame.Active && SupportsJump);
            // 복귀 전환 중에도 Animator는 나가는 Jump 자세를 함께 읽습니다.
            // Ready의 0초를 쓰면 착지 자세가 갑자기 준비 자세로 되감깁니다.
            if (SupportsJump && jumpFrame.Active)
                animator.SetFloat(jumpTime, Mathf.Clamp01(jumpFrame.PoseTime / 2f));
            return;
        }
        ApplyLegacyController(intent);
    }

    // 다른 씬에서 아직 사용하는 매개변수 없는 Idle.controller의 동작을 보존합니다.
    // PlaytestScene01의 PlayerJump.controller는 이 호환 경로를 사용하지 않습니다.
    void ApplyLegacyController(MovementIntent intent)
    {
        MovementState state = MovementPolicy.SelectAnimation(intent);
        int next = state == MovementState.Idle ? idle : state == MovementState.Run ? run : walk;
        if (hasState && next == currentState) return;
        animator.CrossFadeInFixedTime(next, transitionSeconds);
        currentState = next;
        hasState = true;
    }

    public void InvalidateState()
    {
        if (usesStateGraph)
        {
            if (animator)
            {
                animator.SetInteger(Movement, 0);
                animator.SetBool(JumpActive, false);
                if (SupportsJump) animator.SetFloat(jumpTime, 0);
            }
            return;
        }
        hasState = false;
    }
}
