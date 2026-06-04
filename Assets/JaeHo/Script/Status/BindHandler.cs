using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Health))]
public class BindHandler : MonoBehaviour, IBindable
{
    public bool IsBound { get; private set; }

    private Health _health;
    private Coroutine _bindCoroutine;
    private float _bindEndsAt;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    public void ApplyBind(float duration, float damagePerTick, float tickInterval)
    {
        if (duration <= 0f) return;

        if (_bindCoroutine != null)
            StopCoroutine(_bindCoroutine);

        _bindEndsAt = Time.time + duration;
        _bindCoroutine = StartCoroutine(BindRoutine(damagePerTick, Mathf.Max(0f, tickInterval)));
    }

    private IEnumerator BindRoutine(float damagePerTick, float tickInterval)
    {
        IsBound = true;

        if (damagePerTick <= 0f || tickInterval <= 0f)
        {
            yield return new WaitForSeconds(Mathf.Max(0f, _bindEndsAt - Time.time));
        }
        else
        {
            WaitForSeconds wait = new WaitForSeconds(tickInterval);

            while (Time.time < _bindEndsAt)
            {
                yield return wait;

                if (_health.IsDead) break;
                _health.TakeDamage(damagePerTick);
            }
        }

        IsBound = false;
        _bindCoroutine = null;
    }

    private void OnDisable()
    {
        if (_bindCoroutine != null)
        {
            StopCoroutine(_bindCoroutine);
            _bindCoroutine = null;
        }

        IsBound = false;
        _bindEndsAt = 0f;
    }
}
