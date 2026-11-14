using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Item;
using UPlayGround.Data.Path;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.EditorTools
{
    /// <summary>경제 CSV의 장비 수치만 재생성하고 기존 ID·GUID·외형 참조를 보존한다.</summary>
    public static class ItemEconomyEquipmentImport
    {
        private sealed class Row
        {
            public EquipmentSO asset;
            public EquipmentSO template;
            public string path;
            public int id;
            public string name;
            public float[] values;
            public ItemRarity? rarity;
            public float rankScale;
            public AttributeReference[] pool;
            public int[] rollLimits;
            public ModifierType[] operations = new ModifierType[4];
        }

        /// <summary>전수 사전 검사 뒤 하나의 Undo 그룹으로 적용하며 실패하면 전체를 복원한다.</summary>
        public static int Import(string csvPath, string databasePath, int failAfter = -1)
        {
            var rows = ReadRows(csvPath);
            var database = AssetDatabase.LoadAssetAtPath<ItemDatabase>(databasePath);
            if (database == null) throw new InvalidDataException("아이템 데이터베이스가 없습니다.");
            Undo.IncrementCurrentGroup();
            int group = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("아이템 경제 CSV 반영");
            var changed = new List<UnityEngine.Object> { database };
            try
            {
                Undo.RecordObject(database, "아이템 등록");
                var db = new SerializedObject(database);
                var items = db.FindProperty("allItems");
                int applied = 0;
                foreach (var row in rows)
                {
                    var equipment = row.asset;
                    if (equipment == null)
                    {
                        equipment = UnityEngine.Object.Instantiate(row.template);
                        equipment.name = Path.GetFileNameWithoutExtension(row.path);
                        equipment.itemId = row.id;
                        AssetDatabase.CreateAsset(equipment, row.path);
                        Undo.RegisterCreatedObjectUndo(equipment, "장비 생성");
                    }
                    else Undo.RecordObject(equipment, "장비 수치 반영");
                    changed.Add(equipment);
                    equipment.itemName = row.name;
                    if (row.values != null) WriteStats(equipment, row.values, row.operations);
                    if (row.rarity.HasValue)
                    {
                        equipment.itemRarity = row.rarity.Value;
                        equipment.rankValueScale = row.rankScale;
                        equipment.randomAttributePool = new List<AttributeReference>(row.pool);
                        equipment.randomAttributeCountMin = row.rollLimits[0];
                        equipment.randomAttributeCountMax = row.rollLimits[1];
                        equipment.randomRankMin = row.rollLimits[2];
                        equipment.randomRankMax = row.rollLimits[3];
                    }
                    EditorUtility.SetDirty(equipment);
                    bool registered = false;
                    for (int i = 0; i < items.arraySize; i++)
                        if (items.GetArrayElementAtIndex(i).objectReferenceValue == equipment) registered = true;
                    if (!registered)
                    {
                        items.InsertArrayElementAtIndex(items.arraySize);
                        items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = equipment;
                    }
                    if (++applied == failAfter) throw new InvalidOperationException("롤백 검증을 위한 중간 실패");
                }
                db.ApplyModifiedProperties();
                EditorUtility.SetDirty(database);
                foreach (var asset in changed) AssetDatabase.SaveAssetIfDirty(asset);
                Undo.CollapseUndoOperations(group);
                return rows.Count;
            }
            catch
            {
                Undo.RevertAllDownToGroup(group);
                foreach (var asset in changed)
                    if (asset != null && AssetDatabase.Contains(asset)) AssetDatabase.SaveAssetIfDirty(asset);
                throw;
            }
        }

        private static List<Row> ReadRows(string path)
        {
            var lines = File.ReadAllLines(path);
            const string legacyHeader = "guid,path,id,name,templateGuid,hp,defense,attack,crit";
            const string rollHeader = legacyHeader + ",rarity,rankScale,pool,minCount,maxCount,minRank,maxRank,band,stage";
            bool hasOperations = lines.Length > 0 && lines[0] == rollHeader + ",hpOperation,defenseOperation,attackOperation,critOperation";
            bool hasRollSettings = hasOperations || (lines.Length > 0 && lines[0] == rollHeader);
            if (lines.Length < 2 || (!hasRollSettings && lines[0] != legacyHeader))
                throw new InvalidDataException("장비 CSV 헤더가 다릅니다.");
            var allItems = AssetDatabase.FindAssets("t:ItemSO", new[] { "Assets/10.Datas/Item" })
                .Select(g => AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            var rows = new List<Row>();
            var ids = new HashSet<int>();
            var paths = new HashSet<string>(StringComparer.Ordinal);
            foreach (string line in lines.Skip(1).Where(l => !string.IsNullOrWhiteSpace(l)))
            {
                string[] cells = line.Split(',');
                if (cells.Length != (hasOperations ? 22 : hasRollSettings ? 18 : 9) || line.Contains('"')) throw new InvalidDataException("장비 CSV 열 수 또는 인용부호가 잘못되었습니다.");
                string assetPath = cells[1];
                if (!assetPath.StartsWith("Assets/10.Datas/Item/Equipment/", StringComparison.Ordinal)
                    || assetPath.Contains("..") || !assetPath.EndsWith(".asset", StringComparison.Ordinal))
                    throw new InvalidDataException("장비 폴더 밖 경로입니다.");
                if (!string.IsNullOrEmpty(cells[0]) && AssetDatabase.GUIDToAssetPath(cells[0]) != assetPath)
                    throw new InvalidDataException("GUID와 정확한 경로가 일치하지 않습니다.");
                var asset = AssetDatabase.LoadAssetAtPath<EquipmentSO>(assetPath);
                if (asset == null && (File.Exists(assetPath) || !string.IsNullOrEmpty(cells[0])))
                    throw new InvalidDataException("기존 GUID/경로를 장비로 해석할 수 없습니다.");
                int id = int.Parse(cells[2], CultureInfo.InvariantCulture);
                if (id <= 0 || !ids.Add(id) || !paths.Add(assetPath)
                    || allItems.Any(item => item != null && item.itemId == id && item != asset))
                    throw new InvalidDataException("아이템 ID 또는 경로 중복입니다.");
                if (asset != null && asset.itemId != id) throw new InvalidDataException("기존 ID 변경은 허용하지 않습니다.");
                var template = string.IsNullOrEmpty(cells[4]) ? null
                    : AssetDatabase.LoadAssetAtPath<EquipmentSO>(AssetDatabase.GUIDToAssetPath(cells[4]));
                if (asset == null && template == null) throw new InvalidDataException("신규 장비의 원본 GUID가 없습니다.");
                float[] values = null;
                if (cells.Skip(5).Take(4).Any(c => !string.IsNullOrEmpty(c)))
                {
                    values = cells.Skip(5).Take(4).Select(c => float.Parse(c, CultureInfo.InvariantCulture)).ToArray();
                    if (values.Any(v => float.IsNaN(v) || float.IsInfinity(v) || v < 0f) || values.All(v => v == 0f))
                        throw new InvalidDataException("장비 스탯은 유한한 양수여야 합니다.");
                }
                if (string.IsNullOrWhiteSpace(cells[3])) throw new InvalidDataException("아이템 이름이 없습니다.");
                var row = new Row { asset = asset, template = template, path = assetPath, id = id, name = cells[3], values = values };
                if (hasOperations)
                {
                    row.operations = cells.Skip(18).Take(4).Select(c => (ModifierType)System.Enum.Parse(typeof(ModifierType), c)).ToArray();
                    if (row.operations.Any(op => !System.Enum.IsDefined(typeof(ModifierType), op)))
                        throw new InvalidDataException("장비 수정자 연산이 잘못되었습니다.");
                }
                if (hasRollSettings)
                {
                    row.rarity = (ItemRarity)System.Enum.Parse(typeof(ItemRarity), cells[9]);
                    row.rankScale = float.Parse(cells[10], CultureInfo.InvariantCulture);
                    row.pool = cells[11].Split(';').Select(AttributeReference.CreateRegistered).ToArray();
                    row.rollLimits = cells.Skip(12).Take(4).Select(c => int.Parse(c, CultureInfo.InvariantCulture)).ToArray();
                    if (!System.Enum.IsDefined(typeof(ItemRarity), row.rarity.Value)
                        || float.IsNaN(row.rankScale) || float.IsInfinity(row.rankScale) || row.rankScale < 0f
                        || row.pool.Distinct().Count() != row.pool.Length
                        || row.rollLimits.Any(v => v < 1) || row.rollLimits[0] > row.rollLimits[1]
                        || row.rollLimits[2] > row.rollLimits[3] || row.rollLimits[1] > row.pool.Length)
                        throw new InvalidDataException("장비 롤 설정이 잘못되었습니다.");
                }
                rows.Add(row);
            }
            return rows;
        }

        private static void WriteStats(EquipmentSO asset, float[] values, ModifierType[] operations)
        {
            string[] attributes = { "Vital.MaxHealth", "Combat.Defense", "Combat.AttackPower", "Combat.CritRate" };
            var serialized = new SerializedObject(asset);
            var stats = serialized.FindProperty("_statModifiers");
            stats.ClearArray();
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] == 0f) continue;
                stats.InsertArrayElementAtIndex(stats.arraySize);
                var entry = stats.GetArrayElementAtIndex(stats.arraySize - 1);
                entry.FindPropertyRelative("attributeId").stringValue = attributes[i];
                entry.FindPropertyRelative("modifierType").enumValueIndex = (int)operations[i];
                entry.FindPropertyRelative("value").floatValue = values[i];
            }
            serialized.ApplyModifiedProperties();
        }
    }
}
