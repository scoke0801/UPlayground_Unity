using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Cycle;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>
    /// 완성된 Actor ID를 Cycle 보스 풀과 BossAssist DB에 원자적으로 연결한다.
    /// 런타임 핸들은 CycleWorldSpawnService가 생성하므로 프리팹에는 붙이지 않는다.
    /// </summary>
    public sealed class SyncCycleDataStep : IBuildStep
    {
        public void Execute(BuildContext ctx)
        {
            if (ctx?.Config?.ActorKind != BuilderActorKind.Enemy)
                return;

            CycleBuildSettings settings = ctx.Config.Cycle;
            if (settings == null || !settings.isCycleBoss)
                return;

            string actorId = ctx.PrefabName;
            if (string.IsNullOrWhiteSpace(actorId))
                throw new BuildException("Cycle 데이터에 등록할 Actor ID가 비어있습니다.");

            RegisterWorldPools(settings, actorId);
            if (settings.createOrUpdateBossAssist)
                CreateOrUpdateAssist(ctx, settings, actorId);

            ctx.Logs.Add($"Cycle 데이터 동기화: {actorId}");
        }

        private static void RegisterWorldPools(CycleBuildSettings settings, string actorId)
        {
            CycleWorldConfigSO world = settings.worldConfig;
            if (world == null)
                throw new BuildException("CycleWorldConfigSO가 지정되지 않았습니다.");

            Undo.RecordObject(world, "P09 Builder: Sync Cycle Boss Pool");
            world.outerBossActorIds ??= new List<string>();
            world.centralBossActorIds ??= new List<string>();
            SetMembership(world.outerBossActorIds, actorId, settings.registerAsOuterBoss);
            SetMembership(world.centralBossActorIds, actorId, settings.registerAsCentralBoss);
            EditorUtility.SetDirty(world);
        }

        private static void CreateOrUpdateAssist(BuildContext ctx, CycleBuildSettings settings, string actorId)
        {
            BossAssistDatabaseSO database = settings.assistDatabase;
            if (database == null)
                throw new BuildException("BossAssistDatabaseSO가 지정되지 않았습니다.");

            string assistId = string.IsNullOrWhiteSpace(settings.assistId)
                ? $"{actorId}_Assist"
                : settings.assistId.Trim();

            BossAssistDefinitionSO definition = database.FindByBossActorId(actorId);
            BossAssistDefinitionSO sameId = database.FindByAssistId(assistId);
            if (definition == null && sameId != null &&
                !string.Equals(sameId.sourceBossActorId, actorId, StringComparison.Ordinal))
                throw new BuildException($"Assist ID가 다른 보스에서 이미 사용 중입니다: {assistId} ({sameId.sourceBossActorId})");
            definition ??= sameId;
            if (definition == null)
            {
                definition = ScriptableObject.CreateInstance<BossAssistDefinitionSO>();
                string folder = PathConfig.GetGeneratedDataFolder(typeof(BossAssistDefinitionSO));
                string path = PathConfig.CreateOrReplaceAsset(definition, folder, $"{actorId}_BossAssist");
                ctx.GeneratedDescs.Add(definition);
                ctx.GeneratedAssetPaths.Add(path);
            }
            else
            {
                Undo.RecordObject(definition, "P09 Builder: Update Boss Assist");
            }

            definition.assistId = assistId;
            definition.sourceBossActorId = actorId;
            definition.role = settings.role;
            definition.icon = settings.icon;
            definition.assistPrefab = settings.assistPrefab;
            definition.motionSet = settings.motionSet;
            definition.cooldownSeconds = Mathf.Max(1f, settings.cooldownSeconds);
            definition.maxExecutionSeconds = Mathf.Max(0.1f, settings.maxExecutionSeconds);
            definition.placementPolicy = settings.placementPolicy;
            definition.placementOffset = settings.placementOffset;
            definition.requiresTarget = settings.requiresTarget;
            definition.recruitableFromCentralBoss = settings.recruitableFromCentralBoss;
            definition.healAmount = Mathf.Max(0f, settings.healAmount);
            EditorUtility.SetDirty(definition);

            Undo.RecordObject(database, "P09 Builder: Register Boss Assist");
            database.definitions ??= new List<BossAssistDefinitionSO>();
            database.definitions.RemoveAll(value => value == null);
            if (!database.definitions.Contains(definition))
                database.definitions.Add(definition);
            EditorUtility.SetDirty(database);
        }

        private static void SetMembership(List<string> values, string actorId, bool included)
        {
            values.RemoveAll(value => string.Equals(value, actorId, StringComparison.Ordinal));
            if (included)
                values.Add(actorId);
        }
    }
}
