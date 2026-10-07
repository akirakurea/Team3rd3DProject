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

/// <summary>허용 씬의 이동·턱·카메라를 검사하는 수동 회귀 검증입니다.</summary>
public static class PlayerValidation
{
    const string ScenePath = PlayerEditorScope.ScenePath;
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
    static PlayerController motor;
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
    static PlayerValidationSample sample;
    static Collider[] sceneColliders;

    [MenuItem(Menu + "협업 에셋 연결 검사 %#F8")]
    public static void InspectSharedAssets()
    {
        var scene = PlayerEditorScope.RequireScene();
        PlayerEditorScope.RequireValidationIdle();
        var text = new StringBuilder("Player shared asset inspection | " + DateTime.Now.ToString("s") + "\n");
        int errors = 0;
        Action<bool, string> check = (ok, message) =>
        { text.AppendLine((ok ? "PASS " : "FAIL ") + message); if (!ok) errors++; };
        text.AppendLine($"Scene={scene.path} dirty={scene.isDirty} play={EditorApplication.isPlaying} compiling={EditorApplication.isCompiling}");
        try
        {
            check(!EditorUtility.scriptCompilationFailed, "스크립트 컴파일 상태");
            var player = PlayerEditorScope.FindSingle<PlayerController>();
            text.AppendLine("Player=" + player.name + " prefab=" + PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(player));
            foreach (var transform in player.GetComponentsInChildren<Transform>(true))
            {
                check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0,
                    "Missing Script: " + transform.name);
                check(PrefabUtility.GetPrefabAssetType(transform.gameObject) != PrefabAssetType.MissingAsset,
                    "Prefab source: " + transform.name);
            }
            foreach (var component in PlayerEditorScope.FindAll<MonoBehaviour>())
            {
                if (!component) continue;
                var script = MonoScript.FromMonoBehaviour(component);
                if (!AssetDatabase.GetAssetPath(script).StartsWith("Assets/02Scripts/01Player/", StringComparison.Ordinal)) continue;
                text.AppendLine("Component=" + component.GetType().Name + " owner=" + component.name);
                using var serialized = new SerializedObject(component);
                var property = serialized.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
#if UNITY_6000_5_OR_NEWER
                    bool missing = property.objectReferenceValue == null && property.objectReferenceEntityIdValue != EntityId.None;
#else
                    bool missing = property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0;
#endif
                    check(!missing, component.GetType().Name + "." + property.propertyPath + "=" +
                        (property.objectReferenceValue ? property.objectReferenceValue.name : "None"));
                }
            }
            foreach (var renderer in player.GetComponentsInChildren<Renderer>(true))
            {
                var skin = renderer as SkinnedMeshRenderer;
                var filter = renderer.GetComponent<MeshFilter>();
                var mesh = skin ? skin.sharedMesh : filter ? filter.sharedMesh : null;
                check(mesh != null, "Mesh: " + renderer.name);
                if (mesh) text.AppendLine("  mesh=" + AssetDatabase.GetAssetPath(mesh) + " vertices=" + mesh.vertexCount);
                if (skin) check(skin.bones.All(bone => bone != null), "Skin bones: " + renderer.name);
                foreach (var material in renderer.sharedMaterials)
                    check(material && material.shader && !ShaderUtil.ShaderHasError(material.shader),
                        "Material/shader: " + renderer.name + " / " + (material ? AssetDatabase.GetAssetPath(material) : "Missing"));
            }
            var animator = player.GetComponent<Animator>();
            check(animator && animator.runtimeAnimatorController, "Animator controller");
            if (animator && animator.runtimeAnimatorController)
            {
                foreach (var clip in animator.runtimeAnimatorController.animationClips.Distinct())
                {
                    var paths = AnimationUtility.GetCurveBindings(clip).Select(binding => binding.path).Distinct();
                    foreach (var path in paths) check(string.IsNullOrEmpty(path) || player.transform.Find(path),
                        "Animation binding: " + clip.name + " / " + path);
                }
            }
            try { Inspect(); text.AppendLine("PASS 이동·물리·카메라 필수 연결"); }
            catch (Exception exception) { check(false, "이동·물리·카메라 연결: " + exception.Message); }
            var combat = player.GetComponent<ShotgunCombat>();
            check(combat && combat.pose && combat.pose.fireMotion && combat.muzzle && combat.guardOrigin,
                "전투·발사 클립·총구·차단점 필수 연결");
            if (combat) text.AppendLine($"Combat pellets={combat.pelletCount} hip={combat.spreadDegrees} aim={combat.aimedSpreadDegrees}");
        }
        catch (Exception exception) { check(false, exception.ToString()); }
        text.AppendLine("RESULT failures=" + errors + "; 파일·meta·씬 저장 없음");
        string output = Path.Combine(PlayerEditorScope.OutputDirectory, "SharedAssets.txt");
        File.WriteAllText(output, text.ToString());
        Debug.Log("[Cat Player] 협업 에셋 검사 오류 " + errors + ": " + output);
    }

    [MenuItem(Menu + "연결 검사")]
    public static void Inspect()
    {
        RequireScene();
        PlayerEditorScope.RequireValidationIdle();
        ResolveReferences();
        Debug.Log("[Cat Player] PASS 연결 검사: 이동·물리·Idle/Walk/Run·Cinemachine 추적 및 입력 참조 정상.");
    }

    [MenuItem(Menu + "Play 모드 이동·턱·카메라 검증")]
    public static void Start()
    {
        RequireScene();
        PlayerEditorScope.RequireValidationIdle();
        if (running) throw new InvalidOperationException("이미 플레이어 검증이 실행 중입니다.");
        if (!EditorApplication.isPlaying)
            throw new InvalidOperationException("PlaytestScene01에서 Play를 시작한 뒤 검증을 실행하세요. 씬을 자동 저장하거나 열지 않습니다.");
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

        PlayerEditorScope.BeginValidation(nameof(PlayerValidation));
        running = true;
        try
        {
            failures = unavailable = caseIndex = 0;
            sessionStart = EditorApplication.timeSinceStartup;
            report.Clear();
            report.AppendLine("Cat Player 수동 회귀 검증 | " + DateTime.Now.ToString("s"));
            report.AppendLine("씬: " + ScenePath + " | 기존 씬·프리팹은 저장하지 않음");
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

    static void RequireScene() => PlayerEditorScope.RequireScene();

    static void ResolveReferences()
    {
        sceneColliders = PlayerEditorScope.FindAll<Collider>();
        var motors = PlayerEditorScope.FindAll<PlayerController>();
        if (motors.Length != 1 || !motors[0].isActiveAndEnabled)
            throw new InvalidOperationException("활성 PlayerController가 정확히 하나 있어야 합니다.");
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
        var orbits = PlayerEditorScope.FindAll<CinemachineOrbitalFollow>()
            .Where(o => o.isActiveAndEnabled).ToArray();
        if (!camera || !camera.isActiveAndEnabled || !brain || !brain.isActiveAndEnabled || orbits.Length != 1)
            throw new InvalidOperationException("출력 Camera·CinemachineBrain 및 단일 활성 Orbital Follow 연결이 필요합니다.");
        orbit = orbits[0];
        var virtualCamera = orbit.GetComponent<CinemachineCamera>();
        var input = orbit.GetComponent<CinemachineInputAxisController>();
        if (!virtualCamera || !virtualCamera.Follow || !virtualCamera.Follow.IsChildOf(motor.transform) ||
            !input || !orbit.GetComponent<CameraCursorLock>())
            throw new InvalidOperationException("Cinemachine의 플레이어 추적 대상·입력·커서 컴포넌트를 확인하세요.");
        PlayerEditorScope.RequireOwned(motor, camera, brain, orbit, virtualCamera.Follow);
        int outputCount = PlayerEditorScope.FindAll<Camera>()
            .Count(c => c.isActiveAndEnabled && c.targetTexture == null);
        if (outputCount != 1) throw new InvalidOperationException("화면 출력용 활성 카메라 수가 1이 아닙니다: " + outputCount);
    }

    static PlayerValidationCase Current => PlayerValidationCases.All[caseIndex];

    static void BeginCase()
    {
        if (caseIndex >= PlayerValidationCases.All.Length) { Finish(null); return; }
        var test = Current;
        motor.ResetRuntimeState();
        motor.view = test.kind == PlayerValidationKind.Orbit ? originalView : motor.transform;
        body.position = test.position;
        body.rotation = Quaternion.LookRotation(test.direction == Vector3.zero ? Vector3.forward : test.direction);
        body.linearVelocity = body.angularVelocity = Vector3.zero;
        body.WakeUp();
        Physics.SyncTransforms();
        Cursor.lockState = test.kind == PlayerValidationKind.Click ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = Cursor.lockState != CursorLockMode.Locked;
        caseStart = Time.time;
        nextSample = 0;
        sample = new PlayerValidationSample(test.position, camera.transform.position, orbit.HorizontalAxis.Value);
        report.AppendLine("BEGIN " + test.name);
        // 하드 코딩된 맵 사례가 빈 공간에서 성공하는 것을 방지합니다.
        bool supported = Physics.RaycastAll(test.position + Vector3.up * 1.5f, Vector3.down, 3f,
            Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            .Any(hit => hit.collider.attachedRigidbody != body && hit.collider.gameObject.scene.path == ScenePath);
        if (!supported || !PlayerValidationCases.HasExpectedMap(test, sceneColliders))
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
            if (test.kind == PlayerValidationKind.Reenable)
            {
                bool active = elapsed < 0.75f || elapsed >= 1.05f;
                if (motor.gameObject.activeSelf != active) motor.gameObject.SetActive(active);
                sample.sawDisabled |= !active;
                sample.sawReenabled |= sample.sawDisabled && active;
            }
            bool moving = elapsed > test.settle && test.direction != Vector3.zero;
            bool escape = test.kind == PlayerValidationKind.Escape && elapsed >= 0.75f && elapsed < 0.9f;
            if (test.kind == PlayerValidationKind.Stop && elapsed > 0.85f) moving = false;
            if (test.kind == PlayerValidationKind.StopRise && body.position.y > 0.015f) sample.stoppedDuringRise = true;
            if (sample.stoppedDuringRise) moving = false;
            var keys = escape ? escapeInput : moving ? test.run ? runInput : walkInput : default;
            InputState.Change(keyboard, keys);
            var mouseState = new MouseState { position = previousMouse.position };
            if (test.kind == PlayerValidationKind.Click && elapsed >= 0.15f && elapsed < 0.3f) mouseState.buttons = 1;
            if (test.kind == PlayerValidationKind.Orbit && elapsed > test.settle) mouseState.delta = new Vector2(3, 0.5f);
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
            bool passed = PlayerValidationCases.Evaluate(Current, sample, motor.walkSpeed, motor.runSpeed, brain, out reason);
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
        try
        {
            InputSystem.onAfterUpdate -= PumpInput;
            EditorApplication.update -= Tick;
            EditorApplication.playModeStateChanged -= OnPlayModeChanged;
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeReload;
            if (interruption != null)
            {
                report.AppendLine(interruption);
                if (interruption.StartsWith("FAIL")) failures++;
            }
            report.AppendLine($"RESULT completed={caseIndex}/{PlayerValidationCases.All.Length}, failed={failures}, unavailable={unavailable}, interrupted={interruption != null}");
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
                    string directory = PlayerEditorScope.OutputDirectory;
                    string path = Path.Combine(directory, "PlayerValidation.txt");
                    File.WriteAllText(path, report.ToString());
                    Debug.Log($"[Cat Player] 검증 종료. 완료 {caseIndex}/{PlayerValidationCases.All.Length}, 실패 {failures}, 검증불가 {unavailable}. {path}");
                }
                catch (Exception exception) { Debug.LogError("검증 보고서 저장 실패: " + exception.Message + "\n" + report); }
                if (EditorApplication.isPlaying) EditorApplication.isPlaying = false;
            }
        }
        finally { PlayerEditorScope.EndValidation(nameof(PlayerValidation)); }
    }
}

/// <summary>수동 설치·검증 도구의 씬 경계입니다. 다른 씬을 열거나 에셋을 만들지 않습니다.</summary>
internal static class PlayerEditorScope
{
    internal const string ScenePath = "Assets/01Scenes/PlaytestScene01.unity";
    static string validationOwner;

    // Play 검증기가 같은 씬·입력·카메라를 동시에 제어하지 않도록 실행 소유자를 공유합니다.
    internal static void RequireValidationIdle()
    {
        if (validationOwner != null)
            throw new InvalidOperationException("Play 검증이 실행 중입니다: " + validationOwner + ". 종료하거나 중단한 뒤 실행하세요.");
    }

    internal static void BeginValidation(string owner)
    {
        RequireValidationIdle();
        validationOwner = owner;
    }

    internal static void EndValidation(string owner)
    {
        if (validationOwner == owner) validationOwner = null;
    }

    internal static string OutputDirectory
    {
        get
        {
            string path = Path.Combine(Path.GetTempPath(), "PlayerValidation");
            Directory.CreateDirectory(path);
            return path;
        }
    }

    internal static Scene RequireScene(bool editMode = false)
    {
        var scene = SceneManager.GetActiveScene();
        if (SceneManager.sceneCount != 1 || !scene.IsValid() || !scene.isLoaded || scene.path != ScenePath)
            throw new InvalidOperationException("PlaytestScene01 하나만 열려 있어야 합니다. 다른 씬은 자동으로 열거나 변경하지 않습니다.");
        if (editMode && EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("PlaytestScene01 편집 모드에서만 연결을 수정할 수 있습니다.");
        return scene;
    }

    internal static bool Owns(Component component)
        => component && component.gameObject.scene == RequireScene();

    internal static void RequireOwned(params UnityEngine.Object[] objects)
    {
        var scene = RequireScene();
        foreach (var target in objects)
        {
            GameObject owner = target is Component component ? component.gameObject : target as GameObject;
            if (!target || !owner || owner.scene != scene)
                throw new InvalidOperationException("필수 연결이 없거나 다른 씬·프리팹 에셋을 가리킵니다: " +
                    (target ? target.name : "Missing"));
        }
    }

    internal static T[] FindAll<T>() where T : Component
        => RequireScene().GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();

    internal static T FindSingle<T>() where T : Component
    {
        var matches = FindAll<T>();
        if (matches.Length != 1)
            throw new InvalidOperationException(typeof(T).Name + " 연결 대상이 정확히 하나 필요합니다. 발견: " + matches.Length);
        return matches[0];
    }

    internal static GameObject FindObject(string name)
    {
        var matches = FindAll<Transform>().Where(item => item.name == name).ToArray();
        if (matches.Length > 1)
            throw new InvalidOperationException("같은 이름의 객체가 여러 개라 자동으로 선택하지 않습니다: " + name);
        return matches.Length == 1 ? matches[0].gameObject : null;
    }

    internal static Camera OutputCamera(PlayerController motor)
    {
        var linked = Owns(motor.view) ? motor.view.GetComponent<Camera>() : null;
        if (linked && linked.isActiveAndEnabled && !linked.targetTexture) return linked;
        var matches = FindAll<Camera>().Where(camera => camera.isActiveAndEnabled && !camera.targetTexture).ToArray();
        if (matches.Length != 1)
            throw new InvalidOperationException("허용 씬의 출력 카메라가 정확히 하나 필요합니다. 발견: " + matches.Length);
        return matches[0];
    }

    // 기존 직렬화 참조가 없는 경우에만 사용합니다. 폴더를 옮겨도 기존 meta의 GUID로 찾습니다.
    internal static T ExistingPlayerAsset<T>(string guid, string label) where T : UnityEngine.Object
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        // 기존 Player 에셋 보관 폴더만 허용합니다. 끝의 /로 비슷한 이름의 다른 폴더를 제외합니다.
        bool playerPath = path.StartsWith("Assets/04Prefabs/Player/", StringComparison.Ordinal) ||
            path.StartsWith("Assets/03Sprites/Player/", StringComparison.Ordinal) ||
            path.StartsWith("Assets/03Sprites/Cat_Player/", StringComparison.Ordinal);
        var asset = playerPath ? AssetDatabase.LoadAssetAtPath<T>(path) : null;
        if (!asset)
            throw new InvalidOperationException(label + " 기존 Player 에셋을 찾을 수 없습니다. 에셋이나 meta를 새로 만들지 않습니다.");
        return asset;
    }
}
