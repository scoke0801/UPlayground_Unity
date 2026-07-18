using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using IOPath = System.IO.Path;

namespace UPlayGround.Data.Editor.Ability
{
    public enum AbilityMigrationPlanStatus
    {
        Ready,
        NoConvertibleData,
        InvalidSource,
        Conflict,
    }

    [Serializable]
    public sealed class AbilityBatchMigrationOptions
    {
        public string sourceRoot = "Assets/10.Datas/Actor/Player/AttackData";
        public string outputRoot = "Assets/10.Datas/Ability/Migrated";
        public float abilityCost;
        public float ultimateCost = 100f;
        public float abilityCooldown = 3f;
        public float ultimateCooldown = 12f;
        public bool includeLegacyFallback = true;
    }

    public sealed class AbilityMigrationSlotPlan
    {
        public PlayerSkillSlot Slot { get; set; }
        public string AbilityId { get; set; }
        public string AssetPath { get; set; }
        public int VariantCount { get; set; }
        public bool UsesLegacyFallback { get; set; }
    }

    public sealed class AbilityMigrationPlanEntry
    {
        public PlayerAttackDataSO Source { get; set; }
        public string SourcePath { get; set; }
        public string OutputFolder { get; set; }
        public string SetPath { get; set; }
        public AbilityMigrationPlanStatus Status { get; set; }
        public string Message { get; set; }
        public bool Selected { get; set; }
        public List<AbilityMigrationSlotPlan> Slots { get; } = new();

        public int VariantCount => Slots.Sum(slot => slot.VariantCount);
    }

    public sealed class AbilityBatchMigrationResult
    {
        public int ConvertedSources;
        public int CreatedAbilities;
        public int CreatedPayloads;
        public int SkippedSources;
        public readonly List<string> Messages = new();
        public string ReportPath;
    }

    /// <summary>
    /// PlayerAttackDataSO를 신규 Ability 에셋으로 비파괴 변환한다.
    /// 계획 생성은 읽기 전용이며 Execute를 명시적으로 호출하기 전에는 에셋을 만들거나 수정하지 않는다.
    /// </summary>
    public static class AbilityBatchMigrationService
    {
        public static List<AbilityMigrationPlanEntry> BuildPlan(
            AbilityBatchMigrationOptions options)
        {
            ValidateOptions(options);

            var entries = new List<AbilityMigrationPlanEntry>();
            var occupiedIds = LoadExistingAbilityIds();
            var plannedIds = new HashSet<string>(StringComparer.Ordinal);
            var plannedPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            string[] guids = AssetDatabase.FindAssets(
                "t:PlayerAttackDataSO",
                new[] { NormalizeAssetPath(options.sourceRoot) });

            Array.Sort(guids, StringComparer.Ordinal);
            for (int i = 0; i < guids.Length; i++)
            {
                string sourcePath = AssetDatabase.GUIDToAssetPath(guids[i]);
                PlayerAttackDataSO source =
                    AssetDatabase.LoadAssetAtPath<PlayerAttackDataSO>(sourcePath);
                if (source == null)
                    continue;

                AbilityMigrationPlanEntry entry = BuildEntry(source, sourcePath, options);
                CheckConflicts(entry, occupiedIds, plannedIds, plannedPaths);
                entry.Selected = entry.Status == AbilityMigrationPlanStatus.Ready;
                entries.Add(entry);
            }

            return entries;
        }

        public static AbilityBatchMigrationResult Execute(
            IReadOnlyList<AbilityMigrationPlanEntry> plan,
            AbilityBatchMigrationOptions options)
        {
            ValidateOptions(options);
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var result = new AbilityBatchMigrationResult();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Ability 데이터 일괄 마이그레이션");

            try
            {
                for (int i = 0; i < plan.Count; i++)
                {
                    AbilityMigrationPlanEntry entry = plan[i];
                    if (entry == null
                        || !entry.Selected
                        || entry.Status != AbilityMigrationPlanStatus.Ready)
                    {
                        result.SkippedSources++;
                        continue;
                    }

                    EditorUtility.DisplayProgressBar(
                        "Ability 데이터 일괄 마이그레이션",
                        entry.SourcePath,
                        plan.Count == 0 ? 1f : (float)i / plan.Count);

                    if (HasRuntimeConflict(entry))
                    {
                        result.SkippedSources++;
                        result.Messages.Add(
                            $"[건너뜀] {entry.SourcePath}: 미리보기 후 출력 경로에 에셋이 생성되었습니다.");
                        continue;
                    }

                    ConvertEntry(entry, options, result);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                result.ReportPath = WriteReport(plan, options, result);
                return result;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Undo.CollapseUndoOperations(undoGroup);
            }
        }

        private static AbilityMigrationPlanEntry BuildEntry(
            PlayerAttackDataSO source,
            string sourcePath,
            AbilityBatchMigrationOptions options)
        {
            string safeName = SanitizeFileName(source.name);
            string outputFolder =
                $"{NormalizeAssetPath(options.outputRoot)}/{safeName}";
            var entry = new AbilityMigrationPlanEntry
            {
                Source = source,
                SourcePath = sourcePath,
                OutputFolder = outputFolder,
                SetPath = $"{outputFolder}/AbilitySet_{safeName}.asset",
                Status = AbilityMigrationPlanStatus.Ready,
                Message = "변환 준비 완료",
            };

            var definitions = new Dictionary<PlayerSkillSlot, PlayerSkillDefinition>();
            if (source.skillDefinitions != null)
            {
                for (int i = 0; i < source.skillDefinitions.Count; i++)
                {
                    PlayerSkillDefinition definition = source.skillDefinitions[i];
                    if (definition == null)
                        continue;
                    if (!definitions.TryAdd(definition.slot, definition))
                    {
                        entry.Status = AbilityMigrationPlanStatus.InvalidSource;
                        entry.Message = $"'{definition.slot}' 스킬 정의가 중복되어 있습니다.";
                        return entry;
                    }
                }
            }

            foreach (PlayerSkillSlot slot in Enum.GetValues(typeof(PlayerSkillSlot)))
            {
                bool fallback = false;
                int variantCount;
                if (definitions.TryGetValue(slot, out PlayerSkillDefinition definition))
                {
                    variantCount = CountExecutableVariants(definition);
                    if (variantCount == 0)
                    {
                        entry.Status = AbilityMigrationPlanStatus.InvalidSource;
                        entry.Message = $"'{slot}' 정의에 실행 가능한 Variant가 없습니다.";
                        return entry;
                    }
                }
                else
                {
                    if (!options.includeLegacyFallback
                        || !TryGetLegacyAttack(source, slot, out _))
                        continue;
                    fallback = true;
                    variantCount = 1;
                }

                string abilityId =
                    $"Ability.Player.{SanitizeId(source.name)}.{slot}";
                entry.Slots.Add(new AbilityMigrationSlotPlan
                {
                    Slot = slot,
                    AbilityId = abilityId,
                    AssetPath = $"{outputFolder}/GA_{safeName}_{slot}.asset",
                    VariantCount = variantCount,
                    UsesLegacyFallback = fallback,
                });
            }

            if (entry.Slots.Count == 0)
            {
                entry.Status = AbilityMigrationPlanStatus.NoConvertibleData;
                entry.Message = "스킬 정의와 실행 가능한 레거시 스킬 데이터가 없습니다.";
            }
            else
            {
                int fallbackCount = entry.Slots.Count(slot => slot.UsesLegacyFallback);
                entry.Message =
                    $"Ability {entry.Slots.Count}개, Variant {entry.VariantCount}개"
                    + (fallbackCount > 0 ? $", 레거시 폴백 {fallbackCount}개" : "");
            }

            return entry;
        }

        private static void CheckConflicts(
            AbilityMigrationPlanEntry entry,
            HashSet<string> occupiedIds,
            HashSet<string> plannedIds,
            HashSet<string> plannedPaths)
        {
            if (entry.Status != AbilityMigrationPlanStatus.Ready)
                return;

            var conflicts = new List<string>();
            if (AssetDatabase.LoadMainAssetAtPath(entry.SetPath) != null
                || !plannedPaths.Add(entry.SetPath))
                conflicts.Add($"출력 경로: {entry.SetPath}");

            for (int i = 0; i < entry.Slots.Count; i++)
            {
                AbilityMigrationSlotPlan slot = entry.Slots[i];
                if (AssetDatabase.LoadMainAssetAtPath(slot.AssetPath) != null
                    || !plannedPaths.Add(slot.AssetPath))
                    conflicts.Add($"출력 경로: {slot.AssetPath}");
                if (occupiedIds.Contains(slot.AbilityId)
                    || !plannedIds.Add(slot.AbilityId))
                    conflicts.Add($"Ability ID: {slot.AbilityId}");
            }

            if (conflicts.Count == 0)
                return;
            entry.Status = AbilityMigrationPlanStatus.Conflict;
            entry.Message = string.Join(" / ", conflicts);
        }

        private static void ConvertEntry(
            AbilityMigrationPlanEntry entry,
            AbilityBatchMigrationOptions options,
            AbilityBatchMigrationResult result)
        {
            EnsureFolder(entry.OutputFolder);
            var createdPaths = new List<string>();
            int createdAbilities = 0;
            int createdPayloads = 0;
            try
            {
                var abilities = new Dictionary<PlayerSkillSlot, GameplayAbilitySO>();
                for (int i = 0; i < entry.Slots.Count; i++)
                {
                    AbilityMigrationSlotPlan slotPlan = entry.Slots[i];
                    PlayerSkillDefinition definition =
                        FindDefinition(entry.Source, slotPlan.Slot);
                    GameplayAbilitySO ability = CreateAbility(
                        entry.Source,
                        definition,
                        slotPlan,
                        options,
                        out int payloadCount);

                    AssetDatabase.CreateAsset(ability, slotPlan.AssetPath);
                    createdPaths.Add(slotPlan.AssetPath);
                    Undo.RegisterCreatedObjectUndo(ability, "Gameplay Ability 생성");
                    AddPayloadSubAssets(ability);
                    EditorUtility.SetDirty(ability);
                    abilities.Add(slotPlan.Slot, ability);
                    createdAbilities++;
                    createdPayloads += payloadCount;
                }

                var set = ScriptableObject.CreateInstance<AbilitySetSO>();
                set.name = IOPath.GetFileNameWithoutExtension(entry.SetPath);
                foreach (AbilityMigrationSlotPlan slot in entry.Slots)
                {
                    set.playerSlots.Add(new AbilitySetSO.PlayerSlotEntry
                    {
                        slot = slot.Slot,
                        ability = abilities[slot.Slot],
                    });
                }

                AssetDatabase.CreateAsset(set, entry.SetPath);
                createdPaths.Add(entry.SetPath);
                Undo.RegisterCreatedObjectUndo(set, "Ability Set 생성");
                EditorUtility.SetDirty(set);
                result.ConvertedSources++;
                result.CreatedAbilities += createdAbilities;
                result.CreatedPayloads += createdPayloads;
                result.Messages.Add(
                    $"[완료] {entry.SourcePath} -> {entry.OutputFolder} "
                    + $"(Ability {entry.Slots.Count}, Payload {entry.VariantCount})");
            }
            catch (Exception exception)
            {
                for (int i = createdPaths.Count - 1; i >= 0; i--)
                    AssetDatabase.DeleteAsset(createdPaths[i]);
                result.SkippedSources++;
                result.Messages.Add(
                    $"[실패/롤백] {entry.SourcePath}: {exception.Message}");
                Debug.LogException(exception, entry.Source);
            }
        }

        private static GameplayAbilitySO CreateAbility(
            PlayerAttackDataSO source,
            PlayerSkillDefinition definition,
            AbilityMigrationSlotPlan slotPlan,
            AbilityBatchMigrationOptions options,
            out int payloadCount)
        {
            var ability = ScriptableObject.CreateInstance<GameplayAbilitySO>();
            ability.name = IOPath.GetFileNameWithoutExtension(slotPlan.AssetPath);
            ability.abilityId = slotPlan.AbilityId;
            ability.presentation.displayName = definition?.displayName
                ?? $"{source.name} {slotPlan.Slot}";
            ability.presentation.category =
                slotPlan.Slot == PlayerSkillSlot.Ultimate
                    ? AbilityCategory.Ultimate
                    : AbilityCategory.Attack;
            ability.activation.targetRelation = AbilityTargetRelation.Enemy;
            ApplyCostAndCooldown(ability, definition, slotPlan.Slot, options);

            if (definition != null)
            {
                for (int i = 0; i < definition.variants.Count; i++)
                {
                    PlayerSkillVariant legacy = definition.variants[i];
                    if (legacy == null || !legacy.IsExecutable)
                        continue;
                    ability.variants.Add(ConvertVariant(legacy, i));
                }
            }
            else if (TryGetLegacyAttack(source, slotPlan.Slot, out PlayerAttackInfo attack))
            {
                ability.variants.Add(new AbilityVariantDefinition
                {
                    variantId = "Legacy",
                    priority = 0,
                    animKey = attack.baseInfo.animKey,
                    playerAttackInfo = Clone(attack),
                    condition = new AbilityVariantCondition
                    {
                        requiresFullResource =
                            slotPlan.Slot == PlayerSkillSlot.Ultimate,
                    },
                });
            }

            payloadCount = ability.variants.Count;
            return ability;
        }

        private static void AddPayloadSubAssets(GameplayAbilitySO ability)
        {
            for (int i = 0; i < ability.variants.Count; i++)
            {
                AbilityVariantDefinition variant = ability.variants[i];
                var payload =
                    ScriptableObject.CreateInstance<UPlayGroundMotionAbilityPayloadSO>();
                payload.name =
                    $"{ability.name}_{SanitizeFileName(variant.variantId)}_Payload";
                payload.executionId =
                    $"{ability.abilityId}.{SanitizeId(variant.variantId)}";
                payload.animKey = variant.ResolveLegacyAnimKey();
                payload.playerAttackInfo = Clone(variant.playerAttackInfo);
                AssetDatabase.AddObjectToAsset(payload, ability);
                Undo.RegisterCreatedObjectUndo(payload, "Ability 실행 Payload 생성");
                variant.executionPayload = payload;
                EditorUtility.SetDirty(payload);
            }
        }

        private static AbilityVariantDefinition ConvertVariant(
            PlayerSkillVariant legacy,
            int index)
        {
            return new AbilityVariantDefinition
            {
                variantId = string.IsNullOrWhiteSpace(legacy.variantName)
                    ? $"Variant_{index}"
                    : legacy.variantName.Trim(),
                priority = legacy.priority,
                animKey = legacy.ResolveAnimKey(),
                playerAttackInfo = Clone(legacy.attackInfo),
                condition = new AbilityVariantCondition
                {
                    groundCondition = legacy.condition?.groundCondition switch
                    {
                        SkillGroundCondition.Grounded => AbilityGroundCondition.Grounded,
                        SkillGroundCondition.Airborne => AbilityGroundCondition.Airborne,
                        _ => AbilityGroundCondition.Any,
                    },
                    minResource = legacy.condition?.minSkillGauge ?? 0f,
                    requiresFullResource =
                        legacy.condition?.requiresFullSkillGauge ?? false,
                    requiredTagIds = legacy.condition?.requiredTagIds != null
                        ? new List<Gameplay.Tag.GameplayTagId>(
                            legacy.condition.requiredTagIds)
                        : new List<Gameplay.Tag.GameplayTagId>(),
                    blockedTagIds = legacy.condition?.blockedTagIds != null
                        ? new List<Gameplay.Tag.GameplayTagId>(
                            legacy.condition.blockedTagIds)
                        : new List<Gameplay.Tag.GameplayTagId>(),
                },
            };
        }

        private static void ApplyCostAndCooldown(
            GameplayAbilitySO ability,
            PlayerSkillDefinition definition,
            PlayerSkillSlot slot,
            AbilityBatchMigrationOptions options)
        {
            float cost =
                slot == PlayerSkillSlot.Ultimate
                    ? options.ultimateCost
                    : options.abilityCost;
            bool usesCost =
                definition?.costPolicy == SkillCostPolicy.UseGaugeSlot
                || definition == null;
            if (usesCost && cost > 0f)
            {
                ability.cost.resourceType = AbilityResourceType.UltimateEnergy;
                ability.cost.policy = AbilityCostPolicy.Fixed;
                ability.cost.value = cost;
            }

            bool usesCooldown =
                definition?.cooldownPolicy != SkillCooldownPolicy.NoCooldown;
            ability.cooldown.durationSeconds = usesCooldown
                ? slot == PlayerSkillSlot.Ultimate
                    ? options.ultimateCooldown
                    : options.abilityCooldown
                : 0f;
            ability.cooldown.cooldownGroupId =
                $"Cooldown.Player.{SanitizeId(ability.name)}";
        }

        private static bool HasRuntimeConflict(AbilityMigrationPlanEntry entry)
        {
            if (AssetDatabase.LoadMainAssetAtPath(entry.SetPath) != null)
                return true;
            HashSet<string> existingIds = LoadExistingAbilityIds();
            for (int i = 0; i < entry.Slots.Count; i++)
                if (AssetDatabase.LoadMainAssetAtPath(entry.Slots[i].AssetPath) != null
                    || existingIds.Contains(entry.Slots[i].AbilityId))
                    return true;
            return false;
        }

        private static int CountExecutableVariants(PlayerSkillDefinition definition)
        {
            if (definition?.variants == null)
                return 0;
            int count = 0;
            for (int i = 0; i < definition.variants.Count; i++)
                if (definition.variants[i]?.IsExecutable == true)
                    count++;
            return count;
        }

        private static PlayerSkillDefinition FindDefinition(
            PlayerAttackDataSO source,
            PlayerSkillSlot slot)
        {
            if (source?.skillDefinitions == null)
                return null;
            for (int i = 0; i < source.skillDefinitions.Count; i++)
                if (source.skillDefinitions[i]?.slot == slot)
                    return source.skillDefinitions[i];
            return null;
        }

        private static bool TryGetLegacyAttack(
            PlayerAttackDataSO source,
            PlayerSkillSlot slot,
            out PlayerAttackInfo attack)
        {
            attack = null;
            int index = (int)slot;
            if (source?.skillAttackList == null
                || index < 0
                || index >= source.skillAttackList.Count)
                return false;
            attack = source.skillAttackList[index];
            return attack?.baseInfo != null
                   && attack.baseInfo.animKey != AnimKey.None;
        }

        private static PlayerAttackInfo Clone(PlayerAttackInfo source)
        {
            return source == null
                ? new PlayerAttackInfo()
                : JsonUtility.FromJson<PlayerAttackInfo>(JsonUtility.ToJson(source));
        }

        private static HashSet<string> LoadExistingAbilityIds()
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            string[] guids = AssetDatabase.FindAssets("t:GameplayAbilitySO");
            for (int i = 0; i < guids.Length; i++)
            {
                GameplayAbilitySO ability =
                    AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(
                        AssetDatabase.GUIDToAssetPath(guids[i]));
                if (!string.IsNullOrWhiteSpace(ability?.abilityId))
                    ids.Add(ability.abilityId.Trim());
            }
            return ids;
        }

        private static string WriteReport(
            IReadOnlyList<AbilityMigrationPlanEntry> plan,
            AbilityBatchMigrationOptions options,
            AbilityBatchMigrationResult result)
        {
            Directory.CreateDirectory("Temp");
            string path =
                $"Temp/AbilityMigration-{DateTime.Now:yyyyMMdd-HHmmss}.txt";
            var report = new StringBuilder();
            report.AppendLine("Gameplay Ability 일괄 마이그레이션 결과");
            report.AppendLine($"실행 시각: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            report.AppendLine($"원본 루트: {options.sourceRoot}");
            report.AppendLine($"출력 루트: {options.outputRoot}");
            report.AppendLine(
                $"비용 Ability/Ultimate: {options.abilityCost}/{options.ultimateCost}");
            report.AppendLine(
                $"쿨다운 Ability/Ultimate: {options.abilityCooldown}/{options.ultimateCooldown}");
            report.AppendLine(
                $"완료 소스: {result.ConvertedSources}, Ability: {result.CreatedAbilities}, "
                + $"Payload: {result.CreatedPayloads}, 건너뜀: {result.SkippedSources}");
            report.AppendLine();
            report.AppendLine("원본 PlayerAttackDataSO는 수정하지 않았습니다.");
            report.AppendLine("출력 에셋 연결 전 비용/쿨다운 및 Variant 비교 검토가 필요합니다.");
            report.AppendLine();
            report.AppendLine("[변환 계획]");
            for (int i = 0; i < plan.Count; i++)
            {
                AbilityMigrationPlanEntry entry = plan[i];
                report.AppendLine(
                    $"- {entry.SourcePath} | {entry.Status} | 선택={entry.Selected} | "
                    + entry.Message);
                for (int j = 0; j < entry.Slots.Count; j++)
                {
                    AbilityMigrationSlotPlan slot = entry.Slots[j];
                    report.AppendLine(
                        $"  · {slot.Slot}: {slot.AbilityId}, Variant {slot.VariantCount}, "
                        + $"레거시 폴백={slot.UsesLegacyFallback}, {slot.AssetPath}");
                }
            }
            report.AppendLine();
            report.AppendLine("[실행 결과]");
            for (int i = 0; i < result.Messages.Count; i++)
                report.AppendLine(result.Messages[i]);
            File.WriteAllText(path, report.ToString(), Encoding.UTF8);
            return path;
        }

        private static void ValidateOptions(AbilityBatchMigrationOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));
            options.sourceRoot = NormalizeAssetPath(options.sourceRoot);
            options.outputRoot = NormalizeAssetPath(options.outputRoot);
            if (!AssetDatabase.IsValidFolder(options.sourceRoot))
                throw new ArgumentException(
                    $"원본 폴더가 존재하지 않습니다: {options.sourceRoot}");
            if (!options.outputRoot.StartsWith("Assets/", StringComparison.Ordinal)
                && options.outputRoot != "Assets")
                throw new ArgumentException("출력 폴더는 Assets 내부여야 합니다.");
            if (options.abilityCost < 0f
                || options.ultimateCost < 0f
                || options.abilityCooldown < 0f
                || options.ultimateCooldown < 0f)
                throw new ArgumentException("비용과 쿨다운은 음수일 수 없습니다.");
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = NormalizeAssetPath(folder).Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string NormalizeAssetPath(string path) =>
            string.IsNullOrWhiteSpace(path)
                ? string.Empty
                : path.Replace('\\', '/').TrimEnd('/');

        private static string SanitizeFileName(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unnamed";
            char[] invalid = IOPath.GetInvalidFileNameChars();
            var builder = new StringBuilder(value.Length);
            foreach (char character in value.Trim())
                builder.Append(invalid.Contains(character) || character == '/'
                    ? '_'
                    : character);
            return builder.ToString();
        }

        private static string SanitizeId(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "Unnamed";
            var builder = new StringBuilder(value.Length);
            foreach (char character in value.Trim())
                builder.Append(char.IsLetterOrDigit(character) || character == '_'
                    ? character
                    : '_');
            return builder.ToString();
        }
    }
}
