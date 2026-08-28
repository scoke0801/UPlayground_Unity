#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UPlayGround.Animation;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Event;
using UPlayGround.EditorTools;
using UPlayGround.MovementController;
using MotionData = UPlayGround.Animation.Motion;

namespace UPlayGround.Editor
{
    /// <summary>
    /// MotionWarp 윈도우의 순수 애니메이션 루트 변위를 Play Mode 없이 일괄 측정해 이벤트에 베이크한다.
    ///
    /// ■ 왜 필요한가
    /// DeltaWarp는 "윈도우 전체의 루트모션 총량"을 알아야 잔여 보정을 경로 비례로 분배해 정확히 착지한다.
    /// 이 총량은 베이크가 없으면 Editor/Development의 첫 시전에 존재하지 않아 방향 스티어 폴백으로 내려간다.
    /// Release는 빌드 검증에서 누락·stale 프로필을 차단하며, 유효한 bakedValid 시드로 첫 시전부터
    /// 프로필 기반 보정을 쓴다.
    ///
    /// ■ 기존 Play Mode 베이크(WarpBakePanel)와의 관계
    /// WarpBakePanel은 모션 에디터에서 MotionSet 하나씩, Play Mode에서 ActorAnimator.DeltaPosition을
    /// 누적한다. 정의상 가장 정확하지만 200개 규모를 손으로 돌릴 수 없다. 이 창은 같은 값을
    /// AnimationMode 오프라인 샘플링으로 산출하며, 두 측정이 같은 값을 내는지는 [검증] 탭이
    /// 기존 베이크와 직접 비교해 증명한다. 검증을 통과하기 전에는 적용하지 않는 것을 전제로 한다.
    ///
    /// ■ 안전 규칙
    /// 분석·검증은 에셋을 건드리지 않는다. 적용은 Undo 그룹으로 묶고 예외 시 전체 롤백한다.
    /// 측정 경로가 0에 수렴하면(=루트모션이 샘플링되지 않음) 0을 쓰지 않고 실패로 보고한다.
    /// </summary>
    public sealed class WarpRootMotionBatchBakeWindow : EditorWindow
    {
        private const float SampleRate = 120f;

        // 이 값보다 짧은 경로는 베이크해도 DeltaWarp 프로필 기반 보정이 의미를 갖지 못한다.
        // (remainingPath가 0에 붙어 share가 즉시 1로 튀므로 사실상 폴백과 같다)
        private const float MinimumUsablePathLength = 0.0001f;

        // 제자리 모션 의심 경계. 실패는 아니지만 저작 검토 대상으로 보고한다.
        private const float InPlaceSuspicionPathLength = 0.05f;

        // 순 변위가 이동 경로의 절반보다 작으면 전진 후 복귀하는 클립으로 본다.
        // 타겟 착지 보정이 원본 발 동작을 크게 바꿀 수 있어 자동 적용에서 제외한다.
        private const float MinimumNetDisplacementRatio = 0.5f;

        // 검증 허용 오차. Play Mode 측정은 가변 프레임이라 완전 일치할 수 없다.
        private const float VerifyRelativeTolerance = 0.08f;
        private const float VerifyAbsoluteTolerance = 0.02f;

        private const string ReportPath = "Library/WarpRootMotionBatchBake.json";

        private enum Scope
        {
            전체_프로젝트,
            선택한_MotionSet,
        }

        private sealed class OwnerProfile
        {
            public GameObject Prefab;
            public string PrefabPath;
            public string AnimatorPath;
            public string AvatarName;
            public Avatar Avatar;
            public Vector3 AnimatorScale;
        }

        private sealed class MeasurementJob
        {
            public MotionSetAsset Asset;
            public OwnerProfile Owner;
        }

        /// <summary> 워프 윈도우 1개의 측정 결과. </summary>
        private sealed class WindowResult
        {
            public MotionSetAsset Asset;
            public MotionEvent_MotionWarp Warp;
            public string AssetPath;
            public string OwnerPrefabPath;
            public string AnimatorPath;
            public string AvatarName;
            public Avatar MeasuredAvatar;
            public Vector3 AnimatorScale;
            public float GlobalStart;
            public float GlobalEnd;
            public Vector3 MeasuredLocal;
            public float MeasuredPath;
            public float MeasuredYaw;
            public string SourceFingerprint;
            public Vector3[] CumulativeLocalPositions;
            public float[] CumulativePathLengths;
            public float[] CumulativeYaw;
            public bool HasExistingBake;
            public bool HasPlayModeReference;
            public Vector3 ExistingLocal;
            public float ExistingPath;
            public Vector3[] ExistingCumulativeLocalPositions;
            public float[] ExistingCumulativePathLengths;
            public float[] ExistingCumulativeYaw;
            public string Status;      // OK / InPlace / Backtracking / NoRootMotion / Layered
            public string Message;
        }

        [Serializable]
        private sealed class ReportRow
        {
            public string asset;
            public string ownerPrefab;
            public string animatorPath;
            public string avatar;
            public Vector3 animatorScale;
            public float windowStart;
            public float windowEnd;
            public float measuredPathLen;
            public float measuredLocalMagnitude;
            public float measuredYaw;
            public int trajectorySampleCount;
            public string sourceFingerprint;
            public float existingPathLen;
            public string status;
            public string message;
        }

        [Serializable]
        private sealed class Report
        {
            public string generatedAt;
            public bool applied;
            public int prefabCount;
            public int assetCount;
            public int windowCount;
            public int profileCount;
            public int okCount;
            public int inPlaceCount;
            public int backtrackingCount;
            public int failedCount;
            public int unmappedWindowCount;
            public List<ReportRow> rows = new();
            public List<string> unmappedWindows = new();
        }

        [SerializeField] private Scope _scope = Scope.전체_프로젝트;

        [Tooltip("이미 같은 Avatar·스케일 프로필이 있는 윈도우도 다시 측정한다. 끄면 누락 프로필만 채운다.")]
        [SerializeField] private bool _overwriteExisting;

        private readonly List<WindowResult> _results = new();

        // preset 이 DeltaWarp 를 강제하지 않는 순수 레거시 윈도우. 자동 변환하지 않고 보고만 한다
        // (돌진·잡기처럼 Additive 를 의도한 저작일 수 있어 일괄 변경이 위험하다).
        private readonly List<string> _legacyAdditiveWindows = new();

        // 어떤 ActorAnimationMotionSet에서도 참조하지 않는 DeltaWarp 윈도우.
        // 실행 데이터는 아니므로 빌드를 막지 않되 신규 모션 매핑 누락을 찾을 수 있게 보고한다.
        private readonly List<string> _unmappedDeltaWarpWindows = new();

        // 검증 실행 동안만 켜지는 내부 플래그. 직렬화 설정(_overwriteExisting)을 건드리지 않는다.
        private bool _forceIncludeBaked;
        private bool _verificationPassed;

        private SerializedObject _serialized;
        private Vector2 _scroll;
        private string _summary = "[분석]으로 대상 윈도우와 측정값을 먼저 확인하세요.";

        [UPlaygroundTool("UPlayGround/게임플레이/전투/Motion Warp/루트모션 일괄 베이크", false, 331)]
        public static void Open()
        {
            var window = GetWindow<WarpRootMotionBatchBakeWindow>(true, "워프 루트모션 일괄 베이크");
            window.minSize = new Vector2(760f, 600f);
            window.Show();
        }

        /// <summary>CI와 배치 검증에서 프로젝트 전체 베이크 대상을 비파괴 분석한다.</summary>
        public static void AnalyzeProjectFromCommandLine()
        {
            var window = CreateInstance<WarpRootMotionBatchBakeWindow>();
            try
            {
                window._scope = Scope.전체_프로젝트;
                window._overwriteExisting = false;
                window.Run(RunMode.Analyze);
                if (window._results.Count == 0)
                    throw new InvalidOperationException(
                        "측정 가능한 MotionWarp 프로필이 없습니다. Library/WarpRootMotionBatchBake.json을 확인하세요.");
            }
            finally
            {
                DestroyImmediate(window);
            }
        }

        /// <summary>출처가 기록된 기존 PlayMode 베이크와 오프라인 샘플러를 비파괴 대조한다.</summary>
        public static void VerifyPlayModeReferenceFromCommandLine()
        {
            var window = CreateInstance<WarpRootMotionBatchBakeWindow>();
            try
            {
                if (!window.VerifyKnownPlayModeReferences())
                    throw new InvalidOperationException(
                        "PlayMode 기준 베이크와 오프라인 측정이 일치하지 않습니다. Library/WarpRootMotionBatchBake.json을 확인하세요.");
            }
            finally
            {
                DestroyImmediate(window);
            }
        }

        /// <summary>현재 ActorAnimator 오도미터로 기준 프로필 하나를 PlayMode에서 다시 캡처한다.</summary>
        public static void CapturePlayModeReferenceFromCommandLine()
            => MotionWarpPlayModeReferenceCapture.Begin();

        /// <summary>검증을 통과한 현재 포맷 프로필을 프로젝트 전체 DeltaWarp 데이터에 적용한다.</summary>
        public static void ApplyProjectFromCommandLine()
        {
            var window = CreateInstance<WarpRootMotionBatchBakeWindow>();
            try
            {
                if (!window.VerifyKnownPlayModeReferences())
                    throw new InvalidOperationException(
                        "PlayMode 기준 검증에 실패해 전체 베이크를 중단했습니다.");

                window._scope = Scope.전체_프로젝트;
                window._overwriteExisting = true;
                window.Run(RunMode.Analyze);
                if (window._results.Count == 0
                    || !window._results.Any(IsApplicableResult))
                {
                    throw new InvalidOperationException(
                        "적용 가능한 MotionWarp 프로필이 없습니다.");
                }

                window._verificationPassed = true;
                window.Apply(requireConfirmation: false);
                window.ValidateAppliedData();
            }
            finally
            {
                DestroyImmediate(window);
            }
        }

        /// <summary>현재 프로젝트 베이크 데이터를 전체 재측정 결과와 비파괴 대조한다.</summary>
        public static void ValidateProjectFromCommandLine()
        {
            var window = CreateInstance<WarpRootMotionBatchBakeWindow>();
            try
            {
                window._scope = Scope.전체_프로젝트;
                window._overwriteExisting = true;
                window.Run(RunMode.Analyze);
                if (window._results.Count == 0)
                    throw new InvalidOperationException(
                        "검증 가능한 MotionWarp 프로필이 없습니다.");
                window.ValidateAppliedData();
            }
            finally
            {
                DestroyImmediate(window);
            }
        }

        /// <summary>빌드에 사용되는 MotionSet의 v3 프로필·출처 지문·중복 여부를 샘플링 없이 검사한다.</summary>
        internal static bool TryValidateMappedProfiles(out string validationReport)
        {
            var errors = new List<string>();
            var warnings = new List<string>();
            try
            {
                Dictionary<MotionSetAsset, List<OwnerProfile>> ownership = BuildOwnership();
                foreach ((MotionSetAsset asset, List<OwnerProfile> owners) in ownership)
                {
                    string assetPath = AssetDatabase.GetAssetPath(asset);
                    foreach (OwnerProfile owner in GetDistinctOwnerProfiles(owners))
                    {
                        foreach (MotionEvent_MotionWarp warp in CollectWarpEvents(asset.motionSet))
                        {
                            if (!UsesDeltaWarp(warp))
                                continue;
                            if (warp.endTime - warp.startTime <= 0f)
                            {
                                errors.Add($"빈 워프 윈도우: {assetPath}");
                                continue;
                            }

                            string identity =
                                $"{assetPath} [{warp.startTime:F3}~{warp.endTime:F3}] "
                                + $"avatar={owner.AvatarName} scale={owner.AnimatorScale:F3}";
                            if (!warp.TryGetBakedProfile(
                                    owner.Avatar,
                                    owner.AnimatorScale,
                                    out MotionWarpRootMotionBakeProfile profile))
                            {
                                errors.Add(
                                    $"v3 프로필 누락: {identity}. "
                                    + "제자리·무루트 모션이면 MotionWarp 이벤트를 제거하세요.");
                                continue;
                            }

                            int duplicateCount = warp.bakedProfiles.Count(candidate =>
                                candidate != null
                                && candidate.Matches(owner.Avatar, owner.AnimatorScale));
                            if (duplicateCount != 1)
                                errors.Add($"프로필 키 중복({duplicateCount}): {identity}");

                            string expectedFingerprint = BuildSourceFingerprint(
                                asset,
                                warp,
                                owner.Avatar,
                                owner.AnimatorScale);
                            if (profile.sourceFingerprint != expectedFingerprint)
                                errors.Add($"stale 프로필: {identity}");
                        }
                    }
                }

                var ownedAssets = new HashSet<MotionSetAsset>(ownership.Keys);
                foreach (string guid in AssetDatabase.FindAssets(
                             "t:MotionSetAsset",
                             new[] { "Assets/10.Datas" }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    MotionSetAsset asset = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path);
                    if (asset?.motionSet == null || ownedAssets.Contains(asset))
                        continue;
                    int windowCount = CollectWarpEvents(asset.motionSet).Count(UsesDeltaWarp);
                    if (windowCount > 0)
                        warnings.Add($"미매핑 DeltaWarp({windowCount}): {path}");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            var builder = new StringBuilder();
            builder.AppendLine(
                errors.Count == 0
                    ? "MotionWarp 빌드 검증 통과"
                    : $"MotionWarp 빌드 검증 실패: {errors.Count}건");
            foreach (string error in errors.Take(40))
                builder.AppendLine($"- {error}");
            if (errors.Count > 40)
                builder.AppendLine($"- 외 {errors.Count - 40}건");
            if (warnings.Count > 0)
            {
                builder.AppendLine($"미매핑 경고: {warnings.Count}건");
                foreach (string warning in warnings.Take(20))
                    builder.AppendLine($"- {warning}");
            }

            validationReport = builder.ToString();
            return errors.Count == 0;
        }

        private void OnEnable() => _serialized = new SerializedObject(this);

        private void OnGUI()
        {
            _serialized ??= new SerializedObject(this);
            _serialized.Update();

            EditorGUILayout.HelpBox(
                "DeltaWarp는 윈도우 루트모션 총량이 있어야 첫 시전부터 정확히 착지합니다.\n"
                + "[검증]은 기존 Play Mode 베이크와 오프라인 측정을 비교해 두 측정이 같은 값인지 증명합니다.\n"
                + "검증이 통과한 뒤에 [적용]하세요.",
                MessageType.Info);

            SerializedProperty property = _serialized.GetIterator();
            property.NextVisible(true);
            while (property.NextVisible(false))
                EditorGUILayout.PropertyField(property, true);
            _serialized.ApplyModifiedProperties();

            EditorGUILayout.Space(4f);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("분석", GUILayout.Height(28f)))
                    Run(RunMode.Analyze);
                if (GUILayout.Button("검증 (기존 베이크 대조)", GUILayout.Height(28f)))
                    Run(RunMode.Verify);
                using (new EditorGUI.DisabledScope(!HasApplicableResult()))
                {
                    if (GUILayout.Button("적용", GUILayout.Height(28f)))
                        Apply();
                }
            }

            EditorGUILayout.Space(6f);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_summary, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private bool HasApplicableResult()
            => _verificationPassed
               && _results.Any(IsApplicableResult);

        private static bool IsApplicableResult(WindowResult result) =>
            result != null
            && (result.Status == "OK" || result.Status == "Backtracking");

        private enum RunMode
        {
            Analyze,
            Verify,
        }

        private bool VerifyKnownPlayModeReferences()
        {
            _results.Clear();
            _legacyAdditiveWindows.Clear();
            _unmappedDeltaWarpWindows.Clear();
            _verificationPassed = false;
            _forceIncludeBaked = true;

            Dictionary<MotionSetAsset, List<OwnerProfile>> ownership =
                BuildOwnership();
            OwnerProfile[] owners = ownership.Values
                .SelectMany(profiles => profiles)
                .OrderBy(GetVerificationOwnerPriority)
                .ThenBy(profile => profile.PrefabPath)
                .ThenBy(profile => profile.AnimatorPath)
                .ToArray();
            var byPrefab = new Dictionary<GameObject, List<MeasurementJob>>();

            string[] guids = AssetDatabase.FindAssets(
                "t:MotionSetAsset",
                new[] { "Assets/10.Datas" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MotionSetAsset asset =
                    AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path);
                if (asset?.motionSet == null)
                    continue;

                foreach (MotionEvent_MotionWarp warp in
                         CollectWarpEvents(asset.motionSet))
                {
                    if (!TryGetPlayModeReferenceKey(
                            warp,
                            out Avatar referenceAvatar,
                            out Vector3 referenceScale))
                        continue;

                    OwnerProfile owner = owners.FirstOrDefault(candidate =>
                        candidate.Avatar == referenceAvatar
                        && (candidate.AnimatorScale - referenceScale)
                           .sqrMagnitude <= 0.0001f);
                    if (owner == null)
                        continue;

                    if (!byPrefab.TryGetValue(
                            owner.Prefab,
                            out List<MeasurementJob> jobs))
                    {
                        jobs = new List<MeasurementJob>();
                        byPrefab.Add(owner.Prefab, jobs);
                    }

                    if (jobs.Any(job => job.Asset == asset))
                        continue;
                    jobs.Add(new MeasurementJob
                    {
                        Asset = asset,
                        Owner = owner,
                    });
                }
            }

            try
            {
                foreach ((GameObject prefab, List<MeasurementJob> jobs) in
                         byPrefab)
                    MeasurePrefab(prefab, jobs);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (AnimationMode.InAnimationMode())
                    AnimationMode.StopAnimationMode();
                _forceIncludeBaked = false;
            }

            _summary = BuildVerifySummary();
            WriteReport(applied: false);
            Debug.Log(_summary);
            return _verificationPassed;
        }

        private static bool TryGetPlayModeReferenceKey(
            MotionEvent_MotionWarp warp,
            out Avatar avatar,
            out Vector3 animatorScale)
        {
            avatar = null;
            animatorScale = Vector3.one;
            if (warp?.bakedProfiles != null)
            {
                foreach (MotionWarpRootMotionBakeProfile profile in
                         warp.bakedProfiles)
                {
                    if (profile == null
                        || !profile.HasPlayModeReferenceTrajectory
                        || profile.playModeReferenceFormatVersion
                        != MotionEvent_MotionWarp.CurrentBakeFormatVersion)
                        continue;
                    avatar = profile.avatar;
                    animatorScale = profile.animatorScale;
                    return true;
                }
            }

            if (warp == null
                || !warp.bakedValid
                || !warp.bakedFromPlayMode
                || warp.bakedFormatVersion
                != MotionEvent_MotionWarp.CurrentBakeFormatVersion
                || warp.bakedPathLen <= MinimumUsablePathLength)
                return false;
            avatar = warp.bakedAvatar;
            animatorScale = warp.bakedAnimatorScale;
            return true;
        }

        /// <summary>
        /// PlayMode 기준은 실제 플레이어 프리뷰에서 기록되므로 같은 Avatar를 공유하는 몬스터보다
        /// 플레이어 모델 프리팹을 우선한다. Avatar·Scale만 같다는 이유로 다른 런타임 계층을 고르면
        /// 검증 자체가 프리팹 선택 순서에 따라 달라진다.
        /// </summary>
        private static int GetVerificationOwnerPriority(OwnerProfile owner)
        {
            string path = owner?.PrefabPath ?? string.Empty;
            if (path.IndexOf(
                    "/Actor/Player/Models/",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return 0;
            if (path.IndexOf(
                    "/Actor/Player/",
                    StringComparison.OrdinalIgnoreCase) >= 0)
                return 1;
            return 2;
        }

        // ── 측정 파이프라인 ────────────────────────────────────────────────

        private void Run(RunMode mode)
        {
            _results.Clear();
            _legacyAdditiveWindows.Clear();
            _unmappedDeltaWarpWindows.Clear();
            _verificationPassed = false;
            // 검증은 기존 베이크와 대조하는 것이 목적이라 토글과 무관하게 베이크된 윈도우까지 다시 측정한다.
            _forceIncludeBaked = mode == RunMode.Verify;
            try
            {
                // 프로젝트 전체 프리팹 스캔은 비싸므로 범위 오류를 먼저 걸러낸다.
                HashSet<MotionSetAsset> scopeFilter = ResolveScopeFilter();
                Dictionary<MotionSetAsset, List<OwnerProfile>> ownership = BuildOwnership();
                CollectLegacyAdditiveWindows(ownership.Keys, scopeFilter);
                CollectUnmappedDeltaWarpWindows(ownership.Keys, scopeFilter);

                var byPrefab = new Dictionary<GameObject, List<MeasurementJob>>();
                foreach ((MotionSetAsset asset, List<OwnerProfile> owners) in ownership)
                {
                    if (scopeFilter != null && !scopeFilter.Contains(asset))
                        continue;
                    foreach (OwnerProfile owner in GetDistinctOwnerProfiles(owners))
                    {
                        if (!HasBakeTargetWindow(asset, owner))
                            continue;
                        if (!byPrefab.TryGetValue(
                                owner.Prefab,
                                out List<MeasurementJob> jobs))
                        {
                            jobs = new List<MeasurementJob>();
                            byPrefab.Add(owner.Prefab, jobs);
                        }
                        jobs.Add(new MeasurementJob
                        {
                            Asset = asset,
                            Owner = owner,
                        });
                    }
                }

                int prefabIndex = 0;
                foreach ((GameObject prefab, List<MeasurementJob> jobs) in byPrefab)
                {
                    if (EditorUtility.DisplayCancelableProgressBar(
                            "워프 루트모션 측정",
                            $"{prefab.name} ({jobs.Count}개 프로필)",
                            prefabIndex / (float)Mathf.Max(1, byPrefab.Count)))
                        throw new OperationCanceledException("사용자가 취소했습니다.");
                    prefabIndex++;
                    MeasurePrefab(prefab, jobs);
                }
            }
            catch (OperationCanceledException cancel)
            {
                _summary = $"측정 취소 — {cancel.Message}";
                _results.Clear();
                return;
            }
            catch (InvalidOperationException invalid)
            {
                _summary = $"측정 불가 — {invalid.Message}";
                _results.Clear();
                return;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                if (AnimationMode.InAnimationMode())
                    AnimationMode.StopAnimationMode();
            }

            _summary = mode == RunMode.Verify
                ? BuildVerifySummary()
                : BuildAnalyzeSummary();
            WriteReport(applied: false);
        }

        /// <summary>
        /// 프리팹 하나를 로드해 소유한 MotionSet 전부의 워프 윈도우를 측정한다.
        /// 프리팹 로드는 비싸므로 에셋이 아니라 프리팹 단위로 묶어 1회만 연다.
        /// </summary>
        private void MeasurePrefab(
            GameObject prefabAsset,
            List<MeasurementJob> jobs)
        {
            string prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
            GameObject root = null;
            try
            {
                root = PrefabUtility.LoadPrefabContents(prefabPath);

                foreach (MeasurementJob job in jobs)
                {
                    Animator animator = ResolveAnimatorAtPath(
                        root,
                        job.Owner.AnimatorPath);
                    if (animator == null)
                    {
                        AddAssetFailure(
                            job.Asset,
                            job.Owner,
                            "NoRootMotion",
                            "프리팹에서 측정 대상 Animator를 찾지 못했습니다.");
                        continue;
                    }

                    // 비활성 계층은 샘플링되지 않는다. 프리팹 사본이라 원본 activeSelf를 되돌릴 필요는 없다.
                    for (Transform cursor = animator.transform;
                         cursor != null;
                         cursor = cursor.parent)
                        cursor.gameObject.SetActive(true);

                    // 런타임과 같은 Animator.deltaPosition을 얻기 위해 수동 PlayableGraph를 평가한다.
                    // 프리팹 복제본만 건드리므로 원본 Animator 설정은 변하지 않는다.
                    animator.applyRootMotion = true;
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    MeasureAsset(
                        job.Asset,
                        job.Owner,
                        animator,
                        root.transform,
                        job.Owner.AnimatorPath,
                        job.Owner.AvatarName);
                }
            }
            finally
            {
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private void MeasureAsset(
            MotionSetAsset asset,
            OwnerProfile owner,
            Animator animator,
            Transform actorRoot,
            string animatorPath,
            string avatarName)
        {
            MotionSet set = asset.motionSet;
            List<MotionEvent_MotionWarp> warps = CollectWarpEvents(set);
            var targets = new List<WindowResult>();
            foreach (MotionEvent_MotionWarp warp in warps)
            {
                if (!IsBakeTarget(warp, owner))
                    continue;
                if (!set.TryGetEventGlobalStart(warp, out float globalStart))
                {
                    AddAssetFailure(asset, owner, "Layered",
                        "이벤트의 글로벌 시각을 해석하지 못했습니다(레이어 소속 가능).");
                    continue;
                }

                bool hasExistingBake = TryGetExistingBake(
                    warp,
                    animator.avatar,
                    animator.transform.lossyScale,
                    out Vector3 existingLocal,
                    out float existingPath,
                    out bool fromPlayMode,
                    out Vector3[] existingPositions,
                    out float[] existingPaths,
                    out float[] existingYaw);

                targets.Add(new WindowResult
                {
                    Asset = asset,
                    Warp = warp,
                    AssetPath = AssetDatabase.GetAssetPath(asset),
                    OwnerPrefabPath = owner.PrefabPath,
                    AnimatorPath = animatorPath,
                    AvatarName = avatarName,
                    MeasuredAvatar = animator.avatar,
                    AnimatorScale = animator.transform.lossyScale,
                    GlobalStart = globalStart,
                    GlobalEnd = globalStart + Mathf.Max(0f, warp.endTime - warp.startTime),
                    SourceFingerprint = BuildSourceFingerprint(
                        asset,
                        warp,
                        animator.avatar,
                        animator.transform.lossyScale),
                    HasExistingBake = hasExistingBake,
                    HasPlayModeReference = hasExistingBake && fromPlayMode,
                    ExistingLocal = existingLocal,
                    ExistingPath = existingPath,
                    ExistingCumulativeLocalPositions = existingPositions,
                    ExistingCumulativePathLengths = existingPaths,
                    ExistingCumulativeYaw = existingYaw,
                });
            }

            if (targets.Count == 0)
                return;

            SampleTimeline(set, animator, actorRoot, targets);

            foreach (WindowResult result in targets)
            {
                if (result.MeasuredPath <= MinimumUsablePathLength)
                {
                    result.Status = "NoRootMotion";
                    result.Message =
                        "루트모션이 없는 모션에는 MotionWarp 이벤트를 두지 않습니다. "
                        + "이벤트를 제거하거나 루트모션이 있는 클립으로 교체해야 합니다.";
                }
                else if (result.MeasuredPath < InPlaceSuspicionPathLength)
                {
                    result.Status = "InPlace";
                    result.Message =
                        "제자리 모션은 모션워핑 대상이 아닙니다. MotionWarp 이벤트를 제거해야 합니다.";
                }
                else if (result.MeasuredLocal.magnitude / result.MeasuredPath
                         < MinimumNetDisplacementRatio)
                {
                    result.Status = "Backtracking";
                    result.Message =
                        "전진 후 복귀하는 루트 경로입니다. 총 경로에 비해 순 변위가 작아 "
                        + "v3 trajectory로 기록하지만 발 동작과 착지 체감을 별도로 검토해야 합니다.";
                }
                else
                {
                    result.Status = "OK";
                    result.Message = string.Empty;
                }

                _results.Add(result);
            }
        }

        /// <summary>
        /// MotionSet 타임라인을 런타임과 같은 시간 매핑으로 훑으며 Animator 루트 변위를 누적한다.
        ///
        /// AnimationMode의 Transform 위치는 Humanoid import의 루트모션 추출 정책과 다를 수 있다.
        /// 수동 PlayableGraph의 deltaPosition을 읽어 PlayMode 베이크와 같은 값을 사용한다.
        /// 모션 경계의 첫 평가는 이전 클립과 연결된 델타가 아니므로 버린다.
        /// </summary>
        private static void SampleTimeline(
            MotionSet set,
            Animator animator,
            Transform actorRoot,
            List<WindowResult> targets)
        {
            foreach (WindowResult target in targets)
                SampleWindowTrajectory(set, animator, actorRoot, target);
        }

        private static void SampleWindowTrajectory(
            MotionSet set,
            Animator animator,
            Transform actorRoot,
            WindowResult target)
        {
            int trajectorySampleCount = MotionEvent_MotionWarp.TrajectorySampleCount;
            target.CumulativeLocalPositions = new Vector3[trajectorySampleCount];
            target.CumulativePathLengths = new float[trajectorySampleCount];
            target.CumulativeYaw = new float[trajectorySampleCount];

            float globalStart = Mathf.Clamp(target.GlobalStart, 0f, set.TotalDuration);
            float globalEnd = Mathf.Clamp(target.GlobalEnd, globalStart, set.TotalDuration);
            float duration = globalEnd - globalStart;
            if (duration <= 0.0001f || actorRoot == null)
                return;

            Vector3 initialPosition = actorRoot.position;
            Quaternion initialRotation = actorRoot.rotation;
            Vector3 initialUp = initialRotation * Vector3.up;
            Quaternion inverseInitialRotation = Quaternion.Inverse(initialRotation);

            PlayableGraph graph = PlayableGraph.Create(
                $"MotionWarpTrajectory_{animator.GetInstanceID()}");
            graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
            AnimationPlayableOutput output = AnimationPlayableOutput.Create(
                graph,
                "RootMotion",
                animator);
            output.SetWeight(1f);

            AnimationClipPlayable activePlayable = default;
            int activeMotionIndex = -1;
            float activeMotionEnd = globalStart;
            float currentTime = globalStart;
            int nextTrajectorySample = 1;
            Vector3 cumulativeLocal = Vector3.zero;
            float cumulativePath = 0f;
            float cumulativeYaw = 0f;
            try
            {
                graph.Play();
                if (!ConfigurePlayable(
                        set,
                        graph,
                        output,
                        globalStart,
                        ref activePlayable,
                        out activeMotionIndex,
                        out activeMotionEnd))
                    return;

                while (currentTime < globalEnd - 0.000001f)
                {
                    if (currentTime >= activeMotionEnd - 0.000001f)
                    {
                        if (!ConfigurePlayable(
                                set,
                                graph,
                                output,
                                currentTime,
                                ref activePlayable,
                                out activeMotionIndex,
                                out activeMotionEnd))
                            break;
                    }

                    float trajectoryTime = globalStart
                        + duration * (nextTrajectorySample
                                      / (float)(trajectorySampleCount - 1));
                    float nextTime = Mathf.Min(
                        globalEnd,
                        Mathf.Min(
                            currentTime + 1f / SampleRate,
                            Mathf.Min(trajectoryTime, activeMotionEnd)));
                    float step = nextTime - currentTime;
                    if (step <= 0.000001f)
                        break;

                    graph.Evaluate(step);
                    Vector3 worldDelta = animator.deltaPosition;
                    worldDelta.y = 0f;
                    Quaternion beforeRotation = actorRoot.rotation;
                    Quaternion afterRotation =
                        (beforeRotation * animator.deltaRotation).normalized;
                    cumulativeLocal += inverseInitialRotation * worldDelta;
                    cumulativePath += worldDelta.magnitude;
                    cumulativeYaw += ResolveYawDelta(
                        beforeRotation,
                        afterRotation,
                        initialUp);
                    actorRoot.SetPositionAndRotation(
                        actorRoot.position + animator.deltaPosition,
                        afterRotation);

                    currentTime = nextTime;
                    if (nextTrajectorySample < trajectorySampleCount
                        && currentTime >= trajectoryTime - 0.000001f)
                    {
                        target.CumulativeLocalPositions[nextTrajectorySample] =
                            cumulativeLocal;
                        target.CumulativePathLengths[nextTrajectorySample] =
                            cumulativePath;
                        target.CumulativeYaw[nextTrajectorySample] = cumulativeYaw;
                        nextTrajectorySample++;
                    }
                }

                while (nextTrajectorySample < trajectorySampleCount)
                {
                    target.CumulativeLocalPositions[nextTrajectorySample] = cumulativeLocal;
                    target.CumulativePathLengths[nextTrajectorySample] = cumulativePath;
                    target.CumulativeYaw[nextTrajectorySample] = cumulativeYaw;
                    nextTrajectorySample++;
                }

                target.MeasuredLocal = cumulativeLocal;
                target.MeasuredPath = cumulativePath;
                target.MeasuredYaw = cumulativeYaw;
            }
            finally
            {
                actorRoot.SetPositionAndRotation(initialPosition, initialRotation);
                if (graph.IsValid())
                    graph.Destroy();
            }
        }

        internal static bool ConfigurePlayable(
            MotionSet set,
            PlayableGraph graph,
            AnimationPlayableOutput output,
            float globalTime,
            ref AnimationClipPlayable activePlayable,
            out int motionIndex,
            out float motionEnd)
        {
            motionIndex = -1;
            motionEnd = globalTime;
            if (!TryResolveMotionSpan(
                    set,
                    globalTime,
                    out motionIndex,
                    out float motionStart,
                    out motionEnd)
                || motionIndex < 0
                || motionIndex >= set.motions.Count)
                return false;

            MotionData motion = set.motions[motionIndex];
            if (motion?.motionClip == null)
                return false;
            if (activePlayable.IsValid())
                graph.DestroySubgraph(activePlayable);
            activePlayable = AnimationClipPlayable.Create(graph, motion.motionClip);
            activePlayable.SetApplyFootIK(false);
            activePlayable.SetApplyPlayableIK(false);
            float playbackSpeed = Mathf.Max(0.0001f, motion.playbackSpeed);
            activePlayable.SetTime(
                motion.ClipStartTime
                + Mathf.Max(0f, globalTime - motionStart) * playbackSpeed);
            activePlayable.SetSpeed(playbackSpeed);
            output.SetSourcePlayable(activePlayable);
            graph.Evaluate(0f);
            return true;
        }

        internal static bool TryResolveMotionSpan(
            MotionSet set,
            float globalTime,
            out int motionIndex,
            out float motionStart,
            out float motionEnd)
        {
            motionIndex = -1;
            motionStart = 0f;
            motionEnd = 0f;
            if (set?.motions == null)
                return false;

            float cursor = 0f;
            for (int index = 0; index < set.motions.Count; index++)
            {
                MotionData motion = set.motions[index];
                float duration = motion?.Duration ?? 0f;
                float end = cursor + duration;
                bool isLast = index == set.motions.Count - 1;
                if (duration > 0f
                    && globalTime >= cursor - 0.000001f
                    && (globalTime < end - 0.000001f
                        || (isLast && globalTime <= end + 0.000001f)))
                {
                    motionIndex = index;
                    motionStart = cursor;
                    motionEnd = end;
                    return true;
                }

                cursor = end;
            }

            return false;
        }

        internal static float ResolveYawDelta(
            Quaternion beforeRotation,
            Quaternion afterRotation,
            Vector3 up)
        {
            Vector3 beforeForward = Vector3.ProjectOnPlane(
                beforeRotation * Vector3.forward,
                up);
            Vector3 afterForward = Vector3.ProjectOnPlane(
                afterRotation * Vector3.forward,
                up);
            if (beforeForward.sqrMagnitude <= 0.000001f
                || afterForward.sqrMagnitude <= 0.000001f)
                return 0f;
            return Vector3.SignedAngle(beforeForward, afterForward, up);
        }

        /// <summary>클립·리그·윈도우·샘플러 계약이 바뀌면 달라지는 v3 출처 지문을 만든다.</summary>
        internal static string BuildSourceFingerprint(
            MotionSetAsset asset,
            MotionEvent_MotionWarp warp,
            Avatar avatar,
            Vector3 animatorScale)
        {
            var builder = new StringBuilder(1024);
            string assetPath = AssetDatabase.GetAssetPath(asset);
            builder.Append(AssetDatabase.AssetPathToGUID(assetPath));
            builder.Append('|').Append(MotionEvent_MotionWarp.CurrentBakeFormatVersion);
            builder.Append('|').Append(MotionEvent_MotionWarp.TrajectorySampleCount);
            AppendFloat(builder, warp.startTime);
            AppendFloat(builder, warp.endTime);
            AppendFloat(builder, warp.globalStartTimeOffset);
            AppendFloat(builder, animatorScale.x);
            AppendFloat(builder, animatorScale.y);
            AppendFloat(builder, animatorScale.z);

            string avatarPath = AssetDatabase.GetAssetPath(avatar);
            builder.Append('|').Append(AssetDatabase.AssetPathToGUID(avatarPath));
            if (!string.IsNullOrEmpty(avatarPath))
                builder.Append('|').Append(AssetDatabase.GetAssetDependencyHash(avatarPath));

            if (asset?.motionSet?.motions != null)
            {
                foreach (MotionData motion in asset.motionSet.motions)
                {
                    string clipPath = AssetDatabase.GetAssetPath(motion?.motionClip);
                    builder.Append('|').Append(AssetDatabase.AssetPathToGUID(clipPath));
                    if (!string.IsNullOrEmpty(clipPath))
                        builder.Append('|').Append(AssetDatabase.GetAssetDependencyHash(clipPath));
                    if (motion == null)
                        continue;
                    AppendFloat(builder, motion.ClipStartTime);
                    AppendFloat(builder, motion.ClipEndTime);
                    AppendFloat(builder, motion.playbackSpeed);
                }
            }

            return Hash128.Compute(builder.ToString()).ToString();
        }

        private static void AppendFloat(StringBuilder builder, float value)
        {
            builder.Append('|').Append(
                value.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// 프리팹의 현재 활성 캐릭터를 구동하는 ActorAnimator를 고른다.
        /// Player처럼 여러 캐릭터 모델을 품은 프리팹에서 비활성 첫 모델을 고르면 전체 측정이 0이 된다.
        /// </summary>
        private static ActorAnimator ResolveActorAnimator(GameObject root)
        {
            ActorAnimator best = null;
            int bestDepth = int.MaxValue;
            foreach (ActorAnimator actorAnimator in
                     root.GetComponentsInChildren<ActorAnimator>(false))
            {
                int depth = 0;
                for (Transform cursor = actorAnimator.transform;
                     cursor != null && cursor != root.transform;
                     cursor = cursor.parent)
                    depth++;
                if (depth >= bestDepth)
                    continue;
                bestDepth = depth;
                best = actorAnimator;
            }

            if (best != null)
                return best;

            ActorAnimator[] inactiveCandidates =
                root.GetComponentsInChildren<ActorAnimator>(true);
            return inactiveCandidates.Length == 1
                ? inactiveCandidates[0]
                : null;
        }

        private static Animator ResolveDrivenAnimator(
            ActorAnimator actorAnimator)
        {
            if (actorAnimator == null)
                return null;
            return actorAnimator.GetComponent<Animator>()
                   ?? actorAnimator.GetComponentInParent<Animator>(true)
                   ?? actorAnimator.GetComponentInChildren<Animator>(true);
        }

        private static Animator ResolveAnimatorAtPath(
            GameObject root,
            string animatorPath)
        {
            if (root == null)
                return null;
            Transform target = string.IsNullOrEmpty(animatorPath)
                ? root.transform
                : root.transform.Find(animatorPath);
            return target != null ? target.GetComponent<Animator>() : null;
        }

        // ── 적용 ──────────────────────────────────────────────────────────

        private void Apply()
            => Apply(requireConfirmation: true);

        private void Apply(bool requireConfirmation)
        {
            if (!_verificationPassed)
            {
                _summary = "적용 차단 — 기존 Play Mode 베이크와의 검증을 100% 통과해야 합니다.";
                return;
            }

            WindowResult[] authoringErrors = _results
                .Where(result => !IsApplicableResult(result))
                .ToArray();
            if (authoringErrors.Length > 0)
            {
                string message =
                    $"적용 차단 — 모션워핑 비대상 또는 측정 실패가 {authoringErrors.Length}건 있습니다. "
                    + "InPlace·NoRootMotion이면 MotionWarp 이벤트를 제거하세요.";
                _summary = message + "\n\n" + _summary;
                if (!requireConfirmation)
                    throw new InvalidOperationException(message);
                return;
            }

            WindowResult[] applicable = _results
                .Where(IsApplicableResult)
                .ToArray();
            if (applicable.Length == 0)
                return;

            if (requireConfirmation
                && !EditorUtility.DisplayDialog(
                    "워프 루트모션 프로필 적용",
                    $"워프 루트모션 프로필 {applicable.Length}개를 기록합니다. 계속할까요?",
                    "적용",
                    "취소"))
                return;

            var changedAssets = new HashSet<MotionSetAsset>();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("워프 루트모션 일괄 베이크");
            bool assetEditing = false;
            try
            {
                foreach (IGrouping<MotionSetAsset, WindowResult> assetGroup in
                         applicable.GroupBy(result => result.Asset))
                {
                    Undo.RegisterCompleteObjectUndo(assetGroup.Key, "워프 루트모션 일괄 베이크");
                    changedAssets.Add(assetGroup.Key);
                    foreach (WindowResult result in assetGroup)
                    {
                        MotionEvent_MotionWarp warp = result.Warp;
                        warp.RecordBakedProfile(
                            result.MeasuredAvatar,
                            result.AnimatorScale,
                            result.SourceFingerprint,
                            result.CumulativeLocalPositions,
                            result.CumulativePathLengths,
                            result.CumulativeYaw,
                            fromPlayMode: false);
                    }

                    EditorUtility.SetDirty(assetGroup.Key);
                }

                int invalidatedLegacyCount = InvalidateLegacyBakeData(changedAssets);

                AssetDatabase.StartAssetEditing();
                assetEditing = true;
                foreach (MotionSetAsset asset in changedAssets)
                    AssetDatabase.SaveAssetIfDirty(asset);
                AssetDatabase.StopAssetEditing();
                assetEditing = false;
                Undo.CollapseUndoOperations(group);

                _summary =
                    $"적용 완료 — MotionSet {changedAssets.Count}개, 리그별 프로필 {applicable.Length}개, "
                    + $"구형 유효 베이크 비활성 {invalidatedLegacyCount}개를 기록했습니다.\n\n"
                    + _summary;
            }
            catch
            {
                if (assetEditing)
                {
                    AssetDatabase.StopAssetEditing();
                    assetEditing = false;
                }
                Undo.RevertAllDownToGroup(group);
                foreach (MotionSetAsset asset in changedAssets)
                {
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssetIfDirty(asset);
                }

                throw;
            }
            WriteReport(applied: true);
        }

        private static int InvalidateLegacyBakeData(
            ISet<MotionSetAsset> changedAssets)
        {
            int invalidated = 0;
            foreach (string guid in AssetDatabase.FindAssets(
                         "t:MotionSetAsset",
                         new[] { "Assets/10.Datas" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MotionSetAsset asset = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path);
                if (asset?.motionSet == null)
                    continue;

                List<MotionEvent_MotionWarp> warps = CollectWarpEvents(asset.motionSet);
                bool needsChange = warps.Any(warp =>
                    warp.bakedValid
                    && warp.bakedFormatVersion
                    != MotionEvent_MotionWarp.CurrentBakeFormatVersion);
                needsChange |= warps.Any(warp =>
                    warp.bakedProfiles != null
                    && warp.bakedProfiles.Any(profile =>
                        profile == null
                        || profile.formatVersion
                        != MotionEvent_MotionWarp.CurrentBakeFormatVersion));
                if (!needsChange)
                    continue;

                if (changedAssets.Add(asset))
                    Undo.RegisterCompleteObjectUndo(asset, "워프 루트모션 구형 베이크 비활성");
                foreach (MotionEvent_MotionWarp warp in warps)
                {
                    if (warp.bakedValid
                        && warp.bakedFormatVersion
                        != MotionEvent_MotionWarp.CurrentBakeFormatVersion)
                    {
                        warp.bakedValid = false;
                        warp.bakedFromPlayMode = false;
                        invalidated++;
                    }

                    if (warp.bakedProfiles != null)
                    {
                        invalidated += warp.bakedProfiles.RemoveAll(profile =>
                            profile == null
                            || profile.formatVersion
                            != MotionEvent_MotionWarp.CurrentBakeFormatVersion);
                    }
                }

                EditorUtility.SetDirty(asset);
            }

            return invalidated;
        }

        private void ValidateAppliedData()
        {
            var errors = new List<string>();
            int applicable = 0;
            foreach (WindowResult result in _results)
            {
                if (!IsApplicableResult(result))
                {
                    errors.Add(
                        $"저작 오류({result.Status}): {result.AssetPath} / "
                        + $"{result.AvatarName} {result.AnimatorScale} — {result.Message}");
                    continue;
                }

                if (result.Warp == null)
                {
                    errors.Add($"윈도우 참조 누락: {result.AssetPath} / {result.AvatarName}");
                    continue;
                }
                applicable++;
                if (!result.Warp.TryGetBakedProfile(
                        result.MeasuredAvatar,
                        result.AnimatorScale,
                        out MotionWarpRootMotionBakeProfile profile))
                {
                    errors.Add(
                        $"누락: {result.AssetPath} / {result.AvatarName} {result.AnimatorScale}");
                    continue;
                }

                if (Mathf.Abs(profile.pathLen - result.MeasuredPath) > 0.0001f
                    || (profile.localTotal - result.MeasuredLocal).sqrMagnitude > 0.00000001f
                    || profile.sourceFingerprint != result.SourceFingerprint
                    || !TrajectoryMatches(profile, result))
                {
                    errors.Add(
                        $"값 불일치: {result.AssetPath} / {result.AvatarName} {result.AnimatorScale}");
                }
            }

            int currentProfileCount = 0;
            int legacyAdditiveCount = 0;
            foreach (string guid in AssetDatabase.FindAssets(
                         "t:MotionSetAsset",
                         new[] { "Assets/10.Datas" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MotionSetAsset asset = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path);
                if (asset?.motionSet == null)
                    continue;
                foreach (MotionEvent_MotionWarp warp in CollectWarpEvents(asset.motionSet))
                {
                    if (warp.preset == MotionWarpPreset.Custom
                        && warp.modifierType != MotionWarpModifierType.DeltaWarp)
                        legacyAdditiveCount++;
                    if (warp.bakedValid
                        && warp.bakedFormatVersion
                        != MotionEvent_MotionWarp.CurrentBakeFormatVersion)
                    {
                        errors.Add($"구형 단일 베이크 활성: {path}");
                    }

                    List<MotionWarpRootMotionBakeProfile> profiles = warp.bakedProfiles;
                    if (profiles == null)
                        continue;
                    for (int index = 0; index < profiles.Count; index++)
                    {
                        MotionWarpRootMotionBakeProfile profile = profiles[index];
                        if (profile == null
                            || profile.formatVersion
                            != MotionEvent_MotionWarp.CurrentBakeFormatVersion
                            || !profile.IsValid
                            || !profile.HasTrajectory
                            || string.IsNullOrEmpty(profile.sourceFingerprint))
                        {
                            errors.Add($"무효 프로필: {path} index={index}");
                            continue;
                        }

                        currentProfileCount++;
                        for (int otherIndex = index + 1;
                             otherIndex < profiles.Count;
                             otherIndex++)
                        {
                            MotionWarpRootMotionBakeProfile other = profiles[otherIndex];
                            if (other != null
                                && other.Matches(profile.avatar, profile.animatorScale))
                            {
                                errors.Add(
                                    $"중복 프로필: {path} index={index}/{otherIndex}");
                            }
                        }
                    }
                }
            }

            if (currentProfileCount < applicable)
                errors.Add(
                    $"현재 프로필 수 부족: {currentProfileCount} < 적용 대상 {applicable}");
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    $"MotionWarp 베이크 데이터 검증 실패 {errors.Count}건\n"
                    + string.Join("\n", errors.Take(40)));
            }

            Debug.Log(
                $"[MotionWarp] 데이터 검증 통과: 적용 대상 {applicable}, "
                + $"현재 프로필 {currentProfileCount}, 레거시 Additive 유지 {legacyAdditiveCount}");
        }

        private static bool TrajectoryMatches(
            MotionWarpRootMotionBakeProfile profile,
            WindowResult result)
        {
            int count = MotionEvent_MotionWarp.TrajectorySampleCount;
            if (profile.cumulativeLocalPositions?.Length != count
                || profile.cumulativePathLengths?.Length != count
                || profile.cumulativeYaw?.Length != count
                || result.CumulativeLocalPositions?.Length != count
                || result.CumulativePathLengths?.Length != count
                || result.CumulativeYaw?.Length != count)
                return false;

            for (int index = 0; index < count; index++)
            {
                if ((profile.cumulativeLocalPositions[index]
                     - result.CumulativeLocalPositions[index]).sqrMagnitude
                    > 0.00000001f
                    || Mathf.Abs(
                        profile.cumulativePathLengths[index]
                        - result.CumulativePathLengths[index]) > 0.0001f
                    || Mathf.Abs(
                        profile.cumulativeYaw[index]
                        - result.CumulativeYaw[index]) > 0.001f)
                    return false;
            }

            return true;
        }

        // ── 소유 관계 · 대상 선별 ──────────────────────────────────────────

        /// <summary>
        /// MotionSetAsset → 이 에셋을 실제로 재생하는 액터 프리팹 목록.
        /// 베이크 값은 프리팹 스케일이 반영된 실측치라 어떤 액터로 쟀는지가 결과를 좌우한다.
        /// </summary>
        private static Dictionary<MotionSetAsset, List<OwnerProfile>> BuildOwnership()
        {
            var ownership =
                new Dictionary<MotionSetAsset, List<OwnerProfile>>();
            string[] guids = AssetDatabase.FindAssets("t:Prefab");
            for (int index = 0; index < guids.Length; index++)
            {
                if (index % 64 == 0
                    && EditorUtility.DisplayCancelableProgressBar(
                        "워프 루트모션 측정",
                        $"액터 프리팹 스캔 {index}/{guids.Length}",
                        index / (float)Mathf.Max(1, guids.Length)))
                    throw new OperationCanceledException("프리팹 스캔 중 취소했습니다.");

                string guid = guids[index];
                string path = AssetDatabase.GUIDToAssetPath(guid);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                ActorAnimator actorAnimator = prefab != null
                    ? ResolveActorAnimator(prefab)
                    : null;
                Animator animator = ResolveDrivenAnimator(actorAnimator);
                if (animator == null)
                    continue;

                var owner = new OwnerProfile
                {
                    Prefab = prefab,
                    PrefabPath = path,
                    AnimatorPath = AnimationUtility.CalculateTransformPath(
                        animator.transform,
                        prefab.transform),
                    AvatarName = animator.avatar != null
                        ? animator.avatar.name
                        : "(Avatar 없음)",
                    Avatar = animator.avatar,
                    AnimatorScale = animator.transform.lossyScale,
                };

                foreach (ActorAnimationMotionSet motionSet in EnumerateActorMotionSets(actorAnimator))
                foreach (MotionSetAsset asset in EnumerateMotionSetAssets(motionSet))
                {
                    if (asset == null || asset.motionSet == null)
                        continue;
                    if (!ownership.TryGetValue(
                            asset,
                            out List<OwnerProfile> owners))
                    {
                        owners = new List<OwnerProfile>();
                        ownership.Add(asset, owners);
                    }

                    if (!owners.Any(existing =>
                            existing.Prefab == prefab
                            && existing.AnimatorPath == owner.AnimatorPath))
                        owners.Add(owner);
                }
            }

            return ownership;
        }

        private static IEnumerable<ActorAnimationMotionSet> EnumerateActorMotionSets(
            ActorAnimator actorAnimator)
        {
            var visited = new HashSet<ActorAnimationMotionSet>();
            var roots = new List<ActorAnimationMotionSet>();
            if (actorAnimator is PlayerActorAnimator playerAnimator
                && playerAnimator.PlayerMotionSet != null
                && playerAnimator.PlayerMotionSet.motionSets != null)
                roots.AddRange(playerAnimator.PlayerMotionSet.motionSets.Values);
            else if (actorAnimator.MotionSet != null)
                roots.Add(actorAnimator.MotionSet);

            foreach (ActorAnimationMotionSet root in roots)
            {
                ActorAnimationMotionSet cursor = root;
                // fallbackMotionSet은 순환 참조가 가능하므로 방문 집합으로 끊는다.
                while (cursor != null && visited.Add(cursor))
                {
                    yield return cursor;
                    cursor = cursor.fallbackMotionSet;
                }
            }
        }

        private static IEnumerable<MotionSetAsset> EnumerateMotionSetAssets(
            ActorAnimationMotionSet motionSet)
        {
            if (motionSet == null)
                yield break;
            if (motionSet.abilityMotions != null)
                foreach (MotionSetAsset asset in motionSet.abilityMotions.Values)
                    yield return asset;
            if (motionSet.motionSlots != null)
                foreach (MotionSetAsset asset in motionSet.motionSlots.Values)
                    yield return asset;
        }

        private static IEnumerable<OwnerProfile> GetDistinctOwnerProfiles(
            List<OwnerProfile> owners)
        {
            var distinct = new List<OwnerProfile>();
            foreach (OwnerProfile owner in owners)
            {
                if (distinct.Any(existing =>
                        existing.Avatar == owner.Avatar
                        && (existing.AnimatorScale - owner.AnimatorScale)
                        .sqrMagnitude <= 0.0001f))
                    continue;
                distinct.Add(owner);
                yield return owner;
            }
        }

        private HashSet<MotionSetAsset> ResolveScopeFilter()
        {
            if (_scope != Scope.선택한_MotionSet)
                return null;
            var selected = new HashSet<MotionSetAsset>(
                Selection.objects.OfType<MotionSetAsset>());
            if (selected.Count == 0)
                throw new InvalidOperationException(
                    "Project 창에서 MotionSetAsset을 선택하거나 범위를 전체로 바꾸세요.");
            return selected;
        }

        /// <summary>
        /// 아직 DeltaWarp 로 실행되지 않는 윈도우를 모은다.
        /// 이 윈도우들은 베이크를 채워도 런타임이 읽지 않으므로 저작 판단이 먼저 필요하다.
        /// </summary>
        private void CollectLegacyAdditiveWindows(
            IEnumerable<MotionSetAsset> assets,
            HashSet<MotionSetAsset> scopeFilter)
        {
            foreach (MotionSetAsset asset in assets)
            {
                if (scopeFilter != null && !scopeFilter.Contains(asset))
                    continue;
                if (asset.motionSet == null)
                    continue;
                foreach (MotionEvent_MotionWarp warp in CollectWarpEvents(asset.motionSet))
                {
                    if (warp.preset != MotionWarpPreset.Custom
                        || warp.modifierType == MotionWarpModifierType.DeltaWarp)
                        continue;
                    _legacyAdditiveWindows.Add(
                        $"{ShortAssetName(AssetDatabase.GetAssetPath(asset))} "
                        + $"[{warp.startTime:F2}~{warp.endTime:F2}] {warp.modifierType}");
                }
            }
        }

        /// <summary>액터 MotionSet 매핑이 없어 리그별 베이크 대상을 결정할 수 없는 윈도우를 보고한다.</summary>
        private void CollectUnmappedDeltaWarpWindows(
            IEnumerable<MotionSetAsset> ownedAssets,
            HashSet<MotionSetAsset> scopeFilter)
        {
            var owned = new HashSet<MotionSetAsset>(ownedAssets);
            foreach (string guid in AssetDatabase.FindAssets(
                         "t:MotionSetAsset",
                         new[] { "Assets/10.Datas" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                MotionSetAsset asset = AssetDatabase.LoadAssetAtPath<MotionSetAsset>(path);
                if (asset?.motionSet == null
                    || owned.Contains(asset)
                    || (scopeFilter != null && !scopeFilter.Contains(asset)))
                    continue;

                foreach (MotionEvent_MotionWarp warp in CollectWarpEvents(asset.motionSet))
                {
                    if (!UsesDeltaWarp(warp) || warp.endTime - warp.startTime <= 0f)
                        continue;
                    _unmappedDeltaWarpWindows.Add(
                        $"{path} [{warp.startTime:F2}~{warp.endTime:F2}]");
                }
            }
        }

        private static bool UsesDeltaWarp(MotionEvent_MotionWarp warp) =>
            warp != null
            && (warp.preset != MotionWarpPreset.Custom
                || warp.modifierType == MotionWarpModifierType.DeltaWarp);

        private bool HasBakeTargetWindow(
            MotionSetAsset asset,
            OwnerProfile owner)
            => asset.motionSet != null
               && CollectWarpEvents(asset.motionSet)
                   .Any(warp => IsBakeTarget(warp, owner));

        private bool IsBakeTarget(
            MotionEvent_MotionWarp warp,
            OwnerProfile owner)
        {
            if (warp.endTime - warp.startTime <= 0f)
                return false;
            // preset이 Custom이 아니면 ApplyPreset이 modifierType을 DeltaWarp로 덮어쓰므로
            // 직렬화된 modifierType과 무관하게 베이크가 쓰인다.
            if (!UsesDeltaWarp(warp))
                return false;
            if (_overwriteExisting || _forceIncludeBaked)
                return true;
            return !HasAttributedBake(warp, owner.Avatar, owner.AnimatorScale);
        }

        private void AddAssetFailure(
            MotionSetAsset asset,
            OwnerProfile owner,
            string status,
            string message)
        {
            _results.Add(new WindowResult
            {
                Asset = asset,
                AssetPath = AssetDatabase.GetAssetPath(asset),
                OwnerPrefabPath = owner?.PrefabPath,
                AnimatorPath = owner?.AnimatorPath,
                AvatarName = owner?.AvatarName,
                MeasuredAvatar = owner?.Avatar,
                AnimatorScale = owner?.AnimatorScale ?? Vector3.one,
                Status = status,
                Message = message,
            });
        }

        private static bool HasAttributedBake(
            MotionEvent_MotionWarp warp,
            Avatar avatar,
            Vector3 animatorScale)
        {
            if (warp.TryGetBakedProfile(avatar, animatorScale, out _))
                return true;
            return IsLegacyBakeCompatible(warp, avatar, animatorScale);
        }

        private static bool TryGetExistingBake(
            MotionEvent_MotionWarp warp,
            Avatar avatar,
            Vector3 animatorScale,
            out Vector3 localTotal,
            out float pathLen,
            out bool fromPlayMode,
            out Vector3[] cumulativeLocalPositions,
            out float[] cumulativePathLengths,
            out float[] cumulativeYaw)
        {
            if (warp.TryGetBakedProfile(
                    avatar,
                    animatorScale,
                    out MotionWarpRootMotionBakeProfile profile))
            {
                fromPlayMode =
                    profile.HasPlayModeReferenceTrajectory
                    && profile.playModeReferenceFormatVersion
                    == MotionEvent_MotionWarp.CurrentBakeFormatVersion;
                localTotal = fromPlayMode
                    ? profile.playModeReferenceLocalTotal
                    : profile.localTotal;
                pathLen = fromPlayMode
                    ? profile.playModeReferencePathLen
                    : profile.pathLen;
                cumulativeLocalPositions = fromPlayMode
                    ? profile.playModeReferenceCumulativeLocalPositions
                    : profile.cumulativeLocalPositions;
                cumulativePathLengths = fromPlayMode
                    ? profile.playModeReferenceCumulativePathLengths
                    : profile.cumulativePathLengths;
                cumulativeYaw = fromPlayMode
                    ? profile.playModeReferenceCumulativeYaw
                    : profile.cumulativeYaw;
                return true;
            }

            if (IsLegacyBakeCompatible(warp, avatar, animatorScale))
            {
                localTotal = warp.bakedLocalTotal;
                pathLen = warp.bakedPathLen;
                fromPlayMode =
                    warp.bakedFromPlayMode
                    && warp.bakedFormatVersion
                    == MotionEvent_MotionWarp.CurrentBakeFormatVersion;
                cumulativeLocalPositions = null;
                cumulativePathLengths = null;
                cumulativeYaw = null;
                return true;
            }

            localTotal = Vector3.zero;
            pathLen = 0f;
            fromPlayMode = false;
            cumulativeLocalPositions = null;
            cumulativePathLengths = null;
            cumulativeYaw = null;
            return false;
        }

        private static bool IsLegacyBakeCompatible(
            MotionEvent_MotionWarp warp,
            Avatar avatar,
            Vector3 animatorScale)
        {
            return warp.bakedValid
                   && warp.bakedPathLen > MinimumUsablePathLength
                   && warp.bakedFormatVersion
                   == MotionEvent_MotionWarp.CurrentBakeFormatVersion
                   && Mathf.Approximately(warp.bakedStartTime, warp.startTime)
                   && Mathf.Approximately(warp.bakedEndTime, warp.endTime)
                   && warp.bakedAvatar == avatar
                   && (warp.bakedAnimatorScale - animatorScale).sqrMagnitude
                   <= 0.0001f;
        }

        private static List<MotionEvent_MotionWarp> CollectWarpEvents(MotionSet set)
        {
            var results = new List<MotionEvent_MotionWarp>();
            AddEvents(set.globalEvents, results);
            AddMotionEvents(set.motions, results);
            if (set.layers != null)
            {
                foreach (MotionLayer layer in set.layers)
                {
                    if (layer == null)
                        continue;
                    AddEvents(layer.globalEvents, results);
                    AddMotionEvents(layer.motions, results);
                }
            }

            return results;
        }

        private static void AddMotionEvents(
            IEnumerable<MotionData> motions,
            ICollection<MotionEvent_MotionWarp> results)
        {
            if (motions == null)
                return;
            foreach (MotionData motion in motions)
                if (motion != null)
                    AddEvents(motion.events, results);
        }

        private static void AddEvents(
            IEnumerable<MotionEventBase> events,
            ICollection<MotionEvent_MotionWarp> results)
        {
            if (events == null)
                return;
            foreach (MotionEventBase motionEvent in events)
                if (motionEvent is MotionEvent_MotionWarp warp)
                    results.Add(warp);
        }

        // ── 보고 ──────────────────────────────────────────────────────────

        private string BuildAnalyzeSummary()
        {
            var builder = new StringBuilder();
            int windowCount = _results
                .Where(result => result.Warp != null)
                .GroupBy(result => new { result.Asset, result.Warp })
                .Count();
            int ok = _results.Count(result => result.Status == "OK");
            int inPlace = _results.Count(result => result.Status == "InPlace");
            int backtracking = _results.Count(
                result => result.Status == "Backtracking");
            int recordable = ok + backtracking;
            int failed = _results.Count - recordable;
            builder.AppendLine(
                $"측정 완료 — 리그별 프로필 {_results.Count}개 / 윈도우 {windowCount}개 "
                + $"(v3 기록 가능 {recordable}, 왕복 trajectory {backtracking}, "
                + $"비대상 이벤트 오류 {inPlace}, 문제 {failed})");
            builder.AppendLine(
                _overwriteExisting || _forceIncludeBaked
                    ? "대상: DeltaWarp의 모든 Avatar·스케일 프로필 (기존 프로필 포함)"
                    : "대상: 아직 같은 Avatar·스케일 베이크가 없는 DeltaWarp 프로필만. "
                      + "기존 프로필까지 보려면 위 덮어쓰기 옵션을 켜세요.");
            builder.AppendLine();

            foreach (IGrouping<string, WindowResult> group in _results
                         .GroupBy(result => result.Status)
                         .OrderBy(group => group.Key))
            {
                builder.AppendLine($"── {group.Key} ({group.Count()}) ──");
                foreach (WindowResult result in group.OrderBy(result => result.AssetPath))
                {
                    builder.Append(ShortAssetName(result.AssetPath));
                    builder.Append($" [{result.GlobalStart:F2}~{result.GlobalEnd:F2}]");
                    builder.Append($" path={result.MeasuredPath:F4}");
                    builder.Append($" |local|={result.MeasuredLocal.magnitude:F4}");
                    builder.Append($" @{ShortPrefabName(result.OwnerPrefabPath)}");
                    if (!string.IsNullOrEmpty(result.AnimatorPath))
                        builder.Append(
                            $" animator={result.AnimatorPath} avatar={result.AvatarName}"
                            + $" scale={result.AnimatorScale:F3}");
                    if (result.HasExistingBake)
                        builder.Append($" (기존 {result.ExistingPath:F4})");
                    if (!string.IsNullOrEmpty(result.Message))
                        builder.Append($" — {result.Message}");
                    builder.AppendLine();
                }

                builder.AppendLine();
            }

            if (_legacyAdditiveWindows.Count > 0)
            {
                builder.AppendLine(
                    $"── 레거시 Additive ({_legacyAdditiveWindows.Count}) — 베이크를 채워도 런타임이 읽지 않음 ──");
                builder.AppendLine(
                    "preset 이 Custom 이라 modifierType 이 그대로 쓰입니다. DeltaWarp 로 바꿀지는 "
                    + "돌진·잡기 등 의도된 Additive 인지 확인한 뒤 저작에서 결정하세요.");
                foreach (string row in _legacyAdditiveWindows)
                    builder.AppendLine(row);
                builder.AppendLine();
            }

            if (_unmappedDeltaWarpWindows.Count > 0)
            {
                builder.AppendLine(
                    $"── 미매핑 DeltaWarp ({_unmappedDeltaWarpWindows.Count}) — 런타임 소유 리그를 결정할 수 없음 ──");
                builder.AppendLine(
                    "신규 모션이면 ActorAnimationMotionSet에 먼저 연결한 뒤 다시 베이크하세요. "
                    + "사용하지 않는 에셋이면 정리 대상인지 확인하세요.");
                foreach (string row in _unmappedDeltaWarpWindows)
                    builder.AppendLine(row);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private string BuildVerifySummary()
        {
            WindowResult[] comparable = _results
                .Where(result => result.HasPlayModeReference)
                .ToArray();
            var builder = new StringBuilder();
            if (comparable.Length == 0)
            {
                builder.AppendLine(
                    "대조할 최신 Play Mode 기준 베이크가 없습니다. 이전 형식이나 일괄 베이크 결과는 "
                    + "독립 기준으로 인정하지 않습니다. 수정된 모션 에디터에서 대표 MotionSet 몇 개를 "
                    + "Play Mode 베이크한 뒤 다시 검증하세요.");
                builder.AppendLine();
                builder.Append(BuildAnalyzeSummary());
                return builder.ToString();
            }

            int matched = 0;
            var rows = new List<string>();
            foreach (WindowResult result in comparable.OrderBy(result => result.AssetPath))
            {
                float pathDifference = Mathf.Abs(
                    result.MeasuredPath - result.ExistingPath);
                float pathTolerance = Mathf.Max(
                    VerifyAbsoluteTolerance,
                    result.ExistingPath * VerifyRelativeTolerance);
                float localDifference = Vector3.Distance(
                    result.MeasuredLocal,
                    result.ExistingLocal);
                float localTolerance = Mathf.Max(
                    VerifyAbsoluteTolerance,
                    result.ExistingLocal.magnitude * VerifyRelativeTolerance);
                bool hasTrajectoryReference =
                    TryMeasureTrajectoryDifference(
                        result,
                        out float maximumPositionDifference,
                        out float maximumPathDifference,
                        out float maximumYawDifference);
                bool isMatch = pathDifference <= pathTolerance
                               && localDifference <= localTolerance
                               && hasTrajectoryReference
                               && maximumPositionDifference <= localTolerance
                               && maximumPathDifference <= pathTolerance
                               && maximumYawDifference <= 2f;
                if (isMatch)
                    matched++;
                rows.Add(
                    $"{(isMatch ? "일치" : "불일치")} | {ShortAssetName(result.AssetPath)} "
                    + $"@{ShortPrefabName(result.OwnerPrefabPath)} "
                    + $"avatar={result.AvatarName} scale={result.AnimatorScale:F3} "
                    + $"| path 오프라인 {result.MeasuredPath:F4} vs PlayMode {result.ExistingPath:F4} "
                    + $"(차이 {pathDifference:F4}, 허용 {pathTolerance:F4}) "
                    + $"| local vector 차이 {localDifference:F4}, 허용 {localTolerance:F4} "
                    + $"| trajectory 최대차 position={maximumPositionDifference:F4}, "
                    + $"path={maximumPathDifference:F4}, yaw={maximumYawDifference:F2}°");
            }

            float ratio = matched / (float)comparable.Length;
            _verificationPassed = matched == comparable.Length;
            builder.AppendLine(
                $"검증 결과 — 대조 {comparable.Length}개 중 {matched}개 일치 ({ratio:P0})");
            builder.AppendLine(
                _verificationPassed
                    ? "오프라인 측정이 Play Mode 측정과 동등합니다. 적용해도 됩니다."
                    : "불일치가 하나라도 있어 적용을 차단했습니다. 아래 항목의 Animator·Avatar·레이어·클립 매핑을 확인하세요.");
            builder.AppendLine();
            foreach (string row in rows)
                builder.AppendLine(row);

            return builder.ToString();
        }

        private static bool TryMeasureTrajectoryDifference(
            WindowResult result,
            out float maximumPositionDifference,
            out float maximumPathDifference,
            out float maximumYawDifference)
        {
            maximumPositionDifference = 0f;
            maximumPathDifference = 0f;
            maximumYawDifference = 0f;
            int count = MotionEvent_MotionWarp.TrajectorySampleCount;
            if (result.CumulativeLocalPositions?.Length != count
                || result.CumulativePathLengths?.Length != count
                || result.CumulativeYaw?.Length != count
                || result.ExistingCumulativeLocalPositions?.Length != count
                || result.ExistingCumulativePathLengths?.Length != count
                || result.ExistingCumulativeYaw?.Length != count)
                return false;

            for (int index = 0; index < count; index++)
            {
                maximumPositionDifference = Mathf.Max(
                    maximumPositionDifference,
                    Vector3.Distance(
                        result.CumulativeLocalPositions[index],
                        result.ExistingCumulativeLocalPositions[index]));
                maximumPathDifference = Mathf.Max(
                    maximumPathDifference,
                    Mathf.Abs(
                        result.CumulativePathLengths[index]
                        - result.ExistingCumulativePathLengths[index]));
                maximumYawDifference = Mathf.Max(
                    maximumYawDifference,
                    Mathf.Abs(
                        result.CumulativeYaw[index]
                        - result.ExistingCumulativeYaw[index]));
            }

            return true;
        }

        /// <summary>
        /// 상위 폴더 두 단계를 남긴 식별 이름.
        /// Humanoid/Katana 와 Player/Katana 처럼 파일명이 같은 에셋이 쌍으로 존재하므로
        /// 파일명만 찍으면 리포트에서 서로 구분되지 않는다.
        /// </summary>
        private static string ShortAssetName(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
                return "(경로 없음)";
            string[] segments = assetPath.Split('/');
            int start = Mathf.Max(0, segments.Length - 3);
            string name = string.Join("/", segments, start, segments.Length - start);
            return name.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)
                ? name.Substring(0, name.Length - ".asset".Length)
                : name;
        }

        private static string ShortPrefabName(string prefabPath)
            => string.IsNullOrEmpty(prefabPath)
                ? "(프리팹 없음)"
                : System.IO.Path.GetFileNameWithoutExtension(prefabPath);

        private void WriteReport(bool applied)
        {
            var report = new Report
            {
                generatedAt = DateTime.Now.ToString("s"),
                applied = applied,
                prefabCount = _results.Select(result => result.OwnerPrefabPath).Distinct().Count(),
                assetCount = _results.Select(result => result.AssetPath).Distinct().Count(),
                windowCount = _results
                    .Where(result => result.Warp != null)
                    .GroupBy(result => new { result.Asset, result.Warp })
                    .Count(),
                profileCount = _results.Count,
                okCount = _results.Count(IsApplicableResult),
                inPlaceCount = _results.Count(result => result.Status == "InPlace"),
                backtrackingCount = _results.Count(
                    result => result.Status == "Backtracking"),
                failedCount = _results.Count(result => !IsApplicableResult(result)),
                unmappedWindowCount = _unmappedDeltaWarpWindows.Count,
            };
            report.unmappedWindows.AddRange(_unmappedDeltaWarpWindows);
            foreach (WindowResult result in _results)
                report.rows.Add(new ReportRow
                {
                    asset = result.AssetPath,
                    ownerPrefab = result.OwnerPrefabPath,
                    animatorPath = result.AnimatorPath,
                    avatar = result.AvatarName,
                    animatorScale = result.AnimatorScale,
                    windowStart = result.GlobalStart,
                    windowEnd = result.GlobalEnd,
                    measuredPathLen = result.MeasuredPath,
                    measuredLocalMagnitude = result.MeasuredLocal.magnitude,
                    measuredYaw = result.MeasuredYaw,
                    trajectorySampleCount =
                        result.CumulativeLocalPositions?.Length ?? 0,
                    sourceFingerprint = result.SourceFingerprint,
                    existingPathLen = result.ExistingPath,
                    status = result.Status,
                    message = result.Message,
                });

            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        }
    }

    /// <summary>출시 빌드 전에 런타임에서 참조되는 DeltaWarp 프로필의 완전성을 강제한다.</summary>
    internal sealed class MotionWarpBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 100;

        public void OnPreprocessBuild(BuildReport report)
        {
            bool isDevelopmentBuild =
                (report.summary.options & BuildOptions.Development) != 0;
            if (!WarpRootMotionBatchBakeWindow.TryValidateMappedProfiles(
                    out string validationReport))
            {
                if (!isDevelopmentBuild)
                    throw new BuildFailedException(validationReport);
                Debug.LogWarning(
                    "Development Build에서는 MotionWarp 런타임 캐시 폴백을 허용합니다.\n"
                    + validationReport);
                return;
            }

            if (validationReport.IndexOf(
                    "미매핑 경고",
                    StringComparison.Ordinal) >= 0)
                Debug.LogWarning(validationReport);
        }
    }
}
#endif
