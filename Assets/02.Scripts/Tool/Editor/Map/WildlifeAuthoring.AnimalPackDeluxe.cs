using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UPlayGround.Data.World;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.World.LivingWorld;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class WildlifeAuthoring
    {
        private const string AnimalPackCatalog = "Tools/Wildlife/AnimalPackDeluxe.catalog.json";
        private const string AnimalPackLayout = "Tools/Wildlife/LakeVillageWorld.animals.json";

        /// <summary>원본 패키지를 보존하며 선택한 동물의 환경 생물 프리팹을 생성한다.</summary>
        [UPlaygroundTool("Wildlife/Animal Pack Deluxe 에셋 생성")]
        public static void BuildAnimalPackDeluxe()
        {
            if (Application.isPlaying) throw new InvalidOperationException("편집 모드에서 실행해야 합니다.");
            Directory.CreateDirectory(DataRoot + "/Clips");
            Directory.CreateDirectory(PrefabRoot);
            AssetDatabase.Refresh();
            var catalog = JsonUtility.FromJson<GroundCatalog>(File.ReadAllText(AnimalPackCatalog));
            foreach (var species in catalog.species) BuildGroundSpecies(species);
            AssetDatabase.SaveAssets();
        }

        private static void ApplyUrpMaterials(GameObject root)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit 셰이더가 없습니다.");
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                var materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var source = materials[i];
                    if (source == null) throw new InvalidOperationException(root.name + ": 원본 머티리얼 누락");
                    // 이름이 같은 원본 머티리얼도 GUID로 구별한다.
                    string path = DataRoot + "/APD_" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(source)) + ".mat";
                    var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (material == null)
                    {
                        material = new Material(shader) { name = "APD_" + source.name };
                        material.SetTexture("_BaseMap", source.mainTexture);
                        material.SetColor("_BaseColor", source.color);
                        material.SetTextureScale("_BaseMap", source.mainTextureScale);
                        material.SetTextureOffset("_BaseMap", source.mainTextureOffset);
                        material.SetFloat("_Smoothness", source.HasProperty("_Glossiness") ? source.GetFloat("_Glossiness") : 0);
                        if (source.HasProperty("_BumpMap") && source.GetTexture("_BumpMap") != null)
                        {
                            material.SetTexture("_BumpMap", source.GetTexture("_BumpMap"));
                            material.EnableKeyword("_NORMALMAP");
                        }
                        if (source.IsKeywordEnabled("_ALPHATEST_ON"))
                        {
                            material.SetFloat("_AlphaClip", 1);
                            material.SetFloat("_Cutoff", source.GetFloat("_Cutoff"));
                            material.EnableKeyword("_ALPHATEST_ON");
                            material.renderQueue = 2450;
                        }
                        AssetDatabase.CreateAsset(material, path);
                    }
                    materials[i] = material;
                }
                renderer.sharedMaterials = materials;
            }
        }

        /// <summary>배치표를 적용하되 이전 좌표에서 사용자가 옮긴 개체는 보존한다.</summary>
        [UPlaygroundTool("Wildlife/Animal Pack Deluxe 월드 배치")]
        public static void PlaceAnimalPackDeluxe()
        {
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(AnimalPackLayout));
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (Application.isPlaying || scene.path != layout.scene)
                throw new InvalidOperationException("LakeVillageWorld 편집 모드에서 실행해야 합니다.");
            var habitat = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<WildlifeHabitat>(true)).Single();
            var pending = new List<(Placement entry, GameObject prefab, Transform existing, Vector3 position)>();
            var ids = new HashSet<string>();
            foreach (var entry in layout.placements)
            {
                if (!ids.Add(entry.id)) throw new InvalidOperationException("중복 배치 ID: " + entry.id);
                var existing = habitat.transform.Find(entry.id);
                if (existing != null && (!entry.hasPreviousPosition || (existing.position - entry.previousPosition).sqrMagnitude > 0.0025f)) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/" + entry.prefab + ".prefab");
                var creature = prefab != null ? prefab.GetComponent<WildlifeCreature>() : null;
                if (creature == null || creature.Profile == null)
                    throw new InvalidOperationException(entry.id + ": 프리팹·프로필 누락");
                if (existing != null && PrefabUtility.GetCorrespondingObjectFromSource(existing.gameObject) != prefab) continue;
                Vector3 position = entry.position;
                if (!creature.Profile.IsFlying)
                {
                    var agent = prefab.GetComponent<NavMeshAgent>();
                    var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
                    if (!NavMesh.SamplePosition(position, out var hit, 0.5f, filter))
                        throw new InvalidOperationException(entry.id + ": 배치표의 접지 재검증 실패");
                    position = hit.position;
                    float probeDistance = creature.Profile.LandingProbeDistance;
                    if (creature.Profile.ProjectGroundMovement
                        && Physics.Raycast(position + Vector3.up * (probeDistance * 0.5f), Vector3.down, out var ground, probeDistance,
                            creature.Profile.LandingMask, QueryTriggerInteraction.Ignore))
                        position.y = ground.point.y + creature.Profile.GroundClearance;
                    if (!habitat.CanOccupy(position)) throw new InvalidOperationException(entry.id + ": 수면 아래 배치");
                }
                pending.Add((entry, prefab, existing, position));
            }
            Renderer[] water = null;
            var serializedHabitat = new SerializedObject(habitat);
            var surfaces = serializedHabitat.FindProperty("_waterSurfaces");
            if (layout.waterSurfaces != null && surfaces.arraySize == 0)
            {
                var transforms = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).ToArray();
                water = layout.waterSurfaces.Select(n => transforms.Single(t => t.name == n).GetComponent<Renderer>()).ToArray();
                if (water.Any(r => r == null)) throw new InvalidOperationException("배치표의 수면 렌더러 누락");
            }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Animal Pack Deluxe 동물 배치");
            try
            {
                if (water != null)
                {
                    Undo.RecordObject(habitat, "동물 서식지 수면 연결");
                    surfaces.arraySize = water.Length;
                    for (int i = 0; i < water.Length; i++) surfaces.GetArrayElementAtIndex(i).objectReferenceValue = water[i];
                    serializedHabitat.ApplyModifiedProperties();
                }
                foreach (var item in pending)
                {
                    var instance = item.existing != null ? item.existing.gameObject
                        : (GameObject)PrefabUtility.InstantiatePrefab(item.prefab, habitat.transform);
                    if (item.existing == null)
                    {
                        Undo.RegisterCreatedObjectUndo(instance, "환경 동물 배치");
                        instance.name = item.entry.id;
                        instance.transform.rotation = Quaternion.Euler(0, item.entry.yaw, 0);
                        instance.transform.localScale *= item.entry.scale;
                    }
                    else Undo.RecordObject(instance.transform, "환경 동물 서식 위치 조정");
                    instance.transform.position = item.position;
                    PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
                }
                Undo.CollapseUndoOperations(group);
                if (pending.Count > 0 || water != null) EditorSceneManager.MarkSceneDirty(scene);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                throw;
            }
        }
        private const string BehaviorCatalogPath = "Tools/Wildlife/behavior-tuning.json";

        [Serializable] private sealed class BehaviorCatalog { public BehaviorEntry[] profiles; }
        [Serializable] private sealed class BehaviorEntry
        {
            public string profile, observeSource, restSource;
            public bool hasFeed;
            public BehaviorSettings settings;
        }

        // 클립 참조는 포함하지 않아 수치 재적용이 사용자의 모션 연결을 덮어쓰지 않는다.
        [Serializable] private sealed class BehaviorSettings
        {
            public bool UseStagedResponse = true, FreezesWhenApproached;
            public float RoamRadius, MoveSpeed, FleeSpeed, AlertDistance, AvoidDistance, FleeDistance, CalmDistance, CalmSeconds;
            public float Acceleration, TurnSpeed, NeighbourRadius, PersonalSpace, Cohesion;
            public float WalkCycleSpeed, RunCycleSpeed;
            public Vector2 IdleSeconds, FeedSeconds, ObserveSeconds;
            public Vector2Int ForageSteps;
            public Vector2 ForageStepDistance;
            public float SocialAlertDistance;
        }

        private static void ApplyInitialBehavior(WildlifeProfileSO profile)
        {
            var catalog = JsonUtility.FromJson<BehaviorCatalog>(File.ReadAllText(BehaviorCatalogPath));
            var entry = catalog.profiles.SingleOrDefault(e => e.profile == profile.name);
            if (entry == null) return;
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(entry.settings), profile);
            if (!entry.hasFeed) profile.Feed = null;
            if (!string.IsNullOrEmpty(entry.observeSource))
                profile.Observe = CopyClip(entry.observeSource, null, entry.profile + "_Observe");
            if (!string.IsNullOrEmpty(entry.restSource))
                profile.Rest = CopyClip(entry.restSource, null, entry.profile + "_Rest");
            EditorUtility.SetDirty(profile);
        }

        /// <summary>기존 GUID와 모델을 보존하며 조사한 생활 데이터만 명시적으로 적용한다.</summary>
        [UPlaygroundTool("Wildlife/관찰 행동 데이터 적용")]
        public static void ApplyAnimalBehavior()
        {
            if (Application.isPlaying) throw new InvalidOperationException("편집 모드에서 실행해야 합니다.");
            var catalog = JsonUtility.FromJson<BehaviorCatalog>(File.ReadAllText(BehaviorCatalogPath));
            // 참조 누락을 먼저 검사하여 중간 실패로 일부 종만 갱신되는 것을 막는다.
            foreach (var entry in catalog.profiles)
            {
                if (AssetDatabase.LoadAssetAtPath<WildlifeProfileSO>(DataRoot + "/" + entry.profile + ".asset") == null)
                    throw new InvalidOperationException(entry.profile + ": 기존 프로필 누락");
                foreach (var source in new[] { entry.observeSource, entry.restSource })
                    if (!string.IsNullOrEmpty(source) && !AssetDatabase.LoadAllAssetsAtPath(source).OfType<AnimationClip>().Any(c => !c.name.StartsWith("__preview")))
                        throw new InvalidOperationException(source + ": 원본 클립 누락");
            }
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            try
            {
                foreach (var entry in catalog.profiles)
                {
                    var profile = AssetDatabase.LoadAssetAtPath<WildlifeProfileSO>(DataRoot + "/" + entry.profile + ".asset");
                    Undo.RecordObject(profile, "동물 생활 데이터 적용");
                    ApplyInitialBehavior(profile);
                    AssetDatabase.SaveAssetIfDirty(profile);
                }
                Undo.CollapseUndoOperations(group);
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                AssetDatabase.SaveAssets();
                throw;
            }
        }
    }
}
