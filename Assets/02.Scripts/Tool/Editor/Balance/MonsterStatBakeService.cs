#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// 몬스터 ActorDefinitionSO에서 AttributeProfileSO를 발급/갱신하는 단일 에디터 서비스.
    /// 계산 공식은 MonsterStatCalculator에만 두고, 에셋 생성/연결/동기화 정책은 여기로 모은다.
    /// </summary>
    public static class MonsterStatBakeService
    {
        public const string DefaultStatSavePath = "Assets/10.Datas/Ability/Attributes/Generated";
        public const string DefaultBreakGaugeSavePath = "Assets/10.Datas/Actor/Enemy/BreakGauge/Generated";

        public sealed class Options
        {
            public MonsterScalingSO PreferredScaling;
            public string StatSavePath = DefaultStatSavePath;
            public string BreakGaugeSavePath = DefaultBreakGaugeSavePath;
            public float DifficultyOverride;
            public bool CreateMissingStat = true;
            public bool ForceRegenerate;
            public bool ReplaceExistingStatAsset;
            public bool UseStableStatAssetPath;
            public bool LinkMissingScaling = true;
            public bool RecordUndo = true;
            public bool SyncGeneratedPoise;
            public bool GenerateMissingBreakGauge;
            public bool SyncExistingBreakGauge;
            public string UndoLabel = "Bake Monster Stat";
        }

        public readonly struct Result
        {
            public Result(
                AttributeProfileSO stat,
                string statPath,
                string breakGaugePath,
                bool linkedScaling,
                bool createdStat,
                bool updatedStat,
                bool replacedStat,
                bool createdBreakGauge,
                bool syncedBreakGauge)
            {
                Stat = stat;
                StatPath = statPath;
                BreakGaugePath = breakGaugePath;
                LinkedScaling = linkedScaling;
                CreatedStat = createdStat;
                UpdatedStat = updatedStat;
                ReplacedStat = replacedStat;
                CreatedBreakGauge = createdBreakGauge;
                SyncedBreakGauge = syncedBreakGauge;
            }

            public AttributeProfileSO Stat { get; }
            public string StatPath { get; }
            public string BreakGaugePath { get; }
            public bool LinkedScaling { get; }
            public bool CreatedStat { get; }
            public bool UpdatedStat { get; }
            public bool ReplacedStat { get; }
            public bool CreatedBreakGauge { get; }
            public bool SyncedBreakGauge { get; }
        }

        public static Result Bake(ActorDefinitionSO actor, Options options = null)
        {
            options ??= new Options();
            if (!IsMonster(actor))
                return default;

            EnsureFolder(options.StatSavePath);
            BackfillGradeLevelFromPrefab(actor, options.RecordUndo);

            bool linkedScaling = EnsureScalingLinked(actor, options);
            AttributeProfileSO stat = actor.attributeProfile;
            string statPath = null;
            bool createdStat = false;
            bool updatedStat = false;
            bool replacedStat = false;

            bool shouldCreate = stat == null && options.CreateMissingStat;
            bool shouldRegenerate = stat != null && options.ForceRegenerate;

            if (shouldCreate || (shouldRegenerate && options.ReplaceExistingStatAsset))
            {
                AttributeProfileSO previous = stat;
                stat = options.UseStableStatAssetPath
                    ? LoadStableProfileAsset(actor, options.StatSavePath)
                    : null;
                if (stat == null)
                    stat = ScriptableObject.CreateInstance<AttributeProfileSO>();
                WriteMonsterStatValues(stat, actor, options);

                statPath = AssetDatabase.GetAssetPath(stat);
                if (string.IsNullOrEmpty(statPath))
                    statPath = CreateProfileAsset(stat, actor, options.StatSavePath, options.UseStableStatAssetPath);
                else
                    EditorUtility.SetDirty(stat);
                AssignProfile(actor, stat, options.RecordUndo, options.UndoLabel);
                createdStat = previous == null;
                replacedStat = previous != null;
            }
            else if (shouldRegenerate && stat != null)
            {
                if (options.RecordUndo)
                    Undo.RecordObject(stat, options.UndoLabel);
                WriteMonsterStatValues(stat, actor, options);
                EditorUtility.SetDirty(stat);
                updatedStat = true;
            }

            if (stat != null && options.SyncGeneratedPoise)
                SyncPoiseFromStat(actor, stat, options.RecordUndo);

            string breakGaugePath = null;
            bool createdBreakGauge = false;
            bool syncedBreakGauge = false;
            if (stat != null && options.GenerateMissingBreakGauge)
            {
                breakGaugePath = GenerateMissingBreakGauge(actor, stat, options);
                createdBreakGauge = !string.IsNullOrEmpty(breakGaugePath);
            }

            if (stat != null && !createdBreakGauge && options.SyncExistingBreakGauge)
                syncedBreakGauge = SyncExistingBreakGaugeMax(actor, stat, options.RecordUndo);

            return new Result(
                stat,
                statPath,
                breakGaugePath,
                linkedScaling,
                createdStat,
                updatedStat,
                replacedStat,
                createdBreakGauge,
                syncedBreakGauge);
        }

        public static Dictionary<AttributeId, float> CalculatePlanned(
            ActorDefinitionSO actor,
            MonsterScalingSO preferredScaling = null,
            float difficultyOverride = 0f)
        {
            MonsterScalingSO scaling = actor != null && actor.monsterScaling != null
                ? actor.monsterScaling
                : preferredScaling;
            return MonsterStatCalculator.Calculate(scaling, actor, difficultyOverride);
        }

        public static MonsterScalingSO FindOrCreateScaling(string savePath = DefaultStatSavePath)
        {
            MonsterScalingSO existing = FindFirstScaling();
            if (existing != null)
                return existing;

            EnsureFolder(savePath);
            var scaling = ScriptableObject.CreateInstance<MonsterScalingSO>();
            scaling.FillDefaults();
            string path = AssetDatabase.GenerateUniqueAssetPath($"{savePath}/MonsterScaling_Default.asset");
            AssetDatabase.CreateAsset(scaling, path);
            Debug.Log($"[MonsterStatBakeService] 기본 MonsterScalingSO 생성: {path}");
            return scaling;
        }

        public static MonsterScalingSO FindFirstScaling()
        {
            string[] guids = AssetDatabase.FindAssets("t:MonsterScalingSO");
            if (guids == null || guids.Length == 0)
                return null;

            Array.Sort(guids, (a, b) => string.Compare(
                AssetDatabase.GUIDToAssetPath(a),
                AssetDatabase.GUIDToAssetPath(b),
                StringComparison.Ordinal));
            return AssetDatabase.LoadAssetAtPath<MonsterScalingSO>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        public static void WriteMonsterStatValues(AttributeProfileSO stat, ActorDefinitionSO actor, Options options = null)
        {
            if (stat == null || actor == null)
                return;

            options ??= new Options();
            MonsterScalingSO scaling = actor.monsterScaling != null
                ? actor.monsterScaling
                : options.PreferredScaling;
            Dictionary<AttributeId, float> values = MonsterStatCalculator.Calculate(
                scaling,
                actor,
                options.DifficultyOverride);

            var entries = new List<AttributeProfileEntry>(values.Count);
            foreach (KeyValuePair<AttributeId, float> pair in values)
                entries.Add(new AttributeProfileEntry(pair.Key, pair.Value));
            stat.EditorReplace(entries);
        }

        private static bool EnsureScalingLinked(ActorDefinitionSO actor, Options options)
        {
            if (!options.LinkMissingScaling || actor.monsterScaling != null)
                return false;

            MonsterScalingSO scaling = options.PreferredScaling != null
                ? options.PreferredScaling
                : FindOrCreateScaling(options.StatSavePath);
            if (scaling == null)
                return false;

            if (options.RecordUndo)
                Undo.RecordObject(actor, "Link Monster Scaling");
            var so = new SerializedObject(actor);
            so.FindProperty("monsterScaling").objectReferenceValue = scaling;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(actor);
            return true;
        }

        private static void AssignProfile(ActorDefinitionSO actor, AttributeProfileSO stat, bool recordUndo, string undoLabel)
        {
            if (recordUndo)
                Undo.RecordObject(actor, undoLabel);
            var so = new SerializedObject(actor);
            so.FindProperty("attributeProfile").objectReferenceValue = stat;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(actor);
        }

        private static AttributeProfileSO LoadStableProfileAsset(ActorDefinitionSO actor, string savePath)
        {
            string path = $"{savePath}/AttributeProfile_{SafeName(actor)}.asset";
            return AssetDatabase.LoadAssetAtPath<AttributeProfileSO>(path);
        }

        private static string CreateProfileAsset(AttributeProfileSO stat, ActorDefinitionSO actor, string savePath, bool stablePath)
        {
            string rawPath = $"{savePath}/AttributeProfile_{SafeName(actor)}.asset";
            string path = stablePath ? rawPath : AssetDatabase.GenerateUniqueAssetPath(rawPath);
            AssetDatabase.CreateAsset(stat, path);
            return path;
        }

        private static void SyncPoiseFromStat(ActorDefinitionSO actor, AttributeProfileSO stat, bool recordUndo)
        {
            if (actor == null || stat == null || actor.poiseData == null)
                return;

            if (recordUndo)
                Undo.RecordObject(actor.poiseData, "Sync Generated Poise");
            actor.poiseData.maxPoise = Mathf.Max(
                1f, Get(stat, global::UPlayGround.Data.Stat.Attributes.Vital.MaxPoise));
            actor.poiseData.recoveryRate =
                Get(stat, global::UPlayGround.Data.Stat.Attributes.Vital.PoiseRecoveryRate);
            actor.poiseData.recoveryDelay =
                Get(stat, global::UPlayGround.Data.Stat.Attributes.Vital.PoiseRecoveryDelay);
            EditorUtility.SetDirty(actor.poiseData);
        }

        private static string GenerateMissingBreakGauge(ActorDefinitionSO actor, AttributeProfileSO stat, Options options)
        {
            if (actor == null || actor.breakGaugeData != null)
                return null;

            EnsureFolder(options.BreakGaugeSavePath);
            var data = ScriptableObject.CreateInstance<MonsterBreakGaugeSO>();
            data.name = $"BreakGauge_{SafeName(actor)}";
            data.maxGauge = CalculateBreakGauge(actor, stat);
            data.gradePolicy = new MonsterBreakGradePolicy
            {
                weakGaugeMultiplier = 1f,
                normalGaugeMultiplier = 1f,
                eliteGaugeMultiplier = 1f,
                bossGaugeMultiplier = 1f,
            };

            string path = AssetDatabase.GenerateUniqueAssetPath($"{options.BreakGaugeSavePath}/{data.name}.asset");
            AssetDatabase.CreateAsset(data, path);

            if (options.RecordUndo)
                Undo.RecordObject(actor, "Generate Monster Break Gauge");
            var so = new SerializedObject(actor);
            so.FindProperty("breakGaugeData").objectReferenceValue = data;
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(actor);
            return path;
        }

        private static bool SyncExistingBreakGaugeMax(ActorDefinitionSO actor, AttributeProfileSO stat, bool recordUndo)
        {
            if (actor == null || stat == null)
                return false;
            if (actor.breakGaugeData == null || !actor.breakGaugeData.useBreakGauge)
                return false;

            float newMax = CalculateBreakGauge(actor, stat);
            if (Mathf.Approximately(actor.breakGaugeData.maxGauge, newMax))
                return false;

            if (recordUndo)
                Undo.RecordObject(actor.breakGaugeData, "Sync Break Gauge Max");
            actor.breakGaugeData.maxGauge = newMax;
            EditorUtility.SetDirty(actor.breakGaugeData);
            return true;
        }

        private static float CalculateBreakGauge(ActorDefinitionSO actor, AttributeProfileSO stat)
        {
            if (stat != null)
                return Mathf.Max(
                    1f,
                    Mathf.Round(Get(stat, global::UPlayGround.Data.Stat.Attributes.Vital.MaxPoise)));
            if (actor?.poiseData != null)
                return Mathf.Max(1f, Mathf.Round(actor.poiseData.maxPoise));
            return Mathf.Max(
                1f,
                UPlayGroundAttributeDefaults.Get(global::UPlayGround.Data.Stat.Attributes.Vital.MaxPoise));
        }

        private static void BackfillGradeLevelFromPrefab(ActorDefinitionSO actor, bool recordUndo)
        {
            if (actor == null || actor.prefab == null)
                return;

            // 런타임은 MonsterActor.SetDefinition에서 정의(grade/level)를 권위 소스로 프리팹에 주입한다.
            // 이 백필은 과거 빌드 마이그레이션용 — grade/level이 한 번도 저작되지 않아 기본값(Normal/Lv1)으로
            // 남은 정의만 프리팹 보존값으로 채운다. 저작된 정의를 프리팹의 낡은 캐시로 덮어쓰면
            // 생성기 미리보기(정의 기준)와 다른 스탯이 bake되고 등록한 Lv/Grade가 소실된다.
            if (actor.grade != MonsterActorGrade.Normal || actor.level > 1)
                return;

            var monster = actor.prefab.GetComponent<MonsterActor>();
            if (monster == null)
                return;

            int level = Mathf.Max(1, monster.Level);
            if (actor.grade == monster.Grade && actor.level == level)
                return;

            if (recordUndo)
                Undo.RecordObject(actor, "Backfill Grade/Level From Prefab");
            actor.grade = monster.Grade;
            actor.level = level;
            EditorUtility.SetDirty(actor);
        }

        private static float Get(
            AttributeProfileSO profile,
            AttributeId attributeId)
        {
            if (profile != null
                && profile.TryGetBaseValue(attributeId, out float value))
                return value;
            return UPlayGroundAttributeDefaults.Get(attributeId);
        }

        private static bool IsMonster(ActorDefinitionSO actor)
            => actor != null && (actor.actorType & ActorType.Monster) != 0;

        private static string SafeName(ActorDefinitionSO actor)
        {
            string raw = actor != null && !string.IsNullOrWhiteSpace(actor.actorId)
                ? actor.actorId
                : actor != null ? actor.name : "Unknown";
            foreach (char invalid in System.IO.Path.GetInvalidFileNameChars())
                raw = raw.Replace(invalid, '_');
            return raw.Replace('/', '_').Replace('\\', '_').Replace(' ', '_');
        }

        public static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;

            string[] parts = path.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
#endif
