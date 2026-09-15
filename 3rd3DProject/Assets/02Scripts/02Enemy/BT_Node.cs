public enum BT_NodeStatus
{
    Success,
    Failure,
    Running
}

public abstract class BT_Node
{

    public abstract BT_NodeStatus Evaluate();
}