using UnityEngine;

/// <summary>발사 클립의 준비·반동 곡선을 총과 양손 잡는 자세로 표현합니다. 이동 클립은 바꾸지 않습니다.</summary>
[DisallowMultipleComponent]
public sealed class CatShotgunPose : MonoBehaviour
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
    public float CurrentRaise { get; private set; }
    public float Elapsed => elapsed;
    float elapsed, aimWeight;
    bool shotAcknowledged, hasTarget;
    Vector3 aimPoint, cameraForward;

    public void SetTarget(Vector3 target, bool valid, Vector3 forward) { aimPoint = target; hasTarget = valid; cameraForward = forward; }
    public void BeginShot() { elapsed = 0; shotAcknowledged = false; IsShotPlaying = true; }
    public void AcknowledgeShot() { shotAcknowledged = true; elapsed = fireMoment; }

    public void Tick(float deltaTime, bool aiming)
    {
        aimWeight = Mathf.MoveTowards(aimWeight, aiming ? 1 : 0, deltaTime / Mathf.Max(.01f, raiseSeconds));
        if (IsShotPlaying)
        {
            elapsed += deltaTime;
            if (shotAcknowledged && (fireMotion == null || elapsed >= fireMotion.length)) IsShotPlaying = false;
        }
        raiseWeight = recoilDistance = recoilPitch = 0;
        if (IsShotPlaying && fireMotion)
        {
            // 판정 직전에는 정확히 발사 시각의 자세를 사용합니다. 낮은 FPS에서도 반동이 조준을 먼저 틀지 않습니다.
            float sample = shotAcknowledged ? elapsed : Mathf.Min(elapsed, fireMoment);
            fireMotion.SampleAnimation(gameObject, sample);
        }
        CurrentRaise = Mathf.Clamp01(Mathf.Max(aimWeight, raiseWeight));
    }

    public void Apply(Transform weapon, Transform motionRoot)
    {
        if (CurrentRaise <= 0 || !hasTarget) return;
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
        aimedRotation = Quaternion.AngleAxis(-recoilPitch, right) * aimedRotation;
        readyPosition -= direction * (recoilDistance * Mathf.Abs(motionRoot.lossyScale.x));
        weapon.SetPositionAndRotation(Vector3.Lerp(weapon.position, readyPosition, CurrentRaise),
            Quaternion.Slerp(weapon.rotation, aimedRotation, CurrentRaise));
    }

    public void ResetPose()
    {
        IsShotPlaying = false; shotAcknowledged = false; elapsed = aimWeight = CurrentRaise = 0;
        raiseWeight = recoilDistance = recoilPitch = 0; hasTarget = false;
    }
    void OnDisable() => ResetPose();
}
