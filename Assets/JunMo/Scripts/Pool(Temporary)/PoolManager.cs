using System.Collections.Generic;
using Global;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : Singleton<PoolManager>
{
    [System.Serializable]
    public class PoolData
    {
        public string key;
        public GameObject prefab;
        public int defaultCapacity = 10;
        public int maxSize = 10;
    }
    
    [SerializeField] private List<PoolData> poolDataList;
    
    private Dictionary<string, ObjectPool<GameObject>> _pools = new();
    private Dictionary<string, GameObject> _prefabMap = new();
    private Dictionary<string, Transform> _parentMap = new();

    protected override void Awake()
    {
        base.Awake();
        
        InitializePools();
    }

    private void InitializePools()
    {
        foreach (var data in poolDataList)
        {
            RegisterPool(data);
        }
    }

    // 인스펙터 등록 + 런타임 동적 추가 모두 지원
    public void RegisterPool(PoolData data)
    {
        if (_pools.ContainsKey(data.key)) return;

        _prefabMap[data.key] = data.prefab;

        // 풀별 부모 오브젝트 생성 (Hierarchy 정리)
        var parent = new GameObject($"Pool_{data.key}").transform;
        parent.SetParent(transform);
        _parentMap[data.key] = parent;

        var pool = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                var obj = Instantiate(_prefabMap[data.key], _parentMap[data.key]);
                obj.GetComponent<PooledObject>()?.Init(data.key); // 키 주입
                return obj;
            },
            actionOnGet:     obj => obj.SetActive(true),
            actionOnRelease: obj => obj.SetActive(false),
            actionOnDestroy: obj => Destroy(obj),
            collectionCheck: Application.isEditor, // 에디터에서만 중복 체크
            defaultCapacity: data.defaultCapacity,
            maxSize:         data.maxSize
        );

        _pools[data.key] = pool;
    }

    public GameObject Get(string key)
    {
        if (!_pools.TryGetValue(key, out var pool))
        {
            Debug.LogError($"[PoolManager] '{key}' 풀이 없습니다!");
            return null;
        }
        return pool.Get();
    }

    public void Release(string key, GameObject obj)
    {
        if (!_pools.TryGetValue(key, out var pool))
        {
            Destroy(obj);
            return;
        }
        pool.Release(obj);
    }
}
