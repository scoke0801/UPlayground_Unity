using System;
using System.IO;
using System.Linq;
using Animancer;
using KinematicCharacterController;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.AI.BehaviorTree;
using UPlayGround.AI.BehaviorTree.Editor;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.EditorTools;
using UPlayGround.Gameplay.Tag;
using UPlayGround.MovementController;

namespace UPlayGround.Tool.Editor.Map
{
    /// <summary>환경 늑대의 기존 모델을 보존하며 정규 몬스터 전투 데이터를 연결한다.</summary>
    public static class WildlifeWolfAuthoring
    {
        private const string Root = "Assets/10.Datas/World/Wildlife/Combat";
        private const string PrefabPath = "Assets/03.Prefabs/Environment/Wildlife/Wolf.prefab";
        private const string SourceSetPath = "Assets/10.Datas/Ability/Actor/Spider_Minion/AbilitySet_Spider_Minion.asset";
        private const string MotionKeyValue = "WoodlandWolf.Bite";

        /// <summary>새 물기 모션과 독립 GAS·BT를 생성하고 늑대 프리팹에 한 번 연결한다.</summary>
        [UPlaygroundTool("UPlayGround/월드/환경 생물/늑대 전투 생성")]
        public static void Build()
        {
            if (Application.isPlaying) throw new InvalidOperationException("편집 모드에서 실행해야 합니다.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath).GetComponent<MonsterActor>() != null)
                throw new InvalidOperationException("이미 전투 늑대입니다. 기존 데이터는 각 에디터에서 수정하세요.");
            if (Directory.Exists(Root) && Directory.EnumerateFiles(Root, "*", SearchOption.AllDirectories).Any())
                throw new InvalidOperationException("늑대 전투 데이터가 이미 있습니다. 기존 에셋을 덮어쓰지 않습니다.");
            Directory.CreateDirectory(Root);
            AssetDatabase.Refresh();
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var idle = LoadClip("Wolf_Idle");
                var bite = CreatePoseClip(root, idle, "Wolf_Bite", 1.2f, PoseKind.Bite);
                var hit = CreatePoseClip(root, idle, "Wolf_Hit", 0.5f, PoseKind.Hit);
                var death = CreatePoseClip(root, idle, "Wolf_Death", 1f, PoseKind.Death);
                var biteMotion = CreateMotion("Wolf_Bite", bite, false);
                biteMotion.motionSet.globalEvents.Add(new BeginCollisionEvent
                {
                    startTime = 0.5f, endTime = 0.68f, hitPhaseIndex = 0, hitboxGroupId = "Bite"
                });
                EditorUtility.SetDirty(biteMotion);
                var set = CreateAbilitySet();
                var motionSet = CreateAsset<ActorAnimationMotionSet>("Wolf_Motions");
                motionSet.attackAbilitySet = set;
                motionSet.SetAbilityMotionAsset(new MotionKey(MotionKeyValue), biteMotion);
                motionSet.motionSlots = new();
                AddSlot(motionSet, "Motion.Locomotion.Idle", CreateMotion("Wolf_Idle", idle, true));
                AddSlot(motionSet, "Motion.Locomotion.Walk", CreateMotion("Wolf_Walk", LoadClip("Wolf_Walk"), true));
                var run = CreateMotion("Wolf_Run", LoadClip("Wolf_Run"), true);
                AddSlot(motionSet, "Motion.Locomotion.Run", run);
                AddSlot(motionSet, "Motion.Locomotion.Sprint", run);
                AddSlot(motionSet, "Motion.Reaction.Death", CreateMotion("Wolf_Death", death, false));
                var hitMotion = CreateMotion("Wolf_Hit", hit, false);
                foreach (string slot in new[] { "Motion.Reaction.Hit.F", "Motion.Reaction.Hit.B", "Motion.Reaction.Hit.L", "Motion.Reaction.Hit.R", "Motion.Reaction.Knockback", "Motion.Reaction.Stun" })
                    AddSlot(motionSet, slot, hitMotion);
                var behavior = CreateAsset<EnemyBehaviorSO>("Wolf_Behavior");
                behavior.behaviorTree = MonsterBehaviorTreeJsonImporter.ImportFromMonsterBehaviorJson(
                    Path.GetFullPath("Assets/10.Datas/AI/BehaviorTree/SourceJson/EnemyBehavior_WoodlandWolf.json"),
                    "Assets/10.Datas/AI/BehaviorTree/Generated/EnemyBehavior_WoodlandWolf.asset");
                behavior.enablePatrol = true;
                behavior.patrolRadius = 10;
                behavior.patrolWaitTime = 3;
                behavior.optimalCombatDistance = 1.6f;
                behavior.chaseStopDistance = 1.35f;
                behavior.minCombatDistance = 1;
                behavior.personalSpaceDistance = 0.55f;
                behavior.retreatDistance = 2;
                behavior.actionDelayMin = 1.2f;
                behavior.actionDelayMax = 2;
                behavior.continueAttackChance = behavior.guardChance = 0;
                var source = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>("Assets/10.Datas/Actor/DataBase/SpiderMinion_1.asset");
                var definition = CreateAsset<ActorDefinitionSO>("Wolf_Definition");
                definition.actorId = "WoodlandWolf";
                definition.displayName = "숲 늑대";
                definition.actorType = ActorType.Monster | ActorType.Combat;
                definition.attributeProfile = source.attributeProfile;
                definition.poiseData = source.poiseData;
                definition.targetLayerMask = LayerMask.GetMask("Player");
                var profile = CreateAsset<MonsterActorProfileSO>("Wolf_Profile");
                profile.abilitySet = set;
                profile.behaviorData = behavior;
                definition.monsterProfile = profile;
                ConfigurePrefab(root, definition, behavior, set, motionSet);
                bite.SampleAnimation(root, 0.5f);
                var biteBox = root.GetComponentInChildren<CombatHitbox>(true).GetComponent<BoxCollider>();
                var bitePayload = (UPlayGroundMotionAbilityPayloadSO)set.additionalAbilities.Last().variants[0].executionPayload;
                var phase = bitePayload.attackInfo.baseInfo.hitPhases[0];
                phase.impactOffset = root.transform.InverseTransformPoint(biteBox.transform.TransformPoint(biteBox.center));
                phase.targetingRange = set.additionalAbilities.Last().activation.maxDistance;
                EditorUtility.SetDirty(bitePayload);
                idle.SampleAnimation(root, 0);

                // 모든 연결을 만든 뒤 동일 프리팹에 저장해 씬 인스턴스의 GUID를 유지한다.
                definition.prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
                foreach (var asset in new ScriptableObject[] { motionSet, behavior, definition, profile }) EditorUtility.SetDirty(asset);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static AbilitySetSO CreateAbilitySet()
        {
            var source = AssetDatabase.LoadAssetAtPath<AbilitySetSO>(SourceSetPath);
            var sourceAttack = source.EnumerateAll().First(a => a.variants.Any(v =>
                v.executionPayload is UPlayGroundMotionAbilityPayloadSO p && p.attackInfo.aiSelectable));
            var ability = UnityEngine.Object.Instantiate(sourceAttack);
            ability.name = "GA_WoodlandWolf_Bite";
            ability.abilityId = "Monster.WoodlandWolf.Bite";
            ability.presentation.displayName = "물기";
            ability.presentation.description = "몸을 낮춘 뒤 앞을 물어뜯는다.";
            ability.activation.maxDistance = 1.7f;
            ability.cooldown.durationSeconds = 2.5f;
            ability.editorMemo = "소형 근접 적 SpiderMinion 기본 공격의 피해·쿨다운 구조를 기준으로 독립 제작.";
            var payload = UnityEngine.Object.Instantiate((UPlayGroundMotionAbilityPayloadSO)sourceAttack.variants[0].executionPayload);
            payload.name = "Payload_WoodlandWolf_Bite";
            payload.attackInfo.motionKey = new MotionKey(MotionKeyValue);
            payload.attackInfo.baseInfo.hitPhases[0].hitboxGroupId = "Bite";
            var reaction = payload.attackInfo.baseInfo.hitPhases[0].reactionProfile;
            reaction.useAutoReaction = true;
            reaction.autoData.impactTime = 0.5f;
            reaction.autoData.hitStopDuration = 0.04f;
            reaction.autoData.hitStopScale = 0.15f;
            ability.variants[0].executionPayload = payload;
            AssetDatabase.CreateAsset(payload, Root + "/Payload_WoodlandWolf_Bite.asset");
            AssetDatabase.CreateAsset(ability, Root + "/GA_WoodlandWolf_Bite.asset");
            var set = CreateAsset<AbilitySetSO>("AbilitySet_WoodlandWolf");
            // 다른 종의 신규 공격까지 상속하지 않도록 공용 라우터만 공유한다.
            set.additionalAbilities.AddRange(source.EnumerateAll().Where(a => a != sourceAttack));
            set.additionalAbilities.Add(ability);
            set.tagTriggerMigrationVersion = source.tagTriggerMigrationVersion;
            EditorUtility.SetDirty(set);
            return set;
        }

        private static void ConfigurePrefab(GameObject root, ActorDefinitionSO definition, EnemyBehaviorSO behavior,
            AbilitySetSO set, ActorAnimationMotionSet motionSet)
        {
            UnityEngine.Object.DestroyImmediate(root.GetComponent<Gameplay.World.LivingWorld.WildlifeCreature>());
            UnityEngine.Object.DestroyImmediate(root.GetComponent<NavMeshAgent>());
            foreach (var t in root.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = LayerMask.NameToLayer("Enemy");
            var motor = root.AddComponent<KinematicCharacterMotor>();
            motor.SetCapsuleDimensions(0.35f, 1.2f, 0.6f);
            motor.StableGroundLayers = LayerMask.GetMask("Default", "Ground", "InteractableObject");
            var movement = root.AddComponent<EnemyMovementController>();
            movement.MaxWalkMoveSpeed = 0.6f;
            movement.MaxRunMoveSpeed = 1.7f;
            movement.MaxSprintMoveSpeed = 2.4f;
            var actor = root.AddComponent<MonsterActor>();
            SetObject(actor, "_definition", definition);
            var animator = root.AddComponent<ActorAnimator>();
            SetObject(animator, "_motionSet", motionSet);
            var detection = root.AddComponent<EnemyDetection>();
            var detectionData = new SerializedObject(detection);
            detectionData.FindProperty("_detectionRadius").floatValue = 8;
            detectionData.FindProperty("_lostTargetRadius").floatValue = 15;
            detectionData.FindProperty("_maxChaseDistanceFromAnchor").floatValue = 24;
            detectionData.FindProperty("_fieldOfView").floatValue = 360;
            detectionData.FindProperty("_targetLayer").intValue = LayerMask.GetMask("Player");
            detectionData.FindProperty("_obstacleLayer").intValue = LayerMask.GetMask("Default", "Ground", "InteractableObject");
            detectionData.ApplyModifiedPropertiesWithoutUndo();
            SetObject(root.AddComponent<EnemyAIController>(), "_behaviorData", behavior);
            root.AddComponent<BehaviorTreeRunner>();
            var combat = root.AddComponent<EnemyCombat>();
            SetObject(combat, "_abilitySet", set);
            SetObject(combat, "_attackOrigin", root.transform);
            var poise = root.AddComponent<PoiseStat>();
            SetObject(poise, "_data", definition.poiseData);
            var head = root.GetComponentsInChildren<Transform>(true).First(t => t.name == "Head");
            var hitboxObject = new GameObject("BiteHitbox");
            hitboxObject.transform.SetParent(head, false);
            // 월드 미터 기준 형상을 본 로컬 좌표로 옮겨 FBX의 단위 스케일을 보존한다.
            hitboxObject.transform.position = root.transform.TransformPoint(new Vector3(0, 1, 1.05f));
            hitboxObject.transform.rotation = root.transform.rotation;
            Vector3 scale = head.lossyScale;
            hitboxObject.transform.localScale = new Vector3(1 / scale.x, 1 / scale.y, 1 / scale.z);
            hitboxObject.layer = LayerMask.NameToLayer("HitBox");
            var shape = hitboxObject.AddComponent<BoxCollider>();
            shape.size = new Vector3(0.65f, 0.65f, 0.8f);
            hitboxObject.AddComponent<CombatHitbox>().Configure("Bite", shape, true, 0.1f, 8);
            var center = new GameObject("Center").transform;
            center.SetParent(root.transform, false);
            center.localPosition = Vector3.up * 0.8f;
            var sockets = new SerializedObject(actor);
            var list = sockets.FindProperty("_socketDict._serializedList");
            list.arraySize = 2;
            for (int i = 0; i < 2; i++)
            {
                list.GetArrayElementAtIndex(i).FindPropertyRelative("Key").intValue = (int)(i == 0 ? ActorSocketType.Center : ActorSocketType.UI_HpBar);
                list.GetArrayElementAtIndex(i).FindPropertyRelative("Value").objectReferenceValue = center;
            }
            sockets.ApplyModifiedPropertiesWithoutUndo();
        }

        private enum PoseKind { Bite, Hit, Death }

        private static AnimationClip CreatePoseClip(GameObject root, AnimationClip idle, string name, float length, PoseKind kind)
        {
            var clip = new AnimationClip { name = name, frameRate = 30 };
            var transforms = root.GetComponentsInChildren<Transform>(true).Where(t => t != root.transform).ToArray();
            var curves = new AnimationCurve[transforms.Length, 7];
            for (int i = 0; i < transforms.Length; i++)
                for (int j = 0; j < 7; j++) curves[i, j] = new AnimationCurve();
            var head = transforms.First(t => t.name == "Head");
            var neck = transforms.First(t => t.name == "Neck");
            var spine = transforms.First(t => t.name == "Spine");
            for (int frame = 0; frame <= Mathf.RoundToInt(length * 30); frame++)
            {
                float time = frame / 30f;
                idle.SampleAnimation(root, 0);
                if (kind == PoseKind.Bite)
                {
                    float prepare = Mathf.Clamp01(time / 0.4f) * (1 - Mathf.Clamp01((time - 0.4f) / 0.1f));
                    float strike = Mathf.Clamp01((time - 0.4f) / 0.12f) * (1 - Mathf.Clamp01((time - 0.65f) / 0.45f));
                    neck.rotation = Quaternion.AngleAxis(prepare * -15 + strike * 22, root.transform.right) * neck.rotation;
                    head.position += root.transform.forward * (-prepare * 0.12f + strike * 0.25f);
                }
                else if (kind == PoseKind.Hit)
                {
                    float recoil = Mathf.Sin(time / length * Mathf.PI);
                    head.position -= root.transform.forward * recoil * 0.12f;
                    spine.rotation = Quaternion.AngleAxis(recoil * 8, root.transform.forward) * spine.rotation;
                }
                else
                {
                    float fall = Mathf.SmoothStep(0, 1, time / length);
                    spine.rotation = Quaternion.AngleAxis(fall * 75, root.transform.forward) * spine.rotation;
                    spine.position -= root.transform.up * fall * 0.45f;
                }
                for (int i = 0; i < transforms.Length; i++)
                {
                    Vector3 position = transforms[i].localPosition;
                    Quaternion rotation = transforms[i].localRotation;
                    for (int j = 0; j < 3; j++) curves[i, j].AddKey(time, position[j]);
                    for (int j = 0; j < 4; j++) curves[i, j + 3].AddKey(time, rotation[j]);
                }
            }
            for (int i = 0; i < transforms.Length; i++)
            {
                string path = AnimationUtility.CalculateTransformPath(transforms[i], root.transform);
                for (int j = 0; j < 7; j++)
                    AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(path, typeof(Transform),
                        j < 3 ? "m_LocalPosition." + "xyz"[j] : "m_LocalRotation." + "xyzw"[j - 3]), curves[i, j]);
            }
            clip.EnsureQuaternionContinuity();
            AssetDatabase.CreateAsset(clip, Root + "/" + name + ".anim");
            idle.SampleAnimation(root, 0);
            return clip;
        }

        private static MotionSetAsset CreateMotion(string name, AnimationClip clip, bool loop)
        {
            var asset = CreateAsset<MotionSetAsset>(name);
            asset.motionSet = MotionSet.CreateAuthored(name, loop ? MotionSectionEndPolicy.LoopSelf : MotionSectionEndPolicy.Stop);
            asset.motionSet.motions.Add(new UPlayGround.Animation.Motion { id = Guid.NewGuid().ToString("N"), motionName = name, motionClip = clip });
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void AddSlot(ActorAnimationMotionSet set, string tag, MotionSetAsset motion)
            => set.motionSlots[GameplayTagRegistry.GetRequired(tag)] = motion;

        private static AnimationClip LoadClip(string name)
            => AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/10.Datas/World/Wildlife/Clips/" + name + ".anim");

        private static T CreateAsset<T>(string name) where T : ScriptableObject
        {
            string path = Root + "/" + name + ".asset";
            if (AssetDatabase.LoadMainAssetAtPath(path) != null) throw new InvalidOperationException("기존 에셋을 덮어쓸 수 없습니다: " + path);
            var asset = ScriptableObject.CreateInstance<T>();
            asset.name = name;
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static void SetObject(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var data = new SerializedObject(target);
            data.FindProperty(field).objectReferenceValue = value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
