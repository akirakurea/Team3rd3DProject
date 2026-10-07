using System;
using UnityEngine;

/// <summary>장비·입력·자세·명중 서비스를 연결합니다. 체력이나 인벤토리 구현을 알지 않습니다.</summary>
[DefaultExecutionOrder(-90)]
[DisallowMultipleComponent]
[UnityEngine.Scripting.APIUpdating.MovedFrom(true, sourceNamespace: "", sourceAssembly: "Assembly-CSharp", sourceClassName: "CatShotgunCombat")]
public sealed class ShotgunCombat : MonoBehaviour, IAimState, IShotSpreadState
{
    [Header("연결")]
    public ShotgunEquipment equipment;
    public ShotgunPose pose;
    public Camera view;
    [Tooltip("실제 총열 끝. 로컬 +Z가 총구가 향하는 방향입니다.")] public Transform muzzle;
    [Tooltip("총이 벽을 뚫고 나왔는지 검사하는 몸통 쪽 시작점입니다.")] public Transform guardOrigin;
    [Header("발사")]
    [Min(.1f)] public float shotInterval = .55f;
    [Min(1)] public float range = 75f;
    [Range(1, 16)] public int pelletCount = 4;
    [Tooltip("비조준 산탄 원뿔의 반각입니다.")]
    [Range(0, 12)] public float spreadDegrees = 4f;
    [Tooltip("줌 중 산탄 원뿔의 반각입니다. 비조준보다 넓어지지 않습니다.")]
    [Range(0, 12)] public float aimedSpreadDegrees = 1.5f;
    [Min(.001f)] public float muzzleRadius = .025f;
    public LayerMask hitLayers = ~0;

    public bool IsAiming { get; private set; }
    public bool IsWeaponEquipped => equipment && equipment.IsEquipped;
    public float CurrentSpreadDegrees => Mathf.Max(0, IsAiming ? Mathf.Min(spreadDegrees, aimedSpreadDegrees) : spreadDegrees);
    public bool WantsCameraFacing => IsAiming || (equipment && equipment.IsEquipped && pose && pose.IsShotPlaying);
    public int ShotsFired { get; private set; }
    public ShotHit LastHit { get; private set; }
    public event Action<ShotHit> PelletResolved;
    public event Action<int> ShotFired;
    ICombatInputSource inputSource = new CombatInput();
    CombatPolicy policy = new CombatPolicy();
    HitscanQuery query;
    ShotgunEquipment subscribed;
    bool aimPointValid;

    void OnEnable() { query = new HitscanQuery(transform); policy ??= new CombatPolicy(); Bind(); }
    void Bind()
    {
        if (subscribed == equipment) return;
        Unbind();
        subscribed = equipment;
        if (!subscribed) return;
        subscribed.BeforePoseApplied += ResolveAim;
        subscribed.PoseApplied += FinishShot;
        subscribed.Unequipped += ResetCombat;
    }
    void Unbind()
    {
        if (!subscribed) return;
        subscribed.BeforePoseApplied -= ResolveAim;
        subscribed.PoseApplied -= FinishShot;
        subscribed.Unequipped -= ResetCombat;
        subscribed = null;
    }
    public void SetInputSource(ICombatInputSource source) { inputSource = source ?? new CombatInput(); ResetCombat(); }
    void Update()
    {
        Bind();
        inputSource ??= new CombatInput();
        var input = inputSource.Read();
        bool ready = equipment && equipment.IsEquipped && equipment.IsReady && pose && pose.isActiveAndEnabled &&
            equipment.aimingPose == pose && pose.fireMotion && pose.fireMoment <= pose.fireMotion.length &&
            view && view.isActiveAndEnabled && muzzle && guardOrigin;
        IsAiming = ready && input.Enabled && input.AimHeld;
        if (!ready || !input.Enabled) { ResetCombat(); return; }
        bool started = !pose.IsShotPlaying && policy.TryRequest(input.Enabled, ready, input.FirePressed, Time.time, shotInterval);
        if (started) pose.BeginShot();
        // 누른 프레임 이전의 deltaTime은 새 준비 시간에 포함하지 않습니다.
        pose.Tick(started ? 0f : Time.deltaTime, IsAiming);
    }
    void ResolveAim()
    {
        if (!pose || !view) return;
        // 총을 내린 상태에서는 자세가 조준점을 사용하지 않습니다. 줌 해제 중의 자세 전환은 유지합니다.
        if (pose.CurrentRaise <= 0 && !pose.IsShotPlaying)
        {
            aimPointValid = false;
            return;
        }
        query ??= new HitscanQuery(transform);
        aimPointValid = query.TryGetAimPoint(view, range, hitLayers.value, out Vector3 target);
        pose.SetTarget(target, aimPointValid, view.transform.forward);
    }
    void FinishShot()
    {
        if (!equipment.IsEquipped || !pose || !pose.ReadyToFire) return;
        Vector3 flatView = Vector3.ProjectOnPlane(view.transform.forward, Vector3.up).normalized;
        // 몸은 카메라 방향으로 회전합니다. 가까운 표적으로 수렴하는 총열 각도를 기준으로 기다리면
        // 벽 앞에서 몸이 다 돌아간 뒤에도 발사 준비가 끝나지 않을 수 있습니다.
        if (flatView.sqrMagnitude > .01f && Vector3.Dot(transform.forward, flatView) < .94f) return;
        pose.AcknowledgeShot();
        // 포화된 검사 결과로 벽 너머에 쏘지 않도록 조준점 계산이 실패하면 발사를 취소합니다.
        if (!aimPointValid) return;
        int count = Mathf.Clamp(pelletCount, 1, 16);
        ShotsFired++;
        for (int i = 0; i < count; i++)
        {
            // 네 방향이 중심을 균형 있게 둘러쌉니다. 표시 원의 범위 안에서 매 발 분포가 회전합니다.
            float radius = count == 1 ? 0 : CurrentSpreadDegrees * (.6f + .4f * Mathf.Repeat(ShotsFired * .618034f, 1));
            float angle = (i * (360f / count) + ShotsFired * 47.3f) * Mathf.Deg2Rad;
            Vector2 spread = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            LastHit = query.Cast(guardOrigin.position, muzzle, range, hitLayers.value, spread, muzzleRadius);
            Debug.DrawLine(LastHit.Origin, LastHit.Point, LastHit.Kind == ShotHitKind.Hit ? Color.yellow : Color.red, .5f);
            PelletResolved?.Invoke(LastHit);
            if (LastHit.Collider && LastHit.Kind == ShotHitKind.Hit)
            {
                var receiver = LastHit.Collider.GetComponentInParent<IShotReceiver>();
                receiver?.ReceiveShot(LastHit);
            }
        }
        ShotFired?.Invoke(ShotsFired);
    }
    // 장착 토글·메뉴를 열어도 발사 간격은 유지합니다. 자세 취소와 발사 간격 초기화는 구분합니다.
    void ResetCombat() { IsAiming = false; aimPointValid = false; pose?.ResetPose(); }
    void OnApplicationFocus(bool focused)
    {
        if (focused) return;
        if (inputSource is IResettableInputSource resettableInput) resettableInput.Reset();
        ResetCombat();
    }
    void OnDisable()
    {
        if (inputSource is IResettableInputSource resettableInput) resettableInput.Reset();
        ResetCombat(); Unbind();
    }
}

/// <summary>맞은 대상의 담당 코드가 구현하는 명중 전달점. 피해량·체력 규칙은 대상 쪽에서 정합니다.</summary>
public interface IShotReceiver { void ReceiveShot(ShotHit hit); }
