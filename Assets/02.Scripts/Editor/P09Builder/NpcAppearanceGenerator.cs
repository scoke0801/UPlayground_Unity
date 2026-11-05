using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using P09.Modular.Humanoid.Data;
using UnityEditor;
using UnityEngine;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>허용 목록을 전수 검증한 뒤 결정적인 NPC 프리셋을 생성한다.</summary>
    public static class NpcAppearanceGenerator
    {
        public const string Root = "Assets/10.Datas/Actor/GeneratedNpcAppearances";
        public const string RulesPath = Root + "/NpcAppearanceSet.asset";
        public const string PresetFolder = Root + "/Presets";
        public const string PrefabRoot = "Assets/03.Prefabs/Characters/GeneratedNpcAppearances";
        private const string OwnershipLabel = "P09.GeneratedNpcAppearance";

        /// <summary>기본 허용 목록으로 프리셋을 생성한다. 런처에는 노출하지 않는다.</summary>
        [UPlaygroundTool("p09.npc-appearance.generate")]
        public static void GenerateDefault()
        {
            Generate(AssetDatabase.LoadAssetAtPath<NpcAppearanceSetSO>(RulesPath));
        }

        /// <summary>쓰기 전에 모든 규칙·설정·출력 충돌을 검사하고 프리셋을 제자리 갱신한다.</summary>
        public static IReadOnlyList<BuildPreset> Generate(NpcAppearanceSetSO source)
            => Generate(source, PresetFolder, null);

        // 실패 주입은 실제 쓰기 경계의 롤백을 검증하기 위한 테스트 전용 진입점이다.
        internal static IReadOnlyList<BuildPreset> Generate(NpcAppearanceSetSO source, string folder, Action<int> afterWrite)
        {
            var configs = CreateConfigs(source);
            ValidateOutput(configs, folder);
            var createdPaths = new List<string>();
            var createdFolders = new List<string>();
            var backups = new BuildContext(null);
            var results = new List<BuildPreset>();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("NPC 외형 프리셋 생성");
            try
            {
                EnsureFolders(folder, createdFolders);
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (var config in configs)
                    {
                        var preset = WritePreset(config, source.description, folder, createdPaths, backups);
                        results.Add(preset);
                        afterWrite?.Invoke(results.Count);
                    }
                }
                finally { AssetDatabase.StopAssetEditing(); }
                Undo.FlushUndoRecordObjects();
                // 신규 SO가 임포트되어 GUID로 저장 가능해진 뒤 저장 알림도 한 번으로 묶는다.
                AssetDatabase.StartAssetEditing();
                try
                {
                    foreach (string path in createdPaths)
                        AssetDatabase.SetLabels(AssetDatabase.LoadAssetAtPath<BuildPreset>(path), new[] { OwnershipLabel });
                    foreach (var preset in results) AssetDatabase.SaveAssetIfDirty(preset);
                }
                finally { AssetDatabase.StopAssetEditing(); }
                Undo.CollapseUndoOperations(group);
                return results;
            }
            catch
            {
                AssetDatabase.StartAssetEditing();
                try
                {
                    Undo.RevertAllDownToGroup(group);
                    backups.RestoreStagedAssetBackups();
                    foreach (var preset in results)
                        if (preset != null && AssetDatabase.Contains(preset)) AssetDatabase.SaveAssetIfDirty(preset);
                    foreach (string path in createdPaths) AssetDatabase.DeleteAsset(path);
                    for (int i = createdFolders.Count - 1; i >= 0; i--)
                        AssetDatabase.DeleteAsset(createdFolders[i]);
                }
                finally { AssetDatabase.StopAssetEditing(); }
                throw;
            }
            finally { backups.DiscardStagedAssetBackups(); }
        }

        private static BuildPreset WritePreset(CharacterBuildConfig config, string description, string folder,
            List<string> createdPaths, BuildContext backups)
        {
            string path = GetPresetPath(folder, config);
            var preset = AssetDatabase.LoadAssetAtPath<BuildPreset>(path);
            if (preset == null)
            {
                preset = ScriptableObject.CreateInstance<BuildPreset>();
                createdPaths.Add(path);
                AssetDatabase.CreateAsset(preset, path);
                Undo.RegisterCreatedObjectUndo(preset, "NPC 외형 프리셋 추가");
            }
            else
            {
                backups.StageAssetForUpdate(preset);
                Undo.RegisterCompleteObjectUndo(preset, "NPC 외형 프리셋 갱신");
            }
            preset.config = config;
            preset.description = description;
            EditorUtility.SetDirty(preset);
            return preset;
        }

        /// <summary>쓰기 없이 288개를 전개하고 모든 설정 오류를 모아서 보고한다.</summary>
        public static List<CharacterBuildConfig> CreateConfigs(NpcAppearanceSetSO source)
        {
            var errors = ValidateRules(source);
            var result = new List<CharacterBuildConfig>();
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            var signatures = new HashSet<string>();
            foreach (var rule in source.rules.OrderBy(r => r.setId).ThenBy(r => r.sex))
            {
                foreach (var config in ExpandRule(source, rule))
                {
                    errors.AddRange(config.Validate().Select(error => config.ManualName + ": " + error));
                    if (!signatures.Add(GetAppearanceSignature(config)))
                        errors.Add(config.ManualName + ": 동일 외형 조합이 중복됩니다.");
                    result.Add(config);
                }
            }
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
            return result;
        }

        private static IEnumerable<CharacterBuildConfig> ExpandRule(NpcAppearanceSetSO source, NpcAppearanceRule rule)
        {
            int sequence = 0;
            var faces = rule.sex == BuilderSex.Male ? source.maleFaces : source.femaleFaces;
            foreach (var style in rule.hairStyles)
            foreach (var color in rule.colors)
            foreach (var face in faces)
                yield return CreateConfig(source, rule, style, color, face, ++sequence);
        }

        /// <summary>이름과 스탯을 제외한 실제 외형 조합의 식별자를 반환한다.</summary>
        public static string GetAppearanceSignature(CharacterBuildConfig config)
        {
            var parts = new List<int> { (int)config.Sex };
            foreach (BuilderArmorSlot slot in Enum.GetValues(typeof(BuilderArmorSlot)))
                parts.Add(ReadId(config.ArmorSelections.Get(slot)));
            parts.AddRange(new[] { ReadId(config.HairStyleSo), ReadId(config.HairColorSo),
                ReadId(config.FaceTypeSo), ReadId(config.SkinColorSo), ReadId(config.EyeColorSo),
                config.FacialHairId, ReadId(config.BustSizeSo) });
            return string.Join("/", parts);
        }

        private static CharacterBuildConfig CreateConfig(NpcAppearanceSetSO source, NpcAppearanceRule rule,
            RendererEditPartData style, NpcHairEyePair color, NpcFaceSkinPair face, int sequence)
        {
            var config = new CharacterBuildConfig
            {
                ActorKind = BuilderActorKind.Npc, Sex = rule.sex,
                HairStyleSo = style, HairColorSo = color.hair, EyeColorSo = color.eye,
                FaceTypeSo = face.face, SkinColorSo = face.skin,
                FacialHairSo = rule.facialHair, FacialHairId = rule.facialHairId,
                BustSizeSo = rule.bustSize, UseMagicaCloth = source.useMagicaCloth,
                UseManualName = true,
                ManualName = $"NPC_Gen_S{rule.setId:00}_{(rule.sex == BuilderSex.Male ? "M" : "F")}_{sequence:00}",
                SaveBaseFolder = PrefabRoot,
                Stats = JsonUtility.FromJson<StatsAssignment>(JsonUtility.ToJson(source.npcStats))
            };
            config.ArmorSelections.Set(BuilderArmorSlot.Arm, rule.arm);
            config.ArmorSelections.Set(BuilderArmorSlot.Chest, rule.chest);
            config.ArmorSelections.Set(BuilderArmorSlot.Waist, rule.waist);
            config.ArmorSelections.Set(BuilderArmorSlot.Leg, rule.leg);
            return config;
        }

        private static List<string> ValidateRules(NpcAppearanceSetSO source)
        {
            var errors = new List<string>();
            if (source == null) { errors.Add("NPC 외형 허용 목록이 없습니다."); return errors; }
            if (source.npcStats == null) errors.Add("공통 NPC 데이터 설정이 없습니다.");
            var catalog = new P09AssetCatalog();
            catalog.Refresh();
            ValidateFaces(source.maleFaces, BuilderSex.Male, catalog, errors);
            ValidateFaces(source.femaleFaces, BuilderSex.Female, catalog, errors);
            if (source.rules == null || source.rules.Length != 24)
            { errors.Add("12세트 × 성별 2개의 규칙이 필요합니다."); return errors; }
            var keys = new HashSet<string>();
            foreach (var rule in source.rules)
            {
                if (rule == null) { errors.Add("빈 규칙이 있습니다."); continue; }
                string key = $"S{rule.setId:00}/{rule.sex}";
                if (rule.setId < 1 || rule.setId > 12 || !Enum.IsDefined(typeof(BuilderSex), rule.sex) || !keys.Add(key))
                    errors.Add(key + ": 세트·성별이 잘못되었거나 중복됩니다.");
                ValidatePart(rule.arm, catalog.Arms, rule.setId, key, errors);
                ValidatePart(rule.chest, catalog.Chests, rule.setId, key, errors);
                ValidatePart(rule.waist, catalog.Waists, rule.setId, key, errors);
                ValidatePart(rule.leg, catalog.Legs, rule.setId, key, errors);
                ValidateHair(rule, catalog, key, errors);
                if (rule.sex == BuilderSex.Male)
                {
                    ValidatePart(rule.facialHair, catalog.FacialHairs, rule.facialHairId, key, errors);
                    if (rule.bustSize != null) errors.Add(key + ": 남성 가슴 크기는 미사용이어야 합니다.");
                }
                else if (rule.facialHair != null || rule.facialHairId != 0 || !catalog.BustSizes.Contains(rule.bustSize))
                    errors.Add(key + ": 여성 수염은 미사용, 가슴 크기는 한 개여야 합니다.");
            }
            return errors;
        }

        private static void ValidateFaces(NpcFaceSkinPair[] pairs, BuilderSex sex, P09AssetCatalog catalog, List<string> errors)
        {
            if (pairs == null || pairs.Length != 3) { errors.Add(sex + ": 얼굴·피부 묶음 3개가 필요합니다."); return; }
            // 피부 SO 자체에는 성별이 없으므로 원본 컨테이너의 성별·참조가 기준이다.
            var skins = new HashSet<ScriptableObject>();
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { PathConfig.CatalogRoot }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(guid));
                var serialized = new SerializedObject(asset);
                var sexProperty = serialized.FindProperty("_sexId");
                var list = serialized.FindProperty("_partDataList");
                if (sexProperty == null || list == null || sexProperty.intValue != (sex == BuilderSex.Male ? 1 : 2)) continue;
                for (int i = 0; i < list.arraySize; i++)
                    if (list.GetArrayElementAtIndex(i).objectReferenceValue is ColorEditPartData skin) skins.Add(skin);
            }
            var ids = new HashSet<int>();
            foreach (var pair in pairs)
            {
                if (pair == null || pair.face == null || pair.skin == null || !catalog.FaceTypes.Contains(pair.face)
                    || !skins.Contains(pair.skin) || pair.face.ContentId != pair.skin.ContentId
                    || pair.face.ContentId < 1 || pair.face.ContentId > 3 || !ids.Add(pair.face.ContentId))
                    errors.Add(sex + ": 원본 FaceType 01~03과 같은 번호·성별의 피부색을 짝지어야 합니다.");
            }
        }

        private static void ValidateHair(NpcAppearanceRule rule, P09AssetCatalog catalog, string key, List<string> errors)
        {
            if (rule.hairStyles == null || rule.hairStyles.Length != 2 || rule.hairStyles.Distinct().Count() != 2
                || rule.hairStyles.Any(s => s == null || !catalog.HairStyles.Contains(s)))
                errors.Add(key + ": 서로 다른 헤어 스타일 2개가 필요합니다.");
            if (rule.colors == null || rule.colors.Length != 2 || rule.colors.Any(c => c == null || c.hair == null
                || c.eye == null || !catalog.HairColors.Contains(c.hair) || !catalog.EyeColors.Contains(c.eye))
                || rule.colors.Select(c => c.hair.ContentId).Distinct().Count() != 2)
                errors.Add(key + ": 서로 다른 헤어 컬러 2개와 눈 색 매핑이 필요합니다.");
            if (rule.hairStyles == null || rule.colors == null) return;
            foreach (var style in rule.hairStyles.Where(s => s != null))
            foreach (var color in rule.colors.Where(c => c?.hair != null))
                if (style.MeshName != string.Format(color.hair.MeshName, style.ContentId))
                    errors.Add(key + ": 헤어 컬러가 적용되지 않는 스타일은 2개 컬러 축에 사용할 수 없습니다.");
        }

        private static void ValidatePart(ScriptableObject part, List<ScriptableObject> catalog, int id, string key, List<string> errors)
        {
            if (part == null || !catalog.Contains(part) || ReadId(part) != id)
                errors.Add(key + ": 부위 카탈로그 또는 ContentId가 일치하지 않습니다.");
        }

        private static int ReadId(ScriptableObject part) => (part as IEditPartData)?.ContentId ?? -1;
        private static string GetPresetPath(string folder, CharacterBuildConfig config) => $"{folder}/{config.ManualName}_BuildPreset.asset";

        private static void ValidateOutput(List<CharacterBuildConfig> configs, string folder)
        {
            if (folder != PresetFolder && !folder.StartsWith("Assets/__NpcAppearanceTests/", StringComparison.Ordinal))
                throw new InvalidOperationException("전용 생성 폴더만 사용할 수 있습니다.");
            var errors = new List<string>();
            foreach (var config in configs)
            {
                string path = GetPresetPath(folder, config);
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                if (asset == null && !File.Exists(path) && !File.Exists(path + ".meta")) continue;
                if (asset is not BuildPreset || !AssetDatabase.GetLabels(asset).Contains(OwnershipLabel) || EditorUtility.IsDirty(asset))
                    errors.Add(path + ": 다른 자산 또는 저장되지 않은 변경을 덮어쓸 수 없습니다.");
            }
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        }

        private static void EnsureFolders(string folder, List<string> created)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = folder.Substring(0, folder.LastIndexOf('/'));
            EnsureFolders(parent, created);
            string guid = AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
            if (string.IsNullOrEmpty(guid)) throw new IOException("폴더 생성 실패: " + folder);
            created.Add(folder);
        }
    }
}
