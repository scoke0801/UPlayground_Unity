using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.Stat;
using UPlayGround.Tool.Editor.Balance;

namespace UPlayGround.Tool.Editor.Combat
{
    /// <summary>전투 구도 데이터의 누락을 기존 GUID를 보존하며 정리한다.</summary>
    public static partial class CombatEncounterCompositionSetup
    {
        private const string ResourceFolder = "Assets/10.Datas/Combat/Resources";
        private const string ToolId = "UPlayGround/게임플레이/전투/정책/전투 구도 P0 적용";

        [UPlayGround.EditorTools.UPlaygroundTool(ToolId)]
        public static void ApplyP0()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("컴파일이 끝난 Edit Mode에서 적용하세요.");
            if (!CombatPolicyAssetGenerator.TryLoadDefaultPolicies(out _, out var elite, out var boss)
                || elite == null || boss == null)
                throw new InvalidOperationException("Elite/Boss 경직 정책 원본이 필요합니다.");

            Directory.CreateDirectory(ResourceFolder);
            AssetDatabase.Refresh();
            var fallback = LoadOrCreate<CombatReactionPolicySO>(
                ResourceFolder + "/" + CombatPolicyResolver.DefaultPolicyResourceKey + ".asset");
            foreach (MonsterActorGrade grade in Enum.GetValues(typeof(MonsterActorGrade)))
            {
                if (fallback.GetRule(grade) != null) continue;
                var source = grade == MonsterActorGrade.Boss ? boss.GetRule(grade)
                    : grade == MonsterActorGrade.Elite ? elite.GetRule(grade) : null;
                fallback.monsterGradeRules.Add(source != null
                    ? JsonUtility.FromJson<CombatReactionPolicySO.GradeRule>(JsonUtility.ToJson(source))
                    : new CombatReactionPolicySO.GradeRule { grade = grade });
            }
            EditorUtility.SetDirty(fallback);
            LoadOrCreate<CombatBalanceTargetsSO>(ResourceFolder + "/" + CombatBalanceTargetsSO.ResourceKey + ".asset");

            // 승인된 기존 프로필 GUID만 재분류한다. 실행 중 이름 기반 예외는 만들지 않는다.
            var reclassify = new HashSet<string>
            {
                "d66cd37cdbf4561479d33ddaafc90048", "1c5eb8b4cb88b1949b77c8674265a880",
                "a4bdea6e7681f034fa57d05385a02333",
            };
            var report = new List<string> { "종류,경로,등급,정책,등급규칙" };
            foreach (string guid in AssetDatabase.FindAssets("t:MonsterActorProfileSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var profile = AssetDatabase.LoadAssetAtPath<MonsterActorProfileSO>(path);
                var grade = reclassify.Contains(guid) ? MonsterActorGrade.Elite : profile.grade;
                var policy = ResolvePolicy(profile.combatReactionPolicy, grade, elite, boss, fallback);
                if (profile.grade != grade || profile.combatReactionPolicy != policy)
                {
                    Undo.RecordObject(profile, "전투 구도 경직 정책 정리");
                    profile.grade = grade;
                    profile.combatReactionPolicy = policy;
                    EditorUtility.SetDirty(profile);
                }
                report.Add($"Profile,{path},{profile.grade},{profile.combatReactionPolicy.name},True");
            }
            foreach (string guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var actor = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                if ((actor.actorType & ActorType.Monster) == 0) continue;
                var grade = actor.EffectiveGrade;
                var policy = ResolvePolicy(actor.EffectiveCombatReactionPolicy, grade, elite, boss, fallback);
                bool synchronizeGrade = actor.monsterProfile != null && reclassify.Contains(
                    AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(actor.monsterProfile)));
                if ((synchronizeGrade && actor.grade != grade) || actor.combatReactionPolicy != policy)
                {
                    Undo.RecordObject(actor, "전투 구도 경직 정책 정리");
                    if (synchronizeGrade) actor.grade = grade;
                    actor.combatReactionPolicy = policy;
                    EditorUtility.SetDirty(actor);
                }
                report.Add($"Definition,{path},{actor.EffectiveGrade},{actor.EffectiveCombatReactionPolicy.name},True");
                if (actor.EffectiveCombatReactionPolicy.GetRule(actor.EffectiveGrade) == null)
                    throw new InvalidOperationException($"등급 규칙 누락: {path}");
            }
            AssetDatabase.SaveAssets();
            Directory.CreateDirectory("output/combat-encounter-composition");
            File.WriteAllLines("output/combat-encounter-composition/p0-unity-policy-audit.csv", report);
            Debug.Log($"[전투 구도] P0 정책 연결 검증 완료: {report.Count - 1}개");
        }

        /// <summary>에셋을 변경하지 않고 P1의 배율 제안과 보상·리액션 영향 목록을 만든다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/게임플레이/전투/정책/전투 구도 P1 미리보기")]
        public static void PreviewP1()
        {
            var scenario = AssetDatabase.LoadAssetAtPath<BalanceScenarioAsset>(
                "Assets/10.Datas/Balance/Scenarios/BalanceScenario_Raon.asset");
            var targets = Resources.Load<CombatBalanceTargetsSO>(CombatBalanceTargetsSO.ResourceKey);
            if (scenario == null || targets == null)
                throw new InvalidOperationException("P0 목표와 기준 플레이어 시나리오가 필요합니다.");
            float attackPower = scenario.manualPlayerAttackPower;
            if (scenario.playerAttributeProfile != null
                && scenario.playerAttributeProfile.TryGetBaseValue(Attributes.Combat.AttackPower, out float value))
                attackPower = value;
            float dps = BalanceAttackAnalyzer.EstimateLightComboDps(scenario.playerAbilitySet,
                scenario.playerAttackInterval) * attackPower;
            if (dps <= 0f) throw new InvalidOperationException("기준 DPS가 0입니다.");
            var scalings = new HashSet<MonsterScalingSO>();
            var actors = new List<string> { "Actor,Grade,Level,BaseHP,BasePoise,ExpPerKill,GoldPerKill,AttributeProfile,AbilitySet" };
            var reactions = new List<string> { "Actor,MotionKey,HitPhase,Reaction,HasDataTelegraph,Proposal" };
            foreach (string guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
            {
                var actor = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                if ((actor.actorType & ActorType.Monster) == 0) continue;
                if (actor.EffectiveGrade != MonsterActorGrade.Normal && actor.EffectiveGrade != MonsterActorGrade.Weak) continue;
                if (actor.EffectiveMonsterScaling != null) scalings.Add(actor.EffectiveMonsterScaling);
                float hp = 0f, poise = 0f;
                if (actor.attributeProfile != null)
                {
                    actor.attributeProfile.TryGetBaseValue(Attributes.Vital.MaxHealth, out hp);
                    actor.attributeProfile.TryGetBaseValue(Attributes.Vital.MaxPoise, out poise);
                }
                actors.Add(FormattableString.Invariant($"{actor.actorId},{actor.EffectiveGrade},{actor.EffectiveLevel},{hp},{poise},{actor.EffectiveExpReward},{actor.EffectiveGoldReward},{AssetDatabase.GetAssetPath(actor.attributeProfile)},{AssetDatabase.GetAssetPath(actor.EffectiveAbilitySet)}"));
                foreach (var entry in UPlayGround.EditorTools.AbilityAttackEditorUtility.Collect(actor.EffectiveAbilitySet, true))
                {
                    var info = entry.AttackInfo;
                    if (info?.baseInfo?.hitPhases == null || !info.aiSelectable) continue;
                    for (int phase = 0; phase < info.baseInfo.hitPhases.Count; phase++)
                    {
                        var hit = info.baseInfo.hitPhases[phase];
                        if (hit == null) continue;
                        bool strong = hit.reactionType is AttackReactionType.KnockBack or AttackReactionType.Knockdown or AttackReactionType.Airborne;
                        reactions.Add($"{actor.actorId},{info.motionKey},{phase},{hit.reactionType},{info.useTelegraph || info.useDangerRing},{(strong && !info.useTelegraph && !info.useDangerRing ? "Motion 텔레그래프 확인 후 Hit/Light 검토" : "유지")}");
                    }
                }
            }
            var report = new List<string> { "Scaling,Grade,HealthBefore,HealthProposed,PoiseBefore,PoiseProposed,L1TimeBefore,L1TimeProposed,ReferenceDps" };
            foreach (var scaling in scalings)
            {
                foreach (var grade in new[] { MonsterActorGrade.Weak, MonsterActorGrade.Normal })
                {
                    if (!scaling.TryGetGrade(grade, out var entry)) continue;
                    var before = MonsterStatCalculator.Calculate(scaling, grade, 1);
                    float duration = before[Attributes.Vital.MaxHealth] / (dps * (1f - before[Attributes.Combat.Defense]));
                    if (duration <= 0f || entry.healthMultiplier <= 0f) throw new InvalidOperationException("등급 배율이 유효하지 않습니다.");
                    float health = entry.healthMultiplier * targets.GetTargetSeconds(grade) / duration;
                    float poise = grade == MonsterActorGrade.Weak ? 0.2f : 0.5f;
                    report.Add(FormattableString.Invariant($"{AssetDatabase.GetAssetPath(scaling)},{grade},{entry.healthMultiplier},{health},{entry.poiseMultiplier},{poise},{duration},{targets.GetTargetSeconds(grade)},{dps}"));
                }
            }
            Directory.CreateDirectory("output/combat-encounter-composition");
            File.WriteAllLines("output/combat-encounter-composition/p1-grade-preview.csv", report);
            File.WriteAllLines("output/combat-encounter-composition/p1-actor-preview.csv", actors);
            File.WriteAllLines("output/combat-encounter-composition/p1-reaction-audit.csv", reactions);
            Debug.Log($"[전투 구도] P1 읽기 전용 미리보기: DPS {dps}, 액터 {actors.Count - 1}개. 에셋 변경 없음.");
        }

        private static CombatReactionPolicySO ResolvePolicy(CombatReactionPolicySO current,
            MonsterActorGrade grade, CombatReactionPolicySO elite, CombatReactionPolicySO boss,
            CombatReactionPolicySO fallback)
        {
            if (current != null && current.GetRule(grade) != null) return current;
            return grade switch { MonsterActorGrade.Boss => boss, MonsterActorGrade.Elite => elite, _ => fallback };
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
