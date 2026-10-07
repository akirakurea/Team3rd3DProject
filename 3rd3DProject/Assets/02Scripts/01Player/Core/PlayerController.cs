using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(Animator))]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "CatPlayerMotor")]
public sealed class PlayerController : MonoBehaviour, IMotionState
{
    [Header("연결")]
    [Tooltip("이동 방향의 기준 카메라. 비어 있으면 월드 전방을 사용합니다.")]
    public Transform view;
    [Tooltip("조준 상태를 제공하는 컴포넌트. 조준 중에는 Shift를 눌러도 걷습니다.")]
    public MonoBehaviour aimSource;
    [Header("이동 · 회전")]
    [Tooltip("걷기 속도 (월드 단위/초)")]
    [Min(0)] public float walkSpeed = 1.6f;
    [Tooltip("달리기 속도 (월드 단위/초)")]
    [Min(0)] public float runSpeed = 3.4f;
    [Min(0)] public float acceleration = 18f;
    [Tooltip("초당 회전 각도")]
    [Min(0)] public float turnSpeed = 720f;
    [Header("작은 턱 넘기 (월드 단위)")]
    [SerializeField] StepSettings steps = new StepSettings();
    [Header("애니메이션")]
    [SerializeField] PlayerAnimation.Settings animationSettings = new PlayerAnimation.Settings();

    IPlayerInputSource inputSource;
    PlayerMovement locomotion;
    PlayerAnimation animationPresenter;
    PlayerInputFrame currentInput;

    /// <summary>현재 이동 의도만 공개하여 카메라 등이 이동 구현에 직접 의존하지 않도록 합니다.</summary>
    public bool IsMoving => currentInput.IsMoving;

    // 씬 연결과 Unity 실행 순서만 담당합니다. 기능별 계산은 아래 서비스로 위임합니다.
    void Awake() => EnsureRuntimeServices();

    // Play 중 재컴파일하면 일반 C# 서비스는 사라질 수 있고 Awake는 다시 호출되지 않습니다.
    void OnEnable() => EnsureRuntimeServices();

    void EnsureRuntimeServices()
    {
        inputSource ??= new KeyboardInput();
        if (locomotion == null)
        {
            var body = GetComponent<Rigidbody>();
            var stepSolver = new StepSolver(body, GetComponent<CapsuleCollider>(), steps);
            locomotion = new PlayerMovement(body, transform, stepSolver);
        }
        animationPresenter ??= new PlayerAnimation(GetComponent<Animator>(), animationSettings);
    }

    void Update()
    {
        currentInput = inputSource.Read();
        if (aimSource && aimSource.isActiveAndEnabled && aimSource is IAimState aim)
        {
            var intent = CombatPolicy.RestrictMovement(currentInput.Intent, aim.IsAiming);
            currentInput = new PlayerInputFrame(new Vector2(intent.Horizontal, intent.Forward), intent.Sprint);
        }
        animationPresenter.Apply(currentInput.Intent);
    }

    void FixedUpdate()
    {
        var settings = new MovementSettings(walkSpeed, runSpeed, acceleration, turnSpeed);
        bool faceView = aimSource && aimSource.isActiveAndEnabled && aimSource is IAimState aim && aim.WantsCameraFacing;
        locomotion.Apply(currentInput.Intent, view, settings, Time.fixedDeltaTime, faceView);
    }

    /// <summary>게임패드·AI·재생 입력을 연결할 확장점. null은 기본 키보드 입력으로 복귀합니다.</summary>
    public void SetInputSource(IPlayerInputSource source)
    {
        inputSource = source ?? new KeyboardInput();
        currentInput = default;
    }

    // 비활성화·재사용 때 이전 입력과 턱 목표가 남지 않도록 합니다.
    void OnDisable() => ResetRuntimeState();

    public void ResetRuntimeState()
    {
        currentInput = default;
        locomotion?.Reset();
        animationPresenter?.InvalidateState();
    }
}
