using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.Data.Save;

namespace UPlayGround.FlowGraph.Tests
{
    /// <summary>격리된 암호화 원본과 빈 복사 슬롯에서 실제 이관 API의 보존 계약을 검사한다.</summary>
    public sealed class RedTreeSaveFileCopyTests
    {
        [TestCase("Raon", 0)]
        [TestCase("Raon", 1)]
        [TestCase("Raon", 2)]
        [TestCase("Arin", 0)]
        [TestCase("Arin", 1)]
        [TestCase("Arin", 2)]
        public void 암호화_복사본은_원본과_성장장비를_보존하고_재이관해도_같다(string protagonist, int oldStage)
        {
            string directory = Path.GetFullPath("output/RedTreeR03Integration/SaveCopies/" + protagonist + "-" + oldStage + "-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var root = new GameObject("격리 저장 복사 검사");
            root.SetActive(false);
            try
            {
                Type saveType = Type.GetType("UPlayGround.Manager.SaveManager, Assembly-CSharp", true);
                Component manager = root.AddComponent(saveType);
                saveType.GetField("_saveFolder", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(manager, directory);
                MethodInfo copy = saveType.GetMethod("TryCreateRedTreeCopy");
                var source = CreateSource(protagonist, oldStage);
                string sourcePath = Path.Combine(directory, "save_slot_0.sav");
                File.WriteAllBytes(sourcePath, Encrypt(Serialize(source)));
                string sourceHash = Hash(sourcePath);
                object[] args = { 0, 1, "test_red_tree_map", "test_red_tree_scene", null };
                Assert.That(copy.Invoke(manager, args), Is.EqualTo(true), args[4] as string);
                string destinationPath = Path.Combine(directory, "save_slot_1.sav");
                string destinationHash = Hash(destinationPath);
                GameSaveData migrated = Read(destinationPath);
                Assert.That(Hash(sourcePath), Is.EqualTo(sourceHash));
                Assert.That(Serialize(Read(sourcePath)), Is.EqualTo(Serialize(source)));
                Assert.That(migrated.party.storyProtagonistType, Is.EqualTo(protagonist));
                Assert.That(migrated.party.roster, Is.EqualTo(source.party.roster));
                Assert.That(migrated.party.battleOrder, Is.EqualTo(source.party.battleOrder));
                Assert.That(migrated.party.activeIndex, Is.EqualTo(source.party.activeIndex));
                Assert.That(Serialize(migrated.party.members), Is.EqualTo(Serialize(source.party.members)));
                Assert.That(Serialize(migrated.party.abilitySystems), Is.EqualTo(Serialize(source.party.abilitySystems)));
                Assert.That(Serialize(migrated.inventory), Is.EqualTo(Serialize(source.inventory)));
                Assert.That(migrated.quest.activeQuests, Is.Empty);
                Assert.That(migrated.quest.trackedQuestId, Is.EqualTo(RedTreeSaveMigration.ArrivalQuestId));
                Assert.That(migrated.flags.flags.ContainsKey("lake.red_tree.hwarin_consent"), Is.False);
                Assert.That(migrated.flags.flags.ContainsKey("lake.red_tree.lucia_defeated"), Is.False);
                Assert.That(migrated.flags.flags.GetValueOrDefault("lake.story.alternate_self_defeated"), Is.EqualTo(oldStage == 2));
                Assert.That(saveType.GetProperty("ActiveSlot").GetValue(manager), Is.Null);

                Assert.That(copy.Invoke(manager, args), Is.EqualTo(false), "기존 대상 슬롯 덮어쓰기 금지");
                Assert.That(Hash(destinationPath), Is.EqualTo(destinationHash));
                args = new object[] { 0, 0, "test_red_tree_map", "test_red_tree_scene", null };
                Assert.That(copy.Invoke(manager, args), Is.EqualTo(false));
                args = new object[] { 1, 2, "another_map", "another_scene", null };
                Assert.That(copy.Invoke(manager, args), Is.EqualTo(true), args[4] as string);
                Assert.That(Serialize(Read(Path.Combine(directory, "save_slot_2.sav"))),
                    Is.EqualTo(Serialize(migrated)));
                Assert.That(Hash(sourcePath), Is.EqualTo(sourceHash));
                Assert.That(Hash(destinationPath), Is.EqualTo(destinationHash));
                File.WriteAllText(Path.Combine(directory, "result.json"), Serialize(new
                {
                    protagonist, oldStage, sourceHash, destinationHash, originalPreserved = true,
                    growthAndEquipmentPreserved = true, idempotent = true,
                    scope = "실제 암호화 파일과 SaveManager API. 씬 로드·본편 재개는 검사하지 않음."
                }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static GameSaveData CreateSource(string protagonist, int oldStage)
        {
            var save = new GameSaveData();
            save.party.storyProtagonistType = protagonist;
            save.party.roster.Add(protagonist);
            if (oldStage > 0) save.party.roster.Add("Hwarin");
            save.party.battleOrder = new List<string>(save.party.roster);
            save.party.activeIndex = oldStage > 0 ? 1 : 0;
            save.party.hasLocation = true;
            save.party.members.Add(new PartyMemberSaveEntry { type = protagonist, level = 9, exp = 731 });
            save.party.abilitySystems.Add(new CharacterAbilitySystemSaveEntry { type = protagonist });
            save.inventory.gold = 731;
            save.inventory.items.Add(new ItemSaveEntry { itemId = 12345, slotKey = 7, count = 1, enhancementLevel = 3 });
            save.inventory.equipment.Add(new CharacterEquipmentSaveEntry { type = protagonist, rightHand = 7 });
            save.quest.activeQuests.Add(new ActiveQuestSaveEntry { questId = "quest_sub_lake_rescue_honoka" });
            save.quest.trackedQuestId = "quest_sub_lake_rescue_honoka";
            if (oldStage == 2)
            {
                save.flags.flags["lake.story.alternate_self_defeated"] = true;
                save.quest.completedQuestIds.Add("quest_main_lake_shrine");
                save.flow.firedKeys.Add("flow_lake_shrine_chapter1:entry");
            }
            return save;
        }

        private static string Serialize(object value) => (string)Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json", true).GetMethod("SerializeObject", new[] { typeof(object) }).Invoke(null, new[] { value });
        private static GameSaveData Deserialize(string value) => (GameSaveData)Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json", true).GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) }).Invoke(null, new object[] { value, typeof(GameSaveData) });

        private static byte[] Encrypt(string json) => (byte[])Crypto("Encrypt").Invoke(null, new object[] { json });
        private static GameSaveData Read(string path) => Deserialize(
            (string)Crypto("Decrypt").Invoke(null, new object[] { File.ReadAllBytes(path) }));
        private static MethodInfo Crypto(string method) => Type.GetType("UPlayGround.Manager.SaveCrypto, Assembly-CSharp", true).GetMethod(method);
        private static string Hash(string path)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(File.ReadAllBytes(path))).Replace("-", "");
        }
    }
}
