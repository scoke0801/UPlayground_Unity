using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using UPlayGround.Tool.Editor.Balance;

namespace UPlayGround.EditorTools
{
    /// <summary>정책 TTK와 기준 장비 DPS로 공용 체력 성장만 보정하고 기존 프로필을 재베이크한다.</summary>
    public static class ItemEconomyCombatCalibration
    {
        public const string ScalingPath = "Assets/10.Datas/Stat/Generated/MonsterScaling_Default.asset";
        public const string ProfileFolder = "Assets/10.Datas/Ability/Attributes/Generated";

        /// <summary>캐릭터·스케일링 이름 분기 없이 CSV 밴드 시작 레벨을 해석한다.</summary>
        public static int GetBand(int level)
        {
            var rows = File.ReadAllLines(ItemEconomyBalanceVerification.BandsCsv).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Split(',')).ToArray();
            for (int i = rows.Length - 1; i >= 0; i--)
                if (level >= int.Parse(rows[i][1], CultureInfo.InvariantCulture)) return int.Parse(rows[i][0]);
            return 1;
        }

        /// <summary>사전 계산·사본 보존 뒤 한 Undo 그룹으로 적용하며 실패 시 전부 복원한다.</summary>
        public static string Apply(string evidenceDirectory, int failAfter = -1)
        {
            Directory.CreateDirectory(evidenceDirectory);
            var source = AssetDatabase.LoadAssetAtPath<MonsterScalingSO>(ScalingPath);
            var targets = Resources.Load<CombatBalanceTargetsSO>(CombatBalanceTargetsSO.ResourceKey);
            if (source == null || targets == null) throw new InvalidOperationException("스케일링 또는 전투 목표 SO가 없습니다.");
            var actors = AssetDatabase.FindAssets("t:ActorDefinitionSO").Select(g => AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(a => a != null && (a.actorType & ActorType.Monster) != 0 && (a.EffectiveMonsterScaling == source || a.EffectiveMonsterScaling == null))
                .OrderBy(a => a.actorId, StringComparer.Ordinal).ToArray();
            if (actors.Any(a => a.attributeProfile == null)) throw new InvalidOperationException("몬스터 프로필 누락으로 재베이크를 중단합니다.");

            var plan = UnityEngine.Object.Instantiate(source);
            plan.name = source.name;
            try
            {
                var unit = UnityEngine.Object.Instantiate(source);
                float baseHealth;
                try
                {
                    unit.growthRules.RemoveAll(r => r.AttributeId == GrowthAttributeCatalog.Health);
                    int normalIndex = unit.gradeScalings.FindIndex(g => g.grade == MonsterActorGrade.Normal);
                    var normal = unit.gradeScalings[normalIndex]; normal.healthMultiplier = 1f; unit.gradeScalings[normalIndex] = normal;
                    baseHealth = MonsterStatCalculator.Calculate(unit, MonsterActorGrade.Normal, 1, 1f)[GrowthAttributeCatalog.Health];
                }
                finally { UnityEngine.Object.DestroyImmediate(unit); }

                int cap = Mathf.Max(1, plan.levelCap);
                var dps = new double[cap];
                for (int level = 1; level <= cap; level++)
                    dps[level - 1] = ItemEconomyBalanceVerification.Analyze(GetBand(level), level).expectedDps;
                var keys = new Keyframe[cap];
                for (int i = 0; i < keys.Length; i++) keys[i] = new Keyframe(cap == 1 ? 0f : (float)i / (cap - 1), (float)(dps[i] / dps[0]));
                var curve = new AnimationCurve(keys);
                for (int i = 0; i < curve.length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                }
                int ruleIndex = plan.growthRules.FindIndex(r => r.AttributeId == GrowthAttributeCatalog.Health);
                var rule = plan.growthRules[ruleIndex]; rule.formula = MonsterGrowthFormula.Curve; rule.curve = curve;
                plan.growthRules[ruleIndex] = rule;
                for (int i = 0; i < plan.gradeScalings.Count; i++)
                {
                    var grade = plan.gradeScalings[i];
                    float defense = MonsterStatCalculator.Calculate(source, grade.grade, 1)[GrowthAttributeCatalog.Defense];
                    grade.healthMultiplier = (float)(targets.GetTargetSeconds(grade.grade) * dps[0] * (1 - defense) / (baseHealth * plan.difficultyMultiplier));
                    plan.gradeScalings[i] = grade;
                }

                var touched = new HashSet<UnityEngine.Object> { source };
                foreach (var actor in actors)
                {
                    touched.Add(actor); touched.Add(actor.attributeProfile);
                    if (actor.monsterProfile != null) touched.Add(actor.monsterProfile);
                }
                foreach (var asset in touched) Backup(asset, evidenceDirectory);
                var plannedHealth = actors.ToDictionary(a => a, a => MonsterStatCalculator.Calculate(plan, a)[GrowthAttributeCatalog.Health]);
                var sourceProfiles = actors.GroupBy(a => a.attributeProfile).ToArray();
                var originalValues = sourceProfiles.ToDictionary(g => g.Key, g => g.Key.Entries.Where(e => e.AttributeId != GrowthAttributeCatalog.Health).ToDictionary(e => e.AttributeId, e => e.BaseValue));

                Undo.IncrementCurrentGroup();
                int group = Undo.GetCurrentGroup();
                Undo.SetCurrentGroupName("경제 전투 시간 보정");
                int splits = 0, linked = 0, applied = 0;
                var evidence = new List<string> { "actor,level,grade,profile,guid,maxHealth" };
                try
                {
                    foreach (var asset in touched) Undo.RecordObject(asset, "전투 시간 데이터 보정");
                    EditorUtility.CopySerialized(plan, source);
                    EditorUtility.SetDirty(source);
                    foreach (var actor in actors.Where(a => a.EffectiveMonsterScaling == null))
                    {
                        var owner = actor.monsterProfile != null ? (UnityEngine.Object)actor.monsterProfile : actor;
                        var serialized = new SerializedObject(owner);
                        serialized.FindProperty("monsterScaling").objectReferenceValue = source;
                        serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(owner); linked++;
                    }
                    foreach (var profileGroup in sourceProfiles)
                    {
                        bool first = true;
                        foreach (var values in profileGroup.GroupBy(a => plannedHealth[a].ToString("R", CultureInfo.InvariantCulture)))
                        {
                            AttributeProfileSO target = profileGroup.Key;
                            if (!first)
                            {
                                var representative = values.First();
                                string actorGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(representative));
                                string path = $"{ProfileFolder}/EconomyHealth_{actorGuid}.asset";
                                if (AssetDatabase.LoadMainAssetAtPath(path) != null || File.Exists(path)) throw new InvalidOperationException($"분리 프로필 경로가 이미 있습니다: {path}");
                                target = UnityEngine.Object.Instantiate(profileGroup.Key);
                                target.name = Path.GetFileNameWithoutExtension(path);
                                AssetDatabase.CreateAsset(target, path);
                                Undo.RegisterCreatedObjectUndo(target, "공유 체력 프로필 분리");
                                touched.Add(target); splits++;
                                foreach (var actor in values)
                                {
                                    var serialized = new SerializedObject(actor);
                                    serialized.FindProperty("attributeProfile").objectReferenceValue = target;
                                    serialized.ApplyModifiedProperties(); EditorUtility.SetDirty(actor);
                                }
                            }
                            first = false;
                            var item = values.First();
                            if (!MonsterStatBakeService.BakeHealthOnly(item)) throw new InvalidOperationException($"체력 재베이크 실패: {item.actorId}");
                            foreach (var actor in values)
                            {
                                if (!target.TryGetBaseValue(GrowthAttributeCatalog.Health, out float hp) || !Mathf.Approximately(hp, plannedHealth[actor])) throw new InvalidOperationException("공유 프로필 체력 충돌");
                                foreach (var previous in originalValues[profileGroup.Key])
                                    if (!target.TryGetBaseValue(previous.Key, out float current) || current != previous.Value) throw new InvalidOperationException("체력 외 스탯이 변경되었습니다.");
                                string path = AssetDatabase.GetAssetPath(target);
                                evidence.Add(FormattableString.Invariant($"{actor.actorId},{actor.EffectiveLevel},{actor.EffectiveGrade},{path},{AssetDatabase.AssetPathToGUID(path)},{hp}"));
                            }
                            if (++applied == failAfter) throw new InvalidOperationException("보정 롤백 검증을 위한 중간 실패");
                        }
                    }
                    Undo.FlushUndoRecordObjects();
                    foreach (var asset in touched) if (asset != null) AssetDatabase.SaveAssetIfDirty(asset);
                    Undo.CollapseUndoOperations(group);
                }
                catch
                {
                    Undo.FlushUndoRecordObjects();
                    Undo.RevertAllDownToGroup(group);
                    foreach (var asset in touched) if (asset != null && AssetDatabase.Contains(asset)) AssetDatabase.SaveAssetIfDirty(asset);
                    throw;
                }
                File.WriteAllLines(Path.Combine(evidenceDirectory, "baked-health.csv"), evidence);
                File.WriteAllLines(Path.Combine(evidenceDirectory, "reference-dps.csv"), new[] { "level,band,expectedDps" }.Concat(dps.Select((value, i) => FormattableString.Invariant($"{i + 1},{GetBand(i + 1)},{value}"))));
                return $"actors={actors.Length}, profiles={sourceProfiles.Length}, splits={splits}, linked={linked}";
            }
            finally { UnityEngine.Object.DestroyImmediate(plan); }
        }

        private static void Backup(UnityEngine.Object asset, string directory)
        {
            string path = AssetDatabase.GetAssetPath(asset);
            if (string.IsNullOrEmpty(path)) return;
            string target = Path.Combine("scratchpad/item-economy/acceptance/before-assets", path);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (!File.Exists(target)) File.Copy(path, target);
            if (File.Exists(path + ".meta") && !File.Exists(target + ".meta")) File.Copy(path + ".meta", target + ".meta");
            string json = target + ".editor.json";
            if (!File.Exists(json)) File.WriteAllText(json, EditorJsonUtility.ToJson(asset));
        }
    }
}
