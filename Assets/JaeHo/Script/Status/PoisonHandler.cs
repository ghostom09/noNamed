using System.Collections;
using UnityEngine;

/// <summary>
/// IPoisonable 구현체 레퍼런스.
/// IDamageable(Health)과 함께 붙여서 사용.
/// 독이 중첩으로 들어오면 더 강한 쪽 스탯으로 타이머를 갱신.
///
/// 사용법:
///   같은 GameObject에 Health, PoisonHandler 컴포넌트를 추가하면 끝.
/// </summary>
[RequireComponent(typeof(Health))]
public class PoisonHandler : MonoBehaviour, IPoisonable
{
    // 현재 독이 걸려있는지 외부에서 확인용 (UI 등)
    public bool IsPoisoned { get; private set; }

    private Health _health;
    private Coroutine _poisonCoroutine;

    // 현재 활성 독 스탯 (중첩 비교용)
    private float _currentDamagePerTick;
    private float _currentDuration;
    private float _currentTickInterval;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    public void ApplyPoison(float damagePerTick, float duration, float tickInterval)
    {
        bool isStronger = damagePerTick > _currentDamagePerTick ||
                          tickInterval < _currentTickInterval;

        if (_poisonCoroutine != null)
        {
            // 중첩: 더 강하거나 오래 지속되는 쪽으로 갱신
            if (!isStronger && duration <= _currentDuration) return;
            StopCoroutine(_poisonCoroutine);
        }

        _currentDamagePerTick = damagePerTick;
        _currentDuration      = duration;
        _currentTickInterval  = tickInterval;

        _poisonCoroutine = StartCoroutine(PoisonRoutine(damagePerTick, duration, tickInterval));
    }

    private IEnumerator PoisonRoutine(float damagePerTick, float duration, float tickInterval)
    {
        IsPoisoned = true;

        float elapsed = 0f;
        var tick = new WaitForSeconds(tickInterval);

        while (elapsed < duration)
        {
            yield return tick;
            elapsed += tickInterval;

            if (_health.IsDead) break;
            _health.TakeDamage(damagePerTick);
        }

        IsPoisoned = false;
        _poisonCoroutine = null;

        _currentDamagePerTick = 0f;
        _currentDuration      = 0f;
        _currentTickInterval  = 0f;
    }

    private void OnDisable()
    {
        if (_poisonCoroutine != null)
        {
            StopCoroutine(_poisonCoroutine);
            _poisonCoroutine = null;
        }
        IsPoisoned = false;
        _currentDamagePerTick = 0f;
        _currentDuration      = 0f;
        _currentTickInterval  = 0f;
    }
}