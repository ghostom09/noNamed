using UnityEngine;
using System.Collections;
using System;
using BossSystem.Scripable;

public enum TelegraphShape
{
    Circle,
    Sector,
    Square,
    Line,
}

public class Telegraph : MonoBehaviour
{
    [SerializeField] private Transform timeTelegraph;
    [SerializeField] private float defaultDuration = 1f;

    private SpriteRenderer _sr;
    private Coroutine _activeCoroutine;

    private BossAttackData _currentData;
    private Vector3 _targetScale;
    private float _duration;
    public event Action OnComplete;

    private void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();

        if (timeTelegraph != null)
            _targetScale = timeTelegraph.localScale;
    }

    public void SpawnTelegraph(BossAttackData data, Action onComplete, float duration = -1f)
    {
        Debug.Log($"[Telegraph] SpawnTelegraph 호출, timeTelegraph={timeTelegraph}, data={data}");
        _currentData = data;
        _duration = duration > 0f ? duration : defaultDuration;

        if (onComplete != null)
            OnComplete += onComplete;

        if (_activeCoroutine != null)
            StopCoroutine(_activeCoroutine);

        _activeCoroutine = StartCoroutine(ChargeTelegraph());
    }

    public void Cancel()
    {
        if (_activeCoroutine != null)
        {
            StopCoroutine(_activeCoroutine);
            _activeCoroutine = null;
        }

        Hide();
    }

    private IEnumerator ChargeTelegraph()
    {
        float duration = _duration;
        float elapsed = 0f;

        switch (_currentData.shape)
        {
            case TelegraphShape.Line:
                timeTelegraph.localScale =
                    new Vector3(_targetScale.x, 0f, _targetScale.z);
                break;

            default:
                timeTelegraph.localScale = Vector3.zero;
                break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            switch (_currentData.shape)
            {
                case TelegraphShape.Line:
                    timeTelegraph.localScale =
                        new Vector3(
                            _targetScale.x,
                            Mathf.Lerp(0f, _targetScale.y, t),
                            _targetScale.z);
                    break;

                default:
                    timeTelegraph.localScale =
                        Vector3.Lerp(Vector3.zero, _targetScale, t);
                    break;
            }

            yield return null;
        }

        timeTelegraph.localScale = _targetScale;
        Complete();
    }

    private void Complete()
    {
        Action complete = OnComplete;
        OnComplete = null;
        complete?.Invoke();

        Hide();
    }

    private void Hide()
    {
        if (_sr != null)
            _sr.enabled = false;

        timeTelegraph.localScale = Vector3.zero;
    }
}