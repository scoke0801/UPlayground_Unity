using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Codex;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Editor.Codex
{
    /// <summary>ActorDatabase의 몬스터 정의를 기준으로 도감 초안 에셋을 생성/검증한다.</summary>
    public static class MonsterCodexDatabaseBuilder
    {
        private const string Root = "Assets/10.Datas/Codex/Monster";
        private const string DatabasePath = Root + "/MonsterCodexDatabase.asset";

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/도감/몬스터 도감 데이터 생성 또는 갱신")]
        public static void Build()
        {
            ActorDatabase actorDatabase = FindFirst<ActorDatabase>();
            if (actorDatabase == null)
            {
                Debug.LogError("[MonsterCodexBuilder] ActorDatabase를 찾지 못했습니다.");
                return;
            }

            EnsureFolder(Root);
            MonsterCodexDatabaseSO database =
                AssetDatabase.LoadAssetAtPath<MonsterCodexDatabaseSO>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<MonsterCodexDatabaseSO>();
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            int created = 0;
            foreach (ActorDefinitionSO definition in actorDatabase.All)
            {
                if (definition == null ||
                    (definition.actorType & ActorType.Monster) == 0 ||
                    string.IsNullOrWhiteSpace(definition.actorId))
                {
                    continue;
                }

                string safeName = MakeSafeFileName(definition.actorId);
                string path = $"{Root}/MonsterCodexEntry_{safeName}.asset";
                MonsterCodexEntrySO entry =
                    AssetDatabase.LoadAssetAtPath<MonsterCodexEntrySO>(path);
                if (entry == null)
                {
                    entry = ScriptableObject.CreateInstance<MonsterCodexEntrySO>();
                    entry.actorId = definition.actorId;
                    entry.fullRecordKillCount = 10;
                    entry.bonus = new MonsterCodexBonus
                    {
                        maxExpBonus = 0.2f,
                        maxDamageDealtBonus = 0.1f,
                        maxDamageTakenReduce = 0.1f,
                    };
                    AssetDatabase.CreateAsset(entry, path);
                    created++;
                }

                database.AddEntry(entry);
            }

            database.InvalidateLookup();
            EditorUtility.SetDirty(database);
            RegisterAddressable(database);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = database;
            Debug.Log($"[MonsterCodexBuilder] 생성 {created}개, 전체 {database.Entries.Count}개");
        }

        [UPlayGround.EditorTools.UPlaygroundTool("UPlayGround/도감/몬스터 도감 데이터 검증")]
        public static void Validate()
        {
            ActorDatabase actors = FindFirst<ActorDatabase>();
            MonsterCodexDatabaseSO codex = FindFirst<MonsterCodexDatabaseSO>();
            if (actors == null || codex == null)
            {
                Debug.LogError("[MonsterCodexValidator] ActorDatabase 또는 MonsterCodexDatabase가 없습니다.");
                return;
            }

            var monsterIds = new HashSet<string>();
            foreach (ActorDefinitionSO definition in actors.All)
            {
                if (definition != null && (definition.actorType & ActorType.Monster) != 0)
                    monsterIds.Add(definition.actorId);
            }

            int errors = 0;
            var seen = new HashSet<string>();
            foreach (MonsterCodexEntrySO entry in codex.Entries)
            {
                if (entry == null || !seen.Add(entry.actorId))
                {
                    Debug.LogError("[MonsterCodexValidator] null 또는 중복 항목이 있습니다.", codex);
                    errors++;
                    continue;
                }

                if (!monsterIds.Remove(entry.actorId))
                {
                    Debug.LogWarning($"[MonsterCodexValidator] ActorDatabase 몬스터와 불일치: {entry.actorId}", entry);
                    errors++;
                }
                if (entry.fullRecordKillCount <= 0)
                {
                    Debug.LogError($"[MonsterCodexValidator] 처치 목표가 0 이하입니다: {entry.actorId}", entry);
                    errors++;
                }
                if (entry.bonus.maxExpBonus < 0f ||
                    entry.bonus.maxDamageDealtBonus < 0f ||
                    entry.bonus.maxDamageTakenReduce < 0f)
                {
                    Debug.LogError($"[MonsterCodexValidator] 음수 보정이 있습니다: {entry.actorId}", entry);
                    errors++;
                }
            }

            foreach (string missing in monsterIds)
            {
                Debug.LogError($"[MonsterCodexValidator] 도감 누락 몬스터: {missing}", actors);
                errors++;
            }

            Debug.Log($"[MonsterCodexValidator] 검증 완료: 오류/경고 {errors}개");
        }

        private static void RegisterAddressable(MonsterCodexDatabaseSO database)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogWarning("[MonsterCodexBuilder] Addressables Settings가 없어 주소 등록을 건너뜁니다.");
                return;
            }

            string guid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(database));
            AddressableAssetGroup group = settings.DefaultGroup;
            AddressableAssetEntry addressable = settings.CreateOrMoveEntry(guid, group);
            addressable.address = "MonsterCodexDatabase";
            EditorUtility.SetDirty(settings);
        }

        private static T FindFirst<T>() where T : Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length == 0
                ? null
                : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            string[] parts = path.Split('/');
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string MakeSafeFileName(string value)
        {
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value;
        }
    }
}
