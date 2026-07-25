using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;

namespace UPlayGround.Data.Editor.Ability.Production
{
    [Serializable]
    public sealed class AbilitySetCompositionRequest
    {
        public AbilitySetSO BaseSet;
        public List<GameplayAbilitySO> AddedAbilities = new();
        public List<AbilitySetSO.AbilityOverrideEntry> Overrides = new();
        public string AssetName = "AbilitySet_Common";
        public string SaveRoot = "Assets/10.Datas/Ability/Sets";
        public MonsterActorProfileSO TargetMonsterProfile;
        public ActorDefinitionSO TargetActorDefinition;
    }

    public sealed class AbilitySetCompositionPlan
    {
        private readonly List<AbilityProductionIssue> _issues = new();

        public AbilitySetCompositionRequest Request { get; internal set; }
        public string AssetPath { get; internal set; }
        public string Signature { get; internal set; }
        public IReadOnlyList<AbilityProductionIssue> Issues => _issues;
        public bool IsDerived => Request?.BaseSet != null;
        public bool CanApply
        {
            get
            {
                if (string.IsNullOrWhiteSpace(AssetPath))
                    return false;
                for (int i = 0; i < _issues.Count; i++)
                    if (_issues[i].Severity == AbilityProductionSeverity.Error)
                        return false;
                return true;
            }
        }

        internal void AddIssue(
            string code,
            AbilityProductionSeverity severity,
            string message,
            UnityEngine.Object context = null)
        {
            _issues.Add(new AbilityProductionIssue(
                code,
                severity,
                message,
                context));
        }
    }

    public sealed class AbilitySetCompositionResult
    {
        public bool Success { get; internal set; }
        public string Message { get; internal set; }
        public AbilitySetSO Set { get; internal set; }
    }

    public static class AbilitySetCompositionService
    {
        public static AbilitySetCompositionPlan Build(
            AbilitySetCompositionRequest request)
        {
            var plan = new AbilitySetCompositionPlan { Request = request };
            if (request == null)
            {
                plan.AddIssue(
                    "SET.REQUEST",
                    AbilityProductionSeverity.Error,
                    "Set 구성 요청이 없습니다.");
                return plan;
            }

            string name = request.AssetName?.Trim();
            string root = request.SaveRoot?.Trim()?.Replace('\\', '/');
            if (string.IsNullOrWhiteSpace(name)
                || name.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0
                || name.Contains('/'))
            {
                plan.AddIssue(
                    "SET.ASSET_NAME",
                    AbilityProductionSeverity.Error,
                    "유효한 Set 에셋 이름을 입력하세요.");
            }
            if (string.IsNullOrWhiteSpace(root)
                || !root.StartsWith("Assets/", StringComparison.Ordinal))
            {
                plan.AddIssue(
                    "SET.SAVE_ROOT",
                    AbilityProductionSeverity.Error,
                    "저장 루트는 Assets/ 아래 경로여야 합니다.");
            }
            if (!string.IsNullOrWhiteSpace(name)
                && !string.IsNullOrWhiteSpace(root))
            {
                plan.AssetPath = $"{root.TrimEnd('/')}/{name}.asset";
                if (AssetDatabase.LoadMainAssetAtPath(plan.AssetPath) != null)
                {
                    plan.AddIssue(
                        "SET.PATH_CONFLICT",
                        AbilityProductionSeverity.Error,
                        $"이미 에셋이 존재합니다: {plan.AssetPath}",
                        AssetDatabase.LoadMainAssetAtPath(plan.AssetPath));
                }
            }

            var added = new HashSet<GameplayAbilitySO>();
            for (int i = 0; i < (request.AddedAbilities?.Count ?? 0); i++)
            {
                GameplayAbilitySO ability = request.AddedAbilities[i];
                if (ability == null)
                {
                    plan.AddIssue(
                        "SET.ADD_NULL",
                        AbilityProductionSeverity.Error,
                        $"추가 Ability {i}번이 비어 있습니다.");
                }
                else if (!added.Add(ability))
                {
                    plan.AddIssue(
                        "SET.ADD_DUPLICATE",
                        AbilityProductionSeverity.Error,
                        $"추가 Ability '{ability.name}'이 중복되었습니다.",
                        ability);
                }
                else if (request.BaseSet?.Contains(ability) == true)
                {
                    plan.AddIssue(
                        "SET.ADD_INHERITED",
                        AbilityProductionSeverity.Warning,
                        $"'{ability.name}'은 Base Set에 이미 포함되어 추가하지 않아도 됩니다.",
                        ability);
                }
            }

            var sources = new HashSet<GameplayAbilitySO>();
            for (int i = 0; i < (request.Overrides?.Count ?? 0); i++)
            {
                AbilitySetSO.AbilityOverrideEntry entry = request.Overrides[i];
                if (request.BaseSet == null)
                {
                    plan.AddIssue(
                        "SET.OVERRIDE_WITHOUT_BASE",
                        AbilityProductionSeverity.Error,
                        "독립 공용 Set에는 Replace/Remove를 선언할 수 없습니다.");
                    break;
                }
                if (entry == null || entry.sourceAbility == null)
                {
                    plan.AddIssue(
                        "SET.OVERRIDE_SOURCE",
                        AbilityProductionSeverity.Error,
                        $"Override {i}의 원본 Ability가 없습니다.");
                    continue;
                }
                if (!sources.Add(entry.sourceAbility))
                {
                    plan.AddIssue(
                        "SET.OVERRIDE_DUPLICATE",
                        AbilityProductionSeverity.Error,
                        $"'{entry.sourceAbility.name}' 원본 Override가 중복되었습니다.",
                        entry.sourceAbility);
                }
                if (!request.BaseSet.Contains(entry.sourceAbility))
                {
                    plan.AddIssue(
                        "SET.OVERRIDE_NOT_IN_BASE",
                        AbilityProductionSeverity.Error,
                        $"'{entry.sourceAbility.name}'은 Base Set의 유효 Ability가 아닙니다.",
                        entry.sourceAbility);
                }
                if (entry.operation == AbilitySetOverrideOperation.Replace
                    && entry.replacementAbility == null)
                {
                    plan.AddIssue(
                        "SET.REPLACEMENT",
                        AbilityProductionSeverity.Error,
                        $"'{entry.sourceAbility.name}'의 교체 Ability가 없습니다.",
                        entry.sourceAbility);
                }
            }

            if (request.BaseSet == null
                && (request.AddedAbilities?.Count ?? 0) == 0)
            {
                plan.AddIssue(
                    "SET.EMPTY_COMMON",
                    AbilityProductionSeverity.Warning,
                    "공용 Set에 추가할 Ability가 없습니다.");
            }
            if (request.BaseSet != null
                && (request.AddedAbilities?.Count ?? 0) == 0
                && (request.Overrides?.Count ?? 0) == 0)
            {
                plan.AddIssue(
                    "SET.EMPTY_DERIVED",
                    AbilityProductionSeverity.Warning,
                    "Override가 없는 파생 Set은 Base Set과 동일하게 동작합니다.");
            }

            if (request.TargetMonsterProfile != null
                && request.BaseSet != null)
            {
                plan.AddIssue(
                    "SET.PROFILE_DERIVED",
                    AbilityProductionSeverity.Error,
                    "MonsterProfile에는 공용 독립 Set만 연결할 수 있습니다. "
                    + "파생 Set은 특수 ActorDefinition에 연결하세요.",
                    request.TargetMonsterProfile);
            }
            AbilitySetSO definitionShared =
                request.TargetActorDefinition?.monsterProfile?.abilitySet;
            if (request.TargetActorDefinition != null
                && definitionShared != null
                && request.BaseSet != definitionShared)
            {
                plan.AddIssue(
                    "SET.PROFILE_BASE_MISMATCH",
                    AbilityProductionSeverity.Error,
                    "ActorDefinition의 MonsterProfile 공용 Set이 "
                    + "지정한 Base Set과 다릅니다.",
                    request.TargetActorDefinition);
            }
            plan.Signature = BuildSignature(request, plan.AssetPath);
            return plan;
        }

        public static AbilitySetCompositionResult Apply(
            AbilitySetCompositionPlan plan)
        {
            if (plan?.CanApply != true)
                return Failure("오류가 없는 Set 구성 Preview가 필요합니다.");
            AbilitySetCompositionPlan latest = Build(plan.Request);
            if (!latest.CanApply
                || !string.Equals(
                    latest.AssetPath,
                    plan.AssetPath,
                    StringComparison.Ordinal)
                || !string.Equals(
                    latest.Signature,
                    plan.Signature,
                    StringComparison.Ordinal))
            {
                return Failure(
                    "Preview 이후 경로나 참조 조건이 변경되었습니다. 다시 확인하세요.");
            }

            int undoGroup = Undo.GetCurrentGroup();
            Undo.IncrementCurrentGroup();
            undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName($"AbilitySet 구성: {plan.Request.AssetName}");
            AbilitySetSO created = null;
            var createdFolders = new List<string>();
            try
            {
                EnsureFolder(
                    System.IO.Path.GetDirectoryName(plan.AssetPath)
                        ?.Replace('\\', '/'),
                    createdFolders);
                created = ScriptableObject.CreateInstance<AbilitySetSO>();
                created.name = plan.Request.AssetName.Trim();
                created.baseSet = plan.Request.BaseSet;
                created.additionalAbilities = new List<GameplayAbilitySO>();
                var inherited = new HashSet<GameplayAbilitySO>(
                    plan.Request.BaseSet?.EnumerateAll()
                    ?? Array.Empty<GameplayAbilitySO>());
                for (int i = 0;
                     i < (plan.Request.AddedAbilities?.Count ?? 0);
                     i++)
                {
                    GameplayAbilitySO ability =
                        plan.Request.AddedAbilities[i];
                    if (ability != null && !inherited.Contains(ability))
                        created.additionalAbilities.Add(ability);
                }
                created.abilityOverrides =
                    CloneOverrides(plan.Request.Overrides);
                Undo.RegisterCreatedObjectUndo(created, "AbilitySet 생성");
                AssetDatabase.CreateAsset(created, plan.AssetPath);

                if (plan.Request.TargetMonsterProfile != null)
                {
                    Undo.RecordObject(
                        plan.Request.TargetMonsterProfile,
                        "MonsterProfile 공용 AbilitySet 연결");
                    plan.Request.TargetMonsterProfile.abilitySet = created;
                    EditorUtility.SetDirty(plan.Request.TargetMonsterProfile);
                }
                if (plan.Request.TargetActorDefinition != null)
                {
                    Undo.RecordObject(
                        plan.Request.TargetActorDefinition,
                        "ActorDefinition 파생 AbilitySet 연결");
                    plan.Request.TargetActorDefinition.abilitySet = created;
                    EditorUtility.SetDirty(plan.Request.TargetActorDefinition);
                }

                EditorUtility.SetDirty(created);
                AssetDatabase.SaveAssets();
                List<AbilityValidationIssue> validation =
                    AbilityDataValidator.Validate(created);
                for (int i = 0; i < validation.Count; i++)
                    if (validation[i].Severity
                        == AbilityValidationSeverity.Error)
                        throw new InvalidOperationException(
                            validation[i].Message);

                Undo.CollapseUndoOperations(undoGroup);
                Selection.activeObject = created;
                EditorGUIUtility.PingObject(created);
                return new AbilitySetCompositionResult
                {
                    Success = true,
                    Set = created,
                    Message = plan.IsDerived
                        ? "파생 AbilitySet과 Override 연결을 완료했습니다."
                        : "공용 AbilitySet 구성을 완료했습니다.",
                };
            }
            catch (Exception exception)
            {
                try
                {
                    Undo.RevertAllDownToGroup(undoGroup);
                }
                catch (Exception undoException)
                {
                    Debug.LogException(undoException);
                }
                if (AssetDatabase.LoadMainAssetAtPath(plan.AssetPath) != null)
                    AssetDatabase.DeleteAsset(plan.AssetPath);
                for (int i = createdFolders.Count - 1; i >= 0; i--)
                {
                    if (!AssetDatabase.IsValidFolder(createdFolders[i]))
                        continue;
                    string[] contents = AssetDatabase.FindAssets(
                        string.Empty,
                        new[] { createdFolders[i] });
                    if (contents.Length == 0)
                        AssetDatabase.DeleteAsset(createdFolders[i]);
                }
                AssetDatabase.SaveAssets();
                return Failure($"AbilitySet 구성 실패: {exception.Message}");
            }
        }

        private static List<AbilitySetSO.AbilityOverrideEntry> CloneOverrides(
            List<AbilitySetSO.AbilityOverrideEntry> source)
        {
            var result = new List<AbilitySetSO.AbilityOverrideEntry>();
            for (int i = 0; i < (source?.Count ?? 0); i++)
            {
                AbilitySetSO.AbilityOverrideEntry entry = source[i];
                if (entry == null) continue;
                result.Add(new AbilitySetSO.AbilityOverrideEntry
                {
                    sourceAbility = entry.sourceAbility,
                    operation = entry.operation,
                    replacementAbility = entry.replacementAbility,
                });
            }
            return result;
        }

        private static void EnsureFolder(
            string folder,
            List<string> createdFolders)
        {
            if (string.IsNullOrWhiteSpace(folder))
                throw new InvalidOperationException("저장 폴더가 없습니다.");
            if (AssetDatabase.IsValidFolder(folder))
                return;
            string parent = System.IO.Path.GetDirectoryName(folder)
                ?.Replace('\\', '/');
            string name = System.IO.Path.GetFileName(folder);
            EnsureFolder(parent, createdFolders);
            if (string.IsNullOrWhiteSpace(
                    AssetDatabase.CreateFolder(parent, name)))
                throw new InvalidOperationException(
                    $"폴더를 만들지 못했습니다: {folder}");
            createdFolders.Add(folder);
        }

        private static string BuildSignature(
            AbilitySetCompositionRequest request,
            string assetPath)
        {
            var parts = new List<string>
            {
                assetPath ?? string.Empty,
                AssetKey(request.BaseSet),
                AssetKey(request.TargetMonsterProfile),
                AssetKey(request.TargetActorDefinition),
            };
            for (int i = 0; i < (request.AddedAbilities?.Count ?? 0); i++)
                parts.Add($"A:{AssetKey(request.AddedAbilities[i])}");
            for (int i = 0; i < (request.Overrides?.Count ?? 0); i++)
            {
                AbilitySetSO.AbilityOverrideEntry entry = request.Overrides[i];
                parts.Add(
                    entry == null
                        ? "O:null"
                        : $"O:{AssetKey(entry.sourceAbility)}:"
                          + $"{entry.operation}:"
                          + $"{AssetKey(entry.replacementAbility)}");
            }
            return string.Join("|", parts);
        }

        private static string AssetKey(UnityEngine.Object asset)
        {
            if (asset == null)
                return "null";
            string path = AssetDatabase.GetAssetPath(asset);
            return string.IsNullOrWhiteSpace(path)
                ? $"instance:{asset.GetInstanceID()}"
                : AssetDatabase.AssetPathToGUID(path);
        }

        private static AbilitySetCompositionResult Failure(string message) =>
            new()
            {
                Success = false,
                Message = message,
            };
    }
}
