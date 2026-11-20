using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Data.UI;
using UPlayGround.Manager.World;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.WorldBuilder.Editor;
using UPlayGround.WorldBuilder.Runtime;

namespace UPlayGround.Editor
{
    public static class WorldBuilderP08Validation
    {
        public const string World = "a808a808a808a808a808a808a808a808";
        public const string Data = "Assets/10.Datas/WorldBuilder/P08Validation";
        public const string Folder = "Assets/WorldBuilder/Worlds/" + World;
        public const string Bootstrap = Folder + "/P08Bootstrap.unity";

        public static void CreateBootstrap()
        {
            var pointer = WorldBakePipeline.ReadPublished(World) ?? throw new InvalidOperationException("P08 발행본이 없습니다.");
            var current = ReadChunkCatalog(pointer.current);
            var party = AssetDatabase.LoadAssetAtPath<PartyConfigSO>("Assets/10.Datas/Party/PartyConfig.asset");
            if (party == null || string.IsNullOrWhiteSpace(party.playerActorId))
                throw new InvalidOperationException("파티 설정의 플레이어 Actor ID가 없습니다.");
            var map = LoadOrCreate<MinimapIconConfigSO>(Data + "/Minimap.asset");
            map.captureCenter = new Vector2(current.worldBounds.center.x, current.worldBounds.center.z);
            map.captureWorldSizeXY = new Vector2(current.worldBounds.size.x, current.worldBounds.size.z);
            map.customMarker = new MinimapIconConfigSO.IconEntry { color = new Color(.9f, .78f, .43f), size = 10 };
            EditorUtility.SetDirty(map);
            AssetDatabase.SaveAssetIfDirty(map);
            var region = LoadOrCreate<MapRegionInfoSO>(Data + "/Region.asset");

            string materialPath = Data + "/Proxy.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                material.SetColor("_BaseColor", new Color(.30f, .36f, .23f));
                material.SetFloat("_Smoothness", 0);
                AssetDatabase.CreateAsset(material, materialPath);
            }

            var previous = SceneManager.GetActiveScene();
            bool exists = AssetDatabase.LoadAssetAtPath<SceneAsset>(Bootstrap) != null;
            var scene = exists ? EditorSceneManager.OpenScene(Bootstrap, OpenSceneMode.Additive) :
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                if (scene.isDirty) throw new InvalidOperationException("P08 부트스트랩에 미저장 편집이 있습니다.");
                var root = Array.Find(scene.GetRootGameObjects(), go => go.GetComponent<WorldChunkStreamer>() != null);
                if (root == null)
                {
                    root = new GameObject("WorldBuilder P08");
                    SceneManager.MoveGameObjectToScene(root, scene);
                    root.AddComponent<WorldBakedNavigation>();
                    root.AddComponent<WorldChunkStreamer>();
                    root.AddComponent<WorldBuilderRuntimeBridge>();
                    root.AddComponent<SceneContext>();
                    var light = new GameObject("햇빛").AddComponent<Light>();
                    SceneManager.MoveGameObjectToScene(light.gameObject, scene);
                    light.type = LightType.Directional;
                    light.intensity = 1.1f;
                    light.transform.rotation = Quaternion.Euler(45, -30, 0);
                }
                if (!Array.Exists(scene.GetRootGameObjects(), go => go.CompareTag("MainCamera")))
                {
                    var cameraRoot = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                    SceneManager.MoveGameObjectToScene(cameraRoot, scene);
                    cameraRoot.tag = "MainCamera";
                    var camera = cameraRoot.GetComponent<Camera>();
                    camera.farClipPlane = 3000;
                    camera.transform.position = current.bake.routes[0].from + new Vector3(0, 4, -8);
                }

                var streamer = root.GetComponent<WorldChunkStreamer>();
                var navigation = root.GetComponent<WorldBakedNavigation>();
                streamer.Configure(AssetDatabase.LoadAssetAtPath<TextAsset>(WorldBakePipeline.PublishedPath(World)),
                    Array.Empty<WorldChunkCatalog>(), navigation, material);

                var context = root.GetComponent<SceneContext>();
                context.SceneType = SceneType.GamePlay;
                context.MapID = World;
                context.WorldMapConfig = map;
                using (var fields = new SerializedObject(context))
                {
                    fields.FindProperty("_regionInfoOverride").objectReferenceValue = region;
                    fields.ApplyModifiedPropertiesWithoutUndo();
                }

                using (var fields = new SerializedObject(root.GetComponent<WorldBuilderRuntimeBridge>()))
                {
                    fields.FindProperty("streamer").objectReferenceValue = streamer;
                    fields.FindProperty("playerActorId").stringValue = party.playerActorId;
                    fields.FindProperty("spawnPosition").vector3Value = current.bake.routes[0].from;
                    fields.ApplyModifiedPropertiesWithoutUndo();
                }

                if (!EditorSceneManager.SaveScene(scene, Bootstrap)) throw new IOException("P08 부트스트랩 저장에 실패했습니다.");
                var scenes = EditorBuildSettings.scenes;
                if (Array.FindIndex(scenes, entry => entry.path == Bootstrap) < 0)
                {
                    Array.Resize(ref scenes, scenes.Length + 1);
                    scenes[scenes.Length - 1] = new EditorBuildSettingsScene(Bootstrap, true);
                    EditorBuildSettings.scenes = scenes;
                }
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
                SceneManager.SetActiveScene(previous);
            }
        }

        private static WorldChunkCatalog ReadChunkCatalog(string revision)
        {
            _ = WorldBakePipeline.ReadRevision(World, revision, reopen: false);
            return AssetDatabase.LoadAssetAtPath<WorldChunkCatalog>(WorldBakePipeline.Revision(World, revision) + "/Chunks.asset")
                ?? throw new InvalidOperationException("청크 catalog가 없습니다.");
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
