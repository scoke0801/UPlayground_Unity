#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UPlayGround.Components;
using UPlayGround.Data.World;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>배치 규칙 프로필과 배치 시점 검증 게이트.</summary>
    public partial class GatheringPlacementEditorWindow
    {
        private const string RuleProfileFolder = "Assets/10.Datas/World/RuleProfile";
        private const string RuleProfilePrefsKey = PrefsPrefix + "RuleProfileGuid";

        private readonly List<PlacementRuleProfileSO> _ruleProfiles = new();
        private PlacementRuleProfileSO _activeRuleProfile;

        /// <summary>검증 결과 심각도. Blocked만 배치를 막는다.</summary>
        private enum PlacementIssueSeverity
        {
            Warning = 0,
            Blocked = 1,
        }

        private readonly struct PlacementIssue
        {
            public PlacementIssue(PlacementIssueSeverity severity, string message)
            {
                Severity = severity;
                Message = message;
            }

            public PlacementIssueSeverity Severity { get; }
            public string Message { get; }
        }

        private readonly List<PlacementIssue> _previewIssues = new();
        private readonly List<WorldPlacementMetadata> _placementQueryCache = new();
        private double _placementQueryCacheExpiresAt;

        #region 프로필 목록 / 적용

        private void RefreshRuleProfiles()
        {
            _ruleProfiles.Clear();

            foreach (string guid in AssetDatabase.FindAssets("t:PlacementRuleProfileSO"))
            {
                var profile = AssetDatabase.LoadAssetAtPath<PlacementRuleProfileSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (profile != null)
                    _ruleProfiles.Add(profile);
            }

            _ruleProfiles.Sort((a, b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));

            if (_activeRuleProfile == null)
            {
                string savedGuid = EditorPrefs.GetString(RuleProfilePrefsKey, "");
                if (!string.IsNullOrEmpty(savedGuid))
                {
                    var savedProfile = AssetDatabase.LoadAssetAtPath<PlacementRuleProfileSO>(
                        AssetDatabase.GUIDToAssetPath(savedGuid));
                    if (savedProfile != null)
                        ApplyRuleProfile(savedProfile);
                }
            }
        }

        /// <summary>상단 툴바에 얹는 프로필 선택 드롭다운.</summary>
        private void DrawRuleProfileToolbar()
        {
            if (_ruleProfiles.Count == 0)
                return;

            var options = new string[_ruleProfiles.Count + 1];
            options[0] = "(프로필 없음)";
            int current = 0;
            for (int i = 0; i < _ruleProfiles.Count; i++)
            {
                options[i + 1] = _ruleProfiles[i].DisplayName;
                if (_ruleProfiles[i] == _activeRuleProfile)
                    current = i + 1;
            }

            int picked = EditorGUILayout.Popup(current, options, EditorStyles.toolbarPopup, GUILayout.Width(130f));
            if (picked == current)
                return;

            if (picked <= 0)
            {
                _activeRuleProfile = null;
                EditorPrefs.SetString(RuleProfilePrefsKey, "");
                SetTemporaryStatus("배치 규칙 프로필을 해제했습니다.", MessageType.Info);
                return;
            }

            ApplyRuleProfile(_ruleProfiles[picked - 1]);
        }

        private void ApplyRuleProfile(PlacementRuleProfileSO profile)
        {
            if (profile == null)
                return;

            _activeRuleProfile = profile;
            _surfaceSnapMode = ToWindowSnapMode(profile.SurfaceSnapMode);
            _alignToSurface = profile.AlignToSurface;
            _heightOffset = profile.HeightOffset;
            _raycastMask = profile.RaycastMask;
            _ignoreTriggerColliders = profile.IgnoreTriggerColliders;
            _snapToGrid = profile.SnapToGrid;
            _gridSize = Mathf.Max(0.01f, profile.GridSize);
            _yawOffset = profile.YawOffset;
            _randomRotation = profile.RandomRotation;
            _randomRotationXRange = profile.RandomRotationXRange;
            _randomRotationYRange = profile.RandomRotationYRange;
            _randomRotationZRange = profile.RandomRotationZRange;
            _autoSetupCollider = profile.AutoSetupCollider;
            _addSceneEntityId = profile.AddSceneEntityId;
            _addPlacementMetadata = profile.AddPlacementMetadata;
            _placementBakeMode = profile.BakeTarget == PlacementBakeTarget.RuntimeData
                ? WorldPlacementMetadata.PlacementBakeMode.RuntimeData
                : WorldPlacementMetadata.PlacementBakeMode.SceneObject;

            EditorPrefs.SetString(RuleProfilePrefsKey, GetAssetGuid(profile));
            SetTemporaryStatus($"'{profile.DisplayName}' 규칙 프로필을 적용했습니다.", MessageType.Info);
            SceneView.RepaintAll();
        }

        private void SaveCurrentSettingsAsProfile()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "배치 규칙 프로필 저장", "PlacementRuleProfile", "asset", "현재 배치 규칙을 저장할 경로를 지정하세요.", RuleProfileFolder);

            if (string.IsNullOrEmpty(path))
                return;

            EnsureFolder(RuleProfileFolder);
            var profile = ScriptableObject.CreateInstance<PlacementRuleProfileSO>();
            AssetDatabase.CreateAsset(profile, path);

            profile.EditorCapture(
                ToProfileSnapMode(_surfaceSnapMode),
                _alignToSurface,
                _heightOffset,
                _raycastMask,
                _ignoreTriggerColliders,
                _snapToGrid,
                _gridSize,
                _yawOffset,
                _randomRotation,
                _randomRotationXRange,
                _randomRotationYRange,
                _randomRotationZRange,
                _autoSetupCollider,
                _addSceneEntityId,
                _addPlacementMetadata,
                _placementBakeMode == WorldPlacementMetadata.PlacementBakeMode.RuntimeData
                    ? PlacementBakeTarget.RuntimeData
                    : PlacementBakeTarget.SceneObject);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            RefreshRuleProfiles();
            _activeRuleProfile = profile;
            EditorPrefs.SetString(RuleProfilePrefsKey, GetAssetGuid(profile));
            SetTemporaryStatus($"'{profile.DisplayName}' 프로필로 저장했습니다.", MessageType.Info);
        }

        private static SurfaceSnapMode ToWindowSnapMode(PlacementSurfaceSnap snap) => snap switch
        {
            PlacementSurfaceSnap.None => SurfaceSnapMode.None,
            PlacementSurfaceSnap.Full => SurfaceSnapMode.Full,
            _ => SurfaceSnapMode.LowerOnly,
        };

        private static PlacementSurfaceSnap ToProfileSnapMode(SurfaceSnapMode snap) => snap switch
        {
            SurfaceSnapMode.None => PlacementSurfaceSnap.None,
            SurfaceSnapMode.Full => PlacementSurfaceSnap.Full,
            _ => PlacementSurfaceSnap.LowerOnly,
        };

        #endregion

        #region 검증 게이트

        /// <summary>
        /// 프리뷰 지점의 배치 적합성을 평가한다.
        /// 프로필이 없으면 아무 것도 검사하지 않아 기존 동작과 같다.
        /// </summary>
        private void EvaluatePlacementIssues(Vector3 position, Vector3 normal)
        {
            _previewIssues.Clear();

            var profile = _activeRuleProfile;
            if (profile == null)
                return;

            float slope = Vector3.Angle(normal, Vector3.up);
            if (slope > profile.MaxSlopeAngle)
                _previewIssues.Add(new PlacementIssue(
                    PlacementIssueSeverity.Warning,
                    $"경사 {slope:0}° (허용 {profile.MaxSlopeAngle:0}°)"));

            if (profile.OverlapWarnRadius > 0f && HasNearbyPlacement(position, profile.OverlapWarnRadius, out string nearbyName))
                _previewIssues.Add(new PlacementIssue(
                    PlacementIssueSeverity.Warning,
                    $"'{nearbyName}'와 {profile.OverlapWarnRadius:0.##}m 이내로 겹칩니다"));

            if (profile.RequireNavMesh && !NavMesh.SamplePosition(position, out _, 1.5f, NavMesh.AllAreas))
                _previewIssues.Add(new PlacementIssue(
                    PlacementIssueSeverity.Warning,
                    "NavMesh 밖입니다"));
        }

        private bool HasNearbyPlacement(Vector3 position, float radius, out string nearbyName)
        {
            nearbyName = null;
            float sqrRadius = radius * radius;

            foreach (var placement in GetPlacementQueryCache())
            {
                if (placement == null)
                    continue;

                if ((placement.transform.position - position).sqrMagnitude > sqrRadius)
                    continue;

                nearbyName = placement.name;
                return true;
            }

            return false;
        }

        private IReadOnlyList<WorldPlacementMetadata> GetPlacementQueryCache(bool forceRefresh = false)
        {
            if (!forceRefresh && EditorApplication.timeSinceStartup < _placementQueryCacheExpiresAt)
                return _placementQueryCache;

            _placementQueryCache.Clear();
            _placementQueryCache.AddRange(UnityEngine.Object.FindObjectsByType<WorldPlacementMetadata>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None));
            _placementQueryCacheExpiresAt = EditorApplication.timeSinceStartup + 0.25d;
            return _placementQueryCache;
        }

        private void InvalidatePlacementQueryCache()
        {
            _placementQueryCacheExpiresAt = 0d;
        }

        private bool HasBlockingIssue()
        {
            foreach (var issue in _previewIssues)
                if (issue.Severity == PlacementIssueSeverity.Blocked)
                    return true;

            return false;
        }

        /// <summary>씬 뷰 프리뷰 라벨에 붙일 검증 요약. 문제가 없으면 빈 문자열.</summary>
        private string BuildIssueSummary()
        {
            if (_previewIssues.Count == 0)
                return "";

            var lines = new string[_previewIssues.Count];
            for (int i = 0; i < _previewIssues.Count; i++)
                lines[i] = "⚠ " + _previewIssues[i].Message;

            return "\n" + string.Join("\n", lines);
        }

        #endregion
    }
}
#endif
