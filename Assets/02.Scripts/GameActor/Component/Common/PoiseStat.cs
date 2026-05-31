using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Enemy;

namespace UPlayGround.Component
{
    /// <summary>
    /// Poise(강인도) 런타임 컴포넌트.
    /// 수치 설정은 PoiseSO로 분리. Init()으로 주입받거나 Inspector에서 직접 할당 가능.
    /// </summary>
    public class PoiseStat : MonoBehaviour
    {
        [HideInInspector, SerializeField] private PoiseSO _data;

        private float _currentPoise;
        private float _recoveryTimer;
        private bool _isBroken = false;
        private bool  _isHyperArmorActive;
        private UI_ActorHpBar _actorUIBar;
        
        public bool  IsHyperArmorActive => _isHyperArmorActive;
        public float PoisePercent       => _data != null ? _currentPoise / _data.maxPoise : 1f;

        public bool IsPoiseBroken => _isBroken;
        public float CurrentPoise => _currentPoise;
        public float MaxPoise => _data != null ? _data.maxPoise : 100f;
        
        private void Awake()
        {
            var actor = GetComponent<GameActor>();
            if (actor?.Definition != null)
                Init(actor.Definition);
            else
                InitFromData();
        }

        /// <summary> MonsterActor.Init() 등에서 SO를 주입할 때 사용 </summary>
        public void Init(PoiseSO data)
        {
            _data = data;
            InitFromData();
        }

        public void Init(ActorDefinitionSO definition)
        {
            if (definition?.poiseData == null) return;
            Init(definition.poiseData);
        }

        private void InitFromData()
        {
            _currentPoise  = _data != null ? _data.maxPoise : 100f;
            _isBroken      = false;
            _recoveryTimer = 0f;
        }

        public void ConnectUiBar(UI_ActorHpBar actorUIBar)
        {
            _actorUIBar = actorUIBar;
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
                    
                    
                    if(_actorUIBar != null)
                        _actorUIBar.UpdatePoise(_currentPoise, MaxPoise);
                }
                return;
            }

            if (_currentPoise < _data.maxPoise)
            {
                _recoveryTimer += Time.deltaTime;
                if (_recoveryTimer >= _data.recoveryDelay)
                {
                    _currentPoise = Mathf.Min(_currentPoise + _data.recoveryRate * Time.deltaTime, _data.maxPoise);
                    
                    if(_actorUIBar != null)
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
            
            if(_actorUIBar != null)
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
            
            if(_actorUIBar != null)
                _actorUIBar.UpdatePoise(_currentPoise, MaxPoise);
        }
    }
}
