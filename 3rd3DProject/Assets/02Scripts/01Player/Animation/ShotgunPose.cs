using UnityEngine;

/// <summary>발사 상태를 Animator에 전달하고, 조준점에 맞춰 총과 양손 자세를 보정합니다.</summary>
[DisallowMultipleComponent]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "CatShotgunPose")]
public sealed class ShotgunPose : MonoBehaviour
{
    [Tooltip("이 컴포넌트의 raiseWeight/recoilDistance/recoilPitch를 움직이는 발사 클립입니다.")]
    public AnimationClip fireMotion;
    public Transform muzzle;
    [Tooltip("몸통 본 기준 조준 자세의 손잡이 위치입니다.")]
    public Vector3 aimedLocalPosition = new Vector3(.3f, 1.15f, .5f);
    [Min(.01f)] public float raiseSeconds = .12f;
    [Tooltip("클립에서 총을 완전히 앞으로 들고 실제 판정을 내는 시각입니다.")]
    [Min(0)] public float fireMoment = .12f;

    // Unity Animation 창에서 수정할 수 있는 클립 바인딩입니다.
    [HideInInspector] public float raiseWeight;
    [HideInInspector] public float recoilDistance;
    [HideInInspector] public float recoilPitch;

    public bool IsShotPlaying { get; private set; }
    public bool ReadyToFire => IsShotPlaying && !shotAcknowledged && elapsed >= fireMoment;
    // Animator는 Update 뒤에 곡선을 평가합니다. 장비가 자세를 적용할 때 최신 값을 읽습니다.
    public float CurrentRaise => Mathf.Clamp01(Mathf.Max(aimWeight, IsShotPlaying ? raiseWeight : 0f));
    public float Elapsed => elapsed;
    float elapsed, aimWeight;
    bool shotAcknowledged, hasTarget;
    Vector3 aimPoint, cameraForward;
    Animator animator;
    RuntimeAnimatorController boundController;
    bool usesAnimator;
    static readonly int ShotActive = Animator.StringToHash("ShotActive");
    static readonly int ShotTime = Animator.StringToHash("ShotTime");

    void OnEnable() => RefreshAnimationBinding();

    public void SetTarget(Vector3 target, bool valid, Vector3 forward) { aimPoint = target; hasTarget = valid; cameraForward = forward; }
    public void BeginShot() { elapsed = 0; shotAcknowledged = false; IsShotPlaying = true; }
    public void AcknowledgeShot() { shotAcknowledged = true; elapsed = fireMoment; }

    public void Tick(float deltaTime, bool aiming)
    {
        RefreshAnimationBinding();
        aimWeight = Mathf.MoveTowards(aimWeight, aiming ? 1 : 0, deltaTime / Mathf.Max(.01f, raiseSeconds));
        if (IsShotPlaying)
        {
            elapsed += deltaTime;
            if (shotAcknowledged && (fireMotion == null || elapsed >= fireMotion.length)) IsShotPlaying = false;
        }
        if (usesAnimator)
        {
            // Shotgun 레이어의 전환과 클립 평가는 Animator가 담당합니다.
            animator.SetBool(ShotActive, IsShotPlaying && fireMotion);
            animator.SetFloat(ShotTime, IsShotPlaying && fireMotion
                ? Mathf.Clamp01(SampleTime / Mathf.Max(.0001f, fireMotion.length)) : 0f);
        }
        else
        {
            // 다른 씬의 기존 Idle.controller에는 Shotgun 레이어가 없습니다.
            // 그 연결을 바꾸지 않기 위한 호환 경로이며 새 컨트롤러에서는 실행하지 않습니다.
            raiseWeight = recoilDistance = recoilPitch = 0;
            if (IsShotPlaying && fireMotion)
                fireMotion.SampleAnimation(gameObject, SampleTime);
        }
    }

    // 발사 판정 전에는 준비 자세에서 기다립니다. 실제 판정 뒤에만 반동이 진행됩니다.
    float SampleTime => shotAcknowledged ? elapsed : Mathf.Min(elapsed, fireMoment);

    void RefreshAnimationBinding()
    {
        if (!animator) animator = GetComponent<Animator>();
        var controller = animator ? animator.runtimeAnimatorController : null;
        if (!controller)
        {
            boundController = null;
            usesAnimator = false;
            return;
        }
        if (controller == boundController) return;
        boundController = controller;
        usesAnimator = false;
        if (animator.GetLayerIndex("Shotgun") < 0) return;
        bool hasActive = false, hasTime = false;
        foreach (var parameter in animator.parameters)
        {
            hasActive |= parameter.nameHash == ShotActive && parameter.type == AnimatorControllerParameterType.Bool;
            hasTime |= parameter.nameHash == ShotTime && parameter.type == AnimatorControllerParameterType.Float;
        }
        usesAnimator = hasActive && hasTime;
    }

    public void Apply(Transform weapon, Transform motionRoot)
    {
        float currentRaise = CurrentRaise;
        if (currentRaise <= 0 || !hasTarget) return;
        Vector3 readyPosition = motionRoot.TransformPoint(aimedLocalPosition);
        Vector3 offset = muzzle ? Vector3.Scale(weapon.InverseTransformPoint(muzzle.position), weapon.lossyScale) : Vector3.zero;
        Vector3 target = aimPoint;
        // 총구보다 가까운 벽에 맞추려고 총을 뒤집지 않습니다. 이때에도 실제 총구/몸통의 벽 검사는 유지합니다.
        if (Vector3.Dot(target - readyPosition, cameraForward) <= offset.magnitude + .1f)
            target = readyPosition + cameraForward * Mathf.Max(2f, offset.magnitude + 1f);
        Vector3 direction = target - readyPosition;
        if (direction.sqrMagnitude < .0001f) return;
        direction.Normalize();
        // 원본 샷건 총열의 로컬 축은 -X입니다. 정확한 +Z 총구 표시점은 설치 도구에서 따로 연결합니다.
        Quaternion aimedRotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.FromToRotation(Vector3.left, Vector3.forward);
        // 손잡이와 총구 높이가 다른 원본 모델도 중앙 조준점에 모이도록 총구 위치에서 방향을 다시 맞춥니다.
        if (muzzle)
        {
            for (int i = 0; i < 4; i++)
            {
                Vector3 delta = target - (readyPosition + aimedRotation * offset);
                if (delta.sqrMagnitude < .0001f) break;
                direction = delta.normalized;
                aimedRotation = Quaternion.LookRotation(direction, Vector3.up) * Quaternion.FromToRotation(Vector3.left, Vector3.forward);
            }
        }
        Vector3 up = Vector3.up;
        Vector3 right = Vector3.Cross(up, direction).normalized;
        // 취소 직후 Animator가 아직 이전 프레임의 값을 가지고 있어도 반동을 적용하지 않습니다.
        float pitch = IsShotPlaying ? recoilPitch : 0f;
        float distance = IsShotPlaying ? recoilDistance : 0f;
        aimedRotation = Quaternion.AngleAxis(-pitch, right) * aimedRotation;
        readyPosition -= direction * (distance * Mathf.Abs(motionRoot.lossyScale.x));
        weapon.SetPositionAndRotation(Vector3.Lerp(weapon.position, readyPosition, currentRaise),
            Quaternion.Slerp(weapon.rotation, aimedRotation, currentRaise));
    }

    public void ResetPose()
    {
        IsShotPlaying = false; shotAcknowledged = false; elapsed = aimWeight = 0;
        raiseWeight = recoilDistance = recoilPitch = 0; hasTarget = false;
        RefreshAnimationBinding();
        if (usesAnimator)
        {
            animator.SetBool(ShotActive, false);
            animator.SetFloat(ShotTime, 0f);
        }
    }
    void OnDisable() => ResetPose();
}
