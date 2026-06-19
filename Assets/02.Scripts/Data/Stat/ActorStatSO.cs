using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Data.Stat
{
    /// <summary>
    /// 액터 한 종류의 기본 스탯 값을 정의하는 ScriptableObject.
    /// ActorDefinitionSO.statData로 참조하거나, PlayerActor에 직접 주입한다.
    /// </summary>
    [CreateAssetMenu(fileName = "ActorStat_", menuName = "UPlayGround/스탯/Actor Stat")]
    public class ActorStatSO : ScriptableObject
    {
        [Serializable]
        public struct StatEntry
        {
            public StatType statType;
            public float    baseValue;
        }

        [SerializeField] private List<StatEntry> _stats = new();

        /// <summary>
        /// 정의되지 않은 스탯 조회 시 사용되는 폴백 값.
        /// StatType 추가 시 여기도 함께 업데이트할 것.
        /// </summary>
        private static readonly Dictionary<StatType, float> _defaults = new()
        {
            { StatType.MaxHealth,          100f },
            { StatType.HealthRegenRate,    0f   },
            { StatType.AttackPower,        1.0f },
            { StatType.Defense,            0.0f },
            { StatType.CritRate,           0.0f },
            { StatType.CritMultiplier,     1.5f },
            { StatType.MoveSpeed,          1.0f },
            { StatType.DashDistance,       1.0f },
            { StatType.MaxPoise,           100f },
            { StatType.PoiseRecoveryRate,  40f  },
            { StatType.PoiseRecoveryDelay, 2.0f },
            { StatType.SkillGaugeRate,     1.0f },
            { StatType.InvincibleDuration, 1.0f },
        };

        public IReadOnlyList<StatEntry> Entries => _stats;

        /// <summary>
        /// StatType의 기본값을 조회. 등록된 항목이 없으면 _defaults를 반환한다.
        /// </summary>
        public float GetBase(StatType type)
        {
            for (int i = 0; i < _stats.Count; i++)
                if (_stats[i].statType == type) return _stats[i].baseValue;

            return _defaults.TryGetValue(type, out float def) ? def : 0f;
        }

        /// <summary>
        /// StatType의 기본값을 가져오되 명시적으로 등록되었는지도 반환.
        /// 에디터에서 "누락" 표시에 사용.
        /// </summary>
        public bool TryGetExplicit(StatType type, out float value)
        {
            for (int i = 0; i < _stats.Count; i++)
            {
                if (_stats[i].statType == type)
                {
                    value = _stats[i].baseValue;
                    return true;
                }
            }
            value = _defaults.TryGetValue(type, out float def) ? def : 0f;
            return false;
        }

        public static float GetDefault(StatType type)
            => _defaults.TryGetValue(type, out float def) ? def : 0f;

#if UNITY_EDITOR
        /// <summary>에디터 전용: StatType 순서대로 정렬해 가독성 향상.</summary>
        private void OnValidate()
        {
            _stats.Sort((a, b) => a.statType.CompareTo(b.statType));
        }

        /// <summary>에디터 전용: 스탯 항목을 안전하게 설정한다.</summary>
        public void EditorSet(StatType type, float value)
        {
            for (int i = 0; i < _stats.Count; i++)
            {
                if (_stats[i].statType == type)
                {
                    var entry = _stats[i];
                    entry.baseValue = value;
                    _stats[i] = entry;
                    return;
                }
            }
            _stats.Add(new StatEntry { statType = type, baseValue = value });
        }

        /// <summary>에디터 전용: 항목 제거.</summary>
        public void EditorRemove(StatType type)
        {
            _stats.RemoveAll(e => e.statType == type);
        }

        /// <summary>에디터 전용: 누락된 모든 StatType을 기본값으로 채운다.</summary>
        public void EditorFillMissing()
        {
            foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
            {
                if (!TryGetExplicit(type, out _))
                    _stats.Add(new StatEntry { statType = type, baseValue = GetDefault(type) });
            }
            _stats.Sort((a, b) => a.statType.CompareTo(b.statType));
        }
#endif
    }
}
