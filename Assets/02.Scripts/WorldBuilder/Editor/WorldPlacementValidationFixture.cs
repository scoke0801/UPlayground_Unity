using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Editor
{
    /// <summary>B/C와 분리된 합성 월드에서 경계·Pin·Delete를 확인하는 재현 fixture.</summary>
    public static class WorldPlacementValidationFixture
    {
        public const string WorldId = "d303d303d303d303d303d303d303d303";
        public const string WorldFolder = "Assets/WorldBuilder/Worlds/" + WorldId;
        public const string DataFolder = "Assets/10.Datas/WorldBuilder/P03Validation";
        public const string ScenePath = WorldFolder + "/P03Validation.unity";
        public const string DefinitionPath = DataFolder + "/Definition.asset";

        /// <summary>기존 결과를 덮어쓰지 않고 2,048m 규격·512m 첫 사용 범위의 전용 씬을 만든다.</summary>
        public static WorldDefinition CreateValidationScene() => Create(WorldId, WorldFolder, DataFolder, 2048, 2049, 512);

        /// <summary>자동 검사에서는 같은 경로를 작은 독립 월드에서 실행한다.</summary>
        public static WorldDefinition Create(string id, string worldFolder, string dataFolder, int worldSize, int samples, int tileCells, NewSceneMode sceneMode = NewSceneMode.Additive)
        {
            if (Directory.Exists(worldFolder) || Directory.Exists(dataFolder)) throw new IOException("기존 fixture를 덮어쓸 수 없습니다.");
            Scene previous = SceneManager.GetActiveScene(), scene = default;
            try
            {
                WorldBuilderAuthoringAssets.EnsureFolder(worldFolder + "/Terrain"); WorldBuilderAuthoringAssets.EnsureFolder(dataFolder);
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, sceneMode); SceneManager.SetActiveScene(scene);
                var profile = ScriptableObject.CreateInstance<WorldProfile>(); profile.Configure(worldSize, samples, 0, 700, tileCells);
                AssetDatabase.CreateAsset(profile, dataFolder + "/Profile.asset");
                var values = new ushort[samples * samples]; for (int i = 0; i < values.Length; i++) values[i] = 936;
                var source = new HeightField(samples, samples, values);
                File.WriteAllBytes(worldFolder + "/Base.bytes", Raw16HeightmapCodec.Encode(source, HeightmapRowOrigin.South));
                AssetDatabase.ImportAsset(worldFolder + "/Base.bytes", ImportAssetOptions.ForceSynchronousImport);
                var terrains = WorldBuilderSampleImporter.CreateTerrains(source, profile, scene, worldFolder + "/Terrain");
                WorldBuilderSampleImporter.ApplyPreviewSurface(terrains, worldFolder + "/Terrain");
                var shader = Shader.Find("Universal Render Pipeline/Lit");
                if (shader == null) throw new InvalidOperationException("URP Lit 셰이더가 필요합니다.");
                var material = new Material(shader); material.SetColor("_BaseColor", new Color(.55f, .30f, .13f)); material.SetFloat("_Smoothness", 0);
                AssetDatabase.CreateAsset(material, worldFolder + "/Prop.mat");
                var template = new GameObject("P03 검증 소품"); var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.transform.SetParent(template.transform, false); cube.transform.localPosition = new Vector3(0, 2, 0); cube.transform.localScale = new Vector3(2, 4, 4);
                cube.GetComponent<MeshRenderer>().sharedMaterial = material;
                var prefab = PrefabUtility.SaveAsPrefabAsset(template, worldFolder + "/Prop.prefab"); UnityEngine.Object.DestroyImmediate(template);
                var catalog = ScriptableObject.CreateInstance<PlacementCatalog>(); AssetDatabase.CreateAsset(catalog, dataFolder + "/Catalog.asset");
                var serialized = new SerializedObject(catalog); var entries = serialized.FindProperty("_entries"); entries.arraySize = 1;
                var entry = entries.GetArrayElementAtIndex(0); string prefabGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(prefab));
                entry.FindPropertyRelative("prefabGuid").stringValue = prefabGuid; entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                entry.FindPropertyRelative("halfExtents").vector2Value = new Vector2(1, 2); serialized.ApplyModifiedPropertiesWithoutUndo();
                double tileSize = worldSize / profile.TileCount;
                var rule = ScriptableObject.CreateInstance<PlacementRuleAsset>(); rule.layerGuid = Guid.NewGuid().ToString("N"); rule.ruleGuid = Guid.NewGuid().ToString("N"); rule.prefabGuid = prefabGuid;
                float sector = Math.Min(512, worldSize); float sectorX = worldSize > 512 ? (float)tileSize - sector / 2 : 0;
                rule.region = new Rect(sectorX, 0, sector, sector); rule.cellSize = worldSize > 512 ? 32 : 8; rule.spacing = 3; rule.maxSupportDelta = 1;
                AssetDatabase.CreateAsset(rule, dataFolder + "/Rule.asset");
                var camera = new GameObject("P03 검증 카메라").AddComponent<Camera>(); camera.transform.position = new Vector3((float)tileSize + sector * .45f, 10 + sector * .9f, -sector * .25f);
                camera.transform.LookAt(new Vector3((float)tileSize, 10, sector / 2)); camera.farClipPlane = 3000;
                var light = new GameObject("P03 검증 햇빛").AddComponent<Light>(); light.type = LightType.Directional; light.transform.rotation = Quaternion.Euler(55, -25, 0);
                var boundary = GameObject.CreatePrimitive(PrimitiveType.Cube); boundary.name = "타일 경계 표시 (배치 아님)";
                boundary.transform.position = new Vector3((float)tileSize, (float)profile.CreateGrid().SampleToHeight(936) + .01f, sector / 2);
                boundary.transform.localScale = new Vector3(.25f, .02f, sector); UnityEngine.Object.DestroyImmediate(boundary.GetComponent<Collider>());
                boundary.GetComponent<MeshRenderer>().sharedMaterial = material;
                string path = worldFolder + "/P03Validation.unity";
                if (!EditorSceneManager.SaveScene(scene, path)) throw new IOException("검증 씬 저장 실패");
                var definition = ScriptableObject.CreateInstance<WorldDefinition>();
                definition.Initialize(id, profile, AssetDatabase.LoadAssetAtPath<TextAsset>(worldFolder + "/Base.bytes"), AssetDatabase.AssetPathToGUID(path));
                AssetDatabase.CreateAsset(definition, dataFolder + "/Definition.asset");
                var def = new SerializedObject(definition); def.FindProperty("_placementCatalog").objectReferenceValue = catalog;
                def.FindProperty("_placementRules").arraySize = 1; def.FindProperty("_placementRules").GetArrayElementAtIndex(0).objectReferenceValue = rule; def.ApplyModifiedPropertiesWithoutUndo();
                WorldPlacementAuthoring.Regenerate(definition);
                PlacementRecord farthest = null; double distance = -1;
                foreach (var p in definition.Placements)
                {
                    double d = Math.Pow(p.x - tileSize, 2) + Math.Pow(p.z - sector / 2, 2);
                    if (d > distance) { farthest = p; distance = d; }
                }
                var pin = WorldPlacementAuthoring.CreatePin(farthest ?? throw new InvalidOperationException("검증 후보가 없습니다."));
                WorldPlacementAuthoring.ApplyOverride(definition, pin);
                PlacementRecord deleteTarget = null;
                foreach (var p in definition.Placements) if (p.id != pin.placement.id) { deleteTarget = p; break; }
                var deleted = WorldPlacementAuthoring.CreateTargetOverride(deleteTarget ?? throw new InvalidOperationException("삭제 검증 후보가 없습니다."), PlacementOverrideKind.Delete);
                WorldPlacementAuthoring.ApplyOverride(definition, deleted);
                var fixedRecord = new PlacementRecord { id = Guid.NewGuid().ToString("N"), prefabGuid = prefabGuid, x = tileSize, z = sector / 2,
                    y = profile.CreateGrid().SampleToHeight(936), anchor = PlacementAnchor.Absolute, maxSupportDelta = 1, spacing = 3, scaleX = 4, scaleY = 1, scaleZ = 4 };
                fixedRecord.sourceIdentity = "manual:" + id + ":" + fixedRecord.id;
                // 비균일 배율은 catalog에서 명시적으로 지원한 검증 소품에만 허용한다.
                serialized.Update(); serialized.FindProperty("_entries").GetArrayElementAtIndex(0).FindPropertyRelative("supportsNonUniformScale").boolValue = true; serialized.ApplyModifiedPropertiesWithoutUndo();
                WorldPlacementAuthoring.ApplyOverride(definition, new PlacementOverride { overrideGuid = Guid.NewGuid().ToString("N"), kind = PlacementOverrideKind.Manual, placement = fixedRecord });
                AssetDatabase.SaveAssetIfDirty(catalog); AssetDatabase.SaveAssetIfDirty(rule); AssetDatabase.SaveAssetIfDirty(definition);
                foreach (var terrain in terrains) AssetDatabase.SaveAssetIfDirty(terrain.terrainData);
                EditorSceneManager.SaveScene(scene);
                return definition;
            }
            catch
            {
                if (scene.IsValid() && scene.isLoaded)
                {
                    if (SceneManager.sceneCount == 1) EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    else EditorSceneManager.CloseScene(scene, true);
                }
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                // 이번 호출만 만든 두 폴더를 정리한다. 기존 B/C에는 접근하지 않는다.
                AssetDatabase.DeleteAsset(worldFolder); AssetDatabase.DeleteAsset(dataFolder);
                throw;
            }
        }
    }
}

