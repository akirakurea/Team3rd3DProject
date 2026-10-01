using UnityEngine;

/// <summary>
/// 쥐 오브젝트에 붙이는 "보물 운반" 컴포넌트. 쥐 스크립트를 만드는 팀원은 아래 메서드만 호출하면 된다.
///
///   - TryPickUpNearest()  : 주변(pickupRange)의 보물을 집는다. 성공하면 true
///                           (내부에서 보물 전체를 순회하므로, 매 프레임보다는 목표 근처일 때만 호출하는 것을 권장)
///   - CompleteEscape()    : 탈출 지점에 도달했을 때 호출 -> 보물이 털린 것으로 확정 (GameManager에 자동 통보)
///   - DropOnCaught()      : 고양이에게 잡혔을 때 호출 -> 보물이 제자리로 복귀
///                           (쥐가 잡힌 것 자체는 GameManager.Instance.OnRatCaught()를 따로 호출)
///
/// 목표 보물을 고를 때는 TreasureManager.Instance.GetNearestAvailable(위치)를 쓰면 된다.
/// 여러 쥐가 같은 보물을 노려도 먼저 집은 쥐만 성공하고 나머지는 false를 받는다.
/// </summary>
public class TreasureCarrier : MonoBehaviour
{
    [SerializeField] private Transform carryPoint;        // 보물이 붙을 위치 (입/등). 비우면 쥐 자신
    [SerializeField, Min(0.1f)] private float pickupRange = 1.2f;

    private Treasure carried;
    private bool quitting;

    public Transform CarryPoint => carryPoint != null ? carryPoint : transform;

    // 다른 곳에서 보물이 초기화/복귀된 경우(예: 재시작)를 대비해 실제 상태까지 확인
    public bool IsCarrying => carried != null && carried.Carrier == this && carried.State == Treasure.TreasureState.Carried;
    public Treasure Carried => IsCarrying ? carried : null;

    public bool TryPickUp(Treasure treasure)
    {
        if (treasure == null || IsCarrying) return false;
        if (!treasure.PickUp(this)) return false;
        carried = treasure;
        return true;
    }

    public bool TryPickUpNearest()
    {
        if (IsCarrying || TreasureManager.Instance == null) return false;

        Treasure nearest = TreasureManager.Instance.GetNearestAvailable(transform.position);
        if (nearest == null) return false;

        // Vector3.Distance(제곱근 계산) 대신 제곱 거리로 비교
        float sqr = (nearest.transform.position - transform.position).sqrMagnitude;
        if (sqr > pickupRange * pickupRange) return false;

        return TryPickUp(nearest);
    }

    /// <summary>탈출 성공. 보물을 들고 있었으면 털린 것으로 확정하고 true.</summary>
    public bool CompleteEscape()
    {
        if (!IsCarrying) return false;
        Treasure t = carried;
        carried = null;
        t.Steal();
        return true;
    }

    /// <summary>잡혔을 때 호출. 들고 있던 보물은 제자리로 돌아간다.</summary>
    public void DropOnCaught()
    {
        if (IsCarrying) carried.ReturnHome();
        carried = null;
    }

    private void OnApplicationQuit()
    {
        quitting = true;
    }

    // 안전장치: 팀원 코드가 DropOnCaught()를 빼먹고 쥐를 비활성화/삭제해도 보물이 사라지지 않게 복귀시킨다
    private void OnDisable()
    {
        if (quitting || !gameObject.scene.isLoaded) return;
        DropOnCaught();
    }
}