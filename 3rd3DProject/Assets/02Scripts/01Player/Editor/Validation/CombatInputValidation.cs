using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

/// <summary>가상 장치의 원래 입력 이벤트로 실제 기본 입력·장비·발사 경로를 검사합니다.</summary>
public static class CombatInputValidation
{
    static readonly StringBuilder report = new StringBuilder();
    static ShotgunCombat combat;
    static ShotgunEquipment equipment;
    static PlayerController motor;
    static InteractionController interaction;
    static CameraCursorLock cursor;
    static Animator animator;
    static Rigidbody body;
    static Keyboard keyboard, originalKeyboard;
    static Mouse mouse, originalMouse;
    static KeyboardState keys;
    static bool running, left, right, previousBackground, previousVisible;
    static CursorLockMode previousLock;
    static EditorWindow previousWindow;
    static Vector3 originalPosition;
    static Quaternion originalRotation;
    static IEnumerator<Pause> cases;
    static Pause pause;
    static double started;
    static int checks, failures, pellets;
    static float originalFov;
    static string logPath;

    sealed class Pause
    {
        public readonly double Until;
        public readonly int Frame;
        public Pause(float seconds = .2f, int frames = 4)
        { Until = EditorApplication.timeSinceStartup + seconds; Frame = Time.frameCount + frames; }
    }

    [MenuItem("Tools/Cat Player/Play 모드 실제 조준 입력 검증")]
    public static void Start()
    {
        RequireScene();
        PlayerEditorScope.RequireValidationIdle();
        if (!EditorApplication.isPlaying || EditorApplication.isPaused || running)
            throw new InvalidOperationException("PlaytestScene01의 일시정지하지 않은 새 Play 모드에서 실행하세요.");
        if (InputSystem.settings.updateMode != InputSettings.UpdateMode.ProcessEventsInDynamicUpdate)
            throw new InvalidOperationException("Dynamic Update 입력 설정에서만 검증합니다. 프로젝트 설정은 변경하지 않습니다.");
        combat = PlayerEditorScope.FindSingle<ShotgunCombat>();
        cursor = PlayerEditorScope.FindSingle<CameraCursorLock>();
        equipment = combat.equipment; motor = combat.GetComponent<PlayerController>();
        interaction = combat.GetComponent<InteractionController>();
        animator = combat.GetComponent<Animator>(); body = combat.GetComponent<Rigidbody>();
        if (!combat.isActiveAndEnabled || !cursor.isActiveAndEnabled || !equipment || !equipment.IsReady ||
            !motor || !motor.isActiveAndEnabled || !interaction || !interaction.isActiveAndEnabled || !animator || !body || !combat.view)
            throw new InvalidOperationException("전투·장비·기본 이동·상호작용·카메라 연결을 먼저 확인하세요.");
        PlayerEditorScope.RequireOwned(combat, cursor, equipment, motor, interaction, animator, body,
            combat.view, combat.pose, combat.muzzle, combat.guardOrigin, equipment.weaponRoot,
            equipment.motionRoot, equipment.leftHand, equipment.rightHand, equipment.leftGrip, equipment.rightGrip);
        if (equipment.IsEquipped || !interaction.HasFreeHands)
            throw new InvalidOperationException("물건·장비를 들지 않은 새 Play 모드에서 시작하세요.");
        string logs = PlayerEditorScope.OutputDirectory;
        logPath = Path.Combine(logs, "CatCombatInputValidation_" + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff") + ".txt");
        report.Clear(); checks = failures = pellets = 0; left = right = false; keys = default;
        report.AppendLine("Cat combat default device input validation " + DateTime.Now.ToString("s"));
        report.AppendLine("PlaytestScene01 Play 전용. QueueStateEvent → 실제 기본 입력 → 이동/조준/발사. 씬·에셋 저장 없음.");
        previousBackground = Application.runInBackground; previousLock = Cursor.lockState; previousVisible = Cursor.visible;
        previousWindow = EditorWindow.focusedWindow; originalKeyboard = Keyboard.current; originalMouse = Mouse.current;
        originalPosition = body.position; originalRotation = body.rotation; originalFov = combat.view.fieldOfView;
        PlayerEditorScope.BeginValidation(nameof(CombatInputValidation));
        running = true;
        try
        {
            started = EditorApplication.timeSinceStartup;
            keyboard = InputSystem.AddDevice<Keyboard>("CatCombatValidationKeyboard");
            mouse = InputSystem.AddDevice<Mouse>("CatCombatValidationMouse");
            keyboard.MakeCurrent(); mouse.MakeCurrent();
            Application.runInBackground = true;
            combat.SetInputSource(null); motor.SetInputSource(null); interaction.SetInputSource(null);
            combat.PelletResolved += OnPellet;
            var gameView = typeof(Editor).Assembly.GetType("UnityEditor.GameView");
            if (gameView != null) EditorWindow.GetWindow(gameView).Focus();
            cursor.SetLocked(true);
            InputSystem.onBeforeUpdate += QueueDevices;
            InputSystem.onAfterUpdate += SelectDevices;
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += Mode;
            AssemblyReloadEvents.beforeAssemblyReload += Reload;
            cases = Cases(); pause = new Pause(.4f, 6); Flush();
        }
        catch (Exception exception) { Finish("FAIL 시작 오류: " + exception); }
    }

    [MenuItem("Tools/Cat Player/실제 조준 입력 검증 중단")]
    public static void Stop() => Finish("검증 중단: 남은 항목은 통과하지 않았습니다.");
    [MenuItem("Tools/Cat Player/실제 조준 입력 검증 중단", true)]
    static bool CanStop() => running;

    static void RequireScene() => PlayerEditorScope.RequireScene();

    static void QueueDevices()
    {
        if (!running || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        InputSystem.QueueStateEvent(keyboard, keys);
        InputSystem.QueueStateEvent(mouse, new MouseState
        { position = new Vector2(Screen.width * .5f, Screen.height * .5f), buttons = (ushort)((left ? 1 : 0) | (right ? 2 : 0)) });
    }
    static void SelectDevices()
    {
        if (!running || InputState.currentUpdateType != InputUpdateType.Dynamic) return;
        // 실제 장치의 상태나 활성 여부는 건드리지 않고 검사 중 사용할 current만 선택합니다.
        keyboard.MakeCurrent(); mouse.MakeCurrent();
    }
    static void OnPellet(ShotHit hit) => pellets++;
    static void Tick()
    {
        if (!running) return;
        try
        {
            RequireScene();
            if (!EditorApplication.isPlaying) { Finish("검증 중단: Play 종료."); return; }
            if (EditorApplication.timeSinceStartup - started > 100) throw new TimeoutException("100초 초과. 프레임과 Game 창 포커스를 확인하세요.");
            if (EditorApplication.timeSinceStartup < pause.Until || Time.frameCount < pause.Frame) return;
            if (cases.MoveNext()) pause = cases.Current; else Finish(null);
        }
        catch (Exception exception) { Finish("FAIL 검증 오류: " + exception); }
    }

    static IEnumerator<Pause> Cases()
    {
        Check(Application.isFocused && Cursor.lockState == CursorLockMode.Locked && Mouse.current == mouse && Keyboard.current == keyboard,
            "Game 포커스·커서 잠금·가상 장치 current 준비");
        right = true; yield return new Pause();
        Check(!combat.IsAiming, "빈손 우클릭은 줌으로 처리하지 않음");
        right = false; yield return new Pause(.4f);
        keys = new KeyboardState(Key.Digit1); yield return new Pause();
        Check(equipment.IsEquipped, "숫자 1 실제 기본 입력으로 샷건 장착");
        keys = default; yield return new Pause();
        int before = combat.ShotsFired, beforePellets = pellets;
        left = true; yield return new Pause(.9f, 6);
        Check(combat.ShotsFired == before + 1 && pellets == beforePellets + 4, "비줌 좌클릭 1회 → 발사 1회·산탄 4개");
        yield return new Pause(1f, 6);
        Check(combat.ShotsFired == before + 1, "좌클릭을 발사 간격보다 오래 유지해도 반복 발사하지 않음");
        left = false; yield return new Pause();
        keys = new KeyboardState(Key.W, Key.LeftShift); yield return new Pause(.5f, 6);
        Check(motor.IsMoving && animator.GetCurrentAnimatorStateInfo(0).IsName("Run"), "기본 W+Shift가 Run 모션으로 연결");
        right = true; yield return new Pause(.8f, 6);
        Check(combat.IsAiming && combat.view.fieldOfView < originalFov - 5f, "우클릭 유지로 실제 카메라 줌");
        Vector3 velocity = body.linearVelocity;
        Check(motor.IsMoving && animator.GetCurrentAnimatorStateInfo(0).IsName("Walk") &&
            new Vector2(velocity.x, velocity.z).magnitude <= motor.walkSpeed + .15f,
            "우클릭+W+Shift는 Walk 모션이며 수평 속도는 걷기 상한 이내");
        keys = default; yield return new Pause();
        before = combat.ShotsFired; beforePellets = pellets;
        left = true; yield return new Pause(.9f, 6);
        Check(combat.ShotsFired == before + 1 && pellets == beforePellets + 4, "줌 상태 좌클릭 1회 → 발사 1회·산탄 4개");
        left = false; right = false; yield return new Pause(.8f, 6);
        Check(!combat.IsAiming && Mathf.Abs(combat.view.fieldOfView - originalFov) < .2f, "우클릭 해제 시 비줌 시야각 복귀");
        keys = new KeyboardState(Key.Escape); yield return new Pause();
        Check(Cursor.lockState == CursorLockMode.None && !combat.IsAiming, "Esc 실제 입력으로 커서 해제·조준 중단");
        keys = default; yield return new Pause(); before = combat.ShotsFired;
        left = true; yield return new Pause(.8f, 6);
        Check(Cursor.lockState == CursorLockMode.Locked && combat.ShotsFired == before, "재잠금용 좌클릭은 발사하지 않음");
        left = false; yield return new Pause(); left = true; yield return new Pause(.9f, 6);
        Check(combat.ShotsFired == before + 1, "재잠금 후 새 좌클릭은 정상 발사");
        left = false; yield return new Pause();
        keys = new KeyboardState(Key.Digit1); yield return new Pause();
        Check(!equipment.IsEquipped, "숫자 1 재입력으로 샷건 장착 해제");
        keys = default; yield return new Pause(); before = combat.ShotsFired;
        right = left = true; yield return new Pause(.8f, 6);
        Check(!combat.IsAiming && combat.ShotsFired == before, "빈손 양 버튼 입력은 줌·총 발사로 처리하지 않음");
        left = right = false; yield return new Pause();
    }

    static void Check(bool passed, string message)
    {
        checks++; if (!passed) failures++;
        report.AppendLine((passed ? "PASS " : "FAIL ") + message);
        if (!passed) report.AppendLine($"  focus={Application.isFocused}, lock={Cursor.lockState}, equipped={equipment.IsEquipped}, aim={combat.IsAiming}, shots={combat.ShotsFired}, pellets={pellets}");
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
            InputSystem.onBeforeUpdate -= QueueDevices; InputSystem.onAfterUpdate -= SelectDevices;
            EditorApplication.update -= Tick; EditorApplication.playModeStateChanged -= Mode;
            AssemblyReloadEvents.beforeAssemblyReload -= Reload;
            if (interruption != null) { failures++; report.AppendLine(interruption); }
            try
            {
                if (combat) { combat.PelletResolved -= OnPellet; combat.SetInputSource(null); }
                if (equipment) equipment.Unequip();
                if (interaction) { interaction.ReleaseHeld(); interaction.SetInputSource(null); }
                if (motor) motor.SetInputSource(null);
                if (body) { body.linearVelocity = Vector3.zero; body.angularVelocity = Vector3.zero; body.position = originalPosition; body.rotation = originalRotation; }
            }
            catch (Exception exception) { failures++; report.AppendLine("FAIL 정리 오류: " + exception); }
            finally
            {
                if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
                if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
                keyboard = null; mouse = null;
                if (originalKeyboard != null && originalKeyboard.added) originalKeyboard.MakeCurrent();
                if (originalMouse != null && originalMouse.added) originalMouse.MakeCurrent();
                Application.runInBackground = previousBackground;
                Cursor.lockState = previousLock; Cursor.visible = previousVisible;
                cases?.Dispose(); cases = null;
                report.AppendLine($"RESULT checks={checks}, failures={failures}, interrupted={interruption != null}"); Flush();
                Debug.Log($"[Cat Combat Input] 검사 {checks}, 실패 {failures}. {logPath}");
                if (previousWindow) previousWindow.Focus();
                if (EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
        }
        finally { PlayerEditorScope.EndValidation(nameof(CombatInputValidation)); }
    }
}
