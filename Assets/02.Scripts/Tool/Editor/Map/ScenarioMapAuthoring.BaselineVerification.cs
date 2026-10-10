using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Components;
using UPlayGround.Data.UI;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        /// <summary>임시 씬 사본에서 구 배치 제외와 실제 배치 함수의 반복 적용을 검사한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/맵/시나리오 P00 재생성 검증")]
        public static void VerifyBaselineRegeneration()
        {
            RequireEditMode();
            for (int index = 0; index < SceneManager.sceneCount; index++)
                if (SceneManager.GetSceneAt(index).isDirty)
                    throw new InvalidOperationException("미저장 씬이 있어 검증을 중단합니다.");
            const string staging = "Assets/01.Scenes/Scenario/P00VerificationStaging.unity";
            if (File.Exists(staging) || File.Exists(staging + ".meta"))
                throw new InvalidOperationException("임시 씬 경로가 이미 사용 중입니다.");
            var manifest = JsonUtility.FromJson<RemovalManifest>(File.ReadAllText(RemovalDirectory + "/RemovalManifest.json"));
            Layout layout = ReadLayout();
            var quality = JsonUtility.FromJson<QualityLayout>(File.ReadAllText(QualityPath));
            var world = JsonUtility.FromJson<WorldLayout>(File.ReadAllText(WorldPath));
            var region = AssetDatabase.LoadAssetAtPath<MapRegionInfoSO>(layout.assetDirectory + "/RegionInfo.asset");
            if (region == null || EditorUtility.IsDirty(region) || region.flowGraphs.Count != 0)
                throw new InvalidOperationException("RegionInfo 상태를 먼저 확인하세요.");
            string regionSnapshot = EditorJsonUtility.ToJson(region);
            var protectedFiles = new List<BaselineFile>(manifest.protectedFiles);
            using (var sha = System.Security.Cryptography.SHA256.Create())
                foreach (BaselineFile file in manifest.files)
                    protectedFiles.Add(new BaselineFile { path = file.path,
                        sha256 = BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(file.path))).Replace("-", "").ToLowerInvariant() });
            try
            {
                Scene source = EditorSceneManager.OpenScene(SourceScenePath);
                if (!EditorSceneManager.SaveScene(source, staging, true))
                    throw new InvalidOperationException("원본 검사 사본을 만들지 못했습니다.");
                Scene scene = EditorSceneManager.OpenScene(staging);
                RemoveExcludedSourceObjects(scene, layout);
                AssertRemovalResult(scene, manifest);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AssetDatabase.DeleteAsset(staging);
                scene = EditorSceneManager.OpenScene(layout.scenePath);
                Dictionary<string, string> identities = CaptureIdentities(scene);
                if (!EditorSceneManager.SaveScene(scene, staging, true))
                    throw new InvalidOperationException("재적용 검사 사본을 만들지 못했습니다.");
                scene = EditorSceneManager.OpenScene(staging);
                RestoreIdentities(scene, identities);
                Terrain terrain = FindQualityTransform(scene, "AuthoredEnvironment").GetComponentInChildren<Terrain>();
                for (int pass = 0; pass < 2; pass++)
                {
                    ApplyDistrictResidents(layout.district, terrain, scene);
                    var actors = new Dictionary<string, SceneEntityId>();
                    foreach (GameObject root in scene.GetRootGameObjects())
                        foreach (SceneEntityId identity in root.GetComponentsInChildren<SceneEntityId>(true))
                            actors.Add(identity.Guid, identity);
                    MoveQualityActors(quality, terrain, actors);
                    ConfigureQualityGroups(scene, quality);
                    PlaceInteractionVisuals(scene, terrain, quality);
                    PlaceQualityResources(scene, quality, terrain);
                    RemoveWorldRoot(scene, "WorldExpansionActors");
                    Transform worldRoot = new GameObject("WorldExpansionActors").transform;
                    PlaceWorldMonsters(world, terrain, worldRoot);
                    PlaceWorldInteractions(world, terrain, worldRoot);
                    PlaceWorldPortals(world, layout, terrain, worldRoot);
                    AssertRemovalResult(scene, manifest);
                    Dictionary<string, string> applied = CaptureIdentities(scene);
                    if (applied.Count != identities.Count)
                        throw new InvalidOperationException("반복 적용 후 배치 수가 달라졌습니다.");
                    foreach (var pair in identities)
                        if (!applied.TryGetValue(pair.Key, out string id) || id != pair.Value)
                            throw new InvalidOperationException("반복 적용 후 ID 변경: " + pair.Key);
                    if (!EditorSceneManager.SaveScene(scene, staging))
                        throw new InvalidOperationException("검사 사본을 저장하지 못했습니다.");
                    scene = EditorSceneManager.OpenScene(staging);
                    terrain = FindQualityTransform(scene, "AuthoredEnvironment").GetComponentInChildren<Terrain>();
                    AssertRemovalResult(scene, manifest);
                }

            }
            finally
            {
                region = AssetDatabase.LoadAssetAtPath<MapRegionInfoSO>(layout.assetDirectory + "/RegionInfo.asset");
                EditorJsonUtility.FromJsonOverwrite(regionSnapshot, region);
                EditorUtility.ClearDirty(region);
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                if (File.Exists(staging)) AssetDatabase.DeleteAsset(staging);
                EditorSceneManager.OpenScene(layout.scenePath);
                VerifyBaselineHashes(protectedFiles.ToArray());
            }
            File.WriteAllText(RemovalDirectory + "/RegenerationResult.json",
                "{\"sourceExclusion\":true,\"reapplicationPasses\":2,\"reloaded\":true,\"preservedIds\":true,\"brokenReferences\":0}");
        }
    }
}
