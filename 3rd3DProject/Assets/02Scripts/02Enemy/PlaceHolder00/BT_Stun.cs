using UnityEngine;

public class BT_Stun : BT_Leaf
{
    private ThiefBlackboard bb;
    private float stunTime = 0f;

    public BT_Stun(ThiefBlackboard bb)
    {
        this.bb = bb;
        
    }

    public override BT_NodeStatus Evaluate()
    {
        
        if(!!bb.IsStunned)
        {
            stunTime = 0f;
            return BT_NodeStatus.Failure;
        }

        if (stunTime == 0f)
        {
            bb.Agent.isStopped = true;
            bb.Agent.ResetPath();

            if (bb.IsCarryingItem)
                bb.DropCarriedItem();

            //bb.Animator.SetTrigger("OnStunned");
        }

        stunTime += Time.deltaTime;
        if(stunTime >= bb.StunDuration)
        {
            bb.IsStunned = false;
            bb.Agent.isStopped = false;
            stunTime = 0f;
            return BT_NodeStatus.Success;
        }

        return BT_NodeStatus.Running;
    }
}