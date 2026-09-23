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
        Debug.Log($"[BT]아이템 훔치기 시작{currentTimer}초");
        currentTimer += Time.deltaTime;
        if (currentTimer >= stealDuration)
        {
            Object.Destroy(blackboard.TargetItem.gameObject);
            blackboard.TargetItem = null;

            currentTimer = 0f;
            isNotifying = false;
            Debug.Log("[BT] 아이템 훔치기 완료");
            return BT_NodeStatus.Success;


        }
        return BT_NodeStatus.Running;
    }



}