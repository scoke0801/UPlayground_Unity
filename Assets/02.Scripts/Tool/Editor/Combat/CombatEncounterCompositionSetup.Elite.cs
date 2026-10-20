using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Path;
using UPlayGround.Gameplay.Tag;

namespace UPlayGround.Tool.Editor.Combat
{
    public static partial class CombatEncounterCompositionSetup
    {
        /// <summary>신규 전용 에셋만 생성하고 기존 세트에는 중복 없이 참조 하나를 추가한다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/게임플레이/전투/정책/정예 브레이크 충격파 적용")]
        public static void ApplyEliteShockwave()
        {
            const string folder = "Assets/10.Datas/Combat/Encounter";
            const string output = "output/combat-encounter-composition";
            Directory.CreateDirectory(folder);
            Directory.CreateDirectory(output);
            AssetDatabase.Refresh();
            var ability = AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(folder + "/GA_EliteBreakShockwave.asset");
            if (ability == null)
            {
                foreach (string name in new[] { "EliteBreakShockwavePayload", "EliteBreakShockwaveEffect", "EliteBreakShake", "GA_EliteBreakShockwave" })
                    if (File.Exists(folder + "/" + name + ".asset"))
                        throw new InvalidOperationException("부분 생성 또는 기존 에셋을 먼저 검토하세요: " + name);
                var payload = ScriptableObject.CreateInstance<EliteBreakShockwavePayloadSO>();
                var effect = ScriptableObject.CreateInstance<EliteBreakShockwaveEffectSO>();
                var shake = ScriptableObject.CreateInstance<ShakeCameraEffectData>();
                ability = ScriptableObject.CreateInstance<GameplayAbilitySO>();
                shake.effectKey = "EliteBreakShockwave";
                shake.shakeDataKey = CameraShakeIdType.HeavyHit.ToKey();
                shake.duration = 0.3f;
                effect.cameraEffect = shake;
                effect.visualEffectKey = FXKeyType.GriffinDiveImpact.ToKey();
                payload.presentation = effect;
                ability.abilityId = "Monster.EliteBreak.Shockwave";
                ability.editorMemo = "정예 특수 브레이크 마무리: 반경 4m Weak·Normal 다운. Request 전용.";
                ability.concurrency = AbilityConcurrencyPolicy.Background;
                ability.persistence.backgroundMaxDurationSeconds = 1f;
                ability.triggers.Add(new AbilityTriggerDefinition
                {
                    triggerTag = GameplayTags.Trigger_Monster_EliteBreak, source = AbilityTriggerSource.GameplayEvent,
                    mode = AbilityTriggerActivationMode.Request, retriggerIntervalSeconds = 0.1f,
                });
                ability.variants.Add(new AbilityVariantDefinition { variantId = "Shockwave", executionPayload = payload });
                AssetDatabase.CreateAsset(shake, folder + "/EliteBreakShake.asset");
                AssetDatabase.CreateAsset(effect, folder + "/EliteBreakShockwaveEffect.asset");
                AssetDatabase.CreateAsset(payload, folder + "/EliteBreakShockwavePayload.asset");
                AssetDatabase.CreateAsset(ability, folder + "/GA_EliteBreakShockwave.asset");
            }
            if (ability.abilityId != "Monster.EliteBreak.Shockwave" || ability.variants.Count != 1
                || ability.variants[0].executionPayload is not EliteBreakShockwavePayloadSO
                || ability.triggers.Count != 1 || ability.triggers[0].triggerTag != GameplayTags.Trigger_Monster_EliteBreak)
                throw new InvalidOperationException("기존 충격파 에셋의 계약이 다릅니다. 자동 덮어쓰기는 하지 않습니다.");
            foreach (var issue in UPlayGround.Data.Editor.Ability.AbilityDataValidator.Validate(ability))
                if (issue.Severity == UPlayGround.Data.Editor.Ability.AbilityValidationSeverity.Error)
                    throw new InvalidOperationException(issue.Message);
            var sets = new HashSet<AbilitySetSO>();
            foreach (string guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
            {
                var actor = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                if ((actor.actorType & ActorType.Monster) != 0 && actor.EffectiveGrade == MonsterActorGrade.Elite
                    && actor.EffectiveAbilitySet != null) sets.Add(actor.EffectiveAbilitySet);
            }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            var report = new List<string> { "AbilitySet,Added" };
            try
            {
                foreach (var set in sets)
                {
                    bool contains = false;
                    foreach (var existing in set.GetRuntimeAbilities()) if (existing == ability) contains = true;
                    if (contains)
                    {
                        report.Add(AssetDatabase.GetAssetPath(set) + ",False" );
                        continue;
                    }
                    string path = AssetDatabase.GetAssetPath(set);
                    string backup = output + "/p5-set-" + AssetDatabase.AssetPathToGUID(path) + ".asset.txt";
                    if (!File.Exists(backup)) File.Copy(path, backup);
                    Undo.RecordObject(set, "정예 브레이크 참조 추가");
                    set.additionalAbilities.Add(ability);
                    EditorUtility.SetDirty(set);
                    report.Add(path + ",True");
                }
                foreach (var set in sets) AssetDatabase.SaveAssetIfDirty(set);
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (var set in sets) { EditorUtility.SetDirty(set); AssetDatabase.SaveAssetIfDirty(set); }
                throw;
            }
            File.WriteAllLines(output + "/p5-ability-bindings.csv", report);
            var issues = UPlayGround.Data.Editor.Ability.AbilityDataValidator.ValidateAll();
            var audit = new List<string> { "Severity\tAsset\tMessage" };
            foreach (var issue in issues)
                audit.Add(issue.Severity + "\t" + AssetDatabase.GetAssetPath(issue.Context) + "\t" + issue.Message);
            File.WriteAllLines(output + "/p5-ability-audit.tsv", audit);

        }
    }
}
