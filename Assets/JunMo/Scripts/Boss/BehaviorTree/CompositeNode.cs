using System.Collections.Generic;

namespace BossSystem.BehaviorTree
{
    // ────────────────────────────────────────────────────────────────
    //  Composite Nodes
    // ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Selector (OR): 자식 중 하나라도 Success면 즉시 Success
    /// 우선순위 높은 행동부터 순서대로 시도
    /// </summary>
    public class SelectorNode : BTNode
    {
        private List<BTNode> children = new List<BTNode>();

        public SelectorNode(BossBlackboard bb) : base(bb) { }

        public SelectorNode AddChild(BTNode node) { children.Add(node); return this; }

        protected override NodeState OnEvaluate()
        {
            foreach (var child in children)
            {
                var result = child.Evaluate();
                if (result == NodeState.Running) return NodeState.Running;
                if (result == NodeState.Success) return NodeState.Success;
            }
            return NodeState.Failure;
        }
    }

    /// <summary>
    /// Sequence (AND): 모든 자식이 Success여야 Success
    /// Running 상태인 자식 인덱스를 유지 → 이어서 평가
    /// </summary>
    public class SequenceNode : BTNode
    {
        private List<BTNode> children = new List<BTNode>();
        private int currentIndex = 0;   // Running 중인 자식 위치 기억

        public SequenceNode(BossBlackboard bb) : base(bb) { }

        public SequenceNode AddChild(BTNode node) { children.Add(node); return this; }

        public override void OnEnter() => currentIndex = 0;

        protected override NodeState OnEvaluate()
        {
            // currentIndex부터 이어서 평가 (Running 유지)
            while (currentIndex < children.Count)
            {
                var result = children[currentIndex].Evaluate();

                if (result == NodeState.Running)
                    return NodeState.Running;          // 같은 자식을 다음 프레임에도 평가

                if (result == NodeState.Failure)
                {
                    currentIndex = 0;                  // 실패 시 리셋
                    return NodeState.Failure;
                }

                currentIndex++;                        // Success → 다음 자식
            }
            currentIndex = 0;
            return NodeState.Success;
        }
    }

    /// <summary>
    /// Parallel: 모든 자식을 동시에 실행
    /// requiredSuccess개 이상 Success → 전체 Success
    /// Running 중인 자식이 있으면 Running 유지
    /// </summary>
    public class ParallelNode : BTNode
    {
        private List<BTNode> children = new List<BTNode>();
        private int requiredSuccess;

        public ParallelNode(BossBlackboard bb, int requiredSuccess = -1) : base(bb)
            => this.requiredSuccess = requiredSuccess;

        public ParallelNode AddChild(BTNode node) { children.Add(node); return this; }

        protected override NodeState OnEvaluate()
        {
            int successCount = 0;
            bool anyRunning  = false;
            int req = requiredSuccess < 0 ? children.Count : requiredSuccess;

            foreach (var child in children)
            {
                var result = child.Evaluate();
                if (result == NodeState.Success) successCount++;
                if (result == NodeState.Running)  anyRunning = true;
            }

            if (successCount >= req) return NodeState.Success;
            if (anyRunning)          return NodeState.Running;
            return NodeState.Failure;
        }
    }
}