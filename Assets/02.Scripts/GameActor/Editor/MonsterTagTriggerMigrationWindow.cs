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
    /// <summary>몬스터 공격/피격 태그 트리거 Ability를 안전하게 생성·연결한다.</summary>
    public sealed class MonsterTagTriggerMigrationWindow : EditorWindow
    {
        private const int MigrationVersion = 1;
        private const string SetRoot = "Assets/10.Datas/Ability/Actor";
        private const string AssetRoot =
            "Assets/10.Datas/Ability/Actor/TagTriggers";
        private const string SourceJsonRoot =
            "Assets/10.Datas/AI/BehaviorTree/SourceJson";
        private const string RawJsonRoot =
            "Assets/10.Datas/AI/BehaviorTree/Json";
        private const string BehaviorTreeRoot =
            "Assets/10.Datas/AI/BehaviorTree";
        private const string BackupRoot =
            "Assets/10.Datas/_MigrationBackup";

        private Vector2 _scroll;
        private string _report = "드라이런을 실행하세요.";
        private bool _canApply;

        private readonly struct Definition
        {
            public Definition(string id, GameplayTag tag, bool reaction)
            {
                Id = id;
                Tag = tag;
                Reaction = reaction;
            }

            public string Id { get; }
            public GameplayTag Tag { get; }
            public bool Reaction { get; }
            public string Path => $"{AssetRoot}/{Id}.asset";
        }

        [Serializable]
        private sealed class RulesSourceIdentity
        {
            public string id;
        }

        [MenuItem("UPlayGround/Ability/몬스터 태그 트리거 마이그레이션")]
        private static void Open() =>
            GetWindow<MonsterTagTriggerMigrationWindow>(
                "Monster Tag Trigger Migration");

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "먼저 드라이런으로 고정 경로와 대상 AbilitySet을 검증합니다. "
                + "적용은 새 에셋 생성과 AbilitySet 연결만 수행하며 기존 에셋을 삭제하지 않습니다.",
                MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("M5 드라이런", GUILayout.Height(28f)))
                    RunDryRun();
                using (new EditorGUI.DisabledScope(!_canApply))
                    if (GUILayout.Button("M2·M3 적용", GUILayout.Height(28f)))
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
                List<AbilitySetSO> sets = LoadMonsterSets();
                List<Definition> definitions = BuildDefinitions();
                var lines = new List<string>
                {
                    "[Monster Tag Trigger Migration Dry Run]",
                    $"대상 AbilitySet: {sets.Count}",
                    $"트리거 Ability 정의: {definitions.Count} (공격 3 + 피격 9)",
                };

                int createCount = 0;
                int linkCount = 0;
                foreach (Definition definition in definitions)
                {
                    GameplayAbilitySO existing =
                        AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(
                            definition.Path);
                    UnityEngine.Object raw =
                        AssetDatabase.LoadMainAssetAtPath(definition.Path);
                    if (raw != null && existing == null)
                        throw new InvalidDataException(
                            $"고정 경로에 다른 타입의 에셋이 있습니다: {definition.Path}");
                    if (existing == null)
                        createCount++;
                    else
                        ValidateExisting(existing, definition);

                    for (int i = 0; i < sets.Count; i++)
                        if (!sets[i].Contains(existing)
                            || existing == null)
                            linkCount++;
                }

                lines.Add($"신규 생성 예정: {createCount}");
                lines.Add($"AbilitySet 연결 예정: {linkCount}");
                AppendBehaviorTreeReport(lines);
                lines.Add("M4: 기존 ExecuteAttack/RequestAction 런타임 노드가 동일 트리거 요청기를 사용하므로 JSON 의미 구조는 보존합니다.");
                lines.Add("결과: 적용 가능");
                _report = string.Join("\n", lines);
                _canApply = true;
            }
            catch (Exception exception)
            {
                _report = $"드라이런 실패\n{exception}";
                _canApply = false;
            }
        }

        private static void AppendBehaviorTreeReport(List<string> lines)
        {
            var sourcePathsByTreeName = new Dictionary<string, string>(
                StringComparer.Ordinal);
            var rulesPaths = LoadJsonPaths(SourceJsonRoot);
            var rawPaths = LoadJsonPaths(RawJsonRoot);

            for (int i = 0; i < rulesPaths.Count; i++)
            {
                string json = File.ReadAllText(rulesPaths[i]);
                RulesSourceIdentity identity =
                    JsonUtility.FromJson<RulesSourceIdentity>(json);
                if (identity == null || string.IsNullOrWhiteSpace(identity.id))
                    throw new InvalidDataException(
                        $"Rules JSON id가 없습니다: {rulesPaths[i]}");

                AddSourceMapping(
                    sourcePathsByTreeName,
                    $"BT_{identity.id}",
                    rulesPaths[i]);
            }

            for (int i = 0; i < rawPaths.Count; i++)
            {
                AddSourceMapping(
                    sourcePathsByTreeName,
                    Path.GetFileNameWithoutExtension(rawPaths[i]),
                    rawPaths[i]);
            }

            List<BehaviorTreeAsset> trees = AssetDatabase
                .FindAssets("t:BehaviorTreeAsset", new[] { BehaviorTreeRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>)
                .Where(tree => tree != null)
                .OrderBy(tree => AssetDatabase.GetAssetPath(tree), StringComparer.Ordinal)
                .ToList();
            var treeNames = new HashSet<string>(
                trees.Select(tree => tree.name),
                StringComparer.Ordinal);
            List<BehaviorTreeAsset> manualTrees = trees
                .Where(tree => !sourcePathsByTreeName.ContainsKey(tree.name))
                .ToList();
            List<KeyValuePair<string, string>> orphanSources = sourcePathsByTreeName
                .Where(pair => !treeNames.Contains(pair.Key))
                .OrderBy(pair => pair.Value, StringComparer.Ordinal)
                .ToList();

            int rulesAttackCount = rulesPaths.Count(ContainsAttackRequest);
            int rawAttackCount = rawPaths.Count(ContainsAttackRequest);
            lines.Add(
                $"공격 SourceJson(M4 검토 대상): "
                + $"{rulesAttackCount + rawAttackCount} "
                + $"(Rules {rulesAttackCount} + Raw {rawAttackCount})");
            lines.Add(
                $"BT/JSON 대응: BT {trees.Count}, JSON "
                + $"{sourcePathsByTreeName.Count}, 대응 에셋 없음 {orphanSources.Count}");
            lines.Add($"수동 처리 대상(소스 JSON 없음): {manualTrees.Count}");
            for (int i = 0; i < manualTrees.Count; i++)
            {
                BehaviorTreeAsset tree = manualTrees[i];
                int attackNodeCount = tree.Nodes.Count(node =>
                    node is ExecuteEnemyAttackNode
                    || node is RequestEnemyActionNode);
                lines.Add(
                    $"- {AssetDatabase.GetAssetPath(tree)} "
                    + $"(공격 요청 노드 {attackNodeCount})");
            }

            if (orphanSources.Count > 0)
            {
                lines.Add($"대응 에셋 없는 JSON: {orphanSources.Count}");
                for (int i = 0; i < orphanSources.Count; i++)
                    lines.Add($"- {orphanSources[i].Value}");
            }
        }

        private static List<string> LoadJsonPaths(string root)
        {
            if (!Directory.Exists(root))
                return new List<string>();

            return Directory.GetFiles(root, "*.json", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.Ordinal)
                .ToList();
        }

        private static void AddSourceMapping(
            Dictionary<string, string> sourcePathsByTreeName,
            string treeName,
            string sourcePath)
        {
            if (!sourcePathsByTreeName.TryAdd(treeName, sourcePath))
                throw new InvalidDataException(
                    $"같은 BT를 가리키는 JSON이 중복되었습니다: "
                    + $"{sourcePathsByTreeName[treeName]}, {sourcePath}");
        }

        private static bool ContainsAttackRequest(string path)
        {
            string json = File.ReadAllText(path);
            return json.Contains("\"action\": \"ExecuteAttack\"")
                || json.Contains("\"action\": \"RequestAction\"")
                || json.Contains("ExecuteEnemyAttackNode")
                || json.Contains("RequestEnemyActionNode");
        }

        private void ApplyMigration()
        {
            if (!_canApply)
                return;

            int undoGroup = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("몬스터 태그 트리거 마이그레이션");
            var createdPaths = new List<string>();
            bool createdFolder = false;
            try
            {
                createdFolder = EnsureAssetRoot();
                List<Definition> definitions = BuildDefinitions();
                List<AbilitySetSO> sets = LoadMonsterSets();
                string backupPath = CreateMigrationBackup(sets, definitions);
                var abilities = new List<GameplayAbilitySO>(definitions.Count);
                for (int i = 0; i < definitions.Count; i++)
                    abilities.Add(GetOrCreate(definitions[i], createdPaths));

                for (int i = 0; i < sets.Count; i++)
                {
                    AbilitySetSO set = sets[i];
                    Undo.RecordObject(set, "태그 트리거 Ability 연결");
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
                _report += $"\n적용 완료: Ability {abilities.Count}, AbilitySet {sets.Count}"
                           + $"\n백업: {backupPath}";
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

        private static string CreateMigrationBackup(
            IReadOnlyList<AbilitySetSO> sets,
            IReadOnlyList<Definition> definitions)
        {
            string sessionRoot = $"{BackupRoot}/AbilityTagTrigger_"
                                 + DateTime.Now.ToString("yyyyMMdd_HHmmss_fff");
            var sourcePaths = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < sets.Count; i++)
            {
                string path = AssetDatabase.GetAssetPath(sets[i]);
                if (!string.IsNullOrWhiteSpace(path))
                    sourcePaths.Add(path);
            }
            for (int i = 0; i < definitions.Count; i++)
                if (File.Exists(definitions[i].Path))
                    sourcePaths.Add(definitions[i].Path);

            foreach (string sourcePath in sourcePaths.OrderBy(
                         path => path,
                         StringComparer.Ordinal))
            {
                string relativePath = sourcePath.StartsWith(
                    "Assets/",
                    StringComparison.Ordinal)
                    ? sourcePath.Substring("Assets/".Length)
                    : Path.GetFileName(sourcePath);
                string backupPath = Path.Combine(
                    sessionRoot,
                    relativePath + ".backup");
                string directory = Path.GetDirectoryName(backupPath);
                if (string.IsNullOrWhiteSpace(directory))
                    throw new IOException($"백업 경로를 만들 수 없습니다: {backupPath}");
                Directory.CreateDirectory(directory);
                File.Copy(sourcePath, backupPath, overwrite: false);

                string metaPath = sourcePath + ".meta";
                if (File.Exists(metaPath))
                    File.Copy(metaPath, backupPath + ".meta.backup", overwrite: false);
            }

            return sessionRoot;
        }

        private static List<AbilitySetSO> LoadMonsterSets()
        {
            var sets = AssetDatabase.FindAssets("t:AbilitySetSO", new[] { SetRoot })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !path.StartsWith(AssetRoot, StringComparison.Ordinal))
                .Select(AssetDatabase.LoadAssetAtPath<AbilitySetSO>)
                .Where(set => set != null)
                .OrderBy(set => AssetDatabase.GetAssetPath(set), StringComparer.Ordinal)
                .ToList();
            if (sets.Count == 0)
                throw new InvalidDataException("몬스터 AbilitySet을 찾지 못했습니다.");
            return sets;
        }

        private static List<Definition> BuildDefinitions() => new()
        {
            new("GA_Monster_Attack_Basic_Trigger", GameplayTags.Trigger_Monster_Attack_Basic, false),
            new("GA_Monster_Attack_Heavy_Trigger", GameplayTags.Trigger_Monster_Attack_Heavy, false),
            new("GA_Monster_Attack_Skill_Trigger", GameplayTags.Trigger_Monster_Attack_Skill, false),
            new("GA_Monster_Hit_Light", GameplayTags.Trigger_Monster_Hit_Light, true),
            new("GA_Monster_Hit_Hit", GameplayTags.Trigger_Monster_Hit_Hit, true),
            new("GA_Monster_Hit_Heavy", GameplayTags.Trigger_Monster_Hit_Heavy, true),
            new("GA_Monster_Hit_KnockBack", GameplayTags.Trigger_Monster_Hit_KnockBack, true),
            new("GA_Monster_Hit_Stun", GameplayTags.Trigger_Monster_Hit_Stun, true),
            new("GA_Monster_Hit_Pull", GameplayTags.Trigger_Monster_Hit_Pull, true),
            new("GA_Monster_Hit_Airborne", GameplayTags.Trigger_Monster_Hit_Airborne, true),
            new("GA_Monster_Hit_Knockdown", GameplayTags.Trigger_Monster_Hit_Knockdown, true),
            new("GA_Monster_Hit_Grab", GameplayTags.Trigger_Monster_Hit_Grab, true),
        };

        private static GameplayAbilitySO GetOrCreate(
            Definition definition,
            List<string> createdPaths)
        {
            GameplayAbilitySO existing =
                AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(definition.Path);
            if (existing != null)
            {
                ValidateExisting(existing, definition);
                return existing;
            }
            if (AssetDatabase.LoadMainAssetAtPath(definition.Path) != null)
                throw new InvalidDataException(
                    $"고정 경로에 다른 타입의 에셋이 있습니다: {definition.Path}");

            GameplayAbilitySO ability =
                ScriptableObject.CreateInstance<GameplayAbilitySO>();
            ability.name = definition.Id;
            ability.abilityId = definition.Id;
            ability.editorMemo = definition.Reaction
                ? "태그 트리거 마이그레이션: 몬스터 피격 리액션 라우터"
                : "태그 트리거 마이그레이션: 몬스터 공격 카테고리 라우터";
            ability.concurrency = definition.Reaction
                ? AbilityConcurrencyPolicy.CancelExisting
                : AbilityConcurrencyPolicy.RejectNew;
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
                allowPreemption = definition.Reaction,
            });
            if (definition.Reaction)
            {
                ability.activation.ownerTagRequirement.blockAny.AddRange(new[]
                {
                    GameplayTags.State_Hit,
                    GameplayTags.State_Stun,
                    GameplayTags.State_Knockdown,
                    GameplayTags.State_Grabbed,
                    GameplayTags.State_Death,
                    GameplayTags.State_SpecialBreakVictim,
                });
            }

            Undo.RegisterCreatedObjectUndo(ability, "태그 트리거 Ability 생성");
            AssetDatabase.CreateAsset(ability, definition.Path);
            createdPaths.Add(definition.Path);
            return ability;
        }

        private static void ValidateExisting(
            GameplayAbilitySO ability,
            Definition definition)
        {
            if (!string.Equals(
                    ability.abilityId,
                    definition.Id,
                    StringComparison.Ordinal)
                || ability.triggers == null
                || !ability.triggers.Any(trigger =>
                    trigger != null
                    && trigger.triggerTag == definition.Tag
                    && trigger.source == AbilityTriggerSource.GameplayEvent
                    && trigger.mode == AbilityTriggerActivationMode.Request
                    && trigger.matchMode == AbilityTagMatchMode.Exact
                    && trigger.allowPreemption == definition.Reaction))
                throw new InvalidDataException(
                    $"기존 에셋이 예상 정의와 다릅니다. 자동 덮어쓰지 않습니다: {definition.Path}");
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
