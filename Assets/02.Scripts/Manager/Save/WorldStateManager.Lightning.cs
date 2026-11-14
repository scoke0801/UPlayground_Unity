using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Core.ReactiveEnvironment;
using UPlayGround.Data.ReactiveEnvironment;
using UPlayGround.Data.Save;
using UPlayGround.Gameplay.World.ReactiveEnvironment;

namespace UPlayGround.Manager
{
    public partial class WorldStateManager
    {
        private Dictionary<string, EnvironmentLightningSaveState> _lightningStates = new();
        private readonly Collider[] _lightningOverlaps = new Collider[256];
        private readonly HashSet<IEnvironmentReactionTarget> _lightningTargets = new();
        private readonly HashSet<IDamageable> _lightningCellTargets = new();
        private readonly List<CombatHit> _lightningCellHits = new();

        /// <summary>게임 시간과 지역 난수만 진행하며 언로드·시간 점프를 누적 재생하지 않는다.</summary>
        public bool TryAdvanceLightning(string mapId, EnvironmentLightningProfileSO profile,
            double gameMinute, float storm, out EnvironmentLightningStrike strike)
        {
            strike = default;
            if (!Application.isPlaying || !IsValidLightningInput(mapId, profile, gameMinute)
                || !float.IsFinite(storm) || storm < 0f || storm > 1f) return false;
            var state = GetLightningState(mapId, profile);
            if (!state.isResumed)
            {
                state.isResumed = true; state.lastMinute = gameMinute;
                return false;
            }
            double delta = gameMinute - state.lastMinute;
            state.lastMinute = gameMinute;
            if (delta <= 0 || delta > profile.maximumAdvanceMinutes) return false;
            state.remainingMinutes -= delta * storm;
            if (state.remainingMinutes > 0 || storm == 0f) return false;
            state.remainingMinutes = NextLightningInterval(state, profile);
            int index = (int)(NextLightningRandom(state) * profile.areas.Length);
            var bounds = profile.areas[index].sourceBounds;
            var point = new Vector3(Mathf.Lerp(bounds.min.x, bounds.max.x, NextLightningRandom(state)),
                bounds.max.y, Mathf.Lerp(bounds.min.z, bounds.max.z, NextLightningRandom(state)));
            return TryRequestLightning(mapId, profile, index, point, gameMinute, out strike);
        }

        /// <summary>명시적 시험도 정규 날씨와 같은 첫 표면·중앙 입력을 사용한다.</summary>
        public bool TryRequestLightning(string mapId, EnvironmentLightningProfileSO profile,
            int areaIndex, Vector3 sourcePoint, double gameMinute, out EnvironmentLightningStrike strike)
        {
            strike = default;
            if (!Application.isPlaying || !IsValidLightningInput(mapId, profile, gameMinute)
                || areaIndex < 0 || areaIndex >= profile.areas.Length || !IsFiniteDirection(sourcePoint)
                || !profile.areas[areaIndex].sourceBounds.Contains(sourcePoint)) return false;
            Vector3 origin = WorldCoordinates.ToWorld(mapId, sourcePoint);
            bool solidHit = Physics.Raycast(origin, Vector3.down, out var hit, profile.probeDistance,
                profile.surfaceLayers, QueryTriggerInteraction.Ignore);
            bool waterHit = UPlayGround.Components.FootstepWaterSurface.TryRaycastDown(origin, profile.probeDistance,
                out var water, out var waterPoint) && (!solidHit || waterPoint.y > hit.point.y);
            if (!solidHit && !waterHit) return false;
            if (waterHit && World.RegionOwnership.Resolve(water) != mapId) return false;
            if (!waterHit && World.RegionOwnership.Resolve(hit.transform) != mapId) return false;
            var state = GetLightningState(mapId, profile);
            if (state.sequence == ulong.MaxValue) throw new InvalidOperationException("낙뢰 사건 순번이 소진되었습니다.");
            var direct = waterHit ? null : CombatHitDetector.ResolveDamageable(hit.collider) as IEnvironmentReactionTarget;
            int layer = direct != null && _environmentLookup.ContainsKey(direct)
                ? direct.Definition.surfaceLayerId : profile.areas[areaIndex].surfaceLayerId;
            if (!waterHit && direct == null)
                foreach (var terrain in _detailTerrains)
                    if (terrain != null && terrain.isActiveAndEnabled && terrain.transform == hit.transform
                        && terrain.Definition != null)
                    { layer = terrain.Definition.surfaceLayerId; break; }
            strike = new EnvironmentLightningStrike(mapId, profile.profileId, state.sequence + 1,
                gameMinute, waterHit ? waterPoint : hit.point, waterHit ? Vector3.up : hit.normal,
                layer, profile.radius, waterHit ? 0f : profile.heatImpulse, waterHit);
            CommitLightning(state, profile, strike, direct);
            return true;
        }

        /// <summary>재진입 첫 갱신은 현재 시각부터 시작하며 남은 간격만 보존한다.</summary>
        public void SuspendLightning(string mapId)
        {
            if (mapId != null && _lightningStates.TryGetValue(mapId, out var state)) state.isResumed = false;
        }

        private static bool IsValidLightningInput(string mapId, EnvironmentLightningProfileSO profile, double minute) =>
            !string.IsNullOrWhiteSpace(mapId) && profile != null && profile.IsValid()
            && !double.IsNaN(minute) && !double.IsInfinity(minute) && minute >= 0;

        private EnvironmentLightningSaveState GetLightningState(string mapId, EnvironmentLightningProfileSO profile)
        {
            if (_lightningStates.TryGetValue(mapId, out var state))
            {
                if (state.profileId != profile.profileId)
                    throw new InvalidOperationException("낙뢰 프로필 변경은 저장 이관이 필요합니다.");
                return state;
            }
            uint seed = unchecked((uint)(profile.randomSeed ^ NewGameElementSeed));
            foreach (char c in mapId) seed = unchecked((seed ^ c) * 16777619u);
            state = new EnvironmentLightningSaveState { mapId = mapId, profileId = profile.profileId,
                randomState = seed == 0 ? 1u : seed };
            state.remainingMinutes = NextLightningInterval(state, profile);
            _lightningStates.Add(mapId, state);
            return state;
        }

        private static float NextLightningRandom(EnvironmentLightningSaveState state)
        {
            uint x = state.randomState;
            x ^= x << 13; x ^= x >> 17; x ^= x << 5;
            state.randomState = x;
            return (x >> 8) * (1f / 16777216f);
        }

        private static float NextLightningInterval(EnvironmentLightningSaveState state, EnvironmentLightningProfileSO profile) =>
            Mathf.Lerp(profile.intervalGameMinutes.x, profile.intervalGameMinutes.y, NextLightningRandom(state));

        private void CommitLightning(EnvironmentLightningSaveState state, EnvironmentLightningProfileSO profile,
            in EnvironmentLightningStrike strike, IEnvironmentReactionTarget direct)
        {
            if (strike.StrikeId <= state.committedSequence) return;
            if (strike.IsWaterSurface)
            {
                state.sequence = state.committedSequence = strike.StrikeId;
                return;
            }
            _lightningTargets.Clear(); _lightningCellTargets.Clear(); _lightningCellHits.Clear();
            if (direct != null) _lightningTargets.Add(direct);
            int count = Physics.OverlapSphereNonAlloc(strike.Point, strike.Radius, _lightningOverlaps,
                profile.surfaceLayers, QueryTriggerInteraction.Collide);
            // 공용 검출기와 같이 포화 시 전체 결과를 복구해 대상 누락을 막는다.
            Collider[] overlaps = count == _lightningOverlaps.Length
                ? Physics.OverlapSphere(strike.Point, strike.Radius, profile.surfaceLayers, QueryTriggerInteraction.Collide)
                : _lightningOverlaps;
            if (overlaps != _lightningOverlaps) count = overlaps.Length;
            for (int i = 0; i < count; i++)
                if (CombatHitDetector.ResolveDamageable(overlaps[i]) is IEnvironmentReactionTarget target)
                    _lightningTargets.Add(target);
            var shape = CombatHitboxShape.Sphere(strike.Point, strike.Radius);
            foreach (var terrain in _detailTerrains)
                if (terrain != null && terrain.isActiveAndEnabled && World.RegionOwnership.Resolve(terrain) == strike.MapId)
                    terrain.CollectHits(shape, null, _lightningCellTargets, _lightningCellHits);
            foreach (var hit in _lightningCellHits)
                if (hit.Damageable is IEnvironmentReactionTarget cell) _lightningTargets.Add(cell);
            var source = new EnvironmentSource { position = strike.Point + strike.Normal * profile.surfaceOffset,
                blockers = profile.blockerLayers.value };
            foreach (var target in _lightningTargets)
            {
                if (!target.IsAvailable || !_environmentLookup.TryGetValue(target, out var binding)
                    || binding.record.mapId != strike.MapId || target.Definition.surfaceLayerId != strike.SurfaceLayerId) continue;
                var direction = target.ReactionPosition - source.position;
                if (target != direct && IsEnvironmentSourceBlocked(source, target.OcclusionRoot, direction, direction.magnitude)) continue;
                binding.reaction.TryApplyCombatDamage(0f, strike.Heat, 0f);
                CaptureEnvironment(binding);
            }
            // 이 메서드는 yield/외부 콜백 없이 끝난다. 대기 열과 순번을 함께 저장하며 FX는 그 뒤에만 실행한다.
            state.sequence = strike.StrikeId;
            state.committedSequence = strike.StrikeId;
        }

        private void ExportLightning(WorldStateSaveData world)
        {
            world.environmentLightning = new List<EnvironmentLightningSaveState>(_lightningStates.Count);
            foreach (var state in _lightningStates.Values) world.environmentLightning.Add(state.Copy());
        }

        private static Dictionary<string, EnvironmentLightningSaveState> PrepareLightningImport(WorldStateSaveData world)
        {
            var states = new Dictionary<string, EnvironmentLightningSaveState>();
            if (world?.environmentLightning == null) return states;
            foreach (var state in world.environmentLightning)
            {
                if (state == null || string.IsNullOrWhiteSpace(state.mapId) || string.IsNullOrWhiteSpace(state.profileId)
                    || state.schemaVersion != 1 || state.randomState == 0 || state.sequence != state.committedSequence
                    || double.IsNaN(state.remainingMinutes) || double.IsInfinity(state.remainingMinutes) || state.remainingMinutes < 0
                    || !states.TryAdd(state.mapId, state.Copy()))
                    throw new ArgumentException("낙뢰 저장의 버전·키·순번·남은 간격이 유효하지 않습니다.");
                states[state.mapId].isResumed = false;
            }
            return states;
        }
    }
}
