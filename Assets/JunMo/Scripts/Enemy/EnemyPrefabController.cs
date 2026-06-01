using UnityEngine;
using System.Collections.Generic;

public class EnemyPrefabController : MonoBehaviour
{
    public static EnemyPrefabController Instance;

    [System.Serializable]
    public class AttackPrefabData
    {
        public AttackType attackType;
        public GameObject prefab;
    }

    [SerializeField]
    private List<AttackPrefabData> attackPrefabs;

    private Dictionary<AttackType, GameObject> prefabMap;

    private void Awake()
    {
        Instance = this;

        prefabMap = new();

        foreach (var data in attackPrefabs)
        {
            prefabMap[data.attackType] = data.prefab;
        }
    }

    public GameObject GetPrefab(AttackType type)
    {
        if (prefabMap.TryGetValue(type, out GameObject prefab))
            return prefab;

        Debug.LogWarning($"No prefab : {type}");
        return null;
    }
}