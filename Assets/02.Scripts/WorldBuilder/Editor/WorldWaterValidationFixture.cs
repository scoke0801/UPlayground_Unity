using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.WorldBuilder.Editor
{
    /// <summary>P04 연결을 새 자산으로 재현하는 P05 전용 합성 월드. 기존 B/C/P03/P04는 수정하지 않는다.</summary>
    public static class WorldWaterValidationFixture
    {
        public const string WorldId = "e505e505e505e505e505e505e505e505";
        public const string WorldFolder = "Assets/WorldBuilder/Worlds/" + WorldId;
        public const string DataFolder = "Assets/10.Datas/WorldBuilder/P05Validation";
        public const string ScenePath = WorldFolder + "/P05Validation.unity";
        public const string DefinitionPath = DataFolder + "/Definition.asset";
        public static WorldDefinition CreateValidationScene() => Create(WorldId, WorldFolder, DataFolder, true);
        public static WorldDefinition Create(string worldId, string folder, string dataFolder, bool fullSize, NewSceneMode mode = NewSceneMode.Additive)
        {
            var previous = SceneManager.GetActiveScene(); WorldDefinition definition = null;
            try
            {
                definition = WorldPlanningValidationFixture.Create(worldId, folder, dataFolder, fullSize, mode);
                string oldPath = SceneManager.GetActiveScene().path, scenePath = folder + "/P05Validation.unity";
                string moved = AssetDatabase.MoveAsset(oldPath, scenePath); if (!string.IsNullOrEmpty(moved)) throw new IOException(moved);
                var normal = new Texture2D(32, 32, TextureFormat.RGBA32, false, true) { name = "P05 Wave Normal", wrapMode = TextureWrapMode.Repeat };
                var pixels = new Color[32 * 32];
                for (int z = 0; z < 32; z++) for (int x = 0; x < 32; x++)
                {
                    var n = new Vector3(Mathf.Sin(x * Mathf.PI / 8 + Mathf.Sin(z * Mathf.PI / 16)) * .35f, Mathf.Cos(z * Mathf.PI / 8) * .35f, 1).normalized;
                    pixels[z * 32 + x] = new Color(n.x * .5f + .5f, n.y * .5f + .5f, n.z * .5f + .5f, 1);
                }
                normal.SetPixels(pixels); normal.Apply(); AssetDatabase.CreateAsset(normal, dataFolder + "/WaveNormal.asset");
                var waterMaterial = Material(dataFolder + "/Surface.mat", "UPlayGround/WorldBuilder/Water");
                var river = ScriptableObject.CreateInstance<WaterBodyPreset>(); river.material = waterMaterial; river.normalMap = normal; river.width = fullSize ? 24 : 8;
                river.footstepProfile = LoadResource<UPlayGround.Data.Actor.FootstepSurfaceProfileSO>("Assets/10.Datas/Actor/Footsteps/Surface_ShallowWater.asset");
                river.shoreWidth = fullSize ? 2 : 1; river.bankWidth = fullSize ? 2 : 1; river.bankClearance = 2; river.shoreFalloff = fullSize ? 4 : 2; river.maxCutFill = 30;
                AssetDatabase.CreateAsset(river, dataFolder + "/RiverPreset.asset");
                var upperPreset = UnityEngine.Object.Instantiate(river); upperPreset.width = 8; AssetDatabase.CreateAsset(upperPreset, dataFolder + "/UpperRiverPreset.asset");
                var waterfall = ScriptableObject.CreateInstance<WaterfallPreset>();
                waterfall.curtainMaterial = Material(dataFolder + "/Curtain.mat", "UPlayGround/Environment/Toon Waterfall");
                waterfall.foamMaterial = Material(dataFolder + "/Foam.mat", "UPlayGround/WorldBuilder/Foam"); waterfall.foamMaterial.SetColor("_BaseColor", new Color(.75f, .88f, .87f, .75f));
                waterfall.particleMaterial = LoadResource<Material>("Assets/10.Datas/Actor/Footsteps/Effects/FootstepWaterSplash.mat");
                waterfall.audioClip = LoadResource<AudioClip>("Assets/ExternalAssets/SFX/Footsteps - Essentials/Footsteps_Water/Footsteps_Water_Jump/Footsteps_Water_Jump_Big_01.wav");
                if (waterfall.particleMaterial == null || waterfall.audioClip == null) throw new InvalidOperationException("기존 물보라 재질/실제 물소리 리소스를 찾지 못했습니다.");
                AssetDatabase.CreateAsset(waterfall, dataFolder + "/WaterfallPreset.asset");
                var source = ScriptableObject.CreateInstance<WaterSystemDefinition>(); source.worldGuid = worldId; source.systemGuid = System.Guid.NewGuid().ToString("N"); source.waterfallPreset = waterfall;
                AssetDatabase.CreateAsset(source, dataFolder + "/WaterSystem.asset");
                source.bodies.Add(new WaterSystemDefinition.BodyRecord { waterGuid = WorldPlanningValidationFixture.Id(9), kind = WaterBodyKind.River, isRouteLocked = true, preset = river,
                    route = fullSize ? new[] { P(512, 8, 180), P(512, 8, 320) } : new[] { P(64, 8, 30), P(64, 8, 100) } });
                source.bodies.Add(new WaterSystemDefinition.BodyRecord { waterGuid = WorldPlanningValidationFixture.Id(101), kind = WaterBodyKind.River, isRouteLocked = true, preset = upperPreset,
                    route = fullSize ? new[] { P(560, 20, 400), P(560, 20, 440) } : new[] { P(24, 20, 88), P(24, 20, 104) } });
                source.bodies.Add(new WaterSystemDefinition.BodyRecord { waterGuid = WorldPlanningValidationFixture.Id(102), kind = WaterBodyKind.Lake, level = 8, preset = river,
                    boundary = fullSize ? Rect(540, 448, 580, 480) : Rect(12, 108, 36, 124) });
                if (fullSize) source.bodies.Add(new WaterSystemDefinition.BodyRecord { waterGuid = WorldPlanningValidationFixture.Id(103), kind = WaterBodyKind.Sea, level = 8, preset = river, boundary = Rect(0, 700, 80, 900) });
                source.waterfalls.Add(new WaterfallEdge { guid = WorldPlanningValidationFixture.Id(104), upstreamGuid = WorldPlanningValidationFixture.Id(101), downstreamGuid = WorldPlanningValidationFixture.Id(102),
                    width = upperPreset.width, upperLip = fullSize ? P(560, 20, 440) : P(24, 20, 104), lowerLanding = fullSize ? P(560, 8, 454) : P(24, 8, 114) });
                source.exclusions.Add(new WaterExclusion { guid = WorldPlanningValidationFixture.Id(105), boundary = fullSize ? Rect(508, 200, 516, 208) : Rect(62, 40, 66, 44), minY = 0, maxY = 6.5 });
                WorldWaterAuthoring.Regenerate(definition, source);
                var query = UnityEngine.Object.FindFirstObjectByType<WorldWaterQuery>();
                var probe = new GameObject("P05 물리 조회 probe"); probe.transform.position = fullSize ? new Vector3(512, 7, 240) : new Vector3(64, 7, 60); probe.AddComponent<WorldWaterQueryProbe>().query = query;
                foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                {
                    var camera = root.GetComponent<Camera>(); if (camera == null) continue;
                    camera.gameObject.name = "P05 전용 검증 카메라"; if (camera.GetComponent<AudioListener>() == null) camera.gameObject.AddComponent<AudioListener>();
                    var data = camera.GetUniversalAdditionalCameraData(); data.requiresDepthOption = CameraOverrideOption.On; data.requiresColorOption = CameraOverrideOption.Off;
                    if (fullSize) { camera.transform.position = new Vector3(700, 180, 130); camera.transform.LookAt(new Vector3(520, 10, 330)); }
                }
                foreach (var asset in new UnityEngine.Object[] { river, upperPreset, waterfall, waterfall.foamMaterial, source, definition }) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
                if (!EditorSceneManager.SaveScene(SceneManager.GetActiveScene())) throw new IOException("P05 씬 저장 실패"); return definition;
            }
            catch
            {
                if (definition != null)
                {
                    var scene = SceneManager.GetActiveScene();
                    if (scene.IsValid() && scene.isLoaded && scene.path.StartsWith(folder, StringComparison.Ordinal))
                    { if (SceneManager.sceneCount == 1) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single); else EditorSceneManager.CloseScene(scene, true); }
                    AssetDatabase.DeleteAsset(folder); AssetDatabase.DeleteAsset(dataFolder);
                }
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous); throw;
            }
        }
        private static Material Material(string path, string shaderName)
        { var shader = Shader.Find(shaderName); if (shader == null || !shader.isSupported) throw new InvalidOperationException("URP Shader 미확보: " + shaderName); var material = new Material(shader); AssetDatabase.CreateAsset(material, path); return material; }
        private static T LoadResource<T>(string path) where T : UnityEngine.Object
        {
            string guid = AssetDatabase.AssetPathToGUID(path), resolved = AssetDatabase.GUIDToAssetPath(guid);
            var resource = string.IsNullOrEmpty(guid) ? null : AssetDatabase.LoadAssetAtPath<T>(resolved);
            if (resolved != path || resource == null) throw new IOException("기존 리소스 GUID/경로/타입 오류: " + path); return resource;
        }
        private static PlanPoint P(double x, double y, double z) => new PlanPoint(x, y, z);
        private static PlanPoint[] Rect(double x0, double z0, double x1, double z1) => new[] { P(x0, 0, z0), P(x1, 0, z0), P(x1, 0, z1), P(x0, 0, z1) };
    }
}
