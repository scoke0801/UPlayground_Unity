#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;
using UPlayGround.Gameplay.Ability;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// 밸런스 핵심 수치(스탯/브레이크/공격 데이터)를 JSON 베이스라인으로 저장하고,
    /// 현재 에셋 상태와 비교해 의도치 않은 변경(생성기 clobber 등)을 잡아내는 스냅샷 서비스.
    /// 저장 위치: 프로젝트 루트의 BalanceSnapshots/ (Assets 외부 — 임포트 대상 아님).
    /// </summary>
    public static class BalanceSnapshotService
    {
        public const string BaselineFileName = "baseline.json";

        public static string SnapshotDirectory
            => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "BalanceSnapshots"));

        public static string BaselinePath => Path.Combine(SnapshotDirectory, BaselineFileName);

        public static bool HasBaseline => File.Exists(BaselinePath);

        #region Snapshot Models

        [Serializable]
        public sealed class Snapshot
        {
            public string createdAt;
            public List<ActorSnapshot> actors = new();
            public List<PlayerAttackSnapshot> playerAttacks = new();
        }

        [Serializable]
        public sealed class ActorSnapshot
        {
            public string actorId;
            public string definitionPath;
            public int level;
            public string grade;
            public List<StatValue> stats = new();
            public bool hasBreakGauge;
            public float breakMaxGauge;
            public float breakResist;
            public float breakExposedDuration;
            public float breakDamageTakenMultiplier;
            public List<SkillSnapshot> skills = new();
        }

        [Serializable]
        public sealed class StatValue
        {
            public string attributeId;
            public float value;
        }

        [Serializable]
        public sealed class SkillSnapshot
        {
            public int index;
            public string motion;
            public float selectionWeight;
            public float cooldown;
            public float minRange;
            public float maxRange;
            public int requiredLevel;
            public float totalDamage;
            public float totalPoiseDamage;
            public float totalBreakDamage;
        }

        [Serializable]
        public sealed class PlayerAttackSnapshot
        {
            public string assetPath;
            public string assetName;
            public List<PlayerAttackEntry> attacks = new();
        }

        [Serializable]
        public sealed class PlayerAttackEntry
        {
            public string slot;
            public string motion;
            public float totalDamage;
            public float totalPoiseDamage;
            public float totalBreakDamage;
        }

        #endregion

        #region Diff Models

        public enum DiffKind
        {
            ValueChanged,
            Added,
            Removed,
        }

        public sealed class DiffEntry
        {
            public DiffKind Kind;
            public string Owner;     // actorId 또는 플레이어 공격 데이터 이름
            public string Field;     // 예: "stats.MaxHealth", "skills[2].cooldown"
            public float OldValue;
            public float NewValue;
            public string Detail;    // Added/Removed 설명

            /// <summary>상대 변화량(0~). Added/Removed는 1로 취급.</summary>
            public float RelativeChange
            {
                get
                {
                    if (Kind != DiffKind.ValueChanged)
                        return 1f;
                    float baseValue = Mathf.Max(Mathf.Abs(OldValue), 0.0001f);
                    return Mathf.Abs(NewValue - OldValue) / baseValue;
                }
            }

            public override string ToString()
            {
                return Kind switch
                {
                    DiffKind.ValueChanged => $"{Owner} | {Field}: {OldValue.ToString("0.###", CultureInfo.InvariantCulture)} → {NewValue.ToString("0.###", CultureInfo.InvariantCulture)} ({RelativeChange * 100f:F0}%)",
                    DiffKind.Added => $"{Owner} | {Field} 추가됨 {Detail}",
                    _ => $"{Owner} | {Field} 제거됨 {Detail}",
                };
            }
        }

        #endregion

        #region Capture

        public static Snapshot Capture()
        {
            var snapshot = new Snapshot
            {
                createdAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            };

            foreach (ActorDefinitionSO def in LoadAll<ActorDefinitionSO>())
                snapshot.actors.Add(CaptureActor(def));
            snapshot.actors.Sort((a, b) => string.CompareOrdinal(a.actorId, b.actorId));

            foreach (AbilitySetSO asset in LoadAll<AbilitySetSO>())
                if (asset.combatBindings != null && asset.combatBindings.Count > 0)
                    snapshot.playerAttacks.Add(CapturePlayerAttack(asset));
            snapshot.playerAttacks.Sort((a, b) => string.CompareOrdinal(a.assetName, b.assetName));

            return snapshot;
        }

        private static ActorSnapshot CaptureActor(ActorDefinitionSO def)
        {
            var actor = new ActorSnapshot
            {
                actorId = string.IsNullOrEmpty(def.actorId) ? def.name : def.actorId,
                definitionPath = AssetDatabase.GetAssetPath(def),
                level = def.level,
                grade = def.grade.ToString(),
            };

            if (def.attributeProfile != null)
            {
                foreach (AttributeProfileEntry entry in def.attributeProfile.Entries)
                {
                    if (entry == null) continue;
                    actor.stats.Add(new StatValue
                    {
                        attributeId = entry.AttributeId.Value,
                        value = entry.BaseValue,
                    });
                }
            }

            if (def.breakGaugeData != null && def.breakGaugeData.useBreakGauge)
            {
                actor.hasBreakGauge = true;
                actor.breakMaxGauge = def.breakGaugeData.maxGauge;
                actor.breakResist = def.breakGaugeData.breakResist;
                actor.breakExposedDuration = def.breakGaugeData.exposedDuration;
                actor.breakDamageTakenMultiplier = def.breakGaugeData.damageTakenMultiplierWhileExposed;
            }

            List<AbilityAttackEditorUtility.Entry> entries =
                AbilityAttackEditorUtility.Collect(
                    def.EffectiveAbilitySet,
                    true);
            for (int i = 0; i < entries.Count; i++)
            {
                    AbilityAttackInfo skill = entries[i].AttackInfo;
                    if (skill?.baseInfo == null)
                        continue;
                    GameplayAbilitySO ability = entries[i].Ability;

                    actor.skills.Add(new SkillSnapshot
                    {
                        index = i,
                        motion = skill.baseInfo.motionKey.IsValid
                            ? skill.baseInfo.motionKey.ToString()
                            : "-",
                        selectionWeight = skill.selectionWeight,
                        cooldown = ability?.cooldown?.durationSeconds ?? 0f,
                        minRange = ability?.activation?.minDistance ?? 0f,
                        maxRange = ability?.activation?.maxDistance ?? 0f,
                        requiredLevel = skill.requiredLevel,
                        totalDamage = BalanceAttackAnalyzer.SumDamage(skill.baseInfo),
                        totalPoiseDamage = BalanceAttackAnalyzer.SumPoiseDamage(skill.baseInfo),
                        totalBreakDamage = BalanceAttackAnalyzer.SumBreakDamage(skill.baseInfo),
                    });
            }

            return actor;
        }

        private static PlayerAttackSnapshot CapturePlayerAttack(AbilitySetSO asset)
        {
            var snapshot = new PlayerAttackSnapshot
            {
                assetPath = AssetDatabase.GetAssetPath(asset),
                assetName = asset.name,
            };

            PlayerCombatAbilityDataView view =
                PlayerCombatAbilityDataView.Build(asset);
            CaptureSlotList(snapshot, "lite", view.liteComboAttackList);
            CaptureSlotList(snapshot, "heavy", view.heavyComboAttackList);
            CaptureSlotList(snapshot, "jump", view.jumpAttackList);
            CaptureSlotList(snapshot, "dash", view.dashAttackList);
            CaptureSlotList(snapshot, "skill", view.skillAttackList);
            CaptureSlot(snapshot, "counter", view.counterAttack);
            CaptureSlot(snapshot, "parryCounter", view.parryCounterAttack);
            CaptureSlot(snapshot, "entry", view.entryAttack);
            CaptureSlot(snapshot, "swapEvadeCounter", view.swapEvadeCounterAttack);
            CaptureSlot(snapshot, "swapSpecial", view.swapSpecialAttack);
            return snapshot;
        }

        private static void CaptureSlotList(PlayerAttackSnapshot snapshot, string prefix, List<AbilityAttackInfo> list)
        {
            if (list == null)
                return;
            for (int i = 0; i < list.Count; i++)
                CaptureSlot(snapshot, $"{prefix}[{i}]", list[i]);
        }

        private static void CaptureSlot(PlayerAttackSnapshot snapshot, string slot, AbilityAttackInfo info)
        {
            if (info?.baseInfo == null)
                return;

            snapshot.attacks.Add(new PlayerAttackEntry
            {
                slot = slot,
                motion = info.baseInfo.motionKey.IsValid
                    ? info.baseInfo.motionKey.ToString()
                    : "-",
                totalDamage = BalanceAttackAnalyzer.SumDamage(info.baseInfo),
                totalPoiseDamage = BalanceAttackAnalyzer.SumPoiseDamage(info.baseInfo),
                totalBreakDamage = BalanceAttackAnalyzer.SumBreakDamage(info.baseInfo),
            });
        }

        #endregion

        #region Save / Load

        public static void SaveBaseline(Snapshot snapshot)
        {
            Directory.CreateDirectory(SnapshotDirectory);
            File.WriteAllText(BaselinePath, JsonUtility.ToJson(snapshot, true));

            // 비교 이력 추적용 타임스탬프 사본도 함께 남긴다.
            string history = Path.Combine(SnapshotDirectory, $"snapshot_{DateTime.Now:yyyyMMdd_HHmmss}.json");
            File.WriteAllText(history, JsonUtility.ToJson(snapshot, true));
        }

        public static Snapshot LoadBaseline()
        {
            if (!HasBaseline)
                return null;
            try
            {
                return JsonUtility.FromJson<Snapshot>(File.ReadAllText(BaselinePath));
            }
            catch (Exception e)
            {
                Debug.LogError($"[BalanceSnapshot] 베이스라인 로드 실패: {e.Message}");
                return null;
            }
        }

        #endregion

        #region Diff

        public static List<DiffEntry> Diff(Snapshot baseline, Snapshot current)
        {
            var diffs = new List<DiffEntry>();
            if (baseline == null || current == null)
                return diffs;

            DiffActors(baseline, current, diffs);
            DiffPlayerAttacks(baseline, current, diffs);

            diffs.Sort((a, b) => b.RelativeChange.CompareTo(a.RelativeChange));
            return diffs;
        }

        private static void DiffActors(Snapshot baseline, Snapshot current, List<DiffEntry> diffs)
        {
            var baseMap = new Dictionary<string, ActorSnapshot>();
            foreach (ActorSnapshot actor in baseline.actors)
                baseMap[actor.actorId] = actor;

            var seen = new HashSet<string>();
            foreach (ActorSnapshot cur in current.actors)
            {
                seen.Add(cur.actorId);
                if (!baseMap.TryGetValue(cur.actorId, out ActorSnapshot old))
                {
                    diffs.Add(new DiffEntry { Kind = DiffKind.Added, Owner = cur.actorId, Field = "actor", Detail = cur.definitionPath });
                    continue;
                }

                CompareValue(diffs, cur.actorId, "level", old.level, cur.level);
                if (old.grade != cur.grade)
                    diffs.Add(new DiffEntry { Kind = DiffKind.ValueChanged, Owner = cur.actorId, Field = $"grade ({old.grade}→{cur.grade})", OldValue = 0f, NewValue = 1f });

                DiffStats(diffs, cur.actorId, old.stats, cur.stats);
                DiffBreakGauge(diffs, old, cur);
                DiffSkills(diffs, cur.actorId, old.skills, cur.skills);
            }

            foreach (ActorSnapshot old in baseline.actors)
            {
                if (!seen.Contains(old.actorId))
                    diffs.Add(new DiffEntry { Kind = DiffKind.Removed, Owner = old.actorId, Field = "actor", Detail = old.definitionPath });
            }
        }

        private static void DiffStats(List<DiffEntry> diffs, string owner, List<StatValue> oldStats, List<StatValue> newStats)
        {
            var oldMap = new Dictionary<string, float>();
            foreach (StatValue stat in oldStats)
                oldMap[stat.attributeId] = stat.value;

            var seen = new HashSet<string>();
            foreach (StatValue cur in newStats)
            {
                seen.Add(cur.attributeId);
                if (oldMap.TryGetValue(cur.attributeId, out float oldValue))
                    CompareValue(diffs, owner, $"attributes.{cur.attributeId}", oldValue, cur.value);
                else
                    diffs.Add(new DiffEntry { Kind = DiffKind.Added, Owner = owner, Field = $"attributes.{cur.attributeId}", Detail = $"= {cur.value:0.###}" });
            }

            foreach (StatValue old in oldStats)
            {
                if (!seen.Contains(old.attributeId))
                    diffs.Add(new DiffEntry { Kind = DiffKind.Removed, Owner = owner, Field = $"attributes.{old.attributeId}", Detail = $"이전 값 {old.value:0.###}" });
            }
        }

        private static void DiffBreakGauge(List<DiffEntry> diffs, ActorSnapshot old, ActorSnapshot cur)
        {
            string owner = cur.actorId;
            if (old.hasBreakGauge != cur.hasBreakGauge)
            {
                diffs.Add(new DiffEntry
                {
                    Kind = cur.hasBreakGauge ? DiffKind.Added : DiffKind.Removed,
                    Owner = owner,
                    Field = "breakGauge",
                    Detail = cur.hasBreakGauge ? $"maxGauge {cur.breakMaxGauge:0.###}" : $"이전 maxGauge {old.breakMaxGauge:0.###}",
                });
                return;
            }

            if (!cur.hasBreakGauge)
                return;

            CompareValue(diffs, owner, "break.maxGauge", old.breakMaxGauge, cur.breakMaxGauge);
            CompareValue(diffs, owner, "break.resist", old.breakResist, cur.breakResist);
            CompareValue(diffs, owner, "break.exposedDuration", old.breakExposedDuration, cur.breakExposedDuration);
            CompareValue(diffs, owner, "break.damageTakenMultiplier", old.breakDamageTakenMultiplier, cur.breakDamageTakenMultiplier);
        }

        private static void DiffSkills(List<DiffEntry> diffs, string owner, List<SkillSnapshot> oldSkills, List<SkillSnapshot> newSkills)
        {
            // 스킬은 인덱스+motion 조합으로 매칭한다. 순서가 바뀌면 Added/Removed로 표시된다.
            var oldMap = new Dictionary<string, SkillSnapshot>();
            foreach (SkillSnapshot skill in oldSkills)
                oldMap[$"{skill.index}:{skill.motion}"] = skill;

            var seen = new HashSet<string>();
            foreach (SkillSnapshot cur in newSkills)
            {
                string key = $"{cur.index}:{cur.motion}";
                seen.Add(key);
                string label = $"skills[{cur.index}]({cur.motion})";
                if (!oldMap.TryGetValue(key, out SkillSnapshot old))
                {
                    diffs.Add(new DiffEntry { Kind = DiffKind.Added, Owner = owner, Field = label, Detail = $"damage {cur.totalDamage:0.###}" });
                    continue;
                }

                CompareValue(diffs, owner, $"{label}.weight", old.selectionWeight, cur.selectionWeight);
                CompareValue(diffs, owner, $"{label}.cooldown", old.cooldown, cur.cooldown);
                CompareValue(diffs, owner, $"{label}.minRange", old.minRange, cur.minRange);
                CompareValue(diffs, owner, $"{label}.maxRange", old.maxRange, cur.maxRange);
                CompareValue(diffs, owner, $"{label}.requiredLevel", old.requiredLevel, cur.requiredLevel);
                CompareValue(diffs, owner, $"{label}.damage", old.totalDamage, cur.totalDamage);
                CompareValue(diffs, owner, $"{label}.poiseDamage", old.totalPoiseDamage, cur.totalPoiseDamage);
                CompareValue(diffs, owner, $"{label}.breakDamage", old.totalBreakDamage, cur.totalBreakDamage);
            }

            foreach (SkillSnapshot old in oldSkills)
            {
                if (!seen.Contains($"{old.index}:{old.motion}"))
                    diffs.Add(new DiffEntry { Kind = DiffKind.Removed, Owner = owner, Field = $"skills[{old.index}]({old.motion})", Detail = $"damage {old.totalDamage:0.###}" });
            }
        }

        private static void DiffPlayerAttacks(Snapshot baseline, Snapshot current, List<DiffEntry> diffs)
        {
            var baseMap = new Dictionary<string, PlayerAttackSnapshot>();
            foreach (PlayerAttackSnapshot snapshot in baseline.playerAttacks)
                baseMap[snapshot.assetName] = snapshot;

            var seenAssets = new HashSet<string>();
            foreach (PlayerAttackSnapshot cur in current.playerAttacks)
            {
                seenAssets.Add(cur.assetName);
                if (!baseMap.TryGetValue(cur.assetName, out PlayerAttackSnapshot old))
                {
                    diffs.Add(new DiffEntry { Kind = DiffKind.Added, Owner = cur.assetName, Field = "playerAttackData", Detail = cur.assetPath });
                    continue;
                }

                var oldMap = new Dictionary<string, PlayerAttackEntry>();
                foreach (PlayerAttackEntry entry in old.attacks)
                    oldMap[entry.slot] = entry;

                var seenSlots = new HashSet<string>();
                foreach (PlayerAttackEntry entry in cur.attacks)
                {
                    seenSlots.Add(entry.slot);
                    if (!oldMap.TryGetValue(entry.slot, out PlayerAttackEntry oldEntry))
                    {
                        diffs.Add(new DiffEntry { Kind = DiffKind.Added, Owner = cur.assetName, Field = entry.slot, Detail = $"damage {entry.totalDamage:0.###}" });
                        continue;
                    }

                    CompareValue(diffs, cur.assetName, $"{entry.slot}.damage", oldEntry.totalDamage, entry.totalDamage);
                    CompareValue(diffs, cur.assetName, $"{entry.slot}.poiseDamage", oldEntry.totalPoiseDamage, entry.totalPoiseDamage);
                    CompareValue(diffs, cur.assetName, $"{entry.slot}.breakDamage", oldEntry.totalBreakDamage, entry.totalBreakDamage);
                }

                foreach (PlayerAttackEntry oldEntry in old.attacks)
                {
                    if (!seenSlots.Contains(oldEntry.slot))
                        diffs.Add(new DiffEntry { Kind = DiffKind.Removed, Owner = cur.assetName, Field = oldEntry.slot, Detail = $"damage {oldEntry.totalDamage:0.###}" });
                }
            }

            foreach (PlayerAttackSnapshot old in baseline.playerAttacks)
            {
                if (!seenAssets.Contains(old.assetName))
                    diffs.Add(new DiffEntry { Kind = DiffKind.Removed, Owner = old.assetName, Field = "playerAttackData", Detail = old.assetPath });
            }
        }

        private static void CompareValue(List<DiffEntry> diffs, string owner, string field, float oldValue, float newValue)
        {
            if (Mathf.Abs(oldValue - newValue) <= 0.0001f)
                return;

            diffs.Add(new DiffEntry
            {
                Kind = DiffKind.ValueChanged,
                Owner = owner,
                Field = field,
                OldValue = oldValue,
                NewValue = newValue,
            });
        }

        #endregion

        private static IEnumerable<T> LoadAll<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                    yield return asset;
            }
        }
    }
}
#endif
