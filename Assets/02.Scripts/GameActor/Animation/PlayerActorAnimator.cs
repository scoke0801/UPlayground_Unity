using Animancer;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Component;
using UPlayGround.Manager;

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
        private WeaponType _lastPlayedWeaponType = WeaponType.NoWeapon;

        public bool IsOpenedComboWindow { get; set; } = false;
        public PlayerActorAnimationMotionSet PlayerMotionSet => _playerActorAnimationMotionSet;

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
                return _playerActorAnimationMotionSet.GetMotionSet(_playerEquipment.GetMainWeaponType(), key) != null;
            }

            return (_playerActorAnimationMotionSet.GetMotionSet(WeaponType.NoWeapon, key) != null);
        }
        public override AnimancerState PlayMotion(AnimKey key, float fadeDuration = 0.0f, int layerIndex = 0)
        {
            WeaponType activeWeaponType = GetActiveWeaponTypeForMotion(key);
            if (_isPlayingMotionSet
                && _lastPlayedKey == key
                && _lastPlayedWeaponType == activeWeaponType)
            {
                return _currentState;
            }

            MotionSet nextMotionSet = _playerActorAnimationMotionSet != null
                ? _playerActorAnimationMotionSet.GetMotionSet(activeWeaponType, key)
                : null;
            if (nextMotionSet == null || nextMotionSet.IsValid() == false)
            {
                return null;
            }

            // 기존 MotionSet이 재생 중이었다면 안전하게 정리
            if (_isPlayingMotionSet && _currentMotionSet != null)
            {
                StopMotionSet();
            }

            _currentMotionSet = nextMotionSet;

            _currentMotionIndex = 0;
            _globalTime = 0f;
            _isPlayingMotionSet = true;
            _lastPlayedKey = key;
            _lastPlayedWeaponType = activeWeaponType;

            // 이벤트 실행기 초기화
            _eventExecutor?.PlayMotionSet(_currentMotionSet);

            // 첫 번째 모션 재생
            PlayMotionAtIndex(0, fadeDuration, layerIndex);

            PlaySubAnimatorMotion(key, fadeDuration, layerIndex);

            return _currentState;
        }

        /// <summary>
        /// MotionSet의 총 재생 시간 가져오기
        /// </summary>
        public override float GetMotionSetDuration(AnimKey key)
        {
            var motionSet = _playerActorAnimationMotionSet.GetMotionSet(GetActiveWeaponTypeForMotion(key), key);
            return motionSet?.TotalDuration ?? 0f;
        }

        // 손에 들고 있을 때만 무기 모션셋, 등에 멘 상태에서는 NoWeapon 모션셋을 사용한다.
        // 단 발도/납도 및 전투 모션은 무기 정의 기준이라 WeaponType 그대로 사용.
        private WeaponType GetActiveWeaponTypeForMotion(AnimKey key)
        {
            if (_playerEquipment == null)
                return WeaponType.NoWeapon;

            if (key == AnimKey.Equip_Weapon || key == AnimKey.UnEquip_Weapon || key == AnimKey.Equip_LeftWeapon)
                return _playerEquipment.GetMainWeaponType();

            // 전투 모션은 무기를 든 상태 전제 → IsMainWeaponEquipped 무관하게 무기 타입 사용
            if (IsCombatMotion(key))
                return _playerEquipment.GetMainWeaponType();

            return _playerEquipment.IsMainWeaponEquipped
                ? _playerEquipment.GetMainWeaponType()
                : WeaponType.NoWeapon;
        }

        private static bool IsCombatMotion(AnimKey key)
        {
            int v = (int)key;
            return v >= (int)AnimKey.Attack_1 && v <= (int)AnimKey.FinishAttack;
        }
    }

    public partial class PlayerActorAnimator : ActorAnimator
    {
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
