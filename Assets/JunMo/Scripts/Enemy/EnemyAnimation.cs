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
    private EnemyAnimationType? _currentAnimation;
    private bool _deathLocked;

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

        if (_deathLocked && animationType != EnemyAnimationType.Die)
            return;

        if (_currentAnimation == animationType && animationType != EnemyAnimationType.Attack)
            return;

        _currentAnimation = animationType;
        if (animationType == EnemyAnimationType.Die)
            _deathLocked = true;

        animator.Play(_stateHashes[animationType], 0, 0f);
    }

    public bool IsPlaying(EnemyAnimationType animationType)
    {
        if (animator == null)
            return false;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return stateInfo.shortNameHash == _stateHashes[animationType];
    }

    public bool IsFinished(EnemyAnimationType animationType)
    {
        if (!IsPlaying(animationType))
            return true;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        return !stateInfo.loop && stateInfo.normalizedTime >= 1f;
    }
}
