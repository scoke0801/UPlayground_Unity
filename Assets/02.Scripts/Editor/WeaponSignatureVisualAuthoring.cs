using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Projectile;

namespace UPlayGround.Editor
{
    /// <summary>시그니처 전용 파생 FX와 투사체를 생성한다. 구매 원본과 기존 결과는 덮어쓰지 않는다.</summary>
    public static class WeaponSignatureVisualAuthoring
    {
        public const string Root = "Assets/03.Prefabs/Effects/Signature";
        private const string ProjectileRoot = "Assets/10.Datas/Projectile/Signature";
        private const string GroundHit = "Assets/ExternalAssets/FX/Cartoon FX Remaster/CFXR Prefabs/Impacts/CFXR2 Ground Hit.prefab";
        private const string Spiral = "Assets/ExternalAssets/FX/Cartoon FX Remaster/CFXR Prefabs/Sword Trails/Plain/CFXR4 Sword Trail PLAIN (360 Spiral).prefab";
        private static readonly List<string> Sources = new();

        public static string Build()
        {
            if (EditorApplication.isPlaying || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("정상 편집 모드에서만 제작합니다.");
            Directory.CreateDirectory(Root);
            Directory.CreateDirectory(ProjectileRoot);
            AssetDatabase.Refresh();
            Sources.Clear();
            BuildParticleCopy("Katana", "Assets/ExternalAssets/FX/Hovl Studio/Sword slash VFX/Prefabs/Sword Slash 11.prefab", Vector3.one, .3f);
            BuildParticleCopy("DoubleAxe", Spiral, Vector3.one * 1.5f, 2.5f);
            BuildParticleCopy("DualBlade", "Assets/ExternalAssets/FX/FreeQuickEffectsVol1/Prefabs/vfx_Tornado_01.prefab", Vector3.one, 3f);
            BuildParticleCopy("Whip", Spiral, Vector3.one * 3f, 1.5f);
            foreach (string weapon in new[] { "Spear", "GreatSword", "SwordShield", "Bow" })
                BuildParticleCopy(weapon + "_Impact", GroundHit, Vector3.one * (weapon == "Spear" ? 2f : 3f), .65f);
            BuildRain();
            BuildBeam();
            BuildProp("Spear", ModelMesh("Reine", "Spear1H_Epic2"), Vector3.one, false);
            BuildProp("SwordShield", Load("Assets/03.Prefabs/Weapon/Shield/Shield_001.prefab"), Vector3.one * 6f, false);
            BuildProp("GreatSword", Load("Assets/03.Prefabs/Weapon/GreatSword/GreatSword_001.prefab"), Vector3.one * 5f, true);
            BuildProjectiles();
            Directory.CreateDirectory("docs/work-status/weapon-signature");
            File.WriteAllLines("docs/work-status/weapon-signature/FX_SOURCES.txt", Sources);
            return "시그니처 전용 FX·투사체 생성 완료. 미술 인수는 별도입니다.";
        }

        private static GameObject Load(string path)
        {
            var value = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (value == null) throw new InvalidOperationException("원본 누락: " + path);
            Sources.Add(path + " | " + AssetDatabase.AssetPathToGUID(path));
            return value;
        }

        private static GameObject ModelMesh(string character, string name)
        {
            var model = Load("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_" + character + ".prefab");
            var part = model.GetComponentsInChildren<MeshFilter>(true).FirstOrDefault(x => x.name == name);
            if (part == null) throw new InvalidOperationException("무기 메시 누락: " + name);
            return part.gameObject;
        }

        private static void BuildParticleCopy(string weapon, string source, Vector3 scale, float duration)
        {
            string path = Root + "/" + weapon + "_Signature.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject root = UnityEngine.Object.Instantiate(Load(source));
            root.name = weapon + "_Signature";
            try
            {
                root.transform.localScale = Vector3.Scale(root.transform.localScale, scale);
                foreach (var script in root.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(script);
                foreach (var ps in root.GetComponentsInChildren<ParticleSystem>(true))
                {
                    ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = ps.main;
                    main.duration = duration;
                    main.loop = false;
                    main.playOnAwake = true;
                    main.stopAction = ParticleSystemStopAction.None;
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void BuildRain()
        {
            string path = Root + "/Bow_Signature.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            GameObject arrow = Load("Assets/03.Prefabs/Weapon/Bow/Arrow_001.prefab");
            var filter = arrow.GetComponentInChildren<MeshFilter>(true);
            var renderer = arrow.GetComponentInChildren<Renderer>(true);
            Mesh arrowMesh = filter != null ? filter.sharedMesh : (renderer as SkinnedMeshRenderer)?.sharedMesh;
            if (arrowMesh == null) throw new InvalidOperationException("화살 메시가 없습니다.");
            var root = new GameObject("Bow_Signature");
            try
            {
                var ps = root.AddComponent<ParticleSystem>();
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.duration = 2.4f; main.loop = false; main.startDelay = .6f - 7f / 22f;
                main.startLifetime = .35f; main.startSpeed = 22f; main.startSize = .8f;
                main.startRotation3D = true; main.startRotationX = Mathf.PI;
                main.maxParticles = 96; main.simulationSpace = ParticleSystemSimulationSpace.Local;
                var emission = ps.emission;
                emission.rateOverTime = 0;
                emission.SetBursts(Enumerable.Range(0, 6).Select(i => new ParticleSystem.Burst(i * .3f, 12)).ToArray());
                var shape = ps.shape;
                shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 4;
                shape.rotation = new Vector3(90, 0, 0); shape.position = Vector3.up * 7f;
                var velocity = ps.velocityOverLifetime;
                velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.Local;
                velocity.x = 0; velocity.y = -22; velocity.z = 0;
                main.startSpeed = 0;
                var visual = ps.GetComponent<ParticleSystemRenderer>();
                visual.renderMode = ParticleSystemRenderMode.Mesh;
                visual.mesh = arrowMesh; visual.sharedMaterials = renderer.sharedMaterials;
                var impact = Load(Root + "/Bow_Impact_Signature.prefab");
                for (int i = 0; i < 6; i++)
                {
                    var burst = UnityEngine.Object.Instantiate(impact, root.transform);
                    foreach (var part in burst.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        part.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        var settings = part.main; settings.startDelay = .6f + .3f * i;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        /// <summary>발사 전 동작과 지면 범위를 읽을 수 있게 보조 연출을 기존 실행 데이터에 추가한다.</summary>
        public static string ApplySupportingEffects()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("정상 편집 모드에서만 저작합니다.");
            FitAuthoredProps();
            RouteDetonationCamera();
            const string flashPath = "Assets/ExternalAssets/FX/Hovl Studio/AAA Projectiles Vol 1/Prefabs/Flash and hits/Flash 10 blue laser.prefab";
            var material = Load(flashPath).GetComponentsInChildren<ParticleSystemRenderer>(true).First(r => r.sharedMaterial != null).sharedMaterial;
            string chargePath = Root + "/Staff_Charge_Signature.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(chargePath) == null)
            {
                var root = new GameObject("Staff_Charge_Signature");
                try
                {
                    var ps = root.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = ps.main; main.duration = .9f; main.loop = false; main.startLifetime = .9f;
                    main.startSpeed = 0; main.startSize = .8f; main.startColor = new Color(.2f, .7f, 1, .8f);
                    var shape = ps.shape; shape.enabled = false;
                    var emission = ps.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 1) });
                    var size = ps.sizeOverLifetime; size.enabled = true; size.size = new ParticleSystem.MinMaxCurve(1, AnimationCurve.EaseInOut(0, .12f, 1, 1));
                    ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = material;
                    PrefabUtility.SaveAsPrefabAsset(root, chargePath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            string launchPath = Root + "/Bow_Launch_Signature.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(launchPath) == null)
            {
                var source = Load("Assets/03.Prefabs/Weapon/Bow/Arrow_001.prefab").GetComponentInChildren<SkinnedMeshRenderer>(true);
                var root = new GameObject("Bow_Launch_Signature");
                try
                {
                    var ps = root.AddComponent<ParticleSystem>(); ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var main = ps.main; main.duration = .5f; main.loop = false; main.startLifetime = .5f;
                    main.startSpeed = 0; main.startSize = .8f; main.startRotation3D = true; main.startRotationX = -65 * Mathf.Deg2Rad;
                    var shape = ps.shape; shape.enabled = false;
                    var emission = ps.emission; emission.rateOverTime = 0; emission.SetBursts(new[] { new ParticleSystem.Burst(0, 1) });
                    var velocity = ps.velocityOverLifetime; velocity.enabled = true; velocity.space = ParticleSystemSimulationSpace.Local;
                    velocity.x = 0; velocity.y = 18; velocity.z = 8;
                    var renderer = ps.GetComponent<ParticleSystemRenderer>(); renderer.renderMode = ParticleSystemRenderMode.Mesh;
                    renderer.mesh = source.sharedMesh; renderer.sharedMaterials = source.sharedMaterials;
                    PrefabUtility.SaveAsPrefabAsset(root, launchPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            string ringPath = Root + "/Bow_Telegraph_Signature.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(ringPath) == null)
            {
                var root = new GameObject("Bow_Telegraph_Signature");
                try
                {
                    var line = root.AddComponent<LineRenderer>(); line.useWorldSpace = false; line.loop = true;
                    line.positionCount = 96; line.startWidth = .05f; line.endWidth = .05f; line.sharedMaterial = material;
                    line.startColor = new Color(.2f, .7f, 1, .65f); line.endColor = line.startColor;
                    for (int i = 0; i < 96; i++)
                    {
                        float angle = i * Mathf.PI * 2 / 96;
                        line.SetPosition(i, new Vector3(Mathf.Cos(angle) * 4, .03f, Mathf.Sin(angle) * 4));
                    }
                    PrefabUtility.SaveAsPrefabAsset(root, ringPath);
                }
                finally { UnityEngine.Object.DestroyImmediate(root); }
            }
            string rainPath = Root + "/Bow_Signature.prefab";
            var rain = PrefabUtility.LoadPrefabContents(rainPath);
            try
            {
                if (rain.transform.Find("판정 범위") == null)
                {
                    var ring = UnityEngine.Object.Instantiate(Load(ringPath), rain.transform); ring.name = "판정 범위";
                    PrefabUtility.SaveAsPrefabAsset(rain, rainPath);
                }
            }
            finally { PrefabUtility.UnloadPrefabContents(rain); }
            AppendParticle("Staff", chargePath, 0, .9f, new Vector3(0, 1.2f, .6f));
            AppendParticle("Bow", launchPath, 22f / 30f, 38f / 30f, new Vector3(0, 1.4f, .4f));
            return "Staff 차징·Bow 상승 화살·반경 4m 지면 표시 연결 완료";
        }

        private static void AppendParticle(string weapon, string prefabPath, float start, float end, Vector3 offset)
        {
            string path = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Signature/" + weapon + "_Signature.asset";
            var motion = AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>(path);
            if (motion == null) throw new InvalidOperationException("실행 MotionSet 누락: " + path);
            var prefab = Load(prefabPath);
            if (motion.motionSet.globalEvents.OfType<UPlayGround.Data.Event.BeginParticleEvent>().Any(e => e.particlePrefab == prefab)) return;
            Undo.RegisterCompleteObjectUndo(motion, "시그니처 보조 FX");
            motion.motionSet.globalEvents.Add(new UPlayGround.Data.Event.BeginParticleEvent
            { particlePrefab = prefab, startTime = start, endTime = end, offset = offset, attachToTarget = true, destroyOnFinish = true });
            EditorUtility.SetDirty(motion); AssetDatabase.SaveAssetIfDirty(motion);
        }

        private static void BuildBeam()
        {
            string path = Root + "/Staff_Signature.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var source = Load("Assets/ExternalAssets/FX/Hovl Studio/3D Lasers Pack/Prefabs/Laser beam 5 magic.prefab");
            var root = new GameObject("Staff_Signature");
            try
            {
                // 구매 프리팹의 발사/착탄 입자와 같은 재질을 쓰되 물리 Raycast로 빔을 자르지 않는다.
                var sourceRenderer = source.GetComponentsInChildren<ParticleSystemRenderer>(true).First(x => x.sharedMaterial != null);
                var beam = root.AddComponent<LineRenderer>();
                beam.useWorldSpace = false; beam.positionCount = 2;
                beam.SetPosition(0, new Vector3(0, 1.2f, .5f)); beam.SetPosition(1, new Vector3(0, 1.2f, 18.5f));
                beam.startWidth = 1.6f; beam.endWidth = 1.6f;
                beam.sharedMaterial = sourceRenderer.sharedMaterial;
                beam.startColor = new Color(.35f, .7f, 1f, .8f); beam.endColor = beam.startColor;
                foreach (string name in new[] { "Flash", "Hit" })
                {
                    var child = source.transform.Find(name);
                    if (child == null) continue;
                    var copy = UnityEngine.Object.Instantiate(child.gameObject, root.transform);
                    copy.transform.localPosition = new Vector3(0, 1.2f, name == "Hit" ? 18.5f : .5f);
                    foreach (var ps in copy.GetComponentsInChildren<ParticleSystem>(true))
                    {
                        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                        var main = ps.main; main.loop = true; main.playOnAwake = true; main.stopAction = ParticleSystemStopAction.None;
                    }
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void BuildProp(string weapon, GameObject source, Vector3 scale, bool falling)
        {
            string path = Root + "/" + weapon + "_Signature.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) return;
            var root = new GameObject(weapon + "_Signature");
            try
            {
                GameObject mesh = UnityEngine.Object.Instantiate(source, root.transform);
                mesh.name = "형상";
                mesh.transform.localPosition = Vector3.zero;
                mesh.transform.localRotation = Quaternion.identity;
                mesh.transform.localScale = scale;
                foreach (var component in mesh.GetComponentsInChildren<MonoBehaviour>(true)) UnityEngine.Object.DestroyImmediate(component);
                foreach (var collider in mesh.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(collider);
                foreach (var constraint in mesh.GetComponentsInChildren<UnityEngine.Animations.ParentConstraint>(true)) UnityEngine.Object.DestroyImmediate(constraint);
                foreach (var trail in mesh.GetComponentsInChildren<TrailRenderer>(true)) UnityEngine.Object.DestroyImmediate(trail);
                if (falling)
                {
                    // 정확한 접지 동기는 MotionEvent의 타임라인 샘플링이 소유한다.
                    FitGreatSword(mesh.transform);
                }
                if (weapon == "Spear") mesh.transform.localRotation = Quaternion.Euler(90, 0, 0);
                if (weapon == "SwordShield") AddShieldGrowth(root, mesh.transform);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static void AddShieldGrowth(GameObject root, Transform mesh)
        {
            const string folder = "Assets/07.Animations/Player/Signature/SwordShield";
            const string path = folder + "/Shield_Growth.anim";
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            bool created = clip == null;
            if (created) clip = new AnimationClip { name = "방패 낙하 확대", legacy = true, frameRate = 30, wrapMode = WrapMode.Once };
            Bounds bounds = GetMeshBounds(mesh);
            float diameter = Mathf.Max(bounds.size.x, bounds.size.z);
            mesh.localScale = Vector3.one * (9 / diameter);
            for (int axis = 0; axis < 3; axis++)
            {
                float final = mesh.localScale[axis];
                var curve = new AnimationCurve(new Keyframe(0, final / 6), new Keyframe(.4f, final / 6), new Keyframe(.9f, final));
                clip.SetCurve("형상", typeof(Transform), "localScale." + "xyz"[axis], curve);
            }
            if (created) AssetDatabase.CreateAsset(clip, path);
            else { EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip); }
            mesh.localScale /= 6;
            var animation = root.GetComponent<UnityEngine.Animation>() ?? root.AddComponent<UnityEngine.Animation>();
            animation.AddClip(clip, clip.name); animation.clip = clip;
            animation.playAutomatically = true; animation.cullingType = AnimationCullingType.AlwaysAnimate;
        }

        private static Bounds GetMeshBounds(Transform root)
        {
            var filter = root.GetComponentInChildren<MeshFilter>(true);
            var skin = root.GetComponentInChildren<SkinnedMeshRenderer>(true);
            Mesh mesh = filter != null ? filter.sharedMesh : skin != null ? skin.sharedMesh : null;
            if (mesh == null) throw new InvalidOperationException("소품 메시 누락: " + root.name);
            return mesh.bounds;
        }

        private static void RouteDetonationCamera()
        {
            foreach (string weapon in new[] { "Spear", "SwordShield" })
            {
                string motionPath = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Signature/" + weapon + "_Signature.asset";
                var motion = AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>(motionPath);
                if (motion == null) throw new InvalidOperationException("착탄 카메라 MotionSet 누락: " + weapon);
                string prefix = "Signature." + weapon + ".";
                var events = motion.motionSet.globalEvents.OfType<UPlayGround.Data.Event.CameraEffectEvent>()
                    .Where(e => e.effectDataList.Any(d => d != null && (d.effectKey.StartsWith(prefix + "Impact") || d.effectKey == prefix + "Slow" || d.effectKey == prefix + "BreakFov"))).ToArray();
                string path = Root + "/" + weapon + "_Impact_Signature.prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var cue = root.GetComponent<UPlayGround.Components.CameraEffectOnSpawn>() ?? root.AddComponent<UPlayGround.Components.CameraEffectOnSpawn>();
                    if (events.Length > 0) cue.effects = cue.effects.Concat(events.SelectMany(e => e.effectDataList)).Where(d => d != null).Distinct().ToList();
                    if (cue.effects.Count == 0) throw new InvalidOperationException("착탄 카메라 SO 누락: " + weapon);
                    // ShowFX는 품질·거리 컬링 없이 생성하고 기본 5초 뒤 제거한다. 효과 SO는 모두 0.3초 이내다.
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                Undo.RegisterCompleteObjectUndo(motion, "착탄 카메라 소유권 이전");
                foreach (var evt in events) motion.motionSet.globalEvents.Remove(evt);
                EditorUtility.SetDirty(motion); AssetDatabase.SaveAssetIfDirty(motion);
            }
        }

        private static void FitGreatSword(Transform mesh)
        {
            Bounds bounds = GetMeshBounds(mesh);
            float angle = 105 * Mathf.Deg2Rad, cosine = Mathf.Abs(Mathf.Cos(angle)), sine = Mathf.Sin(angle);
            float denominator = cosine * cosine - sine * sine;
            float blade = (3 * cosine - 9 * sine) / denominator;
            float thickness = (9 * cosine - 3 * sine) / denominator;
            mesh.localRotation = Quaternion.Euler(105, 0, 0);
            mesh.localScale = new Vector3(3 / bounds.size.x, blade / bounds.size.y, thickness / bounds.size.z);
            // 이벤트 끝의 z=7을 더하면 형상 AABB가 본타 Box의 (0,1.5,5.5), (3,3,9)와 일치한다.
            mesh.localPosition = new Vector3(0, 1.5f, -1.5f) - mesh.localRotation * Vector3.Scale(bounds.center, mesh.localScale);
        }

        private static void FitAuthoredProps()
        {
            foreach (string weapon in new[] { "Spear", "GreatSword", "SwordShield" })
            {
                string path = Root + "/" + weapon + "_Signature.prefab";
                var root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var mesh = root.transform.Find("형상");
                    if (mesh == null) throw new InvalidOperationException("형상 루트 누락: " + path);
                    if (weapon == "GreatSword") FitGreatSword(mesh);
                    else if (weapon == "Spear") mesh.localRotation = Quaternion.Euler(90, 0, 0);
                    else AddShieldGrowth(root, mesh);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            foreach (string weapon in new[] { "Bow", "DualBlade", "SwordShield" })
            {
                string suffix = weapon == "Bow" ? "Rain" : weapon == "DualBlade" ? "Tornado" : "Fall";
                var definition = AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(ProjectileRoot + "/Projectile_" + weapon + "Signature" + suffix + ".asset");
                if (definition == null) throw new InvalidOperationException("투사체 누락: " + weapon);
                Undo.RecordObject(definition, "장판·소품 방향 보존");
                definition.keepWorldOrientation = true;
                if (definition.motion is StationaryProjectileMotion stationary) stationary.groundLayers = (1 << 0) | (1 << 3);
                if (weapon == "DualBlade") definition.GetBehavior<AreaTickProjectileBehavior>().radius = 3.5f;
                EditorUtility.SetDirty(definition); AssetDatabase.SaveAssetIfDirty(definition);
            }
        }

        private static void BuildProjectiles()
        {
            CreateProjectile("BowSignatureRain", "Bow", new StationaryProjectileMotion { attachToGround = true, groundLayers = (1 << 0) | (1 << 3) },
                new AreaTickProjectileBehavior { radius = 4, interval = .3f, activationDelay = .6f, maxHitsPerTarget = 6,
                    damageFalloff = AnimationCurve.Constant(0, 1, 1) }, 2.4f);
            CreateProjectile("DualBladeSignatureTornado", "DualBlade", new StationaryProjectileMotion { attachToGround = true, groundLayers = (1 << 0) | (1 << 3) },
                new AreaTickProjectileBehavior { radius = 3.5f, interval = .25f, maxHitsPerTarget = 12, damageFalloff = AnimationCurve.Constant(0, 1, 1) }, 3f);
            CreateProjectile("SpearSignatureFall", "Spear", new ArcProjectileMotion { arcHeight = 0, flightTimeMode = ProjectileArcFlightTimeMode.Fixed, fixedFlightTime = .3f },
                new DetonateProjectileBehavior { radius = 3.5f, onHit = false, onExpire = true, damageFalloff = AnimationCurve.Constant(0, 1, 1) }, .3f);
            CreateProjectile("SwordShieldSignatureFall", "SwordShield", new ArcProjectileMotion { arcHeight = 8, flightTimeMode = ProjectileArcFlightTimeMode.Fixed, fixedFlightTime = .9f },
                new DetonateProjectileBehavior { radius = 4.5f, onHit = false, onExpire = true, damageFalloff = AnimationCurve.Constant(0, 1, 1) }, .9f);
        }

        private static void CreateProjectile(string name, string weapon, ProjectileMotionData motion, ProjectileBehaviorData behavior, float lifetime)
        {
            string path = ProjectileRoot + "/Projectile_" + name + ".asset";
            if (AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(path) != null) return;
            var asset = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            asset.visualPrefab = Load(Root + "/" + weapon + "_Signature.prefab");
            asset.motion = motion; asset.behaviors = new List<ProjectileBehaviorData> { behavior };
            asset.lifetime = lifetime; asset.destroyOnHit = false; asset.inheritOwnerTimeScale = false;
            asset.returnOnOwnerDeath = true; asset.prewarmCount = 1;
            asset.keepWorldOrientation = weapon != "Spear";
            if (behavior is DetonateProjectileBehavior)
            {
                asset.ignoreFlightContacts = true;
                asset.detonationEffectKey = "Signature_" + weapon + "_Impact";
            }
            // 지정 지점 투척은 단일 접촉에 소멸하지 않고 착탄 시각에만 폭발한다.
            asset.collisionRadius = .1f;
            AssetDatabase.CreateAsset(asset, path);
        }
    }
}
