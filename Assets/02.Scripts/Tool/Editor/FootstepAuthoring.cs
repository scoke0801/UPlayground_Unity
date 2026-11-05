using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UPlayGround.Animation;
using UPlayGround.Components;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Event;
using UPlayGround.Data.Sound;
using UPlayGround.EditorTools;
using Motion = UPlayGround.Animation.Motion;

namespace UPlayGround.Tool.Editor
{
    /// <summary>라온의 첫 발 디딤 콘텐츠를 기존 리소스에서 만들고 월드 지면에 연결한다.</summary>
    public static class FootstepAuthoring
    {
        public const string Root = "Assets/10.Datas/Actor/Footsteps";
        public const string ModelPath = "Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab";
        private const string SourceSet = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Traversal/SuperJumpGliding/Raon_Traversal_PlayerAnimationSet.asset";
        private const string AudioRoot = "Assets/ExternalAssets/SFX/Footsteps - Essentials";
        private const string WorldPath = "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity";

        [Serializable]
        private sealed class ContactRecord
        {
            public string source;
            public string destination;
            public string clip;
            public string kind;
            public string[] contacts;
        }

        [Serializable]
        private sealed class AuthoringReport
        {
            public List<ContactRecord> motions = new();
        }

        [UPlaygroundTool("Footsteps/라온 첫 콘텐츠 제작")]
        public static void Build()
        {
            RequireEditMode();
            Directory.CreateDirectory(Root + "/Motions");
            Directory.CreateDirectory(Root + "/Sounds");
            Directory.CreateDirectory(Root + "/Effects");
            AssetDatabase.Refresh();
            var report = new AuthoringReport();
            var dust = CreateDust();
            var splash = CreateWaterEffect("WaterSplash", false);
            var ripple = CreateWaterEffect("WaterRipple", true);
            var dirt = CreateSurface("Dirt", "DirtyGround", dust, null, 0.4f, 0.7f, 1.1f);
            var grass = CreateSurface("Grass", "Grass", null, null, 0f, 0f, 0.65f);
            grass.landing.effect = dust;
            var stone = CreateSurface("Stone", "Rock", null, null, 0f, 0f, 0.35f);
            stone.landing.effect = dust;
            var water = CreateSurface("ShallowWater", "Water", splash, ripple, 0.6f, 0.85f, 1.3f, "_Jump_Light_");
            var settings = LoadOrCreate<FootstepSettingsSO>(Root + "/Raon_FootstepSettings.asset");
            settings.surfaces = new[] { dirt, grass, stone, water };
            settings.fallbackSurface = stone;
            settings.groundLayers = LayerMask.GetMask("Ground");
            if (settings.groundLayers == 0) throw new InvalidOperationException("Ground 레이어가 없습니다.");
            EditorUtility.SetDirty(settings);
            RegisterSounds(settings);

            var source = AssetDatabase.LoadAssetAtPath<PlayerActorAnimationMotionSet>(SourceSet);
            if (source == null) throw new InvalidOperationException("현재 라온의 이동 모션 세트가 없습니다.");
            string setPath = Root + "/Raon_Footstep_PlayerAnimationSet.asset";
            var destination = CopyAsset<PlayerActorAnimationMotionSet>(SourceSet, setPath);
            var copiedMotions = new Dictionary<MotionSetAsset, MotionSetAsset>();
            var copiedSets = new Dictionary<ActorAnimationMotionSet, ActorAnimationMotionSet>();
            var previewScene = EditorSceneManager.NewPreviewScene();
            GameObject preview = null;
            bool startedAnimationMode = false;
            try
            {
                preview = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ModelPath), previewScene);
                preview.hideFlags = HideFlags.HideAndDontSave;
                foreach (MonoBehaviour behaviour in preview.GetComponentsInChildren<MonoBehaviour>(true)) behaviour.enabled = false;
                Animator animator = preview.GetComponent<Animator>();
                if (animator == null || !animator.isHuman) throw new InvalidOperationException("라온 휴머노이드 Avatar가 없습니다.");
                if (AnimationMode.InAnimationMode()) throw new InvalidOperationException("기존 애니메이션 프리뷰를 닫은 뒤 실행하세요.");
                AnimationMode.StartAnimationMode();
                startedAnimationMode = true;
                foreach (var pair in source.motionSets)
                {
                    if (pair.Value == null) continue;
                    if (pair.Key != UPlayGround.Data.EnumType.WeaponType.NoWeapon
                        && pair.Key != UPlayGround.Data.EnumType.WeaponType.Katana)
                    {
                        destination.motionSets[pair.Key] = pair.Value;
                        continue;
                    }
                    if (!copiedSets.TryGetValue(pair.Value, out var actorSet))
                    {
                        string path = Root + "/Motions/Set_" + pair.Key + ".asset";
                        actorSet = CopyAsset<ActorAnimationMotionSet>(AssetDatabase.GetAssetPath(pair.Value), path);
                        copiedSets.Add(pair.Value, actorSet);
                    }
                    destination.motionSets[pair.Key] = actorSet;
                    var tags = new[] { MotionTags.Walk, MotionTags.Run, MotionTags.Sprint, MotionTags.Land };
                    for (int i = 0; i < tags.Length; i++)
                    {
                        MotionSetAsset original = source.GetMotionSetAsset(pair.Key, tags[i]);
                        if (original == null) continue;
                        if (!copiedMotions.TryGetValue(original, out var motion))
                        {
                            string originalPath = AssetDatabase.GetAssetPath(original);
                            string path = Root + "/Motions/" + original.name + "_" + AssetDatabase.AssetPathToGUID(originalPath).Substring(0, 8) + ".asset";
                            motion = CopyAsset<MotionSetAsset>(originalPath, path);
                            BakeContacts(motion, originalPath, animator, i == 0 ? FootstepKind.Walk : i == 3 ? FootstepKind.Landing : FootstepKind.Run, report);
                            copiedMotions.Add(original, motion);
                        }
                        actorSet.motionSlots ??= new();
                        actorSet.motionSlots[tags[i]] = motion;
                    }
                    EditorUtility.SetDirty(actorSet);
                }
                EditorUtility.SetDirty(destination);
            }
            finally
            {
                if (startedAnimationMode) AnimationMode.StopAnimationMode();
                if (preview != null) UnityEngine.Object.DestroyImmediate(preview);
                EditorSceneManager.ClosePreviewScene(previewScene);
            }
            GameObject model = PrefabUtility.LoadPrefabContents(ModelPath);
            try
            {
                var receiver = model.GetComponent<ActorFootstep>() ?? model.AddComponent<ActorFootstep>();
                receiver.settings = settings;
                var animator = new SerializedObject(model.GetComponent<PlayerActorAnimator>());
                animator.FindProperty("_playerActorAnimationMotionSet").objectReferenceValue = destination;
                animator.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(model, ModelPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(model); }
            SaveAuthoredAssets();
            Directory.CreateDirectory("output/Footsteps");
            File.WriteAllText("output/Footsteps/authoring.json", JsonUtility.ToJson(report, true));
        }

        private static void BakeContacts(MotionSetAsset asset, string source, Animator animator, FootstepKind kind, AuthoringReport report)
        {
            var record = new ContactRecord { source = source, destination = AssetDatabase.GetAssetPath(asset), kind = kind.ToString() };
            var contacts = new List<string>();
            foreach (Motion motion in asset.motionSet.motions)
            {
                motion.events ??= new();
                motion.events.RemoveAll(e => e is FootstepEvent step && step.requireSurfaceProfile);
                record.clip = AssetDatabase.GetAssetPath(motion.motionClip) + "/" + motion.motionClip.name;
                if (kind == FootstepKind.Landing)
                {
                    motion.events.Add(CreateEvent(0f, FootstepEvent.Foot.Left, kind));
                    contacts.Add("Landing:0");
                    continue;
                }
                int samples = Mathf.Max(12, Mathf.CeilToInt(motion.Duration * 60f));
                var left = new float[samples];
                var right = new float[samples];
                Transform leftBone = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
                Transform rightBone = animator.GetBoneTransform(HumanBodyBones.RightFoot);
                for (int i = 0; i < samples; i++)
                {
                    AnimationMode.BeginSampling();
                    AnimationMode.SampleAnimationClip(animator.gameObject, motion.motionClip,
                        motion.ClipStartTime + i * motion.Duration / samples * motion.playbackSpeed);
                    AnimationMode.EndSampling();
                    left[i] = leftBone.position.y - animator.transform.position.y;
                    right[i] = rightBone.position.y - animator.transform.position.y;
                }
                AddContacts(motion, left, FootstepEvent.Foot.Left, kind, contacts);
                AddContacts(motion, right, FootstepEvent.Foot.Right, kind, contacts);
                if (motion.events.Count(e => e is FootstepEvent) < 2)
                    throw new InvalidOperationException("양발 접촉을 검출하지 못했습니다: " + source);
                motion.events.Sort((a, b) => a.startTime.CompareTo(b.startTime));
            }
            record.contacts = contacts.ToArray();
            report.motions.Add(record);
            EditorUtility.SetDirty(asset);
        }

        private static void AddContacts(Motion motion, float[] heights, FootstepEvent.Foot foot, FootstepKind kind, List<string> contacts)
        {
            float minimum = heights.Min();
            float maximum = heights.Max();
            if (maximum - minimum < 0.015f) return;
            float threshold = Mathf.Lerp(minimum, maximum, 0.16f);
            float last = -1f;
            for (int i = 0; i < heights.Length; i++)
            {
                int previous = (i + heights.Length - 1) % heights.Length;
                if (heights[previous] <= threshold || heights[i] > threshold) continue;
                float time = i * motion.Duration / heights.Length;
                if (time - last < 0.12f) continue;
                last = time;
                motion.events.Add(CreateEvent(time, foot, kind));
                contacts.Add(foot + ":" + time.ToString("F3", System.Globalization.CultureInfo.InvariantCulture));
            }
        }

        private static FootstepEvent CreateEvent(float time, FootstepEvent.Foot foot, FootstepKind kind) => new()
        {
            startTime = time, endTime = time, foot = foot, kind = kind, volume = 1f,
            requireSurfaceProfile = true, reentryPolicy = MotionEventReentryPolicy.EveryCrossing,
            evaluationPhase = MotionEventEvaluationPhase.PostAnimationEvaluation
        };

        private static FootstepSurfaceProfileSO CreateSurface(string id, string folder, GameObject effect, GameObject secondary, float walkScale, float runScale, float landScale, string landingToken = "_Land_")
        {
            var surface = LoadOrCreate<FootstepSurfaceProfileSO>(Root + "/Surface_" + id + ".asset");
            surface.walk = CreateResponse(folder, "Walk", effect, secondary, walkScale, 0.5f);
            surface.run = CreateResponse(folder, "Run", effect, secondary, runScale, 0.65f);
            surface.landing = CreateResponse(folder, "Land", effect, secondary, landScale, 0.8f, landingToken);
            EditorUtility.SetDirty(surface);
            return surface;
        }

        private static FootstepSurfaceProfileSO.Response CreateResponse(string surface, string action, GameObject effect, GameObject secondary, float scale, float volume, string sourceToken = null)
        {
            string folder = AudioRoot + "/Footsteps_" + surface;
            var clips = AssetDatabase.FindAssets("t:AudioClip", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath)
                .Where(p => p.Contains(sourceToken ?? "_" + action + "_"))
                .OrderBy(p => p, StringComparer.Ordinal).Take(4).ToArray();
            if (clips.Length == 0) throw new InvalidOperationException("발소리 원본 누락: " + surface + "/" + action);
            var sounds = new SoundEntrySO[clips.Length];
            for (int i = 0; i < sounds.Length; i++)
            {
                var sound = LoadOrCreate<SoundEntrySO>(Root + "/Sounds/Footstep_" + surface + "_" + action + "_" + i + ".asset");
                sound.key = "Footstep." + surface + "." + action + "." + i;
                sound.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(clips[i]);
                sound.bus = SoundBusType.SFX;
                sound.distanceMode = SoundDistanceMode.Logarithmic3D;
                sound.volume = 1f;
                sound.pitchMin = 0.96f; sound.pitchMax = 1.04f;
                sound.minDistance = 1.5f; sound.maxDistance = 18f;
                sound.cooldown = 0f; sound.maxSimultaneous = 4; sound.priority = 160;
                EditorUtility.SetDirty(sound);
                sounds[i] = sound;
            }
            return new FootstepSurfaceProfileSO.Response { sounds = sounds, effect = effect, secondaryEffect = secondary, scale = scale, volume = volume };
        }

        private static void RegisterSounds(FootstepSettingsSO settings)
        {
            var database = AssetDatabase.LoadAssetAtPath<SoundDatabaseSO>("Assets/10.Datas/Sound/SoundDatabase.asset");
            var serialized = new SerializedObject(database);
            SerializedProperty entries = serialized.FindProperty("entries");
            var registered = new HashSet<UnityEngine.Object>();
            for (int i = 0; i < entries.arraySize; i++) registered.Add(entries.GetArrayElementAtIndex(i).objectReferenceValue);
            foreach (var surface in settings.surfaces)
            foreach (var response in new[] { surface.walk, surface.run, surface.landing })
            foreach (var sound in response.sounds)
                if (registered.Add(sound))
                {
                    int index = entries.arraySize++;
                    entries.GetArrayElementAtIndex(index).objectReferenceValue = sound;
                }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject CreateDust()
        {
            const string sourcePath = "Assets/ExternalAssets/FX/Magic effects pack/Prefabs/Smoke effects/Dust ground.prefab";
            var source = AssetDatabase.LoadAssetAtPath<GameObject>(sourcePath);
            var sourceMaterial = source.GetComponent<ParticleSystemRenderer>().sharedMaterial;
            var material = LoadOrCreateMaterial(Root + "/Effects/FootstepDust.mat", Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetTexture("_BaseMap", sourceMaterial.mainTexture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 0f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            var root = new GameObject("FootstepDust", typeof(ParticleSystem));
            try
            {
                var particles = root.GetComponent<ParticleSystem>();
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.loop = false; main.playOnAwake = false; main.duration = 0.12f;
                main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.4f);
                main.startSpeed = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
                main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.3f);
                main.startColor = new Color(0.57f, 0.49f, 0.35f, 0.2f);
                main.maxParticles = 10; main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                var emission = particles.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 5) });
                var shape = particles.shape; shape.shapeType = ParticleSystemShapeType.Hemisphere; shape.radius = 0.12f;
                var color = particles.colorOverLifetime;
                color.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.12f), new GradientAlphaKey(0f, 1f) });
                color.color = gradient;
                root.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
                return PrefabUtility.SaveAsPrefabAsset(root, Root + "/Effects/FootstepDust.prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static GameObject CreateWaterEffect(string name, bool isRipple)
        {
            string texturePath = isRipple
                ? "Assets/ExternalAssets/FX/Anime VFX URP/Shared/Textures/T_VFX_Ring_01.png"
                : "Assets/ExternalAssets/FX/Cartoon FX Remaster/CFXR Assets/Graphics/cfxr water drop blur anim.png";
            if (!isRipple)
            {
                // 원본은 R 채널 전용 BC4다. URP의 RGB 샘플러에서 붉게 보이지 않도록 복제본만 RGBA로 가져온다.
                string convertedPath = Root + "/Effects/WaterDrops.png";
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(convertedPath) == null
                    && !AssetDatabase.CopyAsset(texturePath, convertedPath))
                    throw new InvalidOperationException("물방울 텍스처 복제 실패");
                var importer = (TextureImporter)AssetImporter.GetAtPath(convertedPath);
                if (importer.textureType != TextureImporterType.Default || importer.alphaSource != TextureImporterAlphaSource.FromGrayScale)
                {
                    importer.textureType = TextureImporterType.Default;
                    importer.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                    importer.SaveAndReimport();
                }
                texturePath = convertedPath;
            }
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null) throw new InvalidOperationException("수면 입자 텍스처 누락: " + texturePath);
            var material = LoadOrCreateMaterial(Root + "/Effects/Footstep" + name + ".mat", Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_Blend", 2f);
            material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.One);
            material.SetFloat("_ZWrite", 0f);
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = 3000;
            EditorUtility.SetDirty(material);
            var root = new GameObject("Footstep" + name, typeof(ParticleSystem));
            try
            {
                var particles = root.GetComponent<ParticleSystem>();
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particles.main;
                main.loop = false; main.playOnAwake = false; main.duration = 0.1f;
                main.stopAction = ParticleSystemStopAction.None;
                main.scalingMode = ParticleSystemScalingMode.Hierarchy;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.maxParticles = isRipple ? 2 : 12;
                main.startLifetime = isRipple ? new ParticleSystem.MinMaxCurve(0.5f)
                    : new ParticleSystem.MinMaxCurve(0.18f, 0.35f);
                main.startSize = isRipple ? new ParticleSystem.MinMaxCurve(0.5f)
                    : new ParticleSystem.MinMaxCurve(0.015f, 0.045f);
                main.startSpeed = isRipple ? new ParticleSystem.MinMaxCurve(0f)
                    : new ParticleSystem.MinMaxCurve(0.5f, 1.2f);
                main.gravityModifier = isRipple ? 0f : 0.6f;
                main.startColor = new Color(0.65f, 0.82f, 0.95f, isRipple ? 0.18f : 0.45f);
                var emission = particles.emission;
                emission.rateOverTime = 0f;
                emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(isRipple ? 1 : 7)) });
                var shape = particles.shape;
                shape.enabled = !isRipple;
                shape.shapeType = ParticleSystemShapeType.Cone;
                shape.angle = 40f; shape.radius = 0.08f;
                shape.rotation = new Vector3(-90f, 0f, 0f);
                var size = particles.sizeOverLifetime;
                size.enabled = isRipple;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.3f, 1f, 1.5f));
                var color = particles.colorOverLifetime;
                color.enabled = true;
                var gradient = new Gradient();
                gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                    new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(0f, 1f) });
                color.color = gradient;
                if (!isRipple)
                {
                    var sheet = particles.textureSheetAnimation;
                    sheet.enabled = true; sheet.numTilesX = 1; sheet.numTilesY = 3;
                    sheet.frameOverTime = new ParticleSystem.MinMaxCurve(1f / 3f);
                }
                var renderer = root.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = material;
                renderer.renderMode = isRipple ? ParticleSystemRenderMode.HorizontalBillboard : ParticleSystemRenderMode.Stretch;
                renderer.velocityScale = 0.08f;
                renderer.lengthScale = 1.5f;
                return PrefabUtility.SaveAsPrefabAsset(root, Root + "/Effects/Footstep" + name + ".prefab");
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        [UPlaygroundTool("Footsteps/호수 월드 지면 연결")]
        public static void BindWorld()
        {
            RequireEditMode();
            var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            if (scene.path != WorldPath || scene.isDirty)
                throw new InvalidOperationException("저장된 LakeVillageWorld를 열고 실행하세요. 다른 작업의 변경은 저장하지 않습니다.");
            var settings = AssetDatabase.LoadAssetAtPath<FootstepSettingsSO>(Root + "/Raon_FootstepSettings.asset");
            var dirt = settings.surfaces[0]; var grass = settings.surfaces[1]; var stone = settings.surfaces[2];
            var profiles = new[] { grass, dirt, stone, grass, grass, dirt, dirt };
            foreach (Terrain terrain in UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (terrain.gameObject.scene != scene) continue;
                var data = terrain.terrainData;
                if (data.terrainLayers.Length != profiles.Length)
                    throw new InvalidOperationException("TerrainLayer 수가 달라졌습니다. 재질 매핑을 먼저 확정하세요.");
                // ResourceOverhaul의 실제 텍스처와 확인한 레이어 순서만 연결한다.
                for (int i = 0; i < data.terrainLayers.Length; i++)
                    if (AssetDatabase.GetAssetPath(data.terrainLayers[i]) != "Assets/10.Datas/World/OpenWorld/ResourceOverhaul/Ground_" + i + ".terrainlayer")
                        throw new InvalidOperationException("검토하지 않은 TerrainLayer입니다. 재질 매핑을 먼저 확정하세요.");
                var map = LoadOrCreate<FootstepTerrainMapSO>(Root + "/Terrain_" + AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(data)) + ".asset");
                map.width = data.alphamapWidth; map.height = data.alphamapHeight;
                map.surfaces = profiles;
                map.dominantLayers = new byte[map.width * map.height];
                float[,,] weights = data.GetAlphamaps(0, 0, map.width, map.height);
                for (int y = 0; y < map.height; y++)
                for (int x = 0; x < map.width; x++)
                {
                    int best = 0;
                    for (int layer = 1; layer < data.alphamapLayers; layer++)
                        if (weights[y, x, layer] > weights[y, x, best]) best = layer;
                    map.dominantLayers[y * map.width + x] = (byte)best;
                }
                EditorUtility.SetDirty(map);
                var binding = terrain.GetComponent<FootstepSurface>() ?? Undo.AddComponent<FootstepSurface>(terrain.gameObject);
                binding.terrain = terrain; binding.terrainMap = map; binding.profile = dirt;
                binding.colliders = terrain.GetComponents<Collider>();
                EditorUtility.SetDirty(binding);
            }
            var waterMaterials = new[] {
                AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/LakeVillage/LakeVillageWorld_Water.mat"),
                AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/OpenWorld/HarborNaturalWater.mat") };
            foreach (MeshRenderer renderer in UnityEngine.Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (renderer.gameObject.scene != scene || !renderer.sharedMaterials.Any(m => m != null && waterMaterials.Contains(m))) continue;
                var filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                var water = renderer.GetComponent<FootstepWaterSurface>() ?? Undo.AddComponent<FootstepWaterSurface>(renderer.gameObject);
                water.surfaceMesh = filter; water.profile = settings.surfaces[3];
                EditorUtility.SetDirty(water);
            }
            SaveAuthoredAssets();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void SaveAuthoredAssets()
        {
            foreach (string guid in AssetDatabase.FindAssets("", new[] { Root }))
                AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath(guid)));
            AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadAssetAtPath<SoundDatabaseSO>("Assets/10.Datas/Sound/SoundDatabase.asset"));
        }

        private static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            if (shader == null) throw new InvalidOperationException("URP 파티클 셰이더가 없습니다.");
            var asset = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (asset != null) return asset;
            asset = new Material(shader);
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static T CopyAsset<T>(string source, string destination) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(destination);
            if (asset != null) return asset;
            if (!AssetDatabase.CopyAsset(source, destination)) throw new InvalidOperationException("복제 실패: " + source);
            return AssetDatabase.LoadAssetAtPath<T>(destination);
        }

        private static void RequireEditMode()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("컴파일이 끝난 Edit Mode에서만 실행하세요.");
        }
    }
}
