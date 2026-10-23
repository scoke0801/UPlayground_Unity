using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UPlayGround.Data.Save;

namespace UPlayGround.Manager.Save
{
    /// <summary>I가 확인한 위치와 이동 GUID를 한 복사 작업에 전달하는 메모리 전용 입력이다.</summary>
    public sealed class LivingWorldMapMigrationPlan
    {
        public string SourceMapId;
        public string SourceSceneName;
        public string TargetMapId;
        public string TargetSceneName;
        public bool IsPlayerLocationReviewed;
        // null은 검토 후 기존 위치 유지. 값이 있으면 이 DTO의 위치 필드만 소비한다.
        public PartySaveData PlayerDestination;
        public readonly Dictionary<string, LivingWorldEntityMove> Entities = new(StringComparer.Ordinal);
    }

    /// <summary>이동된 엔티티의 GUID와 검증된 새 재스폰 위치를 대응한다.</summary>
    public sealed class LivingWorldEntityMove
    {
        public bool HasVerifiedPose;
        public SerializableVector3 Position;
        public SerializableQuaternion Rotation;
    }

    /// <summary>본편 사실을 유지하며 명시된 맵 위치와 엔티티 소유만 복사본에서 이관한다.</summary>
    public static class LivingWorldMapMigration
    {
        public const string CompletedFlag = "lake.living_world.map_split.v1";

        /// <summary>원본을 변경하지 않으며 검증 실패 시 부분 적용된 복사본도 반환하지 않는다.</summary>
        public static bool TryCreateCopy(GameSaveData source, LivingWorldMapMigrationPlan plan,
            out GameSaveData copy, out string error)
        {
            copy = null;
            error = null;
            if (source?.party == null || source.saveVersion != "3.4")
                return Fail("확인된 3.4 저장과 파티 정보가 필요합니다.", out error);
            if (source.flags?.flags != null && source.flags.flags.TryGetValue(CompletedFlag, out bool done) && done)
            {
                copy = Clone(source);
                return true;
            }
            if (plan == null || string.IsNullOrWhiteSpace(plan.SourceMapId)
                || string.IsNullOrWhiteSpace(plan.SourceSceneName) || string.IsNullOrWhiteSpace(plan.TargetMapId)
                || string.IsNullOrWhiteSpace(plan.TargetSceneName) || plan.SourceMapId == plan.TargetMapId)
                return Fail("확정된 서로 다른 맵과 씬 대응이 필요합니다.", out error);

            bool sourceMap = source.party.mapId == plan.SourceMapId;
            bool sourceScene = source.party.loadSceneName == plan.SourceSceneName;
            if (sourceMap != sourceScene)
                return Fail("구 맵 ID와 로드 씬이 불일치합니다. 위치를 추정하지 않았습니다.", out error);
            if (sourceMap && source.party.hasLocation && !plan.IsPlayerLocationReviewed)
                return Fail("이 저장의 구역 판정과 안전 도착 검토가 필요합니다.", out error);
            if (plan.PlayerDestination != null)
            {
                var destination = plan.PlayerDestination;
                bool lake = destination.mapId == plan.SourceMapId && destination.loadSceneName == plan.SourceSceneName;
                bool village = destination.mapId == plan.TargetMapId && destination.loadSceneName == plan.TargetSceneName;
                if (!sourceMap || !source.party.hasLocation || !destination.hasLocation || (!lake && !village)
                    || !IsValidPose(destination.playerPos, destination.playerRot))
                    return Fail("검증된 원래 맵 또는 새 맵의 위치/회전만 지정할 수 있습니다.", out error);
            }
            foreach (var pair in plan.Entities)
            {
                if (!Guid.TryParseExact(pair.Key, "N", out _) || pair.Value == null
                    || pair.Value.HasVerifiedPose && !IsValidPose(pair.Value.Position, pair.Value.Rotation))
                    return Fail("이동 GUID 또는 검증 위치가 잘못되었습니다: " + pair.Key, out error);
            }
            if (source.world?.respawnStates != null)
            {
                var targets = new HashSet<string>(StringComparer.Ordinal);
                foreach (var state in source.world.respawnStates)
                {
                    if (state == null) continue;
                    if (state.mapId == plan.TargetMapId && !targets.Add(state.guid))
                        return Fail("새 맵에 중복 재스폰 키가 있습니다: " + state.guid, out error);
                }
                foreach (var state in source.world.respawnStates)
                {
                    if (state == null || state.mapId != plan.SourceMapId || state.guid == null
                        || !plan.Entities.TryGetValue(state.guid, out var move)) continue;
                    if (!move.HasVerifiedPose || !targets.Add(state.guid))
                        return Fail("재스폰 위치 미검증 또는 대상 키 충돌: " + state.guid, out error);
                }
            }

            copy = Clone(source);
            if (plan.PlayerDestination != null)
            {
                var location = plan.PlayerDestination;
                copy.party.mapId = location.mapId;
                copy.party.loadSceneName = location.loadSceneName;
                copy.party.playerPos = location.playerPos;
                copy.party.playerRot = location.playerRot;
            }
            if (copy.world != null)
            {
                MoveKeys(copy.world.killedMonsters, plan);
                MoveKeys(copy.world.consumedInteractables, plan);
                if (copy.world.respawnStates != null)
                    foreach (var state in copy.world.respawnStates)
                    {
                        if (state == null || state.mapId != plan.SourceMapId || state.guid == null
                            || !plan.Entities.TryGetValue(state.guid, out var move)) continue;
                        state.mapId = plan.TargetMapId;
                        state.position = move.Position;
                        state.rotation = move.Rotation;
                    }
            }
            // 완료 키는 저장 담당이 암호화 사본 재독해까지 검증한 뒤 기록한다.
            return true;
        }

        private static void MoveKeys(Dictionary<string, List<string>> maps, LivingWorldMapMigrationPlan plan)
        {
            if (maps == null || !maps.TryGetValue(plan.SourceMapId, out var oldKeys) || oldKeys == null) return;
            foreach (string guid in oldKeys.ToArray())
            {
                if (guid == null || !plan.Entities.ContainsKey(guid)) continue;
                if (!maps.TryGetValue(plan.TargetMapId, out var target) || target == null)
                    maps[plan.TargetMapId] = target = new List<string>();
                if (!target.Contains(guid)) target.Add(guid);
                oldKeys.RemoveAll(key => key == guid);
            }
        }

        private static bool IsValidPose(SerializableVector3 p, SerializableQuaternion q)
        {
            float length = q.x * q.x + q.y * q.y + q.z * q.z + q.w * q.w;
            return IsFinite(p.x) && IsFinite(p.y) && IsFinite(p.z) && IsFinite(length)
                && Math.Abs(length - 1f) < 0.01f;
        }

        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
        private static GameSaveData Clone(GameSaveData source) =>
            JsonConvert.DeserializeObject<GameSaveData>(JsonConvert.SerializeObject(source));
        private static bool Fail(string message, out string error) { error = message; return false; }
    }
}
