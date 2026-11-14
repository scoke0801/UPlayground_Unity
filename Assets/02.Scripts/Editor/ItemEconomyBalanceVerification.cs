using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Combat;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;
using UPlayGround.Data.Party;
using UPlayGround.Data.Path;
using UPlayGround.Data.Stat;
using UPlayGround.Tool.Editor.Balance;

namespace UPlayGround.EditorTools
{
    /// <summary>저장·재추첨 없이 실제 장비와 고정 시드 롤 표본의 두 축 및 TTK를 검산한다.</summary>
    public static class ItemEconomyBalanceVerification
    {
        public const string EquipmentCsv = "Tools/ItemEconomy/BalancedEquipment.csv";
        public const string BandsCsv = "Tools/ItemEconomy/BalanceBands.csv";
        public const int SampleCount = 1024;
        public const int Seed = 731113;

        public sealed class Result
        {
            public int band;
            public int level;
            public int stage;
            public string character;
            public string source;
            public string weapon;
            public double attack;
            public double survival;
            public double attackLow;
            public double attackHigh;
            public double survivalLow;
            public double survivalHigh;
            public double expectedDps;
            public double bareDps;
            public bool passed;
        }

        /// <summary>CSV에 포함된 실제 장비의 ID·참조·단계·밴드 사다리를 검사한다.</summary>
        public static List<string> ValidateData()
        {
            var errors = new List<string>();
            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/10.Datas/Path/ItemDatabase.asset");
            var rows = File.ReadAllLines(EquipmentCsv).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Split(',')).ToArray();
            var ids = new HashSet<int>();
            foreach (var row in rows)
            {
                int id = int.Parse(row[2]);
                var equipment = AssetDatabase.LoadAssetAtPath<EquipmentSO>(row[1]);
                if (equipment == null || equipment.itemId != id || !ids.Add(id)) { errors.Add($"장비 ID/경로 오류: {id}"); continue; }
                if (!string.IsNullOrEmpty(row[0]) && AssetDatabase.AssetPathToGUID(row[1]) != row[0]) errors.Add($"GUID 변경: {id}");
                if (!database.AllItems.Contains(equipment)) errors.Add($"DB 미등록: {id}");
                if (equipment.StatModifiers.Count == 0 || equipment.StatModifiers.Any(s => !s.AttributeId.IsValid || s.value <= 0f)) errors.Add($"잘못된 스탯: {id}");
                if (Math.Abs(equipment.rankValueScale - Parse(row[10])) > .00001f) errors.Add($"롤 배율 불일치: {id}");
                if (!equipment.randomAttributePool.Select(p => p.Value).SequenceEqual(row[11].Split(';'))) errors.Add($"옵션 풀 불일치: {id}");
                string[] attributes = { "Vital.MaxHealth", "Combat.Defense", "Combat.AttackPower", "Combat.CritRate" };
                for (int i = 0; i < attributes.Length; i++)
                {
                    float actual = equipment.StatModifiers.Where(s => s.attributeId == attributes[i]).Sum(s => s.value);
                    if (Math.Abs(actual - Parse(row[5 + i])) > .00001f) errors.Add($"수치 불일치: {id}/{attributes[i]}");
                    if (row.Length > 18 && equipment.StatModifiers.Any(s => s.attributeId == attributes[i] && s.modifierType.ToString() != row[18 + i])) errors.Add($"연산 불일치: {id}/{attributes[i]}");
                }
            }
            foreach (var slot in rows.Where(r => int.Parse(r[2]) < 1000).GroupBy(r => int.Parse(r[2]) / 100))
            {
                var ordered = slot.OrderBy(r => int.Parse(r[2])).ToArray();
                if (ordered.Length != 12) errors.Add($"방어구 사슬 누락: {slot.Key}");
                for (int i = 1; i < ordered.Length; i++)
                    if (Parse(ordered[i][5]) <= Parse(ordered[i - 1][5]) || Parse(ordered[i][6]) <= Parse(ordered[i - 1][6])) errors.Add($"방어구 단계 하락: {ordered[i][2]}");
            }
            var bands = ReadBands();
            for (int i = 1; i < bands.Length; i++)
                if (Parse(bands[i - 1][3]) * (1 + 3 * Parse(bands[i - 1][13])) >= Parse(bands[i][3]) || Parse(bands[i - 1][4]) * (1 + 3 * Parse(bands[i - 1][14])) >= Parse(bands[i][4])) errors.Add($"다음 밴드 기본값 역전: B{i + 1}");
            foreach (var group in rows.Where(r => int.Parse(r[2]) >= 1000 && Parse(r[7]) > 0f).GroupBy(r => int.Parse(r[2]) / 1000))
                for (int band = 1; band < 3; band++)
                {
                    var lower = group.Where(r => int.Parse(r[16]) == band).ToArray();
                    var higher = group.Where(r => int.Parse(r[16]) == band + 1).ToArray();
                    if (lower.Length == 0 || higher.Length == 0 || lower.Max(r => Parse(r[7])) >= higher.Min(r => Parse(r[7]))) errors.Add($"무기 밴드 역전/누락: {group.Key}/{band}");
                }
            return errors;
        }

        /// <summary>같은 표본열을 모든 밴드에 적용하며 계산기에 확정 롤만 전달한다.</summary>
        public static Result Analyze(int band, int level, int stage = 0, CharacterActorType character = CharacterActorType.Raon)
        {
            var growth = AssetDatabase.LoadAssetAtPath<PartyMemberGrowthSO>($"Assets/10.Datas/Party/Growth/PartyMemberGrowth_{character}.asset");
            var definition = AssetDatabase.LoadAssetAtPath<PlayerCharacterDefinitionSO>($"Assets/10.Datas/Party/PlayerCharacters/PlayerCharacterDefinition_{character}.asset");
            var config = AssetDatabase.LoadAssetAtPath<PartyConfigSO>("Assets/10.Datas/Party/PartyConfig.asset");
            var progression = new CharacterSkillProgressionService();
            progression.Configure(config.characterSkillTrees, config.skillPointRule, _ => level);
            var tree = progression.GetTree(character);
            for (int pass = 0; pass < 200 && tree != null; pass++)
            {
                bool changed = false;
                foreach (var node in tree.nodes)
                    if (progression.TryTakeNode(character, node.nodeId)) { changed = true; break; }
                if (!changed) break;
            }
            var skills = progression.GetResolvedGrowth(character).AttributeModifiers;
            var bases = PartyPowerCalculator.CalculateBaseStats(growth);
            var bare = CharacterEffectiveStatCalculator.Calculate(bases, null, skills);
            double bareAttack = Attack(bare), bareSurvival = Survival(bare);
            var items = CreateItems(band, stage, definition.defaultWeaponType);
            var generated = items.Where(i => !AssetDatabase.Contains(i.data)).Select(i => i.data).ToArray();
            double[] attack = new double[SampleCount], survival = new double[SampleCount];
            double rawDps = BalanceAttackAnalyzer.EstimatePlayerRawDps(definition.abilitySet, 1.2f, 45f);
            try
            {
                var random = new System.Random(Seed);
                for (int sample = 0; sample < SampleCount; sample++)
                {
                    foreach (var item in items) SetFixtureRolls(item, random);
                    var stats = CharacterEffectiveStatCalculator.Calculate(bases, items, skills);
                    attack[sample] = 1 - bareAttack / Attack(stats);
                    survival[sample] = 1 - bareSurvival / Survival(stats);
                }
                double meanAttack = attack.Average(), meanSurvival = survival.Average();
                Array.Sort(attack); Array.Sort(survival);
                return new Result
                {
                    band = band, level = level, stage = stage, character = character.ToString(),
                    source = band <= 3 ? "actual" : "proposal",
                    weapon = ((EquipmentSO)items[5].data).weaponType.ToString(),
                    attack = meanAttack, survival = meanSurvival,
                    attackLow = attack[SampleCount / 10], attackHigh = attack[SampleCount * 9 / 10],
                    survivalLow = survival[SampleCount / 10], survivalHigh = survival[SampleCount * 9 / 10],
                    expectedDps = rawDps * bareAttack / (1 - meanAttack), bareDps = rawDps * bareAttack,
                    passed = meanAttack >= .25 && meanAttack <= .35 && meanSurvival >= .25 && meanSurvival <= .35,
                };
            }
            finally { foreach (var asset in generated) UnityEngine.Object.DestroyImmediate(asset); }
        }

        /// <summary>각 밴드 결과와 정책 TTK 대조를 빌드 없이 CSV로 보존한다.</summary>
        public static void ExportBand(int band, string directory)
        {
            Directory.CreateDirectory(directory);
            string[] spec = ReadBands()[band - 1];
            var levels = new[] { int.Parse(spec[1]), int.Parse(spec[2]) }.Distinct().ToArray();
            var results = levels.SelectMany(level => new[] { Analyze(band, level), Analyze(band, level, 3) }).ToArray();
            File.WriteAllLines(Path.Combine(directory, $"band-{band}.csv"), new[] { "band,level,stage,character,source,attackMean,survivalMean,attackP10,attackP90,survivalP10,survivalP90,gatePassed" }.Concat(results.Select(r => FormattableString.Invariant($"{r.band},{r.level},{r.stage},{r.character},{r.source},{r.attack},{r.survival},{r.attackLow},{r.attackHigh},{r.survivalLow},{r.survivalHigh},{r.passed}"))));
            var scaling = AssetDatabase.LoadAssetAtPath<MonsterScalingSO>("Assets/10.Datas/Stat/Generated/MonsterScaling_Default.asset");
            var targets = Resources.Load<CombatBalanceTargetsSO>(CombatBalanceTargetsSO.ResourceKey);
            var lines = new List<string> { "band,level,stage,grade,source,monsterHP,TTK_bare,TTK_equipped,target,within20percent" };
            foreach (var result in results)
                foreach (MonsterActorGrade grade in Enum.GetValues(typeof(MonsterActorGrade)))
                {
                    var monster = MonsterStatCalculator.Calculate(scaling, grade, result.level);
                    double effectiveHealth = monster[GrowthAttributeCatalog.Health] / (1 - monster[GrowthAttributeCatalog.Defense]);
                    double ttk = effectiveHealth / result.expectedDps, target = targets.GetTargetSeconds(grade);
                    lines.Add(FormattableString.Invariant($"{band},{result.level},{result.stage},{grade},{result.source},{monster[GrowthAttributeCatalog.Health]},{effectiveHealth / result.bareDps},{ttk},{target},{ttk >= target * .8 && ttk <= target * 1.2}"));
                }
            File.WriteAllLines(Path.Combine(directory, $"ttk-{band}.csv"), lines);
        }

        /// <summary>이전과 같은 레벨·기여도·TTK 기준으로 모든 캐릭터의 실제 호환 주 무기를 검사한다.</summary>
        public static string ExportAcceptance(string directory)
        {
            Directory.CreateDirectory(directory);
            var characters = AssetDatabase.FindAssets("t:PartyMemberGrowthSO", new[] { "Assets/10.Datas/Party/Growth" })
                .Select(g => AssetDatabase.LoadAssetAtPath<PartyMemberGrowthSO>(AssetDatabase.GUIDToAssetPath(g))).OrderBy(g => g.characterType).ToArray();
            int count = 0, passed = 0, ttkCount = 0, ttkPassed = 0;
            var contributions = new List<string> { "character,band,level,weapon,source,attackMean,survivalMean,passed" };
            var times = new List<string> { "band,level,grade,HP,equippedTTK,target,within20percent" };
            var scaling = AssetDatabase.LoadAssetAtPath<MonsterScalingSO>(ItemEconomyCombatCalibration.ScalingPath);
            var targets = Resources.Load<CombatBalanceTargetsSO>(CombatBalanceTargetsSO.ResourceKey);
            foreach (var row in ReadBands())
            {
                int band = int.Parse(row[0]);
                foreach (int level in new[] { int.Parse(row[1]), int.Parse(row[2]) }.Distinct())
                {
                    foreach (var character in characters)
                    {
                        var result = Analyze(band, level, 0, character.characterType);
                        contributions.Add(FormattableString.Invariant($"{result.character},{band},{level},{result.weapon},{result.source},{result.attack},{result.survival},{result.passed}"));
                        count++; if (result.passed) passed++;
                    }
                    var reference = Analyze(band, level);
                    foreach (MonsterActorGrade grade in Enum.GetValues(typeof(MonsterActorGrade)))
                    {
                        var stats = MonsterStatCalculator.Calculate(scaling, grade, level);
                        double ttk = stats[GrowthAttributeCatalog.Health] / ((1 - stats[GrowthAttributeCatalog.Defense]) * reference.expectedDps);
                        double target = targets.GetTargetSeconds(grade);
                        bool accepted = ttk >= target * .8 && ttk <= target * 1.2;
                        times.Add(FormattableString.Invariant($"{band},{level},{grade},{stats[GrowthAttributeCatalog.Health]},{ttk},{target},{accepted}"));
                        ttkCount++; if (accepted) ttkPassed++;
                    }
                }
            }
            File.WriteAllLines(Path.Combine(directory, "contributions.csv"), contributions);
            File.WriteAllLines(Path.Combine(directory, "ttk.csv"), times);
            string summary = $"contributions={passed}/{count}, ttk={ttkPassed}/{ttkCount}";
            File.WriteAllText(Path.Combine(directory, "summary.txt"), summary);
            return summary;
        }

        private static ItemInstance[] CreateItems(int band, int stage, WeaponType weaponType)
        {
            if (band <= 3)
            {
                int number = (band - 1) * 4 + stage + 1;
                int[] ids = { 100 + number, 200 + number, 300 + number, 400 + number, 500 + number };
                var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>("Assets/10.Datas/Path/ItemDatabase.asset");
                var bandIds = File.ReadAllLines(EquipmentCsv).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Split(',')).Where(r => int.Parse(r[16]) == band).Select(r => int.Parse(r[2])).ToHashSet();
                var weapon = database.AllItems.OfType<EquipmentSO>().Where(e => bandIds.Contains(e.itemId) && e.equipSlot == EquipPosition.RightHand && weaponType.AcceptsMainWeaponItem(e.weaponType)).OrderBy(e => e.weaponType == weaponType ? 0 : 1).ThenBy(e => e.itemId).First();
                return ids.Select(id => database.AllItems.Single(i => i.itemId == id)).Concat(new[] { weapon }).Select(e => new ItemInstance { data = e, count = 1 }).ToArray();
            }
            string[] row = ReadBands()[band - 1];
            var items = new ItemInstance[6];
            for (int slot = 0; slot < items.Length; slot++)
            {
                var equipment = ScriptableObject.CreateInstance<EquipmentSO>();
                equipment.weaponType = slot == 5 ? weaponType : WeaponType.NoWeapon;
                equipment.rankValueScale = Parse(row[7]);
                equipment.randomAttributeCountMin = int.Parse(row[8]); equipment.randomAttributeCountMax = int.Parse(row[9]);
                equipment.randomRankMin = int.Parse(row[10]); equipment.randomRankMax = int.Parse(row[11]);
                string[] ids = slot == 5 ? new[] { GrowthAttributeCatalog.AttackPowerId, GrowthAttributeCatalog.CriticalId, GrowthAttributeCatalog.AttackSpeedId }
                    : new[] { GrowthAttributeCatalog.HealthId, GrowthAttributeCatalog.StaminaId, GrowthAttributeCatalog.DefenseId, GrowthAttributeCatalog.AttackSpeedId };
                equipment.randomAttributePool = ids.Select(AttributeReference.CreateRegistered).ToList();
                float weight = new[] { 1f, 1.4f, 1.2f, .7f, .7f, 0f }[slot];
                var serialized = new SerializedObject(equipment);
                var stats = serialized.FindProperty("_statModifiers"); stats.arraySize = 2;
                for (int i = 0; i < 2; i++)
                {
                    var entry = stats.GetArrayElementAtIndex(i);
                    entry.FindPropertyRelative("attributeId").stringValue = slot == 5 ? (i == 0 ? GrowthAttributeCatalog.AttackPowerId : GrowthAttributeCatalog.CriticalId) : (i == 0 ? GrowthAttributeCatalog.HealthId : GrowthAttributeCatalog.DefenseId);
                    entry.FindPropertyRelative("value").floatValue = slot == 5
                        ? (i == 0 ? 1f + Parse(row[5]) : weaponType == WeaponType.Staff ? 0f : Parse(row[6]))
                        : i == 0 ? Mathf.Pow(1f + Parse(row[3]) * (1f + Parse(row[13]) * stage), weight / 5f)
                        : Parse(row[4]) / 5f * weight * (1f + Parse(row[14]) * stage);
                    entry.FindPropertyRelative("modifierType").enumValueIndex = i == 0 ? (int)ModifierType.Multiply : (int)ModifierType.Flat;
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                items[slot] = new ItemInstance { data = equipment, count = 1 };
            }
            return items;
        }

        private static void SetFixtureRolls(ItemInstance item, System.Random random)
        {
            var equipment = (EquipmentSO)item.data;
            item.growthAttributeRolls.Clear();
            var remaining = equipment.randomAttributePool.ToList();
            int count = random.Next(equipment.randomAttributeCountMin, equipment.randomAttributeCountMax + 1);
            for (int i = 0; i < count; i++)
            {
                int index = random.Next(remaining.Count);
                item.growthAttributeRolls.Add(new EquipmentGrowthAttributeRoll { attributeId = remaining[index].Value, rank = random.Next(equipment.randomRankMin, equipment.randomRankMax + 1) });
                remaining.RemoveAt(index);
            }
        }

        private static double Attack(IReadOnlyDictionary<AttributeId, float> stats) => stats[GrowthAttributeCatalog.AttackPower] * (1 + Mathf.Clamp01(stats[GrowthAttributeCatalog.Critical]) * (Mathf.Max(1, stats[new AttributeId("Combat.CritMultiplier")]) - 1));
        private static double Survival(IReadOnlyDictionary<AttributeId, float> stats) => stats[GrowthAttributeCatalog.Health] / (1.0 - stats[GrowthAttributeCatalog.Defense]);
        private static string[][] ReadBands() => File.ReadAllLines(BandsCsv).Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.Split(',')).ToArray();
        private static float Parse(string value) => float.Parse(value, CultureInfo.InvariantCulture);
    }
}
