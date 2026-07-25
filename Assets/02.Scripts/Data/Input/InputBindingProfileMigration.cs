using System;
using System.Collections.Generic;
using System.Text;

namespace UPlayGround.InputDefine
{
    /// <summary>
    /// 프로필 항목 1건의 마이그레이션 처리 결과.
    /// </summary>
    public enum InputBindingMigrationOutcome
    {
        /// <summary>식별자와 이름이 모두 일치해 손댈 것이 없었다.</summary>
        Unchanged,

        /// <summary>GUID로 액션을 찾아 바뀐 map/action 이름을 갱신했다.</summary>
        RenamedByActionId,

        /// <summary>GUID가 없어 (map, action, deviceGroup, slot) 보조 키로 GUID를 채웠다.</summary>
        AdoptedActionId,

        /// <summary>대응 액션을 찾지 못해 해당 슬롯만 기본값으로 되돌렸다.</summary>
        DroppedMissingAction,

        /// <summary>같은 슬롯으로 접히는 항목이 둘 이상이라 해당 슬롯만 기본값으로 되돌렸다.</summary>
        DroppedAmbiguous,
    }

    public readonly struct InputBindingMigrationReport
    {
        public readonly int FromVersion;
        public readonly int ToVersion;
        public readonly int Unchanged;
        public readonly int Renamed;
        public readonly int Adopted;
        public readonly int DroppedMissing;
        public readonly int DroppedAmbiguous;

        public InputBindingMigrationReport(
            int fromVersion,
            int toVersion,
            int unchanged,
            int renamed,
            int adopted,
            int droppedMissing,
            int droppedAmbiguous)
        {
            FromVersion = fromVersion;
            ToVersion = toVersion;
            Unchanged = unchanged;
            Renamed = renamed;
            Adopted = adopted;
            DroppedMissing = droppedMissing;
            DroppedAmbiguous = droppedAmbiguous;
        }

        public int DroppedTotal => DroppedMissing + DroppedAmbiguous;

        public bool HasChanges =>
            FromVersion != ToVersion || Renamed > 0 || Adopted > 0 || DroppedTotal > 0;

        public override string ToString()
        {
            var builder = new StringBuilder();
            builder.Append($"v{FromVersion}→v{ToVersion}");
            builder.Append($" 유지 {Unchanged}");
            if (Renamed > 0) builder.Append($", 이름복구 {Renamed}");
            if (Adopted > 0) builder.Append($", GUID보강 {Adopted}");
            if (DroppedMissing > 0) builder.Append($", 액션없음 폐기 {DroppedMissing}");
            if (DroppedAmbiguous > 0) builder.Append($", 중복 폐기 {DroppedAmbiguous}");
            return builder.ToString();
        }
    }

    /// <summary>
    /// 액션 이름과 GUID를 서로 변환하는 조회기. 런타임은 InputActionAsset로 구현한다.
    /// </summary>
    public interface IInputActionIdentityLookup
    {
        /// <summary>GUID로 현재 map/action 이름을 찾는다.</summary>
        bool TryResolveById(string actionId, out string mapName, out string actionName);

        /// <summary>map/action 이름으로 현재 GUID를 찾는다.</summary>
        bool TryResolveByName(string mapName, string actionName, out string actionId);
    }

    /// <summary>
    /// 바인딩 프로필 버전 마이그레이션 (스펙 §13.4).
    ///
    /// 식별 우선순위는 액션 GUID → (map, action, deviceGroup, slot) 보조 키다.
    /// 실패한 항목만 기본값으로 되돌리고 프로필 전체를 폐기하지 않는다.
    ///
    /// 스펙 문구의 "binding GUID" 대신 액션 GUID를 사용한다.
    /// 사용자 슬롯 바인딩은 EnsureUserBindingSlot이 런타임에 만들어 binding GUID가 세션 간
    /// 안정적이지 않은 반면, 액션 GUID는 에셋에 저장돼 이름 변경에도 유지되기 때문이다.
    /// 슬롯 식별의 나머지 축은 deviceGroup/slot이 그대로 담당한다.
    /// </summary>
    public static class InputBindingProfileMigration
    {
        /// <summary>v1: 이름만 저장. v2: actionId(액션 GUID) 추가.</summary>
        public const int CurrentProfileVersion = 2;

        public static InputBindingMigrationReport Migrate(
            InputBindingProfileData profile,
            IInputActionIdentityLookup lookup)
        {
            if (profile == null)
                return new InputBindingMigrationReport(0, CurrentProfileVersion, 0, 0, 0, 0, 0);

            int fromVersion = profile.profileVersion;
            profile.entries ??= new List<InputBindingOverrideEntry>();

            if (lookup == null)
            {
                profile.profileVersion = CurrentProfileVersion;
                return new InputBindingMigrationReport(
                    fromVersion,
                    CurrentProfileVersion,
                    profile.entries.Count,
                    0,
                    0,
                    0,
                    0);
            }

            int unchanged = 0;
            int renamed = 0;
            int adopted = 0;
            int droppedMissing = 0;

            var resolved = new List<InputBindingOverrideEntry>(profile.entries.Count);

            foreach (InputBindingOverrideEntry entry in profile.entries)
            {
                if (entry == null)
                    continue;

                switch (ResolveEntry(entry, lookup))
                {
                    case InputBindingMigrationOutcome.Unchanged:
                        unchanged++;
                        resolved.Add(entry);
                        break;
                    case InputBindingMigrationOutcome.RenamedByActionId:
                        renamed++;
                        resolved.Add(entry);
                        break;
                    case InputBindingMigrationOutcome.AdoptedActionId:
                        adopted++;
                        resolved.Add(entry);
                        break;
                    default:
                        droppedMissing++;
                        break;
                }
            }

            // 이름 변경으로 서로 다른 항목이 같은 슬롯으로 접히면 어느 쪽이 사용자의 의도인지
            // 알 수 없다. 모호한 슬롯은 통째로 기본값으로 되돌린다.
            int droppedAmbiguous = RemoveAmbiguousTargets(
                resolved,
                out List<InputBindingOverrideEntry> survivors);

            profile.entries = survivors;
            profile.profileVersion = CurrentProfileVersion;

            return new InputBindingMigrationReport(
                fromVersion,
                CurrentProfileVersion,
                unchanged,
                renamed,
                adopted,
                droppedMissing,
                droppedAmbiguous);
        }

        private static InputBindingMigrationOutcome ResolveEntry(
            InputBindingOverrideEntry entry,
            IInputActionIdentityLookup lookup)
        {
            if (!string.IsNullOrWhiteSpace(entry.actionId)
                && lookup.TryResolveById(entry.actionId, out string mapName, out string actionName))
            {
                bool changed =
                    !string.Equals(entry.mapName, mapName, StringComparison.Ordinal)
                    || !string.Equals(entry.actionName, actionName, StringComparison.Ordinal);

                entry.mapName = mapName;
                entry.actionName = actionName;
                return changed
                    ? InputBindingMigrationOutcome.RenamedByActionId
                    : InputBindingMigrationOutcome.Unchanged;
            }

            // GUID가 없거나 사라졌다. 보조 키로 이전을 시도한다.
            if (lookup.TryResolveByName(entry.mapName, entry.actionName, out string resolvedId)
                && !string.IsNullOrWhiteSpace(resolvedId))
            {
                bool hadId = !string.IsNullOrWhiteSpace(entry.actionId);
                entry.actionId = resolvedId;
                return hadId
                    ? InputBindingMigrationOutcome.RenamedByActionId
                    : InputBindingMigrationOutcome.AdoptedActionId;
            }

            return InputBindingMigrationOutcome.DroppedMissingAction;
        }

        private static int RemoveAmbiguousTargets(
            List<InputBindingOverrideEntry> entries,
            out List<InputBindingOverrideEntry> survivors)
        {
            var counts = new Dictionary<InputBindingTarget, int>();
            foreach (InputBindingOverrideEntry entry in entries)
            {
                counts.TryGetValue(entry.Target, out int count);
                counts[entry.Target] = count + 1;
            }

            survivors = new List<InputBindingOverrideEntry>(entries.Count);
            int dropped = 0;
            foreach (InputBindingOverrideEntry entry in entries)
            {
                if (counts[entry.Target] > 1)
                {
                    dropped++;
                    continue;
                }

                survivors.Add(entry);
            }

            return dropped;
        }
    }
}
