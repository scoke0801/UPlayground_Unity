// KnockbackTestBed.cs
// 넉백 거리 인게임 확인용 테스트 컴포넌트
// 사용법: 빈 씬에 PlayerActor에 붙이고 Play → 숫자키 1~5로 각 force 테스트
// 완료 후 컴포넌트만 제거하면 됨

#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Collections.Generic;
using UnityEngine;
using UPlayGround.MovementController;

namespace UPlayGround
{
    public class KnockbackTestBed : MonoBehaviour
    {
        [Header("테스트 파라미터")]
        public float[] testForces = { 5f, 8f, 10f, 15f, 20f };
        public float drag = 8f;

        [Header("그리드")]
        public int gridSize = 20;       // 중심 기준 ±N 미터
        public Color gridColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        public Color originColor = new Color(1f, 0.8f, 0f, 0.9f);

        [Header("결과 표시")]
        public bool showGUI = true;

        // ─── 내부 상태
        private ActorMovementController _mc;
        private Vector3 _startPos;
        private bool _measuring;
        private float _peakDist;
        private readonly List<(float force, float dist)> _results = new();

        private static readonly string[] KeyLabels = { "1", "2", "3", "4", "5" };

        private void Start()
        {
            _mc = GetComponent<ActorMovementController>();
            if (_mc == null)
                UnityEngine.Debug.LogError("[KnockbackTestBed] ActorMovementController가 없습니다.");
        }

        private void Update()
        {
            for (int i = 0; i < Mathf.Min(testForces.Length, KeyLabels.Length); i++)
            {
                if (UnityEngine.Input.GetKeyDown(KeyLabels[i]))
                    Fire(testForces[i]);
            }

            if (_measuring)
            {
                float dist = Vector3.Distance(transform.position, _startPos);
                _peakDist = Mathf.Max(_peakDist, dist);

                // 임펄스가 끝나면 측정 완료
                if (!_mc.HasImpulse)
                {
                    float finalDist = Vector3.Distance(transform.position, _startPos);
                    _results.Add((_currentForce, finalDist));
                    UnityEngine.Debug.Log($"[Knockback] force={_currentForce} → {finalDist:F2}m (peak={_peakDist:F2}m)");
                    _measuring = false;
                }
            }
        }

        private float _currentForce;

        private void Fire(float force)
        {
            if (_mc == null) return;
            _startPos = transform.position;
            _peakDist = 0f;
            _currentForce = force;
            _measuring = true;

            // 캐릭터 뒤 방향으로 밀기
            _mc.AddPlanarKnockback(-transform.forward * force, drag);
            UnityEngine.Debug.Log($"[Knockback] force={force} 발사!");
        }

        // ─── 인게임 GUI
        private void OnGUI()
        {
            if (!showGUI) return;

            GUILayout.BeginArea(new Rect(16, 16, 280, 400));
            GUILayout.Label("=== Knockback TestBed ===");
            GUILayout.Label($"drag = {drag}");
            GUILayout.Space(4);

            for (int i = 0; i < Mathf.Min(testForces.Length, KeyLabels.Length); i++)
                GUILayout.Label($"  [{KeyLabels[i]}] force {testForces[i]}");

            GUILayout.Space(8);
            if (_measuring)
                GUILayout.Label($"측정 중... {Vector3.Distance(transform.position, _startPos):F2}m");

            if (_results.Count > 0)
            {
                GUILayout.Space(4);
                GUILayout.Label("─ 결과 ─");
                foreach (var (f, d) in _results)
                    GUILayout.Label($"  force {f,4} → {d:F2}m  ({d:F1} 그리드)");
            }

            GUILayout.EndArea();
        }

        // ─── Scene 뷰 그리드 (에디터 + 런타임 Gizmos)
        private void OnDrawGizmos()
        {
            DrawGrid();

            // 출발 지점 마커
            if (_measuring || _results.Count > 0)
            {
                Gizmos.color = originColor;
                Gizmos.DrawSphere(_startPos, 0.08f);
                Gizmos.DrawLine(_startPos, transform.position);
            }
        }

        private void DrawGrid()
        {
            Gizmos.color = gridColor;

            float y = transform.position.y;
            Vector3 center = new Vector3(
                Mathf.Round(transform.position.x),
                y,
                Mathf.Round(transform.position.z));

            for (int i = -gridSize; i <= gridSize; i++)
            {
                // Z축 방향 선
                Gizmos.DrawLine(
                    new Vector3(center.x + i, y, center.z - gridSize),
                    new Vector3(center.x + i, y, center.z + gridSize));
                // X축 방향 선
                Gizmos.DrawLine(
                    new Vector3(center.x - gridSize, y, center.z + i),
                    new Vector3(center.x + gridSize, y, center.z + i));
            }

            // 원점 강조 (굵게 = 4개 근접선)
            Gizmos.color = originColor;
            float h = 0.02f;
            for (float offset = -0.02f; offset <= 0.02f; offset += 0.01f)
            {
                Gizmos.DrawLine(
                    new Vector3(center.x + offset, y + h, center.z - gridSize),
                    new Vector3(center.x + offset, y + h, center.z + gridSize));
                Gizmos.DrawLine(
                    new Vector3(center.x - gridSize, y + h, center.z + offset),
                    new Vector3(center.x + gridSize, y + h, center.z + offset));
            }
        }
    }
}

#endif
