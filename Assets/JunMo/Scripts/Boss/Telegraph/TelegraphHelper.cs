using System;
using BossSystem.Scripable;
using UnityEngine;

namespace BossSystem
{
    /// <summary>
    /// 텔레그래프 생성 유틸
    ///
    /// 부채꼴 호출 규칙:
    ///   direction = new Vector2(dirX, dirY).normalized * fanAngleDeg
    ///   → AttackTelegraph 내부에서 magnitude를 fanAngle(도), 방향을 중심각으로 사용
    /// </summary>
    public static class TelegraphHelper
    {
        // ── 원형 / 부채꼴 (보스 추종 또는 고정) ─────────────────
        public static AttackTelegraph Spawn(
            Transform      parent,
            BossAttackData data,
            TelegraphShape shape        = TelegraphShape.Circle,
            float          radius       = 3f,
            Vector2        direction    = default,
            bool           followParent = true,
            Action         onComplete   = null)
        {
            var (go, tele) = CreateBase($"Telegraph_{(data != null ? data.attackName : "Unknown")}");

            if (followParent && parent != null)
                go.transform.SetParent(parent, worldPositionStays: false);

            go.transform.position = parent != null
                ? new Vector3(parent.position.x, parent.position.y, parent.position.z)
                : Vector3.zero;

            if (!followParent) go.transform.SetParent(null);
            if (onComplete != null) tele.OnFillComplete += onComplete;

            tele.Show(data, shape, radius, direction);
            return tele;
        }

        // ── 월드 좌표 고정 ────────────────────────────────────────
        public static AttackTelegraph SpawnAt(
            Vector3        worldPosition,
            BossAttackData data,
            TelegraphShape shape      = TelegraphShape.Circle,
            float          radius     = 3f,
            Vector2        direction  = default,
            Action         onComplete = null)
        {
            var (go, tele) = CreateBase($"Telegraph_{(data != null ? data.attackName : "Unknown")}");
            go.transform.position = new Vector3(worldPosition.x, worldPosition.y, worldPosition.z);

            if (onComplete != null) tele.OnFillComplete += onComplete;
            tele.Show(data, shape, radius, direction);
            return tele;
        }

        // ── 직선 (돌진·파도 경로) ─────────────────────────────────
        /// <summary>
        /// startPos 에서 direction 방향으로 length 길이, width 폭의 직선 텔레그래프.
        /// </summary>
        public static AttackTelegraph SpawnLine(
            Vector3        startPos,
            Vector2        direction,
            float          length,
            float          width,
            BossAttackData data,
            Action         onComplete = null)
        {
            var (go, tele) = CreateBase($"Telegraph_{(data != null ? data.attackName : "Unknown")}_Line");

            // 직선 중심 = startPos + 방향 * length/2
            Vector2 dir2    = direction.normalized;
            Vector2 center  = (Vector2)startPos + dir2 * (length * 0.5f);
            go.transform.position = new Vector3(center.x, center.y, startPos.z);

            // 방향 회전 (메시는 Y축 방향으로 만들어짐)
            float angle = Mathf.Atan2(dir2.y, dir2.x) * Mathf.Rad2Deg - 90f;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            if (onComplete != null) tele.OnFillComplete += onComplete;

            // Line 형태: direction.x=길이, direction.y=폭 으로 전달
            tele.Show(data, TelegraphShape.Line, radius: 0f,
                      direction: new Vector2(length, width));
            return tele;
        }

        // ── 내부 ─────────────────────────────────────────────────
        private static (GameObject go, AttackTelegraph tele) CreateBase(string name)
        {
            var go = new GameObject(name);
            // AttackTelegraph은 MeshFilter + MeshRenderer 필요
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            var tele = go.AddComponent<AttackTelegraph>();
            return (go, tele);
        }
    }
}