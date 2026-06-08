using UnityEngine;

namespace BossSystem.BehaviorTree
{
    /// <summary>Success ↔ Failure 반전</summary>
    public class InverterNode : BTNode
    {
        private BTNode child;
        public InverterNode(BossBlackboard bb, BTNode child) : base(bb)
            => this.child = child;

        protected override NodeState OnEvaluate()
        {
            var result = child.Evaluate();
            if (result == NodeState.Running) return NodeState.Running;
            return result == NodeState.Success ? NodeState.Failure : NodeState.Success;
        }
    }

    /// <summary>
    /// 쿨다운 데코레이터
    ///
    /// 수정:
    ///  - 자식이 Running → Success/Failure로 전환되는 시점(패턴 첫 진입)에
    ///    child.OnEnter()를 호출하지 않아서 ChargeNode.phase가 리셋되지 않는 문제 수정
    ///  - 자식이 Failure/Success로 완전히 끝났다가 다시 쿨다운이 풀려 재진입할 때
    ///    OnEnter()를 호출해 패턴 노드 상태를 초기화
    /// </summary>
    public class CooldownNode : BTNode
    {
        private BTNode child;
        private string cooldownKey;
        private float  cooldownDuration;

        // 이전 프레임 상태 추적 — Failure/Success → Idle 전환 감지
        private bool wasRunning = false;

        public CooldownNode(BossBlackboard bb, BTNode child, string key, float duration)
            : base(bb)
        {
            this.child            = child;
            this.cooldownKey      = key;
            this.cooldownDuration = duration;
        }

        protected override NodeState OnEvaluate()
        {
            // 쿨다운 중이면 즉시 Failure
            if (blackboard.IsOnCooldown(cooldownKey))
            {
                // 쿨다운으로 인해 실행 안 될 때 wasRunning 리셋
                wasRunning = false;
                return NodeState.Failure;
            }

            // ★ 직전까지 Running이 아니었으면 (새로 진입) OnEnter 호출
            //   → ChargeNode, MeleeSmashNode 등의 phase를 Idle로 리셋
            if (!wasRunning)
            {
                child.OnEnter();
            }

            var result = child.Evaluate();

            if (result == NodeState.Running)
            {
                wasRunning = true;
                return NodeState.Running;
            }

            // Success 또는 Failure — 패턴 종료
            wasRunning = false;

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