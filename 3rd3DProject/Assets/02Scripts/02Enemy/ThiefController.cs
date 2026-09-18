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
        Debug.Log($"NavMesh 연결 상태: {blackboard.Agent.isOnNavMesh}");
        blackboard.UpdatePerception();

        if(rootNode != null )
            rootNode.Evaluate();
    }

    private void BuildBehaviorTree()
    {
        BT_Selector rootSelector = new BT_Selector();

        BT_StunCheck stunCheckNode = new BT_StunCheck(blackboard);//함정밟은 상태를 가장 먼저 체크하기
        rootSelector.AddChild(stunCheckNode);

        BT_Sequence stealSequnce = new BT_Sequence();

        stealSequnce.AddChild(new BT_FindFurtherItem(blackboard));

        //이 구간에는 행동트리 추가 가능
        stealSequnce.AddChild(new BT_MoveToTarget(blackboard));

        rootSelector.AddChild(stealSequnce);

        rootNode = rootSelector;
    }
    public ThiefBlackboard Blackboard => blackboard;

}
