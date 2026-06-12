using UnityEngine;

namespace BossSystem.Scripable
{
    /// <summary>
    /// 보스 패턴 공격 데이터 — ScriptableObject
    /// Create 메뉴: BossSystem > AttackData
    ///
    /// 텔레그래프 동작:
    ///   연한 빨간색 원/扇형이 표시되고,
    ///   fillDuration 동안 진한 빨간색이 채워지면 공격 발동
    /// </summary>
    [CreateAssetMenu(menuName = "BossSystem/AttackData", fileName = "AttackData_New")]
    public class BossAttackData : ScriptableObject
    {
        [Header("기본 정보")]
        [Tooltip("Inspector에서 구분하기 위한 이름")]
        public string attackName = "NewAttack";

        [Header("텔레그래프 타이밍")]
        [Tooltip("진한 빨간색이 채워지는 시간 (초) — 이 시간 동안 보스 정지")]
        [Min(0.1f)]
        public float fillDuration = 1.0f;

        [Tooltip("텔레그래프 표시 후 실제 공격까지 추가 대기 (초)")]
        [Min(0f)]
        public float postFillDelay = 0.05f;

        [Header("보스 체력 (선택)")]
        [Tooltip("0 이하면 BossBase.maxHP 기본값 사용")]
        public float overrideMaxHP = 0f;

        [Header("이동 속도 (선택)")]
        [Tooltip("0 이하면 BossBase.moveSpeed 기본값 사용")]
        public float overrideMoveSpeed = 0f;

        [Header("텔레그래프 타입")]
        public TelegraphShape shape;
    }
}