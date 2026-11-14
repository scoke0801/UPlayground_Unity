using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Projectile;

namespace UPlayGround.Editor
{
    /// <summary>확인된 적 화살과 단일 마법탄만 별도 Definition에 연결하는 일회성 저작 작업.</summary>
    public static class ProjectileParryContentAuthoring
    {
        private const string Root = "Assets/10.Datas/Projectile/Parry";
        private const string VisualRoot = "Assets/03.Prefabs/Projectile/Parry";
        private const string BowAbilityPath = "Assets/10.Datas/Ability/Actor/Humanoid_BowAttackData/GA_Humanoid_BowAttackData_01_Attack_1.asset";
        private const string BoltPayloadPath = "Assets/10.Datas/Ability/Actor/P09Staff/Payload_P09Staff_Bolt.asset";

        [UPlayGround.EditorTools.UPlaygroundTool("Combat/ProjectileParry/초기 콘텐츠 적용")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Edit Mode에서 컴파일이 끝난 뒤 실행하세요.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            var created = new List<string>();
            var modified = new List<UnityEngine.Object>();
            try
            {
                EnsureFolder(Root); EnsureFolder(VisualRoot);
                var sound = LoadGuid<AudioClip>("7c1d87cd132b85745910f7763178c846");
                var material = LoadOrCreateMaterial(created);
                var flight = CreateCue("ProjectileParryFlightCue", material, true, created);
                var trail = CreateCue("ProjectileParryReturnTrail", material, false, created);
                var arrow = LoadOrCreateArrow(created);
                var bolt = LoadOrCopyBolt(created);
                Configure(arrow, flight, trail, sound, modified);
                Configure(bolt, flight, trail, sound, modified);
                var bow = AssetDatabase.LoadAssetAtPath<GameplayAbilitySO>(BowAbilityPath);
                if (bow == null || bow.variants.Count == 0
                    || bow.variants[0].executionPayload is not UPlayGroundMotionAbilityPayloadSO bowPayload)
                    throw new InvalidOperationException("확인한 Bow Payload 연결이 없습니다.");
                var boltPayload = AssetDatabase.LoadAssetAtPath<UPlayGroundMotionAbilityPayloadSO>(BoltPayloadPath);
                Bind(bowPayload, arrow, modified); Bind(boltPayload, bolt, modified);
                var policy = LoadGuid<CombatDefensePolicySO>("367ddf7ac6efe0d40ae5f77efac19d28");
                Undo.RecordObject(policy, "투사체 패리 정책 저작");
                policy.projectilePerfectGuardWindow = 0.18f;
                policy.projectileParryRearmInterval = 0.35f;
                policy.projectileParryFrontAngle = 140f;
                policy.projectileFeedbackInterval = 0.15f;
                policy.projectileParryFeedback = new DefenseSuccessFeedbackProfile
                {
                    successType = DefenseSuccessType.PerfectGuard, freezeDuration = 0.035f,
                    attackerFreezeDuration = 0f, tailDuration = 0f, counterWindowDuration = 0f,
                    spawnVitalOrb = false, shakeKey = UPlayGround.Data.Path.CameraShakeIdType.LiteHit, soundKey = UPlayGround.Data.Sound.GameSoundKey.CombatParry,
                };
                modified.Add(policy);
                foreach (var asset in modified) { EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset); }
                Undo.CollapseUndoOperations(group);
                Audit();
                Undo.IncrementCurrentGroup();
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (var asset in modified) if (asset != null) AssetDatabase.SaveAssetIfDirty(asset);
                for (int i = created.Count - 1; i >= 0; i--) AssetDatabase.DeleteAsset(created[i]);
                throw;
            }
        }

        private static void Bind(UPlayGroundMotionAbilityPayloadSO payload, ProjectileDefinitionSO definition, List<UnityEngine.Object> modified)
        {
            if (payload?.attackInfo?.baseInfo?.hitPhases?.Count != 1)
                throw new InvalidOperationException("확인한 단일 HitPhase 구조와 다릅니다.");
            Undo.RecordObject(payload, "반사 지원 탄 연결");
            payload.attackInfo.baseInfo.hitPhases[0].projectileDefinition = definition;
            payload.attackInfo.defenseType = AttackDefenseType.Parryable;
            modified.Add(payload);
        }

        private static void Configure(ProjectileDefinitionSO definition, GameObject flight, GameObject trail,
            AudioClip sound, List<UnityEngine.Object> modified)
        {
            Undo.RecordObject(definition, "반사 지원 탄 저작");
            definition.behaviors = new List<ProjectileBehaviorData>
            {
                new ReflectableProjectileBehavior { flightCuePrefab = flight, returnTrailPrefab = trail, launchCue = sound },
            };
            definition.inheritOwnerTimeScale = false;
            definition.destroyOnHit = true;
            modified.Add(definition);
        }

        private static ProjectileDefinitionSO LoadOrCreateArrow(List<string> created)
        {
            string path = Root + "/Projectile_EnemyArrow_Parry.asset";
            var definition = AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(path);
            if (definition != null) return definition;
            definition = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            definition.visualPrefab = LoadGuid<GameObject>("a550920db554d3541a9da0cab0af6e9a");
            definition.motion = new LinearProjectileMotion { speed = 20f, maxSpeed = 50f };
            definition.lifetime = 3f;
            definition.collisionRadius = 0.15f;
            definition.obstructionLayers = (1 << 0) | (1 << 3);
            definition.hitEffectKey = "LiteHit";
            AssetDatabase.CreateAsset(definition, path); created.Add(path);
            return definition;
        }

        private static ProjectileDefinitionSO LoadOrCopyBolt(List<string> created)
        {
            string path = "Assets/10.Datas/Ability/Actor/P09Staff/Projectile_P09Staff_Bolt_Parry.asset";
            var definition = AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(path);
            if (definition != null) return definition;
            if (!AssetDatabase.CopyAsset("Assets/10.Datas/Ability/Actor/P09Staff/Projectile_P09Staff_Bolt.asset", path))
                throw new InvalidOperationException("마법탄 Definition 복제 실패");
            created.Add(path);
            return AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(path);
        }

        private static Material LoadOrCreateMaterial(List<string> created)
        {
            string path = VisualRoot + "/ProjectileParryCue.mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) throw new InvalidOperationException("URP Unlit 셰이더가 없습니다.");
            material = new Material(shader);
            material.SetColor("_BaseColor", new Color(1.6f, 1.6f, 1.6f, 1f));
            AssetDatabase.CreateAsset(material, path); created.Add(path);
            return material;
        }

        private static GameObject CreateCue(string name, Material material, bool dual, List<string> created)
        {
            string path = VisualRoot + "/" + name + ".prefab";
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null) return prefab;
            var root = new GameObject(name);
            try
            {
                if (dual)
                {
                    var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    UnityEngine.Object.DestroyImmediate(core.GetComponent<Collider>());
                    core.name = "밝은 중심";
                    core.transform.SetParent(root.transform, false);
                    core.transform.localScale = Vector3.one * 0.075f;
                    core.GetComponent<Renderer>().sharedMaterial = material;
                }
                for (int i = 0; i < (dual ? 2 : 1); i++)
                {
                    var tail = new GameObject("꼬리");
                    tail.transform.SetParent(root.transform, false);
                    tail.transform.localPosition = Vector3.right * (dual ? (i == 0 ? -0.045f : 0.045f) : 0f);
                    var renderer = tail.AddComponent<TrailRenderer>();
                    renderer.sharedMaterial = material;
                    renderer.time = dual ? 0.16f : 0.12f;
                    renderer.startWidth = dual ? 0.025f : 0.06f;
                    renderer.endWidth = 0f;
                    renderer.minVertexDistance = 0.02f;
                    renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }
                prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
                if (prefab == null) throw new InvalidOperationException("투사체 표시 프리팹 저장 실패");
                created.Add(path);
                return prefab;
            }
            finally { UnityEngine.Object.DestroyImmediate(root); }
        }

        private static T LoadGuid<T>(string guid) where T : UnityEngine.Object
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
            if (asset == null) throw new InvalidOperationException("확인한 리소스가 없습니다: " + guid);
            return asset;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = path.Substring(0, path.LastIndexOf('/'));
            EnsureFolder(parent); AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        public static void Audit()
        {
            var lines = new List<string>();
            foreach (string name in new[] { "Projectile_EnemyArrow_Parry", "Projectile_P09Staff_Bolt_Parry" })
            {
                string path = name == "Projectile_P09Staff_Bolt_Parry"
                    ? "Assets/10.Datas/Ability/Actor/P09Staff/" + name + ".asset"
                    : Root + "/" + name + ".asset";
                var definition = AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>(path);
                if (definition == null) throw new InvalidOperationException(path);
                var errors = new List<string>(); definition.CollectValidationErrors(errors);
                if (!definition.SupportsReflection()) errors.Add(path + ": 반사 지원 설정이 저장되지 않았습니다.");
                if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", errors));
                lines.Add(path + " GUID=" + AssetDatabase.AssetPathToGUID(path) + " 오류=0");
                foreach (string dependency in AssetDatabase.GetDependencies(path, true))
                    lines.Add("  " + dependency + " GUID=" + AssetDatabase.AssetPathToGUID(dependency));
            }
            Directory.CreateDirectory("docs/work-status/projectile-parry");
            File.WriteAllLines("docs/work-status/projectile-parry/p3-audit.txt", lines);
        }
    }
}

