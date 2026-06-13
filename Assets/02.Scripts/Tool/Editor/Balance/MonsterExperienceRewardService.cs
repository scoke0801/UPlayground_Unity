#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// 몬스터 처치 경험치를 ActorDefinitionSO.expReward에 기록하는 에디터 전용 계산 서비스.
    /// 기준 플레이어 레벨 대비 몬스터 레벨 차이와 등급을 반영한다.
    /// </summary>
    public static class MonsterExperienceRewardService
    {
        public const string DefaultLevelCurvePath = "Assets/10.Datas/Party/Growth";

        public sealed class Options
        {
            public LevelCurveSO LevelCurve;
            public int PlayerLevel = 1;
            public float SameLevelNormalRewardRatio = 0.18f;
            public float LevelGapStep = 0.12f;
            public float MinLevelGapMultiplier = 0.25f;
            public float MaxLevelGapMultiplier = 2.5f;
            public float WeakMultiplier = 0.6f;
            public float NormalMultiplier = 1f;
            public float EliteMultiplier = 2.75f;
            public float BossMultiplier = 10f;
            public long MinReward = 1;
            public bool PreserveZeroRewardForNonMonsters = true;
            public bool RecordUndo = true;
            public string UndoLabel = "Bake Monster EXP Reward";
        }

        public readonly struct Preview
        {
            public Preview(
                ActorDefinitionSO actor,
                long requiredExp,
                float gradeMultiplier,
                float levelGapMultiplier,
                long reward)
            {
                Actor = actor;
                RequiredExp = requiredExp;
                GradeMultiplier = gradeMultiplier;
                LevelGapMultiplier = levelGapMultiplier;
                Reward = reward;
            }

            public ActorDefinitionSO Actor { get; }
            public long RequiredExp { get; }
            public float GradeMultiplier { get; }
            public float LevelGapMultiplier { get; }
            public long Reward { get; }
        }

        public readonly struct ApplyResult
        {
            public ApplyResult(int scanned, int changed, int skipped)
            {
                Scanned = scanned;
                Changed = changed;
                Skipped = skipped;
            }

            public int Scanned { get; }
            public int Changed { get; }
            public int Skipped { get; }
        }

        public static Preview Calculate(ActorDefinitionSO actor, Options options)
        {
            options ??= new Options();
            int playerLevel = Mathf.Max(1, options.PlayerLevel);
            long required = GetRequiredExp(options.LevelCurve, playerLevel);

            if (!IsMonster(actor))
                return new Preview(actor, required, 0f, 0f, 0L);

            float grade = GetGradeMultiplier(actor.grade, options);
            int delta = Mathf.Max(1, actor.level) - playerLevel;
            float levelGap = Mathf.Clamp(
                1f + delta * Mathf.Max(0f, options.LevelGapStep),
                Mathf.Max(0f, options.MinLevelGapMultiplier),
                Mathf.Max(options.MinLevelGapMultiplier, options.MaxLevelGapMultiplier));

            double raw = required
                         * Math.Max(0.0, options.SameLevelNormalRewardRatio)
                         * Math.Max(0.0, grade)
                         * Math.Max(0.0, levelGap);
            long reward = (long)Math.Round(raw, MidpointRounding.AwayFromZero);
            reward = Math.Max(Math.Max(0, options.MinReward), reward);
            return new Preview(actor, required, grade, levelGap, reward);
        }

        public static bool Apply(ActorDefinitionSO actor, Options options, out Preview preview)
        {
            preview = Calculate(actor, options);
            if (!IsMonster(actor))
                return false;

            if (actor.expReward == preview.Reward)
                return false;

            if (options?.RecordUndo != false)
                Undo.RecordObject(actor, options?.UndoLabel ?? "Bake Monster EXP Reward");

            var serialized = new SerializedObject(actor);
            serialized.FindProperty("expReward").longValue = preview.Reward;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(actor);
            return true;
        }

        public static ApplyResult ApplyAll(IEnumerable<ActorDefinitionSO> actors, Options options)
        {
            int scanned = 0;
            int changed = 0;
            int skipped = 0;

            if (actors == null)
                return new ApplyResult(scanned, changed, skipped);

            foreach (ActorDefinitionSO actor in actors)
            {
                if (actor == null)
                    continue;

                scanned++;
                if (!IsMonster(actor))
                {
                    skipped++;
                    continue;
                }

                if (Apply(actor, options, out _))
                    changed++;
            }

            return new ApplyResult(scanned, changed, skipped);
        }

        public static LevelCurveSO FindOrCreateLevelCurve(string savePath = DefaultLevelCurvePath)
        {
            string defaultPath = $"{savePath}/LevelCurve_Default.asset";
            LevelCurveSO existing = AssetDatabase.LoadAssetAtPath<LevelCurveSO>(defaultPath);
            if (existing != null)
                return existing;

            EnsureFolder(savePath);
            var curve = ScriptableObject.CreateInstance<LevelCurveSO>();
            curve.baseExp = 100;
            curve.exponent = 1.5f;
            AssetDatabase.CreateAsset(curve, defaultPath);
            Debug.Log($"[MonsterExperienceRewardService] 기본 LevelCurveSO 생성: {defaultPath}");
            return curve;
        }

        public static int LinkLevelCurveToGrowthAssets(LevelCurveSO curve, bool overwriteExisting)
        {
            if (curve == null)
                return 0;

            int changed = 0;
            string[] guids = AssetDatabase.FindAssets("t:PartyMemberGrowthSO");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var growth = AssetDatabase.LoadAssetAtPath<PartyMemberGrowthSO>(path);
                if (growth == null)
                    continue;
                if (!overwriteExisting && growth.levelCurve != null)
                    continue;
                if (growth.levelCurve == curve)
                    continue;

                Undo.RecordObject(growth, "Link Level Curve");
                var serialized = new SerializedObject(growth);
                serialized.FindProperty("levelCurve").objectReferenceValue = curve;
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(growth);
                changed++;
            }

            return changed;
        }

        public static List<ActorDefinitionSO> LoadMonsterDefinitions(ActorDatabase database)
        {
            var result = new List<ActorDefinitionSO>();
            if (database != null)
            {
                IReadOnlyList<ActorDefinitionSO> actors = database.All;
                for (int i = 0; i < actors.Count; i++)
                {
                    ActorDefinitionSO actor = actors[i];
                    if (IsMonster(actor))
                        result.Add(actor);
                }
                return result;
            }

            string[] guids = AssetDatabase.FindAssets("t:ActorDefinitionSO");
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var actor = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                if (IsMonster(actor))
                    result.Add(actor);
            }

            result.Sort((a, b) => string.Compare(a.actorId, b.actorId, StringComparison.Ordinal));
            return result;
        }

        private static bool IsMonster(ActorDefinitionSO actor)
            => actor != null && (actor.actorType & ActorType.Monster) != 0;

        private static long GetRequiredExp(LevelCurveSO curve, int playerLevel)
            => curve != null ? curve.GetRequiredExp(playerLevel) : FallbackRequiredExp(playerLevel);

        private static long FallbackRequiredExp(int level)
        {
            double required = 100.0 * Math.Pow(Math.Max(1, level), 1.5);
            return (long)Math.Max(1.0, Math.Round(required, MidpointRounding.AwayFromZero));
        }

        private static float GetGradeMultiplier(MonsterActorGrade grade, Options options)
        {
            return grade switch
            {
                MonsterActorGrade.Weak => Mathf.Max(0f, options.WeakMultiplier),
                MonsterActorGrade.Elite => Mathf.Max(0f, options.EliteMultiplier),
                MonsterActorGrade.Boss => Mathf.Max(0f, options.BossMultiplier),
                _ => Mathf.Max(0f, options.NormalMultiplier),
            };
        }

        private static T FindFirst<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            if (guids == null || guids.Length == 0)
                return null;

            Array.Sort(guids, (a, b) => string.Compare(
                AssetDatabase.GUIDToAssetPath(a),
                AssetDatabase.GUIDToAssetPath(b),
                StringComparison.Ordinal));
            return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void EnsureFolder(string path)
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
