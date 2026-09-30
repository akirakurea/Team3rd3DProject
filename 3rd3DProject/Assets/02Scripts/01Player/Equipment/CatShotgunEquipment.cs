using UnityEngine;

/// <summary>준비된 샷건 장착과 몸통·양손의 잡는 자세만 담당합니다.</summary>
[DefaultExecutionOrder(10010)]
[DisallowMultipleComponent]
public sealed class CatShotgunEquipment : MonoBehaviour, ICatEquipmentPort
{
    [Header("장비 연결")]
    [Tooltip("씬에 미리 배치한 샷건 표시 루트. 플레이어 아래에 두고 손 본이나 몸통 본의 자식으로 두지 않습니다.")]
    public Transform weaponRoot;
    [Tooltip("기존 애니메이션의 몸통 움직임을 따라갈 본. 예: CTRL_Squash")]
    public Transform motionRoot;

    [Header("몸통 기준 샷건 자세")]
    [Tooltip("몸통 본의 좌표를 기준으로 한 샷건 루트 위치입니다.")]
    public Vector3 weaponLocalPosition;
    [Tooltip("몸통 본의 회전을 기준으로 더할 샷건 루트 회전입니다.")]
    public Vector3 weaponLocalEulerAngles;

    [Header("양손 잡는 위치")]
    [Tooltip("기존 왼손 변형 본입니다. 본의 크기는 변경하지 않습니다.")]
    public Transform leftHand;
    [Tooltip("기존 오른손 변형 본입니다. 본의 크기는 변경하지 않습니다.")]
    public Transform rightHand;
    [Tooltip("샷건 루트 아래의 왼손 잡는 위치·회전 표시점입니다.")]
    public Transform leftGrip;
    [Tooltip("샷건 루트 아래의 오른손 잡는 위치·회전 표시점입니다.")]
    public Transform rightGrip;
    [Tooltip("켜면 표시점 회전에 손을 맞춥니다. 끄면 기존 애니메이션의 손 회전을 유지합니다.")]
    public bool matchGripRotation = true;

    public bool IsEquipped { get; private set; }
    public bool IsReady => isActiveAndEnabled && HasValidBindings();

    HandPose leftPose;
    HandPose rightPose;

    struct HandPose
    {
        public Transform hand;
        public Vector3 localPosition;
        public Quaternion localRotation;

        public void Capture(Transform source)
        {
            hand = source;
            localPosition = source.localPosition;
            localRotation = source.localRotation;
        }

        public void Restore()
        {
            if (hand != null)
            {
                hand.localPosition = localPosition;
                hand.localRotation = localRotation;
            }
            hand = null;
        }
    }

    void OnEnable()
    {
        IsEquipped = false;
        SetWeaponVisible(false);
    }

    void Update()
    {
        // 이전 프레임의 보정을 먼저 제거하고 Animator가 이번 프레임 자세를 계산하도록 합니다.
        RestoreHands();
    }

    /// <summary>준비된 모델과 자세를 켭니다. 입력·내려놓기 판단은 상호작용 연결부가 맡습니다.</summary>
    public bool TryEquip()
    {
        if (!IsReady) return false;
        if (IsEquipped) return true;
        IsEquipped = true;
        // 활성화 전에 위치를 맞춰 이전 위치에서 한 프레임 보이는 것을 막습니다.
        UpdateWeaponPose();
        SetWeaponVisible(true);
        return true;
    }

    /// <summary>다른 장비 전환·상태 종료를 위한 표시 해제 진입점입니다.</summary>
    public void Unequip()
    {
        IsEquipped = false;
        RestoreHands();
        SetWeaponVisible(false);
    }

    void LateUpdate()
    {
        if (!IsEquipped) return;
        if (!HasValidBindings())
        {
            Unequip();
            return;
        }
        UpdateWeaponPose();
        leftPose.Capture(leftHand);
        rightPose.Capture(rightHand);
        ApplyGrip(leftHand, leftGrip);
        ApplyGrip(rightHand, rightGrip);
    }

    bool HasValidBindings()
    {
        return weaponRoot != null && weaponRoot != transform && !transform.IsChildOf(weaponRoot) &&
            weaponRoot.parent != null && weaponRoot.parent.gameObject.activeInHierarchy &&
            motionRoot != null && leftHand != null && rightHand != null && leftHand != rightHand &&
            leftGrip != null && rightGrip != null && leftGrip.IsChildOf(weaponRoot) && rightGrip.IsChildOf(weaponRoot) &&
            !motionRoot.IsChildOf(weaponRoot) && !leftHand.IsChildOf(weaponRoot) && !rightHand.IsChildOf(weaponRoot) &&
            !weaponRoot.IsChildOf(leftHand) && !weaponRoot.IsChildOf(rightHand);
    }

    void UpdateWeaponPose()
    {
        weaponRoot.SetPositionAndRotation(motionRoot.TransformPoint(weaponLocalPosition),
            motionRoot.rotation * Quaternion.Euler(weaponLocalEulerAngles));
    }

    void ApplyGrip(Transform hand, Transform grip)
    {
        hand.position = grip.position;
        if (matchGripRotation) hand.rotation = grip.rotation;
    }

    void RestoreHands()
    {
        leftPose.Restore();
        rightPose.Restore();
    }

    void SetWeaponVisible(bool visible)
    {
        // 잘못 연결된 루트 때문에 플레이어 자신이나 부모가 비활성화되지 않도록 합니다.
        if (weaponRoot != null && weaponRoot != transform && !transform.IsChildOf(weaponRoot))
            weaponRoot.gameObject.SetActive(visible);
    }

    void OnDisable() => Unequip();
}
