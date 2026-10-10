using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Editor.Ability;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Party;
using UPlayGround.Data.Projectile;
using Object = UnityEngine.Object;

namespace UPlayGround.Editor
{
    /// <summary>기존 Staff 클립과 외부 VFX로 독립 전투 세트를 제작하고 Staff 캐릭터에 연결한다.</summary>
    public static class StaffMagicContentBuilder
    {
        public const string Root = "Assets/10.Datas/Ability/Player/Staff";
        public const string SetPath = Root + "/AbilitySet_PlayerStaff.asset";
        private const string VisualRoot = "Assets/03.Prefabs/VFX/StaffMagic";
        private const string MotionRoot = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Staff/Humanoid_Staff_";
        private const string MappingPath = "Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Humanoid_Player_StaffAnimationSet.asset";
        private const string SourceSetPath = "Assets/10.Datas/Ability/Migrated/PlayerDefaultAttackData/AbilitySet_PlayerDefaultAttackData.asset";
        private const string ExternalMagic = "Assets/ExternalAssets/FX/Hovl Studio/AAA Projectiles Vol 1/Prefabs/";
        private const string ToolId = "UPlayGround/툴 런처/게임플레이 · 전투/Staff 마법 전투 세트 제작";
        private static readonly List<string> CreatedPaths = new();
        private static readonly List<GameplayAbilitySO> CreatedAbilities = new();
        private static readonly Dictionary<string, string> AbilityRemaps = new();
        private static readonly Dictionary<string, byte[]> OriginalFiles = new();
        private static ActorAnimationMotionSet _mapping;
        private static AbilitySetSO _source;
        private static GameObject _cast, _slash;
        private static AudioClip _sound;
        private static ProjectileDefinitionSO _bolt, _beam;

        [UPlayGround.EditorTools.UPlaygroundTool(ToolId, false, 322)]
        public static void Create()
        {
            if (AssetDatabase.LoadAssetAtPath<AbilitySetSO>(SetPath) != null)
            {
                Validate();
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<AbilitySetSO>(SetPath);
                return;
            }
            Preflight();
            Directory.CreateDirectory("Logs/StaffMagic");
            WriteIssues("baseline");
            CreatedPaths.Clear();
            CreatedAbilities.Clear();
            AbilityRemaps.Clear();
            OriginalFiles.Clear();
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("Staff 마법 전투 세트 제작");
            try
            {
                CreateVisuals();
                var set = ScriptableObject.CreateInstance<AbilitySetSO>();
                set.baseSet = _source;
                set.editorMemo = "Staff 공용 전투. 원본 Default Ability·모션은 유지하고 묘묘·연화에 연결.";
                CreateAsset(set, SetPath);
                RecordOriginal(_mapping);
                _mapping.attackWeaponType = WeaponType.Staff;
                _mapping.attackAbilitySet = set;
                CreateCombatBindings(set);
                CreateSkills(set);
                BindCharacters(set);
                EditorUtility.SetDirty(_mapping);
                EditorUtility.SetDirty(set);
                Validate();
                AssetDatabase.SaveAssets();
                WriteIssues("after");
                Undo.CollapseUndoOperations(group);
                Debug.Log("Staff 마법 공격·스킬 및 묘묘·연화 연결 완료. Logs/StaffMagic 검증 결과를 확인하세요.");
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                for (int i = CreatedPaths.Count - 1; i >= 0; i--)
                    if (File.Exists(CreatedPaths[i]))
                        AssetDatabase.DeleteAsset(CreatedPaths[i]);
                foreach (var original in OriginalFiles)
                {
                    File.WriteAllBytes(original.Key, original.Value);
                    AssetDatabase.ImportAsset(original.Key, ImportAssetOptions.ForceUpdate);
                }
                throw;
            }
        }

        private static void Preflight()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("편집 모드에서 컴파일 완료 후 실행하세요.");
            if (AssetDatabase.IsValidFolder(Root) || AssetDatabase.IsValidFolder(VisualRoot))
                throw new InvalidOperationException("생성 대상 폴더가 이미 있습니다. 기존 데이터를 확인하세요.");
            _mapping = Load<ActorAnimationMotionSet>(MappingPath);
            _source = Load<AbilitySetSO>(SourceSetPath);
            foreach (string guid in AssetDatabase.FindAssets("t:MotionSetAsset", new[] { Path.GetDirectoryName(MotionRoot).Replace('\\', '/') }))
            {
                var motion = Load<MotionSetAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (SerializationUtility.HasManagedReferencesWithMissingTypes(motion))
                    throw new InvalidOperationException(motion.name + ": MotionEvent 타입이 누락되었습니다.");
            }
            Load<GameObject>(ExternalMagic + "Projectiles(transform)/Projectile 7 pink.prefab");
            Load<GameObject>("Assets/ExternalAssets/FX/Hovl Studio/3D Lasers Pack/Prefabs/Laser beam 5 magic.prefab");
        }

        private static void CreateVisuals()
        {
            var projectile = CloneVisual(ExternalMagic + "Projectiles(transform)/Projectile 7 pink.prefab", "StaffBolt");
            _cast = CloneVisual(ExternalMagic + "Flash and hits/Flash 7 pink.prefab", "StaffCast");
            _slash = CloneVisual("Assets/ExternalAssets/FX/Hovl Studio/Sword slash VFX/Prefabs/Sword Slash 7.prefab", "StaffSlash", scale: 0.4f);
            var beam = CloneVisual("Assets/ExternalAssets/FX/Hovl Studio/3D Lasers Pack/Prefabs/Laser beam 5 magic.prefab", "StaffBeam", true);
            _bolt = ScriptableObject.CreateInstance<ProjectileDefinitionSO>();
            _bolt.visualPrefab = projectile;
            _bolt.motion = new LinearProjectileMotion { speed = 20, maxSpeed = 20 };
            _bolt.lifetime = 2;
            _bolt.collisionRadius = 0.2f;
            _bolt.obstructionLayers = LayerMask.GetMask("Default", "Ground", "Environment", "Obstacle");
            _bolt.hitEffectKey = "LiteHit";
            _bolt.inheritOwnerTimeScale = false;
            CreateAsset(_bolt, Root + "/Projectile_StaffBolt.asset");
            _beam = Object.Instantiate(_bolt);
            _beam.visualPrefab = beam;
            _beam.motion = new HitscanProjectileMotion { range = 20, visualDuration = 0.22f };
            _beam.detachTrailOnReturn = false;
            CreateAsset(_beam, Root + "/Projectile_StaffBeam.asset");
            _sound = Load<AudioClip>("Assets/ExternalAssets/SFX/Multiple Solutions/Basic RPG Sounds/Audio/Gattling Laser 1.wav");
        }

        private static GameObject CloneVisual(string path, string name, bool beam = false, float scale = 1f)
        {
            var instance = Object.Instantiate(Load<GameObject>(path));
            try
            {
                instance.name = name;
                instance.transform.localScale *= scale;
                Transform impact = null;
                foreach (var component in instance.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (component == null) throw new InvalidOperationException(path + ": Missing Script");
                    if (beam)
                    {
                        var serialized = new SerializedObject(component);
                        var hit = serialized.FindProperty("HitEffect");
                        if (hit?.objectReferenceValue is GameObject hitObject) impact = hitObject.transform;
                    }
                    // 공급자 데모 이동·충돌·자체 삭제는 게임의 풀링 및 판정과 충돌한다.
                    Object.DestroyImmediate(component);
                }
                foreach (var collider in instance.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(collider);
                foreach (var body in instance.GetComponentsInChildren<Rigidbody>(true)) Object.DestroyImmediate(body);
                // 일부 외부 프리팹은 빈 파티클 컨테이너의 Renderer도 켜 둔다.
                foreach (var renderer in instance.GetComponentsInChildren<Renderer>(true))
                    if (renderer.sharedMaterials.All(material => material == null)) renderer.enabled = false;
                if (beam)
                {
                    var view = instance.AddComponent<ProjectileBeamVisual>();
                    var serialized = new SerializedObject(view);
                    var lines = instance.GetComponentsInChildren<LineRenderer>(true);
                    if (lines.Length == 0) throw new InvalidOperationException("레이저 LineRenderer가 없습니다.");
                    var property = serialized.FindProperty("_lines");
                    property.arraySize = lines.Length;
                    for (int i = 0; i < lines.Length; i++)
                    {
                        property.GetArrayElementAtIndex(i).objectReferenceValue = lines[i];
                        lines[i].enabled = true;
                    }
                    serialized.FindProperty("_impact").objectReferenceValue = impact;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                string destination = VisualRoot + "/" + name + ".prefab";
                EnsureFolder(VisualRoot);
                var result = PrefabUtility.SaveAsPrefabAsset(instance, destination);
                if (result == null) throw new InvalidOperationException(destination + ": 프리팹 저장 실패");
                CreatedPaths.Add(destination);
                return result;
            }
            finally { Object.DestroyImmediate(instance); }
        }

        private static void CreateCombatBindings(AbilitySetSO set)
        {
            var light = new List<GameplayAbilitySO>();
            var jump = new List<GameplayAbilitySO>();
            int lightCount = _source.GetCombatSequence(PlayerCombatAbilitySlot.LightCombo).Count;
            for (int i = 0; i < lightCount; i++)
            {
                light.Add(CreateAttack(_source.GetCombatAbility(PlayerCombatAbilitySlot.LightCombo, i),
                    "Light." + i.ToString("00"), "마력 연격 " + (i + 1), "Attack_" + (i % 3 + 1),
                    i == 5 || i == 9 ? _beam : i % 3 == 1 ? _bolt : null));
            }
            for (int i = 0; i < 3; i++)
            {
                jump.Add(CreateAttack(_source.GetCombatAbility(PlayerCombatAbilitySlot.JumpCombo, i),
                    "Jump.0" + i, "공중 마력탄 " + (i + 1), "JumpAttack_" + (i + 1), _bolt));
            }
            Bind(set, PlayerCombatAbilitySlot.LightCombo, light.ToArray());
            Bind(set, PlayerCombatAbilitySlot.JumpCombo, jump.ToArray());
            var heavy = CreateAttack(_source.GetCombatAbility(PlayerCombatAbilitySlot.HeavyCombo),
                "Heavy.00", "섬광", "HeavyAttack_1", _beam);
            Bind(set, PlayerCombatAbilitySlot.HeavyCombo, heavy);
            var counter = CreateAttack(_source.GetCombatAbility(PlayerCombatAbilitySlot.CounterAttack),
                "Counter", "마력 반격", "CounterAttack", null);
            Bind(set, PlayerCombatAbilitySlot.CounterAttack, counter);
            Bind(set, PlayerCombatAbilitySlot.ParryCounterAttack, counter);
            var entry = CreateAttack(_source.GetCombatAbility(PlayerCombatAbilitySlot.EntryAttack),
                "Entry", "마력 휩쓸기", "Swap_Attack", null);
            foreach (var slot in new[] { PlayerCombatAbilitySlot.EntryAttack, PlayerCombatAbilitySlot.EntryAttackVsGroggy,
                         PlayerCombatAbilitySlot.EntryAttackVsAirborne, PlayerCombatAbilitySlot.SwapEvadeCounterAttack,
                         PlayerCombatAbilitySlot.SwapSpecialAttack }) Bind(set, slot, entry);
        }

        private static void CreateSkills(AbilitySetSO set)
        {
            var skill = CreateAttack(_source.GetPlayerAbility(PlayerSkillSlot.Ability),
                "Ability", "마력탄", "Skill_1", _bolt, 0.65f);
            var ultimate = CreateAttack(_source.GetPlayerAbility(PlayerSkillSlot.Ultimate),
                "Ultimate", "마력 해방", "Skill_2", _beam, 1.05f);
            set.playerSlots.Add(new AbilitySetSO.PlayerSlotEntry { slot = PlayerSkillSlot.Ability, ability = skill });
            set.playerSlots.Add(new AbilitySetSO.PlayerSlotEntry { slot = PlayerSkillSlot.Ultimate, ability = ultimate });
            var charge = CreateAttack(_source.GetCombatAbility(PlayerCombatAbilitySlot.HeavyCombo),
                "Charge", "집중 포격", "Skill_3", _beam, 2.0f);
            set.overrideCharge = true;
            set.charge = new PlayerChargeAbilitySettings
            {
                stages = new List<GameplayAbilitySO> { charge }, stageThresholds = new List<float>(),
                interruptActions = PlayerInterruptAction.Dodge | PlayerInterruptAction.Dash
            };
            var dash = CreateAttack(_source.GetCombatAbility(PlayerCombatAbilitySlot.DashAttack),
                "Dash", "속사", "Skill_4", _bolt, 0.4f);
            Bind(set, PlayerCombatAbilitySlot.DashAttack, dash);
            // 공용 세트의 다른 무기 콤보 라우트가 Staff 모션으로 우회 실행되지 않게 한다.
            set.overrideComboRoutes = true;
            set.comboRoutes.Clear();
        }

        private static GameplayAbilitySO CreateAttack(GameplayAbilitySO source, string key, string label,
            string motionName, ProjectileDefinitionSO projectile, float impact = -1f)
        {
            if (source == null) throw new InvalidOperationException(key + ": 원본 Ability가 없습니다.");
            var ability = Object.Instantiate(source);
            ability.name = "GA_PlayerStaff_" + key.Replace('.', '_');
            ability.abilityId = "Player.Staff." + key;
            AbilityRemaps.TryAdd(source.abilityId, ability.abilityId);
            ability.presentation.displayName = label;
            ability.presentation.description = projectile == _beam ? "전방으로 마력의 빛을 쏩니다." :
                projectile != null ? "전방으로 마력탄을 날립니다." : "지팡이에 마력을 실어 휘두릅니다.";
            ability.cooldown.cooldownGroupId = ability.abilityId;
            ability.editorMemo = "Staff 독립 파생. 원본 " + source.abilityId + "의 비용·쿨다운·타격 수치를 기준으로 제작.";
            ability.variants.RemoveRange(1, ability.variants.Count - 1);
            var variant = ability.variants[0];
            var payload = Object.Instantiate((UPlayGroundMotionAbilityPayloadSO)variant.executionPayload);
            payload.name = ability.name + "_Payload";
            payload.attackInfo.motionKey = new MotionKey("Staff." + key);
            payload.attackInfo.skillType = SkillType.Attack;
            payload.attackInfo.interruptActions = PlayerInterruptAction.Dodge | PlayerInterruptAction.Dash
                | PlayerInterruptAction.Move | PlayerInterruptAction.HeavyAttack | PlayerInterruptAction.Skill;
            if (payload.attackInfo.baseInfo == null || !payload.attackInfo.baseInfo.HasHitPhases)
            {
                var heavy = (UPlayGroundMotionAbilityPayloadSO)_source.GetCombatAbility(PlayerCombatAbilitySlot.HeavyCombo).variants[0].executionPayload;
                payload.attackInfo.baseInfo = JsonUtility.FromJson<AttackInfoBase>(JsonUtility.ToJson(heavy.attackInfo.baseInfo));
                ability.editorMemo += " 히트가 없는 원본은 기본 강공격의 첫 페이즈를 기준으로 사용.";
            }
            var phase = payload.attackInfo.baseInfo.hitPhases[0];
            payload.attackInfo.baseInfo.hitPhases = new List<HitPhaseData> { phase };
            phase.projectileDefinition = projectile;
            if (key == "Charge") phase.damage *= 1.5f;
            if (key == "Ultimate") phase.damage *= 3f;
            phase.reactionProfile.useAutoReaction = true;
            phase.reactionProfile.useManualOverride = true;
            phase.reactionProfile.manualOverride.overrideHitStop = true;
            phase.reactionProfile.manualOverride.hitStopDuration = projectile == null ? 0.045f : 0.015f;
            phase.reactionProfile.manualOverride.hitStopScale = 0.1f;
            variant.executionPayload = payload;
            CreateAsset(ability, Root + "/" + ability.name + ".asset");
            AssetDatabase.AddObjectToAsset(payload, ability);
            Undo.RegisterCreatedObjectUndo(payload, "Staff Payload 생성");
            var motion = Object.Instantiate(Load<MotionSetAsset>(MotionRoot + motionName + ".asset"));
            motion.name = "Staff_" + key.Replace('.', '_');
            CreateAsset(motion, Root + "/Motions/" + motion.name + ".asset");
            AuthorEvents(motion, projectile, impact);
            if (key.StartsWith("Light.", StringComparison.Ordinal)
                && !motion.motionSet.motions[0].events.Any(e => e is ComboWindowEvent))
            {
                motion.motionSet.motions[0].events.Add(new ComboWindowEvent
                {
                    startTime = 0.5f, endTime = 0.85f
                });
            }
            if (key == "Charge")
            {
                ConfigureChargeMotion(motion);
            }
            _mapping.SetAbilityMotionAsset(payload.attackInfo.motionKey, motion);
            ability.balance.expectedDamage = phase.damage;
            ability.balance.expectedDuration = motion.motionSet.motions.Sum(m => m.Duration);
            EditorUtility.SetDirty(ability);
            EditorUtility.SetDirty(payload);
            EditorUtility.SetDirty(motion);
            CreatedAbilities.Add(ability);
            return ability;
        }

        /// <summary>홀드를 놓으면 원본 클립의 발사 자세로 연결해 불필요한 대기 동작을 생략한다.</summary>
        internal static void ConfigureChargeMotion(MotionSetAsset asset)
        {
            if (asset.motionSet.motions.Count != 1) return;
            var hold = asset.motionSet.motions[0];
            var release = new UPlayGround.Animation.Motion
            {
                motionName = "발사", motionClip = hold.motionClip,
                clipStartTime = 3.5f, clipEndTime = hold.motionClip.length,
                events = hold.events.Where(e => e is not LoopEvent && e is not CancelWindowEvent).ToList()
            };
            foreach (var evt in release.events)
            {
                evt.startTime = Mathf.Max(0, evt.startTime - 1.85f);
                evt.endTime = Mathf.Max(evt.startTime, evt.endTime - 1.85f);
            }
            release.events.Add(new CancelWindowEvent
            {
                startTime = 0.33f, endTime = release.Duration,
                maskOverride = PlayerInterruptAction.Dodge | PlayerInterruptAction.Dash
            });
            hold.motionName = "집중";
            hold.clipEndTime = 0.95f;
            hold.events = new List<MotionEventBase>
            {
                new LoopEvent { startTime = 0.8f, endTime = 0.9f, mode = LoopEventMode.InfiniteLoop },
                new CancelWindowEvent { startTime = 0, endTime = 0.95f,
                    maskOverride = PlayerInterruptAction.Dodge | PlayerInterruptAction.Dash }
            };
            asset.motionSet.motions.Add(release);
        }

        private static void AuthorEvents(MotionSetAsset asset, ProjectileDefinitionSO projectile, float impact)
        {
            var motion = asset.motionSet.motions[0];
            float duration = motion.Duration;
            var collision = motion.events.OfType<BeginCollisionEvent>().FirstOrDefault();
            var spawn = motion.events.OfType<SpawnProjectileEvent>().FirstOrDefault();
            float fire = impact >= 0 ? impact : collision?.startTime ?? spawn?.startTime ?? -1;
            if (fire < 0 || fire >= duration) throw new InvalidOperationException(asset.name + ": 발동 시점 근거가 없습니다.");
            float end = collision?.endTime ?? Mathf.Min(duration, fire + 0.18f);
            motion.events.RemoveAll(e => e is SpawnProjectileEvent || e is CancelWindowEvent);
            if (projectile != null)
            {
                motion.events.RemoveAll(e => e is BeginCollisionEvent);
                motion.events.Add(new SpawnProjectileEvent
                {
                    startTime = fire, endTime = Mathf.Min(duration, fire + 0.05f),
                    hitPhaseIndex = 0, projectileDefinition = null,
                    spawnPointName = "StaffMagicMuzzle", spawnOffset = Vector3.zero,
                    targetMode = ProjectileTargetMode.LockOnTarget, targetOffset = Vector3.up,
                    useSpawnRotation = false, evaluationPhase = MotionEventEvaluationPhase.PostAnimationEvaluation
                });
            }
            else if (collision != null)
            {
                collision.hitPhaseIndex = 0;
                // 캐릭터별 지팡이 프리팹에 HitBox가 없어도 동일한 근접 사거리를 제공한다.
                collision.collisionSource = CollisionSourceType.ExplicitShape;
                collision.explicitShape = new ExplicitCollisionShapeData
                {
                    shapeType = CollisionShapeType.Sphere, radius = 1.15f,
                    anchor = CollisionAnchorType.ActorRoot, localOffset = new Vector3(0, 1, 1.2f),
                    anchorSampling = CollisionAnchorSampling.FollowDuringWindow,
                    evaluation = CollisionEvaluationType.Window, direction = CollisionDirectionType.ActorForward
                };
            }
            motion.events.Add(new BeginParticleEvent
            {
                startTime = Mathf.Max(0, fire - 0.22f), endTime = fire + 0.12f,
                particlePrefab = _cast, spawnPointName = "StaffMagicMuzzle", attachToTarget = true, destroyOnFinish = true
            });
            if (projectile == null)
                motion.events.Add(new BeginParticleEvent
                {
                    startTime = fire, endTime = end, particlePrefab = _slash,
                    offset = new Vector3(0, 1, 1), attachToTarget = false,
                    destroyOnFinish = false, particleLifeTime = 1.5f, useSpawnRotation = true
                });
            motion.events.Add(new PlaySoundEvent { startTime = fire, endTime = fire, audioClip = _sound, volume = 0.35f });
            motion.events.Add(new CancelWindowEvent
            {
                startTime = Mathf.Min(duration - 0.01f, end), endTime = duration,
                maskOverride = PlayerInterruptAction.Dodge | PlayerInterruptAction.Dash | PlayerInterruptAction.Move
                    | PlayerInterruptAction.HeavyAttack | PlayerInterruptAction.Skill
            });
        }

        private static void BindCharacters(AbilitySetSO set)
        {
            int count = 0;
            foreach (string guid in AssetDatabase.FindAssets("t:PlayerCharacterDefinitionSO"))
            {
                var character = Load<PlayerCharacterDefinitionSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (character.defaultWeaponType != WeaponType.Staff) continue;
                if (character.abilitySet != _source) throw new InvalidOperationException(character.name + ": 예상과 다른 원본 Set입니다.");
                RecordOriginal(character);
                character.abilitySet = set;
                EditorUtility.SetDirty(character);
                BindSkillTree(character);
                EnsureCastSocket(character);
                count++;
            }
            if (count == 0) throw new InvalidOperationException("Staff 사용 캐릭터가 없습니다.");
        }

        /// <summary>본 이름이 다른 모델에도 같은 모션이 손 위치를 안정적으로 참조하게 한다.</summary>
        internal static void EnsureCastSocket(PlayerCharacterDefinitionSO character)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/03.Prefabs/Actor/Player/Models" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var prefab = Load<GameObject>(path);
                var data = prefab.GetComponentInChildren<UPlayGround.Components.CharacterModelData>(true);
                if (data == null || data.Definition != character) continue;
                RecordOriginal(prefab);
                var instance = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var animator = instance.GetComponentInChildren<Animator>(true);
                    Transform hand = animator != null && animator.isHuman
                        ? animator.GetBoneTransform(HumanBodyBones.RightHand) : null;
                    if (hand == null) throw new InvalidOperationException(path + ": 오른손 본이 없습니다.");
                    Transform muzzle = hand.Find("StaffMagicMuzzle");
                    if (muzzle == null)
                    {
                        muzzle = new GameObject("StaffMagicMuzzle").transform;
                        muzzle.SetParent(hand, false);
                    }
                    PrefabUtility.SaveAsPrefabAsset(instance, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(instance); }
            }
        }

        private static void Bind(AbilitySetSO set, PlayerCombatAbilitySlot slot, params GameplayAbilitySO[] abilities)
            => set.combatBindings.Add(new PlayerCombatAbilityBinding { slot = slot, abilities = abilities.ToList() });

        private static void BindSkillTree(PlayerCharacterDefinitionSO character)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:CharacterSkillTreeSO"))
            {
                var tree = Load<CharacterSkillTreeSO>(AssetDatabase.GUIDToAssetPath(guid));
                if (tree.characterType != character.characterType) continue;
                RecordOriginal(tree);
                foreach (var node in tree.nodes)
                foreach (var effect in node.effects)
                {
                    if (effect is AbilityUnlockEffect unlock && AbilityRemaps.TryGetValue(unlock.abilityId, out string unlockedId))
                        unlock.abilityId = unlockedId;
                    if (effect is AbilityScalarEffect scalar && AbilityRemaps.TryGetValue(scalar.abilityId, out string scalarId))
                        scalar.abilityId = scalarId;
                }
                EditorUtility.SetDirty(tree);
            }
        }

        /// <summary>실제 Staff 소유자의 Set·Motion·HitPhase와 연출 참조를 검사한다.</summary>
        public static void Validate()
        {
            var set = Load<AbilitySetSO>(SetPath);
            var mapping = Load<ActorAnimationMotionSet>(MappingPath);
            var issues = new List<AbilityValidationIssue>();
            issues.AddRange(AbilityDataValidator.Validate(set));
            var report = new List<string>();
            foreach (var ability in set.EnumerateAll().Where(a => a.abilityId.StartsWith("Player.Staff.", StringComparison.Ordinal)))
            {
                issues.AddRange(AbilityDataValidator.Validate(ability));
                var payload = (UPlayGroundMotionAbilityPayloadSO)ability.variants[0].executionPayload;
                var motion = mapping.GetAbilityMotionAsset(payload.attackInfo.motionKey);
                if (motion == null || SerializationUtility.HasManagedReferencesWithMissingTypes(motion))
                    throw new InvalidOperationException(ability.name + ": 모션 매핑 또는 타입 오류");
                foreach (var part in motion.motionSet.motions)
                {
                    if (part.motionClip == null) throw new InvalidOperationException(motion.name + ": 클립 없음");
                    foreach (var evt in part.events)
                    {
                        if (evt == null || evt.startTime < 0 || evt.endTime > part.Duration + 0.001f)
                            throw new InvalidOperationException(motion.name + ": 이벤트 시간/참조 오류");
                        if (evt is SpawnProjectileEvent shot && payload.attackInfo.baseInfo.GetHitPhase(shot.hitPhaseIndex).projectileDefinition == null)
                            throw new InvalidOperationException(ability.name + ": 투사체 정의 없음");
                    }
                }
                report.Add(ability.abilityId + " → " + motion.name);
            }
            if (issues.Any(i => i.Severity == AbilityValidationSeverity.Error))
                throw new InvalidOperationException(string.Join("\n", issues.Select(i => i.Context + ": " + i.Message)));
            Directory.CreateDirectory("Logs/StaffMagic");
            File.WriteAllLines("Logs/StaffMagic/connections.txt", report.Concat(issues.Select(i => i.Severity + ": " + i.Message)));
        }

        private static void WriteIssues(string name)
            => File.WriteAllLines("Logs/StaffMagic/" + name + ".txt", AbilityDataValidator.ValidateAll()
                .Select(i => i.Severity + "|" + AssetDatabase.GetAssetPath(i.Context) + "|" + i.Message));

        private static T Load<T>(string path) where T : Object
            => AssetDatabase.LoadAssetAtPath<T>(path) ?? throw new InvalidOperationException("에셋 없음: " + path);

        private static void CreateAsset(Object asset, string path)
        {
            if (File.Exists(path)) throw new InvalidOperationException("기존 에셋을 덮어쓸 수 없습니다: " + path);
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateAsset(asset, path);
            CreatedPaths.Add(path);
            Undo.RegisterCreatedObjectUndo(asset, "Staff 데이터 생성");
        }

        private static void RecordOriginal(Object asset)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (!OriginalFiles.ContainsKey(path)) OriginalFiles.Add(path, File.ReadAllBytes(path));
            Undo.RegisterCompleteObjectUndo(asset, "Staff 연결 변경");
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
