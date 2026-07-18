#if UNITY_EDITOR
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
    /// MotionSet의 공격 키와 Collision 이벤트를 EnemyAttackDataSO 골격으로 동기화한다.
    /// 플레이어 공격 저작은 Ability Editor가 담당한다.
    /// </summary>
    public sealed class AttackDataFromMotionSetWindow : EditorWindow
    {
        private enum ExistingPolicy
        {
            Skip,
            SyncPhaseCount,
            Replace,
        }

        private ActorAnimationMotionSet _motionSet;
        private EnemyAttackDataSO _attackData;
        private ExistingPolicy _existingPolicy = ExistingPolicy.SyncPhaseCount;
        private bool _includeFallback = true;
        private bool _requireCollisionEvent = true;
        private bool _normalizeCollisionPhaseIndex = true;
        private Vector2 _scroll;
        private List<ScanEntry> _entries = new();
        private string _message;

        [MenuItem(
            "UPlayGround/게임플레이/전투/MotionSet 기반 적 공격 데이터 생성기",
            priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.GameplayCombat + 1)]
        public static void OpenFromMenu()
        {
            var window = GetWindow<AttackDataFromMotionSetWindow>("적 공격 생성기");
            window.minSize = new Vector2(560f, 420f);
            window.BindSelection();
            window.Show();
        }

        public static void Open(EnemyAttackDataSO attackData)
        {
            var window = GetWindow<AttackDataFromMotionSetWindow>("적 공격 생성기");
            window.minSize = new Vector2(560f, 420f);
            window._attackData = attackData;
            window.BindSelection(false);
            window.RefreshScan();
            window.Show();
        }

        public static void Open(ActorAnimationMotionSet motionSet)
        {
            var window = GetWindow<AttackDataFromMotionSetWindow>("적 공격 생성기");
            window.minSize = new Vector2(560f, 420f);
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

        private void BindSelection(bool overwrite = true)
        {
            if (Selection.activeObject is ActorAnimationMotionSet motionSet
                && (overwrite || _motionSet == null))
            {
                _motionSet = motionSet;
            }
            else if (Selection.activeObject is EnemyAttackDataSO attackData
                     && (overwrite || _attackData == null))
            {
                _attackData = attackData;
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("MotionSet 기반 적 공격 데이터 생성기", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "플레이어 공격은 Ability Editor에서 저작합니다. 이 도구는 EnemyAttackDataSO만 생성·동기화합니다.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            _motionSet = (ActorAnimationMotionSet)EditorGUILayout.ObjectField(
                "Animation MotionSet",
                _motionSet,
                typeof(ActorAnimationMotionSet),
                false);
            _attackData = (EnemyAttackDataSO)EditorGUILayout.ObjectField(
                "Enemy Attack Data",
                _attackData,
                typeof(EnemyAttackDataSO),
                false);
            _includeFallback = EditorGUILayout.ToggleLeft("Fallback MotionSet까지 스캔", _includeFallback);
            _requireCollisionEvent = EditorGUILayout.ToggleLeft(
                "Collision 이벤트가 있는 공격만 포함",
                _requireCollisionEvent);
            _normalizeCollisionPhaseIndex = EditorGUILayout.ToggleLeft(
                "Collision hitPhaseIndex를 시간순으로 정규화",
                _normalizeCollisionPhaseIndex);
            _existingPolicy = (ExistingPolicy)EditorGUILayout.EnumPopup(
                "기존 공격 처리",
                _existingPolicy);
            if (EditorGUI.EndChangeCheck())
                RefreshScan();

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField($"스캔 결과 ({_entries.Count}개)", EditorStyles.boldLabel);
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (ScanEntry entry in _entries)
            {
                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                EditorGUILayout.LabelField(entry.Key.ToString(), GUILayout.Width(190f));
                EditorGUILayout.LabelField($"Phase {entry.PhaseCount}", GUILayout.Width(75f));
                EditorGUILayout.ObjectField(entry.Asset, typeof(MotionSetAsset), false);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            if (!string.IsNullOrWhiteSpace(_message))
                EditorGUILayout.HelpBox(_message, MessageType.None);

            using (new EditorGUI.DisabledScope(_motionSet == null || _attackData == null))
            {
                if (GUILayout.Button("EnemyAttackDataSO에 적용", GUILayout.Height(30f)))
                    Apply();
            }
        }

        private void RefreshScan()
        {
            _entries = Scan(_motionSet, _includeFallback, _requireCollisionEvent);
            _message = "";
        }

        private void Apply()
        {
            if (_motionSet == null || _attackData == null)
                return;

            Undo.RecordObject(_attackData, "적 공격 데이터 MotionSet 동기화");
            if (_normalizeCollisionPhaseIndex)
                NormalizeCollisionPhaseIndexes(_entries);

            _attackData.skills ??= new List<EnemyAttackInfo>();
            int created = 0;
            int updated = 0;
            foreach (ScanEntry entry in _entries)
            {
                EnemyAttackInfo existing = _attackData.skills.FirstOrDefault(
                    skill => skill?.baseInfo?.animKey == entry.Key);
                if (existing == null)
                {
                    _attackData.skills.Add(CreateAttack(entry));
                    created++;
                    continue;
                }

                if (_existingPolicy == ExistingPolicy.Skip)
                    continue;

                if (_existingPolicy == ExistingPolicy.Replace || existing.baseInfo == null)
                    existing.baseInfo = CreateBaseInfo(entry);
                else
                    SyncHitPhases(existing.baseInfo, entry);
                updated++;
            }

            EditorUtility.SetDirty(_attackData);
            AssetDatabase.SaveAssets();
            _message = $"생성 {created}개, 갱신 {updated}개 완료.";
        }

        private static EnemyAttackInfo CreateAttack(ScanEntry entry)
        {
            return new EnemyAttackInfo
            {
                baseInfo = CreateBaseInfo(entry),
                selectionWeight = 10f,
                minRange = 0f,
                maxRange = 2.5f,
                cooldown = 2f,
            };
        }

        private static AttackInfoBase CreateBaseInfo(ScanEntry entry)
        {
            var result = new AttackInfoBase
            {
                animKey = entry.Key,
                attackType = AttackType.Melee,
                hitPhases = new List<HitPhaseData>(),
            };
            SyncHitPhases(result, entry);
            return result;
        }

        private static void SyncHitPhases(AttackInfoBase baseInfo, ScanEntry entry)
        {
            baseInfo.hitPhases ??= new List<HitPhaseData>();
            while (baseInfo.hitPhases.Count < entry.PhaseCount)
                baseInfo.hitPhases.Add(new HitPhaseData());
            while (baseInfo.hitPhases.Count > entry.PhaseCount)
                baseInfo.hitPhases.RemoveAt(baseInfo.hitPhases.Count - 1);

            for (int i = 0; i < entry.Collisions.Count; i++)
            {
                BeginCollisionEvent collision = entry.Collisions[i];
                int phaseIndex = Mathf.Clamp(collision.hitPhaseIndex, 0, baseInfo.hitPhases.Count - 1);
                if (!string.IsNullOrWhiteSpace(collision.hitboxGroupId))
                    baseInfo.hitPhases[phaseIndex].hitboxGroupId = collision.hitboxGroupId;
            }
        }

        private static List<ScanEntry> Scan(
            ActorAnimationMotionSet root,
            bool includeFallback,
            bool requireCollision)
        {
            var result = new List<ScanEntry>();
            var seen = new HashSet<AnimKey>();
            foreach (ActorAnimationMotionSet set in EnumerateMotionSets(root, includeFallback))
            {
                if (set?.motionSets == null)
                    continue;

                foreach (KeyValuePair<AnimKey, MotionSetAsset> pair in set.motionSets)
                {
                    if (!seen.Add(pair.Key) || !IsEnemyAttackKey(pair.Key))
                        continue;
                    MotionSetAsset asset = pair.Value;
                    if (asset?.motionSet == null)
                        continue;

                    List<BeginCollisionEvent> collisions = CollectCollisionEvents(asset.motionSet);
                    if (requireCollision && collisions.Count == 0)
                        continue;

                    int phaseCount = collisions.Count == 0
                        ? 1
                        : Mathf.Max(collisions.Count, collisions.Max(x => Mathf.Max(0, x.hitPhaseIndex)) + 1);
                    result.Add(new ScanEntry(pair.Key, asset, collisions, phaseCount));
                }
            }
            return result.OrderBy(entry => (int)entry.Key).ToList();
        }

        private static IEnumerable<ActorAnimationMotionSet> EnumerateMotionSets(
            ActorAnimationMotionSet root,
            bool includeFallback)
        {
            var visited = new HashSet<ActorAnimationMotionSet>();
            ActorAnimationMotionSet current = root;
            int depth = 0;
            while (current != null && visited.Add(current) && depth++ < 8)
            {
                yield return current;
                if (!includeFallback)
                    yield break;
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
            return result.OrderBy(item => item.Time).Select(item => item.Event).ToList();
        }

        private static void NormalizeCollisionPhaseIndexes(List<ScanEntry> entries)
        {
            var dirtyAssets = new HashSet<MotionSetAsset>();
            foreach (ScanEntry entry in entries)
            {
                for (int i = 0; i < entry.Collisions.Count; i++)
                {
                    if (entry.Collisions[i].hitPhaseIndex == i)
                        continue;
                    entry.Collisions[i].hitPhaseIndex = i;
                    dirtyAssets.Add(entry.Asset);
                }
            }
            foreach (MotionSetAsset asset in dirtyAssets)
                EditorUtility.SetDirty(asset);
        }

        private static bool IsEnemyAttackKey(AnimKey key)
        {
            int value = (int)key;
            return key == AnimKey.Fly_Attack
                   || value >= (int)AnimKey.Attack_1 && value <= (int)AnimKey.Attack_10
                   || value >= (int)AnimKey.HeavyAttack_1 && value <= (int)AnimKey.HeavyAttack_10
                   || value >= (int)AnimKey.DashAttack_1 && value <= (int)AnimKey.DashAttack_5
                   || value >= (int)AnimKey.JumpAttack_1 && value <= (int)AnimKey.JumpAttack_7
                   || value >= (int)AnimKey.Skill_1 && value <= (int)AnimKey.Skill_9
                   || value >= (int)AnimKey.Counter_Attack_1 && value <= (int)AnimKey.Counter_Attack_2;
        }

        private sealed class ScanEntry
        {
            public readonly AnimKey Key;
            public readonly MotionSetAsset Asset;
            public readonly List<BeginCollisionEvent> Collisions;
            public readonly int PhaseCount;

            public ScanEntry(
                AnimKey key,
                MotionSetAsset asset,
                List<BeginCollisionEvent> collisions,
                int phaseCount)
            {
                Key = key;
                Asset = asset;
                Collisions = collisions;
                PhaseCount = Mathf.Max(1, phaseCount);
            }
        }
    }
}
#endif
