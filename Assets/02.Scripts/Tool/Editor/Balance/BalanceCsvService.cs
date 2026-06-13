#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Balance
{
    /// <summary>
    /// 밸런스 수치의 CSV 왕복 편집 서비스.
    /// 내보내기 → Excel/시트에서 일괄 수정 → 가져오기로 다수 에셋에 적용한다.
    /// 가져오기는 식별 컬럼(actorId / assetPath+index)이 일치하는 행만 적용하고,
    /// animKey 불일치 등 구조가 달라진 행은 건너뛰며 리포트에 남긴다 (clobber 방지).
    /// </summary>
    public static class BalanceCsvService
    {
        private const string StatColumnPrefix = "stat:";

        #region Monster Stats CSV

        public static void ExportMonsterStats(string path, List<string> report)
        {
            StatType[] statTypes = (StatType[])Enum.GetValues(typeof(StatType));
            var sb = new StringBuilder();

            sb.Append("actorId,definitionPath,grade,level");
            foreach (StatType type in statTypes)
                sb.Append(',').Append(StatColumnPrefix).Append(type);
            sb.AppendLine();

            int count = 0;
            foreach (ActorDefinitionSO def in LoadAll<ActorDefinitionSO>())
            {
                if ((def.actorType & ActorType.Monster) == 0 || def.statData == null)
                    continue;

                sb.Append(Escape(def.actorId)).Append(',')
                  .Append(Escape(AssetDatabase.GetAssetPath(def))).Append(',')
                  .Append(def.grade).Append(',')
                  .Append(def.level);

                foreach (StatType type in statTypes)
                {
                    sb.Append(',');
                    if (def.statData.TryGetExplicit(type, out float value))
                        sb.Append(value.ToString("0.####", CultureInfo.InvariantCulture));
                    // 명시되지 않은 스탯은 빈 칸 — 가져오기에서도 빈 칸은 건드리지 않는다.
                }
                sb.AppendLine();
                count++;
            }

            WriteCsv(path, sb.ToString());
            report.Add($"몬스터 스탯 {count}행 내보내기 완료: {path}");
        }

        public static void ImportMonsterStats(string path, List<string> report)
        {
            List<string[]> rows = ReadCsv(path);
            if (rows.Count < 2)
            {
                report.Add("오류: CSV에 데이터 행이 없습니다.");
                return;
            }

            string[] header = rows[0];
            int actorIdCol = Array.IndexOf(header, "actorId");
            int levelCol = Array.IndexOf(header, "level");
            if (actorIdCol < 0)
            {
                report.Add("오류: actorId 컬럼이 없습니다.");
                return;
            }

            // stat:* 컬럼 매핑
            var statColumns = new List<(int column, StatType type)>();
            for (int c = 0; c < header.Length; c++)
            {
                if (!header[c].StartsWith(StatColumnPrefix, StringComparison.Ordinal))
                    continue;
                if (Enum.TryParse(header[c].Substring(StatColumnPrefix.Length), out StatType type))
                    statColumns.Add((c, type));
                else
                    report.Add($"경고: 알 수 없는 스탯 컬럼 '{header[c]}' — 무시합니다.");
            }

            Dictionary<string, ActorDefinitionSO> actorMap = BuildActorMap();
            var statOwners = new Dictionary<ActorStatSO, string>(); // 공유 statData 이중 적용 감지

            int applied = 0;
            int skipped = 0;
            for (int r = 1; r < rows.Count; r++)
            {
                string[] row = rows[r];
                if (row.Length <= actorIdCol || string.IsNullOrWhiteSpace(row[actorIdCol]))
                    continue;

                string actorId = row[actorIdCol];
                if (!actorMap.TryGetValue(actorId, out ActorDefinitionSO def) || def.statData == null)
                {
                    report.Add($"건너뜀: actorId '{actorId}' — 일치하는 ActorDefinitionSO/statData 없음");
                    skipped++;
                    continue;
                }

                if (statOwners.TryGetValue(def.statData, out string firstOwner) && firstOwner != actorId)
                {
                    report.Add($"경고: '{actorId}'의 statData가 '{firstOwner}'와 공유됨 — 이 행은 건너뜁니다.");
                    skipped++;
                    continue;
                }
                statOwners[def.statData] = actorId;

                bool changed = false;

                if (levelCol >= 0 && levelCol < row.Length && TryParseFloat(row[levelCol], out float levelValue))
                {
                    int level = Mathf.Max(1, Mathf.RoundToInt(levelValue));
                    if (level != def.level)
                    {
                        Undo.RecordObject(def, "Balance CSV Import");
                        def.level = level;
                        EditorUtility.SetDirty(def);
                        changed = true;
                    }
                }

                bool statDirty = false;
                foreach ((int column, StatType type) in statColumns)
                {
                    if (column >= row.Length || string.IsNullOrWhiteSpace(row[column]))
                        continue;
                    if (!TryParseFloat(row[column], out float value))
                    {
                        report.Add($"경고: {actorId}.{type} 값 '{row[column]}' 파싱 실패 — 무시");
                        continue;
                    }

                    if (def.statData.TryGetExplicit(type, out float current) && Mathf.Abs(current - value) <= 0.0001f)
                        continue;

                    if (!statDirty)
                    {
                        Undo.RecordObject(def.statData, "Balance CSV Import");
                        statDirty = true;
                    }
                    def.statData.EditorSet(type, value);
                    changed = true;
                }

                if (statDirty)
                    EditorUtility.SetDirty(def.statData);
                if (changed)
                    applied++;
            }

            AssetDatabase.SaveAssets();
            report.Add($"몬스터 스탯 가져오기 완료 — 적용 {applied}행 / 건너뜀 {skipped}행");
        }

        #endregion

        #region Enemy Skills CSV

        public static void ExportEnemySkills(string path, List<string> report)
        {
            var sb = new StringBuilder();
            sb.AppendLine("attackDataPath,skillIndex,phaseIndex,animKey,category,selectionWeight,cooldown,minRange,maxRange,requiredLevel,damage,poiseDamage,breakDamage");

            int rowCount = 0;
            foreach (EnemyAttackDataSO asset in LoadAll<EnemyAttackDataSO>())
            {
                if (asset.skills == null)
                    continue;

                string assetPath = AssetDatabase.GetAssetPath(asset);
                for (int s = 0; s < asset.skills.Count; s++)
                {
                    EnemyAttackInfo skill = asset.skills[s];
                    if (skill?.baseInfo?.hitPhases == null)
                        continue;

                    for (int p = 0; p < skill.baseInfo.hitPhases.Count; p++)
                    {
                        HitPhaseData phase = skill.baseInfo.hitPhases[p];
                        if (phase == null)
                            continue;

                        sb.Append(Escape(assetPath)).Append(',')
                          .Append(s).Append(',')
                          .Append(p).Append(',')
                          .Append(skill.baseInfo.animKey).Append(',')
                          .Append(skill.attackCategory).Append(',');

                        // 스킬 단위 필드는 phase 0 행에만 기록 (가져오기도 phase 0 행만 반영)
                        if (p == 0)
                        {
                            sb.Append(F(skill.selectionWeight)).Append(',')
                              .Append(F(skill.cooldown)).Append(',')
                              .Append(F(skill.minRange)).Append(',')
                              .Append(F(skill.maxRange)).Append(',')
                              .Append(skill.requiredLevel);
                        }
                        else
                        {
                            sb.Append(",,,,");
                        }

                        sb.Append(',').Append(F(phase.damage))
                          .Append(',').Append(F(phase.poiseDamage))
                          .Append(',').Append(F(phase.breakDamage));
                        sb.AppendLine();
                        rowCount++;
                    }
                }
            }

            WriteCsv(path, sb.ToString());
            report.Add($"적 스킬 {rowCount}행 내보내기 완료: {path}");
        }

        public static void ImportEnemySkills(string path, List<string> report)
        {
            List<string[]> rows = ReadCsv(path);
            if (rows.Count < 2)
            {
                report.Add("오류: CSV에 데이터 행이 없습니다.");
                return;
            }

            string[] header = rows[0];
            int pathCol = Array.IndexOf(header, "attackDataPath");
            int skillCol = Array.IndexOf(header, "skillIndex");
            int phaseCol = Array.IndexOf(header, "phaseIndex");
            int animKeyCol = Array.IndexOf(header, "animKey");
            int weightCol = Array.IndexOf(header, "selectionWeight");
            int cooldownCol = Array.IndexOf(header, "cooldown");
            int minRangeCol = Array.IndexOf(header, "minRange");
            int maxRangeCol = Array.IndexOf(header, "maxRange");
            int requiredLevelCol = Array.IndexOf(header, "requiredLevel");
            int damageCol = Array.IndexOf(header, "damage");
            int poiseCol = Array.IndexOf(header, "poiseDamage");
            int breakCol = Array.IndexOf(header, "breakDamage");

            if (pathCol < 0 || skillCol < 0 || phaseCol < 0)
            {
                report.Add("오류: attackDataPath/skillIndex/phaseIndex 컬럼이 필요합니다.");
                return;
            }

            var assetCache = new Dictionary<string, EnemyAttackDataSO>();
            var dirtyAssets = new HashSet<EnemyAttackDataSO>();
            int applied = 0;
            int skipped = 0;

            int requiredCols = Mathf.Max(pathCol, Mathf.Max(skillCol, phaseCol));
            for (int r = 1; r < rows.Count; r++)
            {
                string[] row = rows[r];
                if (row.Length <= requiredCols || string.IsNullOrWhiteSpace(row[pathCol]))
                    continue;

                string assetPath = row[pathCol];
                if (!assetCache.TryGetValue(assetPath, out EnemyAttackDataSO asset))
                {
                    asset = AssetDatabase.LoadAssetAtPath<EnemyAttackDataSO>(assetPath);
                    assetCache[assetPath] = asset;
                }

                if (asset == null)
                {
                    report.Add($"건너뜀: 에셋 없음 — {assetPath}");
                    skipped++;
                    continue;
                }

                if (!int.TryParse(row[skillCol], out int skillIndex) || !int.TryParse(row[phaseCol], out int phaseIndex)
                    || asset.skills == null || skillIndex < 0 || skillIndex >= asset.skills.Count)
                {
                    report.Add($"건너뜀: {Path.GetFileName(assetPath)} 행 {r + 1} — 스킬 인덱스 불일치");
                    skipped++;
                    continue;
                }

                EnemyAttackInfo skill = asset.skills[skillIndex];
                if (skill?.baseInfo?.hitPhases == null || phaseIndex < 0 || phaseIndex >= skill.baseInfo.hitPhases.Count)
                {
                    report.Add($"건너뜀: {Path.GetFileName(assetPath)} skills[{skillIndex}] phase {phaseIndex} — 구조 불일치");
                    skipped++;
                    continue;
                }

                // animKey 검증 — 내보내기 이후 스킬 순서가 바뀌었으면 잘못된 대상에 쓰지 않는다.
                if (animKeyCol >= 0 && animKeyCol < row.Length && !string.IsNullOrWhiteSpace(row[animKeyCol])
                    && row[animKeyCol] != skill.baseInfo.animKey.ToString())
                {
                    report.Add($"건너뜀: {Path.GetFileName(assetPath)} skills[{skillIndex}] — animKey 불일치 (CSV {row[animKeyCol]} vs 에셋 {skill.baseInfo.animKey})");
                    skipped++;
                    continue;
                }

                bool changed = false;
                void EnsureRecorded()
                {
                    if (dirtyAssets.Add(asset))
                        Undo.RecordObject(asset, "Balance CSV Import");
                }

                if (phaseIndex == 0)
                {
                    changed |= ApplyFloat(row, weightCol, ref skill.selectionWeight, EnsureRecorded);
                    changed |= ApplyFloat(row, cooldownCol, ref skill.cooldown, EnsureRecorded);
                    changed |= ApplyFloat(row, minRangeCol, ref skill.minRange, EnsureRecorded);
                    changed |= ApplyFloat(row, maxRangeCol, ref skill.maxRange, EnsureRecorded);
                    changed |= ApplyInt(row, requiredLevelCol, ref skill.requiredLevel, EnsureRecorded);
                }

                HitPhaseData phase = skill.baseInfo.hitPhases[phaseIndex];
                changed |= ApplyFloat(row, damageCol, ref phase.damage, EnsureRecorded);
                changed |= ApplyFloat(row, poiseCol, ref phase.poiseDamage, EnsureRecorded);
                changed |= ApplyFloat(row, breakCol, ref phase.breakDamage, EnsureRecorded);

                if (changed)
                    applied++;
            }

            foreach (EnemyAttackDataSO asset in dirtyAssets)
                EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            report.Add($"적 스킬 가져오기 완료 — 적용 {applied}행 / 건너뜀 {skipped}행 / 변경 에셋 {dirtyAssets.Count}개");
        }

        private static bool ApplyFloat(string[] row, int column, ref float target, Action ensureRecorded)
        {
            if (column < 0 || column >= row.Length || string.IsNullOrWhiteSpace(row[column]))
                return false;
            if (!TryParseFloat(row[column], out float value) || Mathf.Abs(value - target) <= 0.0001f)
                return false;

            ensureRecorded();
            target = value;
            return true;
        }

        private static bool ApplyInt(string[] row, int column, ref int target, Action ensureRecorded)
        {
            if (column < 0 || column >= row.Length || string.IsNullOrWhiteSpace(row[column]))
                return false;
            if (!TryParseFloat(row[column], out float parsed))
                return false;

            int value = Mathf.RoundToInt(parsed);
            if (value == target)
                return false;

            ensureRecorded();
            target = value;
            return true;
        }

        #endregion

        #region CSV IO

        private static string F(float value) => value.ToString("0.####", CultureInfo.InvariantCulture);

        private static bool TryParseFloat(string text, out float value)
            => float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);

        private static void WriteCsv(string path, string content)
        {
            // 한글 컬럼/값이 Excel에서 깨지지 않도록 UTF-8 BOM으로 저장
            File.WriteAllText(path, content, new UTF8Encoding(true));
        }

        private static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            if (value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) < 0)
                return value;
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        /// <summary>따옴표 이스케이프를 지원하는 최소 CSV 파서.</summary>
        private static List<string[]> ReadCsv(string path)
        {
            var rows = new List<string[]>();
            string text = File.ReadAllText(path);
            var fields = new List<string>();
            var field = new StringBuilder();
            bool inQuotes = false;

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < text.Length && text[i + 1] == '"')
                        {
                            field.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        field.Append(c);
                    }
                    continue;
                }

                switch (c)
                {
                    case '"':
                        inQuotes = true;
                        break;
                    case ',':
                        fields.Add(field.ToString());
                        field.Length = 0;
                        break;
                    case '\r':
                        break;
                    case '\n':
                        fields.Add(field.ToString());
                        field.Length = 0;
                        if (fields.Count > 1 || !string.IsNullOrEmpty(fields[0]))
                            rows.Add(fields.ToArray());
                        fields.Clear();
                        break;
                    default:
                        field.Append(c);
                        break;
                }
            }

            if (field.Length > 0 || fields.Count > 0)
            {
                fields.Add(field.ToString());
                if (fields.Count > 1 || !string.IsNullOrEmpty(fields[0]))
                    rows.Add(fields.ToArray());
            }

            return rows;
        }

        #endregion

        private static Dictionary<string, ActorDefinitionSO> BuildActorMap()
        {
            var map = new Dictionary<string, ActorDefinitionSO>();
            foreach (ActorDefinitionSO def in LoadAll<ActorDefinitionSO>())
            {
                string id = string.IsNullOrEmpty(def.actorId) ? def.name : def.actorId;
                map[id] = def;
            }
            return map;
        }

        private static IEnumerable<T> LoadAll<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                var asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset != null)
                    yield return asset;
            }
        }
    }
}
#endif
