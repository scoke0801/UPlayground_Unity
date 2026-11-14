using System;
using System.IO;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UPlayGround.Data.Item;

namespace UPlayGround.EditorTools.Tests
{
    /// <summary>경제 생성기의 GUID 식별과 중간 실패 시 전체 롤백을 검증한다.</summary>
    public sealed class ItemEconomyEquipmentImportTests
    {
        private const string Csv = "Tools/ItemEconomy/P0Equipment.csv";
        private const string Database = "Assets/10.Datas/Path/ItemDatabase.asset";

        [Test]
        public void Import_MiddleFailure_RestoresExistingAssetAndGuid()
        {
            var first = File.ReadAllLines(Csv)[1].Split(',');
            var asset = AssetDatabase.LoadAssetAtPath<EquipmentSO>(first[1]);
            string before = EditorJsonUtility.ToJson(asset);
            string databaseBefore = EditorJsonUtility.ToJson(AssetDatabase.LoadMainAssetAtPath(Database));
            string path = "scratchpad/item-economy/rollback.csv";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            first[5] = "12345";
            try
            {
                File.WriteAllLines(path, new[] { File.ReadAllLines(Csv)[0], string.Join(",", first) });
                Assert.Throws<InvalidOperationException>(() => ItemEconomyEquipmentImport.Import(path, Database, 1));
            }
            finally { File.Delete(path); }
            Assert.That(EditorJsonUtility.ToJson(asset), Is.EqualTo(before));
            Assert.That(AssetDatabase.AssetPathToGUID(first[1]), Is.EqualTo(first[0]));
            Assert.That(EditorJsonUtility.ToJson(AssetDatabase.LoadMainAssetAtPath(Database)), Is.EqualTo(databaseBefore));
        }

        [Test]
        public void EquipmentData_HasStatsAndUniqueRegisteredIds()
        {
            var database = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Path.ItemDatabase>(Database);
            var equipment = AssetDatabase.FindAssets("t:EquipmentSO", new[] { "Assets/10.Datas/Item" })
                .Select(g => AssetDatabase.LoadAssetAtPath<EquipmentSO>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
            Assert.That(equipment.Select(e => e.itemId).Distinct().Count(), Is.EqualTo(equipment.Length));
            Assert.That(equipment.Any(e => e.itemId == 101), Is.True);
            foreach (var item in equipment)
            {
                Assert.That(database.AllItems.Contains(item), Is.True, item.name);
                Assert.That(item.StatModifiers.Any(s => s.AttributeId.IsValid && s.value > 0f), Is.True, item.name);
            }
            foreach (string line in File.ReadAllLines(Csv).Skip(1))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                string[] cells = line.Split(',');
                if (!string.IsNullOrEmpty(cells[0]))
                    Assert.That(AssetDatabase.AssetPathToGUID(cells[1]), Is.EqualTo(cells[0]));
            }
        }

        [Test]
        public void Import_InvalidGuid_DoesNotFallBackToPath()
        {
            var lines = File.ReadAllLines(Csv);
            var cells = lines[1].Split(',');
            cells[0] = "00000000000000000000000000000001";
            string folder = "scratchpad/item-economy";
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, "invalid-guid.csv");
            try
            {
                File.WriteAllLines(path, new[] { lines[0], string.Join(",", cells) });
                Assert.Throws<InvalidDataException>(() => ItemEconomyEquipmentImport.Import(path, Database));
            }
            finally { File.Delete(path); }
        }
    }
}
