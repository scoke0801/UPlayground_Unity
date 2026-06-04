#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Tool.Editor.Combat
{
    public static class CombatPolicyAssetGenerator
    {
        private const string PolicyFolder = "Assets/10.Datas/Combat/Policy";
        private const string DefaultDefensePolicyPath = PolicyFolder + "/DefaultCombatDefensePolicy.asset";
        private const string EliteReactionPolicyPath = PolicyFolder + "/EliteCombatReactionPolicy.asset";
        private const string BossReactionPolicyPath = PolicyFolder + "/BossCombatReactionPolicy.asset";

        [MenuItem("UPlayGround/게임플레이/전투/정책/기본 정책 에셋 생성", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.GameplayCombatTools + 2)]
        public static void GenerateDefaultPolicyAssets()
        {
            EnsureFolder(PolicyFolder);

            CombatDefensePolicySO defensePolicy = LoadOrCreateAsset<CombatDefensePolicySO>(DefaultDefensePolicyPath, out bool createdDefensePolicy);
            if (createdDefensePolicy)
                ConfigureDefaultDefensePolicy(defensePolicy);

            CombatReactionPolicySO eliteReactionPolicy = LoadOrCreateAsset<CombatReactionPolicySO>(EliteReactionPolicyPath, out bool createdEliteReactionPolicy);
            if (createdEliteReactionPolicy)
                ConfigureEliteReactionPolicy(eliteReactionPolicy);

            CombatReactionPolicySO bossReactionPolicy = LoadOrCreateAsset<CombatReactionPolicySO>(BossReactionPolicyPath, out bool createdBossReactionPolicy);
            if (createdBossReactionPolicy)
                ConfigureBossReactionPolicy(bossReactionPolicy);

            int assignedCount = AssignDefaultPolicies(defensePolicy, eliteReactionPolicy, bossReactionPolicy);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = bossReactionPolicy != null ? bossReactionPolicy : defensePolicy;
            string message = $"기본 전투 정책 에셋 생성/갱신 완료\n\n생성 위치: {PolicyFolder}\nActorDefinition 자동 연결: {assignedCount}개";
            if (Application.isBatchMode)
                Debug.Log(message);
            else
                EditorUtility.DisplayDialog("Combat Policy Assets", message, "OK");
        }

        /// <summary>
        /// 자동 생성된 기본 정책 에셋을 알려진 경로에서 로드한다. 외부 툴(Stat Generator 등)이
        /// 등급 기반 자동 연결을 재사용할 때 경로 상수를 중복하지 않도록 노출한다.
        /// 하나라도 존재하면 true. 없는 항목은 null로 반환된다.
        /// </summary>
        public static bool TryLoadDefaultPolicies(
            out CombatDefensePolicySO defense,
            out CombatReactionPolicySO eliteReaction,
            out CombatReactionPolicySO bossReaction)
        {
            defense = AssetDatabase.LoadAssetAtPath<CombatDefensePolicySO>(DefaultDefensePolicyPath);
            eliteReaction = AssetDatabase.LoadAssetAtPath<CombatReactionPolicySO>(EliteReactionPolicyPath);
            bossReaction = AssetDatabase.LoadAssetAtPath<CombatReactionPolicySO>(BossReactionPolicyPath);
            return defense != null || eliteReaction != null || bossReaction != null;
        }

        /// <summary>등급에 맞는 기본 리액션 정책을 고른다(Elite/Boss만 대상, 그 외 null).</summary>
        public static CombatReactionPolicySO ResolveReactionPolicyForGrade(
            MonsterActorGrade grade,
            CombatReactionPolicySO eliteReaction,
            CombatReactionPolicySO bossReaction)
            => grade switch
            {
                MonsterActorGrade.Elite => eliteReaction,
                MonsterActorGrade.Boss => bossReaction,
                _ => null,
            };

        private static void EnsureFolder(string assetFolder)
        {
            if (AssetDatabase.IsValidFolder(assetFolder))
                return;

            string absolutePath = Path.Combine(Directory.GetCurrentDirectory(), assetFolder);
            Directory.CreateDirectory(absolutePath);
            AssetDatabase.Refresh();
        }

        private static T LoadOrCreateAsset<T>(string path, out bool created) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                created = false;
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            created = true;
            return asset;
        }

        private static void ConfigureDefaultDefensePolicy(CombatDefensePolicySO policy)
        {
            if (policy == null)
                return;

            policy.allowGuardAgainstUnblockable = false;
            policy.allowParryAgainstUnblockable = false;
            policy.allowPerfectDodgeAgainstUnblockable = true;
            EditorUtility.SetDirty(policy);
        }

        private static void ConfigureEliteReactionPolicy(CombatReactionPolicySO policy)
        {
            if (policy == null)
                return;

            policy.monsterGradeRules.Clear();
            policy.monsterGradeRules.Add(new CombatReactionPolicySO.GradeRule
            {
                grade = MonsterActorGrade.Elite,
                requirePoiseBreakForState = true,
                allowForceReaction = true,
                allowHit = true,
                allowStun = true,
                allowKnockdown = true,
                allowAirborne = false,
                allowGrab = false,
            });
            EditorUtility.SetDirty(policy);
        }

        private static void ConfigureBossReactionPolicy(CombatReactionPolicySO policy)
        {
            if (policy == null)
                return;

            policy.monsterGradeRules.Clear();
            policy.monsterGradeRules.Add(new CombatReactionPolicySO.GradeRule
            {
                grade = MonsterActorGrade.Boss,
                requirePoiseBreakForState = true,
                allowForceReaction = false,
                allowHit = false,
                allowStun = true,
                allowKnockdown = false,
                allowAirborne = false,
                allowGrab = false,
            });
            EditorUtility.SetDirty(policy);
        }

        private static int AssignDefaultPolicies(
            CombatDefensePolicySO defensePolicy,
            CombatReactionPolicySO eliteReactionPolicy,
            CombatReactionPolicySO bossReactionPolicy)
        {
            int assignedCount = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var actor = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                if (actor == null)
                    continue;

                bool changed = false;
                // DefensePolicy는 플레이어블 캐릭터 정의에만 적용한다(characterType != None, 또는 Player 플래그).
                // 플레이어는 이들 정의 중 하나로 조작되며, 순수 적(characterType==None)은 DefenseResolver가
                // 읽지 않으므로 정책을 붙이지 않는다. (recruitableAs는 현재 데이터에서 전부 미채움이라 기준에서 제외)
                if (defensePolicy != null
                    && actor.combatDefensePolicy == null
                    && IsPlayableCharacter(actor))
                {
                    actor.combatDefensePolicy = defensePolicy;
                    changed = true;
                }

                if (actor.combatReactionPolicy == null)
                {
                    CombatReactionPolicySO reactionPolicy = actor.grade switch
                    {
                        MonsterActorGrade.Elite => eliteReactionPolicy,
                        MonsterActorGrade.Boss => bossReactionPolicy,
                        _ => null,
                    };

                    if (reactionPolicy != null && HasActorType(actor, ActorType.Monster))
                    {
                        actor.combatReactionPolicy = reactionPolicy;
                        changed = true;
                    }
                }

                if (!changed)
                    continue;

                EditorUtility.SetDirty(actor);
                assignedCount++;
            }

            return assignedCount;
        }

        private static bool HasActorType(ActorDefinitionSO actor, ActorType actorType)
            => (actor.actorType & actorType) == actorType;

        /// <summary>플레이어블 캐릭터 정의 = Player 플래그 또는 characterType 지정(영입/조작 대상).</summary>
        private static bool IsPlayableCharacter(ActorDefinitionSO actor)
            => HasActorType(actor, ActorType.Player) || actor.characterType != CharacterActorType.None;
    }
}
#endif
