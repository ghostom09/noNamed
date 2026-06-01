using UnityEngine;
using System.Collections.Generic;
using BossSystem.Boss;

namespace BossSystem.BehaviorTree
{
    /// <summary>
    /// 보스 AI 전체가 공유하는 데이터 저장소
    /// 탑다운 2D: Y축이 깊이(위아래 이동)이므로 Vector2 거리 사용
    /// </summary>
    public class BossBlackboard
    {
        // ── 참조 ──────────────────────────────────────────────────
        public Transform BossTransform;
        public Transform PlayerTransform;
        public BossBase  Boss;

        // ── 전투 상태 ─────────────────────────────────────────────
        public float CurrentHP;
        public float MaxHP;

        // HP 50% 이하 → 페이즈2
        public bool IsPhase2 => MaxHP > 0f && (CurrentHP / MaxHP) <= 0.5f;
        public bool IsDead   => CurrentHP <= 0f;

        // ── 탑다운 2D 거리 계산 ───────────────────────────────────
        // 3D Physics를 사용하더라도 탑다운에서 의미 있는 거리는 XZ 평면
        public float DistanceToPlayer
        {
            get
            {
                if (PlayerTransform == null || BossTransform == null)
                    return float.MaxValue;
                return Vector2.Distance(
                    BossTransform.position,
                    PlayerTransform.position
                );
            }
        }

        // 플레이어 방향 (XZ 평면, 정규화)
        public Vector3 DirectionToPlayer
        {
            get
            {
                if (PlayerTransform == null || BossTransform == null)
                    return Vector3.right;

                Vector3 dir = PlayerTransform.position - BossTransform.position;
                dir.z = 0f;
                return dir.normalized;
            }
        }

        // ── 쿨다운 관리 ───────────────────────────────────────────
        private Dictionary<string, float> cooldowns = new Dictionary<string, float>();

        public void SetCooldown(string key, float duration)
            => cooldowns[key] = Time.time + duration;

        public bool IsOnCooldown(string key)
            => cooldowns.TryGetValue(key, out float end) && Time.time < end;

        // ── 커스텀 데이터 (보스별 확장) ───────────────────────────
        private Dictionary<string, object> customData = new Dictionary<string, object>();

        public void Set<T>(string key, T value)    => customData[key] = value;
        public bool HasKey(string key)             => customData.ContainsKey(key);

        public T Get<T>(string key, T defaultValue = default)
        {
            if (customData.TryGetValue(key, out object val) && val is T typed)
                return typed;
            return defaultValue;
        }
    }
}
