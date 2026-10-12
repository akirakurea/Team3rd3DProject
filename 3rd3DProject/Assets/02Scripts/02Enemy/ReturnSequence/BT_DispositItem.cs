using UnityEngine;

public class BT_DispositItem : BT_Leaf
{
    private ThiefBlackboard bb;

    public BT_DispositItem(ThiefBlackboard bb)
    {
        this.bb = bb;
    }

    public override BT_NodeStatus Evaluate()
    {
        if(bb.CarriedItem != null)
        {
            Debug.Log("[BT]탈출구 도착. 아이템을 기지로 돌려보냅니다");

            bb.CompletEscape();
            //Object.Destroy(bb.CarriedItem);
            
            //bb.CarriedItem = null;
            //bb.IsCarryingItem = false;
        }
        return BT_NodeStatus.Success;
    }
}