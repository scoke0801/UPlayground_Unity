using System;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Enemy;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Component
{
    public class MonsterBreakGauge : MonoBehaviour
    {
        [SerializeField] private MonsterBreakGaugeSO _data;

        private MonsterActor _owner;
        private UI_ActorHpBar _actorUIBar;
        private float _currentGauge;
        private float _exposedTimer;
        private float _repeatCooldownTimer;
        private bool _isExposed;
        private bool _hasBrokenOnce;

        public bool IsExposed => _isExposed;
        public bool UseBreakGauge => _data != null && _data.useBreakGauge;
        public float GaugePercent => MaxGauge > 0f ? _currentGauge / MaxGauge : 0f;
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

        public event Action<MonsterBreakGauge> OnBreakExposed;
        public event Action<MonsterBreakGauge> OnBreakRecovered;
        public event Action<float, float> OnGaugeChanged;

        private void Awake()
        {
            _owner = GetComponent<MonsterActor>();
            RefreshUi();
        }

        private void Update()
        {
            if (_repeatCooldownTimer > 0f)
                _repeatCooldownTimer -= Time.deltaTime;

            if (!_isExposed) return;

            _exposedTimer -= Time.deltaTime;
            UpdateUiAsTimer();

            if (_exposedTimer <= 0f)
                RecoverFromExpose(false);
        }

        public void Init(MonsterBreakGaugeSO data)
        {
            _data = data;
            ResetGauge(0f);
        }

        public void ConnectUiBar(UI_ActorHpBar actorUIBar)
        {
            _actorUIBar = actorUIBar;
            RefreshUi();
        }

        public void TakeBreakDamage(AttackData attackData)
        {
            if (!CanAccumulate()) return;

            if (attackData != null && attackData.forceBreakExpose)
            {
                ForceExpose();
                return;
            }

            float breakDamage = attackData?.breakDamage ?? 0f;
            if (breakDamage <= 0f) return;

            float finalBreakDamage = breakDamage * (1f - Mathf.Clamp01(_data.breakResist));
            _currentGauge = Mathf.Min(MaxGauge, _currentGauge + finalBreakDamage);
            RefreshUi();

            if (_currentGauge >= MaxGauge)
                ForceExpose();
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
            _currentGauge = MaxGauge;
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
            _currentGauge = MaxGauge * Mathf.Clamp01(ratio);
            _repeatCooldownTimer = Mathf.Max(0f, _data.repeatBreakCooldown);
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
                _actorUIBar.UpdateBreakGauge(_currentGauge, MaxGauge);
        }

        private void UpdateUiAsTimer()
        {
            float max = Mathf.Max(0.1f, _data != null ? _data.exposedDuration : 1f);
            float current = Mathf.Clamp(_exposedTimer, 0f, max);
            OnGaugeChanged?.Invoke(current, max);
            if (_actorUIBar != null)
                _actorUIBar.UpdateBreakGauge(current, max);
        }
    }
}
