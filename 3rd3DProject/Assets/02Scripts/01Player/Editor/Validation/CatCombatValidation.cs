using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>
/// 실제 PlaytestScene01의 이동·렌즈·손 자세·발사 경로를 Play에서 검사합니다.
/// 입력만 교체하며 장비·발사·애니메이션 구현을 대신하지 않습니다. 씬/에셋 저장은 하지 않습니다.
/// </summary>
public static class CatCombatValidation
{
    const string ScenePath = CatPlayerEditorScope.ScenePath;
    static readonly StringBuilder report = new StringBuilder();
    static readonly List<GameObject> temporaryObjects = new List<GameObject>();
    static CatShotgunCombat combat;
    static CatShotgunPose pose;
    static CatShotgunEquipment equipment;
    static CatPlayerMotor motor;
    static CatInteractionController interaction;
    static CatAimZoom zoom;
    static CatShotSpreadRing spreadRing;
    static CatCinemachineCursor cursor;
    static CinemachineInputAxisController cameraInput;
    static CinemachineCamera virtualCamera;
    static Camera view;
    static Rigidbody body;
    static Animator animator;
    static CombatInput input;
    static MotionInput movement;
    static NeutralHandInput handInput;
    static BoxCollider target, obstruction;
    static IEnumerator<Pause> cases;
    static Pause pause;
    static bool running, previousBackground, previousCursorEnabled, previousCameraInputEnabled;
    static bool previousCursorVisible, shotPrepared, sawPose, previousKinematic, previousMotorEnabled;
    static CursorLockMode previousCursorLock;
    static EditorWindow previousFocusedWindow;
    static Vector3 originalPosition;
    static Quaternion originalRotation;
    static float baseFov, largestSpread, averageDirectionDot, handError, firstOriginError, preparationSeconds;
    static Vector3 shotDirectionSum;
    static float shotRaise, shotElapsed, firstShotTime;
    static int checks, failures, shotEvents, pelletEvents, pelletInCurrentShot;
    static double started;
    static string logPath, capturePrefix;
    static CatShotHit firstHit;

    sealed class Pause
    {
        public readonly double Until;
        public readonly int Frame;
        public Pause(float seconds = .8f, int frames = 5)
        { Until = EditorApplication.timeSinceStartup + seconds; Frame = Time.frameCount + frames; }
    }

    sealed class CombatInput : ICatCombatInputSource
    {
        public bool enabled = true, aim;
        public bool pendingFire;
        public float lastPressTime = -1;
        public CatCombatInputFrame Read()
        {
            // 실제 FirePressed와 같이 요청마다 정확히 한 Update에서만 true입니다.
            bool fire = pendingFire;
            pendingFire = false;
            if (fire) lastPressTime = Time.time;
            return new CatCombatInputFrame(enabled, aim, fire);
        }
    }

    sealed class MotionInput : ICatPlayerInputSource
    {
        public Vector2 move;
        public bool sprint;
        public CatPlayerInputFrame Read() => new CatPlayerInputFrame(move, sprint);
    }

    sealed class NeutralHandInput : ICatInteractionInputSource
    {
        public CatHandInput Read() => new CatHandInput(true, false, false, false);
    }

    [MenuItem("Tools/Cat Player/Play 모드 조준 발사 검증")]
    public static void Start()
    {
        RequireScene();
        CatPlayerEditorScope.RequireValidationIdle();
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || running)
            throw new InvalidOperationException("PlaytestScene01의 일시정지하지 않은 새 Play 모드에서 실행하세요.");
        combat = CatPlayerEditorScope.FindSingle<CatShotgunCombat>();
        equipment = combat.equipment; pose = combat.pose; view = combat.view;
        motor = combat.GetComponent<CatPlayerMotor>(); body = combat.GetComponent<Rigidbody>();
        animator = combat.GetComponent<Animator>(); interaction = combat.GetComponent<CatInteractionController>();
        zoom = CatPlayerEditorScope.FindSingle<CatAimZoom>();
        spreadRing = CatPlayerEditorScope.FindAll<CatShotSpreadRing>().SingleOrDefault();
        virtualCamera = zoom.GetComponent<CinemachineCamera>();
        cursor = zoom.GetComponent<CatCinemachineCursor>();
        cameraInput = zoom.GetComponent<CinemachineInputAxisController>();
        if (!equipment || !pose || !pose.fireMotion || !view || !motor || !body || !animator || !interaction ||
            !virtualCamera || !cursor || !cameraInput || !combat.muzzle || !combat.guardOrigin || !equipment.IsReady)
            throw new InvalidOperationException("조준·발사·장비·이동·Cinemachine 연결이 완성되어야 합니다.");
        CatPlayerEditorScope.RequireOwned(combat, equipment, pose, view, motor, body, animator, interaction,
            zoom, virtualCamera, cursor, cameraInput, combat.muzzle, combat.guardOrigin,
            equipment.weaponRoot, equipment.motionRoot, equipment.leftHand, equipment.rightHand,
            equipment.leftGrip, equipment.rightGrip);
        if (spreadRing) CatPlayerEditorScope.RequireOwned(spreadRing);
        if (interaction.Held || (interaction.pickup && interaction.pickup.IsBusy))
            throw new InvalidOperationException("아이템을 들거나 획득하는 중에는 검증을 시작하지 않습니다.");
        string logs = CatPlayerEditorScope.OutputDirectory;
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        logPath = Path.Combine(logs, "CatCombatValidation_" + stamp + ".txt");
        capturePrefix = Path.Combine(logs, "combat_" + stamp + "_");
        report.Clear(); temporaryObjects.Clear();
        report.AppendLine("Cat combat integration validation " + DateTime.Now.ToString("s"));
        report.AppendLine("Scene: " + ScenePath + " | 실제 런타임 서비스, 교체 입력, 임시 바닥/표적, 씬·에셋 저장 없음");
        checks = failures = shotEvents = pelletEvents = pelletInCurrentShot = 0;
        shotPrepared = sawPose = false;
        previousBackground = Application.runInBackground;
        previousCursorLock = Cursor.lockState; previousCursorVisible = Cursor.visible;
        previousCursorEnabled = cursor.enabled; previousCameraInputEnabled = cameraInput.enabled;
        previousFocusedWindow = EditorWindow.focusedWindow;
        previousKinematic = body.isKinematic;
        previousMotorEnabled = motor.enabled;
        originalPosition = body.position; originalRotation = body.rotation;
        baseFov = zoom.CurrentFieldOfView;
        input = new CombatInput(); movement = new MotionInput(); handInput = new NeutralHandInput();
        CatPlayerEditorScope.BeginValidation(nameof(CatCombatValidation));
        running = true;
        try
        {
            started = EditorApplication.timeSinceStartup;
            Application.runInBackground = true;
            cursor.enabled = false; cameraInput.enabled = false;
            Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
            equipment.Unequip(); combat.SetInputSource(input); motor.SetInputSource(movement);
            interaction.SetInputSource(handInput);
            combat.PelletResolved += OnPellet; combat.ShotFired += OnShot;
            equipment.PoseApplied += OnPose;
            float bottomOffset = combat.GetComponent<CapsuleCollider>().bounds.min.y - body.position.y;
            body.position = new Vector3(1000, 10, 1000); body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            combat.transform.position = body.position;
            var floor = CreateBox("CombatValidation_Floor", new Vector3(1000, 10 + bottomOffset - .27f, 1000),
                Quaternion.identity, new Vector3(120, .5f, 120));
            floor.gameObject.hideFlags = HideFlags.DontSave;
            virtualCamera.PreviousStateIsValid = false;
            Physics.SyncTransforms();
            var gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameView != null) EditorWindow.GetWindow(gameView).Focus();
            InputSystem.onAfterUpdate += Pump;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += Mode;
            AssemblyReloadEvents.beforeAssemblyReload += Reload;
            cases = Cases(); pause = new Pause(1f, 8);
            Flush();
        }
        catch (Exception exception) { Finish("FAIL 시작 오류: " + exception); }
    }

    [MenuItem("Tools/Cat Player/조준 발사 검증 중단")]
    public static void Stop() => Finish("검증 중단: 남은 사례는 통과하지 않았습니다.");
    [MenuItem("Tools/Cat Player/조준 발사 검증 중단", true)]
    static bool CanStop() => running;

    static void RequireScene() => CatPlayerEditorScope.RequireScene();

    static void Pump()
    {
        if (!running || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        // 키보드/마우스 장치 값은 바꾸지 않습니다. 운영체제의 자동 커서 해제만 테스트 동안 고정합니다.
        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false;
    }

    static void Tick()
    {
        if (!running) return;
        try
        {
            RequireScene();
            if (!EditorApplication.isPlaying) { Finish("검증 중단: Play 종료."); return; }
            if (EditorApplication.timeSinceStartup - started > 90)
                throw new TimeoutException("90초 안에 검증이 끝나지 않았습니다. 실제 게임 프레임/포커스를 확인하세요.");
            if (EditorApplication.timeSinceStartup < pause.Until || Time.frameCount < pause.Frame) return;
            if (cases.MoveNext()) pause = cases.Current;
            else Finish(null);
        }
        catch (Exception exception) { Finish("FAIL 검증 오류: " + exception); }
    }

    static IEnumerator<Pause> Cases()
    {
        Check(combat.isActiveAndEnabled && pose.isActiveAndEnabled && motor.isActiveAndEnabled &&
            equipment.aimingPose == pose && motor.aimSource == combat && zoom.aimSource == combat,
            "실제 전투·자세·이동·렌즈가 같은 조준 상태를 참조");
        Check(interaction.HasFreeHands && !combat.IsAiming && !pose.IsShotPlaying,
            "빈손 경로 유지: 상호작용 가능, 줌/발사 자세 없음");
        Check(equipment.TryEquip(), "기존 샷건과 양손 연결로 장착 성공");
        yield return new Pause();
        Check(equipment.IsEquipped && !interaction.HasFreeHands && !interaction.TryPickup(),
            "장착 중 빈손 집기 거부, 상호작용 경로와 충돌 없음");
        Check(Mathf.Abs(baseFov - 60) < .1f && Mathf.Abs(view.fieldOfView - baseFov) < .15f,
            $"비조준 실제 Camera FOV={view.fieldOfView:F3}, 초기 Cinemachine FOV={baseFov:F3}");

        movement.move = Vector2.up; movement.sprint = true;
        yield return new Pause(1.1f, 8);
        Check(HorizontalSpeed() > motor.walkSpeed + .25f && IsAnimation("Run"),
            $"비조준 Shift 달리기: speed={HorizontalSpeed():F3}, run={motor.runSpeed:F3}, state={AnimationName()}");
        input.aim = true;
        yield return new Pause(1.1f, 8);
        Check(combat.IsAiming && Mathf.Abs(zoom.CurrentFieldOfView - zoom.aimedFieldOfView) < .15f &&
            Mathf.Abs(view.fieldOfView - zoom.aimedFieldOfView) < .15f,
            $"우클릭 유지 줌: actual={view.fieldOfView:F3}, lens={zoom.CurrentFieldOfView:F3}, target={zoom.aimedFieldOfView:F3}");
        Check(HorizontalSpeed() <= motor.walkSpeed + .06f && HorizontalSpeed() > motor.walkSpeed - .12f && IsAnimation("Walk"),
            $"조준+Shift는 걷기 고정: speed={HorizontalSpeed():F3}, walk={motor.walkSpeed:F3}, state={AnimationName()}");
        movement.move = Vector2.zero;
        yield return new Pause(.9f, 8);
        Check(combat.IsAiming && HorizontalSpeed() < .05f && IsAnimation("Idle"),
            $"조준 중 정지하면 Idle 유지: speed={HorizontalSpeed():F3}, state={AnimationName()}");
        PlaceTarget();
        yield return new Pause(.9f, 8);
        float aimedRingRadius = spreadRing ? spreadRing.RadiusPixels : 0f;
        Check(spreadRing && spreadRing.IsVisible && spreadRing.stateSource == combat &&
            Mathf.Abs(aimedRingRadius - ExpectedRingRadius()) <= .3f,
            $"조준 산탄 원 표시·투영 반경 일치: actual={aimedRingRadius:F3}px, expected={ExpectedRingRadius():F3}px");
        Capture("aim");

        int before = combat.ShotsFired;
        input.pendingFire = true;
        yield return new Pause(.30f, 5);
        Check(combat.ShotsFired == before + 1 && shotEvents == 1,
            $"조준 중 한 번 누름은 한 발: shots={combat.ShotsFired - before}, events={shotEvents}");
        Check(shotPrepared && preparationSeconds >= pose.fireMoment - .025f && shotRaise > .98f,
            $"손을 앞으로 들고 난 뒤 발사: delay={preparationSeconds:F4}s, clipTime={shotElapsed:F4}s, raise={shotRaise:F4}");
        Check(firstHit.Kind == CatShotHitKind.Hit && firstHit.Collider == target,
            $"화면 중앙 10m 표적에 산탄 명중: kind={firstHit.Kind}, collider={firstHit.Collider?.name}");
        Check(largestSpread <= combat.CurrentSpreadDegrees + .1f && averageDirectionDot > .9995f && firstOriginError < .001f,
            $"산탄이 실제 총구 방향 주위로 고르게 출발: maxSpread={largestSpread:F4}deg, meanDirectionDot={averageDirectionDot:F6}, originError={firstOriginError:F6}m");
        Check(pelletEvents == 4 && combat.pelletCount == 4,
            $"한 발의 산탄 수 일치: actual={pelletEvents}, configured={combat.pelletCount}");
        float measuredAimSpread = largestSpread;
        Check(sawPose && handError < .003f,
            $"발사·조준 양손이 실제 손잡이 표시에 붙음: maximum latest grip error={handError:F6}m");
        // 첫 발 클립 재생 중/쿨다운 안에 다시 누르면 준비 동작이나 추가 발사를 재시작하지 않습니다.
        float retryAt = Time.time;
        input.pendingFire = true;
        yield return new Pause(.35f, 5);
        Check(retryAt - firstShotTime < combat.shotInterval && combat.ShotsFired == before + 1,
            $"쿨다운/재생 중 연타 거부: retryAfterShot={retryAt - firstShotTime:F4}s, interval={combat.shotInterval:F3}s, shots={combat.ShotsFired - before}");
        yield return new Pause(.9f, 8);
        Check(combat.ShotsFired == before + 1 && !pose.IsShotPlaying,
            "새 FirePressed 없이 버튼 유지에 해당하는 시간 경과만으로 추가 발사하지 않음");

        input.aim = false; movement.move = Vector2.up;
        yield return new Pause(1.1f, 8);
        Check(!combat.IsAiming && Mathf.Abs(view.fieldOfView - baseFov) < .15f,
            $"우클릭 해제 후 기본 FOV 복귀: actual={view.fieldOfView:F3}, original={baseFov:F3}");
        Check(HorizontalSpeed() > motor.walkSpeed + .25f && IsAnimation("Run"),
            $"줌 해제 후 누르고 있던 Shift 달리기 복귀: speed={HorizontalSpeed():F3}, state={AnimationName()}");
        movement.move = Vector2.zero;
        yield return new Pause(.9f, 8);
        PlaceTarget();
        yield return new Pause(.9f, 8);
        float hipRingRadius = spreadRing ? spreadRing.RadiusPixels : 0f;
        Check(spreadRing && spreadRing.IsVisible && hipRingRadius > aimedRingRadius &&
            Mathf.Abs(hipRingRadius - ExpectedRingRadius()) <= .3f,
            $"비조준 산탄 원이 조준보다 크고 투영 반경 일치: hip={hipRingRadius:F3}px, aim={aimedRingRadius:F3}px, expected={ExpectedRingRadius():F3}px");
        Capture("hip");
        before = combat.ShotsFired;
        input.pendingFire = true;
        yield return new Pause(.9f, 8);
        Check(!combat.IsAiming && combat.ShotsFired == before + 1 && firstHit.Kind == CatShotHitKind.Hit && firstHit.Collider == target,
            $"비조준 발사도 중앙 표적 명중: shots={combat.ShotsFired - before}, kind={firstHit.Kind}, collider={firstHit.Collider?.name}");
        Check(largestSpread > measuredAimSpread + .1f && largestSpread <= combat.CurrentSpreadDegrees + .1f,
            $"줌의 실제 산탄 퍼짐이 비조준보다 좁음: aim={measuredAimSpread:F4}deg, hip={largestSpread:F4}deg");

        input.aim = true;
        yield return new Pause(.9f, 8);
        // 이동 검증 이후만 정지시켜 임시 벽이 캐릭터를 밀지 않게 합니다. 발사/자세 서비스는 계속 실행합니다.
        motor.enabled = false;
        body.isKinematic = true;
        obstruction = CreateBox("CombatValidation_GuardWall", combat.guardOrigin.position,
            Quaternion.identity, Vector3.one * .12f);
        Physics.SyncTransforms();
        before = combat.ShotsFired;
        input.pendingFire = true;
        yield return new Pause(.9f, 8);
        Check(combat.ShotsFired == before + 1 && firstHit.Kind == CatShotHitKind.Blocked && firstHit.Collider == obstruction,
            $"가슴→총구 경로가 벽 안이면 먼 표적 관통 차단: kind={firstHit.Kind}, collider={firstHit.Collider?.name}");
        obstruction.enabled = false;
        yield return new Pause(.9f, 8);
        // 총구 내부 출발은 일반 Raycast가 놓칠 수 있으므로 실제 총구 주변을 덮어 따로 검사합니다.
        obstruction.transform.position = combat.muzzle.position;
        obstruction.size = Vector3.one * .35f; obstruction.enabled = true;
        Physics.SyncTransforms();
        before = combat.ShotsFired;
        input.pendingFire = true;
        yield return new Pause(.9f, 8);
        Check(combat.ShotsFired == before + 1 && firstHit.Kind == CatShotHitKind.Blocked && firstHit.Collider == obstruction,
            $"총구가 벽 안에 겹쳐도 관통하지 않음: kind={firstHit.Kind}, collider={firstHit.Collider?.name}");
        obstruction.enabled = false; Physics.SyncTransforms();

        // 카메라 조준점이 총구보다 뒤에 생기는 밀착 벽: 총을 뒤집거나 발사 준비가 무한 대기하면 안 됩니다.
        obstruction.transform.SetPositionAndRotation(body.position + combat.transform.forward * .3f + Vector3.up * .8f,
            Quaternion.LookRotation(combat.transform.forward, Vector3.up));
        obstruction.size = new Vector3(4, 4, .1f); obstruction.enabled = true;
        Physics.SyncTransforms();
        var nearWallQuery = new CatHitscanQuery(combat.transform);
        bool resolvedNearWall = nearWallQuery.TryGetAimPoint(view, combat.range, combat.hitLayers, out Vector3 nearPoint);
        float alongBarrel = Vector3.Dot(nearPoint - combat.muzzle.position, view.transform.forward);
        Check(resolvedNearWall && alongBarrel < 0,
            $"밀착 벽 검증 전제: 카메라 조준점이 실제 총구보다 뒤쪽, distance={alongBarrel:F4}m");
        before = combat.ShotsFired;
        input.pendingFire = true;
        yield return new Pause(1.1f, 10);
        Check(combat.ShotsFired == before + 1 && !pose.IsShotPlaying && firstHit.Collider == obstruction &&
            (firstHit.Kind == CatShotHitKind.Blocked || firstHit.Kind == CatShotHitKind.Hit) &&
            Vector3.Dot(combat.muzzle.forward, view.transform.forward) > .5f,
            $"밀착 벽에서도 총구 전방 유지·발사 종료·벽 차단: shots={combat.ShotsFired - before}, playing={pose.IsShotPlaying}, kind={firstHit.Kind}, forwardDot={Vector3.Dot(combat.muzzle.forward, view.transform.forward):F4}");
        obstruction.enabled = false; Physics.SyncTransforms();

        before = combat.ShotsFired;
        input.enabled = false; input.pendingFire = true;
        yield return new Pause(.9f, 8);
        Check(!combat.IsAiming && !pose.IsShotPlaying && combat.ShotsFired == before && Mathf.Abs(view.fieldOfView - baseFov) < .15f,
            "입력 비활성화 시 조준·준비 취소, 발사 없음, 렌즈 복귀");
        input.enabled = true; input.aim = true;
        yield return new Pause(.9f, 8);
        Check(combat.IsAiming && combat.ShotsFired == before,
            "입력 복귀만으로 발사하지 않고 조준만 복귀");
        equipment.Unequip(); input.pendingFire = true;
        yield return new Pause(.9f, 8);
        Check(!equipment.IsEquipped && !combat.IsAiming && !pose.IsShotPlaying && combat.ShotsFired == before &&
            interaction.HasFreeHands && Mathf.Abs(view.fieldOfView - baseFov) < .15f,
            "장비 해제 시 발사/줌 취소, 기본 렌즈와 기존 빈손 경로 복구");
        Check(spreadRing && !spreadRing.IsVisible && spreadRing.RadiusPixels == 0f,
            "장비 해제 시 산탄 원 숨김·표시 반경 0");
        Check(shotEvents == combat.ShotsFired && pelletEvents == shotEvents * combat.pelletCount,
            $"종료 이벤트 누락/중복 없음: shots={combat.ShotsFired}, shotEvents={shotEvents}, pellets={pelletEvents}");
    }

    static void PlaceTarget()
    {
        Ray ray = view.ViewportPointToRay(new Vector3(.5f, .5f, 0));
        if (!target) target = CreateBox("CombatValidation_Target", ray.GetPoint(10),
            Quaternion.LookRotation(ray.direction), new Vector3(4, 4, .2f));
        else target.transform.SetPositionAndRotation(ray.GetPoint(10), Quaternion.LookRotation(ray.direction));
        Physics.SyncTransforms();
    }

    static BoxCollider CreateBox(string name, Vector3 position, Quaternion rotation, Vector3 size)
    {
        var created = new GameObject(name) { hideFlags = HideFlags.DontSave };
        temporaryObjects.Add(created);
        created.transform.SetPositionAndRotation(position, rotation);
        var collider = created.AddComponent<BoxCollider>(); collider.size = size;
        return collider;
    }

    static void OnPellet(CatShotHit hit)
    {
        pelletEvents++;
        if (pelletInCurrentShot == 0) { shotDirectionSum = Vector3.zero; largestSpread = 0; }
        shotDirectionSum += hit.Direction.normalized;
        largestSpread = Mathf.Max(largestSpread, Vector3.Angle(hit.Direction, combat.muzzle.forward));
        if (pelletInCurrentShot++ != 0) return;
        firstHit = hit;
        firstOriginError = Vector3.Distance(hit.Origin, combat.muzzle.position);
        shotPrepared = pose.CurrentRaise > .98f && pose.Elapsed >= pose.fireMoment;
        shotRaise = pose.CurrentRaise; shotElapsed = pose.Elapsed;
        preparationSeconds = Time.time - input.lastPressTime;
        if (shotEvents == 0) firstShotTime = Time.time;
    }

    static void OnShot(int count)
    {
        averageDirectionDot = Vector3.Dot(shotDirectionSum.normalized, combat.muzzle.forward);
        shotEvents++; pelletInCurrentShot = 0;
    }
    static void OnPose()
    {
        if (!running || !equipment.IsEquipped) return;
        sawPose = true;
        handError = Mathf.Max(Vector3.Distance(equipment.leftHand.position, equipment.leftGrip.position),
            Vector3.Distance(equipment.rightHand.position, equipment.rightGrip.position));
    }
    static float HorizontalSpeed() => new Vector2(body.linearVelocity.x, body.linearVelocity.z).magnitude;
    static float ExpectedRingRadius() => Mathf.Tan(combat.CurrentSpreadDegrees * Mathf.Deg2Rad) /
        Mathf.Tan(view.fieldOfView * Mathf.Deg2Rad * .5f) * view.pixelHeight * .5f;
    static bool IsAnimation(string state) => animator.GetCurrentAnimatorStateInfo(0).IsName(state);
    static string AnimationName() => IsAnimation("Idle") ? "Idle" : IsAnimation("Walk") ? "Walk" : IsAnimation("Run") ? "Run" : "Other/transition";

    static void Capture(string name)
    {
        string path = capturePrefix + name + ".png";
        var oldTarget = view.targetTexture;
        var oldActive = RenderTexture.active;
        var render = RenderTexture.GetTemporary(900, 650, 24, RenderTextureFormat.ARGB32);
        Texture2D pixels = null;
        try
        {
            view.targetTexture = render; view.Render(); RenderTexture.active = render;
            pixels = new Texture2D(900, 650, TextureFormat.RGB24, false);
            pixels.ReadPixels(new Rect(0, 0, 900, 650), 0, 0); pixels.Apply();
            File.WriteAllBytes(path, pixels.EncodeToPNG());
            report.AppendLine("CAPTURE " + path + " (파일 생성, 시각 판정은 별도)");
        }
        catch (Exception exception) { report.AppendLine("CAPTURE UNAVAILABLE " + exception.Message); }
        finally
        {
            view.targetTexture = oldTarget; RenderTexture.active = oldActive;
            if (pixels) UnityEngine.Object.DestroyImmediate(pixels);
            RenderTexture.ReleaseTemporary(render); Flush();
        }
        // Graphics.DrawMesh로 그리는 산탄 원 등 실제 프레임 표시도 함께 남깁니다.
        string gamePath = capturePrefix + name + "_game.png";
        ScreenCapture.CaptureScreenshot(gamePath);
        report.AppendLine("GAME CAPTURE " + gamePath + " (다음 Game 렌더 프레임 저장 예약, 시각 확인 별도)");
        Flush();
    }

    static void Check(bool passed, string message)
    {
        checks++; if (!passed) failures++;
        report.AppendLine((passed ? "PASS " : "FAIL ") + message);
        if (!passed) report.AppendLine($"  player={body.position:F4}, aim={combat.IsAiming}, shot={pose.IsShotPlaying}, poseElapsed={pose.Elapsed:F4}, lastKind={firstHit.Kind}");
        Flush();
    }
    static void Flush() => File.WriteAllText(logPath, report.ToString());
    static void Mode(PlayModeStateChange mode) { if (mode == PlayModeStateChange.ExitingPlayMode) Finish("검증 중단: Play 종료."); }
    static void Reload() => Finish("검증 중단: 스크립트 재컴파일.");

    static void Finish(string interruption)
    {
        if (!running) return;
        running = false;
        try
        {
            EditorApplication.update -= Tick; InputSystem.onAfterUpdate -= Pump;
            EditorApplication.playModeStateChanged -= Mode; AssemblyReloadEvents.beforeAssemblyReload -= Reload;
            if (interruption != null) { failures++; report.AppendLine(interruption); }
            try
            {
                if (combat) { combat.PelletResolved -= OnPellet; combat.ShotFired -= OnShot; combat.SetInputSource(null); }
                if (equipment) { equipment.PoseApplied -= OnPose; equipment.Unequip(); }
                if (motor) { motor.SetInputSource(null); motor.enabled = previousMotorEnabled; }
                if (interaction) interaction.SetInputSource(null);
                if (body)
                {
                    body.isKinematic = previousKinematic;
                    if (!body.isKinematic) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; }
                    body.position = originalPosition; body.rotation = originalRotation;
                }
            }
            catch (Exception exception) { failures++; report.AppendLine("FAIL 정리 오류: " + exception); }
            finally
            {
                // 이 검증기가 만든 Play 전용 객체만 제거합니다. 원래 씬 객체/파일은 삭제하지 않습니다.
                foreach (var created in temporaryObjects) if (created) UnityEngine.Object.DestroyImmediate(created);
                temporaryObjects.Clear();
                if (cursor) cursor.enabled = previousCursorEnabled;
                if (cameraInput) cameraInput.enabled = previousCameraInputEnabled;
                Application.runInBackground = previousBackground;
                Cursor.lockState = previousCursorLock; Cursor.visible = previousCursorVisible;
                cases?.Dispose(); cases = null;
                report.AppendLine($"RESULT checks={checks}, failures={failures}, interrupted={interruption != null}");
                Flush();
                Debug.Log($"[Cat Combat] 검사 {checks}, 실패 {failures}. {logPath}");
                if (previousFocusedWindow) previousFocusedWindow.Focus();
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
        }
        finally { CatPlayerEditorScope.EndValidation(nameof(CatCombatValidation)); }
    }
}
