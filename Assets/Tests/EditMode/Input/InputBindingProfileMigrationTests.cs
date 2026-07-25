using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UPlayGround.InputDefine;

namespace UPlayGround.Input.Tests
{
    /// <summary>
    /// 스펙 §13.1/13.4 — 바인딩 프로필 저장 라운드트립과 GUID 우선 마이그레이션 검증.
    /// </summary>
    public sealed class InputBindingProfileMigrationTests
    {
        private const string Map = "PlayerAction";

        /// <summary>테스트용 액션 목록. (map, action) ↔ GUID 양방향 조회.</summary>
        private sealed class FakeLookup : IInputActionIdentityLookup
        {
            private readonly List<(string Id, string Map, string Action)> _actions = new();

            public FakeLookup Add(string id, string map, string action)
            {
                _actions.Add((id, map, action));
                return this;
            }

            public bool TryResolveById(string actionId, out string mapName, out string actionName)
            {
                foreach (var entry in _actions)
                {
                    if (!string.Equals(entry.Id, actionId, StringComparison.Ordinal))
                        continue;

                    mapName = entry.Map;
                    actionName = entry.Action;
                    return true;
                }

                mapName = null;
                actionName = null;
                return false;
            }

            public bool TryResolveByName(string mapName, string actionName, out string actionId)
            {
                foreach (var entry in _actions)
                {
                    if (entry.Map != mapName || entry.Action != actionName)
                        continue;

                    actionId = entry.Id;
                    return true;
                }

                actionId = null;
                return false;
            }
        }

        private static InputBindingOverrideEntry Entry(
            string actionId,
            string actionName,
            string controlPath,
            InputBindingSlot slot = InputBindingSlot.Primary) => new()
        {
            actionId = actionId,
            mapName = Map,
            actionName = actionName,
            deviceGroup = InputBindingDeviceGroup.Gamepad,
            slot = slot,
            controlPath = controlPath,
        };

        private static InputBindingProfileData Profile(
            int version,
            params InputBindingOverrideEntry[] entries) => new()
        {
            profileVersion = version,
            entries = entries.ToList(),
        };

        [Test]
        public void 저장_로드_라운드트립에서_항목이_보존된다()
        {
            InputBindingProfileData original = Profile(
                InputBindingProfileMigration.CurrentProfileVersion,
                Entry("guid-dash", "Dash", "<Gamepad>/buttonEast"),
                new InputBindingOverrideEntry
                {
                    actionId = "guid-dodge",
                    mapName = Map,
                    actionName = "Dodge",
                    deviceGroup = InputBindingDeviceGroup.Gamepad,
                    slot = InputBindingSlot.Secondary,
                    isComposite = true,
                    modifierPath = "<Gamepad>/leftShoulder",
                    controlPath = "<Gamepad>/buttonEast",
                });

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<InputBindingProfileData>(json);

            Assert.AreEqual(original.profileVersion, restored.profileVersion);
            Assert.AreEqual(2, restored.entries.Count);

            InputBindingOverrideEntry chord = restored.entries[1];
            Assert.AreEqual("guid-dodge", chord.actionId);
            Assert.IsTrue(chord.isComposite);
            Assert.AreEqual("<Gamepad>/leftShoulder", chord.modifierPath);
            Assert.AreEqual("<Gamepad>/buttonEast", chord.controlPath);
            Assert.AreEqual(InputBindingSlot.Secondary, chord.slot);
        }

        [Test]
        public void GUID가_유지되면_액션_이름이_바뀌어도_override가_살아남는다()
        {
            InputBindingProfileData profile = Profile(
                InputBindingProfileMigration.CurrentProfileVersion,
                Entry("guid-dash", "Dash", "<Gamepad>/buttonEast"));

            var lookup = new FakeLookup().Add("guid-dash", Map, "QuickDash");

            InputBindingMigrationReport report =
                InputBindingProfileMigration.Migrate(profile, lookup);

            Assert.AreEqual(1, profile.entries.Count, "프로필 전체를 폐기하면 안 된다.");
            Assert.AreEqual("QuickDash", profile.entries[0].actionName, "새 이름으로 갱신돼야 한다.");
            Assert.AreEqual("<Gamepad>/buttonEast", profile.entries[0].controlPath);
            Assert.AreEqual(1, report.Renamed);
            Assert.AreEqual(0, report.DroppedTotal);
        }

        [Test]
        public void v1_프로필은_보조_키로_GUID를_보강한다()
        {
            InputBindingProfileData profile = Profile(
                1,
                Entry(null, "Dash", "<Gamepad>/buttonEast"));

            var lookup = new FakeLookup().Add("guid-dash", Map, "Dash");

            InputBindingMigrationReport report =
                InputBindingProfileMigration.Migrate(profile, lookup);

            Assert.AreEqual(InputBindingProfileMigration.CurrentProfileVersion, profile.profileVersion);
            Assert.AreEqual("guid-dash", profile.entries[0].actionId);
            Assert.AreEqual(1, report.Adopted);
            Assert.AreEqual(1, report.FromVersion);
        }

        [Test]
        public void GUID가_사라지면_보조_키로_이전한다()
        {
            // 액션 에셋이 재생성돼 GUID만 바뀐 경우.
            InputBindingProfileData profile = Profile(
                InputBindingProfileMigration.CurrentProfileVersion,
                Entry("guid-old", "Dash", "<Gamepad>/buttonEast"));

            var lookup = new FakeLookup().Add("guid-new", Map, "Dash");

            InputBindingProfileMigration.Migrate(profile, lookup);

            Assert.AreEqual(1, profile.entries.Count);
            Assert.AreEqual("guid-new", profile.entries[0].actionId, "보조 키로 새 GUID를 채워야 한다.");
        }

        [Test]
        public void 후보가_없으면_해당_슬롯만_기본값으로_돌아간다()
        {
            InputBindingProfileData profile = Profile(
                InputBindingProfileMigration.CurrentProfileVersion,
                Entry("guid-gone", "RemovedAction", "<Gamepad>/buttonNorth"),
                Entry("guid-dash", "Dash", "<Gamepad>/buttonEast"));

            var lookup = new FakeLookup().Add("guid-dash", Map, "Dash");

            InputBindingMigrationReport report =
                InputBindingProfileMigration.Migrate(profile, lookup);

            Assert.AreEqual(1, profile.entries.Count, "살아있는 슬롯은 유지돼야 한다.");
            Assert.AreEqual("Dash", profile.entries[0].actionName);
            Assert.AreEqual(1, report.DroppedMissing);
        }

        [Test]
        public void 후보가_둘_이상이면_해당_슬롯만_기본값으로_돌아간다()
        {
            // 두 액션이 같은 이름으로 합쳐져 하나의 슬롯으로 접히는 경우.
            InputBindingProfileData profile = Profile(
                InputBindingProfileMigration.CurrentProfileVersion,
                Entry("guid-a", "OldA", "<Gamepad>/buttonNorth"),
                Entry("guid-b", "OldB", "<Gamepad>/buttonSouth"),
                Entry("guid-dash", "Dash", "<Gamepad>/buttonEast"));

            var lookup = new FakeLookup()
                .Add("guid-a", Map, "Merged")
                .Add("guid-b", Map, "Merged")
                .Add("guid-dash", Map, "Dash");

            InputBindingMigrationReport report =
                InputBindingProfileMigration.Migrate(profile, lookup);

            Assert.AreEqual(1, profile.entries.Count);
            Assert.AreEqual("Dash", profile.entries[0].actionName, "모호하지 않은 슬롯은 유지돼야 한다.");
            Assert.AreEqual(2, report.DroppedAmbiguous);
        }

        [Test]
        public void 같은_액션의_Primary와_Secondary는_서로_다른_슬롯이다()
        {
            InputBindingProfileData profile = Profile(
                InputBindingProfileMigration.CurrentProfileVersion,
                Entry("guid-dash", "Dash", "<Gamepad>/buttonEast", InputBindingSlot.Primary),
                Entry("guid-dash", "Dash", "<Gamepad>/buttonWest", InputBindingSlot.Secondary));

            var lookup = new FakeLookup().Add("guid-dash", Map, "Dash");

            InputBindingMigrationReport report =
                InputBindingProfileMigration.Migrate(profile, lookup);

            Assert.AreEqual(2, profile.entries.Count, "슬롯이 다르면 모호하지 않다.");
            Assert.AreEqual(0, report.DroppedTotal);
        }

        [Test]
        public void 빈_프로필과_null_조회기에서도_예외가_없다()
        {
            Assert.DoesNotThrow(() => InputBindingProfileMigration.Migrate(null, new FakeLookup()));

            InputBindingProfileData profile = Profile(1, Entry("guid-dash", "Dash", "<Gamepad>/buttonEast"));
            InputBindingMigrationReport report = InputBindingProfileMigration.Migrate(profile, null);

            Assert.AreEqual(InputBindingProfileMigration.CurrentProfileVersion, profile.profileVersion);
            Assert.AreEqual(1, profile.entries.Count, "조회기가 없으면 폐기하지 않고 그대로 둔다.");
            Assert.AreEqual(0, report.DroppedTotal);
        }
    }
}
