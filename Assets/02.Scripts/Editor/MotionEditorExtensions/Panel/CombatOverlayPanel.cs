using System.Collections.Generic;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Tool.Editor.Combat;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// Ability 공격 데이터와 MotionSet 이벤트를 겹쳐 보는 프로젝트 전용 패널.
    /// </summary>
    public sealed class CombatOverlayPanel : IMotionEditorPanel
    {
        private static readonly Color Active = new(0.95f, 0.30f, 0.25f);
        private static readonly Color Cancel = new(0.35f, 0.60f, 0.95f);
        private static readonly Color Move = new(0.45f, 0.80f, 0.85f);
        private static readonly Color Combo = new(0.35f, 0.80f, 0.40f);
        private static readonly Color Frame = new(0.55f, 0.55f, 0.60f);

        private AbilitySetSO _abilitySet;
        private UnityEngine.Object _pairingContext;
        private bool _show = true;
        private int _attackIndex;
        private List<CombatTimelineUtility.ResolvedAttack> _resolved = new();
        private UnityEngine.Object _resolvedAsset;
        private AbilitySetSO _resolvedAbilitySet;
        private bool _resolvedShow;
        private bool _resolveDirty = true;
        private bool _tracksDirty = true;
        private int _resolvedSetSignature;
        private List<MotionSetDrawer.OverlayTrack> _cachedTracks;

        public string Title => "전투 오버레이";
        public int Order => 100;

        public bool IsAvailable(IMotionEditorContext context) =>
            context?.Asset != null;

        public void OnGUI(IMotionEditorContext context)
        {
            RestorePairing(context);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _show = EditorGUILayout.ToggleLeft(
                        "표시",
                        _show,
                        GUILayout.Width(48f));
                    AbilitySetSO next = (AbilitySetSO)EditorGUILayout.ObjectField(
                        _abilitySet,
                        typeof(AbilitySetSO),
                        false);
                    if (next != _abilitySet)
                    {
                        _abilitySet = next;
                        SavePairing(context);
                    }

                    if (GUILayout.Button("자동 연결", GUILayout.Width(72f)))
                        AutoConnect(context);
                }

                // ResolveAttacks는 AbilitySet 전체를 순회하며 뷰를 새로 만든다.
                // 재생 중에는 매 프레임 repaint가 들어오므로 입력이 바뀐 경우에만 돈다.
                if (Event.current.type == EventType.Layout)
                    ResolveIfChanged(context);
                if (_resolved.Count > 1)
                {
                    string[] labels = _resolved.ConvertAll(
                        attack => attack.SourceName).ToArray();
                    int nextIndex = EditorGUILayout.Popup(
                        "공격",
                        Mathf.Clamp(_attackIndex, 0, labels.Length - 1),
                        labels);
                    if (nextIndex != _attackIndex)
                    {
                        _attackIndex = nextIndex;
                        _tracksDirty = true;
                    }
                }

                CombatTimelineUtility.ResolvedAttack attack = CurrentAttack();
                if (_abilitySet == null)
                    EditorGUILayout.HelpBox("AbilitySet을 지정하거나 자동 연결하세요.", MessageType.Info);
                else if (attack == null)
                    EditorGUILayout.HelpBox("현재 MotionSet을 사용하는 공격 Ability가 없습니다.", MessageType.Warning);
                else
                    EditorGUILayout.LabelField(
                        $"{attack.SourceName} · Hit Phase {attack.HitPhases?.Count ?? 0}개",
                        EditorStyles.miniLabel);
            }

            // 트랙 재빌드는 변경이 있을 때만. 다만 탭 전환으로 오버레이가 비워졌을 수 있으므로
            // 캐시된 목록은 Layout마다 다시 밀어 넣는다(동일 데이터면 타임라인이 조기 반환한다).
            if (Event.current.type != EventType.Layout)
                return;

            if (_tracksDirty)
            {
                _tracksDirty = false;
                _cachedTracks = _show
                    ? BuildTracks(context.CurrentSet, CurrentAttack())
                    : null;
            }

            context.SetOverlayTracks("전투 데이터", _cachedTracks);
        }

        public void OnSceneGUI(IMotionEditorContext context)
        {
        }

        public void OnPlaybackStateChanged(
            IMotionEditorContext context,
            MotionPreviewPlaybackState state)
        {
            _resolveDirty = true;
        }

        private void RestorePairing(IMotionEditorContext context)
        {
            UnityEngine.Object pairing = PairingContext(context);
            if (ReferenceEquals(pairing, _pairingContext))
                return;
            _pairingContext = pairing;
            _abilitySet = CombatTimelineUtility.LoadAttackDataPairing(pairing);
        }

        private void SavePairing(IMotionEditorContext context)
        {
            UnityEngine.Object pairing = PairingContext(context);
            if (pairing != null)
                CombatTimelineUtility.SaveAttackDataPairing(pairing, _abilitySet);
        }

        private void AutoConnect(IMotionEditorContext context)
        {
            if (context.Catalog?.SourceAsset is ActorAnimationMotionSet actorSet)
            {
                AbilitySetSO found = CombatTimelineUtility.FindAbilitySetForMotionSet(
                    actorSet,
                    out ActorDefinitionSO owner,
                    out bool ambiguous,
                    out string candidates);
                if (found != null)
                {
                    _abilitySet = found;
                    SavePairing(context);
                    Debug.Log($"[MotionSetEditor] 전투 데이터 연결: {owner.name} → {found.name}");
                    return;
                }

                if (ambiguous)
                {
                    Debug.LogWarning(
                        $"[MotionSetEditor] AbilitySet 자동 연결 후보가 모호합니다.\n{candidates}");
                    return;
                }
            }

            _abilitySet = CombatTimelineUtility.LoadAttackDataPairing(
                PairingContext(context));
            if (_abilitySet == null)
                Debug.LogWarning("[MotionSetEditor] AbilitySet 자동 연결에 실패했습니다.");
        }

        /// <summary>
        /// AbilitySet / MotionSetAsset / 표시 토글이 바뀐 경우에만 다시 해석한다.
        /// </summary>
        private void ResolveIfChanged(IMotionEditorContext context)
        {
            UnityEngine.Object asset = context?.Asset;
            int setSignature = CalculateSetSignature(context?.CurrentSet);
            if (setSignature != _resolvedSetSignature)
            {
                _resolvedSetSignature = setSignature;
                _tracksDirty = true;
            }

            if (!_resolveDirty &&
                ReferenceEquals(asset, _resolvedAsset) &&
                ReferenceEquals(_abilitySet, _resolvedAbilitySet) &&
                _show == _resolvedShow)
                return;

            _resolvedAsset = asset;
            _resolvedAbilitySet = _abilitySet;
            _resolvedShow = _show;
            _resolveDirty = false;
            _tracksDirty = true;
            Resolve(context);
        }

        private void Resolve(IMotionEditorContext context)
        {
            _resolved = CombatTimelineUtility.ResolveAttacks(
                _abilitySet,
                context?.Asset);
            _attackIndex = Mathf.Clamp(
                _attackIndex,
                0,
                Mathf.Max(0, _resolved.Count - 1));
            _tracksDirty = true;
        }

        /// <summary>
        /// 오버레이 스팬은 MotionSet의 Collision/Cancel 이벤트 배치에서 파생되므로,
        /// 구조가 바뀌었는지만 싸게 감지해 재빌드 여부를 결정한다.
        /// </summary>
        private static int CalculateSetSignature(MotionSet set)
        {
            if (set == null)
                return 0;

            int hash = 17;
            hash = hash * 31 + set.TotalDuration.GetHashCode();
            hash = hash * 31 + (set.globalEvents?.Count ?? 0);
            if (set.motions == null)
                return hash;

            hash = hash * 31 + set.motions.Count;
            foreach (Motion motion in set.motions)
            {
                if (motion == null)
                    continue;
                hash = hash * 31 + (motion.events?.Count ?? 0);
                hash = hash * 31 + motion.Duration.GetHashCode();
                if (motion.events == null)
                    continue;
                foreach (MotionEventBase motionEvent in motion.events)
                {
                    if (motionEvent == null)
                        continue;
                    hash = hash * 31 + motionEvent.startTime.GetHashCode();
                    hash = hash * 31 + motionEvent.endTime.GetHashCode();
                }
            }

            return hash;
        }

        private CombatTimelineUtility.ResolvedAttack CurrentAttack()
        {
            return _resolved.Count == 0
                ? null
                : _resolved[Mathf.Clamp(_attackIndex, 0, _resolved.Count - 1)];
        }

        private static UnityEngine.Object PairingContext(
            IMotionEditorContext context)
        {
            return context?.Catalog?.SourceAsset != null
                ? context.Catalog.SourceAsset
                : context?.Asset;
        }

        private static List<MotionSetDrawer.OverlayTrack> BuildTracks(
            MotionSet set,
            CombatTimelineUtility.ResolvedAttack attack)
        {
            if (set == null || attack == null)
                return null;

            float total = set.TotalDuration;
            List<CombatTimelineUtility.TimedSpan> collisions =
                CombatTimelineUtility.CollectCollisionSpans(set);
            var tracks = new List<MotionSetDrawer.OverlayTrack>();

            var active = new MotionSetDrawer.OverlayTrack
            {
                label = "⚔ 판정 액티브",
                color = Active,
            };
            foreach (CombatTimelineUtility.TimedSpan span in collisions)
            {
                HitPhaseData phase = attack.GetHitPhase(span.PhaseIndex);
                active.spans.Add(new MotionSetDrawer.OverlaySpan
                {
                    start = span.Start,
                    end = span.End,
                    label = phase != null
                        ? $"P{span.PhaseIndex} {phase.damage:0.#}dmg/{phase.poiseDamage:0.#}p"
                        : $"P{span.PhaseIndex}",
                });
            }
            tracks.Add(active);

            PlayerInterruptAction nonMove =
                attack.InterruptActions & ~PlayerInterruptAction.Move;
            List<CombatTimelineUtility.CancelWindowSpanInfo> authored =
                CombatTimelineUtility.CollectCancelWindowSpans(set);
            if (nonMove != PlayerInterruptAction.None)
            {
                var cancel = new MotionSetDrawer.OverlayTrack
                {
                    label = authored.Count > 0 ? "✂ 캔슬 (저작)" : "✂ 캔슬 [자동]",
                    color = Cancel,
                };
                if (authored.Count > 0)
                {
                    foreach (CombatTimelineUtility.CancelWindowSpanInfo span in authored)
                    {
                        PlayerInterruptAction effective =
                            (span.Mask == PlayerInterruptAction.None
                                ? attack.InterruptActions
                                : attack.InterruptActions & span.Mask) &
                            ~PlayerInterruptAction.Move;
                        cancel.spans.Add(new MotionSetDrawer.OverlaySpan
                        {
                            start = span.Start,
                            end = span.End,
                            label = CombatTimelineUtility.FormatInterruptMask(effective),
                            dashed = effective == PlayerInterruptAction.None,
                        });
                    }
                }
                else
                {
                    foreach (CombatTimelineUtility.TimedSpan span in
                             CombatTimelineUtility.ComputeComplementSpans(
                                 collisions,
                                 total))
                    {
                        cancel.spans.Add(new MotionSetDrawer.OverlaySpan
                        {
                            start = span.Start,
                            end = span.End,
                            label = CombatTimelineUtility.FormatInterruptMask(nonMove),
                            dashed = true,
                        });
                    }
                }
                tracks.Add(cancel);
            }

            if ((attack.InterruptActions & PlayerInterruptAction.Move) != 0 &&
                collisions.Count > 0)
            {
                float lastEnd = 0f;
                foreach (CombatTimelineUtility.TimedSpan span in collisions)
                    lastEnd = Mathf.Max(lastEnd, span.End);
                var move = new MotionSetDrawer.OverlayTrack
                {
                    label = "✂ 이동 캔슬 (후딜)",
                    color = Move,
                };
                move.spans.Add(new MotionSetDrawer.OverlaySpan
                {
                    start = lastEnd,
                    end = total,
                    label = "이동",
                });
                tracks.Add(move);
            }

            List<CombatTimelineUtility.TimedSpan> combos =
                CombatTimelineUtility.CollectSpans<ComboWindowEvent>(set);
            if (combos.Count > 0)
            {
                var combo = new MotionSetDrawer.OverlayTrack
                {
                    label = "⛓ 콤보 윈도우",
                    color = Combo,
                };
                foreach (CombatTimelineUtility.TimedSpan span in combos)
                {
                    combo.spans.Add(new MotionSetDrawer.OverlaySpan
                    {
                        start = span.Start,
                        end = span.End,
                        label = "콤보",
                    });
                }
                tracks.Add(combo);
            }

            if (collisions.Count > 0)
            {
                CombatTimelineUtility.ComputeFrameMetrics(
                    collisions,
                    total,
                    out float startup,
                    out _,
                    out float recovery);
                var frame = new MotionSetDrawer.OverlayTrack
                {
                    label = "◔ 선딜·후딜",
                    color = Frame,
                };
                if (startup > 0.01f)
                    frame.spans.Add(new MotionSetDrawer.OverlaySpan
                    {
                        start = 0f,
                        end = startup,
                        label = $"선딜 {startup:0.00}s",
                    });
                if (recovery > 0.01f)
                    frame.spans.Add(new MotionSetDrawer.OverlaySpan
                    {
                        start = total - recovery,
                        end = total,
                        label = $"후딜 {recovery:0.00}s",
                    });
                tracks.Add(frame);
            }

            return tracks;
        }
    }
}
