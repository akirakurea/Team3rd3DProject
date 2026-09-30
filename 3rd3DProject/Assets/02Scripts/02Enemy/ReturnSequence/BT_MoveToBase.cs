using UnityEngine;

public class BT_MoveToBase : BT_Leaf
{
    private ThiefBlackboard bb;

    public BT_MoveToBase(ThiefBlackboard bb)
    {
        this.bb = bb;
    }

    public override BT_NodeStatus Evaluate()
    {
        if (!bb.IsCarryingItem || bb.SpawnPosition == null) return BT_NodeStatus.Failure;

        bb.Agent.SetDestination(bb.SpawnPosition.position);

        float distance = Vector3.Distance(bb.ThiefTransform.position, bb.SpawnPosition.position);
        if(distance <= 1.2f)
        {
            bb.Agent.ResetPath();
            return BT_NodeStatus.Success;
        }

        return BT_NodeStatus.Running;
    }
}