using System.Collections;
using UnityEngine;

public class StunHandler : MonoBehaviour, IStunnable
{
    public bool IsStunned { get; private set; }

    private Coroutine _stunCoroutine;
    private float _stunEndsAt;

    public void ApplyStun(float duration)
    {
        if (duration <= 0f) return;

        _stunEndsAt = Mathf.Max(_stunEndsAt, Time.time + duration);

        if (_stunCoroutine == null)
            _stunCoroutine = StartCoroutine(StunRoutine());
    }

    private IEnumerator StunRoutine()
    {
        IsStunned = true;

        while (Time.time < _stunEndsAt)
            yield return null;

        IsStunned = false;
        _stunCoroutine = null;
    }

    private void OnDisable()
    {
        if (_stunCoroutine != null)
        {
            StopCoroutine(_stunCoroutine);
            _stunCoroutine = null;
        }

        IsStunned = false;
        _stunEndsAt = 0f;
    }
}
