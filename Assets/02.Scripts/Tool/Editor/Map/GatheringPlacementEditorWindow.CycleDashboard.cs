#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Cycle;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>
    /// 사이클 스폰 마커 대시보드.
    /// 마커를 찍을 수만 있고 사이클 규칙(외곽 보스 3 + 중앙 보스 1)을 충족했는지 알 수 없던 문제를 메운다.
    /// </summary>
    public partial class GatheringPlacementEditorWindow
    {
        /// <summary>사이클 규칙상 필요한 외곽 보스 스폰 지점 수.</summary>
        private const int RequiredOuterBossCount = 3;

        private bool _cycleDashboardFoldout = true;
        private Vector2 _cycleDashboardScroll;
        private bool _cycleDashboardScanned;

        private readonly List<CycleSpawnPoint> _cycleSpawnPoints = new();
        private readonly List<string> _cycleDashboardIssues = new();
        private readonly Dictionary<string, SectorStat> _cycleSectorStats = new();
        private int _centralBossCount;

        private struct SectorStat
        {
            public int PlayerCount;
            public int OuterBossCount;
            public int RespawnCount;
        }

        private void DrawCycleSpawnDashboard()
        {
            EditorGUILayout.Space(6f);
            _cycleDashboardFoldout = EditorGUILayout.Foldout(_cycleDashboardFoldout, "사이클 스폰 대시보드", true);
            if (!_cycleDashboardFoldout)
                return;

            if (GUILayout.Button("씬 스캔"))
                ScanCycleSpawnPoints();

            if (!_cycleDashboardScanned)
            {
                EditorGUILayout.HelpBox("'씬 스캔'을 눌러 배치된 마커의 규칙 충족 여부를 확인하세요.", MessageType.None);
                return;
            }

            EditorGUILayout.LabelField($"마커 {_cycleSpawnPoints.Count}개 · 섹터 {_cycleSectorStats.Count}개 · 중앙 보스 {_centralBossCount}개",
                EditorStyles.miniBoldLabel);

            _cycleDashboardScroll = EditorGUILayout.BeginScrollView(_cycleDashboardScroll, GUILayout.MaxHeight(180f));

            foreach (var pair in _cycleSectorStats)
            {
                var stat = pair.Value;
                EditorGUILayout.LabelField(
                    pair.Key,
                    $"Player {stat.PlayerCount} · OuterBoss {stat.OuterBossCount} · Respawn {stat.RespawnCount}");
            }

            EditorGUILayout.EndScrollView();

            if (_cycleDashboardIssues.Count == 0)
            {
                EditorGUILayout.HelpBox("사이클 배치 규칙을 충족합니다.", MessageType.Info);
                return;
            }

            foreach (string issue in _cycleDashboardIssues)
                EditorGUILayout.HelpBox(issue, MessageType.Warning);
        }

        private void ScanCycleSpawnPoints()
        {
            _cycleDashboardScanned = true;
            _cycleSpawnPoints.Clear();
            _cycleSectorStats.Clear();
            _cycleDashboardIssues.Clear();

            _cycleSpawnPoints.AddRange(UnityEngine.Object.FindObjectsByType<CycleSpawnPoint>(
                FindObjectsInactive.Include, FindObjectsSortMode.None));

            _centralBossCount = UnityEngine.Object.FindObjectsByType<CentralBossSpawnPoint>(
                FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

            var spawnIds = new Dictionary<string, string>();
            int totalOuterBoss = 0;
            int totalPlayer = 0;

            foreach (var point in _cycleSpawnPoints)
            {
                if (point == null)
                    continue;

                string sector = string.IsNullOrEmpty(point.SectorId) ? "(섹터 없음)" : point.SectorId;
                _cycleSectorStats.TryGetValue(sector, out SectorStat stat);

                if (point.Allows(CycleSpawnRole.Player))
                {
                    stat.PlayerCount++;
                    totalPlayer++;
                }

                if (point.Allows(CycleSpawnRole.OuterBoss))
                {
                    stat.OuterBossCount++;
                    totalOuterBoss++;

                    if (string.IsNullOrEmpty(point.SectorId))
                        _cycleDashboardIssues.Add($"'{point.SpawnId}'는 OuterBoss인데 Sector ID가 비어 있습니다.");
                }

                if (point.Allows(CycleSpawnRole.Respawn))
                    stat.RespawnCount++;

                _cycleSectorStats[sector] = stat;

                if (string.IsNullOrEmpty(point.SpawnId))
                    _cycleDashboardIssues.Add($"'{point.name}'에 Spawn ID가 없습니다.");
                else if (spawnIds.TryGetValue(point.SpawnId, out string owner))
                    _cycleDashboardIssues.Add($"Spawn ID '{point.SpawnId}'가 '{owner}'와 중복입니다.");
                else
                    spawnIds[point.SpawnId] = point.name;
            }

            AuditSafetyRadiusOverlap();

            if (_centralBossCount == 0)
                _cycleDashboardIssues.Add("중앙 보스 스폰 지점(CentralBossSpawnPoint)이 없습니다.");
            else if (_centralBossCount > 1)
                _cycleDashboardIssues.Add($"중앙 보스 스폰 지점이 {_centralBossCount}개입니다. 씬에 하나만 있어야 합니다.");

            if (totalOuterBoss < RequiredOuterBossCount)
                _cycleDashboardIssues.Add($"외곽 보스 스폰 지점이 {totalOuterBoss}개입니다. 사이클 규칙상 {RequiredOuterBossCount}개가 필요합니다.");

            if (totalPlayer == 0)
                _cycleDashboardIssues.Add("플레이어 스폰 지점이 없습니다.");

            SetTemporaryStatus(
                _cycleDashboardIssues.Count == 0
                    ? "사이클 스폰 규칙 충족"
                    : $"사이클 스폰 규칙 위반 {_cycleDashboardIssues.Count}건",
                _cycleDashboardIssues.Count == 0 ? MessageType.Info : MessageType.Warning);
        }

        /// <summary>안전 반경이 서로 겹치는 마커 쌍을 찾는다. 겹치면 스폰이 서로의 안전 영역을 침범한다.</summary>
        private void AuditSafetyRadiusOverlap()
        {
            for (int i = 0; i < _cycleSpawnPoints.Count; i++)
            {
                var a = _cycleSpawnPoints[i];
                if (a == null)
                    continue;

                for (int j = i + 1; j < _cycleSpawnPoints.Count; j++)
                {
                    var b = _cycleSpawnPoints[j];
                    if (b == null)
                        continue;

                    float minDistance = Mathf.Max(a.SafetyRadius, b.SafetyRadius);
                    if (minDistance <= 0f)
                        continue;

                    float distance = Vector3.Distance(a.Position, b.Position);
                    if (distance < minDistance)
                        _cycleDashboardIssues.Add(
                            $"'{a.SpawnId}'와 '{b.SpawnId}'가 {distance:0.#}m로 안전 반경({minDistance:0.#}m) 안에 있습니다.");
                }
            }
        }
    }
}
#endif
