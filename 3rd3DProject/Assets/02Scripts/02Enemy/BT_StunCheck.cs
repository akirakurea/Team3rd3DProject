using UnityEngine;

public class BT_StunCheck : BT_Leaf
{
    private ThiefBlackboard blackboard;

    public BT_StunCheck(ThiefBlackboard blackboard)
    {
        this.blackboard = blackboard;
    }

    public override BT_NodeStatus Evaluate()
    {
        if (!blackboard.IsStunned) 
            return BT_NodeStatus.Failure;//스턴이 아니니 다음 행동으로

        blackboard.CurrentStunTimer += Time.deltaTime;//스턴 상태 처리

        if (blackboard.Agent != null && blackboard.Agent.isOnNavMesh) 
            blackboard.Agent.isStopped = true;//이동중이었으면 멈추기

        if(blackboard.CurrentStunTimer >= blackboard.StunDuration) //시간 다 지나면
        {
            blackboard.IsStunned = false;
            blackboard.CurrentStunTimer = 0f;

            if (blackboard.Agent != null && blackboard.Agent.isOnNavMesh)
                blackboard.Agent.isStopped = false;//이동멈춤도 해제

            return BT_NodeStatus.Failure;//상태 해제하고 다음행동으로
        }
        return BT_NodeStatus.Success;//스턴시간중에는 계속 스턴상태반환
    }

}