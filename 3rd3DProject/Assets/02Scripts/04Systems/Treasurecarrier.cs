using UnityEngine;

/// <summary>
/// 쥐(도둑) 프리펩에 붙이는 "보물 운반" 컴포넌트. 쥐 AI(BT)는 아래 메서드만 호출하면 된다.
/// HUD, 점수, 라운드 승패는 GameManager가 처리하므로 쥐 쪽에서는 신경 쓰지 않는다.
///
///   TryPickUp(Treasure)  : 훔치기 동작이 끝난 시점에 호출. 먼저 집은 쥐만 true, 이미 집혔으면 false
///                          -> false면 목표를 비우고 다른 보물을 찾는다 (BT_StealItem에서 Failure 반환)
///   TryPickUpNearest()   : pickupRange 안의 가장 가까운 보물을 집는다. 내부 순회가 있으니 목표 근처에서만 호출
///   CompleteEscape()     : 탈출 지점 도달 시 호출 -> 보물이 털린 것으로 확정 (BT_DispositItem)
///   DropOnCaught()       : 잡혔거나 도망 중 보물을 놓을 때 호출 -> 보물이 원래 자리로 복귀
///                          (잡힌 것 자체는 GameManager.Instance.OnRatCaught()를 따로 호출)
///   IsCarrying           : 보물을 들고 있는지. 블랙보드의 IsCarryingItem 대신 이 값을 읽는다
///
/// 목표 보물 고르기: TreasureManager.Instance.Treasures 순회 또는 GetNearestAvailable(위치)
/// 주의: 예약(IsReserved) 개념은 없다. 이동/훔치는 동안 target.IsAvailable을 확인해 가로채이면 목표를 비운다.
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