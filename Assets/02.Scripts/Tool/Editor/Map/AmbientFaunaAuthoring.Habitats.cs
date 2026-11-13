using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.Data.World;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.World.LivingWorld;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class AmbientFaunaAuthoring
    {
        public const string CompletionEvidence = "output/AmbientFauna/Completion";

        /// <summary>기존 나비·CPU 원본을 보존하며 작은 서식지만 추가한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/본편 서식지 연결")]
        public static void InstallSpeciesHabitats()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.isDirty || scene.name != "LakeVillageWorld")
                throw new InvalidOperationException("저장된 LakeVillageWorld 편집 씬이 필요합니다.");
            Directory.CreateDirectory(CompletionEvidence);
            string backup = CompletionEvidence + "/LakeVillageWorld.before-species.unity";
            if (!File.Exists(backup)) File.Copy(scene.path, backup);
            if (!File.Exists(backup + ".meta")) File.Copy(scene.path + ".meta", backup + ".meta");
            var host = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<AmbientFaunaHabitat>(true)).Single();
            var hostSettings = new SerializedObject(host);
            hostSettings.FindProperty("_quality").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AmbientFaunaQualitySO>(DataRoot + "/Quality.asset");
            hostSettings.ApplyModifiedPropertiesWithoutUndo();
            string[] species = { "Gnat", "Gnat", "Firefly", "Firefly", "AirMote", "AirMote" };
            string[] names = { "Gnat_WetReeds_A", "Gnat_WetReeds_B", "Firefly_LakeMeadow", "Firefly_WetReeds", "AirMote_Shade", "AirMote_Reeds" };
            Vector3[] positions = { new(1642, 78, 771), new(1630, 81, 797), new(1039, 84, 699),
                new(1636, 80, 787), new(1045, 65, 465), new(1628, 84, 812) };
            for (int i = 0; i < species.Length; i++)
            {
                if (host.transform.Find(names[i]) != null) continue;
                var profile = AssetDatabase.LoadAssetAtPath<AmbientFaunaProfileSO>(DataRoot + "/" + species[i] + ".asset");
                Vector3 position = positions[i];
                var ground = Physics.RaycastAll(position + Vector3.up * 20, Vector3.down, 50, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                    .Where(h => h.collider is TerrainCollider).OrderBy(h => h.distance).FirstOrDefault();
                if (ground.collider == null) throw new InvalidOperationException(names[i] + ": 지형 표면이 없습니다.");
                position.y = ground.point.y + 1.8f;
                var group = CreateGroup(host.transform, names[i], profile, position, species[i] == "Firefly" ? 3 : 4, 0.4f, (uint)(300 + i));
                Undo.RegisterCreatedObjectUndo(group.gameObject, "환경 생물 서식지 추가");
                var placement = Undo.AddComponent<AmbientFaunaPlacement>(group.gameObject);
                placement.Size = new Vector3(5, 5, 5);
                placement.Clearance = species[i] == "AirMote" ? 0.45f : 0.3f;
                placement.FlightHeight = species[i] == "AirMote" ? 1.6f : 0.9f;
                placement.Samples = 96;
                placement.MinimumSpacing = 1.2f;
                placement.Seed = 300 + i;
                group.SetBakedAnchors(BakePlacement(placement));
                if (!group.TryPrepare(out string error)) throw new InvalidOperationException(group.name + ": " + error);
            }
            AttachRendering(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            ValidateActiveWorld();
        }

        /// <summary>현재 씬의 CPU 보존·종별 용량·베이크와 누락 컴포넌트를 검사한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/서식지 검증")]
        public static string ValidateActiveWorld()
        {
            var roots = SceneManager.GetActiveScene().GetRootGameObjects();
            var groups = roots.SelectMany(r => r.GetComponentsInChildren<AmbientFaunaGroup>(true)).ToArray();
            var errors = new System.Collections.Generic.List<string>();
            foreach (var group in groups)
            {
                var placement = group.GetComponent<AmbientFaunaPlacement>();
                if (placement != null && placement.GeometryHash != ComputeGeometryHash(placement)) errors.Add(group.name + ": geometry stale");
                if (!Application.isPlaying && !group.TryPrepare(out string error)) errors.Add(group.name + ": " + error);
            }
            int missing = roots.SelectMany(r => r.GetComponentsInChildren<Transform>(true))
                .Sum(t => GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
            var result = new WorldSpeciesReport { CpuCount = roots.SelectMany(r => r.GetComponentsInChildren<WildlifeCreature>(true)).Count(),
                Groups = groups.Length, Capacity = groups.Sum(g => g.Capacity), MissingScripts = missing,
                Fallbacks = groups.Sum(g => new SerializedObject(g).FindProperty("_cpuFallbacks").arraySize),
                Errors = errors.ToArray(), Species = groups.GroupBy(g => g.Profile != null ? g.Profile.SpeciesId : "missing-profile").Select(g => new SpeciesCount {
                    Id = g.Key, Groups = g.Count(), Capacity = g.Sum(x => x.Capacity) }).ToArray() };
            Directory.CreateDirectory(CompletionEvidence);
            string json = JsonUtility.ToJson(result, true);
            File.WriteAllText(CompletionEvidence + "/world-validation.json", json);
            if (errors.Count > 0 || missing > 0) throw new InvalidOperationException(json);
            return json;
        }

        [Serializable] private sealed class WorldSpeciesReport
        {
            public int CpuCount, Groups, Capacity, Fallbacks, MissingScripts;
            public string[] Errors;
            public SpeciesCount[] Species;
        }
        [Serializable] private sealed class SpeciesCount { public string Id; public int Groups, Capacity; }
    }
}
