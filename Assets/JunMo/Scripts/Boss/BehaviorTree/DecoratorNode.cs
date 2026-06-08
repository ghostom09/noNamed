using UnityEngine;

namespace BossSystem.BehaviorTree
{
    // ────────────────────────────────────────────────────────────────
    //  Decorator Nodes
    // ────────────────────────────────────────────────────────────────

    /// <summary>Success ↔ Failure 반전</summary>
    public class InverterNode : BTNode
    {
        private BTNode child;
        public InverterNode(BossBlackboard bb, BTNode child) : base(bb) => this.child = child;

        protected override NodeState OnEvaluate()
        {
            var result = child.Evaluate();
            if (result == NodeState.Running) return NodeState.Running;
            return result == NodeState.Success ? NodeState.Failure : NodeState.Success;
        }
    }

    /// <summary>
    /// 쿨다운이 끝났을 때만 자식 실행
    /// 자식이 Success → 쿨다운 시작
    /// </summary>
    public class CooldownNode : BTNode
    {
        private BTNode  child;
        private string  cooldownKey;
        private float   cooldownDuration;

        public CooldownNode(BossBlackboard bb, BTNode child, string key, float duration)
            : base(bb)
        {
            this.child            = child;
            this.cooldownKey      = key;
            this.cooldownDuration = duration;
        }

        protected override NodeState OnEvaluate()
        {
            if (blackboard.IsOnCooldown(cooldownKey))
                return NodeState.Failure;

            var result = child.Evaluate();
            if (result == NodeState.Success)
                blackboard.SetCooldown(cooldownKey, cooldownDuration);
            return result;
        }
    }

    /// <summary>조건 체크 전용 — Running 없이 즉시 Success/Failure</summary>
    public class ConditionNode : BTNode
    {
        private System.Func<bool> condition;

        public ConditionNode(BossBlackboard bb, System.Func<bool> condition) : base(bb)
            => this.condition = condition;

        protected override NodeState OnEvaluate()
            => condition() ? NodeState.Success : NodeState.Failure;
    }
}