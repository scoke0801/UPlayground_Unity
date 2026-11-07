using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Animation;
using UPlayGround.Combat;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Ability;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Codex;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Tool.Editor.Balance;

namespace UPlayGround.Editor
{
    public static partial class HumanoidSpearAuthoring
    {
        [Serializable] public sealed class Window { public float start, end; }
        [Serializable] public sealed class Attack
        {
            public string id, displayName, description, sourceAbility, sourceMotion, role, category, group;
            public int level;
            public float[] damage, poise;
            public Window[] windows;
            public float minDistance, maxDistance, cooldown, weight;
            public bool dangerRing;
        }
        [Serializable] public sealed class Recipe
        {
            public string actorId, displayName;
            public int level;
            public float optimalDistance, minimumDistance, chaseStopDistance, personalSpace;
            public Attack[] attacks;
        }

        /// <summary>신규 에셋만 저장하고 검증한 뒤 기존 DB에 공개한다. 실패하면 이 배치만 복구한다.</summary>
        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/전투/신규 일반 창병 제작")]
        public static string Apply(bool preview = true, int failurePoint = 0)
        {
            if (EditorApplication.isUpdating || EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode || EditorUtility.scriptCompilationFailed)
                throw new InvalidOperationException("임포트·컴파일이 끝난 Edit Mode 필요");
            if (File.Exists(DefinitionPath) || File.Exists(SetPath) || File.Exists(PrefabPath)) throw new InvalidOperationException("창병이 이미 존재합니다. 재생성하지 않습니다.");
            var recipe = JsonUtility.FromJson<Recipe>(File.ReadAllText(RecipePath));
            var source = Load<ActorDefinitionSO>(SourceDefinitionPath);
            var database = Load<ActorDatabase>("Assets/10.Datas/Actor/DataBase/ActorDatabase.asset");
            var codex = Load<MonsterCodexDatabaseSO>("Assets/10.Datas/Codex/Monster/MonsterCodexDatabase.asset");
            if (database.All.Any(d => d != null && d.actorId == recipe.actorId)) throw new InvalidOperationException("actorId 충돌");
            var backups = new[] { AssetDatabase.GetAssetPath(database), AssetDatabase.GetAssetPath(codex) }
                .SelectMany(p => new[] { p, p + ".meta" }).ToDictionary(p => p, File.ReadAllBytes);
            string backup = LogRoot + "/Backup/" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
            if (!preview) foreach (var pair in backups) { string p = backup + "/" + pair.Key; Directory.CreateDirectory(Path.GetDirectoryName(p)); File.WriteAllBytes(p, pair.Value); }
            var staged = new Dictionary<string, UnityEngine.Object>();
            var clips = new Dictionary<AnimationClip, AnimationClip>();
            var motions = new Dictionary<MotionSetAsset, MotionSetAsset>();
            var created = new List<string>();
            var root = CreateModel();
            int group = -1;
            if (!preview)
            {
                Undo.IncrementCurrentGroup(); group = Undo.GetCurrentGroup();
                Undo.RegisterCompleteObjectUndo(new UnityEngine.Object[] { database, codex }, "일반 창병 등록");
            }
            bool committed = false;
            try
            {
                AnimationClip CopyClip(AnimationClip original)
                {
                    if (clips.TryGetValue(original, out var cached)) return cached;
                    var copy = CreateClip(original, root);
                    // 이름이 Take 001인 FBX 서브 에셋도 원본 GUID로 구분한다.
                    string id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));
                    string path = ClipRoot + "/" + copy.name.Replace(" ", "_") + "_" + id.Substring(0, 8) + ".anim";
                    staged.Add(path, copy); clips.Add(original, copy); return copy;
                }
                MotionSetAsset CopyMotion(MotionSetAsset original)
                {
                    if (motions.TryGetValue(original, out var cached)) return cached;
                    if (SerializationUtility.HasManagedReferencesWithMissingTypes(original)) throw new InvalidOperationException("원본 managed reference 누락");
                    var copy = UnityEngine.Object.Instantiate(original); copy.name = "Spear_" + original.name;
                    foreach (var part in copy.motionSet.motions) if (part.motionClip != null) part.motionClip = CopyClip(part.motionClip);
                    foreach (var layer in copy.motionSet.layers) foreach (var part in layer.motions) if (part.motionClip != null) part.motionClip = CopyClip(part.motionClip);
                    string id = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(original));
                    staged.Add(OutputRoot + "/Motions/" + copy.name + "_" + id.Substring(0, 8) + ".asset", copy); motions.Add(original, copy); return copy;
                }
                var sourceMap = Load<ActorAnimationMotionSet>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid/Humanoid_SpearAnimationSet.asset");
                var map = ScriptableObject.CreateInstance<ActorAnimationMotionSet>(); map.name = "FieldSpearAnimationSet"; map.attackWeaponType = WeaponType.Spear;
                map.motionSlots = new AYellowpaper.SerializedCollections.SerializedDictionary<UPlayGround.Gameplay.Tag.GameplayTag, MotionSetAsset>();
                // 지상 적 상태에서 쓰는 슬롯만 독립시킨다. 낚시·채집·글라이더 클립까지 복제하지 않는다.
                var slots = new[] { MotionTags.Idle, MotionTags.Walk, MotionTags.Run,
                    MotionTags.Walk_B, MotionTags.Walk_B_L45, MotionTags.Walk_B_R45, MotionTags.Walk_F_L45, MotionTags.Walk_F_L90, MotionTags.Walk_F_R45, MotionTags.Walk_F_R90,
                    MotionTags.Walk_Slow, MotionTags.Walk_Slow_B, MotionTags.Walk_Slow_B_L45, MotionTags.Walk_Slow_B_R45, MotionTags.Walk_Slow_F_L45, MotionTags.Walk_Slow_F_L90, MotionTags.Walk_Slow_F_R45, MotionTags.Walk_Slow_F_R90,
                    MotionTags.Run_B, MotionTags.Run_B_L45, MotionTags.Run_B_R45, MotionTags.Run_F_L45, MotionTags.Run_F_L90, MotionTags.Run_F_R45, MotionTags.Run_F_R90,
                    MotionTags.Dodge, MotionTags.Dodge_F, MotionTags.Dodge_B, MotionTags.Dodge_L, MotionTags.Dodge_R,
                    MotionTags.Dash_F, MotionTags.Dash_B, MotionTags.Dash_L, MotionTags.Dash_R,
                    MotionTags.Die, MotionTags.Hit_F, MotionTags.Guard, MotionTags.GuardBreak, MotionTags.Knockback, MotionTags.Knockdown, MotionTags.Knockdown_Getup,
                    MotionTags.Grabbed, MotionTags.Grabbed_End, MotionTags.Stun, MotionTags.Jump, MotionTags.Fall, MotionTags.Land };
                foreach (var slot in slots)
                {
                    var motion = sourceMap.GetMotionSetAsset(slot);
                    if (motion != null) map.motionSlots.Add(slot, CopyMotion(motion));
                }
                var set = ScriptableObject.CreateInstance<AbilitySetSO>(); set.name = "AbilitySet_Spear_Normal";
                set.baseSet = Load<AbilitySetSO>("Assets/10.Datas/Ability/Actor/Humanoid_SpeatAttackData/AbilitySet_Humanoid_SpeatAttackData.asset");
                foreach (var ability in set.baseSet.EnumerateAll())
                    if (ability.variants.Any(v => v.executionPayload is UPlayGroundMotionAbilityPayloadSO p && p.attackInfo.aiSelectable))
                        set.abilityOverrides.Add(new AbilitySetSO.AbilityOverrideEntry { sourceAbility = ability, operation = AbilitySetOverrideOperation.Remove });
                foreach (var entry in recipe.attacks)
                {
                    if (entry.damage.Length != entry.windows.Length || entry.damage.Length != entry.poise.Length) throw new InvalidOperationException("Phase 제작표 불일치");
                    string basePath = "Assets/10.Datas/Ability/Actor/Humanoid_SpeatAttackData/";
                    var original = Load<GameplayAbilitySO>(basePath + "GA_Humanoid_SpeatAttackData_" + entry.sourceAbility + ".asset");
                    var ability = UnityEngine.Object.Instantiate(original); ability.name = "GA_Spear_Normal_" + entry.id;
                    ability.abilityId = "Monster.Humanoid.Spear." + entry.id;
                    ApplyPresentation(ability, entry);
                    ability.editorMemo = "일반 창병 전용. 제작표: " + RecipePath;
                    var payload = UnityEngine.Object.Instantiate((UPlayGroundMotionAbilityPayloadSO)original.variants.Single().executionPayload); payload.name = "Payload_" + ability.name;
                    ability.variants[0].executionPayload = payload;
                    var info = payload.attackInfo;
                    info.motionKey = new MotionKey("Humanoid.Spear." + entry.id);
                    info.aiSelectable = true; info.isMotionContentPending = false; info.requiredLevel = entry.level;
                    info.attackCategory = Enum.Parse<AbilityAttackCategory>(entry.category); info.aiRoles = Enum.Parse<AbilityAIRole>(entry.role);
                    info.selectionWeight = entry.weight; info.useDangerRing = entry.dangerRing; info.useTelegraph = false;
                    info.conditionGroup = new Data.Combat.SkillConditionGroup();
                    var phaseTemplate = JsonUtility.ToJson(info.baseInfo.hitPhases[0]);
                    info.baseInfo.hitPhases.Clear();
                    for (int i = 0; i < entry.damage.Length; i++)
                    {
                        var phase = JsonUtility.FromJson<HitPhaseData>(phaseTemplate);
                        phase.damage = entry.damage[i]; phase.poiseDamage = entry.poise[i]; phase.hitboxGroupId = "MainWeapon";
                        phase.projectileDefinition = null; phase.targetingRange = entry.maxDistance;
                        if (i + 1 < entry.damage.Length) phase.reactionType = AttackReactionType.Light;
                        info.baseInfo.hitPhases.Add(phase);
                    }
                    ability.activation.minDistance = entry.minDistance; ability.activation.maxDistance = entry.maxDistance;
                    ability.cooldown.durationSeconds = entry.cooldown; ability.cooldown.cooldownGroupId = entry.group;
                    ability.balance.expectedDamage = entry.damage.Sum();
                    var originalMotion = Load<MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid/Spear/Humanoid_Spear_" + entry.sourceMotion + ".asset");
                    var motion = CopyMotion(originalMotion);
                    if (motion.motionSet.motions.Count != 1 || motion.motionSet.layers.Count != 0) throw new InvalidOperationException("공격 원본 타임라인 재검수 필요");
                    motion.motionSet.globalEvents.Clear(); motion.motionSet.motions[0].events.Clear();
                    for (int i = 0; i < entry.windows.Length; i++)
                    {
                        var window = entry.windows[i];
                        if (window.start < 0 || window.end <= window.start || window.end > motion.motionSet.TotalDuration) throw new InvalidOperationException("판정 시간 범위 오류");
                        motion.motionSet.motions[0].events.Add(new BeginCollisionEvent { startTime = window.start, endTime = window.end, hitPhaseIndex = i, hitboxGroupId = "MainWeapon" });
                    }
                    map.SetAbilityMotionAsset(info.motionKey, motion);
                    staged.Add(OutputRoot + "/Abilities/" + payload.name + ".asset", payload);
                    staged.Add(OutputRoot + "/Abilities/" + ability.name + ".asset", ability);
                    set.additionalAbilities.Add(ability);
                }
                map.attackAbilitySet = set;
                staged.Add(OutputRoot + "/FieldSpearAnimationSet.asset", map); staged.Add(SetPath, set);
                HumanoidGradeAuthoring.ValidateSet(set, false);
                if (preview) return $"일반 창병: {recipe.attacks.Length} 공격, 독립 클립 {clips.Count}, Motion {motions.Count}. 기존 액터/배치 변경 없음.";
                foreach (var path in staged.Keys) if (File.Exists(path)) throw new InvalidOperationException("경로 충돌: " + path);
                AssetDatabase.StartAssetEditing();
                try { foreach (var pair in staged.OrderBy(p => SaveOrder(p.Value))) SaveNew(pair.Value, pair.Key, created); }
                finally { AssetDatabase.StopAssetEditing(); }
                if (failurePoint == 1) throw new InvalidOperationException("실패 주입: 신규 전투 데이터 저장 후");
                var definition = UnityEngine.Object.Instantiate(source); definition.name = "Enemy_Field_Spear_001";
                definition.actorId = recipe.actorId; definition.displayName = recipe.displayName;
                definition.characterType = CharacterActorType.None;
                definition.grade = MonsterActorGrade.Normal; definition.level = recipe.level; definition.recruitableAs = CharacterActorType.None;
                definition.abilitySet = set; definition.attributeProfile = null; definition.prefab = null;
                definition.poiseData = UnityEngine.Object.Instantiate(source.poiseData);
                definition.breakGaugeData = UnityEngine.Object.Instantiate(source.EffectiveBreakGaugeData);
                definition.monsterProfile = UnityEngine.Object.Instantiate(source.monsterProfile);
                var behavior = UnityEngine.Object.Instantiate(source.EffectiveBehaviorData);
                behavior.name = "Behavior_FieldSpear"; behavior.optimalCombatDistance = recipe.optimalDistance;
                behavior.minCombatDistance = recipe.minimumDistance; behavior.chaseStopDistance = recipe.chaseStopDistance; behavior.personalSpaceDistance = recipe.personalSpace;
                definition.behaviorData = behavior;
                var profile = definition.monsterProfile;
                profile.grade = definition.grade; profile.level = definition.level; profile.abilitySet = set; profile.behaviorData = behavior;
                profile.breakGaugeData = definition.breakGaugeData; profile.recruitableAs = CharacterActorType.None;
                SaveNew(behavior, OutputRoot + "/Behavior.asset", created);
                SaveNew(definition.poiseData, OutputRoot + "/Poise.asset", created);
                SaveNew(definition.breakGaugeData, OutputRoot + "/BreakGauge.asset", created);
                SaveNew(profile, OutputRoot + "/Profile.asset", created);
                SaveNew(definition, DefinitionPath, created);
                var stat = ScriptableObject.CreateInstance<UPlayGround.Ability.Core.AttributeProfileSO>();
                MonsterStatBakeService.WriteMonsterStatValues(stat, definition);
                SaveNew(stat, OutputRoot + "/Attributes.asset", created); definition.attributeProfile = stat;
                MonsterStatBakeService.Bake(definition, new MonsterStatBakeService.Options { ForceRegenerate = true, RecordUndo = false, SyncGeneratedPoise = true, SyncExistingBreakGauge = true });
                AssetDatabase.SaveAssetIfDirty(stat); AssetDatabase.SaveAssetIfDirty(definition.poiseData); AssetDatabase.SaveAssetIfDirty(definition.breakGaugeData);
                SetReference(root.GetComponent<MonsterActor>(), "_definition", definition);
                var identity = new SerializedObject(root.GetComponent<MonsterActor>());
                identity.FindProperty("_actorId").stringValue = recipe.actorId;
                identity.ApplyModifiedPropertiesWithoutUndo();
                SetReference(root.GetComponent<EnemyCombat>(), "_abilitySet", set);
                SetReference(root.GetComponent<ActorAnimator>(), "_motionSet", map);
                SetReference(root.GetComponent<PoiseStat>(), "_data", definition.poiseData);
                EnsureFolder(Path.GetDirectoryName(PrefabPath).Replace('\\', '/')); created.Add(PrefabPath);
                definition.prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                if (definition.prefab == null) throw new InvalidOperationException("프리팹 저장 실패");
                EditorUtility.SetDirty(definition); AssetDatabase.SaveAssetIfDirty(definition);
                if (failurePoint == 2) throw new InvalidOperationException("실패 주입: 프리팹 저장 후");
                var sourceCodex = codex.GetEntry(source.actorId);
                var codexEntry = sourceCodex == null ? ScriptableObject.CreateInstance<MonsterCodexEntrySO>() : UnityEngine.Object.Instantiate(sourceCodex);
                codexEntry.actorId = recipe.actorId; codexEntry.displayNameOverride = ""; codexEntry.descriptionOverride = "";
                SaveNew(codexEntry, OutputRoot + "/Codex.asset", created);
                database.AddDefinition(definition); codex.AddEntry(codexEntry);
                if (failurePoint == 3) throw new InvalidOperationException("실패 주입: DB 공개 후");
                AssetDatabase.SaveAssetIfDirty(database); AssetDatabase.SaveAssetIfDirty(codex);
                Undo.CollapseUndoOperations(group); committed = true;
                return $"{recipe.actorId}: 일반 Lv{recipe.level}, 공격 {recipe.attacks.Length}개, DB/도감 등록. 필드 배치 없음.";
            }
            catch
            {
                if (group >= 0) Undo.RevertAllDownToGroup(group);
                AssetDatabase.StartAssetEditing();
                try { foreach (var path in created.AsEnumerable().Reverse()) AssetDatabase.DeleteAsset(path); }
                finally { AssetDatabase.StopAssetEditing(); }
                if (!preview) foreach (var pair in backups) File.WriteAllBytes(pair.Key, pair.Value);
                if (!preview) foreach (var p in backups.Keys.Where(p => !p.EndsWith(".meta"))) AssetDatabase.ImportAsset(p, ImportAssetOptions.ForceSynchronousImport);
                if (backups.Any(p => !File.ReadAllBytes(p.Key).SequenceEqual(p.Value))) throw new InvalidOperationException("DB 복구 바이트 불일치");
                Directory.CreateDirectory(LogRoot); File.WriteAllText(LogRoot + $"/rollback-{failurePoint}.txt", "DB/meta 바이트 일치. 이 배치의 신규 에셋만 제거.");
                throw;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
                if (!committed) foreach (var asset in staged.Values) if (asset != null && !EditorUtility.IsPersistent(asset)) UnityEngine.Object.DestroyImmediate(asset);
            }
        }

        private static int SaveOrder(UnityEngine.Object asset) => asset is AnimationClip ? 0 : asset is MotionSetAsset ? 1 : asset is UPlayGroundMotionAbilityPayloadSO ? 2 : asset is GameplayAbilitySO ? 3 : asset is AbilitySetSO ? 4 : 5;
        private static void ApplyPresentation(GameplayAbilitySO ability, Attack entry)
        {
            ability.presentation.displayName = entry.displayName;
            ability.presentation.description = entry.description;
            ability.presentation.icon = Load<Sprite>("Assets/04.Images/UI/WeaponIcon/SpearWeaponIcon.png");
        }
        private static void SaveNew(UnityEngine.Object asset, string path, List<string> created)
        {
            if (File.Exists(path)) throw new InvalidOperationException("기존 에셋 덮어쓰기 금지: " + path);
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            AssetDatabase.CreateAsset(asset, path); created.Add(path); EditorUtility.SetDirty(asset); AssetDatabase.SaveAssetIfDirty(asset);
        }
        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/')); AssetDatabase.CreateFolder(Path.GetDirectoryName(path).Replace('\\', '/'), Path.GetFileName(path));
        }
        private static void SetReference(UnityEngine.Object target, string name, UnityEngine.Object value)
        {
            var so = new SerializedObject(target); var property = so.FindProperty(name);
            if (property == null) throw new InvalidOperationException("프리팹 계약 필드 누락: " + name);
            property.objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
