using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.EditorTools;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>기존 자산과의 충돌을 거부하고 신규 NPC 프리팹만 나누어 빌드한다.</summary>
    public static class NpcAppearancePrefabBuilder
    {
        private const string DatabasePath = PathConfig.ActorDefinitionRoot + "/ActorDatabase.asset";

        /// <summary>아직 프리팹이 없는 다음 세트·성별의 최대 12개를 신규 빌드한다.</summary>
        [UPlaygroundTool("p09.npc-appearance.build-next")]
        public static void BuildNextSet()
        {
            var source = AssetDatabase.LoadAssetAtPath<NpcAppearanceSetSO>(NpcAppearanceGenerator.RulesPath);
            var configs = NpcAppearanceGenerator.CreateConfigs(source);
            int start = configs.FindIndex(c => !AssetDatabase.IsValidFolder(GetPrefabFolder(c)));
            if (start < 0) return;
            BuildRange(start, Math.Min(12 - start % 12, configs.Count - start));
        }

        /// <summary>별도 batchmode에서 실패 복구를 먼저 검사한 뒤 288개를 신규 빌드한다.</summary>
        [UPlaygroundTool("p09.npc-appearance.build-all-batch")]
        public static void BuildAllBatch()
        {
            if (!Application.isBatchMode)
                throw new InvalidOperationException("전체 빌드는 별도 batchmode에서 실행하세요.");
            Directory.CreateDirectory("Logs/NpcAppearances");
            VerifyBatchRollback();
            for (int start = 0; start < 288; start += 12)
            {
                BuildRange(start, 12);
                File.AppendAllText("Logs/NpcAppearances/PrefabBuild-progress.txt", $"{start + 12}/288 빌드 성공\n");
                // 이 별도 프로세스에는 사용자 Undo가 없으며 파괴된 임시 계층의 기록을 보관할 필요가 없다.
                Undo.ClearAll();
            }
            VerifyBuiltAssets();
        }

        private static void VerifyBuiltAssets()
        {
            var source = AssetDatabase.LoadAssetAtPath<NpcAppearanceSetSO>(NpcAppearanceGenerator.RulesPath);
            int count = 0;
            foreach (var config in NpcAppearanceGenerator.CreateConfigs(source))
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(GetPrefabFolder(config) + "/" + config.ManualName + ".prefab");
                var definition = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(GetDefinitionPath(config));
                var npcData = AssetDatabase.LoadAssetAtPath<NpcActorSO>(GetNpcDataPath(config));
                if (prefab == null || prefab.GetComponent<NpcActor>() == null || definition == null || npcData == null
                    || definition.prefab != prefab || definition.npcData != npcData)
                    throw new InvalidOperationException(config.ManualName + ": 프리팹·정의·NPC 데이터 연결 누락");
                foreach (var transform in prefab.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) != 0)
                        throw new InvalidOperationException(config.ManualName + ": Missing Script");
                count++;
            }
            File.WriteAllText("Logs/NpcAppearances/PrefabValidation.txt",
                $"프리팹 {count}개, NPC 데이터·ActorDefinition 연결 {count}개 통과, Missing Script 0\n시각 검수·Editor Play Mode는 미수행\n");
        }

        private static void VerifyBatchRollback()
        {
            var database = AssetDatabase.LoadAssetAtPath<ActorDatabase>(DatabasePath);
            string before = EditorJsonUtility.ToJson(database);
            bool hasInjectedFailure = false;
            try
            {
                BuildRange(0, 2, count =>
                {
                    hasInjectedFailure = true;
                    throw new ApplicationException("프리팹 배치 실패 주입");
                });
            }
            catch (ApplicationException) { }
            if (!hasInjectedFailure || before != EditorJsonUtility.ToJson(database))
                throw new InvalidOperationException("프리팹 배치 DB 복구 검증 실패");
            var source = AssetDatabase.LoadAssetAtPath<NpcAppearanceSetSO>(NpcAppearanceGenerator.RulesPath);
            var configs = NpcAppearanceGenerator.CreateConfigs(source).Take(2).ToArray();
            ValidateNewOutputs(configs, database);
            File.WriteAllText("Logs/NpcAppearances/PrefabRollback.txt",
                "첫 프리팹 빌드 후 실패 주입 통과: DB 내용 동일, 신규 프리팹/NpcData/ActorDefinition 잔존 0\n");
            if (Application.isBatchMode) Undo.ClearAll();
        }

        /// <summary>생성 순서의 지정 범위를 기존 P09 파이프라인으로 신규 빌드한다.</summary>
        public static IReadOnlyList<string> BuildRange(int start, int count)
            => BuildRange(start, count, null);

        internal static IReadOnlyList<string> BuildRange(int start, int count, Action<int> afterBuild)
        {
            var source = AssetDatabase.LoadAssetAtPath<NpcAppearanceSetSO>(NpcAppearanceGenerator.RulesPath);
            var configs = NpcAppearanceGenerator.CreateConfigs(source);
            if (start < 0 || count <= 0 || start > configs.Count - count)
                throw new ArgumentOutOfRangeException(nameof(start), "288개 생성 범위 안에서 지정하세요.");
            var selected = configs.Skip(start).Take(count).ToArray();
            var database = AssetDatabase.LoadAssetAtPath<ActorDatabase>(DatabasePath);
            ValidateNewOutputs(selected, database);
            var backup = new BuildContext(null);
            backup.StageAssetForUpdate(database);
            var paths = new List<string>();
            try
            {
                var pipeline = new PrefabBuildPipeline();
                foreach (var config in selected)
                {
                    var result = pipeline.Build(config);
                    if (!result.Success) throw new InvalidOperationException(config.ManualName + ": " + result.ErrorMessage);
                    paths.Add(result.PrefabPath);
                    afterBuild?.Invoke(paths.Count);
                }
                return paths;
            }
            catch
            {
                // 완료된 파이프라인의 임시 계층은 이미 파괴되어 구조 Undo를 되감으면 Unity가 크래시한다.
                // 배치는 신규 경로만 허용하므로 DB 스냅샷과 생성 경로 삭제로 복구한다.
                backup.RestoreStagedAssetBackups();
                database.InvalidateLookup();
                AssetDatabase.SaveAssetIfDirty(database);
                // 사전 검사에서 비어 있음을 확인한 배치 경로만 제거한다.
                foreach (var config in selected)
                {
                    AssetDatabase.DeleteAsset(GetPrefabFolder(config));
                    AssetDatabase.DeleteAsset(GetNpcDataPath(config));
                    AssetDatabase.DeleteAsset(GetDefinitionPath(config));
                }
                throw;
            }
            finally { backup.DiscardStagedAssetBackups(); }
        }

        private static void ValidateNewOutputs(CharacterBuildConfig[] configs, ActorDatabase database)
        {
            var errors = new List<string>();
            if (database == null || EditorUtility.IsDirty(database))
                throw new InvalidOperationException("ActorDatabase가 없거나 저장되지 않은 변경이 있습니다.");
            foreach (var config in configs)
            {
                var preset = AssetDatabase.LoadAssetAtPath<BuildPreset>(NpcAppearanceGenerator.PresetFolder + "/" + config.ManualName + "_BuildPreset.asset");
                if (preset == null || EditorUtility.IsDirty(preset) || JsonUtility.ToJson(preset.config) != JsonUtility.ToJson(config))
                    errors.Add(config.ManualName + ": 저장된 프리셋과 허용 목록이 일치해야 합니다.");
                foreach (string path in new[] { GetPrefabFolder(config), GetNpcDataPath(config), GetDefinitionPath(config) })
                    if (Directory.Exists(path) || File.Exists(path) || File.Exists(path + ".meta") || AssetDatabase.LoadMainAssetAtPath(path) != null)
                        errors.Add(path + ": 기존 자산을 덮어쓸 수 없습니다.");
                if (database.All.Any(d => d != null && d.actorId == config.ManualName))
                    errors.Add(config.ManualName + ": 이미 등록된 Actor ID입니다.");
            }
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));
        }

        private static string GetPrefabFolder(CharacterBuildConfig config)
            => PathConfig.GetPrefabFolder(config.SaveBaseFolder, "Npc", config.ManualName);
        private static string GetNpcDataPath(CharacterBuildConfig config)
            => PathConfig.NpcDataRoot + "/" + config.ManualName + "_NpcData.asset";
        private static string GetDefinitionPath(CharacterBuildConfig config)
            => PathConfig.ActorDefinitionRoot + "/" + config.ManualName + "_ActorDef.asset";
    }
}
