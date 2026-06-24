using System.Collections.Generic;
using UnityEngine;

namespace BossSystem.Boss
{
    public enum BossAnimationType { Idle, Walk, Attack, Die }

    public class BossAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;

        private readonly Dictionary<BossAnimationType, int> stateHashes = new()
        {
            { BossAnimationType.Idle, Animator.StringToHash("Idle") },
            { BossAnimationType.Walk, Animator.StringToHash("Walk") },
            { BossAnimationType.Attack, Animator.StringToHash("Attack") },
            { BossAnimationType.Die, Animator.StringToHash("Die") }
        };

        private BossAnimationType? currentAnimation;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        public void Play(BossAnimationType animationType, bool restart = false)
        {
            if (animator == null)
            {
                Debug.LogWarning($"{name}: Animator가 연결되어 있지 않습니다.", this);
                return;
            }

            if (!restart && currentAnimation == animationType)
                return;

            currentAnimation = animationType;
            animator.Play(stateHashes[animationType], 0, 0f);
        }
    }
}
