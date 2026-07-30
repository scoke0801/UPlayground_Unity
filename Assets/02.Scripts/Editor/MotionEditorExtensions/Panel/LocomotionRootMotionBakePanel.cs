using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AYellowpaper.SerializedCollections;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Gameplay.Tag;
using UPlayGround.MovementController;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 플레이어 로코모션 18개 슬롯을 120Hz 프리뷰로 순회해
    /// Turn 총 yaw와 Walk/Run/Sprint 기준 속도를 일괄 저장한다.
    /// 측정 중에는 에셋을 변경하지 않고 전체 성공 뒤 한 번만 커밋한다.
    /// </summary>
    [InitializeOnLoad]
    public sealed class LocomotionRootMotionBakePanel :
        IMotionEditorPanel,
        IMotionEditorPanelLifecycle
    {
        private const string AutomationPendingKey =
            "UPlayGround.LocomotionBake.AutomationPending";
        private const string AutomationFinishingKey =
            "UPlayGround.LocomotionBake.AutomationFinishing";
        private const string AutomationSucceededKey =
            "UPlayGround.LocomotionBake.AutomationSucceeded";
        private const string PreviewCatalogPath =
            "Assets/10.Datas/System/MotionPreviewCatalog.asset";
        private const string PlayerPreviewDisplayName = "ActorDef_Player";
        private const float SampleStep = 1f / 120f;
        private const int SamplesPerEditorTick = 12;
        private static double _automationWaitStarted;

        private sealed class BakeEntry
        {
            public GameplayTag Slot;
            public string SlotId;
            public ActorAnimationMotionSet Owner;
            public bool BakeYaw;
            public bool BakeReferenceSpeed;
            public float Duration;
            public float PathLength;
            public Quaternion Rotation = Quaternion.identity;
            public float Yaw;
            public float ReferenceSpeed;
            public float SampleTime;
            public string FallbackSource;
        }

        private static readonly GameplayTag[] ReferenceSpeedSlots =
        {
            MotionTags.Walk,
            MotionTags.Run,
            MotionTags.Sprint,
        };

        private static readonly GameplayTag[] TurnSlots =
        {
            MotionTags.Walk_Turn_L45,
            MotionTags.Walk_Turn_R45,
            MotionTags.Walk_Turn_L90,
            MotionTags.Walk_Turn_R90,
            MotionTags.Walk_Turn_180,
            MotionTags.Run_Turn_L45,
            MotionTags.Run_Turn_R45,
            MotionTags.Run_Turn_L90,
            MotionTags.Run_Turn_R90,
            MotionTags.Run_Turn_180,
            MotionTags.Sprint_Turn_L45,
            MotionTags.Sprint_Turn_R45,
            MotionTags.Sprint_Turn_L90,
            MotionTags.Sprint_Turn_R90,
            MotionTags.Sprint_Turn_180,
        };

        private readonly List<BakeEntry> _entries = new();
        private readonly List<string> _errors = new();
        private IMotionEditorContext _context;
        private ActorAnimationMotionSet _rootTarget;
        private int _entryIndex = -1;
        private bool _active;
        private bool _waitingForPlayback;
        private float _previousCaptureDelta;
        private string _originalSlotId;
        private Vector3 _originPosition;
        private Quaternion _originRotation;
        private string _summary;

        static LocomotionRootMotionBakePanel()
        {
            EditorApplication.playModeStateChanged -=
                OnAutomationPlayModeStateChanged;
            EditorApplication.playModeStateChanged +=
                OnAutomationPlayModeStateChanged;
        }

        public string Title => "로코모션 루트 베이크";
        public int Order => 310;

        public bool IsAvailable(IMotionEditorContext context) =>
            context?.Subject is IMotionPreviewRootMotion
            && ResolveRootTarget(context.Catalog) != null;

        public void OnGUI(IMotionEditorContext context)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField(
                    "로코모션 루트모션 일괄 베이크",
                    EditorStyles.boldLabel);
                EditorGUILayout.HelpBox(
                    "Walk/Run/Sprint 3종과 Turn 15종을 자동 순회합니다. 모든 슬롯 측정이 성공한 경우에만 ActorAnimationMotionSet에 저장합니다.",
                    MessageType.Info);

                bool canBake = !_active
                               && Application.isPlaying
                               && context.Subject?.Root != null
                               && ResolveRootTarget(context.Catalog) != null;
                using (new EditorGUI.DisabledScope(!canBake))
                {
                    if (GUILayout.Button(
                        _active
                                ? $"베이크 중... ({_entryIndex + 1}/{_entries.Count})"
                                : "Bake All Locomotion Root Motion",
                            GUILayout.Height(26f)))
                        BeginBatch(context);
                }

                if (_active)
                {
                    Rect progressRect = EditorGUILayout.GetControlRect(false, 18f);
                    float progress = _entries.Count > 0
                        ? Mathf.Clamp01((float)(_entryIndex + 1) / _entries.Count)
                        : 0f;
                    EditorGUI.ProgressBar(
                        progressRect,
                        progress,
                        _entryIndex >= 0 && _entryIndex < _entries.Count
                            ? _entries[_entryIndex].Slot.TagName
                            : "준비 중");
                }

                if (!string.IsNullOrEmpty(_summary))
                    EditorGUILayout.HelpBox(
                        _summary,
                        _errors.Count > 0 ? MessageType.Error : MessageType.None);
            }
        }

        public void OnSceneGUI(IMotionEditorContext context)
        {
        }

        public void OnPlaybackStateChanged(
            IMotionEditorContext context,
            MotionPreviewPlaybackState state)
        {
            if (!_active || _waitingForPlayback)
                return;

            if (state == MotionPreviewPlaybackState.Stopped
                && _entryIndex >= 0
                && _entryIndex < _entries.Count
                && context.PlaybackTime + 0.001f
                < _entries[_entryIndex].Duration)
                Abort("사용자 또는 외부 시스템이 재생을 중단했습니다.");
        }

        public void OnEditorClosed(IMotionEditorContext context)
        {
            if (_active)
                Abort("에디터가 닫혔습니다.");
        }

        /// <summary>
        /// 현재 Motion Editor 프리뷰 대상에서 18개 로코모션 슬롯 베이크를 시작한다.
        /// </summary>
        public bool BeginBatch(IMotionEditorContext context)
        {
            _errors.Clear();
            _entries.Clear();
            _rootTarget = ResolveRootTarget(context.Catalog);
            if (_rootTarget == null)
                return false;

            CollectEntries(ReferenceSpeedSlots, false, true);
            CollectEntries(TurnSlots, true, false);
            if (_errors.Count > 0 || _entries.Count != 18)
            {
                _summary = BuildFailureSummary("베이크 대상 수집 실패");
                FinishAutomation(false);
                return false;
            }

            _context = context;
            _originalSlotId = context.SelectedSlotId;
            _originPosition = context.Subject.Root.transform.position;
            _originRotation = context.Subject.Root.transform.rotation;
            _entryIndex = -1;
            _active = true;
            _waitingForPlayback = false;
            _summary = "베이크 준비 중...";
            _previousCaptureDelta = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 120f;
            EditorApplication.update += Tick;
            try
            {
                StartNextEntry();
                return _active;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                Abort($"베이크 시작 예외: {exception.Message}");
                return false;
            }
        }

        /// <summary>
        /// 플레이어 프리뷰 씬 진입부터 18개 슬롯 저장까지 자동 실행한다.
        /// Unity 명령행의 -executeMethod 진입점으로도 사용할 수 있다.
        /// </summary>
        [MenuItem("Tools/MotionSet/플레이어 로코모션 루트모션 자동 베이크")]
        public static void BakePlayerLocomotion()
        {
            MotionPreviewCatalogSO catalog =
                AssetDatabase.LoadAssetAtPath<MotionPreviewCatalogSO>(
                    PreviewCatalogPath);
            MotionPreviewCatalogSO.SubjectEntry playerEntry =
                catalog?.subjects?.FirstOrDefault(
                    entry => entry != null
                             && entry.displayName == PlayerPreviewDisplayName);
            if (playerEntry == null)
            {
                FailAutomation(
                    $"프리뷰 카탈로그에서 {PlayerPreviewDisplayName} 대상을 찾지 못했습니다.");
                return;
            }

            EditorPrefs.SetBool(AutomationPendingKey, true);
            EditorPrefs.SetBool(AutomationFinishingKey, false);
            EditorPrefs.SetBool(AutomationSucceededKey, false);
            if (!MotionSetEditorWindow.OpenPreviewSubject(
                    catalog,
                    playerEntry.id))
            {
                FailAutomation("Motion Editor 프리뷰 대상을 열지 못했습니다.");
            }
        }

        private void CollectEntries(
            IReadOnlyList<GameplayTag> slots,
            bool bakeYaw,
            bool bakeReferenceSpeed)
        {
            foreach (GameplayTag slot in slots)
            {
                if (!TryFindOwner(_rootTarget, slot, out ActorAnimationMotionSet owner)
                    || owner.GetMotionSetAsset(slot) == null)
                {
                    _errors.Add($"{slot.TagName}: MotionSet 또는 소유 ActorAnimationMotionSet 누락");
                    continue;
                }

                _entries.Add(new BakeEntry
                {
                    Slot = slot,
                    SlotId = slot.TagName,
                    Owner = owner,
                    BakeYaw = bakeYaw,
                    BakeReferenceSpeed = bakeReferenceSpeed,
                });
            }
        }

        private void StartNextEntry()
        {
            _entryIndex++;
            if (_entryIndex >= _entries.Count)
            {
                Commit();
                return;
            }

            BakeEntry entry = _entries[_entryIndex];
            _waitingForPlayback = true;
            _context.Stop();
            if (!_context.SelectSlot(entry.SlotId)
                || _context.CurrentSet == null)
            {
                Abort($"{entry.Slot.TagName}: 슬롯 선택 또는 MotionSet 해석 실패");
                return;
            }

            entry.Duration = _context.CurrentSet.TotalDuration;
            if (entry.Duration <= 0.001f)
            {
                Abort($"{entry.Slot.TagName}: 유효한 재생 길이가 없습니다.");
                return;
            }

            entry.PathLength = 0f;
            entry.Rotation = Quaternion.identity;
            entry.SampleTime = 0f;
            _context.Subject.Refresh();
            if (_context.Subject is not IMotionPreviewRootMotion rootMotion)
            {
                Abort($"{entry.Slot.TagName}: 루트모션 프리뷰 계약 소실");
                return;
            }

            rootMotion.Teleport(_originPosition, _originRotation);
            _context.SetPlaybackTime(0f);
            _context.Play();
            _waitingForPlayback = false;
            _summary =
                $"베이크 중 {_entryIndex + 1}/{_entries.Count}: {entry.Slot.TagName}";
            _context.Repaint();
        }

        private void Tick()
        {
            try
            {
                TickCore();
            }
            catch (Exception exception)
            {
                if (!_active)
                    return;

                Debug.LogException(exception);
                Abort($"루트모션 샘플링 예외: {exception.Message}");
            }
        }

        private void TickCore()
        {
            if (!_active || _context == null)
                return;
            if (!Application.isPlaying
                || _context.Subject is not IMotionPreviewRootMotion rootMotion
                || _context.Subject.Root == null)
            {
                Abort("대상 또는 Play Mode가 소실되었습니다.");
                return;
            }

            if (_waitingForPlayback)
                return;

            BakeEntry entry = _entries[_entryIndex];
            for (int sample = 0;
                 sample < SamplesPerEditorTick
                 && entry.SampleTime + 0.000001f < entry.Duration;
                 sample++)
            {
                float nextTime = Mathf.Min(
                    entry.SampleTime + SampleStep,
                    entry.Duration);
                if (!_context.TrySampleRootMotion(
                        entry.SampleTime,
                        nextTime,
                        out Vector3 deltaPosition,
                        out Quaternion deltaRotation))
                {
                    Abort($"{entry.Slot.TagName}: 루트모션 샘플 평가 실패");
                    return;
                }

                Vector3 up = _context.Subject.Root.transform.up;
                entry.PathLength += Vector3.ProjectOnPlane(
                    deltaPosition,
                    up).magnitude;
                entry.Rotation *= deltaRotation;
                entry.SampleTime = nextTime;
            }

            if (entry.SampleTime + 0.0001f < entry.Duration)
                return;

            CalculateResult(entry, _context.Subject.Root.transform.up);
            ApplyInPlaceFallback(entry);
            if (entry.BakeReferenceSpeed && entry.ReferenceSpeed <= 0.01f)
            {
                Abort($"{entry.Slot.TagName}: 기준 속도가 0에 가까워 저장하지 않았습니다.");
                return;
            }
            if (entry.BakeYaw && entry.Yaw <= 1f)
            {
                Abort($"{entry.Slot.TagName}: 총 yaw가 1° 이하라 저장하지 않았습니다.");
                return;
            }
            StartNextEntry();
        }

        private static void CalculateResult(BakeEntry entry, Vector3 up)
        {
            Vector3 referenceForward = Vector3.ProjectOnPlane(
                Vector3.forward,
                up);
            if (referenceForward.sqrMagnitude <= 0.0001f)
                referenceForward = Vector3.ProjectOnPlane(Vector3.right, up);
            referenceForward.Normalize();

            Vector3 rotatedForward = Vector3.ProjectOnPlane(
                entry.Rotation * referenceForward,
                up);
            entry.Yaw = rotatedForward.sqrMagnitude > 0.0001f
                ? Mathf.Abs(Vector3.SignedAngle(
                    referenceForward,
                    rotatedForward.normalized,
                    up))
                : 0f;
            entry.ReferenceSpeed =
                entry.PathLength / Mathf.Max(0.001f, entry.Duration);
        }

        private void ApplyInPlaceFallback(BakeEntry entry)
        {
            // 인플레이스 임포트는 Animator.deltaPosition/deltaRotation이 0이다.
            // Turn 슬롯명은 저작 의도 각도를, 이동은 프리뷰 캐릭터의 명시적
            // 기준 클립 속도를 사용해 런타임 추정 없이 에디터 데이터로 확정한다.
            if (entry.BakeYaw && entry.Yaw <= 1f)
            {
                entry.Yaw = GetAuthoredTurnYaw(entry.Slot);
                if (entry.Yaw > 1f)
                    entry.FallbackSource = "인플레이스 슬롯 저작 각도";
            }

            if (!entry.BakeReferenceSpeed || entry.ReferenceSpeed > 0.01f)
                return;

            ActorMovementController movement =
                _context.Subject.Root.GetComponentInChildren<
                    ActorMovementController>(true);
            if (movement == null)
                return;

            entry.ReferenceSpeed = entry.Slot == MotionTags.Walk
                ? movement.ReferenceWalkClipSpeed
                : entry.Slot == MotionTags.Run
                    ? movement.ReferenceRunClipSpeed
                    : entry.Slot == MotionTags.Sprint
                        ? movement.ReferenceSprintClipSpeed
                        : 0f;
            if (entry.ReferenceSpeed > 0.01f)
                entry.FallbackSource = "인플레이스 이동 프로파일";
        }

        private static float GetAuthoredTurnYaw(GameplayTag slot)
        {
            string tag = slot.TagName;
            if (tag.EndsWith(".L45", StringComparison.Ordinal)
                || tag.EndsWith(".R45", StringComparison.Ordinal))
                return 45f;
            if (tag.EndsWith(".L90", StringComparison.Ordinal)
                || tag.EndsWith(".R90", StringComparison.Ordinal))
                return 90f;
            return tag.EndsWith(".180", StringComparison.Ordinal)
                ? 180f
                : 0f;
        }

        private void Commit()
        {
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("로코모션 루트모션 일괄 베이크");
            HashSet<ActorAnimationMotionSet> changed = new();

            try
            {
                foreach (BakeEntry entry in _entries)
                {
                    if (changed.Add(entry.Owner))
                        Undo.RecordObject(
                            entry.Owner,
                            "로코모션 루트모션 일괄 베이크");

                    if (entry.BakeYaw)
                    {
                        entry.Owner.motionRootYaw ??=
                            new SerializedDictionary<GameplayTag, float>();
                        entry.Owner.motionRootYaw[entry.Slot] = entry.Yaw;
                    }

                    if (entry.BakeReferenceSpeed)
                    {
                        entry.Owner.motionReferenceSpeed ??=
                            new SerializedDictionary<GameplayTag, float>();
                        entry.Owner.motionReferenceSpeed[entry.Slot] =
                            entry.ReferenceSpeed;
                    }
                }

                foreach (ActorAnimationMotionSet target in changed)
                {
                    EditorUtility.SetDirty(target);
                    AssetDatabase.SaveAssetIfDirty(target);
                }

                Undo.CollapseUndoOperations(undoGroup);
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                _errors.Add(exception.Message);

                try
                {
                    foreach (ActorAnimationMotionSet target in changed)
                    {
                        if (target != null)
                            EditorUtility.SetDirty(target);
                    }
                    AssetDatabase.SaveAssets();
                }
                catch (Exception rollbackException)
                {
                    _errors.Add($"롤백 상태 저장 실패: {rollbackException.Message}");
                    Debug.LogException(rollbackException);
                }

                _summary = BuildFailureSummary("저장 실패 — 전체 롤백");
                try
                {
                    End(true);
                }
                finally
                {
                    FinishAutomation(false);
                }
                return;
            }

            _summary = BuildSuccessSummary();
            Debug.Log($"[LocomotionBake]\n{_summary}");
            try
            {
                End(true);
            }
            finally
            {
                FinishAutomation(true);
            }
        }

        private void Abort(string reason)
        {
            _errors.Add(reason);
            _summary = BuildFailureSummary("베이크 중단 — 에셋 변경 없음");
            Debug.LogError($"[LocomotionBake]\n{_summary}");
            try
            {
                End(true);
            }
            finally
            {
                FinishAutomation(false);
            }
        }

        private void End(bool restoreSlot)
        {
            EditorApplication.update -= Tick;
            Time.captureDeltaTime = _previousCaptureDelta;
            _active = false;
            _waitingForPlayback = false;
            _context?.Stop();
            if (restoreSlot && !string.IsNullOrEmpty(_originalSlotId))
                _context?.SelectSlot(_originalSlotId);
            if (_context?.Subject is IMotionPreviewRootMotion rootMotion)
                rootMotion.Teleport(_originPosition, _originRotation);
            _context?.Repaint();
        }

        private string BuildSuccessSummary()
        {
            var builder = new StringBuilder();
            builder.AppendLine(
                $"일괄 베이크 완료: 기준 속도 {ReferenceSpeedSlots.Length}개 / Turn yaw {TurnSlots.Length}개");
            foreach (BakeEntry entry in _entries)
            {
                builder.Append(entry.Slot.TagName);
                if (entry.BakeReferenceSpeed)
                    builder.Append($" speed={entry.ReferenceSpeed:0.###}m/s");
                if (entry.BakeYaw)
                    builder.Append($" yaw={entry.Yaw:0.###}°");
                if (!string.IsNullOrEmpty(entry.FallbackSource))
                    builder.Append($" ({entry.FallbackSource})");
                builder.AppendLine();
            }
            return builder.ToString();
        }

        private string BuildFailureSummary(string title)
        {
            var builder = new StringBuilder();
            builder.AppendLine(title);
            foreach (string error in _errors)
                builder.AppendLine($"- {error}");
            return builder.ToString();
        }

        private static bool TryFindOwner(
            ActorAnimationMotionSet root,
            GameplayTag slot,
            out ActorAnimationMotionSet owner)
        {
            HashSet<ActorAnimationMotionSet> visited = new();
            for (ActorAnimationMotionSet current = root;
                 current != null && visited.Add(current);
                 current = current.fallbackMotionSet)
            {
                if (current.motionSlots != null
                    && current.motionSlots.TryGetValue(
                        slot,
                        out MotionSetAsset asset)
                    && asset != null)
                {
                    owner = current;
                    return true;
                }
            }

            owner = null;
            return false;
        }

        private static void OnAutomationPlayModeStateChanged(
            PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode
                && EditorPrefs.GetBool(AutomationPendingKey, false))
            {
                _automationWaitStarted = EditorApplication.timeSinceStartup;
                EditorApplication.update -= TryBeginAutomation;
                EditorApplication.update += TryBeginAutomation;
                return;
            }

            if (state != PlayModeStateChange.EnteredEditMode
                || !EditorPrefs.GetBool(AutomationFinishingKey, false))
                return;

            bool succeeded =
                EditorPrefs.GetBool(AutomationSucceededKey, false);
            EditorPrefs.DeleteKey(AutomationFinishingKey);
            EditorPrefs.DeleteKey(AutomationSucceededKey);
            if (Application.isBatchMode)
                EditorApplication.Exit(succeeded ? 0 : 1);
        }

        private static void TryBeginAutomation()
        {
            if (!Application.isPlaying
                || !EditorPrefs.GetBool(AutomationPendingKey, false))
            {
                EditorApplication.update -= TryBeginAutomation;
                return;
            }

            MotionSetEditorWindow window =
                Resources.FindObjectsOfTypeAll<MotionSetEditorWindow>()
                    .FirstOrDefault(candidate =>
                        candidate.Subject is IMotionPreviewRootMotion
                        && ResolveRootTarget(candidate.Catalog) != null);
            if (window != null)
            {
                LocomotionRootMotionBakePanel panel =
                    MotionEditorExtensionRegistry.Panels
                        .OfType<LocomotionRootMotionBakePanel>()
                        .FirstOrDefault();
                if (panel != null && panel.BeginBatch(window))
                {
                    EditorApplication.update -= TryBeginAutomation;
                    return;
                }
            }

            if (EditorApplication.timeSinceStartup - _automationWaitStarted
                <= 30d)
                return;

            EditorApplication.update -= TryBeginAutomation;
            FailAutomation("30초 안에 플레이어 루트모션 프리뷰를 준비하지 못했습니다.");
        }

        private static void FinishAutomation(bool succeeded)
        {
            if (!EditorPrefs.GetBool(AutomationPendingKey, false))
                return;

            EditorPrefs.DeleteKey(AutomationPendingKey);
            EditorPrefs.SetBool(AutomationSucceededKey, succeeded);
            EditorPrefs.SetBool(AutomationFinishingKey, true);
            if (Application.isPlaying)
                EditorApplication.ExitPlaymode();
            else if (Application.isBatchMode)
                EditorApplication.Exit(succeeded ? 0 : 1);
        }

        private static void FailAutomation(string reason)
        {
            Debug.LogError($"[LocomotionBake] {reason}");
            if (!EditorPrefs.GetBool(AutomationPendingKey, false)
                && Application.isBatchMode)
            {
                EditorApplication.Exit(1);
                return;
            }
            FinishAutomation(false);
        }

        private static ActorAnimationMotionSet ResolveRootTarget(
            IMotionSetCatalog catalog)
        {
            if (catalog?.SourceAsset is ActorAnimationMotionSet actorSet)
                return actorSet;
            return (catalog as PlayerActorAnimationMotionSetCatalog)
                ?.ResolvedSource;
        }
    }
}
