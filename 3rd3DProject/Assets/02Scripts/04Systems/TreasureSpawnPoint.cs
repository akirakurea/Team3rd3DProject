using UnityEngine;

/// <summary>
/// 보물이 놓일 수 있는 위치 표시용 마커. 빈 오브젝트에 붙여서 방/거실 곳곳에 배치한다.
/// TreasureManager가 씬에서 자동으로 찾아서, 라운드마다 이 중 일부를 무작위로 골라 보물을 놓는다.
/// (라운드당 보물 수보다 넉넉하게, 예: 8~10곳 배치하면 라운드마다 배치가 달라진다)
/// HouseZone 밖에 있는 포인트는 사용되지 않으며, Scene 뷰에서 빨간색으로 표시된다.
/// </summary>
public class TreasureSpawnPoint : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        bool outside = HouseZone.Current != null && !HouseZone.Current.Contains(transform.position);
        Gizmos.color = outside
            ? new Color(1f, 0.2f, 0.2f, 0.9f)    // 집 밖: 빨강 (사용 안 됨)
            : new Color(1f, 0.85f, 0.1f, 0.9f);  // 집 안: 금색

        Vector3 center = transform.position + Vector3.up * 0.25f;
        Gizmos.DrawSphere(center, 0.2f);
        Gizmos.DrawWireSphere(center, 0.3f);
    }
}