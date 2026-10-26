using System;
using System.IO;
using System.Linq;
using Animancer;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UPlayGround.Data.World;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.World.LivingWorld;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>원본 패키지를 보존하면서 환경 생물 프리팹과 배치표를 연결한다.</summary>
    public static class WildlifeAuthoring
    {
        // 필드가 베이크하는 기본 타입을 사용한다. 원본 패키지의 전용 타입은 프로젝트에 없다.
        private const int GroundAgentTypeId = 0;
        private const string DataRoot = "Assets/10.Datas/World/Wildlife";
        private const string PrefabRoot = "Assets/03.Prefabs/Environment/Wildlife";
        private const string ButterflyRoot = "Assets/ExternalAssets/Environment/Idyllic Fantasy Nature";
        private const string BirdRoot = "Assets/ExternalAssets/Animals/SoltorchAmericanRobin";
        private const string BirdPath = BirdRoot + "/American_Robin_Mesh.fbx";
        private const string WolfPath = "Assets/08.Models/Wildlife/WoodlandWolf.fbx";
        private const string LayoutPath = DataRoot + "/LakeOfLife.wildlife.json";
        [Serializable] private sealed class GroundCatalog { public GroundSpecies[] species; }
        [Serializable] private sealed class GroundSpecies
        {
            public string profile, prefab, source;
            public string[] animationPaths, clipNames;
            public float roamRadius, moveSpeed, fleeSpeed, alertDistance, calmDistance;
        }
        [Serializable] private sealed class Layout { public string scene; public Placement[] placements; }
        [Serializable] private sealed class Placement
        {
            public string id, prefab;
            public Vector3 position;
            public float yaw, scale = 1;
        }

        /// <summary>기존 생물과 신규 모델을 카탈로그·기본 모션에 연결한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/에셋 생성")]
        public static void BuildAssets()
        {
            Directory.CreateDirectory(DataRoot + "/Clips");
            Directory.CreateDirectory(PrefabRoot);
            AssetDatabase.Refresh();
            var catalog = JsonUtility.FromJson<GroundCatalog>(File.ReadAllText(DataRoot + "/GroundSpecies.catalog.json"));
            foreach (var species in catalog.species) BuildGroundSpecies(species);
            for (int i = 1; i <= 3; i++)
            {
                string name = "Butterfly_0" + i;
                var profile = LoadOrCreateProfile(name);
                if (profile.Idle == null)
                {
                    profile.IsFlying = true;
                    profile.RoamRadius = 2.5f;
                    profile.FleeRadius = profile.RoamRadius;
                    profile.MoveSpeed = 0.4f;
                    profile.FleeSpeed = 0.9f;
                    profile.AlertDistance = 1.5f;
                    profile.CalmDistance = 3;
                    profile.SimulationDistance = 45;
                    profile.FlightHeightVariation = 0.6f;
                    profile.Idle = profile.Move = profile.Flee = CopyClip(ButterflyRoot + "/Models/Butterfly/Butterfly.fbx", "Armature|Fly_0" + i, name + "_Fly");
                    EditorUtility.SetDirty(profile);
                }
                BuildPrefab(name, ButterflyRoot + "/Prefabs/Code Related/" + name + ".prefab", profile);
            }
            var bird = LoadOrCreateProfile("Songbird");
            if (bird.Idle == null)
            {
                bird.IsFlying = true;
                bird.CanLand = true;
                bird.FleeRadius = 20;
                bird.CalmSeconds = 4;
                bird.LandingMask = LayerMask.GetMask("Ground");
                bird.RoamRadius = 9;
                bird.MoveSpeed = 2;
                bird.FleeSpeed = 4;
                bird.AlertDistance = 4;
                bird.CalmDistance = 9;
                bird.FlightHeightVariation = 2;
                bird.Idle = CopyClip(BirdPath, "A_Idle_01", "Songbird_Idle");
                bird.Move = bird.Flee = CopyClip(BirdPath, "A_Fly_01", "Songbird_Fly");
                EditorUtility.SetDirty(bird);
            }
            BuildPrefab("Songbird", BirdPath, bird);
            AssetDatabase.SaveAssets();
            Debug.Log("환경 생물 카탈로그와 기본 클립 연결을 확인했습니다.");
        }

        private static void BuildGroundSpecies(GroundSpecies species)
        {
            if (species.animationPaths == null || species.animationPaths.Length != 4
                || species.clipNames == null || species.clipNames.Length != 4)
                throw new InvalidOperationException(species.prefab + ": 대기·걷기·달리기·먹이 클립 4개가 필요합니다.");
            var profile = LoadOrCreateProfile(species.profile);
            if (profile.Idle == null)
            {
                string[] suffixes = { "Idle", "Walk", "Run", "Feed" };
                var clips = new AnimationClip[4];
                for (int i = 0; i < clips.Length; i++)
                    clips[i] = CopyClip(species.animationPaths[i], string.IsNullOrEmpty(species.clipNames[i]) ? null : species.clipNames[i], species.profile + "_" + suffixes[i]);
                profile.Idle = clips[0]; profile.Move = clips[1]; profile.Flee = clips[2]; profile.Feed = clips[3];
                profile.RoamRadius = species.roamRadius;
                profile.MoveSpeed = species.moveSpeed;
                profile.FleeSpeed = species.fleeSpeed;
                profile.AlertDistance = species.alertDistance;
                profile.CalmDistance = species.calmDistance;
                EditorUtility.SetDirty(profile);
            }
            BuildPrefab(species.prefab, species.source, profile);
        }

        private static WildlifeProfileSO LoadOrCreateProfile(string name)
        {
            string path = DataRoot + "/" + name + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<WildlifeProfileSO>(path);
            if (profile != null) return profile;
            profile = ScriptableObject.CreateInstance<WildlifeProfileSO>();
            AssetDatabase.CreateAsset(profile, path);
            return profile;
        }

        /// <summary>새 모델 재출력 후 기존 프리팹·클립의 GUID와 컴포넌트를 보존하여 연결을 갱신한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/새 모델 동기화")]
        public static void RefreshSongbird()
        {
            if (Application.isPlaying) throw new InvalidOperationException("편집 모드에서 모델을 동기화해야 합니다.");
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(BirdPath);
            if (source == null) throw new InvalidOperationException("새 원본 모델이 없습니다.");
            var sourceRenderer = source.GetComponentInChildren<SkinnedMeshRenderer>();
            if (sourceRenderer == null || sourceRenderer.sharedMesh == null || sourceRenderer.bones.Any(b => b == null))
                throw new InvalidOperationException("새 원본 메시 또는 본 연결이 없습니다.");
            string[] clipNames = { "Idle", "Fly" };
            string[] sourceClipNames = { "A_Idle_01", "A_Fly_01" };
            var sourceClips = AssetDatabase.LoadAllAssetsAtPath(BirdPath).OfType<AnimationClip>().ToArray();
            var clips = sourceClipNames.Select(n => sourceClips.Single(c => c.name == n)).ToArray();
            var targets = clipNames.Select(n => AssetDatabase.LoadAssetAtPath<AnimationClip>(DataRoot + "/Clips/Songbird_" + n + ".anim")).ToArray();
            if (targets.Any(c => c == null)) throw new InvalidOperationException("기존 새 클립이 없습니다.");

            string path = PrefabRoot + "/Songbird.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // 모델의 자식 계층만 교체해 씬이 참조하는 루트와 생물 컴포넌트의 식별자를 보존한다.
                var visual = UnityEngine.Object.Instantiate(source);
                try
                {
                    foreach (Transform child in root.transform.Cast<Transform>().ToArray())
                        UnityEngine.Object.DestroyImmediate(child.gameObject);
                    foreach (Transform child in visual.transform.Cast<Transform>().ToArray())
                        child.SetParent(root.transform, false);
                }
                finally { UnityEngine.Object.DestroyImmediate(visual); }
                root.GetComponent<Animator>().avatar = null;
                var renderer = root.GetComponentInChildren<SkinnedMeshRenderer>();
                ApplyBirdMaterials(root);
                for (int i = 0; i < targets.Length; i++)
                {
                    EditorUtility.CopySerialized(clips[i], targets[i]);
                    targets[i].name = "Songbird_" + clipNames[i];
                    var settings = AnimationUtility.GetAnimationClipSettings(targets[i]);
                    settings.loopTime = true;
                    settings.keepOriginalPositionXZ = settings.keepOriginalPositionY = settings.keepOriginalOrientation = true;
                    AnimationUtility.SetAnimationClipSettings(targets[i], settings);
                    AnimationUtility.SetAnimationEvents(targets[i], Array.Empty<AnimationEvent>());
                    EditorUtility.SetDirty(targets[i]);
                }
                // 접힌 첫 자세만으로 경계를 저장하면 펼친 날개가 화면 가장자리에서 사라진다.
                var baked = new Mesh();
                try
                {
                    Bounds bounds = renderer.localBounds;
                    foreach (var clip in targets)
                    {
                        for (int frame = 0; frame < 24; frame++)
                        {
                            clip.SampleAnimation(root, clip.length * frame / 24f);
                            renderer.BakeMesh(baked, true);
                            bounds.Encapsulate(baked.bounds);
                        }
                    }
                    renderer.localBounds = bounds;
                }
                finally { UnityEngine.Object.DestroyImmediate(baked); }
                // 편집 화면에서도 날개를 접은 모습을 보이며 런타임 비행은 기존 프로필이 결정한다.
                targets[0].SampleAnimation(root, 0);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                foreach (var clip in targets) AssetDatabase.SaveAssetIfDirty(clip);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static AnimationClip CopyClip(string path, string clipName, string name)
        {
            string target = DataRoot + "/Clips/" + name + ".anim";
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(target);
            if (clip != null) return clip;
            var source = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>()
                .First(c => !c.name.StartsWith("__preview__") && (clipName == null || c.name == clipName));
            clip = UnityEngine.Object.Instantiate(source);
            clip.name = name;
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            settings.keepOriginalPositionXZ = true;
            settings.keepOriginalPositionY = true;
            settings.keepOriginalOrientation = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AnimationUtility.SetAnimationEvents(clip, Array.Empty<AnimationEvent>());
            if (name.StartsWith("Butterfly_", StringComparison.Ordinal))
            {
                // 원본의 이동·출몰 스케일은 서식지 이동과 중복되므로 몸통만 최초 자세로 고정한다.
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                {
                    if (binding.path != "Armature/Bone") continue;
                    var curve = AnimationUtility.GetEditorCurve(clip, binding);
                    AnimationUtility.SetEditorCurve(clip, binding, AnimationCurve.Constant(0, clip.length, curve.Evaluate(0)));
                }
            }
            AssetDatabase.CreateAsset(clip, target);
            return clip;
        }

        private static void BuildPrefab(string name, string source, WildlifeProfileSO profile)
        {
            string target = PrefabRoot + "/" + name + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(target) != null) return;
            var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(source));
            root.name = name;
            root.SetActive(false);
            try
            {
                foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true))
                    UnityEngine.Object.DestroyImmediate(script);
                foreach (var collider in root.GetComponentsInChildren<Collider>(true))
                    UnityEngine.Object.DestroyImmediate(collider);
                var animator = root.GetComponentInChildren<Animator>(true);
                if (animator == null) animator = root.AddComponent<Animator>();
                animator.runtimeAnimatorController = null;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                var animation = animator.gameObject.AddComponent<AnimancerComponent>();
                animation.Animator = animator;
                animation.ActionOnDisable = AnimancerComponent.DisableAction.Pause;
                var agent = root.GetComponent<NavMeshAgent>();
                if (!profile.IsFlying)
                {
                    if (agent == null) agent = root.AddComponent<NavMeshAgent>();
                    agent.agentTypeID = GroundAgentTypeId;
                    agent.speed = profile.MoveSpeed;
                    agent.angularSpeed = profile.TurnSpeed;
                    agent.acceleration = 8;
                    agent.radius = 0.35f;
                    agent.height = 1.5f;
                    agent.stoppingDistance = 0.3f;
                    agent.autoTraverseOffMeshLink = false;
                }
                else if (agent != null) { UnityEngine.Object.DestroyImmediate(agent); agent = null; }
                if (source.StartsWith(ButterflyRoot, StringComparison.Ordinal)) root.transform.localScale *= 10;
                if (source == BirdPath) ApplyBirdMaterials(root);
                if (source == WolfPath) ApplyWolfMaterials(root);
                var creature = root.AddComponent<WildlifeCreature>();
                var serialized = new SerializedObject(creature);
                serialized.FindProperty("_profile").objectReferenceValue = profile;
                serialized.FindProperty("_animation").objectReferenceValue = animation;
                serialized.FindProperty("_agent").objectReferenceValue = agent;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                root.SetActive(true);
                PrefabUtility.SaveAsPrefabAsset(root, target);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void ApplyBirdMaterials(GameObject root)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit 셰이더를 찾을 수 없습니다.");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(BirdRoot + "/American_Robin_Tex.png");
            if (texture == null) throw new InvalidOperationException("Soltorch 울새 원본 텍스처가 없습니다.");
            var feather = CreateMaterial("Songbird_AmericanRobin", shader, Color.white, texture);
            feather.SetTexture("_BaseMap", texture);
            feather.SetTexture("_MainTex", texture);
            EditorUtility.SetDirty(feather);
            AssetDatabase.SaveAssetIfDirty(feather);
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => feather).ToArray();
        }

        private static void ApplyWolfMaterials(GameObject root)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) throw new InvalidOperationException("URP Lit 셰이더를 찾을 수 없습니다.");
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/04.Images/Environment/Wildlife/Wolf_Fur.png");
            var materials = new System.Collections.Generic.Dictionary<string, Material>
            {
                ["Wolf_Fur"] = CreateMaterial("Wolf_Fur", shader, Color.white, texture),
                ["Wolf_Nose"] = CreateMaterial("Wolf_Nose", shader, new Color(0.018f, 0.022f, 0.026f), null),
                ["Wolf_Eyes"] = CreateMaterial("Wolf_Eyes", shader, new Color(0.55f, 0.25f, 0.035f), null)
            };
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                renderer.sharedMaterials = renderer.sharedMaterials.Select(m => materials[m.name]).ToArray();
        }

        private static Material CreateMaterial(string name, Shader shader, Color color, Texture texture)
        {
            string path = DataRoot + "/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(shader) { name = name };
            material.SetColor("_BaseColor", color);
            material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Smoothness", 0.15f);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        /// <summary>배치표의 누락 개체만 추가하여 기존 수동 수정과 이름을 보존한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/생명의 호수 배치")]
        public static void PlaceLakeWildlife()
        {
            var layout = JsonUtility.FromJson<Layout>(File.ReadAllText(LayoutPath));
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != layout.scene) throw new InvalidOperationException("배치표에 지정된 씬을 먼저 열어 주세요.");
            if (Application.isPlaying) throw new InvalidOperationException("편집 모드에서 배치해야 합니다.");
            var root = scene.GetRootGameObjects().FirstOrDefault(g => g.name == "FieldWildlife");
            if (root == null)
            {
                root = new GameObject("FieldWildlife");
                root.AddComponent<WildlifeHabitat>();
                Undo.RegisterCreatedObjectUndo(root, "환경 생물 서식지 생성");
            }
            foreach (var entry in layout.placements)
            {
                if (root.transform.Find(entry.id) != null) continue;
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabRoot + "/" + entry.prefab + ".prefab");
                if (prefab == null) throw new InvalidOperationException(entry.prefab + " 프리팹이 없습니다.");
                var creature = prefab.GetComponent<WildlifeCreature>();
                Vector3 position = entry.position;
                if (creature == null || !creature.Profile.IsFlying)
                {
                    var agent = prefab.GetComponent<NavMeshAgent>();
                    if (agent == null && prefab.GetComponent<MonsterActor>() == null)
                        throw new InvalidOperationException(entry.id + ": 이동 컴포넌트 누락");
                    var filter = new NavMeshQueryFilter
                    {
                        agentTypeID = agent != null ? agent.agentTypeID : GroundAgentTypeId,
                        areaMask = agent != null ? agent.areaMask : NavMesh.AllAreas
                    };
                    if (!NavMesh.SamplePosition(position, out var hit, 3, filter))
                        throw new InvalidOperationException(entry.id + " 배치 지점에 해당 Agent Type의 NavMesh가 없습니다.");
                    position = hit.position;
                }
                var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root.transform);
                Undo.RegisterCreatedObjectUndo(instance, "환경 생물 배치");
                instance.name = entry.id;
                instance.transform.SetPositionAndRotation(position, Quaternion.Euler(0, entry.yaw, 0));
                instance.transform.localScale *= entry.scale;
                PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
            }
            EditorSceneManager.MarkSceneDirty(scene);
        }

        /// <summary>실제로 로드한 프리팹·클립·씬 연결과 사슴 이동 경로를 검사한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/연결 검증")]
        public static void Validate()
        {
            var failures = new System.Collections.Generic.List<string>();
            var prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabRoot });
            foreach (string guid in prefabs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                        failures.Add(path + ": Missing Script");
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    if (renderer.sharedMaterials.Any(m => m == null || m.shader == null || !m.shader.isSupported))
                        failures.Add(path + ": ????/??? ??");
                var monster = root.GetComponent<MonsterActor>();
                if (monster != null)
                {
                    if (monster.Definition == null || monster.Definition.EffectiveAbilitySet == null
                        || monster.Definition.EffectiveBehaviorData == null)
                        failures.Add(path + ": 전투 생물 Definition·Ability·BT 연결 누락");
                    if (root.GetComponent<WildlifeCreature>() != null || root.GetComponent<NavMeshAgent>() != null)
                        failures.Add(path + ": 전투 이동과 장식 생물 이동의 중복");
                    continue;
                }
                var creature = root.GetComponent<WildlifeCreature>();
                if (creature == null || creature.Profile == null) { failures.Add(path + ": 프로필 누락"); continue; }
                var profile = creature.Profile;
                if (!profile.IsFlying)
                {
                    var agent = root.GetComponent<NavMeshAgent>();
                    if (agent == null) failures.Add(path + ": NavMeshAgent 누락");
                    else if (NavMesh.GetSettingsByID(agent.agentTypeID).agentTypeID != agent.agentTypeID)
                        failures.Add(path + ": 프로젝트에 없는 NavMesh Agent Type " + agent.agentTypeID);
                }
                if (profile.Idle == null || profile.Move == null || profile.Flee == null) failures.Add(path + ": 기본 클립 누락");
                if (profile.CalmDistance < profile.AlertDistance) failures.Add(path + ": 경계 해제 거리가 감지보다 짧음");
                foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                        failures.Add(path + ": Missing Script");
                foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
                    if (renderer.sharedMaterials.Any(m => m == null || m.shader == null || !m.shader.isSupported))
                        failures.Add(path + ": 머티리얼/셰이더 누락");
                if (root.GetComponentsInChildren<Collider>(true).Length != 0) failures.Add(path + ": 통행 방해 Collider");
                if (root.GetComponentsInChildren<GameActor>(true).Length != 0) failures.Add(path + ": 전투 액터 등록");
            }
            var habitat = UnityEngine.Object.FindFirstObjectByType<WildlifeHabitat>();
            int instances = habitat == null ? 0 : habitat.GetComponentsInChildren<WildlifeCreature>(true).Length
                + habitat.GetComponentsInChildren<MonsterActor>(true).Length;
            if (habitat == null) failures.Add("열린 씬에 서식지가 없습니다.");
            else
            {
                foreach (var creature in habitat.GetComponentsInChildren<WildlifeCreature>(true))
                {
                    if (creature.Profile == null) { failures.Add(creature.name + ": 프로필 누락"); continue; }
                    if (creature.Profile.IsFlying) continue;
                    var agent = creature.GetComponent<NavMeshAgent>();
                    if (agent == null) { failures.Add(creature.name + ": NavMeshAgent 누락"); continue; }
                    var filter = new NavMeshQueryFilter { agentTypeID = agent.agentTypeID, areaMask = agent.areaMask };
                    if (!NavMesh.SamplePosition(creature.transform.position, out _, 0.5f, filter))
                        failures.Add(creature.name + ": 해당 Agent Type의 NavMesh 접지 누락");
                    if (Application.isPlaying && creature.isActiveAndEnabled && (!agent.enabled || !agent.isOnNavMesh))
                        failures.Add(creature.name + ": 런타임 NavMeshAgent 연결 실패");
                }
            }
            string result = "프리팹: " + prefabs.Length + " / 씬 개체: " + instances + " / 오류: " + failures.Count;
            Directory.CreateDirectory("output/Wildlife");
            File.WriteAllText("output/Wildlife/validation.txt", result + "\n" + string.Join("\n", failures));
            if (failures.Count != 0) throw new InvalidOperationException(result + "\n" + string.Join("\n", failures));
            Debug.Log(result);
        }
    }
}
