#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// 프로젝트 전체를 스캔해 플레이어 공격 데이터, 몬스터 공격 데이터,
    /// 플레이어 스탯, 몬스터 스탯을 요약 추출하는 에디터 전용 서비스.
    /// Balance Designer가 다루지 못하는 데이터셋 전반의 분포를 한눈에 비교하기 위한 용도.
    /// </summary>
    public static class BalanceDataExtractor
    {
        public enum StatOwner
        {
            Unknown,
            Player,
            Monster,
        }

        public sealed class PlayerAttackSummary
        {
            public PlayerAttackDataSO Asset;
            public string AssetName;
            public string Path;
            public int LiteCount;
            public int HeavyCount;
            public int JumpCount;
            public int DashCount;
            public int SkillCount;
            public int ChargeStageCount;
            public int ComboRouteCount;
            public int TotalAttacks;
            public int TotalHitPhases;
            public float TotalDamage;
            public float AvgDamagePerAttack;
            public float MaxSingleAttackDamage;
        }

        public sealed class MonsterAttackSummary
        {
            public EnemyAttackDataSO Asset;
            public string AssetName;
            public string Path;
            public int SkillCount;
            public int AttackSkillCount;
            public int BasicCount;
            public int HeavyCount;
            public int SkillCatCount;
            public int RangedCount;
            public float GlobalCooldown;
            public float TotalWeight;
            public float StrongWeightShare;
            public float TotalDamage;
            public float AvgDamagePerSkill;
            public float MaxSingleAttackDamage;
            public int DangerRingCovered;
            public int DangerRingMissing;
            public int TelegraphCount;
        }

        public sealed class StatSummary
        {
            public ActorStatSO Asset;
            public string AssetName;
            public string Path;
            public StatOwner Owner;
            public float MaxHealth;
            public float AttackPower;
            public float Defense;
            public float MaxPoise;
            public float MoveSpeed;
            public float CritRate;
        }

        public static List<PlayerAttackSummary> ExtractPlayerAttackData()
        {
            var result = new List<PlayerAttackSummary>();
            foreach (PlayerAttackDataSO asset in LoadAll<PlayerAttackDataSO>())
            {
                var summary = new PlayerAttackSummary
                {
                    Asset = asset,
                    AssetName = asset.name,
                    Path = AssetDatabase.GetAssetPath(asset),
                    LiteCount = Count(asset.liteComboAttackList),
                    HeavyCount = Count(asset.heavyComboAttackList),
                    JumpCount = Count(asset.jumpAttackList),
                    DashCount = Count(asset.dashAttackList),
                    SkillCount = Count(asset.skillAttackList),
                    ChargeStageCount = asset.chargeStages?.Count ?? 0,
                    ComboRouteCount = asset.comboRoutes?.Count ?? 0,
                };

                var attacks = new List<PlayerAttackInfo>();
                AddRange(attacks, asset.liteComboAttackList);
                AddRange(attacks, asset.heavyComboAttackList);
                AddRange(attacks, asset.jumpAttackList);
                AddRange(attacks, asset.dashAttackList);
                AddRange(attacks, asset.skillAttackList);
                AddOne(attacks, asset.counterAttack);
                AddOne(attacks, asset.parryCounterAttack);
                AddOne(attacks, asset.entryAttack);
                AddOne(attacks, asset.swapSpecialAttack);

                float total = 0f;
                int counted = 0;
                for (int i = 0; i < attacks.Count; i++)
                {
                    AttackInfoBase info = attacks[i]?.baseInfo;
                    if (info == null)
                        continue;

                    float damage = BalanceAttackAnalyzer.SumDamage(info);
                    summary.TotalHitPhases += BalanceAttackAnalyzer.CountHitPhases(info);
                    summary.MaxSingleAttackDamage = Mathf.Max(summary.MaxSingleAttackDamage, damage);
                    total += damage;
                    counted++;
                }

                summary.TotalAttacks = counted;
                summary.TotalDamage = total;
                summary.AvgDamagePerAttack = counted > 0 ? total / counted : 0f;
                result.Add(summary);
            }

            result.Sort((a, b) => string.CompareOrdinal(a.AssetName, b.AssetName));
            return result;
        }

        public static List<MonsterAttackSummary> ExtractMonsterAttackData()
        {
            var result = new List<MonsterAttackSummary>();
            foreach (EnemyAttackDataSO asset in LoadAll<EnemyAttackDataSO>())
            {
                var summary = new MonsterAttackSummary
                {
                    Asset = asset,
                    AssetName = asset.name,
                    Path = AssetDatabase.GetAssetPath(asset),
                    SkillCount = asset.skills?.Count ?? 0,
                    GlobalCooldown = asset.globalCooldown,
                };

                float total = 0f;
                float strongWeight = 0f;
                if (asset.skills != null)
                {
                    for (int i = 0; i < asset.skills.Count; i++)
                    {
                        EnemyAttackInfo skill = asset.skills[i];
                        if (skill == null || skill.baseInfo == null)
                            continue;
                        if (skill.skillType != SkillType.Attack)
                            continue;

                        summary.AttackSkillCount++;
                        summary.TotalWeight += Mathf.Max(0f, skill.selectionWeight);

                        switch (skill.attackCategory)
                        {
                            case EnemyAttackCategory.Heavy:
                                summary.HeavyCount++;
                                strongWeight += Mathf.Max(0f, skill.selectionWeight);
                                break;
                            case EnemyAttackCategory.Skill:
                                summary.SkillCatCount++;
                                strongWeight += Mathf.Max(0f, skill.selectionWeight);
                                break;
                            default:
                                summary.BasicCount++;
                                break;
                        }

                        if (skill.baseInfo.attackType == AttackType.Ranged)
                            summary.RangedCount++;

                        if (BalanceAttackAnalyzer.IsStrongEnemyAttack(skill))
                        {
                            if (skill.useDangerRing) summary.DangerRingCovered++;
                            else summary.DangerRingMissing++;
                        }

                        if (skill.useTelegraph)
                            summary.TelegraphCount++;

                        float damage = BalanceAttackAnalyzer.SumDamage(skill.baseInfo);
                        summary.MaxSingleAttackDamage = Mathf.Max(summary.MaxSingleAttackDamage, damage);
                        total += damage;
                    }
                }

                summary.TotalDamage = total;
                summary.AvgDamagePerSkill = summary.AttackSkillCount > 0 ? total / summary.AttackSkillCount : 0f;
                summary.StrongWeightShare = summary.TotalWeight > 0f ? strongWeight / summary.TotalWeight : 0f;
                result.Add(summary);
            }

            result.Sort((a, b) => string.CompareOrdinal(a.AssetName, b.AssetName));
            return result;
        }

        public static List<StatSummary> ExtractStats(StatOwner ownerFilter)
        {
            Dictionary<ActorStatSO, StatOwner> ownership = BuildStatOwnership();
            var result = new List<StatSummary>();

            foreach (ActorStatSO asset in LoadAll<ActorStatSO>())
            {
                StatOwner owner = ownership.TryGetValue(asset, out StatOwner o) ? o : StatOwner.Unknown;
                if (ownerFilter != StatOwner.Unknown && owner != ownerFilter)
                    continue;

                result.Add(new StatSummary
                {
                    Asset = asset,
                    AssetName = asset.name,
                    Path = AssetDatabase.GetAssetPath(asset),
                    Owner = owner,
                    MaxHealth = asset.GetBase(StatType.MaxHealth),
                    AttackPower = asset.GetBase(StatType.AttackPower),
                    Defense = asset.GetBase(StatType.Defense),
                    MaxPoise = asset.GetBase(StatType.MaxPoise),
                    MoveSpeed = asset.GetBase(StatType.MoveSpeed),
                    CritRate = asset.GetBase(StatType.CritRate),
                });
            }

            result.Sort((a, b) =>
            {
                int byOwner = a.Owner.CompareTo(b.Owner);
                return byOwner != 0 ? byOwner : string.CompareOrdinal(a.AssetName, b.AssetName);
            });
            return result;
        }

        /// <summary>
        /// ActorDefinitionSO를 모두 스캔해 각 ActorStatSO가 플레이어/몬스터 중 누구에게 참조되는지 매핑한다.
        /// 양쪽에서 참조되면 Player 우선. 어디에서도 참조되지 않으면 매핑에 없음(Unknown).
        /// </summary>
        private static Dictionary<ActorStatSO, StatOwner> BuildStatOwnership()
        {
            var map = new Dictionary<ActorStatSO, StatOwner>();

            // ActorDefinitionSO.statData → ActorType 기준 분류
            foreach (ActorDefinitionSO def in LoadAll<ActorDefinitionSO>())
            {
                if (def.statData == null)
                    continue;

                if ((def.actorType & ActorType.Player) != 0)
                    Assign(map, def.statData, StatOwner.Player);
                else if ((def.actorType & ActorType.Monster) != 0)
                    Assign(map, def.statData, StatOwner.Monster);
            }

            // PartyMemberGrowthSO.baseStat → 플레이어 캐릭터 스탯 (플레이어는 주로 이 경로로 참조됨)
            foreach (PartyMemberGrowthSO growth in LoadAll<PartyMemberGrowthSO>())
            {
                if (growth.baseStat != null)
                    Assign(map, growth.baseStat, StatOwner.Player);
            }

            return map;
        }

        /// <summary>이미 매핑된 경우 Player를 우선 보존/승격한다.</summary>
        private static void Assign(Dictionary<ActorStatSO, StatOwner> map, ActorStatSO stat, StatOwner owner)
        {
            if (map.TryGetValue(stat, out StatOwner existing))
            {
                if (existing != StatOwner.Player && owner == StatOwner.Player)
                    map[stat] = StatOwner.Player;
            }
            else
            {
                map[stat] = owner;
            }
        }

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

        private static int Count<T>(List<T> list) => list?.Count ?? 0;

        private static void AddRange(List<PlayerAttackInfo> target, List<PlayerAttackInfo> source)
        {
            if (source == null)
                return;
            for (int i = 0; i < source.Count; i++)
                AddOne(target, source[i]);
        }

        private static void AddOne(List<PlayerAttackInfo> target, PlayerAttackInfo value)
        {
            if (value?.baseInfo != null)
                target.Add(value);
        }
    }
}
#endif
