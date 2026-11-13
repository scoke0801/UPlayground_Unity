using System;
using System.Collections.Generic;

namespace UPlayGround.Core.ReactiveEnvironment
{
    public enum EnvironmentCombustion { Inactive, Heating, Burning, Cooling, Spent }
    public enum EnvironmentBreakCause { Impact, Fire }
    public enum EnvironmentImpactKind { Generic, Slash, Blunt, Explosion }

    /// <summary>표현·파괴 수명과 독립적으로 보존하는 표면 및 연료 상태.</summary>
    [Serializable]
    public struct EnvironmentReactionState
    {
        public float heat, wetness, charAmount, fuel, durability;
        public EnvironmentCombustion combustion;
        public bool isBroken;
        public EnvironmentBreakCause breakCause;
        public uint revision;
    }

    /// <summary>초·연료 U·구조 피해 DU 기준으로 저작하는 반응 계수.</summary>
    [Serializable]
    public sealed class EnvironmentReactionParameters
    {
        public bool canIgnite = true;
        public bool canBreak = true;
        public float initialFuel = 20;
        public float maxDurability = 100;
        public float heatMultiplier = 1;
        public float rainAcceptance = 1;
        public float rainAbsorption = 0.35f;
        public float drying = 0.02f;
        public float evaporation = 0.08f;
        public float selfHeat = 0.6f;
        public float cooling = 0.25f;
        public float rainCooling = 0.7f;
        public float wetCooling = 0.6f;
        public float waterImpulseCooling = 0.85f;
        public float waterRateCooling = 0.8f;
        public float igniteHeat = 0.65f;
        public float extinguishHeat = 0.25f;
        public float igniteWetMax = 0.3f;
        public float extinguishWet = 0.6f;
        public float burnRate = 1;
        public float damagePerFuel = 6;
        public float slashMultiplier = 1;
        public float bluntMultiplier = 0.65f;
        public float explosionMultiplier = 1.25f;
        public float scorchPerHeat;
        public float externalScorchLimit = 0.35f;

        /// <summary>잘못된 계수와 발화/소화 임계 역전을 저작 경계에서 거부한다.</summary>
        public bool IsValid()
        {
            float[] values = { initialFuel, maxDurability, heatMultiplier, rainAcceptance, rainAbsorption,
                drying, evaporation, selfHeat, cooling, rainCooling, wetCooling, waterImpulseCooling,
                igniteHeat, extinguishHeat, igniteWetMax, extinguishWet, burnRate, damagePerFuel,
                slashMultiplier, bluntMultiplier, explosionMultiplier, scorchPerHeat, externalScorchLimit, waterRateCooling };
            foreach (float value in values)
                if (!EnvironmentReaction.IsNonNegativeFinite(value)) return false;
            return maxDurability > 0 && externalScorchLimit <= 1 && rainAcceptance <= 1 && waterImpulseCooling <= 1
                && igniteHeat <= 1 && extinguishHeat < igniteHeat
                && extinguishWet <= 1 && igniteWetMax < extinguishWet
                && (!canIgnite || initialFuel > 0 && burnRate > 0);
        }
    }

    /// <summary>즉시 구조 피해와 고정 tick 원소 적분을 한 작성자에 모은다.</summary>
    public sealed class EnvironmentReaction
    {
        private readonly EnvironmentReactionParameters _parameters;
        private readonly HashSet<(ulong, uint)> _acceptedHits = new();
        private double _pendingHeat, _pendingWater, _pendingContinuousWater;
        public EnvironmentReactionState State { get; private set; }

        public EnvironmentReaction(EnvironmentReactionParameters parameters, EnvironmentReactionState? restored = null)
        {
            if (parameters == null || !parameters.IsValid())
                throw new ArgumentException("환경 반응 계수가 유효하지 않습니다.", nameof(parameters));
            _parameters = parameters;
            State = restored ?? new EnvironmentReactionState
            { fuel = parameters.initialFuel, durability = parameters.maxDurability };
            if (!IsValidState(State, parameters))
                throw new ArgumentException("환경 저장 상태가 유효하지 않습니다.", nameof(restored));
        }

        /// <summary>같은 실행·연타의 다중 Collider 입력은 한 번만 반영한다.</summary>
        public bool TryApplyImpact(ulong attackId, uint hitSequence, float damage, float heat, float water)
        {
            if (attackId == 0 || !IsNonNegativeFinite(damage) || !IsNonNegativeFinite(heat)
                || !IsNonNegativeFinite(water) || !_acceptedHits.Add((attackId, hitSequence))) return false;
            _pendingHeat += (double)heat * _parameters.heatMultiplier;
            _pendingWater += water;
            var state = State;
            if (_parameters.canBreak && !state.isBroken)
            {
                state.durability = Math.Max(0, state.durability - damage);
                TryBreak(ref state, EnvironmentBreakCause.Impact);
            }
            state.revision++;
            State = state;
            return true;
        }

        /// <summary>기존 전투 검출기가 중복 제거한 기계적 타격을 원소 키와 섞지 않고 적용한다.</summary>
        public bool TryApplyCombatDamage(float damage, float heat = 0, float water = 0,
            EnvironmentImpactKind kind = EnvironmentImpactKind.Generic)
        {
            if (!IsNonNegativeFinite(damage) || !IsNonNegativeFinite(heat) || !IsNonNegativeFinite(water)
                || !Enum.IsDefined(typeof(EnvironmentImpactKind), kind)
                || State.isBroken && heat == 0 && water == 0) return false;
            _pendingHeat += (double)heat * _parameters.heatMultiplier;
            _pendingWater += water;
            var state = State;
            float multiplier = kind == EnvironmentImpactKind.Slash ? _parameters.slashMultiplier
                : kind == EnvironmentImpactKind.Blunt ? _parameters.bluntMultiplier
                : kind == EnvironmentImpactKind.Explosion ? _parameters.explosionMultiplier : 1;
            if (_parameters.canBreak && !state.isBroken)
            {
                state.durability = (float)Math.Max(0, state.durability - (double)damage * multiplier);
                TryBreak(ref state, EnvironmentBreakCause.Impact);
            }
            state.revision++;
            State = state;
            return true;
        }

        /// <summary>입력 호출 순서와 무관하게 수분을 먼저 적분하고 연료 소비를 확정한다.</summary>
        public void Tick(float seconds, float rain, float exposure)
        {
            if (!IsNonNegativeFinite(seconds) || seconds == 0 || !IsUnit(rain) || !IsUnit(exposure))
                throw new ArgumentOutOfRangeException(nameof(seconds), "환경 tick 입력이 유효하지 않습니다.");
            var p = _parameters;
            var s = State;
            double water = _pendingWater;
            double continuousWater = _pendingContinuousWater;
            double heat = _pendingHeat;
            _pendingHeat = _pendingWater = _pendingContinuousWater = 0;
            float contact = rain * exposure * p.rainAcceptance;
            float wet = Clamp01(s.wetness + water + continuousWater + seconds *
                (contact * p.rainAbsorption - p.drying - p.evaporation * s.heat));
            float selfHeat = s.combustion == EnvironmentCombustion.Burning && s.fuel > 0
                && wet < p.extinguishWet ? p.selfHeat : 0;
            s.heat = Clamp01(s.heat + heat + seconds * (selfHeat - p.cooling * s.heat
                - p.rainCooling * contact - p.wetCooling * wet) - p.waterImpulseCooling * water
                - p.waterRateCooling * continuousWater);
            s.wetness = wet;
            s.charAmount = Math.Max(s.charAmount, (float)Math.Min(p.externalScorchLimit,
                s.charAmount + heat * p.scorchPerHeat));
            bool mustStop = s.fuel <= 0 || wet >= p.extinguishWet || s.heat <= p.extinguishHeat;
            bool burns = s.combustion == EnvironmentCombustion.Burning ? !mustStop
                : p.canIgnite && !mustStop && wet <= p.igniteWetMax && s.heat >= p.igniteHeat;
            float consumed = burns ? Math.Min(s.fuel, p.burnRate * seconds) : 0;
            s.fuel -= consumed;
            if (p.initialFuel > 0) s.charAmount = Math.Max(s.charAmount, Clamp01(1 - s.fuel / p.initialFuel));
            if (p.canBreak && !s.isBroken)
            {
                s.durability = Math.Max(0, s.durability - consumed * p.damagePerFuel);
                TryBreak(ref s, EnvironmentBreakCause.Fire);
            }
            s.combustion = p.initialFuel > 0 && s.fuel == 0 ? EnvironmentCombustion.Spent
                : burns ? EnvironmentCombustion.Burning
                : s.combustion == EnvironmentCombustion.Burning || s.combustion == EnvironmentCombustion.Cooling
                    ? EnvironmentCombustion.Cooling
                : s.heat > 0 ? EnvironmentCombustion.Heating : EnvironmentCombustion.Inactive;
            s.revision++;
            State = s;
        }

        /// <summary>저장 전에 대기 중인 원소 충격량도 보존한다.</summary>
        public void GetPendingInputs(out double heat, out double water)
        { heat = _pendingHeat; water = _pendingWater; }

        /// <summary>지속 물 공급의 별도 냉각 계수까지 저장 경계에 보존한다.</summary>
        public void GetPendingInputs(out double heat, out double water, out double continuousWater)
        { heat = _pendingHeat; water = _pendingWater; continuousWater = _pendingContinuousWater; }

        /// <summary>로드 시 다음 화학 tick에 적용할 충격량을 복원한다.</summary>
        public void RestorePendingInputs(double heat, double water, double continuousWater = 0)
        {
            if (double.IsNaN(heat) || double.IsInfinity(heat) || heat < 0
                || double.IsNaN(water) || double.IsInfinity(water) || water < 0
                || double.IsNaN(continuousWater) || double.IsInfinity(continuousWater) || continuousWater < 0)
                throw new ArgumentException("환경 대기 입력이 유효하지 않습니다.");
            _pendingHeat = heat;
            _pendingWater = water;
            _pendingContinuousWater = continuousWater;
        }

        /// <summary>지속 열원은 dedup 키를 쌓지 않고 초당 rate를 고정 tick에서 한 번 적분한다.</summary>
        public void AddEmitterInput(float heatPerSecond, float waterPerSecond, float seconds)
        {
            if (!IsNonNegativeFinite(heatPerSecond) || !IsNonNegativeFinite(waterPerSecond)
                || !IsNonNegativeFinite(seconds)) throw new ArgumentOutOfRangeException(nameof(seconds));
            _pendingHeat += (double)heatPerSecond * seconds * _parameters.heatMultiplier;
            _pendingContinuousWater += (double)waterPerSecond * seconds;
        }

        /// <summary>오염된 저장 상태를 초기값으로 덮어쓰지 않고 거부한다.</summary>
        public static bool IsValidState(EnvironmentReactionState s, EnvironmentReactionParameters p) =>
            IsUnit(s.heat) && IsUnit(s.wetness) && IsUnit(s.charAmount)
            && IsNonNegativeFinite(s.fuel) && s.fuel <= p.initialFuel
            && IsNonNegativeFinite(s.durability) && s.durability <= p.maxDurability
            && Enum.IsDefined(typeof(EnvironmentCombustion), s.combustion)
            && Enum.IsDefined(typeof(EnvironmentBreakCause), s.breakCause)
            && (!s.isBroken || p.canBreak && s.durability == 0);

        /// <summary>외부 입력의 음수·NaN·무한대를 거부하는 공통 경계.</summary>
        public static bool IsNonNegativeFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && value >= 0;
        private static bool IsUnit(float value) => IsNonNegativeFinite(value) && value <= 1;
        private static float Clamp01(double value) => (float)Math.Max(0, Math.Min(1, value));
        private static void TryBreak(ref EnvironmentReactionState state, EnvironmentBreakCause cause)
        {
            if (state.isBroken || state.durability > 0) return;
            state.isBroken = true;
            state.breakCause = cause;
        }
    }
}
