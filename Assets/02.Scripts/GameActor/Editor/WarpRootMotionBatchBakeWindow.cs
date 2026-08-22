#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
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
    /// 이 총량은 런타임 캐시(_rootTotalCache)가 채워지기 전, 즉 세션 첫 시전에서는 존재하지 않아
    /// MotionWarpController가 방향 스티어만 하는 폴백으로 내려간다. 콤보 한 단은 세션 안에서 재시전이
    /// 드물기 때문에 실전 스윙 대부분이 이 폴백에 걸린다. bakedValid 시드가 있으면 첫 시전부터 정확 모드다.
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

        // 이 값보다 짧은 경로는 베이크해도 DeltaWarp 정확 모드가 의미를 갖지 못한다.
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
            public bool HasExistingBake;
            public bool HasPlayModeReference;
            public Vector3 ExistingLocal;
            public float ExistingPath;
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
            public int failedCount;
            public List<ReportRow> rows = new();
        }

        [SerializeField] private Scope _scope = Scope.전체_프로젝트;

        [Tooltip("이미 같은 Avatar·스케일 프로필이 있는 윈도우도 다시 측정한다. 끄면 누락 프로필만 채운다.")]
        [SerializeField] private bool _overwriteExisting;

        private readonly List<WindowResult> _results = new();

        // preset 이 DeltaWarp 를 강제하지 않는 순수 레거시 윈도우. 자동 변환하지 않고 보고만 한다
        // (돌진·잡기처럼 Additive 를 의도한 저작일 수 있어 일괄 변경이 위험하다).
        private readonly List<string> _legacyAdditiveWindows = new();

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
               && _results.Any(result => result.Status == "OK");

        private enum RunMode
        {
            Analyze,
            Verify,
        }

        // ── 측정 파이프라인 ────────────────────────────────────────────────

        private void Run(RunMode mode)
        {
            _results.Clear();
            _legacyAdditiveWindows.Clear();
            _verificationPassed = false;
            // 검증은 기존 베이크와 대조하는 것이 목적이라 토글과 무관하게 베이크된 윈도우까지 다시 측정한다.
            _forceIncludeBaked = mode == RunMode.Verify;
            try
            {
                // 프로젝트 전체 프리팹 스캔은 비싸므로 범위 오류를 먼저 걸러낸다.
                HashSet<MotionSetAsset> scopeFilter = ResolveScopeFilter();
                Dictionary<MotionSetAsset, List<OwnerProfile>> ownership = BuildOwnership();
                CollectLegacyAdditiveWindows(ownership.Keys, scopeFilter);

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
            bool startedAnimationMode = false;
            try
            {
                root = PrefabUtility.LoadPrefabContents(prefabPath);
                if (!AnimationMode.InAnimationMode())
                {
                    AnimationMode.StartAnimationMode();
                    startedAnimationMode = true;
                }

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

                    // Player는 OnAnimatorMove에서 deltaPosition을 직접 소비하므로 applyRootMotion이 꺼져 있다.
                    // AnimationMode는 변환된 루트 위치를 읽기 때문에 프리팹 복제본에서만 이를 켠다.
                    animator.applyRootMotion = true;
                    MeasureAsset(
                        job.Asset,
                        job.Owner,
                        animator,
                        job.Owner.AnimatorPath,
                        job.Owner.AvatarName);
                }
            }
            finally
            {
                if (startedAnimationMode && AnimationMode.InAnimationMode())
                    AnimationMode.StopAnimationMode();
                if (root != null)
                    PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private void MeasureAsset(
            MotionSetAsset asset,
            OwnerProfile owner,
            Animator animator,
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
                    out bool fromPlayMode);

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
                    HasExistingBake = hasExistingBake,
                    HasPlayModeReference = hasExistingBake && fromPlayMode,
                    ExistingLocal = existingLocal,
                    ExistingPath = existingPath,
                });
            }

            if (targets.Count == 0)
                return;

            SampleTimeline(set, animator, targets);

            foreach (WindowResult result in targets)
            {
                if (result.MeasuredPath <= MinimumUsablePathLength)
                {
                    result.Status = "NoRootMotion";
                    result.Message =
                        "측정 경로가 0입니다. 클립에 루트모션이 없거나 샘플링이 루트를 움직이지 못했습니다. "
                        + "0을 베이크하면 런타임이 폴백으로 내려가므로 기록하지 않습니다.";
                }
                else if (result.MeasuredPath < InPlaceSuspicionPathLength)
                {
                    result.Status = "InPlace";
                    result.Message =
                        "제자리에 가까운 모션입니다. 워프 보정이 사실상 전량 보정이 되므로 자동 적용하지 않습니다.";
                }
                else if (result.MeasuredLocal.magnitude / result.MeasuredPath
                         < MinimumNetDisplacementRatio)
                {
                    result.Status = "Backtracking";
                    result.Message =
                        "전진 후 복귀하는 루트 경로입니다. 총 경로에 비해 순 변위가 작아 "
                        + "타겟 착지가 발 동작을 훼손할 수 있으므로 자동 적용하지 않습니다.";
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
        /// MotionSet 타임라인을 런타임과 같은 시간 매핑으로 훑으며 프레임 루트 변위를 누적한다.
        ///
        /// 모션 경계에서는 델타를 버린다. AnimationMode는 클립 시작 기준 절대 루트 위치를 쓰므로
        /// 다음 클립의 첫 샘플에서 위치가 원점으로 되돌아가는데, 런타임 Animator.deltaPosition에는
        /// 그런 점프가 없기 때문이다.
        ///
        /// 샘플링 중 액터 회전은 고정이므로 월드 수평 델타가 곧 런타임의
        /// Inverse(rotation) * horizontal(=facing 불변 로컬 변위)과 같은 정의가 된다.
        /// </summary>
        private static void SampleTimeline(
            MotionSet set,
            Animator animator,
            List<WindowResult> targets)
        {
            float maxEnd = targets.Max(target => target.GlobalEnd);
            maxEnd = Mathf.Min(maxEnd, set.TotalDuration);
            int sampleCount = Mathf.Max(1, Mathf.CeilToInt(maxEnd * SampleRate));

            Transform rootTransform = animator.transform;
            int previousMotionIndex = -1;
            Vector3 previousPosition = Vector3.zero;

            for (int sample = 0; sample <= sampleCount; sample++)
            {
                float globalTime = maxEnd * (sample / (float)sampleCount);
                if (!set.GetMotionAtTime(globalTime, out int motionIndex, out float localTime)
                    || motionIndex < 0
                    || motionIndex >= set.motions.Count)
                    continue;

                MotionData motion = set.motions[motionIndex];
                if (motion?.motionClip == null)
                    continue;

                float clipTime = motion.ClipStartTime
                                 + localTime * Mathf.Max(0.0001f, motion.playbackSpeed);

                AnimationMode.BeginSampling();
                AnimationMode.SampleAnimationClip(animator.gameObject, motion.motionClip, clipTime);
                AnimationMode.EndSampling();

                Vector3 position = rootTransform.position;
                if (motionIndex == previousMotionIndex)
                {
                    Vector3 delta = position - previousPosition;
                    delta.y = 0f;
                    if (delta.sqrMagnitude > 1e-12f)
                    {
                        foreach (WindowResult target in targets)
                        {
                            if (globalTime < target.GlobalStart || globalTime > target.GlobalEnd)
                                continue;
                            target.MeasuredLocal += delta;
                            target.MeasuredPath += delta.magnitude;
                        }
                    }
                }

                previousMotionIndex = motionIndex;
                previousPosition = position;
            }
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
        {
            if (!_verificationPassed)
            {
                _summary = "적용 차단 — 기존 Play Mode 베이크와의 검증을 100% 통과해야 합니다.";
                return;
            }

            WindowResult[] applicable = _results
                .Where(result => result.Status == "OK")
                .ToArray();
            if (applicable.Length == 0)
                return;

            if (!EditorUtility.DisplayDialog(
                    "워프 루트모션 프로필 적용",
                    $"워프 루트모션 프로필 {applicable.Length}개를 기록합니다. 계속할까요?",
                    "적용",
                    "취소"))
                return;

            var changedAssets = new List<MotionSetAsset>();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("워프 루트모션 일괄 베이크");
            try
            {
                foreach (IGrouping<MotionSetAsset, WindowResult> assetGroup in
                         applicable.GroupBy(result => result.Asset))
                {
                    Undo.RegisterCompleteObjectUndo(assetGroup.Key, "워프 루트모션 일괄 베이크");
                    foreach (WindowResult result in assetGroup)
                    {
                        MotionEvent_MotionWarp warp = result.Warp;
                        warp.RecordBakedProfile(
                            result.MeasuredAvatar,
                            result.AnimatorScale,
                            result.MeasuredLocal,
                            result.MeasuredPath,
                            fromPlayMode: false);
                    }

                    EditorUtility.SetDirty(assetGroup.Key);
                    changedAssets.Add(assetGroup.Key);
                }

                foreach (MotionSetAsset asset in changedAssets)
                    AssetDatabase.SaveAssetIfDirty(asset);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (MotionSetAsset asset in changedAssets)
                {
                    EditorUtility.SetDirty(asset);
                    AssetDatabase.SaveAssetIfDirty(asset);
                }

                throw;
            }

            _summary =
                $"적용 완료 — MotionSet {changedAssets.Count}개, 리그별 프로필 {applicable.Length}개를 기록했습니다.\n\n"
                + _summary;
            WriteReport(applied: true);
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
            bool usesDeltaWarp = warp.preset != MotionWarpPreset.Custom
                                 || warp.modifierType == MotionWarpModifierType.DeltaWarp;
            if (!usesDeltaWarp)
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
            out bool fromPlayMode)
        {
            if (warp.TryGetBakedProfile(
                    avatar,
                    animatorScale,
                    out MotionWarpRootMotionBakeProfile profile))
            {
                fromPlayMode =
                    profile.HasPlayModeReference
                    && profile.playModeReferenceFormatVersion
                    == MotionEvent_MotionWarp.CurrentBakeFormatVersion;
                localTotal = fromPlayMode
                    ? profile.playModeReferenceLocalTotal
                    : profile.localTotal;
                pathLen = fromPlayMode
                    ? profile.playModeReferencePathLen
                    : profile.pathLen;
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
                return true;
            }

            localTotal = Vector3.zero;
            pathLen = 0f;
            fromPlayMode = false;
            return false;
        }

        private static bool IsLegacyBakeCompatible(
            MotionEvent_MotionWarp warp,
            Avatar avatar,
            Vector3 animatorScale)
        {
            return warp.bakedValid
                   && warp.bakedPathLen > MinimumUsablePathLength
                   && warp.bakedFormatVersion > 0
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
            int failed = _results.Count - ok - inPlace - backtracking;
            builder.AppendLine(
                $"측정 완료 — 리그별 프로필 {_results.Count}개 / 윈도우 {windowCount}개 "
                + $"(자동 기록 가능 {ok}, 제자리 {inPlace}, 왕복 경로 {backtracking}, 문제 {failed})");
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
                bool isMatch = pathDifference <= pathTolerance
                               && localDifference <= localTolerance;
                if (isMatch)
                    matched++;
                rows.Add(
                    $"{(isMatch ? "일치" : "불일치")} | {ShortAssetName(result.AssetPath)} "
                    + $"@{ShortPrefabName(result.OwnerPrefabPath)} "
                    + $"avatar={result.AvatarName} scale={result.AnimatorScale:F3} "
                    + $"| path 오프라인 {result.MeasuredPath:F4} vs PlayMode {result.ExistingPath:F4} "
                    + $"(차이 {pathDifference:F4}, 허용 {pathTolerance:F4}) "
                    + $"| local vector 차이 {localDifference:F4}, 허용 {localTolerance:F4}");
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
                okCount = _results.Count(result => result.Status == "OK"),
                inPlaceCount = _results.Count(result => result.Status == "InPlace"),
                failedCount = _results.Count(result => result.Status is not ("OK" or "InPlace")),
            };
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
                    existingPathLen = result.ExistingPath,
                    status = result.Status,
                    message = result.Message,
                });

            File.WriteAllText(ReportPath, JsonUtility.ToJson(report, true));
        }
    }
}
#endif
