using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using NUnit.Framework;
using UPlayGround.Data.Save;

namespace UPlayGround.Core.Tests
{
    /// <summary>Assembly-CSharp 소유 이관을 기존 테스트 경계에서 호출해 원본과 진행 보존을 검사한다.</summary>
    public sealed class LivingWorldMapMigrationTests
    {
        private const string Guid = "246895bd33e24c92ad5e4b8b548345e4";
        private const string Marker = "lake.living_world.map_split.v1";
        private static Type Migration => Type.GetType("UPlayGround.Manager.Save.LivingWorldMapMigration, Assembly-CSharp", true);
        private static object Plan()
        {
            var plan = Activator.CreateInstance(Type.GetType("UPlayGround.Manager.Save.LivingWorldMapMigrationPlan, Assembly-CSharp", true));
            Set(plan, "SourceMapId", "LakeOfLifeScenario");
            Set(plan, "SourceSceneName", "LakeOfLifeScenario");
            Set(plan, "TargetMapId", "DroughtVillage");
            Set(plan, "TargetSceneName", "DroughtVillage");
            Set(plan, "IsPlayerLocationReviewed", true);
            return plan;
        }

        private static void Set(object value, string field, object data) => value.GetType().GetField(field).SetValue(value, data);
        private static GameSaveData Source() => new GameSaveData
        {
            party = new PartySaveData { mapId = "LakeOfLifeScenario", loadSceneName = "LakeOfLifeScenario", hasLocation = true,
                storyProtagonistType = "Arin", playerRot = new SerializableQuaternion { w = 1 } }
        };

        private static bool Apply(GameSaveData source, object plan, out GameSaveData copy)
        {
            object[] args = { source, plan, null, null };
            bool result = (bool)Migration.GetMethod("TryCreateCopy").Invoke(null, args);
            copy = (GameSaveData)args[2];
            return result;
        }

        private static void AddEntity(object plan, bool verified = true)
        {
            var move = Activator.CreateInstance(Type.GetType("UPlayGround.Manager.Save.LivingWorldEntityMove, Assembly-CSharp", true));
            Set(move, "HasVerifiedPose", verified);
            Set(move, "Position", new SerializableVector3 { x = 950, y = 72, z = 480 });
            Set(move, "Rotation", new SerializableQuaternion { w = 1 });
            ((IDictionary)plan.GetType().GetField("Entities").GetValue(plan)).Add(Guid, move);
        }

        [Test]
        public void 이동한_GUID만_옮기고_원본과_진행은_보존한다()
        {
            var source = Source();
            source.flags.flags["lake.red_tree.water_handed_over"] = true;
            source.flags.flags["lake.red_tree.return.common_ending"] = true;
            source.world.killedMonsters["LakeOfLifeScenario"] = new List<string> { Guid, Guid, "unknown" };
            source.world.consumedInteractables["LakeOfLifeScenario"] = new List<string> { Guid, "unmoved" };
            source.world.respawnStates.Add(new MonsterRespawnState { guid = Guid, mapId = "LakeOfLifeScenario", actorId = "unchanged",
                respawnCount = 7, waitingRespawn = true, nextRespawnGameMinute = 500 });
            var plan = Plan(); AddEntity(plan);
            Assert.IsTrue(Apply(source, plan, out var copy));
            CollectionAssert.AreEqual(new[] { "unknown" }, copy.world.killedMonsters["LakeOfLifeScenario"]);
            CollectionAssert.AreEqual(new[] { Guid }, copy.world.killedMonsters["DroughtVillage"]);
            CollectionAssert.AreEqual(new[] { "unmoved" }, copy.world.consumedInteractables["LakeOfLifeScenario"]);
            Assert.AreEqual(3, source.world.killedMonsters["LakeOfLifeScenario"].Count);
            Assert.AreEqual("LakeOfLifeScenario", source.world.respawnStates[0].mapId);
            Assert.AreEqual("DroughtVillage", copy.world.respawnStates[0].mapId);
            Assert.AreEqual(7, copy.world.respawnStates[0].respawnCount);
            Assert.AreEqual(500, copy.world.respawnStates[0].nextRespawnGameMinute);
            Assert.AreEqual("unchanged", copy.world.respawnStates[0].actorId);
            Assert.IsTrue(copy.flags.flags["lake.red_tree.return.common_ending"]);
            Assert.IsTrue(copy.flags.flags["lake.red_tree.water_handed_over"]);
            Assert.IsFalse(copy.flags.flags.ContainsKey(Marker));
            Assert.AreEqual("Arin", copy.party.storyProtagonistType);
        }

        [Test]
        public void 위치_미검토와_씬_불일치와_알수없는_버전은_실패한다()
        {
            var source = Source(); var plan = Plan();
            Set(plan, "IsPlayerLocationReviewed", false);
            Assert.IsFalse(Apply(source, plan, out _));
            Set(plan, "IsPlayerLocationReviewed", true);
            source.party.loadSceneName = "Other";
            Assert.IsFalse(Apply(source, plan, out _));
            source.party.loadSceneName = "LakeOfLifeScenario"; source.saveVersion = "9.0";
            Assert.IsFalse(Apply(source, plan, out _));
        }

        [Test]
        public void 위치가_없는_저장은_기본_스폰을_유지한다()
        {
            var source = Source(); source.party.hasLocation = false;
            var plan = Plan(); Set(plan, "IsPlayerLocationReviewed", false);
            Assert.IsTrue(Apply(source, plan, out var copy));
            Assert.IsFalse(copy.party.hasLocation);
            Assert.AreEqual(source.party.mapId, copy.party.mapId);
        }

        [Test]
        public void 검증된_도착만_적용하며_완료_사본은_멱등이다()
        {
            var source = Source(); var plan = Plan();
            Set(plan, "PlayerDestination", new PartySaveData { mapId = "DroughtVillage", loadSceneName = "DroughtVillage",
                hasLocation = true, playerPos = new SerializableVector3 { x = 978, y = 66, z = 451 }, playerRot = new SerializableQuaternion { w = 1 } });
            Assert.IsTrue(Apply(source, plan, out var copy));
            Assert.AreEqual("DroughtVillage", copy.party.mapId);
            Assert.AreEqual("LakeOfLifeScenario", source.party.mapId);
            copy.flags.flags[Marker] = true;
            Assert.IsTrue(Apply(copy, null, out var repeated));
            Assert.AreEqual(copy.party.mapId, repeated.party.mapId);
            Assert.AreEqual(copy.party.playerPos.x, repeated.party.playerPos.x);
        }

        [Test]
        public void 재스폰_좌표_미검증과_대상_키_충돌은_부분적용하지_않는다()
        {
            var source = Source(); var plan = Plan(); AddEntity(plan, false);
            source.world.respawnStates.Add(new MonsterRespawnState { mapId = "LakeOfLifeScenario", guid = Guid });
            Assert.IsFalse(Apply(source, plan, out var copy)); Assert.IsNull(copy);
            plan = Plan(); AddEntity(plan);
            source.world.respawnStates.Add(new MonsterRespawnState { mapId = "DroughtVillage", guid = Guid });
            Assert.IsFalse(Apply(source, plan, out copy)); Assert.IsNull(copy);
        }

        [Test]
        public void 별도_빈슬롯만_암호화하고_원본과_활성슬롯을_보존한다()
        {
            string folder = Path.Combine(Path.GetTempPath(), "B_MapCopy_" + System.Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            Type goType = Type.GetType("UnityEngine.GameObject, UnityEngine.CoreModule", true);
            object go = Activator.CreateInstance(goType, new object[] { "B_CopySlotTest" });
            Type managerType = Type.GetType("UPlayGround.Manager.SaveManager, Assembly-CSharp", true);
            object manager = goType.GetMethod("AddComponent", new[] { typeof(Type) }).Invoke(go, new object[] { managerType });
            managerType.GetField("_saveFolder", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(manager, folder);
            string original = "{\"saveVersion\":\"3.4\",\"party\":{\"mapId\":\"LakeOfLifeScenario\",\"loadSceneName\":\"LakeOfLifeScenario\",\"hasLocation\":true,\"storyProtagonistType\":\"Arin\"},\"flags\":{\"flags\":{\"lake.red_tree.water_handed_over\":true}}}";
            string source = Path.Combine(folder, "save_slot_1001.sav");
            string destination = Path.Combine(folder, "save_slot_1002.sav");
            try
            {
                File.WriteAllText(source, original);
                var method = managerType.GetMethod("TryCreateLivingWorldCopy");
                object[] args = { 1001, 1002, Plan(), null };
                Assert.IsTrue((bool)method.Invoke(manager, args), args[3] as string);
                Assert.AreEqual(original, File.ReadAllText(source));
                Assert.IsNull(managerType.GetProperty("ActiveSlot").GetValue(manager));
                byte[] firstCopy = File.ReadAllBytes(destination);
                Assert.IsFalse((bool)method.Invoke(manager, args));
                CollectionAssert.AreEqual(firstCopy, File.ReadAllBytes(destination));
                args[1] = 1003;
                File.WriteAllText(Path.Combine(folder, "save_slot_1003.sav.bak"), "keep");
                Assert.IsFalse((bool)method.Invoke(manager, args));
                Assert.IsFalse(File.Exists(Path.Combine(folder, "save_slot_1003.sav")));
                args[1] = 1004;
                File.WriteAllText(Path.Combine(folder, "save_slot_1004.sav.tmp"), "keep");
                Assert.IsFalse((bool)method.Invoke(manager, args));
                // 실제 암호화 사본을 다시 읽어 멱등 복사까지 검사한다.
                args = new object[] { 1002, 1005, null, null };
                Assert.IsTrue((bool)method.Invoke(manager, args), args[3] as string);
                Assert.AreEqual(0, Directory.GetFiles(folder, "*.livingworld.*.tmp").Length);
            }
            finally
            {
                Type unityObject = Type.GetType("UnityEngine.Object, UnityEngine.CoreModule", true);
                unityObject.GetMethod("DestroyImmediate", new[] { unityObject, typeof(bool) }).Invoke(null, new[] { go, (object)false });
                // 이 테스트가 만든 단일 전용 폴더의 파일만 정리한다.
                foreach (string path in Directory.GetFiles(folder)) File.Delete(path);
                Directory.Delete(folder);
            }
        }
    }
}
