using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace Game.Editor.P09Builder
{
    public sealed class SyncActorDatabaseStep : IBuildStep
    {
        private const string DefaultActorDatabasePath = "Assets/10.Datas/Actor/DataBase/ActorDatabase.asset";
        private const string GeneratedStatPath = "Assets/10.Datas/Stat/Generated";

        public void Execute(BuildContext ctx)
        {
            if (ctx == null || ctx.Config == null || ctx.Config.ActorKind != BuilderActorKind.Enemy)
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
            // 중앙 폴더에 고정 경로로 생성 → 재빌드 시 _1,_2 중복 누적 없이 덮어쓴다.
            var assetPath = PathConfig.CreateOrReplaceAsset(definition, dataFolder, $"{actorId}_ActorDef");
            ctx.GeneratedDescs.Add(definition);
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
            definition.displayName = actorId;
            definition.actorType = ActorType.Monster | ActorType.Combat;
            definition.characterType = CharacterActorType.None;
            definition.targetLayerMask = LayerMask.GetMask("Player");
            definition.prefab = prefab;
            definition.poiseData = FindFirst<PoiseSO>(ctx.GeneratedDescs)
                                   ?? ctx.Config?.Stats?.existingPoiseSo as PoiseSO;
            definition.attackData = FindFirst<EnemyAttackDataSO>(ctx.GeneratedDescs)
                                    ?? ctx.Config?.Stats?.attackDataSo as EnemyAttackDataSO;
            definition.behaviorData = FindFirst<EnemyBehaviorSO>(ctx.GeneratedDescs)
                                      ?? ctx.Config?.Stats?.existingBehaviorSo as EnemyBehaviorSO;
            definition.recruitableAs = ctx.Config?.Stats != null && ctx.Config.Stats.recruitableOnDefeat
                ? ctx.Config.Stats.recruitableAs
                : CharacterActorType.None;

            // 등급/레벨을 정의(ActorDefinitionSO)에 기록한다.
            // Stat Data Generator의 마이그레이션이 definition.grade를 읽어 등급 템플릿으로 statData를 발급한다.
            if (ctx.Config?.Stats != null)
            {
                definition.grade = ctx.Config.Stats.grade;
                definition.level = Mathf.Max(1, ctx.Config.Stats.level);
            }

            EnsureMonsterGrowthAndStat(definition, ctx);
        }

        private static void EnsureMonsterGrowthAndStat(ActorDefinitionSO definition, BuildContext ctx)
        {
            if (definition == null)
                return;

            MonsterScalingSO scaling = definition.monsterScaling != null
                ? definition.monsterScaling
                : FindOrCreateMonsterScaling();

            if (scaling == null)
                return;

            definition.monsterScaling = scaling;

            if (definition.statData == null)
            {
                var stat = ScriptableObject.CreateInstance<ActorStatSO>();
                WriteMonsterStat(stat, definition, IsGeneratedDesc(ctx, definition.poiseData));

                // 고정 경로로 생성 → 재빌드 시 _1,_2 중복 누적 없이 덮어쓴다.
                string path = PathConfig.CreateOrReplaceAsset(stat, GeneratedStatPath, $"ActorStat_{SafeName(definition.actorId)}");
                definition.statData = stat;
                ctx?.GeneratedDescs.Add(stat);
                ctx?.GeneratedAssetPaths.Add(path);
            }
            else
            {
                Undo.RecordObject(definition.statData, "P09 Builder: Update Monster Stat");
                WriteMonsterStat(definition.statData, definition, IsGeneratedDesc(ctx, definition.poiseData));
                EditorUtility.SetDirty(definition.statData);
            }
        }

        private static void WriteMonsterStat(ActorStatSO stat, ActorDefinitionSO definition, bool syncGeneratedPoise)
        {
            if (stat == null || definition == null)
                return;

            var values = MonsterStatCalculator.Calculate(definition.monsterScaling, definition);
            foreach (KeyValuePair<StatType, float> pair in values)
                stat.EditorSet(pair.Key, pair.Value);

            if (definition.poiseData != null)
            {
                if (syncGeneratedPoise)
                {
                    Undo.RecordObject(definition.poiseData, "P09 Builder: Sync Generated Poise");
                    definition.poiseData.maxPoise = Mathf.Max(1f, stat.GetBase(StatType.MaxPoise));
                    EditorUtility.SetDirty(definition.poiseData);
                }
                else
                {
                    stat.EditorSet(StatType.MaxPoise, definition.poiseData.maxPoise);
                }

                stat.EditorSet(StatType.PoiseRecoveryRate, definition.poiseData.recoveryRate);
                stat.EditorSet(StatType.PoiseRecoveryDelay, definition.poiseData.recoveryDelay);
            }

            stat.EditorFillMissing();
        }

        private static bool IsGeneratedDesc(BuildContext ctx, ScriptableObject asset)
            => ctx != null && asset != null && ctx.GeneratedDescs != null && ctx.GeneratedDescs.Contains(asset);

        private static MonsterScalingSO FindOrCreateMonsterScaling()
        {
            var guids = AssetDatabase.FindAssets("t:MonsterScalingSO");
            if (guids != null && guids.Length > 0)
            {
                System.Array.Sort(guids, (a, b) => string.Compare(
                    AssetDatabase.GUIDToAssetPath(a),
                    AssetDatabase.GUIDToAssetPath(b),
                    System.StringComparison.Ordinal));
                return AssetDatabase.LoadAssetAtPath<MonsterScalingSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
            }

            PathConfig.EnsureFolderExists(GeneratedStatPath);
            var scaling = ScriptableObject.CreateInstance<MonsterScalingSO>();
            scaling.FillDefaults();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{GeneratedStatPath}/MonsterScaling_Default.asset");
            AssetDatabase.CreateAsset(scaling, path);
            Debug.Log($"[P09Builder] 기본 MonsterScalingSO 생성: {path}");
            return scaling;
        }

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
