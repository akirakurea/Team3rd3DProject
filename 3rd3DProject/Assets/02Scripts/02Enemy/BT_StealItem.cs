using UnityEngine;

public class BT_StealItem : BT_Leaf
{
    private ThiefBlackboard blackboard;
    private float stealDuration = 2.5f;
    private float currentTimer = 0f;
    private bool isNotifying = false;

    public BT_StealItem(ThiefBlackboard blackboard)
    {
        this.blackboard = blackboard;
    }
    public override BT_NodeStatus Evaluate()
    {
        if (blackboard.TargetItem == null) return BT_NodeStatus.Failure;

        if(!isNotifying)//훔치기 시작
        {
            isNotifying = true;
            //AlertSystem.TriggerStealAlert(blackboard.TargetItem.position);
            //경보시스템 스크립트 필요
        }

        currentTimer += Time.deltaTime;
        if (currentTimer >= stealDuration)
        {
            ItemManager2.Instance.RemoveItem(blackboard.TargetItem);
            blackboard.TargetItem = null;

            currentTimer = 0f;
            isNotifying = false;
            return BT_NodeStatus.Success;


        }
        return BT_NodeStatus.Running;
    }



}