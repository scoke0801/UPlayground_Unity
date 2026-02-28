using UnityEngine;
using UPlayGround.Data.Enemy;

namespace UPlayGround.Component
{
    /// <summary>
    /// Poise(강인도) 런타임 컴포넌트.
    /// 수치 설정은 PoiseSO로 분리. Init()으로 주입받거나 Inspector에서 직접 할당 가능.
    /// </summary>
    public class PoiseStat : MonoBehaviour
    {
        [SerializeField] private PoiseSO _data;

        private float _currentPoise;
        private float _recoveryTimer;
        private bool  _isBroken;
        private bool  _isHyperArmorActive;

        public bool  IsHyperArmorActive => _isHyperArmorActive;
        public float PoisePercent       => _data != null ? _currentPoise / _data.maxPoise : 1f;

        private void Awake() => InitFromData();

        /// <summary> MonsterActor.Init() 등에서 SO를 주입할 때 사용 </summary>
        public void Init(PoiseSO data)
        {
            _data = data;
            InitFromData();
        }

        private void InitFromData()
        {
            _currentPoise  = _data != null ? _data.maxPoise : 100f;
            _isBroken      = false;
            _recoveryTimer = 0f;
        }

        private void Update()
        {
            if (_data == null) return;

            if (_isBroken)
            {
                _recoveryTimer += Time.deltaTime;
                if (_recoveryTimer >= _data.recoveryDelay)
                {
                    _isBroken      = false;
                    _currentPoise  = _data.maxPoise;
                    _recoveryTimer = 0f;
                }
                return;
            }

            if (_currentPoise < _data.maxPoise)
            {
                _recoveryTimer += Time.deltaTime;
                if (_recoveryTimer >= _data.recoveryDelay)
                    _currentPoise = Mathf.Min(_currentPoise + _data.recoveryRate * Time.deltaTime, _data.maxPoise);
            }
        }

        /// <summary>
        /// 피격 시 호출.
        /// true → Poise Break, Hit State 진입 필요.
        /// false → Poise로 버팀, Hit State 불필요.
        /// poiseDamage == 0 → 무조건 true (Poise 무시 공격).
        /// </summary>
        public bool TakePoiseDamage(float poiseDamage)
        {
            if (poiseDamage <= 0f) return true;
            if (_isHyperArmorActive) return false;
            if (_isBroken) return false;

            _currentPoise -= poiseDamage;
            _recoveryTimer = 0f;

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

        public void ForcePoisBreak()
        {
            _currentPoise  = 0f;
            _isBroken      = true;
            _recoveryTimer = 0f;
        }
    }
}
