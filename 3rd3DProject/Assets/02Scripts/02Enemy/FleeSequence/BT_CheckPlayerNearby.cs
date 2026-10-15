using UnityEngine;

public class BT_CheckPlayerNearby : BT_Leaf
{
    private ThiefBlackboard bb;

    public BT_CheckPlayerNearby(ThiefBlackboard bb)
    {
        this.bb = bb;
    }

    public override BT_NodeStatus Evaluate()
    {
        if(bb.IsPlayerNearby || bb.IsFleeing)
        {
            Debug.Log("돔황챠");
            return BT_NodeStatus.Success;
        }
        return BT_NodeStatus.Failure;
    }



}