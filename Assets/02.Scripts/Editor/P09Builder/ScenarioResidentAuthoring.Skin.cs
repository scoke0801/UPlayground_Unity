using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor.P09Builder
{
    public static partial class ScenarioResidentAuthoring
    {
        private const string ElderTexturePath = "Assets/04.Images/Character/ScenarioResidents/P09_Male_Body_Diff_Elder.png";
        private const string SourceSkinPath = "Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Materials/Male_Body/P09_Male_Skin.mat";
        private const string ElderMaterialPath = DataRoot + "/P09_Male_Skin_Elder.mat";
        private const string SkinReviewPrefabPath = "Assets/03.Prefabs/Characters/ScenarioResidents/AppearanceReview/P09_Male_ElderSkin.prefab";

        /// <summary>별도 P09 모델에 노년 피부를 적용하고 재로드와 다각도 조명을 검사한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/노년 피부 적용 검수")]
        public static void ReviewElderSkin()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("편집 모드에서 실행하세요.");
            string folder = Output + "/SkinReview";
            Directory.CreateDirectory(folder);
            var source = Require<Material>(SourceSkinPath);
            var texture = Require<Texture2D>(ElderTexturePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(ElderTexturePath);
            // 원본과 같은 2K 런타임 예산에서 주름이 남는지 검수한다.
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            var material = AssetDatabase.LoadAssetAtPath<Material>(ElderMaterialPath);
            if (material == null)
            {
                material = new Material(source) { name = "P09_Male_Skin_Elder" };
                foreach (string property in source.GetTexturePropertyNames())
                    if (source.GetTexture(property) == source.mainTexture) material.SetTexture(property, texture);
                AssetDatabase.CreateAsset(material, ElderMaterialPath);
            }
            if (material.mainTexture != texture || material.shader != source.shader)
                throw new InvalidOperationException("기존 노년 머티리얼 설정을 확인하세요.");
            if (source.HasProperty("_BumpMap") && material.GetTexture("_BumpMap") != source.GetTexture("_BumpMap"))
                throw new InvalidOperationException("원본 노멀맵 참조가 다릅니다.");

            var catalog = new P09AssetCatalog();
            catalog.Refresh();
            var config = new CharacterBuildConfig { Sex = BuilderSex.Male, UseMagicaCloth = false,
                FaceTypeSo = SelectPart(catalog.FaceTypes, 1), HairStyleSo = SelectPart(catalog.HairStyles, 13),
                HairColorSo = SelectPart(catalog.HairColors, 1), SkinColorSo = Require<ScriptableObject>(PathConfig.CatalogRoot + "/SkinColor/SkinColorEditPartData_Male_01.asset"),
                EyeColorSo = SelectPart(catalog.EyeColors, 1) };
            // 원본 카탈로그의 성별 없는 색상 ID가 겹치므로 검수에는 정확한 참조 하나만 공급한다.
            catalog.SkinColorsMale.Clear();
            catalog.SkinColorsMale.Add(config.SkinColorSo);
            ArmorIndexPresetUtility.Apply(config.ArmorSelections, ArmorIndexPresetUtility.Build(catalog).First(x => x.Index == 2));
            config.ArmorSelections.Set(BuilderArmorSlot.Head, null);
            if (!File.Exists(SkinReviewPrefabPath))
            {
                var root = (GameObject)PrefabUtility.InstantiatePrefab(Require<GameObject>(PathConfig.BasePrefabMaleNoPhysics));
                try
                {
                    AppearanceApplier.Apply(root, config, catalog);
                    ApplyWeaponStep.Apply(root, config, catalog);
                    ReplaceSkin(root, source, material);
                    PathConfig.EnsureFolderExists(Path.GetDirectoryName(SkinReviewPrefabPath).Replace('\\', '/'));
                    PrefabUtility.SaveAsPrefabAsset(root, SkinReviewPrefabPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            AssetDatabase.SaveAssetIfDirty(material);
            var saved = Require<GameObject>(SkinReviewPrefabPath);
            var bindings = new List<string>();
            foreach (var renderer in saved.GetComponentsInChildren<Renderer>(true))
            {
                foreach (var slot in renderer.sharedMaterials)
                {
                    if (slot == source) throw new InvalidOperationException("원본 피부 잔존: " + renderer.name);
                    if (slot == material) bindings.Add(renderer.name);
                }
            }
            if (bindings.Count == 0) throw new InvalidOperationException("피부 적용 슬롯이 없습니다.");
            foreach (var transform in saved.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                    throw new InvalidOperationException("Missing Script: " + transform.name);
            foreach (bool baseline in new[] { true, false })
                foreach (bool side in new[] { false, true })
                    foreach (int yaw in new[] { 0, 45, 90, 180 })
                    {
                        var root = (GameObject)PrefabUtility.InstantiatePrefab(saved);
                        try
                        {
                            if (baseline) ReplaceSkin(root, material, source);
                            Capture(root, folder + "/" + (baseline ? "Before" : "After") + "_" + (side ? "Side" : "Soft") + "_" + yaw + ".png", true, yaw, side);
                        }
                        finally { if (root != null) UnityEngine.Object.DestroyImmediate(root); }
                    }
            File.WriteAllText(folder + "/validation.txt", "프리팹 재로드·피부 슬롯 " + bindings.Count
                + "개·Missing Script 0·원본 셰이더/노멀맵 유지 통과.\n" + string.Join("\n", bindings)
                + "\nT 포즈 외형 검수용 모델. NPC 등록·씬 배치·Play Mode 검증 아님.");
        }

        private static void ReplaceSkin(GameObject root, Material source, Material replacement)
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                bool changed = false;
                for (int i = 0; i < materials.Length; i++)
                    if (materials[i] == source) { materials[i] = replacement; changed = true; }
                if (changed) renderer.sharedMaterials = materials;
            }
        }
    }
}
