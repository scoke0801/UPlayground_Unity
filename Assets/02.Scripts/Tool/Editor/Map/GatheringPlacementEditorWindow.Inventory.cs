#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UPlayGround.Components;
using UPlayGround.Group;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>씬 배치 인벤토리와 일괄 감사. 씬에 실제로 놓인 배치물을 목록으로 조작한다.</summary>
    public partial class GatheringPlacementEditorWindow
    {
        private const int InventoryDrawLimit = 300;

        private readonly List<WorldPlacementMetadata> _sceneInventory = new();
        private readonly List<AuditIssue> _auditIssues = new();

        private bool _inventoryFoldout;
        private Vector2 _inventoryScroll;
        private string _inventorySearchFilter = "";
        private bool _inventoryOnlyGrouped;
        private Vector2 _auditScroll;
        private bool _auditRan;

        private readonly struct AuditIssue
        {
            public AuditIssue(GameObject target, string category, string message)
            {
                Target = target;
                Category = category;
                Message = message;
            }

            public GameObject Target { get; }
            public string Category { get; }
            public string Message { get; }
        }

        #region 인벤토리

        private void RefreshSceneInventory()
        {
            _sceneInventory.Clear();

            var placements = UnityEngine.Object.FindObjectsByType<WorldPlacementMetadata>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);

            _sceneInventory.AddRange(placements);
            _sceneInventory.Sort((a, b) =>
            {
                string groupA = GetOwningGroupName(a);
                string groupB = GetOwningGroupName(b);
                int byGroup = string.Compare(groupA, groupB, StringComparison.OrdinalIgnoreCase);
                return byGroup != 0 ? byGroup : string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase);
            });
        }

        private static string GetOwningGroupName(WorldPlacementMetadata metadata)
        {
            if (metadata == null)
                return "";

            var group = metadata.GetComponentInParent<MonsterGroupController>();
            return group != null ? group.name : "(그룹 없음)";
        }

        private void DrawSceneInventorySection()
        {
            EditorGUILayout.Space(4f);
            _inventoryFoldout = EditorGUILayout.Foldout(_inventoryFoldout, "씬 배치 인벤토리", true);
            if (!_inventoryFoldout)
                return;

            EditorGUILayout.BeginHorizontal();
            _inventorySearchFilter = EditorGUILayout.TextField(_inventorySearchFilter, EditorStyles.toolbarSearchField);
            _inventoryOnlyGrouped = GUILayout.Toggle(_inventoryOnlyGrouped, "그룹만", EditorStyles.toolbarButton, GUILayout.Width(52f));
            if (GUILayout.Button("스캔", EditorStyles.toolbarButton, GUILayout.Width(42f)))
                RefreshSceneInventory();
            EditorGUILayout.EndHorizontal();

            if (_sceneInventory.Count == 0)
            {
                EditorGUILayout.HelpBox("'스캔'을 눌러 현재 씬의 배치물을 수집하세요.", MessageType.None);
                DrawAuditSection();
                return;
            }

            EditorGUILayout.LabelField($"배치물 {_sceneInventory.Count}개", EditorStyles.miniLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("전체 표면 재스냅"))
                ResnapAllInventory();
            if (GUILayout.Button("전체 검증"))
                RunSceneAudit();
            EditorGUILayout.EndHorizontal();

            _inventoryScroll = EditorGUILayout.BeginScrollView(_inventoryScroll, GUILayout.MaxHeight(220f));

            string lastGroup = null;
            int drawn = 0;

            foreach (var metadata in _sceneInventory)
            {
                if (metadata == null)
                    continue;

                if (!ContainsIgnoreCase(metadata.name, _inventorySearchFilter))
                    continue;

                string groupName = GetOwningGroupName(metadata);
                if (_inventoryOnlyGrouped && groupName == "(그룹 없음)")
                    continue;

                if (drawn >= InventoryDrawLimit)
                {
                    EditorGUILayout.LabelField($"… 이하 생략 (표시 상한 {InventoryDrawLimit}개)", EditorStyles.miniLabel);
                    break;
                }

                if (groupName != lastGroup)
                {
                    lastGroup = groupName;
                    EditorGUILayout.LabelField(groupName, EditorStyles.miniBoldLabel);
                }

                DrawInventoryRow(metadata);
                drawn++;
            }

            EditorGUILayout.EndScrollView();
            DrawAuditSection();
        }

        private void DrawInventoryRow(WorldPlacementMetadata metadata)
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button(metadata.name, _normalItemStyle))
            {
                Selection.activeGameObject = metadata.gameObject;
                FrameSceneView(metadata.transform.position);
            }

            if (GUILayout.Button(new GUIContent("↓", "표면에 다시 스냅"), GUILayout.Width(24f)))
                ResnapPlacement(metadata);

            if (GUILayout.Button(new GUIContent("×", "삭제"), GUILayout.Width(24f)))
            {
                Undo.DestroyObjectImmediate(metadata.gameObject);
                RefreshSceneInventory();
                GUIUtility.ExitGUI();
            }

            EditorGUILayout.EndHorizontal();
        }

        /// <summary>지형을 수정한 뒤 떠 있거나 파묻힌 배치물을 다시 지면에 붙인다.</summary>
        private void ResnapPlacement(WorldPlacementMetadata metadata)
        {
            if (metadata == null)
                return;

            using var undoScope = new PlacementUndoScope($"Resnap: {metadata.name}");

            if (!ResnapPlacementInternal(metadata))
            {
                SetTemporaryStatus($"'{metadata.name}' 아래에서 표면을 찾지 못했습니다.", MessageType.Warning);
                return;
            }

            EditorSceneManager.MarkSceneDirty(metadata.gameObject.scene);
            undoScope.Complete();
        }

        private void ResnapAllInventory()
        {
            if (!EditorUtility.DisplayDialog(
                    "전체 표면 재스냅",
                    $"씬의 배치물 {_sceneInventory.Count}개를 현재 지형에 다시 붙입니다.\n계속할까요?",
                    "재스냅", "취소"))
                return;

            using var undoScope = new PlacementUndoScope("Resnap All Placements");

            int moved = 0;
            int failed = 0;

            foreach (var metadata in _sceneInventory)
            {
                if (metadata == null)
                    continue;

                if (ResnapPlacementInternal(metadata))
                    moved++;
                else
                    failed++;
            }

            undoScope.Complete();
            SetTemporaryStatus($"재스냅 완료 — {moved}개 이동, {failed}개 실패", failed > 0 ? MessageType.Warning : MessageType.Info);
        }

        private bool ResnapPlacementInternal(WorldPlacementMetadata metadata)
        {
            Vector3 current = metadata.transform.position;
            if (!TryResolveSurfaceIgnoring(current, metadata.gameObject, out Vector3 position, out Vector3 normal))
                return false;

            Undo.RecordObject(metadata.transform, "Resnap Placement");
            metadata.transform.position = position;

            // 스냅 규칙은 배치 시와 동일한 경로를 쓴다.
            Vector3 savedPosition = _previewPosition;
            Vector3 savedNormal = _previewNormal;
            _previewPosition = position;
            _previewNormal = normal;
            StickInstanceToSurface(new PlacementInstance(metadata.gameObject, metadata.gameObject, moveSurfaceTargetOnly: false));
            _previewPosition = savedPosition;
            _previewNormal = savedNormal;

            EditorUtility.SetDirty(metadata.transform);
            return true;
        }

        #endregion

        #region 일괄 감사

        private void DrawAuditSection()
        {
            if (!_auditRan)
                return;

            EditorGUILayout.Space(4f);
            if (_auditIssues.Count == 0)
            {
                EditorGUILayout.HelpBox("검증 결과 문제가 없습니다.", MessageType.Info);
                return;
            }

            EditorGUILayout.LabelField($"검증 결과 {_auditIssues.Count}건", EditorStyles.miniBoldLabel);
            _auditScroll = EditorGUILayout.BeginScrollView(_auditScroll, GUILayout.MaxHeight(160f));

            foreach (var issue in _auditIssues)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button($"[{issue.Category}] {issue.Message}", _normalItemStyle))
                {
                    if (issue.Target != null)
                    {
                        Selection.activeGameObject = issue.Target;
                        FrameSceneView(issue.Target.transform.position);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }

        /// <summary>씬 전체 배치물을 훑어 규칙 위반과 데이터 무결성 문제를 모은다.</summary>
        private void RunSceneAudit()
        {
            RefreshSceneInventory();
            _auditIssues.Clear();
            _auditRan = true;

            var profile = _activeRuleProfile;
            var seenEntityGuids = new Dictionary<string, string>();

            foreach (var metadata in _sceneInventory)
            {
                if (metadata == null)
                    continue;

                AuditSourceIntegrity(metadata);
                AuditEntityIdDuplication(metadata, seenEntityGuids);

                if (profile != null)
                    AuditPlacementRules(metadata, profile);
            }

            SetTemporaryStatus(
                _auditIssues.Count == 0 ? "검증 통과 — 문제 없음" : $"검증 완료 — {_auditIssues.Count}건 발견",
                _auditIssues.Count == 0 ? MessageType.Info : MessageType.Warning);
        }

        private void AuditSourceIntegrity(WorldPlacementMetadata metadata)
        {
            if (string.IsNullOrEmpty(metadata.SourceId))
            {
                _auditIssues.Add(new AuditIssue(metadata.gameObject, "출처", $"'{metadata.name}'에 SourceId가 없습니다"));
                return;
            }

            if (metadata.SourceKind != WorldPlacementMetadata.PlacementSourceKind.DirectPrefab)
                return;

            string path = AssetDatabase.GUIDToAssetPath(metadata.SourceId);
            if (string.IsNullOrEmpty(path) || AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
                _auditIssues.Add(new AuditIssue(metadata.gameObject, "고아", $"'{metadata.name}'의 원본 프리팹을 찾을 수 없습니다"));
        }

        private void AuditEntityIdDuplication(WorldPlacementMetadata metadata, Dictionary<string, string> seen)
        {
            var entityId = metadata.GetComponent<SceneEntityId>();
            if (entityId == null || !entityId.HasGuid)
                return;

            if (seen.TryGetValue(entityId.Guid, out string owner))
            {
                _auditIssues.Add(new AuditIssue(
                    metadata.gameObject, "중복 ID", $"'{metadata.name}'의 SceneEntityId가 '{owner}'와 같습니다"));
                return;
            }

            seen[entityId.Guid] = metadata.name;
        }

        private void AuditPlacementRules(WorldPlacementMetadata metadata, Data.World.PlacementRuleProfileSO profile)
        {
            Vector3 position = metadata.transform.position;

            if (!TryResolveSurfaceIgnoring(position, metadata.gameObject, out Vector3 surface, out Vector3 normal))
            {
                _auditIssues.Add(new AuditIssue(metadata.gameObject, "표면", $"'{metadata.name}' 아래에 표면이 없습니다"));
                return;
            }

            float slope = Vector3.Angle(normal, Vector3.up);
            if (slope > profile.MaxSlopeAngle)
                _auditIssues.Add(new AuditIssue(
                    metadata.gameObject, "경사", $"'{metadata.name}' 경사 {slope:0}° (허용 {profile.MaxSlopeAngle:0}°)"));

            float verticalGap = position.y - surface.y;
            if (Mathf.Abs(verticalGap) > 0.5f)
                _auditIssues.Add(new AuditIssue(
                    metadata.gameObject, "높이", $"'{metadata.name}'이 지면과 {verticalGap:0.##}m 어긋나 있습니다"));

            if (profile.RequireNavMesh && !NavMesh.SamplePosition(position, out _, 1.5f, NavMesh.AllAreas))
                _auditIssues.Add(new AuditIssue(metadata.gameObject, "NavMesh", $"'{metadata.name}'이 NavMesh 밖입니다"));
        }

        #endregion
    }
}
#endif
