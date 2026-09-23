using UnityEngine;

public class BT_MoveToTarget : BT_Leaf
{
    private ThiefBlackboard bb;

    public BT_MoveToTarget(ThiefBlackboard bb)
    { this.bb = bb; }

    public override BT_NodeStatus Evaluate()
    {
        if (bb.TargetItem == null) return BT_NodeStatus.Failure;

        bb.Agent.SetDestination(bb.TargetItem.position);
        float disToTgt = Vector3.Distance(bb.ThiefTransform.position, bb.TargetItem.position);
        Debug.Log($"[BT] {bb.TargetItem.position}방향 이동");
        if (disToTgt<= 1.2f || (!bb.Agent.pathPending && bb.Agent.remainingDistance <= bb.Agent.stoppingDistance))
        {
            bb.Agent.ResetPath();
            return BT_NodeStatus.Success;//이동완료
        }

        return BT_NodeStatus.Running;//이동중
    }
}