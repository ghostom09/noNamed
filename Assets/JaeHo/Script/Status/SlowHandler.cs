using System.Collections;
using UnityEngine;

/// <summary>
/// ISlowable 구현체 레퍼런스.
/// 이동 속도를 가진 컴포넌트(PlayerMover, EnemyMover 등)와 함께 붙여서 사용.
/// 둔화가 중첩으로 들어오면 더 강한 쪽을 유지하고 타이머를 갱신.
///
/// 사용법:
///   1. 이동 컴포넌트에 SlowHandler를 AddComponent 또는 인스펙터에서 추가
///   2. 이동 컴포넌트에서 SlowHandler.SpeedMultiplier를 이동 속도에 곱함
///      예: rb.linearVelocity = moveDir * moveSpeed * slowHandler.SpeedMultiplier;
/// </summary>
public class SlowHandler : MonoBehaviour, ISlowable
{
    /// <summary>
    /// 외부 이동 컴포넌트에서 이 값을 이동속도에 곱해서 사용.
    /// 1.0 = 정상, 0.5 = 50% 속도.
    /// </summary>
    public float SpeedMultiplier { get; private set; } = 1f;

    private Coroutine _slowCoroutine;

    public void ApplySlow(float multiplier, float duration)
    {
        // 중첩 둔화: 더 강한 쪽 유지
        float clampedMultiplier = Mathf.Clamp01(multiplier);

        if (_slowCoroutine != null)
        {
            // 이미 둔화 중이면 더 강한 쪽으로 갱신
            if (clampedMultiplier <= SpeedMultiplier)
                SpeedMultiplier = clampedMultiplier;

            StopCoroutine(_slowCoroutine);
        }
        else
        {
            SpeedMultiplier = clampedMultiplier;
        }

        _slowCoroutine = StartCoroutine(SlowRoutine(duration));
    }

    private IEnumerator SlowRoutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        SpeedMultiplier = 1f;
        _slowCoroutine = null;
    }

    private void OnDisable()
    {
        // 비활성화 시 즉시 초기화 (풀 반환 등 고려)
        if (_slowCoroutine != null)
        {
            StopCoroutine(_slowCoroutine);
            _slowCoroutine = null;
        }
        SpeedMultiplier = 1f;
    }
}