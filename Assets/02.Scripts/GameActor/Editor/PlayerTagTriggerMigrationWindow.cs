#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UPlayGround.Data.Ability;
using UPlayGround.Gameplay.Tag;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.AI.BehaviorTree.Editor
{
    /// <summary>플레이어 피격 Request Ability를 생성하고 모든 플레이어 Set에 연결한다.</summary>
    public sealed class PlayerTagTriggerMigrationWindow : EditorWindow
    {
        private const int MigrationVersion = 1;
        private const string SetRoot = "Assets/10.Datas/Ability/Migrated";
        private const string AssetRoot =
            "Assets/10.Datas/Ability/Migrated/TagTriggers";

        private string _report = "드라이런을 실행하세요.";
        private bool _canApply;
        private Vector2 _scroll;

        private readonly struct Definition
        {
            public Definition(string id, GameplayTag tag)
            {
                Id = id;
                Tag = tag;
            }

            public string Id { get; }
            public GameplayTag Tag { get; }
            public string Path => $"{AssetRoot}/{Id}.asset";
        }

        [MenuItem("UPlayGround/Ability/플레이어 피격 태그 트리거 마이그레이션")]
        private static void Open() =>
            GetWindow<PlayerTagTriggerMigrationWindow>(
                "Player Hit Tag Trigger Migration");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "입력 슬롯은 건드리지 않고 피격 Request Ability만 additionalAbilities에 연결합니다. "
                + "드라이런이 성공해야 적용할 수 있습니다.",
                MessageType.Info);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("드라이런", GUILayout.Height(28f)))
                    RunDryRun();
                using (new EditorGUI.DisabledScope(!_canApply))
                    if (GUILayout.Button("적용", GUILayout.Height(28f)))
                        ApplyMigration();
            }
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_report, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void RunDryRun()
        {
            try
            {
                List<AbilitySetSO> sets = LoadSets();
                List<Definition> definitions = BuildDefinitions();
                int createCount = 0;
                int linkCount = 0;
                foreach (Definition definition in definitions)
                {
                    UnityEngine.Object raw =
                        AssetDatabase.LoadMainAssetAtPath(definition.Path);
                    GameplayAbilitySO existing = raw as GameplayAbilitySO;
                    if (raw != null && existing == null)
                        throw new InvalidDataException(
                            $"고정 경로에 다른 타입이 있습니다: {definition.Path}");
                    if (existing == null)
                        createCount++;
                    else
                        ValidateExisting(existing, definition);
                    for (int i = 0; i < sets.Count; i++)
                        if (existing == null || !sets[i].Contains(existing))
                            linkCount++;
                }

                _report = "[Player Hit Tag Trigger Dry Run]\n"
                    + $"대상 AbilitySet: {sets.Count}\n"
                    + $"신규 Ability: {createCount}\n"
                    + $"AbilitySet 연결: {linkCount}\n"
                    + "입력 슬롯 변경: 0\n결과: 적용 가능";
                _canApply = true;
            }
            catch (Exception exception)
            {
                _report = $"드라이런 실패\n{exception}";
                _canApply = false;
            }
        }

        private void ApplyMigration()
        {
            if (!_canApply)
                return;
            int undoGroup = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("플레이어 피격 태그 트리거 마이그레이션");
            var createdPaths = new List<string>();
            bool createdFolder = false;
            try
            {
                createdFolder = EnsureAssetRoot();
                List<Definition> definitions = BuildDefinitions();
                var abilities = definitions
                    .Select(definition => GetOrCreate(definition, createdPaths))
                    .ToList();
                List<AbilitySetSO> sets = LoadSets();
                for (int i = 0; i < sets.Count; i++)
                {
                    AbilitySetSO set = sets[i];
                    Undo.RecordObject(set, "플레이어 피격 Ability 연결");
                    for (int j = 0; j < abilities.Count; j++)
                        if (!set.Contains(abilities[j]))
                            set.additionalAbilities.Add(abilities[j]);
                    set.tagTriggerMigrationVersion = MigrationVersion;
                    set.RebuildRuntimeIndex();
                    EditorUtility.SetDirty(set);
                }
                AssetDatabase.SaveAssets();
                Undo.CollapseUndoOperations(undoGroup);
                _canApply = false;
                _report += $"\n적용 완료: Ability {abilities.Count}, AbilitySet {sets.Count}";
            }
            catch (Exception exception)
            {
                Undo.RevertAllDownToGroup(undoGroup);
                for (int i = createdPaths.Count - 1; i >= 0; i--)
                    if (AssetDatabase.LoadMainAssetAtPath(createdPaths[i]) != null)
                        AssetDatabase.DeleteAsset(createdPaths[i]);
                if (createdFolder && AssetDatabase.IsValidFolder(AssetRoot))
                    AssetDatabase.DeleteAsset(AssetRoot);
                AssetDatabase.SaveAssets();
                _canApply = false;
                _report = $"적용 실패 — Undo 그룹 전체를 롤백했습니다.\n{exception}";
            }
        }

        private static List<AbilitySetSO> LoadSets()
        {
            List<AbilitySetSO> sets = AssetDatabase
                .FindAssets("t:AbilitySetSO", new[] { SetRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !path.StartsWith(AssetRoot, StringComparison.Ordinal))
                .Select(AssetDatabase.LoadAssetAtPath<AbilitySetSO>)
                .Where(set => set != null)
                .OrderBy(set => AssetDatabase.GetAssetPath(set), StringComparer.Ordinal)
                .ToList();
            if (sets.Count == 0)
                throw new InvalidDataException("플레이어 AbilitySet을 찾지 못했습니다.");
            return sets;
        }

        private static List<Definition> BuildDefinitions() => new()
        {
            new("GA_Player_Hit_Light", GameplayTags.Trigger_Player_Hit_Light),
            new("GA_Player_Hit_Hit", GameplayTags.Trigger_Player_Hit_Hit),
            new("GA_Player_Hit_Heavy", GameplayTags.Trigger_Player_Hit_Heavy),
            new("GA_Player_Hit_KnockBack", GameplayTags.Trigger_Player_Hit_KnockBack),
            new("GA_Player_Hit_Stun", GameplayTags.Trigger_Player_Hit_Stun),
            new("GA_Player_Hit_Pull", GameplayTags.Trigger_Player_Hit_Pull),
            new("GA_Player_Hit_Airborne", GameplayTags.Trigger_Player_Hit_Airborne),
            new("GA_Player_Hit_Knockdown", GameplayTags.Trigger_Player_Hit_Knockdown),
            new("GA_Player_Hit_Grab", GameplayTags.Trigger_Player_Hit_Grab),
        };

        private static GameplayAbilitySO GetOrCreate(
            Definition definition,
            List<string> createdPaths)
        {
            GameplayAbilitySO ability =
                AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(definition.Path);
            if (ability != null)
            {
                ValidateExisting(ability, definition);
                return ability;
            }
            if (AssetDatabase.LoadMainAssetAtPath(definition.Path) != null)
                throw new InvalidDataException(
                    $"고정 경로에 다른 타입이 있습니다: {definition.Path}");

            ability = ScriptableObject.CreateInstance<GameplayAbilitySO>();
            ability.name = definition.Id;
            ability.abilityId = definition.Id;
            ability.editorMemo = "태그 트리거 마이그레이션: 플레이어 피격 리액션";
            ability.concurrency = AbilityConcurrencyPolicy.CancelExisting;
            ability.variants.Add(new AbilityVariantDefinition
            {
                variantId = "Default",
                priority = 1,
            });
            ability.triggers.Add(new AbilityTriggerDefinition
            {
                triggerTag = definition.Tag,
                source = AbilityTriggerSource.GameplayEvent,
                mode = AbilityTriggerActivationMode.Request,
                matchMode = AbilityTagMatchMode.Exact,
                allowPreemption = true,
            });
            ability.activation.ownerTagRequirement.blockAny.AddRange(new[]
            {
                GameplayTags.State_Hit,
                GameplayTags.State_Stun,
                GameplayTags.State_Knockdown,
                GameplayTags.State_Grabbed,
                GameplayTags.State_Death,
                GameplayTags.State_SuperArmor,
            });
            Undo.RegisterCreatedObjectUndo(ability, "플레이어 피격 Ability 생성");
            AssetDatabase.CreateAsset(ability, definition.Path);
            createdPaths.Add(definition.Path);
            return ability;
        }

        private static void ValidateExisting(
            GameplayAbilitySO ability,
            Definition definition)
        {
            if (!string.Equals(ability.abilityId, definition.Id, StringComparison.Ordinal)
                || ability.triggers == null
                || !ability.triggers.Any(trigger =>
                    trigger != null
                    && trigger.triggerTag == definition.Tag
                    && trigger.source == AbilityTriggerSource.GameplayEvent
                    && trigger.mode == AbilityTriggerActivationMode.Request
                    && trigger.matchMode == AbilityTagMatchMode.Exact
                    && trigger.allowPreemption))
                throw new InvalidDataException(
                    $"기존 에셋 정의가 다릅니다. 자동 덮어쓰지 않습니다: {definition.Path}");
        }

        private static bool EnsureAssetRoot()
        {
            if (AssetDatabase.IsValidFolder(AssetRoot))
                return false;
            string guid = AssetDatabase.CreateFolder(SetRoot, "TagTriggers");
            if (string.IsNullOrWhiteSpace(guid))
                throw new IOException($"폴더를 만들지 못했습니다: {AssetRoot}");
            return true;
        }
    }
}
#endif
