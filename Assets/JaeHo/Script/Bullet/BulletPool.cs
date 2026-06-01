using System.Collections.Generic;
using UnityEngine;
using Global;

/// <summary>
/// 총알 오브젝트 풀.
/// BulletData 종류별로 Queue를 따로 관리해 프리팹 혼용 없이 안전하게 재사용.
///
/// 사용법:
///   Bullet b = BulletPool.Instance.Get(bulletData, position, direction);
///   b.Initialize(...);
///
///   반환은 Bullet 내부에서 자동 호출
///   BulletPool.Instance.Return(bullet, bulletData);
/// </summary>
public class BulletPool : Singleton<BulletPool>
{
    [Header("--- Pool Settings ---")]
    [Tooltip("BulletData 종류별 초기 예열(Warm-up) 수량")]
    [SerializeField] private int initialPoolSize = 10;

    [Tooltip("풀 부모 Transform. 없으면 자동 생성.")]
    [SerializeField] private Transform poolRoot;

    // BulletData → 해당 총알 타입의 풀
    private readonly Dictionary<BulletData, Queue<Bullet>> _pools = new();

    protected override void Awake()
    {
        base.Awake();

        if (poolRoot == null)
        {
            poolRoot = new GameObject("BulletPoolRoot").transform;
            poolRoot.SetParent(transform);
        }
    }

    // 외부 API

    /// <summary>
    /// 풀에서 총알을 꺼내 위치/활성화 후 반환.
    /// Initialize()는 호출자(ProjectileSkill)가 담당.
    /// </summary>
    public Bullet Get(BulletData data, Vector2 position, Vector2 direction)
    {
        Queue<Bullet> pool = GetOrCreatePool(data);

        Bullet bullet = pool.Count > 0 ? pool.Dequeue() : CreateBullet(data);

        bullet.transform.SetPositionAndRotation(position, Quaternion.identity);
        bullet.gameObject.SetActive(true);

        return bullet;
    }

    /// <summary>
    /// 총알을 비활성화해 풀에 반환.
    /// Bullet.ReturnToPool() 내부에서 호출됨.
    /// </summary>
    public void Return(Bullet bullet, BulletData data)
    {
        bullet.gameObject.SetActive(false);
        bullet.transform.SetParent(poolRoot);

        GetOrCreatePool(data).Enqueue(bullet);
    }

    // 내부 헬퍼

    private Queue<Bullet> GetOrCreatePool(BulletData data)
    {
        if (!_pools.TryGetValue(data, out var pool))
        {
            pool = new Queue<Bullet>();
            _pools[data] = pool;
            WarmUp(data, pool);
        }
        return pool;
    }

    /// <summary>
    /// 게임 시작 시 미리 총알을 생성해 런타임 스파이크 방지.
    /// </summary>
    private void WarmUp(BulletData data, Queue<Bullet> pool)
    {
        for (int i = 0; i < initialPoolSize; i++)
            pool.Enqueue(CreateBullet(data));
    }

    private Bullet CreateBullet(BulletData data)
    {
        if (data.prefab == null)
        {
            Debug.LogError($"[BulletPool] {data.name}의 prefab이 할당되지 않음");
            return null;
        }

        Bullet bullet = Instantiate(data.prefab, poolRoot);
        bullet.gameObject.SetActive(false);
        return bullet;
    }
}