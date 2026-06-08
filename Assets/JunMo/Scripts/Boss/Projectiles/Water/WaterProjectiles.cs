using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace BossSystem.Boss.WaterBoss
{
    // ═══════════════════════════════════════════════════════════════
    //  PlayerMovement 인터페이스 (프로젝트 컴포넌트로 교체)
    // ═══════════════════════════════════════════════════════════════
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float baseSpeed = 5f;
        private bool  isBound     = false;
        private float bindEnd     = 0f;
        private float slowEnd     = 0f;
        private float slowMult    = 1f;

        private void Update()
        {
            if (isBound && Time.time >= bindEnd)
            {
                isBound = false;
                Debug.Log("[Player] 속박 해제");
            }
            if (slowMult < 1f && Time.time >= slowEnd)
            {
                slowMult = 1f;
                Debug.Log("[Player] 둔화 해제");
            }
        }

        public void ApplyBind(float duration)
        {
            isBound = true;
            bindEnd = Time.time + duration;
            Debug.Log($"[Player] 속박 {duration}s");
        }

        public void ApplySlow(float multiplier, float duration)
        {
            slowMult = Mathf.Min(slowMult, multiplier);   // 더 강한 둔화 우선
            slowEnd  = Mathf.Max(slowEnd, Time.time + duration);
        }

        public void RemoveSlow() => slowMult = 1f;

        public bool  IsBound      => isBound;
        public float SpeedMult    => isBound ? 0f : slowMult;
        public float CurrentSpeed => baseSpeed * SpeedMult;
    }
}
