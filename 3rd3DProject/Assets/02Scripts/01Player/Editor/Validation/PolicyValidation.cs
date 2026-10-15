using System;
using System.Text;

/// <summary>씬이나 Unity 객체 없이 실행하는 규칙 검증. 일반 C# 실행 파일로도 빌드할 수 있습니다.</summary>
public static class PolicyValidation
{
#if UNITY_EDITOR
    [UnityEditor.MenuItem("Tools/Cat Player/순수 규칙 검증")]
    static void RunMenu() => UnityEngine.Debug.Log(Run());
#else
    public static void Main()
    {
        Console.WriteLine(Run());
        Console.WriteLine("ASSEMBLY REFERENCES (Unity DLL 없이 빌드)");
        foreach (var reference in typeof(PolicyValidation).Assembly.GetReferencedAssemblies())
            Console.WriteLine(reference.FullName);
    }
#endif

    public static string Run()
    {
        var report = new StringBuilder("Cat Player 순수 규칙 검증\n");
        int checks = 0;
        int failures = 0;
        RunMovement(report, ref checks, ref failures);
        RunHands(report, ref checks, ref failures);
        RunOrbit(report, ref checks, ref failures);
        RunCombat(report, ref checks, ref failures);
        report.AppendLine($"RESULT checks={checks}, failures={failures}");
        if (failures != 0) throw new InvalidOperationException(report.ToString());
        return report.ToString();
    }

    static void RunCombat(StringBuilder report, ref int checks, ref int failures)
    {
        var policy = new CombatPolicy();
        Check(!policy.TryRequest(false, true, true, 0, .55f) && !policy.TryRequest(true, false, true, 0, .55f),
            "메뉴·미장착 상태 발사 금지", report, ref checks, ref failures);
        Check(policy.TryRequest(true, true, true, 0, .55f), "첫 누름 허용", report, ref checks, ref failures);
        Check(!policy.TryRequest(true, true, true, .54f, .55f) && policy.TryRequest(true, true, true, .55f, .55f),
            "발사 간격 경계", report, ref checks, ref failures);
        Check(!policy.TryRequest(true, true, false, 2, .55f), "누름 유지로 새 발사 없음", report, ref checks, ref failures);
        Check(!policy.TryRequest(true, true, true, float.NaN, .55f), "비정상 발사 시각 거부", report, ref checks, ref failures);
        var sprint = new MovementIntent(.5f, .5f, true);
        var aim = CombatPolicy.RestrictMovement(sprint, true);
        Check(!aim.Sprint && aim.Horizontal == sprint.Horizontal && aim.Forward == sprint.Forward &&
            MovementPolicy.SelectAnimation(aim) == MovementState.Walk, "조준 중 이동 방향 보존·걷기 제한", report, ref checks, ref failures);
        Check(MovementPolicy.SelectAnimation(CombatPolicy.RestrictMovement(default, true)) == MovementState.Idle,
            "조준 중 가만히 있으면 대기", report, ref checks, ref failures);
        Check(CombatPolicy.RestrictMovement(sprint, false).Sprint, "조준 해제 시 달리기 의도 복귀", report, ref checks, ref failures);
    }

    static void RunMovement(StringBuilder report, ref int checks, ref int failures)
    {
        var settings = new MovementSettings(1.6f, 3.4f, 18f, 720f);
        Check(MovementPolicy.SelectAnimation(default) == MovementState.Idle,
            "초기 입력은 대기", report, ref checks, ref failures);
        Check(MovementPolicy.SelectAnimation(new MovementIntent(0f, 0f, true)) == MovementState.Idle,
            "정지 중 달리기 버튼만 눌러도 대기", report, ref checks, ref failures);

        var small = new MovementIntent(0.099f, 0f, true);
        var boundary = new MovementIntent(0.1f, 0f, false);
        Check(!small.IsMoving && boundary.IsMoving
            && MovementPolicy.SelectAnimation(small) == MovementState.Idle
            && MovementPolicy.SelectAnimation(boundary) == MovementState.Walk,
            "이동 판정 임계값 아래·경계 구분", report, ref checks, ref failures);

        bool directionsAgree = true;
        foreach (var intent in new[]
        {
            new MovementIntent(1f, 0f, false), new MovementIntent(-1f, 0f, false),
            new MovementIntent(0f, 1f, false), new MovementIntent(0f, -1f, false),
            new MovementIntent(-0.7071067f, 0.7071067f, false)
        })
        {
            directionsAgree &= MovementPolicy.SelectAnimation(intent) == MovementState.Walk
                && MovementPolicy.SelectSpeed(intent, settings) == settings.WalkSpeed;
        }
        Check(directionsAgree, "전후좌우·대각선의 걷기 규칙 일치", report, ref checks, ref failures);

        var running = new MovementIntent(0f, -1f, true);
        Check(MovementPolicy.SelectAnimation(running) == MovementState.Run
            && MovementPolicy.SelectSpeed(running, settings) == settings.RunSpeed,
            "후진 중에도 달리기 설정 적용", report, ref checks, ref failures);
        var changed = new MovementSettings(2.1f, 4.8f, 18f, 720f);
        Check(MovementPolicy.SelectSpeed(running, changed) == 4.8f
            && MovementPolicy.SelectSpeed(new MovementIntent(0f, 1f, false), changed) == 2.1f,
            "설정 변경에 과거 속도 캐시가 남지 않음", report, ref checks, ref failures);
    }

    static void RunHands(StringBuilder report, ref int checks, ref int failures)
    {
        var empty = new HandState(false, false, false, true);
        var carrying = new HandState(true, false, false, true);
        var equipped = new HandState(false, true, false, true);
        var busy = new HandState(false, false, true, true);
        var noEquipment = new HandState(false, false, false, false);
        var press = new HandInput(true, true, true, false);
        var hold = new HandInput(true, false, true, false);
        var release = new HandInput(true, false, false, false);
        var toggle = new HandInput(true, false, false, true);

        Check(HandPolicy.Decide(press, empty) == HandCommand.Pickup,
            "빈손 집기 버튼 누름은 획득 요청", report, ref checks, ref failures);
        Check(HandPolicy.Decide(hold, empty) == HandCommand.None,
            "계속 누른 입력으로 자동 재획득하지 않음", report, ref checks, ref failures);
        Check(HandPolicy.Decide(new HandInput(true, true, false, false), empty) == HandCommand.None,
            "같은 갱신에서 이미 뗀 누름은 획득하지 않음", report, ref checks, ref failures);
        Check(HandPolicy.Decide(hold, carrying) == HandCommand.None,
            "집기 버튼 유지 중에는 계속 보유", report, ref checks, ref failures);
        Check(HandPolicy.Decide(release, carrying) == HandCommand.Release,
            "집기 버튼을 떼면 안전한 놓기 요청", report, ref checks, ref failures);
        var unlocked = new HandInput(false, true, true, true);
        Check(HandPolicy.Decide(unlocked, carrying) == HandCommand.Release
            && HandPolicy.Decide(unlocked, empty) == HandCommand.None,
            "입력 잠금 해제 시 놓기만 허용", report, ref checks, ref failures);
        Check(HandPolicy.Decide(press, equipped) == HandCommand.None,
            "장비를 든 동안 물건 획득 차단", report, ref checks, ref failures);
        Check(HandPolicy.Decide(press, busy) == HandCommand.None
            && HandPolicy.Decide(toggle, busy) == HandCommand.None,
            "수집 연출 중 획득·장착 차단", report, ref checks, ref failures);
        Check(HandPolicy.Decide(toggle, noEquipment) == HandCommand.None,
            "장비 연결 미준비 시 장착하지 않음", report, ref checks, ref failures);
        Check(HandPolicy.Decide(toggle, carrying) == HandCommand.Equip,
            "동시 손 떼기·장착은 안전하게 놓은 뒤 장착할 요청 우선", report, ref checks, ref failures);
        Check(HandPolicy.Decide(new HandInput(true, true, true, true), empty) == HandCommand.Equip,
            "동시 획득·장착 입력은 장착 우선", report, ref checks, ref failures);
        Check(HandPolicy.Decide(toggle, equipped) == HandCommand.Unequip
            && HandPolicy.Decide(toggle, new HandState(false, true, true, false)) == HandCommand.Unequip,
            "이미 장착했으면 토글로 해제", report, ref checks, ref failures);
        Check(HandPolicy.Decide(release, new HandState(true, false, true, true)) == HandCommand.Release,
            "수집 연출 차단 중에도 손을 떼면 놓기 허용", report, ref checks, ref failures);
    }

    static void RunOrbit(StringBuilder report, ref int checks, ref int failures)
    {
        var policy = new OrbitPolicy();
        var initial = policy.Evaluate(false, 30f, 70f);
        Check(initial.Value == 30f && initial.Center == 30f && initial.Minimum == -40f
            && initial.Maximum == 100f && !initial.Wrap,
            "정지 시작 방향을 기준으로 좌우 70도 범위", report, ref checks, ref failures);
        var right = policy.Evaluate(false, 170f, 70f);
        var left = policy.Evaluate(false, -100f, 70f);
        Check(right.Value == 100f && left.Value == -40f && right.Center == left.Center,
            "정지 중 양쪽 제한에 닿아도 기준 방향 유지", report, ref checks, ref failures);
        var moving = policy.Evaluate(true, 550f, 70f);
        Check(moving.Value == -170f && moving.Minimum == -180f && moving.Maximum == 180f && moving.Wrap,
            "이동 중에는 수평 회전이 360도 순환", report, ref checks, ref failures);
        var stopped = policy.Evaluate(false, -170f, 70f);
        var seam = policy.Evaluate(false, 170f, 70f);
        Check(stopped.Center == -170f && seam.Value == -190f && seam.Center == -170f && !seam.Wrap,
            "정지 범위가 ±180도 경계를 건너도 회전 연속성 유지", report, ref checks, ref failures);
        policy.Evaluate(true, 80f, 70f);
        var restopped = policy.Evaluate(false, 100f, 70f);
        Check(restopped.Center == 100f && restopped.Minimum == 30f && restopped.Maximum == 170f,
            "재이동 후 멈추면 새로운 정지 방향 기록", report, ref checks, ref failures);
        var zero = policy.Evaluate(false, 130f, 0f);
        var negative = policy.Evaluate(false, 70f, -10f);
        Check(zero.Value == 100f && negative.Value == 100f,
            "0도·음수 허용 범위는 정지 방향 고정", report, ref checks, ref failures);
        var wide = policy.Evaluate(false, 120f, 300f);
        Check(wide.Minimum == -80f && wide.Maximum == 280f,
            "과도한 허용 각도는 한 바퀴 범위로 제한", report, ref checks, ref failures);
        policy.Reset();
        var reset = policy.Evaluate(false, -45f, 70f);
        Check(reset.Center == -45f && reset.Minimum == -115f && reset.Maximum == 25f,
            "초기화 후 이전 정지 방향이 남지 않음", report, ref checks, ref failures);
    }

    static void Check(bool passed, string name, StringBuilder report, ref int checks, ref int failures)
    {
        checks++;
        if (!passed) failures++;
        report.AppendLine((passed ? "PASS " : "FAIL ") + name);
    }
}
