using UnityEngine;

/// <summary>
/// 총알의 물리 스탯 데이터.
/// </summary>
[CreateAssetMenu(fileName = "BulletData", menuName = "SkillSystem/BulletData")]
public class BulletData : ScriptableObject
{
    [Header("--- Movement ---")]
    [Tooltip("총알 이동 속도 (유닛/초)")]
    public float speed = 20f;

    [Tooltip("총알이 소멸하기까지의 최대 비행 거리")]
    public float maxDistance = 30f;

    [Header("--- Bounce ---")]
    [Tooltip("최대 바운스 횟수. Bounce 태그가 없으면 무시됨.")]
    public int maxBounceCount = 3;

    [Header("--- Piercing ---")]
    [Tooltip("관통 가능한 최대 적 수. Piercing 태그가 없으면 무시됨.")]
    public int maxPierceCount = 5;

    [Header("--- Visual ---")]
    [Tooltip("풀에서 사용할 총알 프리팹. Bullet 컴포넌트가 붙어 있어야 함.")]
    public Bullet prefab;
}