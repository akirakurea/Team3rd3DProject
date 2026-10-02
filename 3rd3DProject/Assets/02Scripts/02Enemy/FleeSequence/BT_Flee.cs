using UnityEngine;
using UnityEngine.AI;

public class BT_Flee : BT_Leaf
{
    private ThiefBlackboard bb;
    private float fleeDistance = 6.0f;
    private float safeDistance = 14.0f;
    private float minFleeDuration = 1.5f;
    private float currentFleeTimer = 0f;

    private float repathInterval = 0.4f;
    private float currentRepathTimer = 0f;
    public BT_Flee(ThiefBlackboard bb)
    {
        this.bb = bb;
    }

    public override BT_NodeStatus Evaluate()
    {
        if(!bb.IsFleeing)
        {
            currentFleeTimer = 0f;
            currentRepathTimer = repathInterval;
            bb.IsFleeing = true;
        }

        currentFleeTimer += Time.deltaTime;
        currentRepathTimer += Time.deltaTime;
        if (bb.DistanceToPlayer >= safeDistance && currentFleeTimer >= minFleeDuration)
        {   
            //히스테리시스 기법. 플레이어가 근처에 있는지 매 프레임 확인해 진동하지 않게 하기
            currentFleeTimer = 0f;
            bb.Agent.ResetPath();//안전지역판단
            Debug.Log("14미터 도망쳤다");
            bb.IsFleeing = false;
            return BT_NodeStatus.Success;
        }
        
        bb.TargetItem = null;//도망칠때는 노리던 아이템 초기화 장치

        if(bb.IsCarryingItem && (bb.DistanceToPlayer < 5.0f))
        {
            bb.DropCarriedItem();
        }
        //Vector3 runDir = (bb.ThiefTransform.position - bb.PlayerTransform.position).normalized;
        //Vector3 targetPos = bb.ThiefTransform.position + runDir * fleeDistance;
        //NavMeshHit hit;
        //if(NavMesh.SamplePosition(targetPos, out hit, 3.0f, NavMesh.AllAreas))//벽뚫방지
        //{
        //    bb.Agent.SetDestination(hit.position);
        //}
        //else
        //{
        //    bb.Agent.SetDestination(bb.ThiefTransform.position);
        //}
        
        if (!bb.Agent.hasPath || 
            bb.Agent.remainingDistance <= 1.5f ||
            currentRepathTimer >= repathInterval)
        {
            currentRepathTimer = 0f;

            Vector3 bestFleePos = FindBestFleePosition();

            bb.Agent.SetDestination(bestFleePos);
        }
        return BT_NodeStatus.Running;
    }

    private Vector3 FindBestFleePosition()
    {
        Vector3 awayFromPlayer = (bb.ThiefTransform.position - bb.PlayerTransform.position).normalized;
        Vector3 bestPos = bb.ThiefTransform.position;
        float maxDistFromPlayer = -1f;

        float[] checkAngles = { 0f, 45f, -45f, 90f, -90f, 135f, -135f, 180f };
        
        foreach(float angle in checkAngles)
        {
            Vector3 checkDir = Quaternion.Euler(0, angle, 0) * awayFromPlayer;
            Vector3 targetCandidate = bb.ThiefTransform.position + checkDir * fleeDistance;

            NavMeshHit hit;

            if (!NavMesh.Raycast(bb.ThiefTransform.position, targetCandidate, out hit, NavMesh.AllAreas))
            {
                float distToPlayer = Vector3.Distance(targetCandidate, bb.PlayerTransform.position);
                if(distToPlayer > maxDistFromPlayer)
                {
                    maxDistFromPlayer = distToPlayer;
                    bestPos = targetCandidate;
                }
            }
            else
            {
                if(hit.distance > 2.0f)
                {
                    float distToPlayer = Vector3.Distance(hit.position, bb.PlayerTransform.position);
                    if(distToPlayer > maxDistFromPlayer)
                    {
                        maxDistFromPlayer = distToPlayer;
                        bestPos = hit.position;
                    }
                }
            }

        }
        if (bestPos == bb.ThiefTransform.position && bb.SpawnPosition != null)
        {
            return bb.SpawnPosition.position;
        }
        return bestPos;
    }

    
}