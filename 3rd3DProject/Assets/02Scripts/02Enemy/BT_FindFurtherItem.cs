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
        if (items.Count == 0) return BT_NodeStatus.Failure;

        Transform player = blackboard.PlayerTransform;
        if(player == null)  return BT_NodeStatus.Failure;

        Transform furherItem = null;
        float maxDistance = -1f;

        foreach(var item in items)
        {
            if(item == null) continue;
            float distanceToPlayer = Vector3.Distance(item.transform.position, player.position);
            if(distanceToPlayer > maxDistance)
            {
                maxDistance = distanceToPlayer;
                furherItem = item.transform; 
            }
        }
        if(furherItem != null)
        {
            blackboard.TargetItem = furherItem;
            return BT_NodeStatus.Success;
        }
        return BT_NodeStatus.Failure;
    }
}
