using System.Collections.Generic;
using UnityEngine;

public enum EnemyAnimationType
{
    Idle,
    Walk,
    Attack,
    Die
}

public class EnemyAnimation : MonoBehaviour
{
    [SerializeField] private Animator animator;

    private readonly Dictionary<EnemyAnimationType, int> _stateHashes = new()
    {
        { EnemyAnimationType.Idle, Animator.StringToHash("Idle") },
        { EnemyAnimationType.Walk, Animator.StringToHash("Walk") },
        { EnemyAnimationType.Attack, Animator.StringToHash("Attack") },
        { EnemyAnimationType.Die, Animator.StringToHash("Die") }
    };

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    public void Play(EnemyAnimationType animationType)
    {
        if (animator == null)
        {
            Debug.LogWarning($"{name}: Animator가 연결되어 있지 않습니다.", this);
            return;
        }

        animator.Play(_stateHashes[animationType]);
    }
}
