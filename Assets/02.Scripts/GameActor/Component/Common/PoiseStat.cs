using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.Stat;
using UPlayGround.UI;

namespace UPlayGround.Components
{
    /// <summary>
    /// Poise(강인도) 런타임 컴포넌트.
    /// 수치(MaxPoise / 회복률 / 회복지연)는 <see cref="ActorStatContainer"/>(statData)를 단일 소스로 읽는다.
    /// 등급/레벨 배율은 statData 생성 시점에 이미 반영되므로(MonsterScalingSO), 여기서는 별도 스케일링을 하지 않는다.
    /// PoiseSO(_data)는 더 이상 수치 소스가 아니며 hasHyperArmor 플래그 전달용으로만 유지한다.
    /// </summary>
    public class PoiseStat : MonoBehaviour
    {
        // 하이퍼아머 플래그 전용. maxPoise/recovery* 수치는 런타임에서 읽지 않는다(statData 단일 소스).
        [HideInInspector, SerializeField] private PoiseSO _data;

        private const float FallbackMaxPoise      = 100f;
        private const float FallbackRecoveryRate  = 40f;
        private const float FallbackRecoveryDelay = 2f;

        private ActorStatContainer _stats;
        private float _currentPoise;
        private float _recoveryTimer;
        private bool  _isBroken = false;
        private bool  _isHyperArmorActive;
        private UI_ActorHpBar _actorUIBar;
        private float _lastMaxPoise;

        public bool  IsHyperArmorActive => _isHyperArmorActive;
        public float PoisePercent       => MaxPoise > 0f ? _currentPoise / MaxPoise : 1f;

        public bool  IsPoiseBroken => _isBroken;
        public float CurrentPoise  => _currentPoise;

        private ActorStatContainer Stats => _stats != null ? _stats : (_stats = GetComponent<ActorStatContainer>());

        public  float MaxPoise      => Stats != null ? Mathf.Max(1f, Stats.MaxPoise) : FallbackMaxPoise;
        private float RecoveryRate  => Stats != null ? Stats.PoiseRecoveryRate  : FallbackRecoveryRate;
        private float RecoveryDelay => Stats != null ? Stats.PoiseRecoveryDelay : FallbackRecoveryDelay;

        private void Awake()
        {
            // statData 주입 전이라도 합리적 기본값으로 시작. 권위 초기화는 Init()이 담당.
            BindStats();
            InitFromStats();
        }

        private void OnEnable()
        {
            BindStats();
        }

        private void OnDisable()
        {
            if (_stats != null)
                _stats.OnStatChanged -= OnStatChanged;
        }

        /// <summary> 레거시/에디터 호환용. 하이퍼아머 플래그만 받고 수치는 statData에서 읽는다. </summary>
        public void Init(PoiseSO data)
        {
            if (data != null) _data = data;
            InitFromStats();
        }

        /// <summary> MonsterActor.ApplyDefinitionData에서 statData 주입(Stats.Init) 직후 호출하는 권위 초기화. </summary>
        public void Init(ActorDefinitionSO definition)
        {
            // hasHyperArmor 단일 읽기 경로(_data). 정의에 PoiseSO가 있으면 그것을, 없으면 프리팹 기본값을 유지.
            if (definition?.poiseData != null)
                _data = definition.poiseData;
            InitFromStats();
        }

        private void InitFromStats()
        {
            BindStats();
            _currentPoise  = MaxPoise;
            _lastMaxPoise  = MaxPoise;
            _isBroken      = false;
            _recoveryTimer = 0f;
        }

        private void BindStats()
        {
            ActorStatContainer stats = Stats;
            if (stats == null) return;

            stats.OnStatChanged -= OnStatChanged;
            stats.OnStatChanged += OnStatChanged;
            _lastMaxPoise = MaxPoise;
        }

        private void OnStatChanged(StatType type, float newValue)
        {
            if (type != StatType.MaxPoise) return;

            float oldMax = Mathf.Max(1f, _lastMaxPoise);
            float newMax = Mathf.Max(1f, newValue);
            float ratio = oldMax > 0f ? Mathf.Clamp01(_currentPoise / oldMax) : 1f;

            _currentPoise = _isBroken ? 0f : Mathf.Clamp(newMax * ratio, 0f, newMax);
            _lastMaxPoise = newMax;

            if (_actorUIBar != null)
                _actorUIBar.UpdatePoise(_currentPoise, MaxPoise);
        }

        public void ConnectUiBar(UI_ActorHpBar actorUIBar)
        {
            _actorUIBar = actorUIBar;
        }

        private void Update()
        {
            if (_isBroken)
            {
                _recoveryTimer += Time.deltaTime;
                if (_recoveryTimer >= RecoveryDelay)
                {
                    _isBroken      = false;
                    _currentPoise  = MaxPoise;
                    _recoveryTimer = 0f;

                    if (_actorUIBar != null)
                        _actorUIBar.UpdatePoise(_currentPoise, MaxPoise);
                }
                return;
            }

            if (_currentPoise < MaxPoise)
            {
                _recoveryTimer += Time.deltaTime;
                if (_recoveryTimer >= RecoveryDelay)
                {
                    _currentPoise = Mathf.Min(_currentPoise + RecoveryRate * Time.deltaTime, MaxPoise);

                    if (_actorUIBar != null)
                        _actorUIBar.UpdatePoise(_currentPoise, MaxPoise);
                }
            }
        }

        /// <summary>
        /// 피격 시 호출.
        /// true → 이번 피격으로 Poise Break 발생.
        /// false → Poise가 남아 있거나 이미 Broken 상태.
        /// Hit Reaction 재생 여부는 상태별 피격 허용 정책에서 별도로 결정한다.
        /// </summary>
        public bool TakePoiseDamage(float poiseDamage)
        {
            if (poiseDamage <= 0f) return false;
            if (_isBroken) return false;

            _currentPoise -= poiseDamage;
            _recoveryTimer = 0f;

            if (_actorUIBar != null)
                _actorUIBar.UpdatePoise(_currentPoise, MaxPoise);

            if (_currentPoise <= 0f)
            {
                _currentPoise  = 0f;
                _isBroken      = true;
                _recoveryTimer = 0f;
                return true;
            }
            return false;
        }

        public void SetHyperArmor(bool active)
        {
            _isHyperArmorActive = (_data?.hasHyperArmor ?? false) && active;
        }

        public void ForcePoiseBreak()
        {
            _currentPoise  = 0f;
            _isBroken      = true;
            _recoveryTimer = 0f;

            if (_actorUIBar != null)
                _actorUIBar.UpdatePoise(_currentPoise, MaxPoise);
        }

        /// <summary>Poise를 최대치로 즉시 회복하고 브레이크 상태를 해제한다(레벨업 풀 회복 등).</summary>
        public void RecoverFull()
        {
            _isBroken      = false;
            _currentPoise  = MaxPoise;
            _lastMaxPoise  = MaxPoise;
            _recoveryTimer = 0f;

            if (_actorUIBar != null)
                _actorUIBar.UpdatePoise(_currentPoise, MaxPoise);
        }
    }
}
