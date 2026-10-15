using System.Collections.Generic;

public class BT_Selector : BT_Node
{
    private List<BT_Node> children =new List<BT_Node>();

    public BT_Selector() { }

    public BT_Selector(List<BT_Node> children)
    {
        this.children = children ?? new List<BT_Node>();
    }

    public void AddChild(BT_Node node)
    {
        children.Add(node);
    }
    public override BT_NodeStatus Evaluate()
    {
        foreach(var node in children)
        {
            var status = node.Evaluate();
            if(status == BT_NodeStatus.Success )
            {
                return BT_NodeStatus.Success;
            }
            else if (status == BT_NodeStatus.Running)
            {
                return BT_NodeStatus.Running;
            }
        }
        return BT_NodeStatus.Failure;
    }
}