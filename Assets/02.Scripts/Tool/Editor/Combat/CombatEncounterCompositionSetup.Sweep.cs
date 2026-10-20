using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.Stat;
using UPlayGround.Tool.Editor.Balance;

namespace UPlayGround.Tool.Editor.Combat
{
    public static partial class CombatEncounterCompositionSetup
    {
        /// <summary>기본 약공격 시나리오에 맞춰 소탕 등급의 HP·강인도만 갱신한다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/게임플레이/전투/정책/전투 구도 P1 적용")]
        public static void ApplyP1()
        {
            PreviewP1();
            var targets = Resources.Load<CombatBalanceTargetsSO>(CombatBalanceTargetsSO.ResourceKey);
            var scenario = AssetDatabase.LoadAssetAtPath<BalanceScenarioAsset>(
                "Assets/10.Datas/Balance/Scenarios/BalanceScenario_Raon.asset");
            float power = scenario.manualPlayerAttackPower;
            if (scenario.playerAttributeProfile != null && scenario.playerAttributeProfile.TryGetBaseValue(
                Attributes.Combat.AttackPower, out float value)) power = value;
            float dps = BalanceAttackAnalyzer.EstimateLightComboDps(scenario.playerAbilitySet,
                scenario.playerAttackInterval) * power;
            if (dps <= 0f) throw new InvalidOperationException("기본 콤보 DPS를 계산할 수 없습니다.");
            var actors = new List<ActorDefinitionSO>();
            var scalings = new HashSet<MonsterScalingSO>();
            foreach (string guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
            {
                var actor = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                if ((actor.actorType & ActorType.Monster) == 0 ||
                    (actor.EffectiveGrade != MonsterActorGrade.Weak && actor.EffectiveGrade != MonsterActorGrade.Normal)) continue;
                if (actor.EffectiveMonsterScaling == null || actor.attributeProfile == null)
                    throw new InvalidOperationException($"스케일링/스탯 연결 누락: {actor.actorId}");
                actors.Add(actor);
                scalings.Add(actor.EffectiveMonsterScaling);
            }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            var changed = new HashSet<UnityEngine.Object>();
            var report = new List<string> { "Actor,Grade,HPBefore,HPAfter,PoiseBefore,PoiseAfter,ExpBefore,ExpAfter,GoldBefore,GoldAfter" };
            try
            {
                foreach (var scaling in scalings)
                {
                    Undo.RecordObject(scaling, "소탕 등급 배율");
                    for (int i = 0; i < scaling.gradeScalings.Count; i++)
                    {
                        var entry = scaling.gradeScalings[i];
                        if (entry.grade != MonsterActorGrade.Weak && entry.grade != MonsterActorGrade.Normal) continue;
                        var before = MonsterStatCalculator.Calculate(scaling, entry.grade, 1);
                        float duration = before[Attributes.Vital.MaxHealth] / (dps * (1f - before[Attributes.Combat.Defense]));
                        entry.healthMultiplier *= targets.GetTargetSeconds(entry.grade) / duration;
                        entry.poiseMultiplier = entry.grade == MonsterActorGrade.Weak ? 0.2f : 0.5f;
                        scaling.gradeScalings[i] = entry;
                    }
                    changed.Add(scaling);
                }
                var profileValues = new Dictionary<UnityEngine.Object, Vector2>();
                foreach (var actor in actors)
                {
                    var stats = MonsterStatCalculator.Calculate(actor.EffectiveMonsterScaling, actor.EffectiveGrade, actor.EffectiveLevel);
                    var desired = new Vector2(stats[Attributes.Vital.MaxHealth], stats[Attributes.Vital.MaxPoise]);
                    var profile = actor.attributeProfile;
                    if (profileValues.TryGetValue(profile, out var prior) && prior != desired)
                        throw new InvalidOperationException($"공유 스탯 프로필의 등급/레벨 충돌: {actor.actorId}");
                    profileValues[profile] = desired;
                    profile.TryGetBaseValue(Attributes.Vital.MaxHealth, out float hp);
                    profile.TryGetBaseValue(Attributes.Vital.MaxPoise, out float poise);
                    Undo.RecordObject(profile, "소탕 HP·강인도");
                    profile.EditorSetBaseValue(Attributes.Vital.MaxHealth, desired.x);
                    profile.EditorSetBaseValue(Attributes.Vital.MaxPoise, desired.y);
                    changed.Add(profile);
                    report.Add(FormattableString.Invariant($"{actor.actorId},{actor.EffectiveGrade},{hp},{desired.x},{poise},{desired.y},{actor.EffectiveExpReward},{actor.EffectiveExpReward},{actor.EffectiveGoldReward},{actor.EffectiveGoldReward}"));
                }
                foreach (var asset in changed) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (var asset in changed) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
                throw;
            }
            File.WriteAllLines("output/combat-encounter-composition/p1-applied.csv", report);
            Debug.Log($"[전투 구도] P1 기본 콤보 DPS {dps}, 간격 {scenario.playerAttackInterval}초 가정, {actors.Count}개 반영");
        }
    }
}
