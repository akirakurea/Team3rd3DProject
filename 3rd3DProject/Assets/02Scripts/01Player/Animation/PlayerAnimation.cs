using System;
using UnityEngine;

/// <summary>순수 이동 상태를 Animator 상태 이름에 연결합니다. 이동·입력·물리에는 관여하지 않습니다.</summary>
public sealed class PlayerAnimation
{
    [Serializable]
    public sealed class Settings
    {
        [Tooltip("Animator의 대기 상태 이름")] public string idle = "Idle";
        [Tooltip("Animator의 걷기 상태 이름")] public string walk = "Walk";
        [Tooltip("Animator의 달리기 상태 이름")] public string run = "Run";
        [Min(0)] public float transitionSeconds = 0.12f;
    }

    readonly Animator animator;
    readonly int idle, walk, run;
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
        transitionSeconds = Mathf.Max(0, settings.transitionSeconds);
        animator.applyRootMotion = false;
    }

    public void Apply(MovementIntent intent)
    {
        MovementState state = MovementPolicy.SelectAnimation(intent);
        int next = state == MovementState.Idle ? idle : state == MovementState.Run ? run : walk;
        if (hasState && next == currentState) return;
        animator.CrossFadeInFixedTime(next, transitionSeconds);
        currentState = next;
        hasState = true;
    }

    public void InvalidateState() => hasState = false;
}
