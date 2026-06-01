using UnityEngine;
using System.Collections.Generic;

namespace BossSystem.BehaviorTree
{
    public enum NodeState { Running, Success, Failure }

    /// <summary>모든 BT 노드의 기반 클래스</summary>
    public abstract class BTNode
    {
        protected NodeState state;
        public NodeState State => state;
        protected BossBlackboard blackboard;

        public BTNode(BossBlackboard blackboard) => this.blackboard = blackboard;

        public NodeState Evaluate()
        {
            state = OnEvaluate();
            return state;
        }

        protected abstract NodeState OnEvaluate();
        public virtual void OnEnter() { }
        public virtual void OnExit()  { }
    }
}