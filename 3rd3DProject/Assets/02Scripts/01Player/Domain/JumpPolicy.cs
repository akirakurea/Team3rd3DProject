using System;

/// <summary>물리 엔진과 무관한 점프의 현재 단계입니다.</summary>
public enum JumpPhase
{
    Ready,
    Anticipation,
    Airborne,
    Landing
}

/// <summary>이번 갱신에서 보여 줄 자세와 한 번의 이륙 요청입니다.</summary>
public readonly struct JumpFrame
{
    public JumpPhase Phase { get; }
    public float PoseTime { get; }
    public bool TakeOff { get; }
    public bool Active => Phase != JumpPhase.Ready;

    public JumpFrame(JumpPhase phase, float poseTime, bool takeOff = false)
    {
        Phase = phase;
        PoseTime = poseTime;
        TakeOff = takeOff;
    }
}

/// <summary>
/// 접지·수직 속도를 받아 점프 단계를 결정합니다.
/// 위치 변경, 중력 계산, 키 읽기와 애니메이션 재생은 연결부가 담당합니다.
/// </summary>
public sealed class JumpPolicy
{
    const float TakeoffTime = .40f;
    const float ApexTime = .855f;
    const float TouchdownTime = 1.31f;
    const float ClipEnd = 2f;
    const float MinimumAirTime = .08f;
    const float BufferedLandingImpact = .13f;
    const float BufferedPreparationTime = .32f;
    const float MovingLandingTime = .18f;
    const float TimeTolerance = .00001f;

    float phaseTime;
    float initialSpeed = 1f;
    float bufferedInputTime;
    bool leftGround;
    bool landingJumpQueued;
    bool bufferedPreparation;
    bool reboundFlight;
    float preparationStartPose;

    public JumpFrame Current { get; private set; } = new JumpFrame(JumpPhase.Ready, 0f);

    /// <param name="pressed">이번 입력에서 새로 누른 경우만 true입니다. 유지 여부가 아닙니다.</param>
    /// <param name="grounded">충돌 규칙을 만족하는 바닥에 닿아 있는지 여부입니다.</param>
    /// <param name="verticalSpeed">현재 수직 속도입니다. 위로 움직이면 양수입니다.</param>
    /// <param name="deltaTime">이번 물리 갱신에 흐른 시간입니다.</param>
    /// <param name="takeoffSpeed">연결부에서 실제로 적용할 최초 상승 속도입니다.</param>
    /// <param name="bufferSeconds">착지 전에 누른 입력을 보관할 시간입니다. 0이면 보관하지 않습니다.</param>
    /// <param name="inputEnabled">메뉴·포커스 등으로 입력이 차단되면 보관 중인 요청을 취소합니다.</param>
    /// <param name="moving">이동 중에는 착지 충격을 표현한 뒤 보행으로 일찍 복귀합니다.</param>
    public JumpFrame Step(bool pressed, bool grounded, float verticalSpeed, float deltaTime, float takeoffSpeed,
        float bufferSeconds = 0f, bool inputEnabled = true, bool moving = false)
    {
        float dt = Math.Max(0f, deltaTime);
        if (!inputEnabled)
        {
            CancelBufferedInput();
            pressed = false;
        }
        else if (pressed)
            bufferedInputTime = Math.Max(0f, bufferSeconds);
        else
            bufferedInputTime = Math.Max(0f, bufferedInputTime - dt);

        switch (Current.Phase)
        {
            case JumpPhase.Ready:
                // 평소의 짧은 접지 이탈은 기존 턱 넘기에서 발생할 수 있습니다.
                // 승인된 점프만 시작하며 일반 낙하 기능은 추가하지 않습니다.
                if (!grounded)
                    return Current;
                if (pressed || HasBufferedInput)
                    BeginAnticipation(false);
                break;

            case JumpPhase.Anticipation:
                // 준비 도중 절벽을 벗어나면 공중에서 새 상승 힘을 만들지 않습니다.
                if (!grounded)
                    return EnterAirborne(verticalSpeed, takeoffSpeed, false);
                phaseTime += dt;
                if (phaseTime + TimeTolerance >= TakeoffTime)
                    return EnterAirborne(takeoffSpeed, takeoffSpeed, true);
                Current = new JumpFrame(JumpPhase.Anticipation, PreparationPoseTime());
                break;

            case JumpPhase.Airborne:
                phaseTime += dt;
                leftGround |= !grounded;
                // 이륙 직후의 접지 검사 여유 거리 때문에 즉시 착지하지 않도록 합니다.
                if (grounded && verticalSpeed <= .1f &&
                    (leftGround || phaseTime + TimeTolerance >= MinimumAirTime))
                {
                    phaseTime = 0f;
                    QueueLandingJump();
                    Current = new JumpFrame(JumpPhase.Landing, TouchdownTime);
                }
                else
                    Current = new JumpFrame(JumpPhase.Airborne, AirPoseTime(verticalSpeed));
                break;

            case JumpPhase.Landing:
                if (!grounded)
                    return EnterAirborne(verticalSpeed, takeoffSpeed, false);
                // 착지 눌림이 끝나기 전 입력만 압축 자세에서 다음 도약으로 이어 줍니다.
                // 이미 몸이 펴진 뒤의 입력은 갑자기 다시 누르지 않고 보관 시간만 적용합니다.
                if (phaseTime <= BufferedLandingImpact + TimeTolerance)
                    QueueLandingJump();
                phaseTime += dt;
                if (landingJumpQueued && phaseTime + TimeTolerance >= BufferedLandingImpact)
                    BeginAnticipation(true);
                else if (moving && phaseTime + TimeTolerance >= MovingLandingTime &&
                    phaseTime < ClipEnd - TouchdownTime &&
                    (!HasBufferedInput || Current.PoseTime <= TouchdownTime + MovingLandingTime + TimeTolerance))
                {
                    // 짧은 회복 중 새 입력도 0초 준비 자세로 건너뛰지 않습니다.
                    // 현재 착지 자세를 역으로 따라 이륙하며 기존 버퍼를 한 번만 소비합니다.
                    // 이미 펴진 뒤 이동과 점프를 함께 누르면 아래의 일반 회복을 기다립니다.
                    if (HasBufferedInput) BeginAnticipation(true, Current.PoseTime);
                    else Reset();
                }
                else if (phaseTime + TimeTolerance >= ClipEnd - TouchdownTime)
                {
                    if (HasBufferedInput) BeginAnticipation(false);
                    else Reset();
                }
                else
                    Current = new JumpFrame(JumpPhase.Landing, TouchdownTime + phaseTime);
                break;
        }

        return Current;
    }

    public void Reset()
    {
        CancelBufferedInput();
        phaseTime = 0f;
        initialSpeed = 1f;
        leftGround = false;
        reboundFlight = false;
        Current = new JumpFrame(JumpPhase.Ready, 0f);
    }

    /// <summary>물리 이동은 유지하고, 아직 이륙하지 않은 착지 예약만 취소합니다.</summary>
    public void CancelBufferedInput()
    {
        bufferedInputTime = 0f;
        landingJumpQueued = false;
        if (bufferedPreparation)
        {
            // 취소 직전의 본 자세를 그대로 유지하고 기존 착지 회복으로 돌아갑니다.
            phaseTime = Math.Max(0f, Current.PoseTime - TouchdownTime);
            Current = new JumpFrame(JumpPhase.Landing, TouchdownTime + phaseTime);
        }
        bufferedPreparation = false;
    }

    bool HasBufferedInput => bufferedInputTime > TimeTolerance;

    void QueueLandingJump()
    {
        if (!HasBufferedInput) return;
        bufferedInputTime = 0f;
        landingJumpQueued = true;
    }

    void BeginAnticipation(bool fromLanding, float landingPose = TouchdownTime + BufferedLandingImpact)
    {
        bufferedInputTime = 0f;
        landingJumpQueued = false;
        bufferedPreparation = fromLanding;
        preparationStartPose = landingPose;
        phaseTime = fromLanding ? BufferedPreparationTime : 0f;
        Current = new JumpFrame(JumpPhase.Anticipation, PreparationPoseTime());
    }

    float PreparationPoseTime()
    {
        if (!bufferedPreparation) return phaseTime;
        // 별개의 준비 자세로 건너뛰면 귀·손의 위치가 끊깁니다.
        // 같은 착지 곡선을 역으로 따라 압축에서 이륙 자세까지 이어 줍니다.
        float remaining = Math.Max(0f, TakeoffTime - phaseTime) /
            (TakeoffTime - BufferedPreparationTime);
        return TouchdownTime + (preparationStartPose - TouchdownTime) * remaining;
    }

    JumpFrame EnterAirborne(float verticalSpeed, float takeoffSpeed, bool takeOff)
    {
        reboundFlight = takeOff && bufferedPreparation;
        landingJumpQueued = false;
        bufferedPreparation = false;
        // 현재 준비에서 눌렀던 키를 다음 착지의 새 입력으로 재사용하지 않습니다.
        bufferedInputTime = 0f;
        phaseTime = 0f;
        initialSpeed = Math.Max(.001f, Math.Abs(takeoffSpeed));
        leftGround = !takeOff;
        Current = new JumpFrame(JumpPhase.Airborne,
            takeOff ? (reboundFlight ? TouchdownTime : TakeoffTime) : AirPoseTime(verticalSpeed), takeOff);
        return Current;
    }

    float AirPoseTime(float verticalSpeed)
    {
        float speedRatio = Math.Min(1f, Math.Abs(verticalSpeed) / initialSpeed);
        // 실제 속도가 0인 정점에서 연결되고, 장시간 낙하 중에는 착지 직전 자세를 유지합니다.
        // 재도약은 착지 직전 구간을 거꾸로 올라가므로 이륙·정점 모두 같은 본 자세로 연결됩니다.
        return verticalSpeed >= 0f && !reboundFlight
            ? ApexTime - (ApexTime - TakeoffTime) * speedRatio
            : ApexTime + (TouchdownTime - ApexTime) * speedRatio;
    }
}
