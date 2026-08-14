using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;
using UPlayGround.Tool.Editor.Balance;

namespace UPlayGround.Editor.P09Builder
{
    public sealed class SyncActorDatabaseStep : IBuildStep
    {
        private const string DefaultActorDatabasePath = "Assets/10.Datas/Actor/DataBase/ActorDatabase.asset";
        private const string GeneratedStatPath = "Assets/10.Datas/Stat/Generated";

        public void Execute(BuildContext ctx)
        {
            if (ctx == null || ctx.Config == null ||
                (ctx.Config.ActorKind != BuilderActorKind.Enemy &&
                 ctx.Config.ActorKind != BuilderActorKind.Npc))
                return;

            var prefab = ctx.Bag.TryGetValue("finalPrefabAsset", out var prefabObj)
                ? prefabObj as GameObject
                : null;
            if (prefab == null)
            {
                Debug.LogWarning("[P09Builder] ActorDatabase 동기화 실패: 저장된 프리팹을 찾지 못했습니다.");
                return;
            }

            var actorId = ctx.PrefabName;
            if (string.IsNullOrEmpty(actorId))
            {
                Debug.LogWarning("[P09Builder] ActorDatabase 동기화 실패: ActorId가 비어있습니다.");
                return;
            }

            InjectPrefabActorId(prefab, actorId);

            var database = LoadActorDatabase();
            if (database == null)
            {
                Debug.LogWarning("[P09Builder] ActorDatabase를 찾지 못해 ActorDefinition 등록을 건너뜁니다.");
                return;
            }

            var definition = FindDefinition(database, actorId);
            if (definition == null)
                definition = CreateDefinitionAsset(ctx, actorId);

            Undo.RecordObject(definition, "P09 Builder: Update Actor Definition");
            Undo.RecordObject(database, "P09 Builder: Register Actor Definition");
            ApplyDefinitionDefaults(definition, prefab, actorId, ctx);
            InjectPrefabDefinition(prefab, definition);
            database.AddDefinition(definition);
            database.InvalidateLookup();

            EditorUtility.SetDirty(definition);
            EditorUtility.SetDirty(database);
            Debug.Log($"[P09Builder] ActorDatabase 자동 동기화 완료: {actorId}");
        }

        private static void InjectPrefabActorId(GameObject prefab, string actorId)
        {
            var actor = prefab.GetComponent<GameActor>();
            if (actor == null)
            {
                Debug.LogWarning($"[P09Builder] '{prefab.name}' 프리팹 루트에 GameActor가 없어 ActorId를 주입하지 못했습니다.");
                return;
            }

            ReflectionUtil.SetField(actor, "_actorId", actorId);
            EditorUtility.SetDirty(actor);
            EditorUtility.SetDirty(prefab);
        }

        private static void InjectPrefabDefinition(GameObject prefab, ActorDefinitionSO definition)
        {
            var actor = prefab != null ? prefab.GetComponent<GameActor>() : null;
            if (actor == null || definition == null)
                return;

            ReflectionUtil.SetField(actor, "_definition", definition);
            EditorUtility.SetDirty(actor);
            EditorUtility.SetDirty(prefab);
        }

        private static ActorDatabase LoadActorDatabase()
        {
            var database = AssetDatabase.LoadAssetAtPath<ActorDatabase>(DefaultActorDatabasePath);
            if (database != null)
                return database;

            var guids = AssetDatabase.FindAssets("t:ActorDatabase");
            if (guids == null || guids.Length == 0)
                return null;

            return AssetDatabase.LoadAssetAtPath<ActorDatabase>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static ActorDefinitionSO FindDefinition(ActorDatabase database, string actorId)
        {
            IReadOnlyList<ActorDefinitionSO> all = database.All;
            for (int i = 0; i < all.Count; i++)
            {
                var def = all[i];
                if (def != null && def.actorId == actorId)
                    return def;
            }

            return null;
        }

        private static ActorDefinitionSO CreateDefinitionAsset(BuildContext ctx, string actorId)
        {
            var definition = ScriptableObject.CreateInstance<ActorDefinitionSO>();
            var dataFolder = PathConfig.GetGeneratedDataFolder(typeof(ActorDefinitionSO));
            // 고정 경로의 기존 정의는 제자리 갱신해 외부 참조의 GUID를 보존한다.
            definition = PathConfig.CreateOrUpdateAsset(
                definition,
                dataFolder,
                $"{actorId}_ActorDef",
                out string assetPath,
                out bool created,
                ctx);
            ctx.GeneratedDescs.Add(definition);
            if (created)
                ctx.GeneratedAssetPaths.Add(assetPath);
            return definition;
        }

        private static void ApplyDefinitionDefaults(
            ActorDefinitionSO definition,
            GameObject prefab,
            string actorId,
            BuildContext ctx)
        {
            definition.actorId = actorId;
            definition.characterType = CharacterActorType.None;
            definition.prefab = prefab;

            if (ctx.Config.ActorKind == BuilderActorKind.Npc)
            {
                ApplyNpcDefinitionDefaults(definition, ctx);
                return;
            }

            definition.displayName = actorId;
            definition.actorType = ActorType.Monster | ActorType.Combat;
            definition.targetLayerMask = LayerMask.GetMask("Player");
            definition.poiseData = FindFirst<PoiseSO>(ctx.GeneratedDescs)
                                   ?? ctx.Config?.Stats?.existingPoiseSo as PoiseSO;
            definition.monsterProfile = ctx.Config?.Stats?.monsterProfile;
            definition.breakGaugeData = ctx.Config?.Stats?.breakGaugeData;
            definition.monsterScaling = ctx.Config?.Stats?.monsterScaling;
            definition.abilitySet = ctx.Config?.Stats?.abilitySet;
            definition.combatDefensePolicy = ctx.Config?.Stats?.combatDefensePolicy;
            definition.combatReactionPolicy = ctx.Config?.Stats?.combatReactionPolicy;
            definition.behaviorData = FindFirst<EnemyBehaviorSO>(ctx.GeneratedDescs)
                                      ?? ctx.Config?.Stats?.existingBehaviorSo as EnemyBehaviorSO;
            definition.dropTable = ctx.Config?.Stats?.dropTable;
            definition.recruitableAs = ctx.Config?.Stats != null && ctx.Config.Stats.recruitableOnDefeat
                ? ctx.Config.Stats.recruitableAs
                : CharacterActorType.None;

            // 등급/레벨을 정의(ActorDefinitionSO)에 기록한다.
            // Stat Data Generator의 마이그레이션이 definition.grade를 읽어 등급 템플릿으로 statData를 발급한다.
            if (ctx.Config?.Stats != null)
            {
                definition.grade = ctx.Config.Cycle != null && ctx.Config.Cycle.isCycleBoss
                    ? MonsterActorGrade.Boss
                    : ctx.Config.Stats.grade;
                definition.level = Mathf.Max(1, ctx.Config.Stats.level);
                definition.combatElement = ctx.Config.Stats.combatElement;
                definition.elementAssignmentMode = ctx.Config.Stats.elementAssignmentMode;
                definition.elementalAdvantageMultiplier = Mathf.Max(1f, ctx.Config.Stats.elementalAdvantageMultiplier);
                definition.expReward = System.Math.Max(0, ctx.Config.Stats.expReward);
                definition.goldReward = Mathf.Max(0, ctx.Config.Stats.goldReward);
            }

            EnsureMonsterGrowthAndStat(definition, ctx);
        }

        private static void ApplyNpcDefinitionDefaults(ActorDefinitionSO definition, BuildContext ctx)
        {
            NpcActorSO npcData = FindFirst<NpcActorSO>(ctx.GeneratedDescs)
                                 ?? ctx.Config?.Stats?.existingNpcData;
            if (npcData == null)
                throw new BuildException("NPC ActorDefinition에 연결할 NpcActorSO가 없습니다.");

            definition.displayName = string.IsNullOrWhiteSpace(npcData.actorName)
                ? definition.actorId
                : npcData.actorName;
            definition.description = npcData.description ?? string.Empty;
            definition.actorType = ActorType.NPC | ActorType.Talkable;
            definition.targetLayerMask = 0;
            definition.npcData = npcData;
        }

        private static void EnsureMonsterGrowthAndStat(ActorDefinitionSO definition, BuildContext ctx)
        {
            if (definition == null)
                return;

            MonsterStatBakeService.Result result = MonsterStatBakeService.Bake(definition, new MonsterStatBakeService.Options
            {
                StatSavePath = MonsterStatBakeService.DefaultStatSavePath,
                CreateMissingStat = true,
                ForceRegenerate = true,
                ReplaceExistingStatAsset = false,
                UseStableStatAssetPath = true,
                LinkMissingScaling = true,
                RecordUndo = true,
                SyncGeneratedPoise = IsGeneratedDesc(ctx, definition.poiseData),
                UndoLabel = "P09 Builder: Update Monster Stat",
            });

            if (result.CreatedStat && result.Stat != null)
            {
                ctx?.GeneratedDescs.Add(result.Stat);
                if (!string.IsNullOrEmpty(result.StatPath))
                    ctx?.GeneratedAssetPaths.Add(result.StatPath);
            }
        }

        private static bool IsGeneratedDesc(BuildContext ctx, ScriptableObject asset)
            => ctx != null && asset != null && ctx.GeneratedDescs != null && ctx.GeneratedDescs.Contains(asset);

        private static string SafeName(string value)
        {
            string result = string.IsNullOrWhiteSpace(value) ? "Unknown" : value;
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
                result = result.Replace(invalid, '_');
            return result.Replace('/', '_').Replace('\\', '_').Replace(' ', '_');
        }

        private static T FindFirst<T>(List<ScriptableObject> list) where T : ScriptableObject
        {
            if (list == null) return null;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is T match)
                    return match;
            }

            return null;
        }
    }
}
