using UnityEngine;

/// <summary>
/// "집 안" 영역을 표시하는 박스. 빈 오브젝트에 붙이고 Size를 방1-거실-방2 내부 전체를 덮도록 맞춘다.
/// 콜라이더가 아니라 단순 영역 데이터라서 물리/NavMesh/팀원의 OnTrigger 코드에 영향을 주지 않는다.
/// TreasureManager는 이 영역 안에 있는 TreasureSpawnPoint만 사용한다.
/// Scene 뷰에서 초록색 박스로 보인다. (높이는 바닥~천장을 충분히 덮게)
/// </summary>
[ExecuteAlways]
public class HouseZone : MonoBehaviour
{
    public static HouseZone Current { get; private set; }

    [SerializeField] private Vector3 center = new Vector3(0f, 1.5f, 0f);
    [SerializeField] private Vector3 size = new Vector3(20f, 3f, 8f);

    private void OnEnable()
    {
        Current = this;
    }

    private void OnDisable()
    {
        if (Current == this) Current = null;
    }

    /// <summary>월드 좌표가 집 안(박스 내부)인지. 오브젝트를 회전/스케일해도 동작한다.</summary>
    public bool Contains(Vector3 worldPoint)
    {
        Vector3 local = transform.InverseTransformPoint(worldPoint) - center;
        Vector3 half = size * 0.5f;
        return Mathf.Abs(local.x) <= half.x
            && Mathf.Abs(local.y) <= half.y
            && Mathf.Abs(local.z) <= half.z;
    }

    private void OnDrawGizmos()
    {
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.9f);
        Gizmos.DrawWireCube(center, size);
    }
}