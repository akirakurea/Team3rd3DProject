using System;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>PlayerTestScene의 기존 오브젝트만 사용하는 수동 회귀 검증입니다.</summary>
public static class CatPlayerValidation
{
    const string ScenePath = "Assets/01Scenes/PlayerTestScene.unity";
    const string Menu = "Tools/Cat Player/";
    static readonly StringBuilder report = new StringBuilder();
    static readonly KeyboardState walkInput = new KeyboardState(Key.W);
    static readonly KeyboardState runInput = new KeyboardState(Key.W, Key.LeftShift);
    static readonly KeyboardState escapeInput = new KeyboardState(Key.W, Key.Escape);
    static bool running, previousBackground, previousCursorVisible;
    static CursorLockMode previousCursor;
    static KeyboardState previousKeyboard;
    static MouseState previousMouse;
    static Keyboard keyboard;
    static Mouse mouse;
    static CatPlayerMotor motor;
    static Rigidbody body;
    static Animator animator;
    static Camera camera;
    static CinemachineBrain brain;
    static CinemachineOrbitalFollow orbit;
    static Transform originalView;
    static Vector3 originalPosition, originalVelocity, originalAngularVelocity;
    static Quaternion originalRotation;
    static float originalYaw, originalPitch;
    static int caseIndex, failures, unavailable;
    static float caseStart;
    static double sessionStart, nextSample;
    static CatPlayerValidationSample sample;
    static Collider[] sceneColliders;

    [MenuItem(Menu + "연결 검사")]
    public static void Inspect()
    {
        RequireScene();
        if (running) throw new InvalidOperationException("검증 실행 중에는 연결 검사를 실행할 수 없습니다.");
        ResolveReferences();
        Debug.Log("[Cat Player] PASS 연결 검사: 이동·물리·Idle/Walk/Run·Cinemachine 추적 및 입력 참조 정상.");
    }

    [MenuItem(Menu + "Play 모드 전체 검증")]
    public static void Start()
    {
        RequireScene();
        if (running) throw new InvalidOperationException("이미 플레이어 검증이 실행 중입니다.");
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("PlayerTestScene에서 Play를 시작한 뒤 검증을 실행하세요. 씬을 자동 저장하거나 열지 않습니다.");
        ResolveReferences();
        keyboard = Keyboard.current;
        mouse = Mouse.current;
        if (keyboard == null || mouse == null)
            throw new InvalidOperationException("실제 키보드·마우스 입력 장치가 필요합니다.");

        // 검증이 바꾸는 일시 상태는 정상 종료·중단·예외에서 동일하게 복원합니다.
        originalView = motor.view;
        originalPosition = body.position;
        originalRotation = body.rotation;
        originalVelocity = body.linearVelocity;
        originalAngularVelocity = body.angularVelocity;
        originalYaw = orbit.HorizontalAxis.Value;
        originalPitch = orbit.VerticalAxis.Value;
        previousBackground = Application.runInBackground;
        previousCursor = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        previousKeyboard = new KeyboardState(keyboard.allKeys.Where(k => k.isPressed).Select(k => k.keyCode).ToArray());
        previousMouse = new MouseState { position = mouse.position.ReadValue(), scroll = mouse.scroll.ReadValue() };
        if (mouse.leftButton.isPressed) previousMouse.buttons |= 1;
        if (mouse.rightButton.isPressed) previousMouse.buttons |= 2;
        if (mouse.middleButton.isPressed) previousMouse.buttons |= 4;

        running = true;
        failures = unavailable = caseIndex = 0;
        sessionStart = EditorApplication.timeSinceStartup;
        report.Clear();
        report.AppendLine("Cat Player 수동 회귀 검증 | " + DateTime.Now.ToString("s"));
        report.AppendLine("씬: " + ScenePath + " | 기존 씬·프리팹은 저장하지 않음");
        try
        {
            Application.runInBackground = true;
            var gameView = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameView != null) EditorWindow.GetWindow(gameView).Focus();
            InputSystem.onAfterUpdate += PumpInput;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload += BeforeReload;
            BeginCase();
        }
        catch (Exception exception) { Finish("FAIL 시작 오류: " + exception.Message); }
    }

    [MenuItem(Menu + "검증 중단")]
    public static void Stop() => Finish("검증 중단: 완료하지 않은 사례는 통과로 처리하지 않았습니다.");

    [MenuItem(Menu + "검증 중단", true)]
    static bool CanStop() => running;

    static void RequireScene()
    {
        if (SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("PlayerTestScene 이외의 씬에서는 실행할 수 없습니다.");
    }

    static void ResolveReferences()
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        sceneColliders = roots.SelectMany(r => r.GetComponentsInChildren<Collider>(true)).ToArray();
        var motors = roots.SelectMany(r => r.GetComponentsInChildren<CatPlayerMotor>(true)).ToArray();
        if (motors.Length != 1 || !motors[0].isActiveAndEnabled)
            throw new InvalidOperationException("활성 CatPlayerMotor가 정확히 하나 있어야 합니다.");
        motor = motors[0];
        body = motor.GetComponent<Rigidbody>();
        animator = motor.GetComponent<Animator>();
        if (!body || !animator || !motor.GetComponent<CapsuleCollider>() || !motor.view)
            throw new InvalidOperationException("플레이어의 Rigidbody, CapsuleCollider, Animator, view 연결을 확인하세요.");
        if (body.isKinematic || !body.useGravity || animator.applyRootMotion)
            throw new InvalidOperationException("동적 Rigidbody·중력 사용·Apply Root Motion 해제 설정이 필요합니다.");
        var controller = animator.runtimeAnimatorController;
        if (controller is AnimatorOverrideController overrides) controller = overrides.runtimeAnimatorController;
        var editableController = controller as UnityEditor.Animations.AnimatorController;
        foreach (string state in new[] { "Idle", "Walk", "Run" })
        {
            bool exists = EditorApplication.isPlaying
                ? animator.HasState(0, Animator.StringToHash("Base Layer." + state))
                : editableController && editableController.layers.Length > 0 &&
                  editableController.layers[0].stateMachine.states.Any(s => s.state.name == state);
            if (!exists) throw new InvalidOperationException("Animator의 Base Layer." + state + " 상태가 없습니다.");
        }
        camera = motor.view.GetComponent<Camera>();
        brain = camera ? camera.GetComponent<CinemachineBrain>() : null;
        var orbits = roots.SelectMany(r => r.GetComponentsInChildren<CinemachineOrbitalFollow>(true))
            .Where(o => o.isActiveAndEnabled).ToArray();
        if (!camera || !camera.isActiveAndEnabled || !brain || !brain.isActiveAndEnabled || orbits.Length != 1)
            throw new InvalidOperationException("출력 Camera·CinemachineBrain 및 단일 활성 Orbital Follow 연결이 필요합니다.");
        orbit = orbits[0];
        var virtualCamera = orbit.GetComponent<CinemachineCamera>();
        var input = orbit.GetComponent<CinemachineInputAxisController>();
        if (!virtualCamera || !virtualCamera.Follow || !virtualCamera.Follow.IsChildOf(motor.transform) ||
            !input || !orbit.GetComponent<CatCinemachineCursor>())
            throw new InvalidOperationException("Cinemachine의 플레이어 추적 대상·입력·커서 컴포넌트를 확인하세요.");
        int outputCount = roots.SelectMany(r => r.GetComponentsInChildren<Camera>(true))
            .Count(c => c.isActiveAndEnabled && c.targetTexture == null);
        if (outputCount != 1) throw new InvalidOperationException("화면 출력용 활성 카메라 수가 1이 아닙니다: " + outputCount);
    }

    static CatPlayerValidationCase Current => CatPlayerValidationCases.All[caseIndex];

    static void BeginCase()
    {
        if (caseIndex >= CatPlayerValidationCases.All.Length) { Finish(null); return; }
        var test = Current;
        motor.ResetRuntimeState();
        motor.view = test.kind == CatPlayerValidationKind.Orbit ? originalView : motor.transform;
        body.position = test.position;
        body.rotation = Quaternion.LookRotation(test.direction == Vector3.zero ? Vector3.forward : test.direction);
        body.linearVelocity = body.angularVelocity = Vector3.zero;
        body.WakeUp();
        Physics.SyncTransforms();
        Cursor.lockState = test.kind == CatPlayerValidationKind.Click ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        caseStart = Time.time;
        nextSample = 0;
        sample = new CatPlayerValidationSample(test.position, camera.transform.position, orbit.HorizontalAxis.Value);
        report.AppendLine("BEGIN " + test.name);
        // 하드 코딩된 맵 사례가 빈 공간에서 성공하는 것을 방지합니다.
        bool supported = Physics.RaycastAll(test.position + Vector3.up * 1.5f, Vector3.down, 3f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            .Any(hit => hit.collider.attachedRigidbody != body && hit.collider.gameObject.scene.path == ScenePath);
        if (!supported || !CatPlayerValidationCases.HasExpectedMap(test, sceneColliders))
        {
            RecordResult("검증불가", test.name, "시작점 바닥 또는 기준 맵의 지형·높이가 다름: " + test.position);
            caseIndex++;
            BeginCase();
        }
    }

    static void PumpInput()
    {
        if (!running || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        try
        {
            var test = Current;
            float elapsed = Time.time - caseStart;
            if (test.kind == CatPlayerValidationKind.Reenable)
            {
                bool active = elapsed < 0.75f || elapsed >= 1.05f;
                if (motor.gameObject.activeSelf != active) motor.gameObject.SetActive(active);
                sample.sawDisabled |= !active;
                sample.sawReenabled |= sample.sawDisabled && active;
            }
            bool moving = elapsed > test.settle && test.direction != Vector3.zero;
            bool escape = test.kind == CatPlayerValidationKind.Escape && elapsed >= 0.75f && elapsed < 0.9f;
            if (test.kind == CatPlayerValidationKind.Stop && elapsed > 0.85f) moving = false;
            if (test.kind == CatPlayerValidationKind.StopRise && body.position.y > 0.015f) sample.stoppedDuringRise = true;
            if (sample.stoppedDuringRise) moving = false;
            var keys = escape ? escapeInput : moving ? test.run ? runInput : walkInput : default;
            InputState.Change(keyboard, keys);
            var mouseState = new MouseState { position = previousMouse.position };
            if (test.kind == CatPlayerValidationKind.Click && elapsed >= 0.15f && elapsed < 0.3f) mouseState.buttons = 1;
            if (test.kind == CatPlayerValidationKind.Orbit && elapsed > test.settle) mouseState.delta = new Vector2(3, 0.5f);
            InputState.Change(mouse, mouseState);
        }
        catch (Exception exception) { Finish("FAIL 입력 검증 오류: " + exception.Message); }
    }

    static void Tick()
    {
        if (!running) return;
        try
        {
            RequireScene();
            if (!EditorApplication.isPlaying) { Finish("검증 중단: Play 모드가 종료되었습니다."); return; }
            if (!motor || !body || !camera || !orbit) throw new InvalidOperationException("검증 대상이 제거되었습니다.");
            if (EditorApplication.timeSinceStartup - sessionStart > 120)
                throw new TimeoutException("120초 제한을 초과했습니다. 일시정지 또는 게임 업데이트 중단을 확인하세요.");
            float elapsed = Time.time - caseStart;
            var state = animator.GetCurrentAnimatorStateInfo(0);
            sample.Observe(body, camera, orbit, state, elapsed > Current.settle);
            if (elapsed >= nextSample)
            {
                report.AppendLine($"  t={elapsed:F2} pos={body.position:F3} velocity={body.linearVelocity:F3} cursor={Cursor.lockState} state={sample.finalState}");
                nextSample = elapsed + 0.2f;
            }
            if (elapsed < Current.duration + Current.settle) return;
            string reason;
            bool passed = CatPlayerValidationCases.Evaluate(Current, sample, motor.walkSpeed, motor.runSpeed, brain, out reason);
            RecordResult(passed ? "PASS" : "FAIL", Current.name, reason);
            caseIndex++;
            BeginCase();
        }
        catch (Exception exception) { Finish("FAIL 검증 오류: " + exception.Message); }
    }

    static void RecordResult(string status, string name, string detail)
    {
        if (status == "FAIL") failures++;
        if (status == "검증불가") unavailable++;
        report.AppendLine($"{status} {name}: {detail}");
    }

    static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode) Finish("검증 중단: Play 종료 요청.");
    }

    static void BeforeReload() => Finish("검증 중단: 스크립트 재컴파일/도메인 리로드.");

    static void Finish(string interruption)
    {
        if (!running) return;
        running = false;
        InputSystem.onAfterUpdate -= PumpInput;
        EditorApplication.update -= Tick;
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        AssemblyReloadEvents.beforeAssemblyReload -= BeforeReload;
        if (interruption != null)
        {
            report.AppendLine(interruption);
            if (interruption.StartsWith("FAIL")) failures++;
        }
        report.AppendLine($"RESULT completed={caseIndex}/{CatPlayerValidationCases.All.Length}, failed={failures}, unavailable={unavailable}, interrupted={interruption != null}");
        try
        {
            if (motor) motor.view = originalView;
            if (motor)
            {
                if (!motor.gameObject.activeSelf) motor.gameObject.SetActive(true);
                motor.ResetRuntimeState();
            }
            if (body)
            {
                body.position = originalPosition;
                body.rotation = originalRotation;
                body.linearVelocity = originalVelocity;
                body.angularVelocity = originalAngularVelocity;
            }
            if (orbit) { orbit.HorizontalAxis.Value = originalYaw; orbit.VerticalAxis.Value = originalPitch; }
            if (keyboard != null && keyboard.added) InputSystem.QueueStateEvent(keyboard, previousKeyboard);
            if (mouse != null && mouse.added) InputSystem.QueueStateEvent(mouse, previousMouse);
        }
        catch (Exception exception) { report.AppendLine("FAIL 상태 복원: " + exception.Message); }
        finally
        {
            Application.runInBackground = previousBackground;
            Cursor.lockState = previousCursor;
            Cursor.visible = previousCursorVisible;
            try
            {
                string directory = Path.Combine(Path.GetDirectoryName(Application.dataPath), "Logs");
                Directory.CreateDirectory(directory);
                string path = Path.Combine(directory, "CatPlayerValidation.txt");
                File.WriteAllText(path, report.ToString());
                Debug.Log($"[Cat Player] 검증 종료. 완료 {caseIndex}/{CatPlayerValidationCases.All.Length}, 실패 {failures}, 검증불가 {unavailable}. {path}");
            }
            catch (Exception exception) { Debug.LogError("검증 보고서 저장 실패: " + exception.Message + "\n" + report); }
            if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
        }
    }
}
