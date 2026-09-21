using UnityEngine;

public class BT_FindFurtherItem : BT_Leaf
{
    private ThiefBlackboard blackboard;

    public BT_FindFurtherItem(ThiefBlackboard blackboard)
    {
        this.blackboard = blackboard;
    }

    public override BT_NodeStatus Evaluate()
    {
        var items = ItemManager2.Instance.GetRemainingItems();
        if (items == null || items.Count == 0)
        {
            Debug.LogWarning("[BT] 아이템매니저 비어있음");
            return BT_NodeStatus.Failure;
        }

        Transform player = blackboard.PlayerTransform;
        Transform thief = blackboard.ThiefTransform;
        if (player == null)
        {
            Debug.LogWarning("[BT] 플레이어 비어있음");
            return BT_NodeStatus.Failure;
        }

        Transform furherItem = null;
        float maxDistance = -1f;
       
        foreach(var item in items)
        {
            if(item == null) continue;
            float distanceToPlayer = Vector3.Distance(item.transform.position, player.position);
            float distanceToThiedf = Vector3.Distance(item.transform.position, thief.position);

            //플레이어와 멀수록 점수가 올라감 && 도둑과 가까울수록 점수가 더 크게 올라감
            float score = distanceToPlayer / (distanceToThiedf + 0.1f); 

            if(score > maxDistance)
            {
                maxDistance = score;
                furherItem = item.transform; 
            }
        }
        if(furherItem != null)
        {
            blackboard.TargetItem = furherItem;
            Debug.Log($"[BT]아이템 찾기 성공 : {furherItem.name}");
            return BT_NodeStatus.Success;
        }
        Debug.LogWarning($"[BT]아이템 찾기 실패(리스트 or Player Null");
        return BT_NodeStatus.Failure;
    }
}
