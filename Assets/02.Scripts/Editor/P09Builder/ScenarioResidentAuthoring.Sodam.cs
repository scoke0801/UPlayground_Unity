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
        private const string SodamRecipePath = DataRoot + "/P09_SodamAppearance.json";
        private const string SodamTexturePath = "Assets/04.Images/Character/ScenarioResidents/P09_Female_Body_Diff_Sodam.png";
        private const string FemaleSkinPath = "Assets/ExternalAssets/Character/P09_Modular_Humanoid/Model_DATA/Materials/Female_Body/P09_Female_Skin.mat";

        [Serializable] private sealed class SodamRecipe
        {
            public string revision;
            public Resident resident;
            public ElderRecipe sculpt;
            public ElderRecipe hairSculpt;
        }

        /// <summary>소담 제작표의 원본 외형과 얼굴 좌표를 에셋 저장 없이 확인한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/소담 외형 조사")]
        public static void InspectSodamAppearance()
        {
            RequireLifeEditor();
            var recipe = JsonUtility.FromJson<SodamRecipe>(File.ReadAllText(SodamRecipePath));
            var root = CreateSodamModel(recipe, out _);
            string folder = Output + "/Sodam/Source";
            Directory.CreateDirectory(folder);
            try
            {
                var face = root.GetComponentsInChildren<SkinnedMeshRenderer>().Single(r => r.name == recipe.sculpt.faceRenderer);
                File.WriteAllText(folder + "/geometry.txt", face.sharedMesh.bounds + "\n" + string.Join("\n",
                    root.GetComponentsInChildren<SkinnedMeshRenderer>().Select(r => r.name + ": " + r.sharedMesh.bounds + " / " + string.Join(",", r.sharedMaterials.Select(m => AssetDatabase.GetAssetPath(m))))));
                File.WriteAllLines(folder + "/vertices.csv", face.sharedMesh.vertices.Select(v => FormattableString.Invariant($"{v.x},{v.y},{v.z}")));
                foreach (int yaw in new[] { 0, 45, 90, 180 })
                {
                    CaptureSodamCopy(root, folder + "/Face_" + yaw + ".png", true, yaw);
                    CaptureSodamCopy(root, folder + "/FullBody_" + yaw + ".png", false, yaw);
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        /// <summary>소담의 독립 외형 프리팹·피부·메시·프리셋을 만들고 직렬화와 표정을 검사한다.</summary>
        [UPlaygroundTool("UPlayGround/캐릭터/P09/소담 외형 제작 검수")]
        public static void BuildSodamAppearance()
        {
            RequireLifeEditor();
            var recipe = JsonUtility.FromJson<SodamRecipe>(File.ReadAllText(SodamRecipePath));
            if (string.IsNullOrWhiteSpace(recipe.revision) || recipe.revision.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || recipe.sculpt.handles == null || recipe.hairSculpt?.handles == null
                || recipe.sculpt.handles.Concat(recipe.hairSculpt.handles).Any(h => h.radius.x <= 0 || h.radius.y <= 0 || h.radius.z <= 0))
                throw new InvalidOperationException("소담 제작표의 revision과 얼굴 변형 반경을 확인하세요.");
            string assets = DataRoot + "/SodamAppearance_" + recipe.revision;
            string folder = Output + "/Sodam/" + recipe.revision;
            string prefabPath = "Assets/03.Prefabs/Characters/ScenarioResidents/AppearanceReview/P09_Female_Sodam_" + recipe.revision + ".prefab";
            if (Directory.Exists(assets) || File.Exists(prefabPath))
                throw new InvalidOperationException("기존 소담 외형을 덮어쓰지 않습니다. 새 revision으로 제작하세요.");
            var sourceSkin = Require<Material>(FemaleSkinPath);
            var texture = Require<Texture2D>(SodamTexturePath);
            var importer = (TextureImporter)AssetImporter.GetAtPath(SodamTexturePath);
            importer.textureType = TextureImporterType.Default;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = true;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
            var root = CreateSodamModel(recipe, out var config);
            var created = new List<string>();
            var temporary = new List<UnityEngine.Object>();
            Directory.CreateDirectory(folder);
            try
            {
                var renderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                var face = renderers.Single(r => r.name == recipe.sculpt.faceRenderer);
                var sourceFace = face.sharedMesh;
                var sculpted = SculptElderMesh(sourceFace, recipe.sculpt);
                sculpted.name = "Sodam_Face";
                temporary.Add(sculpted);
                ValidateElderMesh(sourceFace, sculpted);
                var bounds = face.localBounds;
                bounds.Expand(sourceFace.vertices.Zip(sculpted.vertices, Vector3.Distance).Max() * 2);
                face.sharedMesh = sculpted;
                face.localBounds = bounds;
                var skin = new Material(sourceSkin) { name = "P09_Sodam_Skin" };
                temporary.Add(skin);
                foreach (string property in sourceSkin.GetTexturePropertyNames())
                    if (sourceSkin.GetTexture(property) == sourceSkin.mainTexture) skin.SetTexture(property, texture);
                ReplaceSkin(root, sourceSkin, skin);
                var hair = renderers.Single(r => r.name == recipe.sculpt.hairRenderer);
                Mesh sourceHair = hair.sharedMesh;
                var sculptedHair = SculptElderMesh(sourceHair, recipe.hairSculpt);
                sculptedHair.name = "Sodam_Hair";
                temporary.Add(sculptedHair);
                ValidateElderMesh(sourceHair, sculptedHair);
                var hairBounds = hair.localBounds;
                hairBounds.Expand(sourceHair.vertices.Zip(sculptedHair.vertices, Vector3.Distance).Max() * 2);
                hair.sharedMesh = sculptedHair;
                hair.localBounds = hairBounds;
                var hairMaterial = CreateElderMaterial(recipe.sculpt.hairMaterial, "P09_Sodam_Hair", recipe.sculpt.hairTone);
                temporary.Add(hairMaterial);
                hair.sharedMaterial = hairMaterial;
                ValidateElderRoot(root);
                if (skin.mainTexture != texture || !renderers.Any(r => r.sharedMaterials.Contains(skin))
                    || renderers.Any(r => r.sharedMaterials.Contains(sourceSkin)))
                    throw new InvalidOperationException("소담 피부 바인딩 누락");
                PathConfig.EnsureFolderExists(assets);
                SaveSodamAsset(sculpted, assets + "/Sodam_Face.asset", created);
                SaveSodamAsset(sculptedHair, assets + "/Sodam_Hair.asset", created);
                SaveSodamAsset(skin, assets + "/P09_Sodam_Skin.mat", created);
                SaveSodamAsset(hairMaterial, assets + "/P09_Sodam_Hair.mat", created);
                var preset = ScriptableObject.CreateInstance<BuildPreset>();
                temporary.Add(preset);
                preset.config = config;
                preset.description = "소담 기본 P09 조합. 최종 노년 피부·메시는 P09_SodamAppearance 제작표와 소담 외형 제작 검수로 적용한다.";
                SaveSodamAsset(preset, assets + "/Npc_Sodam_BuildPreset.asset", created);
                root.name = "P09_Female_Sodam_" + recipe.revision;
                PathConfig.EnsureFolderExists(Path.GetDirectoryName(prefabPath).Replace('\\', '/'));
                created.Add(prefabPath);
                if (PrefabUtility.SaveAsPrefabAsset(root, prefabPath) == null)
                    throw new InvalidOperationException("소담 프리팹 저장 실패");
                var saved = Require<GameObject>(prefabPath);
                ValidateElderRoot(saved);
                var savedFace = saved.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r => r.name == recipe.sculpt.faceRenderer);
                ValidateElderMesh(sourceFace, savedFace.sharedMesh);
                ValidateElderMesh(sourceHair, saved.GetComponentsInChildren<SkinnedMeshRenderer>(true).Single(r => r.name == recipe.sculpt.hairRenderer).sharedMesh);
                foreach (bool side in new[] { false, true })
                    foreach (int yaw in new[] { 0, 45, 90, 180 })
                        CaptureSodamCopy(saved, folder + "/Face_" + (side ? "Side_" : "Soft_") + yaw + ".png", true, yaw, side);
                foreach (int yaw in new[] { 0, 90, 180 })
                    CaptureSodamCopy(saved, folder + "/FullBody_" + yaw + ".png", false, yaw);
                foreach (string expression in new[] { "Eye_Blink", "Eye_Blink_L", "Eye_Blink_R", "Mouth_Smile", "Mouth_Shout", "Lip_aa", "Lip_ii", "Lip_uu", "Lip_ee", "Lip_oo" })
                {
                    if (savedFace.sharedMesh.GetBlendShapeIndex(expression) < 0)
                        throw new InvalidOperationException("표정 누락: " + expression);
                    CaptureElderExpression(saved, expression, folder);
                }
                File.WriteAllText(folder + "/validation.txt", "프리팹 재로드 / Missing Script 0 / 메시·소재·본 참조 유효\n"
                    + "얼굴 UV·삼각형·본 가중치·바인드 포즈·표정 " + sourceFace.blendShapeCount + "종 보존\n"
                    + "원본 셰이더·노멀맵 유지 / 별도 얼굴 피부 / 4각도·2조명·표정 10종 촬영\n" + prefabPath
                    + "\n외형 프리팹. NPC 등록·씬 배치·Play Mode·Player Build는 별도 검증.");
            }
            catch
            {
                // 신규 revision에서 만든 파일만 정리하고 원본 및 다른 주민은 보존한다.
                foreach (string path in created.AsEnumerable().Reverse()) AssetDatabase.DeleteAsset(path);
                throw;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
                foreach (var asset in temporary)
                    if (asset != null && !EditorUtility.IsPersistent(asset)) UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static GameObject CreateSodamModel(SodamRecipe recipe, out CharacterBuildConfig config)
        {
            var catalog = new P09AssetCatalog();
            catalog.Refresh();
            config = CreateConfig(recipe.resident, catalog);
            config.Stats.npcEnableWander = false;
            // 성별 간 중복 ContentId 때문에 정확한 여성 피부 참조만 제공한다.
            catalog.SkinColorsFemale.Clear();
            catalog.SkinColorsFemale.Add(config.SkinColorSo);
            var root = (GameObject)PrefabUtility.InstantiatePrefab(Require<GameObject>(PathConfig.BasePrefabFemaleNoPhysics));
            try
            {
                AppearanceApplier.Apply(root, config, catalog);
                ApplyWeaponStep.Apply(root, config, catalog);
                return root;
            }
            catch { UnityEngine.Object.DestroyImmediate(root); throw; }
        }

        private static void SaveSodamAsset(UnityEngine.Object asset, string path, List<string> created)
        {
            created.Add(path);
            AssetDatabase.CreateAsset(asset, path);
        }

        private static void CaptureSodamCopy(GameObject source, string path, bool face, int yaw, bool side = false)
        {
            var copy = EditorUtility.IsPersistent(source)
                ? (GameObject)PrefabUtility.InstantiatePrefab(source) : UnityEngine.Object.Instantiate(source);
            bool allowAsyncCompilation = ShaderUtil.allowAsyncCompilation;
            try
            {
                ShaderUtil.allowAsyncCompilation = false;
                Capture(copy, path, face, yaw, side, true);
            }
            finally
            {
                ShaderUtil.allowAsyncCompilation = allowAsyncCompilation;
                if (copy != null) UnityEngine.Object.DestroyImmediate(copy);
            }
        }
    }
}
