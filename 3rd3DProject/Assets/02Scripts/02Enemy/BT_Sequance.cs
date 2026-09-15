using System.Collections.Generic;

public class BT_Sequance : BT_Node
{
    private List<BT_Node> children;

    public BT_Sequance(List<BT_Node> children)
    {
        this.children = children;
    }

    public override BT_NodeStatus Evaluate()
    {
        foreach(var node  in children)
        {
            var status = node.Evaluate();
            if(status == BT_NodeStatus.Failure)
            {
                return BT_NodeStatus.Failure;
            }
            else if (status == BT_NodeStatus.Running)
            {
                return BT_NodeStatus.Running;
            }
        }
        return BT_NodeStatus.Success;
    }
}
