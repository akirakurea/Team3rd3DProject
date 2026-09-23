using UnityEngine;
using UnityEngine.AI;

public class BT_Flee : BT_Leaf
{
    private ThiefBlackboard bb;
    private float fleeDistance = 5.0f;
    public BT_Flee(ThiefBlackboard bb)
    {
        this.bb = bb;
    }

    public override BT_NodeStatus Evaluate()
    {

        if (!bb.IsPlayerNearby)
        {
            bb.Agent.ResetPath();//안전지역판단
            return BT_NodeStatus.Success;
        }
        if(bb.IsCarrytingItem && (bb.DistanceToPlayer < 5.0f))
        {
            DropCarriedItem();
        }
        Vector3 runDir = (bb.ThiefTransform.position - bb.PlayerTransform.position).normalized;
        Vector3 targetPos = bb.ThiefTransform.position + runDir * fleeDistance;

        NavMeshHit hit;
        if(NavMesh.SamplePosition(targetPos, out hit, 3.0f, NavMesh.AllAreas))//벽뚫방지
        {
            bb.Agent.SetDestination(hit.position);
        }
        else
        {
            bb.Agent.SetDestination(bb.ThiefTransform.position);
        }
        return BT_NodeStatus.Running;
    }

    private void DropCarriedItem()
    {
        if (bb.CarriedItem != null)
        
            {
                //도둑 발밑에 아이템 재배치
                //이때 아이템스크립트의 start가 자동실행, 아이템매니저에 재등록
                Object.Instantiate(bb.CarriedItem, bb.ThiefTransform.position, Quaternion.identity);
            }

        bb.IsCarrytingItem = false;
        Debug.Log("[BT]플레이어가 다가와 들고있던 아이템을 바닥에 버리고 도망칩니다.");
    }
}