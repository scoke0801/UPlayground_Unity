using System;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Data;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;
using UPlayGround.UI;

namespace UPlayGround.Components
{
    public class MonsterBreakGauge : MonoBehaviour
    {
        private const float DefaultRepeatBreakCooldown = 5f;

        [HideInInspector, SerializeField] private MonsterBreakGaugeSO _data;

        private MonsterActor _owner;
        private UI_ActorHpBar _actorUIBar;
        private float _currentGauge;
        private float _exposedTimer;
        private float _repeatCooldownTimer;
        private bool _isExposed;
        private bool _hasBrokenOnce;

        public bool IsExposed => _isExposed;
        public bool IsRepeatCooldown => _repeatCooldownTimer > 0f;
        public bool UseBreakGauge => _data != null && _data.useBreakGauge;
        public float GaugePercent => MaxGauge > 0f ? _currentGauge / MaxGauge : 0f;
        public float RepeatCooldownPercent => RepeatCooldownDuration > 0f
            ? Mathf.Clamp01(_repeatCooldownTimer / RepeatCooldownDuration)
            : 0f;
        public float DamageTakenMultiplier => _isExposed && _data != null
            ? Mathf.Max(0f, _data.damageTakenMultiplierWhileExposed)
            : 1f;

        private float MaxGauge
        {
            get
            {
                if (_data == null) return 100f;
                MonsterActorGrade grade = _owner != null ? _owner.Grade : MonsterActorGrade.Normal;
                float gradeScale = _data.gradePolicy != null ? _data.gradePolicy.GetGaugeMultiplier(grade) : 1f;
                return Mathf.Max(1f, _data.maxGauge * gradeScale);
            }
        }

        private float RepeatCooldownDuration
        {
            get
            {
                if (_data == null) return DefaultRepeatBreakCooldown;
                return _data.repeatBreakCooldown > 0f
                    ? _data.repeatBreakCooldown
                    : DefaultRepeatBreakCooldown;
            }
        }

        public event Action<MonsterBreakGauge> OnBreakExposed;
        public event Action<MonsterBreakGauge> OnBreakRecovered;
        public event Action<float, float> OnGaugeChanged;

        private void Awake()
        {
            _owner = GetComponent<MonsterActor>();
            if (_owner?.Definition != null)
                Init(_owner.Definition);
            else if (_data != null)
                ResetGauge(MaxGauge);
            else
                RefreshUi();
        }

        private void Update()
        {
            if (_repeatCooldownTimer > 0f)
            {
                _repeatCooldownTimer -= Time.deltaTime;
                if (_repeatCooldownTimer <= 0f)
                {
                    _repeatCooldownTimer = 0f;
                    RefreshUi();
                }
                else
                {
                    UpdateUiAsCooldown();
                }
            }

            if (!_isExposed) return;

            _exposedTimer -= Time.deltaTime;
            UpdateUiAsTimer();

            if (_exposedTimer <= 0f)
                RecoverFromExpose(false);
        }

        public void Init(MonsterBreakGaugeSO data)
        {
            _data = data;
            ResetGauge(MaxGauge);
        }

        public void Init(ActorDefinitionSO definition)
        {
            if (definition?.EffectiveBreakGaugeData == null) return;
            Init(definition.EffectiveBreakGaugeData);
        }

        public void ConnectUiBar(UI_ActorHpBar actorUIBar)
        {
            _actorUIBar = actorUIBar;
            RefreshUi();
        }

        public float TakeBreakDamage(in HitContext hit)
        {
            if (!CanAccumulate()) return 0f;

            if (hit.ForceBreakExpose)
            {
                float before = _currentGauge;
                ForceExpose();
                return Mathf.Max(0f, before - _currentGauge);
            }

            float breakDamage = hit.BreakDamage;
            if (breakDamage <= 0f) return 0f;

            float finalBreakDamage = breakDamage * (1f - Mathf.Clamp01(_data.breakResist));
            float previousGauge = _currentGauge;
            _currentGauge = Mathf.Max(0f, _currentGauge - finalBreakDamage);
            RefreshUi();

            if (_currentGauge <= 0f)
                ForceExpose();

            return Mathf.Max(0f, previousGauge - _currentGauge);
        }

        public void ConsumeBySpecialAttack()
        {
            RecoverFromExpose(true);
        }

        public void ForceExpose()
        {
            if (!CanExpose()) return;

            _isExposed = true;
            _hasBrokenOnce = true;
            _currentGauge = 0f;
            _exposedTimer = Mathf.Max(0.1f, _data.exposedDuration);
            RefreshUi();
            OnBreakExposed?.Invoke(this);
        }

        public void RecoverFromExpose(bool consumed)
        {
            if (!_isExposed) return;

            _isExposed = false;
            float ratio = consumed
                ? _data.resetGaugeRatioOnSpecialAttack
                : _data.resetGaugeRatioOnExpire;
            _currentGauge = MaxGauge * (1f - Mathf.Clamp01(ratio));
            _repeatCooldownTimer = consumed ? RepeatCooldownDuration : 0f;
            RefreshUi();
            OnBreakRecovered?.Invoke(this);
        }

        private bool CanAccumulate()
        {
            if (_data == null || !_data.useBreakGauge) return false;
            if (_isExposed) return false;
            if (!_data.allowRepeatBreak && _hasBrokenOnce) return false;
            return _repeatCooldownTimer <= 0f;
        }

        private bool CanExpose()
        {
            if (_data == null || !_data.useBreakGauge) return false;
            if (_isExposed) return false;
            if (!_data.allowRepeatBreak && _hasBrokenOnce) return false;
            return _repeatCooldownTimer <= 0f;
        }

        private void ResetGauge(float value)
        {
            _currentGauge = Mathf.Clamp(value, 0f, MaxGauge);
            _isExposed = false;
            _exposedTimer = 0f;
            _repeatCooldownTimer = 0f;
            RefreshUi();
        }

        private void RefreshUi()
        {
            OnGaugeChanged?.Invoke(_currentGauge, MaxGauge);
            if (_actorUIBar != null)
            {
                _actorUIBar.UpdateBreakGauge(_currentGauge, MaxGauge);
                _actorUIBar.SetBreakGaugeEmptyUiActive(ShouldShowBreakGaugeEmptyUi());
            }
        }

        private void UpdateUiAsTimer()
        {
            float max = Mathf.Max(0.1f, _data != null ? _data.exposedDuration : 1f);
            float current = Mathf.Clamp(_exposedTimer, 0f, max);
            OnGaugeChanged?.Invoke(current, max);
            if (_actorUIBar != null)
            {
                // 브레이크 노출 중에는 누적 게이지가 이미 가득 찬 상태로 보여야 한다.
                // 같은 fill 이미지를 노출 타이머로 다시 쓰면 활성 이펙트와 게이지 표시 시점이 어긋난다.
                _actorUIBar.SetBreakGaugeEmptyUiActive(ShouldShowBreakGaugeEmptyUi());
            }
        }

        private void UpdateUiAsCooldown()
        {
            float max = Mathf.Max(0.1f, RepeatCooldownDuration);
            float current = Mathf.Clamp(_repeatCooldownTimer, 0f, max);
            OnGaugeChanged?.Invoke(current, max);
            if (_actorUIBar != null)
            {
                _actorUIBar.UpdateBreakGauge(_currentGauge, MaxGauge);
                _actorUIBar.SetBreakGaugeEmptyUiActive(ShouldShowBreakGaugeEmptyUi());
            }
        }

        private bool ShouldShowBreakGaugeEmptyUi()
        {
            return _data != null && _data.useBreakGauge && (_isExposed || _currentGauge <= 0f);
        }
    }
}
