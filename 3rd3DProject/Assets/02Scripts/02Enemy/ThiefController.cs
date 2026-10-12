using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ThiefController : MonoBehaviour
{
    [Header("AI 블랙보드")]
    [SerializeField] private ThiefBlackboard blackboard = new ThiefBlackboard();

    private BT_Node rootNode;


    //애니메이터 파라미터
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsCarryingHash = Animator.StringToHash("IsCarrying");
    private static readonly int IsStunnedHash = Animator.StringToHash("IsStunned");

    private void Awake()
    {
        blackboard.Initialize(gameObject);

        
        BuildBehaviorTree();
    }

    private void Update()
    {
        //Debug.Log($"NavMesh 연결 상태: {blackboard.Agent.isOnNavMesh}");
        blackboard.UpdatePerception();

        UpdateAnimation();

        if (rootNode != null )
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

        BT_Sequence returnSequence = new BT_Sequence();//우선순위 3] R2B시퀀스. 아이템을 들고있는데 훔치기가 우선순위가 높으면 안됨
        returnSequence.AddChild(new BT_MoveToBase(blackboard));
        returnSequence.AddChild(new BT_DispositItem(blackboard));
        rootSelector.AddChild(returnSequence);

        BT_Sequence stealSequence = new BT_Sequence();//우선순위 4]훔치기 시퀀스. 
        stealSequence.AddChild(new BT_FindFurtherItem(blackboard));
        stealSequence.AddChild(new BT_MoveToTarget(blackboard));
        stealSequence.AddChild(new BT_StealItem(blackboard));
        rootSelector.AddChild(stealSequence);
        //이 구간에는 행동트리 추가 가능

        rootNode = rootSelector;
    }

    private void UpdateAnimation()
    {
        if (blackboard.ThiefAnim == null) return;

        if(blackboard.Agent != null)
        {
            float currentSpeed = blackboard.Agent.velocity.magnitude;
            blackboard.ThiefAnim.SetFloat(SpeedHash, currentSpeed);
        }
        blackboard.ThiefAnim.SetBool(IsCarryingHash, blackboard.IsCarryingItem);

        blackboard.ThiefAnim.SetBool(IsStunnedHash, blackboard.IsStunned);
    }

    public void TakeStun(float duration)
    {
        blackboard.ApplyStun(duration);
    }
    public ThiefBlackboard Blackboard => blackboard;

    public bool IsCapturable => blackboard.IsStunned;
    public void GetCaptured()
    {
        //if (blackboard.IsCarryingItem)
        //    blackboard.DropCarriedItem();

        blackboard.HandleCaught();
        GameManager.Instance?.OnRatCaught();

        Debug.Log("[BT]도둑이 잡혔습니다");
        Destroy(gameObject);//풀링방식으로 변경할 시 비활성화 하기.
    }

}
