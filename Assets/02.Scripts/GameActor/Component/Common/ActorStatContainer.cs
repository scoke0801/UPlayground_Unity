using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Stat;

namespace UPlayGround.Components
{
    /// <summary>
    /// 모든 GameActor의 런타임 스탯 컨테이너.
    /// GameActor.Awake에서 GetOrAddComponent로 자동 부착된다.
    /// 베이스 값은 ActorStatSO에서 Init으로 주입하고, 장비/버프 등은 StatModifier로 추가/제거한다.
    /// </summary>
    public class ActorStatContainer : MonoBehaviour
    {
        // ── 내부 상태 ─────────────────────────────────────────────
        private readonly Dictionary<StatType, float> _baseStats    = new();
        private readonly List<TimedModifier>         _modifiers    = new();
        private readonly Dictionary<StatType, float> _cachedFinals = new();
        private bool _cacheDirty = true;

        /// <summary>
        /// 스탯 최종값이 변경될 때 발화. (StatType, newFinalValue)
        /// </summary>
        public event Action<StatType, float> OnStatChanged;

        // ── 편의 프로퍼티 ─────────────────────────────────────────
        public float MaxHealth          => GetFinalStat(StatType.MaxHealth);
        public float HealthRegenRate    => GetFinalStat(StatType.HealthRegenRate);
        public float AttackPower        => GetFinalStat(StatType.AttackPower);
        public float Defense            => GetFinalStat(StatType.Defense);
        public float CritRate           => GetFinalStat(StatType.CritRate);
        public float CritMultiplier     => GetFinalStat(StatType.CritMultiplier);
        public float MoveSpeed          => GetFinalStat(StatType.MoveSpeed);
        public float DashDistance       => GetFinalStat(StatType.DashDistance);
        public float MaxPoise           => GetFinalStat(StatType.MaxPoise);
        public float PoiseRecoveryRate  => GetFinalStat(StatType.PoiseRecoveryRate);
        public float PoiseRecoveryDelay => GetFinalStat(StatType.PoiseRecoveryDelay);
        public float SkillGaugeRate     => GetFinalStat(StatType.SkillGaugeRate);
        public float InvincibleDuration => GetFinalStat(StatType.InvincibleDuration);
        public float GatheringPower     => GetFinalStat(StatType.GatheringPower);

        // ── 초기화 ────────────────────────────────────────────────

        /// <summary>
        /// ActorStatSO로 기본값 전체 교체. SetDefinition 시점이나 PlayerActor.Awake에서 호출.
        /// </summary>
        public void Init(ActorStatSO statSO)
        {
            _baseStats.Clear();
            if (statSO != null)
            {
                foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
                    _baseStats[type] = statSO.GetBase(type);
            }
            else
            {
                foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
                    _baseStats[type] = ActorStatSO.GetDefault(type);
            }
            _modifiers.Clear();
            InvalidateCache();
            FireAllChanged();
        }

        /// <summary>
        /// 특정 스탯 기본값만 직접 설정. SO 없이 레거시 필드값 주입 시 사용.
        /// </summary>
        public void SetBase(StatType type, float value)
        {
            _baseStats[type] = value;
            InvalidateCache();
            OnStatChanged?.Invoke(type, GetFinalStat(type));
        }

        // ── 최종값 조회 ───────────────────────────────────────────

        /// <summary>
        /// 모든 수정자를 적용한 최종 스탯 값.
        /// </summary>
        public float GetFinalStat(StatType type)
        {
            if (_cacheDirty) RebuildCache();
            return _cachedFinals.TryGetValue(type, out float v) ? v : GetBase(type);
        }

        public float GetBase(StatType type)
            => _baseStats.TryGetValue(type, out float v) ? v : ActorStatSO.GetDefault(type);

        // ── 수정자 관리 ───────────────────────────────────────────

        public void AddModifier(StatModifier modifier)
        {
            _modifiers.Add(new TimedModifier(modifier));
            InvalidateCache();
            OnStatChanged?.Invoke(modifier.statType, GetFinalStat(modifier.statType));
        }

        /// <summary>
        /// source 오브젝트가 부착한 모든 수정자 제거. 장비 해제·버프 강제 만료 시 호출.
        /// </summary>
        public void RemoveModifiersBySource(object source)
        {
            if (source == null) return;

            HashSet<StatType> affected = null;
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                if (ReferenceEquals(_modifiers[i].Modifier.source, source))
                {
                    affected ??= new HashSet<StatType>();
                    affected.Add(_modifiers[i].Modifier.statType);
                    _modifiers.RemoveAt(i);
                }
            }

            if (affected == null) return;
            InvalidateCache();
            foreach (var type in affected)
                OnStatChanged?.Invoke(type, GetFinalStat(type));
        }

        public void RemoveAllModifiers()
        {
            if (_modifiers.Count == 0) return;
            _modifiers.Clear();
            InvalidateCache();
            FireAllChanged();
        }

        public int ModifierCount => _modifiers.Count;

        // ── 시간 제한 수정자 업데이트 ────────────────────────────

        private void Update()
        {
            if (_modifiers.Count == 0) return;

            HashSet<StatType> expired = null;
            for (int i = _modifiers.Count - 1; i >= 0; i--)
            {
                var tm = _modifiers[i];
                if (tm.Modifier.IsPermanent) continue;

                tm.RemainingTime -= Time.deltaTime;
                if (tm.RemainingTime <= 0f)
                {
                    expired ??= new HashSet<StatType>();
                    expired.Add(tm.Modifier.statType);
                    _modifiers.RemoveAt(i);
                }
            }

            if (expired == null) return;
            InvalidateCache();
            foreach (var type in expired)
                OnStatChanged?.Invoke(type, GetFinalStat(type));
        }

        // ── 내부 계산 ─────────────────────────────────────────────

        private void RebuildCache()
        {
            _cachedFinals.Clear();
            foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
                _cachedFinals[type] = ComputeFinal(type);
            _cacheDirty = false;
        }

        private float ComputeFinal(StatType type)
        {
            float flat     = 0f;
            float percent  = 0f;
            float multiply = 1f;

            for (int i = 0; i < _modifiers.Count; i++)
            {
                var m = _modifiers[i].Modifier;
                if (m.statType != type) continue;
                switch (m.modifierType)
                {
                    case ModifierType.Flat:     flat     += m.value; break;
                    case ModifierType.Percent:  percent  += m.value; break;
                    case ModifierType.Multiply: multiply *= m.value; break;
                }
            }

            return (GetBase(type) + flat) * (1f + percent) * multiply;
        }

        private void InvalidateCache() => _cacheDirty = true;

        private void FireAllChanged()
        {
            if (OnStatChanged == null) return;
            foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
                OnStatChanged.Invoke(type, GetFinalStat(type));
        }

        // ── 에디터 모니터 전용 ────────────────────────────────────
#if UNITY_EDITOR
        public IReadOnlyList<TimedModifier> EditorGetModifiers() => _modifiers;
#endif

        // ── 내부 헬퍼 ─────────────────────────────────────────────

        public class TimedModifier
        {
            public StatModifier Modifier;
            public float        RemainingTime;

            public TimedModifier(StatModifier m)
            {
                Modifier      = m;
                RemainingTime = m.duration;
            }
        }
    }
}
