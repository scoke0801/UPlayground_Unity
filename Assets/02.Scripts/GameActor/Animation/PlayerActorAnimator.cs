using Animancer;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.Enum;
using UPlayGround.Component;

namespace UPlayGround.Animation
{
    public partial class PlayerActorAnimator : ActorAnimator
    {
        // 무기는 오른손에 장착된 무기를 기준으로 한다?
        [Header("PlayerActor Only")]
        [SerializeField] private PlayerActorAnimationMotionSet _playerActorAnimationMotionSet;

        private PlayerActor _playerActor;
        private PlayerEquipment _playerEquipment;
        private PlayerCombat _playerCombat;

        public bool IsOpenedComboWindow { get; set; } = false;

        public override void Init(GameActor actor)
        {
            base.Init(actor);
            
            _playerActor = actor as PlayerActor;
            if (_playerActor != null)
            {
                _playerEquipment = _playerActor.GetPlayerEquipment();
                _playerCombat = _playerActor.GetCombat();
            }
        }

        public override bool HasMotion(AnimKey key, bool checkWeapon = false)
        {
            if ( _playerActorAnimationMotionSet == null)
            {
                return false;
            }

            if (checkWeapon == true)
            {
                return (_playerActorAnimationMotionSet.GetMotionSet(WeaponType.NoWeapon, key) != null);
            }
            return _playerActorAnimationMotionSet.GetMotionSet(_playerEquipment.GetMainWeaponType(), key) != null;
        }
        public override AnimancerState PlayMotion(AnimKey key, float fadeDuration = 0.0f)
        {
            _currentMotionSet = _playerActorAnimationMotionSet.GetMotionSet(_playerEquipment.GetMainWeaponType(), key);
            if (_currentMotionSet == null || _currentMotionSet.IsValid() == false)
            {
                return null;
            }
            
            _currentMotionIndex = 0;
            _globalTime = 0f;
            
            // 이벤트 실행기 초기화
            _eventExecutor?.PlayMotionSet(_currentMotionSet);
            
            // 첫 번째 모션 재생
            PlayMotionAtIndex(0, fadeDuration);

            return _currentState;
        }
        
        /// <summary>
        /// MotionSet의 총 재생 시간 가져오기
        /// </summary>
        public override float GetMotionSetDuration(AnimKey key)
        {
            var motionSet = _playerActorAnimationMotionSet.GetMotionSet(_playerEquipment.GetMainWeaponType(), key);
            return motionSet?.TotalDuration ?? 0f;
        }
    }

    public partial class PlayerActorAnimator : ActorAnimator
    {
        private void OnAnimationEvent_OpenComboWindow()
        {
            IsOpenedComboWindow = true;
        }
        
        /// <summary>
        /// 애니메이션 이벤트: 히트 판정 실행
        /// 각 공격 애니메이션의 무기가 적에게 닿는 프레임에 호출됨
        /// </summary>
        private void OnAnimationEvent_HitCheck()
        {
            if (_playerCombat == null)
            {
                Debug.LogWarning("[PlayerActorAnimator] PlayerCombat이 없습니다!");
                return;
            }
            
            _playerCombat.PerformHitDetection();
            
            Debug.Log($"[PlayerActorAnimator] 히트 판정 실행: {Time.frameCount} 프레임");
        }
    }
}