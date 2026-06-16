using UnityEngine;

public class PooledObject : MonoBehaviour
{
    private string _poolKey;
    private float _autoReleaseTime = -1f; // -1이면 자동반환 안함

    public void Init(string key) => _poolKey = key;

    // 자동 반환 타이머 설정 (이펙트, 총알 등에 유용)
    public void AutoRelease(float delay)
    {
        _autoReleaseTime = delay;
        CancelInvoke(nameof(ReturnToPool));
        Invoke(nameof(ReturnToPool), delay);
    }

    // 비활성화 시 Invoke 취소 (중복 반환 방지)
    void OnDisable()
    {
        CancelInvoke(nameof(ReturnToPool));
    }

    public void ReturnToPool()
    {
        PoolManager.Instance.Release(_poolKey, gameObject);
    }
}