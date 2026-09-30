using Unity.Cinemachine;
using UnityEngine;

internal enum CatPlayerValidationKind { Idle, Walk, Run, Stop, Escape, Click, Orbit, Rise, Barrier, Descent, Air, StopRise, Reenable }

internal readonly struct CatPlayerValidationCase
{
    public readonly string name;
    public readonly Vector3 position, direction;
    public readonly float duration, settle;
    public readonly bool run;
    public readonly CatPlayerValidationKind kind;

    public CatPlayerValidationCase(string name, CatPlayerValidationKind kind, Vector3 position,
        Vector3 direction, float duration, bool run = false, float settle = 0.35f)
    {
        this.name = name; this.kind = kind; this.position = position; this.direction = direction;
        this.duration = duration; this.run = run; this.settle = settle;
    }
}

internal sealed class CatPlayerValidationSample
{
    public readonly Vector3 start, cameraStart;
    public readonly float yawStart;
    public Vector3 end;
    public float minY, maxY, maxSpeed, finalSpeed, yawChange, cameraTravel;
    public bool sawIdle, sawWalk, sawRun, sawUnlocked, stoppedDuringRise;
    public bool sawDisabled, sawReenabled;
    public string finalState;
    public CursorLockMode finalCursor;

    public CatPlayerValidationSample(Vector3 start, Vector3 cameraStart, float yawStart)
    {
        this.start = end = start; this.cameraStart = cameraStart; this.yawStart = yawStart;
        minY = maxY = start.y;
    }

    public void Observe(Rigidbody body, Camera camera, CinemachineOrbitalFollow orbit,
        AnimatorStateInfo state, bool active)
    {
        end = body.position;
        minY = Mathf.Min(minY, end.y); maxY = Mathf.Max(maxY, end.y);
        finalSpeed = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude;
        if (active) maxSpeed = Mathf.Max(maxSpeed, finalSpeed);
        finalState = state.IsName("Run") ? "Run" : state.IsName("Walk") ? "Walk" : state.IsName("Idle") ? "Idle" : "Unknown";
        sawIdle |= finalState == "Idle"; sawWalk |= finalState == "Walk"; sawRun |= finalState == "Run";
        finalCursor = Cursor.lockState; sawUnlocked |= finalCursor != CursorLockMode.Locked;
        yawChange = Mathf.Max(yawChange, Mathf.Abs(Mathf.DeltaAngle(yawStart, orbit.HorizontalAxis.Value)));
        cameraTravel = Mathf.Max(cameraTravel, Vector3.Distance(cameraStart, camera.transform.position));
    }
}

/// <summary>실제 맵 회귀 지점과 판정만 분리합니다. 위치는 현재 Texas_Station_Unity 배치 기준입니다.</summary>
internal static class CatPlayerValidationCases
{
    static readonly Vector3 Spawn = new Vector3(0, 0.15f, 0);
    internal static readonly CatPlayerValidationCase[] All = {
        new CatPlayerValidationCase("IdleAnimation", CatPlayerValidationKind.Idle, Spawn, Vector3.zero, 0.7f),
        new CatPlayerValidationCase("WalkAnimation", CatPlayerValidationKind.Walk, Spawn, Vector3.forward, 0.9f),
        new CatPlayerValidationCase("RunAnimation", CatPlayerValidationKind.Run, Spawn, Vector3.forward, 0.8f, true),
        new CatPlayerValidationCase("ReleaseStops", CatPlayerValidationKind.Stop, Spawn, Vector3.forward, 1.4f),
        new CatPlayerValidationCase("EscapeStops", CatPlayerValidationKind.Escape, Spawn, Vector3.forward, 1.5f),
        new CatPlayerValidationCase("ClickRelocks", CatPlayerValidationKind.Click, Spawn, Vector3.forward, 1.0f),
        new CatPlayerValidationCase("MouseOrbit", CatPlayerValidationKind.Orbit, Spawn, Vector3.zero, 1.2f),
        new CatPlayerValidationCase("ReenableWalk", CatPlayerValidationKind.Reenable, Spawn, Vector3.forward, 1.5f),
        new CatPlayerValidationCase("WalkCurb", CatPlayerValidationKind.Rise, new Vector3(16.6f,0,5), Vector3.left, 2.4f),
        new CatPlayerValidationCase("RunCurb", CatPlayerValidationKind.Rise, new Vector3(17.2f,0,5), Vector3.left, 1.5f, true),
        new CatPlayerValidationCase("DiagonalCurb", CatPlayerValidationKind.Rise, new Vector3(16.6f,0,7), new Vector3(-1,0,1), 2.7f),
        new CatPlayerValidationCase("PumpWalk", CatPlayerValidationKind.Rise, new Vector3(2.1f,0.15f,2.48f), Vector3.right, 1.3f),
        new CatPlayerValidationCase("PumpRun", CatPlayerValidationKind.Rise, new Vector3(2.1f,0.15f,2.48f), Vector3.right, 0.9f, true),
        new CatPlayerValidationCase("HighBarrier", CatPlayerValidationKind.Barrier, new Vector3(-11.5f,0.15f,11), Vector3.left, 1.6f),
        new CatPlayerValidationCase("LowClearance", CatPlayerValidationKind.Barrier, new Vector3(4.7f,0.15f,-12), Vector3.back, 1.5f),
        new CatPlayerValidationCase("Descent", CatPlayerValidationKind.Descent, new Vector3(14.4f,0.15f,5), Vector3.right, 1.8f),
        new CatPlayerValidationCase("AirApproach", CatPlayerValidationKind.Air, new Vector3(16.1f,0.8f,5), Vector3.left, 1.5f, false, 0.05f),
        new CatPlayerValidationCase("StopDuringRise", CatPlayerValidationKind.StopRise, new Vector3(16.0f,0,5), Vector3.left, 1.5f),
        new CatPlayerValidationCase("Idle", CatPlayerValidationKind.Idle, Spawn, Vector3.zero, 1f)
    };

    // 이 회귀 사례의 좌표가 여전히 같은 지형을 가리키는지 확인합니다.
    internal static bool HasExpectedMap(CatPlayerValidationCase test, Collider[] colliders)
    {
        if (!Matches(colliders, "Concrete_Forecourt.002", new Vector3(0, 0.12f, 0), 0.12f)) return false;
        if (test.name.StartsWith("Pump"))
            return Matches(colliders, "Pump_Island_4.002", new Vector3(3.6f, 0.36f, 2.48f), 0.36f);
        if (test.name == "HighBarrier")
            return Matches(colliders, "Pylon_Base.002", new Vector3(-12.4f, 0.6f, 11), 0.6f);
        if (test.name == "LowClearance")
            return Matches(colliders, "Shelf_Deck.016", new Vector3(4.7f, 0.325f, -13.4f), 0.325f) &&
                Matches(colliders, "Shelf_Deck.017", new Vector3(4.7f, 0.84f, -13.4f), 0.845f);
        return test.kind != CatPlayerValidationKind.Rise && test.kind != CatPlayerValidationKind.Descent ||
            Matches(colliders, "Asphalt_Lot.002", new Vector3(16.5f, -0.03f, 5), -0.03f);
    }

    static bool Matches(Collider[] colliders, string name, Vector3 point, float top)
    {
        foreach (var collider in colliders)
        {
            if (collider.name != name || !collider.enabled || !collider.gameObject.activeInHierarchy || collider.isTrigger) continue;
            Bounds bounds = collider.bounds;
            return Mathf.Abs(bounds.max.y - top) < 0.03f &&
                point.x >= bounds.min.x && point.x <= bounds.max.x && point.z >= bounds.min.z && point.z <= bounds.max.z;
        }
        return false;
    }

    internal static bool Evaluate(CatPlayerValidationCase test, CatPlayerValidationSample value,
        float walkSpeed, float runSpeed, CinemachineBrain brain, out string detail)
    {
        float distance = Vector3.ProjectOnPlane(value.end - value.start, Vector3.up).magnitude;
        float progress = Vector3.Dot(value.end - value.start, test.direction.normalized);
        bool stopped = value.finalSpeed < 0.08f && value.finalState == "Idle";
        bool passed;
        string expectation;
        switch (test.kind)
        {
            case CatPlayerValidationKind.Idle:
                expectation = "이동<0.05m, 속도<0.08, Idle";
                passed = distance < 0.05f && stopped; break;
            case CatPlayerValidationKind.Walk:
            case CatPlayerValidationKind.Run:
                float expectedSpeed = test.run ? runSpeed : walkSpeed;
                expectation = "이동>0.3m, 설정 속도 60% 이상, 해당 애니메이션";
                passed = progress > 0.3f && value.maxSpeed >= expectedSpeed * 0.6f &&
                    (test.run ? value.sawRun && value.finalState == "Run" : value.sawWalk && value.finalState == "Walk"); break;
            case CatPlayerValidationKind.Stop:
                expectation = "Walk 관측 후 입력 해제 시 Idle 및 속도<0.08";
                passed = value.sawWalk && distance > 0.2f && stopped; break;
            case CatPlayerValidationKind.Escape:
                expectation = "W 유지 중 Esc로 잠금 해제, Idle 및 속도<0.08";
                passed = value.sawWalk && value.sawUnlocked && value.finalCursor != CursorLockMode.Locked && stopped; break;
            case CatPlayerValidationKind.Click:
                expectation = "잠금 해제 상태에서 클릭 후 잠금 및 Walk 이동>0.3m";
                passed = value.sawUnlocked && value.finalCursor == CursorLockMode.Locked && value.sawWalk && progress > 0.3f; break;
            case CatPlayerValidationKind.Orbit:
                expectation = "마우스 yaw 변화>2도, 출력 카메라 이동>0.05m, Cinemachine 활성";
                passed = value.yawChange > 2 && value.cameraTravel > 0.05f && brain.ActiveVirtualCamera != null; break;
            case CatPlayerValidationKind.Reenable:
                expectation = "비활성화 후 재활성화, Walk 복귀 및 이동 재개";
                passed = value.sawDisabled && value.sawReenabled && value.finalState == "Walk" &&
                    value.finalSpeed > walkSpeed * 0.6f && progress > 0.3f; break;
            case CatPlayerValidationKind.Rise:
                bool pump = test.name.StartsWith("Pump");
                expectation = pump ? "턱 통과>1.2m, 도착 높이 0.30~0.42m" : "턱 통과>1.8m, 도착 높이 0.07~0.20m";
                passed = progress > (pump ? 1.2f : 1.8f) && value.end.y > (pump ? 0.30f : 0.07f) &&
                    value.end.y < (pump ? 0.42f : 0.20f) && value.maxY < (pump ? 0.45f : 0.22f); break;
            case CatPlayerValidationKind.Barrier:
                expectation = "벽 앞까지 이동 후 정지, 상승<0.04m, 최종 속도<0.15";
                passed = progress > 0.1f && progress < (test.name == "LowClearance" ? 1.25f : 0.85f) &&
                    value.maxY < test.position.y + 0.04f && value.finalSpeed < 0.15f; break;
            case CatPlayerValidationKind.Descent:
                expectation = "앞으로>1.8m, 낮은 지면 -0.08~0.03m에 도착";
                passed = progress > 1.8f && value.end.y > -0.08f && value.end.y < 0.03f; break;
            case CatPlayerValidationKind.Air:
                expectation = "공중 시작보다 높게 상승하지 않음, 낙하 후 지면 도착";
                passed = progress > 0.4f && value.maxY <= test.position.y + 0.02f && value.minY < 0.2f &&
                    value.end.y > -0.08f && value.end.y < 0.2f; break;
            default:
                expectation = "상승 중 입력 해제 후 높이<0.12m, Idle 및 속도<0.08";
                passed = value.stoppedDuringRise && value.maxY < 0.12f && stopped; break;
        }
        detail = $"{expectation}; 이동={distance:F3}, 진행={progress:F3}, Y={value.minY:F3}..{value.maxY:F3}, 도착Y={value.end.y:F3}, " +
            $"최대속도={value.maxSpeed:F3}, 최종속도={value.finalSpeed:F3}, 상태={value.finalState}, 커서={value.finalCursor}, yaw변화={value.yawChange:F2}, 카메라이동={value.cameraTravel:F3}";
        return passed;
    }
}
