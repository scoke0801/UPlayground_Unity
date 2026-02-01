using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Enum;
using UPlayGround.Data.Event;
using UPlayGround.Manager;

namespace UPlayGround.GameActor.Component
{

    /// <summary>
    /// 플레이어의 장비 착용/해제를 관리
    /// State는 "언제" 장비를 착용할지 결정하고
    /// Component는 "어떻게" 장비를 착용하는지 처리
    /// </summary>
    public class PlayerEquipment : PlayerActorComponent
    {
        [Header("Sword")]
        [SerializeField]  private ParentConstraint swordConstraint;
        
        [Header("Shield")]
        [SerializeField]  private ParentConstraint shieldLeftConstraint;
        
        [Header("GreatSword")]
        [SerializeField]  private ParentConstraint greatSwordRightConstraint;

        [Header("Staff")]
        [SerializeField]  private ParentConstraint staffRightConstraint;

        [Header("Bow")]
        [SerializeField]  private ParentConstraint bowRightConstraint;
        
        [Header("Arrow")]
        [SerializeField]  private ParentConstraint arrowLeftConstraint;
        
        private WeaponType _subWeaponType = WeaponType.NoWeapon;
        private WeaponType _mainWeaponType = WeaponType.NoWeapon;
        
        private ParentConstraint _subWeaponConstraint = null;
        private ParentConstraint _mainWeaponConstraint = null;

        // 가지고 있는 무기
        private GameObject _currentMainWeaponObj = null;
        private GameObject _currentSubWeaponObj = null;

        // 현재 장착 상태
        public bool IsMainWeaponEquipped { get; private set; }
        public bool IsSubWeaponEquipped { get; private set; }
        
        // [TODO] 실제 Data로 가져올 수 있어야 하겠지만 우선은 단독 데이터로 관리하는 상태
        public WeaponData CurrentWeapon { get; private set; }

        public WeaponType GetSubWeaponType() => _subWeaponType;
        public WeaponType GetMainWeaponType() => _mainWeaponType;
        private void Start()
        {
            EventManager.Instance.Subscribe<PlayerEvent, PlayerEquipChangeEvent>(
                PlayerEvent.ChangeWeapon, 
                OnWeaponChanged
            );
        }

        private void OnDestroy()
        {
            if (EventManager.Instance == null)
                return;
            
            EventManager.Instance.Unsubscribe<PlayerEvent, PlayerEquipChangeEvent>(
                PlayerEvent.ChangeWeapon, 
                OnWeaponChanged
            );
        }

        private void OnWeaponChanged(PlayerEquipChangeEvent data)
        {
            if (data == null)
            {
                return;
            }

            if (data.equipPosition == EquipPosition.LeftHand)
            {
                SetLeftWeaponType(data.weaponType);
            }
            else if (data.equipPosition == EquipPosition.RightHand)
            {
                SetRightWeaponType(data.weaponType);
            }
            EquipWeapon(data.weaponKey, data.equipPosition);
        }

        /// <summary>
        /// 특정 무기 장착 (아이템 시스템 연동)
        /// </summary>
        public void EquipWeapon(string itemKey, EquipPosition equipPosition)
        {
            DestroyEquippedWeapon(equipPosition);
            
            GameObject newWeapon = GameObjectManager.Instance.CreateWeapon(itemKey);

            ParentConstraint constraint = null;
            switch (equipPosition)
            {
                case EquipPosition.LeftHand: 
                    constraint = _subWeaponConstraint;
                    _currentSubWeaponObj = newWeapon;
                    break;
                case EquipPosition.RightHand: 
                    constraint = _mainWeaponConstraint;
                    _currentMainWeaponObj = newWeapon;
                    break;
                default: return;
            }
            
            if (newWeapon != null)
            {
                // 1. 부모 설정: swordConstraint가 붙은 오브젝트의 자식으로 설정
                newWeapon.transform.SetParent(constraint.transform, false);

                // 2. 위치 및 회전 초기화: 부모 오브젝트(Sword)의 위치에 딱 맞게 정렬
                newWeapon.transform.localPosition = Vector3.zero;
                //newWeapon.transform.localRotation = Quaternion.identity;
                //newWeapon.transform.localScale = Vector3.one; // 크기도 1,1,1로 초기화 (필요시)
            }
        }
        public void SetRightWeaponType(WeaponType type)
        {
            _mainWeaponType = type;
            switch (type)
            {
                case WeaponType.Sword: _mainWeaponConstraint = swordConstraint; break;
                case WeaponType.GreatSword: _mainWeaponConstraint = greatSwordRightConstraint; break;
                case WeaponType.Staff: _mainWeaponConstraint = staffRightConstraint; break;
                case WeaponType.Bow: _mainWeaponConstraint = bowRightConstraint; break;
            }
        }

        public void SetLeftWeaponType(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Shield: _subWeaponConstraint = shieldLeftConstraint; break;
                case WeaponType.Arrow: _subWeaponConstraint = arrowLeftConstraint; break;
                default:
                    _subWeaponType = WeaponType.NoWeapon;
                    return;
            }

            _subWeaponType = type;
        }
        // 애니메이션 이벤트 콜백
        private void OnEquipRightWeapon()
        {
            if (_mainWeaponConstraint == null)
            {
                return;
            }
            var rightHand = _mainWeaponConstraint.GetSource(0);
            var back = _mainWeaponConstraint.GetSource(1);
    
            if (IsMainWeaponEquipped)
            {
                // UnEquip - 등으로
                rightHand.weight = 0;
                back.weight = 1;
                
                IsMainWeaponEquipped = false;
            }
            else
            {
                // Equip - 손으로
                rightHand.weight = 1;
                back.weight = 0;

                IsMainWeaponEquipped = true;
            }
    
            // weight 수정 후 다시 설정
            _mainWeaponConstraint.SetSource(0, rightHand);
            _mainWeaponConstraint.SetSource(1, back);
        }
        // 애니메이션 이벤트 콜백
        private void OnEquipLeftWeapon()
        {           
            if (_subWeaponConstraint == null)
            {
                return;
            }
            var rightHand = _subWeaponConstraint.GetSource(0);
            var back = _subWeaponConstraint.GetSource(1);
    
            if (IsSubWeaponEquipped)
            {
                // UnEquip - 등으로
                rightHand.weight = 0;
                back.weight = 1;
                
                IsSubWeaponEquipped = false;
            }
            else
            {
                // Equip - 손으로
                rightHand.weight = 1;
                back.weight = 0;

                IsSubWeaponEquipped = true;
            }
    
            // weight 수정 후 다시 설정
            _subWeaponConstraint.SetSource(0, rightHand);
            _subWeaponConstraint.SetSource(1, back);
        }

        private void DestroyEquippedWeapon(EquipPosition equipPosition)
        {
            if (equipPosition == EquipPosition.LeftHand)
            {
                if (_currentSubWeaponObj != null)
                {
                    Destroy(_currentSubWeaponObj);
                    _currentSubWeaponObj = null;
                }
            }
            else if (equipPosition == EquipPosition.RightHand)
            {
                if (_currentMainWeaponObj != null)
                {
                    Destroy(_currentMainWeaponObj);
                    _currentMainWeaponObj = null;
                }
            }
        }
    }

}