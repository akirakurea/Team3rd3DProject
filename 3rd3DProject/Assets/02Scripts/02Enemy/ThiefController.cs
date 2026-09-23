using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ThiefController : MonoBehaviour
{
    [Header("AI 블랙보드")]
    [SerializeField] private ThiefBlackboard blackboard = new ThiefBlackboard();

    private BT_Node rootNode;

    private void Awake()
    {
        blackboard.Initailize(gameObject);

        
        BuildBehaviorTree();
    }

    private void Update()
    {
        //Debug.Log($"NavMesh 연결 상태: {blackboard.Agent.isOnNavMesh}");
        blackboard.UpdatePerception();

        if(rootNode != null )
            rootNode.Evaluate();
    }

    private void BuildBehaviorTree()
    {
        BT_Selector rootSelector = new BT_Selector();

        BT_StunCheck stunCheckNode = new BT_StunCheck(blackboard);//우선순위1 ]함정밟은 상태를 가장 먼저 체크하기 
        rootSelector.AddChild(stunCheckNode);

        BT_Sequence fleeSequence = new BT_Sequence();//우선순위2]도망치기
        fleeSequence.AddChild(new BT_CheckPlayerNearby(blackboard));
        fleeSequence.AddChild(new BT_Flee(blackboard));
        rootSelector.AddChild(fleeSequence);

        BT_Sequence stealSequence = new BT_Sequence();//우선순위 3]훔치기 시퀀스. 
        stealSequence.AddChild(new BT_FindFurtherItem(blackboard));
        stealSequence.AddChild(new BT_MoveToTarget(blackboard));
        stealSequence.AddChild(new BT_StealItem(blackboard));
        rootSelector.AddChild(stealSequence);
        //이 구간에는 행동트리 추가 가능

        rootNode = rootSelector;
    }
    public ThiefBlackboard Blackboard => blackboard;

}
