using System;
using System.Collections.Generic;
using UnityEngine;

public enum NodeState { Success, Failure, Running }

public abstract class Node
{
    protected NodeState state;
    public abstract NodeState Evaluate();
}

public class Selector : Node
{
    protected List<Node> nodes;
    public Selector(List<Node> nodes) { this.nodes = nodes; }

    public override NodeState Evaluate()
    {
        foreach (var node in nodes)
        {
            switch (node.Evaluate())
            {
                case NodeState.Failure:  continue;
                case NodeState.Success:  state = NodeState.Success; return state;
                case NodeState.Running:  state = NodeState.Running; return state;
            }
        }
        state = NodeState.Failure;
        return state;
    }
}

public class Sequence : Node
{
    protected List<Node> nodes;
    public Sequence(List<Node> nodes) { this.nodes = nodes; }

    public override NodeState Evaluate()
    {
        foreach (var node in nodes)
        {
            switch (node.Evaluate())
            {
                case NodeState.Failure:  state = NodeState.Failure; return state;
                case NodeState.Running:  state = NodeState.Running; return state;
                case NodeState.Success:  continue;
            }
        }
        state = NodeState.Success;
        return state;
    }
}

public class Inverter : Node
{
    Node child;
    public Inverter(Node child) { this.child = child; }

    public override NodeState Evaluate()
    {
        switch (child.Evaluate())
        {
            case NodeState.Success: return NodeState.Failure;
            case NodeState.Failure: return NodeState.Success;
            default:                return NodeState.Running;
        }
    }
}

public class ConditionNode : Node
{
    Func<bool> condition;
    public ConditionNode(Func<bool> condition) { this.condition = condition; }

    public override NodeState Evaluate()
        => condition() ? NodeState.Success : NodeState.Failure;
}

public class ActionNode : Node
{
    Func<NodeState> action;
    public ActionNode(Func<NodeState> action) { this.action = action; }

    public override NodeState Evaluate() => action();
}
