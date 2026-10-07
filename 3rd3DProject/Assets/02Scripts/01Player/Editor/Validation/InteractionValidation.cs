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
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>실제 입력으로 현재 상호작용·장비·카메라를 검사합니다. 임시 변경은 Play 종료와 함께 사라집니다.</summary>
public static class InteractionValidation
{
    const string ScenePath = PlayerEditorScope.ScenePath;
    static readonly StringBuilder report = new StringBuilder();
    static InteractionController control;
    static InventoryPickupEffect presenter;
    static ShotgunEquipment equipment;
    static ShotgunCombat combat;
    static PlayerController motor;
    static Rigidbody body;
    static Camera view;
    static CinemachineBrain brain;
    static CameraCursorLock cursor;
    static CinemachineOrbitalFollow orbit;
    static CinemachineInputAxisController cameraInput;
    static CameraOrbitLimit orbitLimit;
    static CatInteractionItem[] cylinders, cubes;
    static Keyboard keyboard;
    static Mouse mouse;
    static KeyboardState keys;
    static bool running, leftButton, rightButton, locked, previousBackground, trackingOrbit;
    static bool forceCursorLock;
    static CursorLockMode previousLock;
    static bool previousVisible;
    static Vector2 mouseDelta, mousePosition;
    static double started;
    static int checks, failures, events;
    static float previousYaw, accumulatedYaw, pickupDuration;
    static string expectedItemId, logPath, capturePrefix;
    static GameObject obstruction, testCanvas, genericRoot;
    static RectTransform destination;
    static IEnumerator<Pause> sequence;
    static Pause waiting;
    static MotionInput motionInput;
    static bool observedEquipmentPose;
    static float renderedHandError, renderedWeaponError;
    static bool observeRelease, releaseObserved, firstReleaseFrameEmpty;
    static int releaseInputFrame;

    sealed class Pause
    {
        public readonly double until;
        public readonly int frame;
        public Pause(float seconds = .15f, int frames = 3)
        {
            until = EditorApplication.timeSinceStartup + seconds;
            frame = Time.frameCount + frames;
        }
    }

    sealed class MotionInput : IPlayerInputSource
    {
        public Vector2 move;
        public bool sprint;
        public PlayerInputFrame Read() => new PlayerInputFrame(move, sprint);
    }

    [MenuItem("Tools/Cat Player/Play 모드 상호작용 검증")]
    public static void Start()
    {
        RequireScene();
        PlayerEditorScope.RequireValidationIdle();
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || running)
            throw new InvalidOperationException("새 PlaytestScene01의 일시정지하지 않은 Play 모드에서 실행하세요.");
        Resolve();
        keyboard = Keyboard.current; mouse = Mouse.current;
        if (keyboard == null || mouse == null) throw new InvalidOperationException("Input System 키보드·마우스가 필요합니다.");
        if (control.Held != null || presenter.IsBusy || equipment.IsEquipped)
            throw new InvalidOperationException("빈손·미장착 상태에서 검증을 시작하세요.");

        string logs = PlayerEditorScope.OutputDirectory;
        string stamp = DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
        logPath = Path.Combine(logs, "CatInteractionValidation_" + stamp + ".txt");
        capturePrefix = Path.Combine(logs, "interaction_" + stamp + "_");
        report.Clear();
        report.AppendLine("Cat interaction validation " + DateTime.Now.ToString("s"));
        report.AppendLine("Scene: " + ScenePath + " | 실제 입력, Play 전용 임시 객체, 씬·에셋 저장 없음");
        checks = failures = events = 0;
        keys = default; leftButton = rightButton = false; locked = true; forceCursorLock = true; mouseDelta = Vector2.zero;
        mousePosition = mouse.position.ReadValue();
        previousBackground = Application.runInBackground;
        previousLock = Cursor.lockState; previousVisible = Cursor.visible;
        pickupDuration = new SerializedObject(presenter).FindProperty("duration").floatValue;
        trackingOrbit = false; observedEquipmentPose = false; motionInput = new MotionInput();
        observeRelease = false;
        PlayerEditorScope.BeginValidation(nameof(InteractionValidation));
        running = true;
        try
        {
            started = EditorApplication.timeSinceStartup;
            Application.runInBackground = true;
            motor.enabled = false; body.isKinematic = true; brain.enabled = false; cursor.enabled = false;
            control.SetInputSource(null);
            presenter.RegisterTarget(null);
            presenter.Collected.AddListener(OnCollected);
            var gameView = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameView != null) EditorWindow.GetWindow(gameView).Focus();
            InputSystem.onAfterUpdate += Pump;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += Mode;
            AssemblyReloadEvents.beforeAssemblyReload += Reload;
            RenderPipelineManager.endCameraRendering += EndCamera;
            Camera.onPostRender += ObserveEquipmentPose;
            sequence = Cases(); waiting = new Pause(.3f, 3);
            Flush();
        }
        catch (Exception exception) { Finish("FAIL 시작 오류: " + exception); }
    }

    [MenuItem("Tools/Cat Player/상호작용 검증 중단")]
    public static void Stop() => Finish("검증 중단: 남은 사례는 통과 처리하지 않았습니다.");
    [MenuItem("Tools/Cat Player/상호작용 검증 중단", true)]
    static bool CanStop() => running;

    static void RequireScene() => PlayerEditorScope.RequireScene();

    static void Resolve()
    {
        control = PlayerEditorScope.FindSingle<InteractionController>();
        presenter = control.pickup; equipment = control.equipment as ShotgunEquipment; view = control.view;
        motor = control.GetComponent<PlayerController>(); body = control.GetComponent<Rigidbody>();
        combat = control.GetComponent<ShotgunCombat>();
        brain = view ? view.GetComponent<CinemachineBrain>() : null;
        cursor = PlayerEditorScope.FindSingle<CameraCursorLock>();
        orbit = cursor.GetComponent<CinemachineOrbitalFollow>();
        cameraInput = cursor.GetComponent<CinemachineInputAxisController>();
        orbitLimit = cursor.GetComponent<CameraOrbitLimit>();
        var items = PlayerEditorScope.FindAll<CatInteractionItem>();
        cylinders = Enumerable.Range(1, 3).Select(i => items.Single(x => x.name == "Cylinder_" + i)).ToArray();
        cubes = Enumerable.Range(1, 2).Select(i => items.Single(x => x.name == "Cube_" + i)).ToArray();
        if (!control.isActiveAndEnabled || !presenter || !equipment || !equipment.IsReady || !view || !motor ||
            !body || !brain || !orbit || !cameraInput || !orbitLimit || !orbitLimit.isActiveAndEnabled ||
            !combat || !combat.isActiveAndEnabled)
            throw new InvalidOperationException("상호작용·장비·카메라 제한 연결을 먼저 확인하세요.");
        PlayerEditorScope.RequireOwned(control, presenter, equipment, combat, view, motor, body, brain, cursor,
            orbit, cameraInput, orbitLimit, equipment.weaponRoot, equipment.motionRoot,
            equipment.leftHand, equipment.rightHand, equipment.leftGrip, equipment.rightGrip);
    }

    static void Pump()
    {
        if (!running || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        if (trackingOrbit)
        {
            accumulatedYaw += Mathf.DeltaAngle(previousYaw, orbit.HorizontalAxis.Value);
            previousYaw = orbit.HorizontalAxis.Value;
        }
        if (forceCursorLock)
        {
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
        InputState.Change(keyboard, keys);
        InputState.Change(mouse, new MouseState
        {
            position = mousePosition, delta = mouseDelta, buttons = (ushort)((leftButton ? 1 : 0) | (rightButton ? 2 : 0))
        });
        if (observeRelease && !leftButton && releaseInputFrame < 0) releaseInputFrame = Time.frameCount;
    }

    static void Tick()
    {
        if (!running) return;
        try
        {
            RequireScene();
            if (!EditorApplication.isPlaying) { Finish("검증 중단: Play 모드 종료."); return; }
            if (EditorApplication.timeSinceStartup - started > 150)
                throw new TimeoutException("150초 안에 검증이 끝나지 않았습니다. 게임 프레임·포커스를 확인하세요.");
            if (EditorApplication.timeSinceStartup < waiting.until || Time.frameCount < waiting.frame) return;
            if (sequence.MoveNext()) waiting = sequence.Current;
            else Finish(null);
        }
        catch (Exception exception) { Finish("FAIL 검증 오류: " + exception); }
    }

    static IEnumerator<Pause> Cases()
    {
        var material = control.GetComponent<CapsuleCollider>().sharedMaterial;
        Check(material && Mathf.Approximately(material.staticFriction, 0) &&
            Mathf.Approximately(material.dynamicFriction, 0) && material.frictionCombine == PhysicsMaterialCombine.Multiply,
            "플레이어 충돌체 무마찰: 정지·이동 마찰 0, 결합 Multiply");

        AimShelf(cylinders[0]); yield return new Pause();
        Check(control.Target == cylinders[0], "기존 매대 실린더를 화면 중심으로 선택");
        Capture("hover");
        keys = new KeyboardState(Key.F); yield return new Pause();
        Check(control.Held == null, "이전 F 입력으로는 물건을 집지 않음");
        keys = default;
        rightButton = true; yield return new Pause();
        Check(control.Held == null && control.Target == cylinders[0], "빈손 우클릭은 조준 중인 물건을 집지 않음");
        rightButton = false; yield return new Pause();

        // 커서 코드가 실제로 잠금을 처리하게 해 재잠금 클릭과 집기를 구분합니다.
        forceCursorLock = false; cursor.enabled = true; cursor.SetLocked(false);
        yield return new Pause();
        leftButton = true; yield return new Pause(.3f, 5);
        Check(Cursor.lockState == CursorLockMode.Locked && control.Held == null,
            "물건을 조준한 상태에서도 재잠금용 좌클릭은 집기로 처리하지 않음");
        leftButton = false; yield return new Pause();
        cursor.enabled = false; forceCursorLock = true; locked = true;
        yield return new Pause();
        Vector3 grabbed = cylinders[0].transform.position;
        leftButton = true; yield return new Pause(.2f, 4);
        Check(control.Held == cylinders[0] && !cylinders[0].Shape.enabled, "좌클릭 누름으로 집고 원래 충돌을 잠시 해제");
        yield return new Pause(1.2f, 12);
        Check(control.Held == cylinders[0] && Vector3.Distance(grabbed, cylinders[0].transform.position) > .04f,
            "좌클릭 유지 중 매대에서 추출하고 보유 유지");
        BeginReleaseMeasurement(); yield return new Pause();
        Check(releaseObserved && firstReleaseFrameEmpty && control.Held == null &&
            cylinders[0].Shape.enabled && cylinders[0].IsAvailable,
            "좌클릭 해제 입력의 첫 렌더 프레임에 보유 종료·충돌·선택 가능 상태 복구");
        observeRelease = false;
        cylinders[0].gameObject.SetActive(false);

        PlaceInOpenArea(cylinders[1]); yield return new Pause();
        Check(control.Target == cylinders[1], "중앙 정렬 검증 전제: 빈 공간의 실린더 조준");
        leftButton = true; yield return new Pause(1.3f, 15);
        Vector3 projected = view.WorldToScreenPoint(cylinders[1].WorldBounds.center);
        float pixelError = Vector2.Distance(projected, view.pixelRect.center);
        Check(control.Held == cylinders[1] && projected.z > 0 && pixelError <= 2,
            "보유 물건 중심이 화면 중앙 포인터와 2픽셀 이내 일치: " + pixelError.ToString("F3") + "px");
        Capture("center_hold");
        var pointer = view.GetComponent<CenterCursor>();
        Check(pointer && pointer.sphere && pointer.sphere.GetComponent<Collider>() == null &&
            Vector2.Distance(view.WorldToScreenPoint(pointer.sphere.position), view.pixelRect.center) <= 1,
            "구체 포인터 화면 정중앙·물리 충돌 없음");

        int shotsBeforeEquip = combat.ShotsFired;
        keys = new KeyboardState(Key.Digit1); yield return new Pause(.9f, 8);
        Check(control.Held == null && cylinders[1].Shape.enabled && cylinders[1].IsAvailable &&
            equipment.IsEquipped && equipment.weaponRoot.gameObject.activeSelf,
            "보유 중 숫자 1: 안전하게 내려놓은 뒤 기존 샷건 장착");
        Check(combat.ShotsFired == shotsBeforeEquip,
            "집던 좌클릭을 계속 누른 채 샷건을 장착해도 발사하지 않음");
        // 장착 이전의 누름을 재사용하지 않습니다. 발사에는 장착 후 새 좌클릭이 필요합니다.
        keys = default; leftButton = false;
        yield return new Pause(equipment.aimingPose ? equipment.aimingPose.raiseSeconds + .15f : .25f, 8);
        Check(observedEquipmentPose && renderedHandError < .003f && renderedWeaponError < .003f,
            $"비조준 장착 양손과 잡는 위치 일치·몸통 기준 샷건 추적: hand={renderedHandError:F5}, weapon={renderedWeaponError:F5}");
        Check(!control.HasFreeHands && !control.TryPickup(), "장비 장착 중 직접 집기 호출 차단");
        Capture("equipped");
        keys = default; leftButton = false; yield return new Pause();
        keys = new KeyboardState(Key.Digit1); yield return new Pause();
        Check(!equipment.IsEquipped && !equipment.weaponRoot.gameObject.activeSelf && control.HasFreeHands,
            "숫자 1을 다시 누르면 장비 해제·빈손 복구");
        keys = default; cylinders[1].gameObject.SetActive(false);

        PlaceInOpenArea(cylinders[2]); yield return new Pause();
        leftButton = true; yield return new Pause(1.2f, 12);
        Check(control.Held == cylinders[2], "막힌 내려놓기 검증 전제: 실린더 보유");
        obstruction = new GameObject("Runtime_DropObstruction") { hideFlags = HideFlags.DontSave };
        obstruction.transform.position = control.transform.position + Vector3.up;
        obstruction.AddComponent<BoxCollider>().size = Vector3.one * 6;
        Physics.SyncTransforms();
        keys = new KeyboardState(Key.Digit1); yield return new Pause();
        Check(!equipment.IsEquipped && control.Held == cylinders[2],
            "내려놓기 후보가 모두 막히면 장착 거부·좌클릭 유지 중 보유 유지");
        var releaseItem = cylinders[2];
        var releaseBlock = obstruction.GetComponent<Collider>();
        bool penetrationBefore = Physics.ComputePenetration(releaseItem.Shape, releaseItem.Body.position,
            releaseItem.Body.rotation, releaseBlock, obstruction.transform.position, obstruction.transform.rotation,
            out var releaseDirection, out float releaseDepth);
        report.AppendLine($"  release before: item={releaseItem.Body.position}, block={releaseBlock.bounds}, overlap={penetrationBefore}, depth={releaseDepth}, ignore={Physics.GetIgnoreCollision(releaseItem.Shape, releaseBlock)}");
        keys = default; BeginReleaseMeasurement(); yield return new Pause();
        report.AppendLine($"  release after: pending={releaseItem.IsReleasePending}, collider={releaseItem.Shape.enabled}, available={releaseItem.IsAvailable}, position={releaseItem.Body.position}");
        Check(releaseObserved && firstReleaseFrameEmpty && control.Held == null &&
            !cylinders[2].Shape.enabled && !cylinders[2].IsAvailable && cylinders[2].IsReleasePending,
            "공간이 완전히 막혀도 해제 첫 렌더 프레임에 빈손·물건은 충돌 복구 대기");
        observeRelease = false;
        UnityEngine.Object.Destroy(obstruction); obstruction = null;
        yield return new Pause(.35f, 5);
        Check(!cylinders[2].IsReleasePending && cylinders[2].Shape.enabled && cylinders[2].IsAvailable &&
            !cylinders[2].Body.isKinematic && cylinders[2].Body.useGravity,
            "장애물 제거 후 충돌·선택·중력 복구, 보유 재개 없음");
        cylinders[2].gameObject.SetActive(false);

        AimShelf(cubes[0]); yield return new Pause();
        Check(control.Target == cubes[0], "기존 바닥 큐브를 화면 중심으로 선택");
        leftButton = true; yield return new Pause();
        Check(cubes[0].gameObject.activeSelf && cubes[0].IsAvailable && !presenter.IsBusy && events == 0,
            "UI 미등록 상태에서는 좌클릭 획득 거부·큐브 유지");
        leftButton = false;
        obstruction = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obstruction.name = "Runtime_AimObstruction"; obstruction.hideFlags = HideFlags.DontSave;
        obstruction.transform.position = (view.transform.position + cubes[0].WorldBounds.center) * .5f;
        obstruction.transform.localScale = new Vector3(2, 2, .12f);
        Physics.SyncTransforms(); yield return new Pause();
        Check(control.Target == null, "벽 너머의 아이템 선택 차단");
        UnityEngine.Object.Destroy(obstruction); obstruction = null;
        CreateInventoryTarget(); yield return new Pause();
        expectedItemId = cubes[0].itemId; leftButton = true; yield return new Pause(.08f, 2);
        Check(presenter.IsBusy && !control.HasFreeHands, "등록된 실제 UI 대상으로 좌클릭 획득 연출 시작");
        leftButton = false; keys = new KeyboardState(Key.Digit1); yield return new Pause(.08f, 2);
        Check(presenter.IsBusy && !equipment.IsEquipped && !control.TryToggleEquipment(),
            "큐브 획득 연출 중 숫자 1·직접 장착 호출 차단");
        keys = default; Capture("pickup");
        yield return new Pause(1.5f, 12);
        Check(!cubes[0].gameObject.activeSelf && !presenter.IsBusy && events == 1,
            "UI 도착 후 큐브 제거·아이템 식별자 이벤트 정확히 1회");

        AimShelf(cubes[1]); yield return new Pause();
        expectedItemId = cubes[1].itemId; leftButton = true; yield return new Pause(.08f, 2);
        Check(presenter.IsBusy, "UI 취소 검증 전제: 두 번째 큐브 연출 시작");
        presenter.RegisterTarget(null);
        Check(!presenter.IsBusy && cubes[1].gameObject.activeSelf && cubes[1].IsAvailable &&
            cubes[1].Shape.enabled && cubes[1].VisualRenderers.All(x => x.enabled) && events == 1,
            "도착 UI 등록 해제 시 큐브 표시·충돌 복구·추가 이벤트 없음");
        leftButton = false; yield return new Pause();
        locked = false; leftButton = true; keys = new KeyboardState(Key.Digit1); yield return new Pause();
        Check(control.Target == null && control.Held == null && !equipment.IsEquipped &&
            !control.TryPickup() && !control.TryToggleEquipment(), "커서 해제 중 입력·직접 집기·장착 호출 차단");
        leftButton = false; keys = default; locked = true;

        var originalMaterial = CreateGenericHighlight(out var renderer); yield return new Pause();
        var generic = genericRoot.GetComponent<HighlightTarget>();
        Check(control.HoveredHighlight != null && control.HoveredHighlight.HighlightOwner == generic && control.Target == null,
            "일반 오브젝트도 강조 컴포넌트만으로 조준 선택");
        leftButton = true; yield return new Pause();
        Check(control.Held == null && genericRoot.activeSelf && generic.VisualRenderers.Length == 2 &&
            !generic.GetComponent<Rigidbody>() && !generic.GetComponent<CatInteractionItem>() &&
            renderer.sharedMaterial == originalMaterial, "일반 강조 대상은 집기 불가·자식 외형 강조·원본 재질 보존");
        leftButton = false; generic.enabled = false; yield return new Pause();
        Check(control.HoveredHighlight == null, "강조 컴포넌트 비활성화 후 선택 해제");

        control.enabled = false;
        // 실제 재컴파일 오류 재발 검사: 사라진 비직렬화 서비스가 활성화 때 재구성되는지 봅니다.
        foreach (string name in new[] { "inputSource", "locomotion", "animationPresenter" })
            typeof(PlayerController).GetField(name, System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).SetValue(motor, null);
        body.isKinematic = false; body.useGravity = false; body.constraints = RigidbodyConstraints.FreezeAll;
        body.position = new Vector3(0, .15f, 0);
        motor.SetInputSource(motionInput); motor.enabled = true;
        orbitLimit.enabled = false;
        orbit.HorizontalAxis.Value = 179;
        orbitLimit.enabled = true; cameraInput.enabled = true; brain.enabled = true;
        Physics.SyncTransforms(); yield return new Pause(.3f, 4);
        Check(new[] { "inputSource", "locomotion", "animationPresenter" }.All(name =>
            typeof(PlayerController).GetField(name, System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.NonPublic).GetValue(motor) != null),
            "재활성화 시 잃어버린 이동·동작 서비스 재구성");
        Check(orbitLimit.motionSource == motor && Mathf.Approximately(orbitLimit.stationaryHalfAngle, 60),
            "카메라 제한과 이동 상태 연결·정지 좌우 60도 설정");
        float center = orbit.HorizontalAxis.Center;
        float vertical = orbit.VerticalAxis.Value; Vector2 verticalRange = orbit.VerticalAxis.Range;
        mouseDelta = new Vector2(160, 0); yield return new Pause(.25f, 8);
        Check(!orbit.HorizontalAxis.Wrap && Mathf.Abs(orbit.HorizontalAxis.Value - center - 60) < .2f,
            "실제 마우스 입력: 정지 시 오른쪽 60도에서 제한 (180도 경계 포함)");
        mouseDelta = new Vector2(-160, 0); yield return new Pause(.3f, 12);
        Check(!orbit.HorizontalAxis.Wrap && Mathf.Abs(orbit.HorizontalAxis.Value - center + 60) < .2f &&
            Mathf.Abs(orbit.HorizontalAxis.Center - center) < .01f, "실제 마우스 입력: 정지 시 왼쪽 60도 제한·정지 기준 유지");
        Check(Vector2.Distance(orbit.VerticalAxis.Range, verticalRange) < .001f &&
            Mathf.Abs(orbit.VerticalAxis.Value - vertical) < .01f, "수평 제한 중 기존 세로 축 범위·값 보존");
        mouseDelta = Vector2.zero;
        motionInput.move = Vector2.up; yield return new Pause();
        BeginOrbitMeasurement(); mouseDelta = new Vector2(200, 0); yield return new Pause(.4f, 24);
        mouseDelta = Vector2.zero; yield return new Pause(.1f, 2); trackingOrbit = false;
        Check(motor.IsMoving && orbit.HorizontalAxis.Wrap && Mathf.Abs(accumulatedYaw) > 360,
            "걷기 입력 중 실제 마우스로 360도 이상 회전: " + accumulatedYaw.ToString("F1") + "도");
        motionInput.sprint = true; BeginOrbitMeasurement();
        mouseDelta = new Vector2(200, 0); yield return new Pause(.4f, 24);
        mouseDelta = Vector2.zero; yield return new Pause(.1f, 2); trackingOrbit = false;
        Check(orbit.HorizontalAxis.Wrap && Mathf.Abs(accumulatedYaw) > 360,
            "달리기 입력 중 실제 마우스로 360도 이상 회전: " + accumulatedYaw.ToString("F1") + "도");
        float stopDirection = orbit.HorizontalAxis.Value;
        motionInput.move = Vector2.zero; motionInput.sprint = false; yield return new Pause();
        Check(!orbit.HorizontalAxis.Wrap && Mathf.Abs(Mathf.DeltaAngle(stopDirection, orbit.HorizontalAxis.Center)) < .2f,
            "이동 종료 시 새 카메라 방향을 정지 제한 기준으로 저장");
        Capture("camera_stop");
        yield return new Pause(.2f, 3);
    }

    static void AimShelf(CatInteractionItem item)
    {
        body.position = new Vector3(item.transform.position.x, .15f, -11.5f);
        body.rotation = Quaternion.Euler(0, 180, 0);
        view.transform.position = new Vector3(item.transform.position.x, 1.65f, -10.2f);
        view.transform.LookAt(item.WorldBounds.center);
        Physics.SyncTransforms();
    }

    static void PlaceInOpenArea(CatInteractionItem item)
    {
        item.Body.position = new Vector3(0, .8f, 0);
        item.transform.position = item.Body.position;
        body.position = new Vector3(0, .15f, -1);
        body.rotation = Quaternion.identity;
        view.transform.position = new Vector3(0, 1.65f, -2.3f);
        view.transform.LookAt(item.WorldBounds.center);
        Physics.SyncTransforms();
    }

    static void CreateInventoryTarget()
    {
        testCanvas = new GameObject("Runtime_ValidationUI", typeof(Canvas)) { hideFlags = HideFlags.DontSave };
        var canvas = testCanvas.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.targetDisplay = view.targetDisplay;
        var target = new GameObject("Registered_Target", typeof(RectTransform), typeof(UnityEngine.UI.Image));
        destination = target.GetComponent<RectTransform>(); destination.SetParent(canvas.transform, false);
        destination.anchorMin = destination.anchorMax = new Vector2(.9f, .9f); destination.sizeDelta = Vector2.one * 72;
        target.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        presenter.RegisterTarget(destination);
        var settings = new SerializedObject(presenter);
        settings.FindProperty("duration").floatValue = 1.2f;
        settings.ApplyModifiedPropertiesWithoutUndo();
    }

    static Material CreateGenericHighlight(out Renderer renderer)
    {
        genericRoot = new GameObject("Runtime_GenericHighlight") { hideFlags = HideFlags.DontSave };
        genericRoot.transform.position = new Vector3(0, .8f, 0);
        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.transform.SetParent(genericRoot.transform, false); sphere.transform.localScale = Vector3.one * .32f;
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.transform.SetParent(genericRoot.transform, false); box.transform.localPosition = Vector3.right * .26f;
        box.transform.localScale = Vector3.one * .2f;
        renderer = sphere.GetComponent<Renderer>();
        var originalMaterial = renderer.sharedMaterial;
        genericRoot.AddComponent<HighlightTarget>();
        body.position = new Vector3(0, .15f, -1); body.rotation = Quaternion.identity;
        view.transform.position = new Vector3(0, 1.65f, -2.3f); view.transform.LookAt(genericRoot.transform);
        Physics.SyncTransforms();
        return originalMaterial;
    }

    static void BeginOrbitMeasurement()
    {
        previousYaw = orbit.HorizontalAxis.Value; accumulatedYaw = 0; trackingOrbit = true;
    }
    static void EndCamera(ScriptableRenderContext context, Camera camera) => ObserveEquipmentPose(camera);
    static void BeginReleaseMeasurement()
    {
        releaseInputFrame = -1; releaseObserved = firstReleaseFrameEmpty = false;
        observeRelease = true; leftButton = false;
    }
    static void ObserveEquipmentPose(Camera camera)
    {
        if (!running || camera != view) return;
        if (observeRelease && releaseInputFrame >= 0 && !releaseObserved)
        {
            releaseObserved = true; firstReleaseFrameEmpty = control.Held == null;
            report.AppendLine($"  release inputFrame={releaseInputFrame}, renderFrame={Time.frameCount}, empty={firstReleaseFrameEmpty}");
        }
        if (!equipment || !equipment.IsEquipped) return;
        observedEquipmentPose = true;
        renderedHandError = Mathf.Max(Vector3.Distance(equipment.leftHand.position, equipment.leftGrip.position),
            Vector3.Distance(equipment.rightHand.position, equipment.rightGrip.position));
        renderedWeaponError = Vector3.Distance(equipment.weaponRoot.position,
            equipment.motionRoot.TransformPoint(equipment.weaponLocalPosition));
    }
    static void OnCollected(string id) { events++; Check(id == expectedItemId, "획득 이벤트 아이템 식별자 보존"); }
    static void Capture(string name)
    {
        string path = capturePrefix + name + ".png";
        ScreenCapture.CaptureScreenshot(path);
        report.AppendLine("CAPTURE " + path + " (시각 확인 별도)"); Flush();
    }
    static void Check(bool ok, string text)
    {
        checks++; if (!ok) failures++;
        report.AppendLine((ok ? "PASS " : "FAIL ") + text); Flush();
        if (!ok && control && view)
        {
            report.AppendLine($"  state lock={Cursor.lockState}, target={control.Target?.name}, held={control.Held?.name}, equipment={equipment.IsEquipped}, ui={presenter.IsBusy}, free={control.HasFreeHands}");
            foreach (var hit in Physics.RaycastAll(view.ViewportPointToRay(new Vector3(.5f, .5f)), 10,
                ~0, QueryTriggerInteraction.Ignore).OrderBy(x => x.distance).Take(8))
                report.AppendLine($"  ray={hit.collider.name} at {hit.distance:F3}, parent={hit.collider.transform.parent?.name}, scene={hit.collider.gameObject.scene.path}");
            Flush();
        }
    }
    static void Flush() => File.WriteAllText(logPath, report.ToString());
    static void Mode(PlayModeStateChange state) { if (state == PlayModeStateChange.ExitingPlayMode) Finish("검증 중단: Play 종료."); }
    static void Reload() => Finish("검증 중단: 스크립트 재컴파일.");

    static void Finish(string interruption)
    {
        if (!running) return;
        running = false;
        try
        {
            InputSystem.onAfterUpdate -= Pump; EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= Mode; AssemblyReloadEvents.beforeAssemblyReload -= Reload;
            RenderPipelineManager.endCameraRendering -= EndCamera; Camera.onPostRender -= ObserveEquipmentPose;
            if (interruption != null) { report.AppendLine(interruption); if (interruption.StartsWith("FAIL")) failures++; }
            try
            {
                if (presenter)
                {
                    presenter.Collected.RemoveListener(OnCollected); presenter.RegisterTarget(null);
                    var settings = new SerializedObject(presenter); settings.FindProperty("duration").floatValue = pickupDuration;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
                if (motor) motor.SetInputSource(null);
                if (control) { control.ReleaseHeld(); control.SetInputSource(null); }
                if (equipment) equipment.Unequip();
                if (keyboard != null && keyboard.added) InputState.Change(keyboard, new KeyboardState());
                if (mouse != null && mouse.added) InputState.Change(mouse, new MouseState { position = mousePosition });
            }
            catch (Exception exception) { failures++; report.AppendLine("FAIL 종료 정리: " + exception); }
            finally
            {
                // DontSave 객체는 Play 종료만으로 정리되지 않을 수 있으므로 직접 제거합니다.
                if (obstruction) UnityEngine.Object.DestroyImmediate(obstruction);
                if (testCanvas) UnityEngine.Object.DestroyImmediate(testCanvas);
                if (genericRoot) UnityEngine.Object.DestroyImmediate(genericRoot);
                obstruction = testCanvas = genericRoot = null;
                sequence?.Dispose(); sequence = null;
                Application.runInBackground = previousBackground;
                Cursor.lockState = previousLock; Cursor.visible = previousVisible;
                report.AppendLine($"RESULT checks={checks}, failures={failures}, interrupted={interruption != null}");
                Flush();
                Debug.Log($"[Cat Interaction] 검사 {checks}, 실패 {failures}. {logPath}");
                if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            }
        }
        finally { PlayerEditorScope.EndValidation(nameof(InteractionValidation)); }
    }
}
