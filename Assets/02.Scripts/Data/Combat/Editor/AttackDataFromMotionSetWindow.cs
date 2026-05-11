#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Data;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;

namespace UPlayGround.Editor
{
    /// <summary>
    /// ActorAnimationMotionSet의 공격 AnimKey와 Collision 이벤트를 기준으로 AttackDataSO 골격을 생성한다.
    /// </summary>
    public sealed class AttackDataFromMotionSetWindow : EditorWindow
    {
        private enum TargetKind { Player, Enemy }
        private enum ExistingPolicy { Skip, SyncPhaseCount, Replace }

        private ActorAnimationMotionSet _motionSet;
        private AttackDataSO _attackData;
        private TargetKind _targetKind = TargetKind.Player;
        private ExistingPolicy _existingPolicy = ExistingPolicy.SyncPhaseCount;
        private bool _includeFallback = true;
        private bool _requireCollisionEvent = true;
        private bool _normalizeCollisionPhaseIndex = true;
        private bool _applyBalancedDamage = true;
        private bool _overwriteExistingDamage = false;
        private float _playerBaseDamage = 10f;
        private float _enemyBaseDamage = 8f;
        private float _poiseDamageRatio = 3f;
        private float _motionDurationWeight = 0.15f;
        private float _comboStepWeight = 0.08f;
        private Vector2 _scroll;
        private List<ScanEntry> _scanEntries = new();
        private string _lastMessage = "";

        [MenuItem("UPlayGround/Gameplay/Combat/MotionSet 기반 공격 데이터 생성기")]
        public static void OpenFromMenu()
        {
            var window = GetWindow<AttackDataFromMotionSetWindow>("공격 데이터 생성기");
            window.minSize = new Vector2(560f, 480f);
            window.BindSelection();
            window.Show();
        }

        public static void Open(AttackDataSO attackData)
        {
            var window = GetWindow<AttackDataFromMotionSetWindow>("공격 데이터 생성기");
            window.minSize = new Vector2(560f, 480f);
            window._attackData = attackData;
            window._targetKind = attackData is EnemyAttackDataSO ? TargetKind.Enemy : TargetKind.Player;
            window.BindSelection(false);
            window.RefreshScan();
            window.Show();
        }

        public static void Open(ActorAnimationMotionSet motionSet)
        {
            var window = GetWindow<AttackDataFromMotionSetWindow>("공격 데이터 생성기");
            window.minSize = new Vector2(560f, 480f);
            window._motionSet = motionSet;
            window.BindSelection(false);
            window.RefreshScan();
            window.Show();
        }

        private void OnEnable()
        {
            BindSelection();
            RefreshScan();
        }

        private void OnSelectionChange()
        {
            BindSelection();
            RefreshScan();
            Repaint();
        }

        private void BindSelection(bool overwriteExisting = true)
        {
            if (Selection.activeObject is ActorAnimationMotionSet selectedMotionSet)
            {
                if (overwriteExisting || _motionSet == null)
                    _motionSet = selectedMotionSet;
            }
            else if (Selection.activeObject is AttackDataSO selectedAttackData)
            {
                if (overwriteExisting || _attackData == null)
                {
                    _attackData = selectedAttackData;
                    _targetKind = selectedAttackData is EnemyAttackDataSO ? TargetKind.Enemy : TargetKind.Player;
                }
            }
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawSourceFields();
            DrawOptions();
            DrawPreview();
            DrawActions();
        }

        private void DrawHeader()
        {
            Rect header = GUILayoutUtility.GetRect(0, 42f, GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(header, new Color(0.12f, 0.12f, 0.12f));
            EditorGUI.DrawRect(new Rect(header.x, header.yMax - 2f, header.width, 2f), new Color(0.45f, 0.75f, 1f));
            EditorGUI.LabelField(
                new Rect(header.x + 10f, header.y, header.width - 20f, header.height),
                "MotionSet 기반 공격 데이터 생성기",
                new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, normal = { textColor = Color.white } });
        }

        private void DrawSourceFields()
        {
            EditorGUILayout.Space(8);
            EditorGUI.BeginChangeCheck();
            _motionSet = (ActorAnimationMotionSet)EditorGUILayout.ObjectField(
                "Animation MotionSet", _motionSet, typeof(ActorAnimationMotionSet), false);
            _attackData = (AttackDataSO)EditorGUILayout.ObjectField(
                "AttackData 대상", _attackData, typeof(AttackDataSO), false);
            _targetKind = (TargetKind)EditorGUILayout.EnumPopup("생성 대상 타입", _targetKind);
            if (EditorGUI.EndChangeCheck())
            {
                if (_attackData is EnemyAttackDataSO) _targetKind = TargetKind.Enemy;
                else if (_attackData is PlayerAttackDataSO) _targetKind = TargetKind.Player;
                RefreshScan();
            }
        }

        private void DrawOptions()
        {
            EditorGUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _includeFallback = EditorGUILayout.ToggleLeft("Fallback MotionSet까지 스캔", _includeFallback);
                _requireCollisionEvent = EditorGUILayout.ToggleLeft("Collision 이벤트가 있는 공격만 생성", _requireCollisionEvent);
                _normalizeCollisionPhaseIndex = EditorGUILayout.ToggleLeft(
                    "Collision hitPhaseIndex를 시간순으로 0부터 재정렬", _normalizeCollisionPhaseIndex);
                _existingPolicy = (ExistingPolicy)EditorGUILayout.EnumPopup("기존 공격 처리", _existingPolicy);
            }

            EditorGUILayout.Space(4);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                _applyBalancedDamage = EditorGUILayout.ToggleLeft("밸런싱 대미지 자동 설정", _applyBalancedDamage);
                using (new EditorGUI.DisabledScope(!_applyBalancedDamage))
                {
                    _overwriteExistingDamage = EditorGUILayout.ToggleLeft("기존 Phase 대미지/Poise도 갱신", _overwriteExistingDamage);
                    _playerBaseDamage = EditorGUILayout.FloatField("플레이어 기준 대미지", Mathf.Max(0f, _playerBaseDamage));
                    _enemyBaseDamage = EditorGUILayout.FloatField("적 기준 대미지", Mathf.Max(0f, _enemyBaseDamage));
                    _poiseDamageRatio = EditorGUILayout.FloatField("Poise 배율", Mathf.Max(0f, _poiseDamageRatio));
                    _motionDurationWeight = EditorGUILayout.Slider("모션 길이 반영", _motionDurationWeight, 0f, 0.5f);
                    _comboStepWeight = EditorGUILayout.Slider("콤보 순번 반영", _comboStepWeight, 0f, 0.25f);
                }
            }
        }

        private void DrawPreview()
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.BeginHorizontal();
            PlayerAttackDataSODrawer.DrawSectionHeader($"스캔 결과 ({_scanEntries.Count}개)", new Color(0.45f, 0.75f, 1f));
            EditorGUILayout.EndHorizontal();

            if (_motionSet == null)
            {
                EditorGUILayout.HelpBox("ActorAnimationMotionSet을 지정하세요.", MessageType.Info);
                return;
            }

            if (_scanEntries.Count == 0)
            {
                EditorGUILayout.HelpBox("조건에 맞는 공격 AnimKey가 없습니다.", MessageType.Warning);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (ScanEntry entry in _scanEntries)
            {
                using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
                {
                    GUILayout.Label(entry.Key.ToString(), EditorStyles.boldLabel, GUILayout.Width(170f));
                    GUILayout.Label(entry.CategoryLabel, GUILayout.Width(90f));
                    GUILayout.Label($"Collision {entry.CollisionCount}", GUILayout.Width(90f));
                    GUILayout.Label($"Phase {entry.PhaseCount}", GUILayout.Width(70f));
                    GUILayout.Label($"DMG {CalculateTotalDamage(entry):F0}", GUILayout.Width(80f));
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.ObjectField(entry.Asset, typeof(MotionSetAsset), false, GUILayout.Width(180f));
                }
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawActions()
        {
            EditorGUILayout.Space(6);
            using (new EditorGUI.DisabledScope(_motionSet == null))
            {
                if (GUILayout.Button("다시 스캔", GUILayout.Height(26f)))
                    RefreshScan();
            }

            using (new EditorGUI.DisabledScope(_motionSet == null || _attackData == null || _scanEntries.Count == 0))
            {
                GUI.backgroundColor = new Color(0.35f, 0.65f, 1f);
                if (GUILayout.Button("AttackData 생성/동기화", GUILayout.Height(32f)))
                    Generate();
                GUI.backgroundColor = Color.white;
            }

            if (!string.IsNullOrEmpty(_lastMessage))
                EditorGUILayout.HelpBox(_lastMessage, MessageType.Info);
        }

        private void RefreshScan()
        {
            _scanEntries = _motionSet == null
                ? new List<ScanEntry>()
                : CollectScanEntries(_motionSet, _includeFallback, _requireCollisionEvent, _targetKind);
        }

        private void Generate()
        {
            if (_attackData == null || _motionSet == null) return;

            Undo.RecordObject(_attackData, "Generate AttackData From MotionSet");
            int created = 0;
            int updated = 0;

            if (_attackData is PlayerAttackDataSO player)
            {
                foreach (ScanEntry entry in _scanEntries)
                    ApplyPlayerEntry(player, entry, ref created, ref updated);
            }
            else if (_attackData is EnemyAttackDataSO enemy)
            {
                foreach (ScanEntry entry in _scanEntries)
                    ApplyEnemyEntry(enemy, entry, ref created, ref updated);
            }

            if (_normalizeCollisionPhaseIndex)
                NormalizeCollisionPhaseIndexes(_scanEntries);

            EditorUtility.SetDirty(_attackData);
            AssetDatabase.SaveAssets();
            _lastMessage = $"생성 {created}개, 갱신 {updated}개 완료.";
            RefreshScan();
        }

        private void ApplyPlayerEntry(PlayerAttackDataSO data, ScanEntry entry, ref int created, ref int updated)
        {
            if (entry.Category == AttackCategory.Charge)
            {
                if (_existingPolicy != ExistingPolicy.Skip || data.chargeAnimKey == AnimKey.None)
                {
                    data.chargeAnimKey = entry.Key;
                    SyncChargeStageHitPhases(data, entry, _applyBalancedDamage,
                        _existingPolicy == ExistingPolicy.Replace || _overwriteExistingDamage);
                    updated++;
                }
                return;
            }

            if (TryApplySpecialPlayerEntry(data, entry, ref created, ref updated))
                return;

            List<PlayerAttackInfo> list = GetPlayerList(data, entry.Category);
            if (list == null) return;

            PlayerAttackInfo existing = list.FirstOrDefault(x => x?.baseInfo?.animKey == entry.Key);
            if (existing != null)
            {
                if (_existingPolicy == ExistingPolicy.Skip) return;
                if (_existingPolicy == ExistingPolicy.Replace)
                    ReplacePlayerAttack(existing, entry);
                else
                    SyncHitPhases(existing.baseInfo, entry.PhaseCount);
                ApplyBalancedDamage(existing.baseInfo, entry,
                    _existingPolicy == ExistingPolicy.Replace || _overwriteExistingDamage);
                updated++;
                return;
            }

            list.Add(CreatePlayerAttack(entry));
            created++;
        }

        private bool TryApplySpecialPlayerEntry(PlayerAttackDataSO data, ScanEntry entry, ref int created, ref int updated)
        {
            if (entry.Category != AttackCategory.Counter &&
                entry.Category != AttackCategory.Entry &&
                entry.Category != AttackCategory.SwapSpecial)
                return false;

            PlayerAttackInfo target = entry.Category switch
            {
                AttackCategory.Counter => data.counterAttack,
                AttackCategory.Entry => data.entryAttack,
                AttackCategory.SwapSpecial => data.swapSpecialAttack,
                _ => null,
            };

            bool isEmpty = target?.baseInfo == null || target.baseInfo.animKey == AnimKey.None;
            if (!isEmpty && _existingPolicy == ExistingPolicy.Skip) return true;

            PlayerAttackInfo value = target ?? new PlayerAttackInfo();
            if (_existingPolicy == ExistingPolicy.Replace || isEmpty)
                ReplacePlayerAttack(value, entry);
            else
                SyncHitPhases(value.baseInfo, entry.PhaseCount);
            ApplyBalancedDamage(value.baseInfo, entry, isEmpty || _existingPolicy == ExistingPolicy.Replace || _overwriteExistingDamage);

            if (entry.Category == AttackCategory.Counter)
                data.counterAttack = value;
            else if (entry.Category == AttackCategory.Entry)
                data.entryAttack = value;
            else
                data.swapSpecialAttack = value;

            if (isEmpty) created++;
            else updated++;
            return true;
        }

        private void ApplyEnemyEntry(EnemyAttackDataSO data, ScanEntry entry, ref int created, ref int updated)
        {
            data.skills ??= new List<EnemyAttackInfo>();
            EnemyAttackInfo existing = data.skills.FirstOrDefault(x => x?.baseInfo?.animKey == entry.Key);
            if (existing != null)
            {
                if (_existingPolicy == ExistingPolicy.Skip) return;
                if (_existingPolicy == ExistingPolicy.Replace)
                    ReplaceEnemyAttack(existing, entry);
                else
                    SyncHitPhases(existing.baseInfo, entry.PhaseCount);
                ApplyBalancedDamage(existing.baseInfo, entry,
                    _existingPolicy == ExistingPolicy.Replace || _overwriteExistingDamage);
                updated++;
                return;
            }

            data.skills.Add(CreateEnemyAttack(entry));
            created++;
        }

        private void SyncChargeStageHitPhases(PlayerAttackDataSO data, ScanEntry entry, bool applyDamage, bool overwriteDamage)
        {
            data.chargeStages ??= new List<ChargeStageData>();

            if (data.chargeStages.Count == 0)
                data.chargeStages.Add(new ChargeStageData());

            for (int i = 0; i < data.chargeStages.Count; i++)
            {
                ChargeStageData stage = data.chargeStages[i];
                SyncHitPhases(stage, entry.PhaseCount);
                if (applyDamage)
                    ApplyBalancedDamage(stage, entry, overwriteDamage, i, data.chargeStages.Count);
            }
        }

        private static void SyncHitPhases(ChargeStageData stage, int phaseCount)
        {
            if (stage == null) return;
            stage.hitPhases ??= new List<HitPhaseData>();
            phaseCount = Mathf.Max(1, phaseCount);

            while (stage.hitPhases.Count < phaseCount)
                stage.hitPhases.Add(CloneOrDefault(stage.hitPhases.LastOrDefault()));
            while (stage.hitPhases.Count > phaseCount)
                stage.hitPhases.RemoveAt(stage.hitPhases.Count - 1);
        }

        private PlayerAttackInfo CreatePlayerAttack(ScanEntry entry)
        {
            var attack = new PlayerAttackInfo();
            ReplacePlayerAttack(attack, entry);
            return attack;
        }

        private EnemyAttackInfo CreateEnemyAttack(ScanEntry entry)
        {
            var attack = new EnemyAttackInfo();
            ReplaceEnemyAttack(attack, entry);
            return attack;
        }

        private void ReplacePlayerAttack(PlayerAttackInfo attack, ScanEntry entry)
        {
            attack.baseInfo = CreateBaseInfo(entry);
            attack.canBeInterrupted = entry.Category is AttackCategory.Light or AttackCategory.Skill;
            attack.hitAngle = entry.Category is AttackCategory.Jump ? 90f : 60f;
        }

        private void ReplaceEnemyAttack(EnemyAttackInfo attack, ScanEntry entry)
        {
            attack.baseInfo = CreateBaseInfo(entry);
            attack.selectionWeight = 10f;
            attack.minRange = 0f;
            attack.maxRange = 2.5f;
            attack.cooldown = 2f;
            attack.skillType = SkillType.Attack;
            attack.isAerialSkill = entry.Key == AnimKey.Fly_Attack;
        }

        private AttackInfoBase CreateBaseInfo(ScanEntry entry)
        {
            var baseInfo = new AttackInfoBase
            {
                animKey = entry.Key,
                attackType = AttackType.Melee,
                hitPhases = new List<HitPhaseData>()
            };
            SyncHitPhases(baseInfo, entry.PhaseCount);
            ApplyBalancedDamage(baseInfo, entry, true);
            return baseInfo;
        }

        private static void SyncHitPhases(AttackInfoBase baseInfo, int phaseCount)
        {
            if (baseInfo == null) return;
            baseInfo.hitPhases ??= new List<HitPhaseData>();
            phaseCount = Mathf.Max(1, phaseCount);

            while (baseInfo.hitPhases.Count < phaseCount)
                baseInfo.hitPhases.Add(CloneOrDefault(baseInfo.hitPhases.LastOrDefault()));
            while (baseInfo.hitPhases.Count > phaseCount)
                baseInfo.hitPhases.RemoveAt(baseInfo.hitPhases.Count - 1);
        }

        private static HitPhaseData CloneOrDefault(HitPhaseData source)
        {
            if (source == null) return new HitPhaseData();
            return new HitPhaseData
            {
                damage = source.damage,
                poiseDamage = source.poiseDamage,
                reactionType = source.reactionType,
                attackOffset = source.attackOffset,
                attackRadius = source.attackRadius,
                hitHeightRange = source.hitHeightRange,
                hitParticleName = source.hitParticleName,
                pullForce = source.pullForce,
                airborneForce = source.airborneForce,
                knockBackForce = source.knockBackForce,
                knockBackDrag = source.knockBackDrag,
                grabDuration = source.grabDuration,
                victimForcedAnimKey = source.victimForcedAnimKey,
            };
        }

        private void ApplyBalancedDamage(AttackInfoBase baseInfo, ScanEntry entry, bool overwriteDamage)
        {
            if (!_applyBalancedDamage || baseInfo?.hitPhases == null) return;
            ApplyBalancedDamage(baseInfo.hitPhases, entry, overwriteDamage);
        }

        private void ApplyBalancedDamage(ChargeStageData stage, ScanEntry entry, bool overwriteDamage, int stageIndex, int stageCount)
        {
            if (!_applyBalancedDamage || stage?.hitPhases == null) return;
            float stageMultiplier = stageCount <= 1 ? 1f : Mathf.Lerp(1f, 2.25f, (float)stageIndex / (stageCount - 1));
            ApplyBalancedDamage(stage.hitPhases, entry, overwriteDamage, stageMultiplier);
        }

        private void ApplyBalancedDamage(List<HitPhaseData> phases, ScanEntry entry, bool overwriteDamage, float extraMultiplier = 1f)
        {
            if (phases == null || phases.Count == 0) return;
            float totalDamage = CalculateTotalDamage(entry) * Mathf.Max(0f, extraMultiplier);
            float totalWeight = 0f;
            float[] weights = new float[phases.Count];

            for (int i = 0; i < phases.Count; i++)
            {
                weights[i] = Mathf.Lerp(1f, 1.25f, phases.Count <= 1 ? 0f : (float)i / (phases.Count - 1));
                totalWeight += weights[i];
            }

            for (int i = 0; i < phases.Count; i++)
            {
                HitPhaseData phase = phases[i];
                if (phase == null) continue;
                if (!overwriteDamage && phase.damage != 0f && !Mathf.Approximately(phase.damage, 10f)) continue;

                float damage = totalWeight > 0f ? totalDamage * weights[i] / totalWeight : totalDamage;
                phase.damage = Mathf.Round(damage);
                phase.poiseDamage = Mathf.Round(phase.damage * _poiseDamageRatio);
            }
        }

        private float CalculateTotalDamage(ScanEntry entry)
        {
            if (!_applyBalancedDamage || entry == null) return 0f;

            float baseDamage = _targetKind == TargetKind.Enemy ? _enemyBaseDamage : _playerBaseDamage;
            float categoryMultiplier = GetCategoryDamageMultiplier(entry.Category);
            float comboMultiplier = 1f + GetComboStep(entry.Key, entry.Category) * _comboStepWeight;
            float durationMultiplier = 1f + Mathf.Max(0f, entry.Duration - 1f) * _motionDurationWeight;
            float multiHitCompensation = 1f + Mathf.Max(0, entry.PhaseCount - 1) * 0.18f;

            return Mathf.Max(1f, baseDamage * categoryMultiplier * comboMultiplier * durationMultiplier * multiHitCompensation);
        }

        private static float GetCategoryDamageMultiplier(AttackCategory category)
        {
            return category switch
            {
                AttackCategory.Light => 1.00f,
                AttackCategory.Heavy => 1.55f,
                AttackCategory.Dash => 1.25f,
                AttackCategory.Jump => 1.20f,
                AttackCategory.Skill => 2.10f,
                AttackCategory.Counter => 1.75f,
                AttackCategory.Entry => 1.15f,
                AttackCategory.SwapSpecial => 2.40f,
                AttackCategory.Charge => 1.35f,
                _ => 1.00f,
            };
        }

        private static int GetComboStep(AnimKey key, AttackCategory category)
        {
            int value = (int)key;
            return category switch
            {
                AttackCategory.Light => Mathf.Max(0, value - (int)AnimKey.Attack_1),
                AttackCategory.Heavy => Mathf.Max(0, value - (int)AnimKey.HeavyAttack_1),
                AttackCategory.Dash => key == AnimKey.JumpDashAttack_1 ? 1 : Mathf.Max(0, value - (int)AnimKey.DashAttack_1),
                AttackCategory.Jump => Mathf.Max(0, value - (int)AnimKey.JumpAttack_1),
                AttackCategory.Skill => Mathf.Max(0, value - (int)AnimKey.Skill_1),
                AttackCategory.Counter => Mathf.Max(0, value - (int)AnimKey.Counter_Attack_1),
                AttackCategory.Entry => Mathf.Max(0, value - (int)AnimKey.Player_SwapAttack_1),
                AttackCategory.Charge => Mathf.Max(0, value - (int)AnimKey.ChargeAttack_1),
                _ => 0,
            };
        }

        private static List<PlayerAttackInfo> GetPlayerList(PlayerAttackDataSO data, AttackCategory category)
        {
            return category switch
            {
                AttackCategory.Light => data.liteComboAttackList,
                AttackCategory.Heavy => data.heavyComboAttackList,
                AttackCategory.Jump => data.jumpAttackList,
                AttackCategory.Dash => data.dashAttackList,
                AttackCategory.Skill => data.skillAttackList,
                _ => null,
            };
        }

        private static List<ScanEntry> CollectScanEntries(
            ActorAnimationMotionSet root,
            bool includeFallback,
            bool requireCollision,
            TargetKind targetKind)
        {
            var result = new List<ScanEntry>();
            var seen = new HashSet<AnimKey>();

            foreach (ActorAnimationMotionSet set in EnumerateMotionSets(root, includeFallback))
            {
                if (set?.motionSets == null) continue;

                foreach (KeyValuePair<AnimKey, MotionSetAsset> pair in set.motionSets)
                {
                    AnimKey key = pair.Key;
                    if (!seen.Add(key)) continue;
                    if (!TryGetAttackCategory(key, targetKind, out AttackCategory category)) continue;

                    MotionSetAsset asset = pair.Value;
                    if (asset == null || asset.motionSet == null) continue;

                    List<BeginCollisionEvent> collisions = CollectCollisionEvents(asset.motionSet);
                    if (requireCollision && collisions.Count == 0) continue;

                    int phaseCount = CalculatePhaseCount(collisions);
                    result.Add(new ScanEntry(key, category, asset, collisions, phaseCount, asset.motionSet.TotalDuration));
                }
            }

            return result.OrderBy(x => (int)x.Key).ToList();
        }

        private static IEnumerable<ActorAnimationMotionSet> EnumerateMotionSets(ActorAnimationMotionSet root, bool includeFallback)
        {
            var visited = new HashSet<ActorAnimationMotionSet>();
            ActorAnimationMotionSet current = root;
            int depth = 0;
            while (current != null && visited.Add(current) && depth++ < 8)
            {
                yield return current;
                if (!includeFallback) yield break;
                current = current.fallbackMotionSet;
            }
        }

        private static List<BeginCollisionEvent> CollectCollisionEvents(MotionSet motionSet)
        {
            var result = new List<(float Time, BeginCollisionEvent Event)>();

            if (motionSet.globalEvents != null)
            {
                foreach (MotionEventBase evt in motionSet.globalEvents)
                    if (evt is BeginCollisionEvent collision)
                        result.Add((collision.startTime, collision));
            }

            float offset = 0f;
            if (motionSet.motions != null)
            {
                foreach (UPlayGround.Animation.Motion motion in motionSet.motions)
                {
                    if (motion?.events != null)
                    {
                        foreach (MotionEventBase evt in motion.events)
                            if (evt is BeginCollisionEvent collision)
                                result.Add((offset + collision.startTime, collision));
                    }
                    offset += motion?.Duration ?? 0f;
                }
            }

            return result.OrderBy(x => x.Time).Select(x => x.Event).ToList();
        }

        private static int CalculatePhaseCount(List<BeginCollisionEvent> collisions)
        {
            if (collisions == null || collisions.Count == 0) return 1;
            int maxIndex = collisions.Max(x => Mathf.Max(0, x.hitPhaseIndex));
            return Mathf.Max(collisions.Count, maxIndex + 1);
        }

        private static void NormalizeCollisionPhaseIndexes(List<ScanEntry> entries)
        {
            var dirtyAssets = new HashSet<MotionSetAsset>();
            foreach (ScanEntry entry in entries)
            {
                for (int i = 0; i < entry.Collisions.Count; i++)
                {
                    if (entry.Collisions[i].hitPhaseIndex == i) continue;
                    entry.Collisions[i].hitPhaseIndex = i;
                    dirtyAssets.Add(entry.Asset);
                }
            }

            foreach (MotionSetAsset asset in dirtyAssets)
                EditorUtility.SetDirty(asset);
        }

        private static bool TryGetAttackCategory(AnimKey key, TargetKind targetKind, out AttackCategory category)
        {
            int value = (int)key;
            category = AttackCategory.Unknown;

            if (key == AnimKey.Fly_Attack)
            {
                category = AttackCategory.Skill;
                return targetKind == TargetKind.Enemy;
            }

            if (value >= (int)AnimKey.Attack_1 && value <= (int)AnimKey.Attack_10)
                category = AttackCategory.Light;
            else if (value >= (int)AnimKey.HeavyAttack_1 && value <= (int)AnimKey.HeavyAttack_10)
                category = AttackCategory.Heavy;
            else if (value >= (int)AnimKey.DashAttack_1 && value <= (int)AnimKey.DashAttack_5)
                category = AttackCategory.Dash;
            else if (key == AnimKey.JumpDashAttack_1)
                category = AttackCategory.Dash;
            else if (value >= (int)AnimKey.JumpAttack_1 && value <= (int)AnimKey.JumpAttack_7)
                category = AttackCategory.Jump;
            else if (value >= (int)AnimKey.Skill_1 && value <= (int)AnimKey.Skill_9)
                category = AttackCategory.Skill;
            else if (value >= (int)AnimKey.Counter_Attack_1 && value <= (int)AnimKey.Counter_Attack_2)
                category = AttackCategory.Counter;
            else if (value >= (int)AnimKey.Player_SwapAttack_1 && value <= (int)AnimKey.Player_SwapAttack_5)
                category = AttackCategory.Entry;
            else if (key == AnimKey.Player_SwapSpecialAttack_1)
                category = AttackCategory.SwapSpecial;
            else if (value >= (int)AnimKey.ChargeAttack_1 && value <= (int)AnimKey.ChargeAttack_5)
                category = AttackCategory.Charge;

            if (category == AttackCategory.Unknown) return false;
            return targetKind == TargetKind.Player || category is not (AttackCategory.Entry or AttackCategory.SwapSpecial or AttackCategory.Charge);
        }

        private enum AttackCategory
        {
            Unknown,
            Light,
            Heavy,
            Dash,
            Jump,
            Skill,
            Counter,
            Entry,
            SwapSpecial,
            Charge,
        }

        private sealed class ScanEntry
        {
            public readonly AnimKey Key;
            public readonly AttackCategory Category;
            public readonly MotionSetAsset Asset;
            public readonly List<BeginCollisionEvent> Collisions;
            public readonly int PhaseCount;
            public readonly float Duration;

            public ScanEntry(AnimKey key, AttackCategory category, MotionSetAsset asset,
                List<BeginCollisionEvent> collisions, int phaseCount, float duration)
            {
                Key = key;
                Category = category;
                Asset = asset;
                Collisions = collisions ?? new List<BeginCollisionEvent>();
                PhaseCount = Mathf.Max(1, phaseCount);
                Duration = Mathf.Max(0f, duration);
            }

            public int CollisionCount => Collisions.Count;
            public string CategoryLabel => Category switch
            {
                AttackCategory.Light => "약공격",
                AttackCategory.Heavy => "강공격",
                AttackCategory.Dash => "대쉬",
                AttackCategory.Jump => "점프",
                AttackCategory.Skill => "스킬",
                AttackCategory.Counter => "카운터",
                AttackCategory.Entry => "등장",
                AttackCategory.SwapSpecial => "특수",
                AttackCategory.Charge => "차지",
                _ => "기타",
            };
        }
    }
}
#endif
