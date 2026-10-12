using UnityEngine;

public class BT_MoveToTarget : BT_Leaf
{
    private ThiefBlackboard bb;

    public BT_MoveToTarget(ThiefBlackboard bb)
    { this.bb = bb; }

    public override BT_NodeStatus Evaluate()
    {
        if (bb.TargetItem == null) return BT_NodeStatus.Failure;

        if(!bb.TargetItem.IsAvailable)
        {
            bb.ClearTarget();
            return BT_NodeStatus.Failure;
        }

        Vector3 targetPos = bb.TargetItem.transform.position;
        bb.Agent.SetDestination(targetPos);
        float disToTgt = Vector3.Distance(bb.ThiefTransform.position, targetPos);
        Debug.Log($"[BT] {targetPos}방향 이동");
        if (disToTgt<= 1.2f || (!bb.Agent.pathPending && bb.Agent.remainingDistance <= bb.Agent.stoppingDistance))
        {
            bb.Agent.ResetPath();
            return BT_NodeStatus.Success;//이동완료
        }

        return BT_NodeStatus.Running;//이동중
    }
}