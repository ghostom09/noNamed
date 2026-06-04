using System;
using BossSystem.Scripable;
using UnityEngine;

namespace BossSystem
{
    public enum TelegraphShape { Circle, Sector, Line }

    /// <summary>
    /// 공격 텔레그래프 시각화
    ///
    /// 수정:
    ///  - SO null 시 즉시 OnFillComplete 호출 (보스 영구 정지 방지)
    ///  - 부채꼴: Mesh를 런타임으로 생성 (원 스프라이트 재사용 X)
    ///  - Line: 흰 사각형 스프라이트로 표시
    /// </summary>
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class AttackTelegraph : MonoBehaviour
    {
        public event Action OnFillComplete;

        private BossAttackData data;
        private MeshRenderer   mr;
        private MeshFilter     mf;
        private MaterialPropertyBlock mpb;

        private bool  isRunning  = false;
        private float elapsed    = 0f;
        private bool  fillDone   = false;
        private float postElapsed = 0f;

        private void Awake()
        {
            mr  = GetComponent<MeshRenderer>();
            mf  = GetComponent<MeshFilter>();
            mpb = new MaterialPropertyBlock();

            // 기본 머티리얼 (Sprites/Default 또는 Unlit/Color)
            if (mr.sharedMaterial == null)
                mr.sharedMaterial = new Material(Shader.Find("Sprites/Default"));

            mr.sortingOrder = 5;
        }

        // ── 공개 API ──────────────────────────────────────────────

        /// <summary>
        /// SO가 null이면 즉시 OnFillComplete를 호출해 패턴이 멈추지 않도록 함.
        /// </summary>
        public void Show(BossAttackData attackData,
                         TelegraphShape shape     = TelegraphShape.Circle,
                         float          radius    = 3f,
                         Vector2        direction = default)
        {
            data = attackData;

            // ★ null 폴백: 텔레그래프 없이 즉시 공격으로 진행
            if (data == null)
            {
                Debug.LogWarning("[AttackTelegraph] BossAttackData가 null — 텔레그래프 스킵, 즉시 공격");
                gameObject.SetActive(false);
                OnFillComplete?.Invoke();
                return;
            }

            isRunning   = true;
            elapsed     = 0f;
            fillDone    = false;
            postElapsed = 0f;

            BuildMesh(shape, radius, direction);
            ApplyColor(data.baseColor);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            isRunning = false;
            gameObject.SetActive(false);
        }

        public float Progress => (data != null && data.fillDuration > 0f)
            ? Mathf.Clamp01(elapsed / data.fillDuration) : 0f;

        public bool IsRunning => isRunning;

        // ── 매 프레임 ─────────────────────────────────────────────
        private void Update()
        {
            if (!isRunning || data == null) return;

            if (!fillDone)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / data.fillDuration);
                ApplyColor(Color.Lerp(data.baseColor, data.fillColor, t));

                if (t >= 1f) { fillDone = true; postElapsed = 0f; }
            }
            else
            {
                postElapsed += Time.deltaTime;
                if (postElapsed >= data.postFillDelay)
                {
                    isRunning = false;
                    gameObject.SetActive(false);
                    OnFillComplete?.Invoke();
                }
            }
        }

        // ── 메시 생성 ─────────────────────────────────────────────

        private void BuildMesh(TelegraphShape shape, float radius, Vector2 direction)
        {
            switch (shape)
            {
                case TelegraphShape.Circle:
                    mf.mesh = MakeCircleMesh(radius, 48);
                    transform.localRotation = Quaternion.identity;
                    break;

                case TelegraphShape.Sector:
                    // direction을 각도로 변환 (보스 forward 기준 중심 방향)
                    float centerAngle = direction != Vector2.zero
                        ? Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg
                        : 90f;
                    // fanAngle은 radius 파라미터 대신 별도 전달이 필요하나
                    // 현재 구조상 radius = 반지름, direction.magnitude = 각도(도) 로 약속
                    float fanAngle = direction.magnitude > 0f ? direction.magnitude : 90f;
                    mf.mesh = MakeSectorMesh(radius, fanAngle, centerAngle, 32);
                    transform.localRotation = Quaternion.identity;
                    break;

                case TelegraphShape.Line:
                    // direction.x = 길이, direction.y = 폭
                    float len = direction.x > 0f ? direction.x : radius * 2f;
                    float wid = direction.y > 0f ? direction.y : 1f;
                    mf.mesh = MakeRectMesh(len, wid);
                    // 회전은 TelegraphHelper.SpawnLine에서 처리
                    break;
            }
        }

        // 원형 메시
        private static Mesh MakeCircleMesh(float radius, int segments)
        {
            var mesh     = new Mesh();
            var verts    = new Vector3[segments + 1];
            var tris     = new int[segments * 3];
            var uvs      = new Vector2[segments + 1];

            verts[0] = Vector3.zero;
            uvs[0]   = new Vector2(0.5f, 0.5f);

            for (int i = 0; i < segments; i++)
            {
                float a = 2f * Mathf.PI * i / segments;
                verts[i + 1] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
                uvs[i + 1]   = new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f);
            }

            for (int i = 0; i < segments; i++)
            {
                tris[i * 3]     = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = (i + 1) % segments + 1;
            }

            mesh.vertices  = verts;
            mesh.triangles = tris;
            mesh.uv        = uvs;
            mesh.RecalculateNormals();
            return mesh;
        }

        // 부채꼴 메시 (centerAngleDeg: 월드 기준 중심 각도, fanAngleDeg: 전체 각도)
        private static Mesh MakeSectorMesh(float radius, float fanAngleDeg,
                                           float centerAngleDeg, int segments)
        {
            var mesh  = new Mesh();
            int count = segments + 2;
            var verts = new Vector3[count];
            var tris  = new int[segments * 3];
            var uvs   = new Vector2[count];

            verts[0] = Vector3.zero;
            uvs[0]   = new Vector2(0.5f, 0.5f);

            float startRad = (centerAngleDeg - fanAngleDeg * 0.5f) * Mathf.Deg2Rad;
            float endRad   = (centerAngleDeg + fanAngleDeg * 0.5f) * Mathf.Deg2Rad;

            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float a = Mathf.Lerp(startRad, endRad, t);
                verts[i + 1] = new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
                uvs[i + 1]   = new Vector2(Mathf.Cos(a) * 0.5f + 0.5f, Mathf.Sin(a) * 0.5f + 0.5f);
            }

            for (int i = 0; i < segments; i++)
            {
                tris[i * 3]     = 0;
                tris[i * 3 + 1] = i + 1;
                tris[i * 3 + 2] = i + 2;
            }

            mesh.vertices  = verts;
            mesh.triangles = tris;
            mesh.uv        = uvs;
            mesh.RecalculateNormals();
            return mesh;
        }

        // 직사각형 메시 (중심 기준, 길이 방향 = Y축)
        private static Mesh MakeRectMesh(float length, float width)
        {
            var mesh = new Mesh();
            float hw = width  * 0.5f;
            float hl = length * 0.5f;

            mesh.vertices = new Vector3[]
            {
                new Vector3(-hw, -hl, 0f),
                new Vector3( hw, -hl, 0f),
                new Vector3( hw,  hl, 0f),
                new Vector3(-hw,  hl, 0f),
            };
            mesh.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            mesh.uv = new Vector2[]
            {
                new Vector2(0, 0), new Vector2(1, 0),
                new Vector2(1, 1), new Vector2(0, 1),
            };
            mesh.RecalculateNormals();
            return mesh;
        }

        private void ApplyColor(Color color)
        {
            mr.GetPropertyBlock(mpb);
            mpb.SetColor("_Color", color);
            mr.SetPropertyBlock(mpb);
        }
    }
}