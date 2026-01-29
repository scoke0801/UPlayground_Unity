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
        
        private WeaponType _leftWeaponType = WeaponType.NoWeapon;

        private WeaponType _rightWeaponType = WeaponType.NoWeapon;
        
        private ParentConstraint _leftConstraint = null;
        private ParentConstraint _rightConstraint = null;

        // 가지고 있는 무기
        private GameObject _currentRightWeaponObj = null;
        private GameObject _currentLeftWeaponObj = null;

        // 현재 장착 상태
        public bool IsRightWeaponEquipped { get; private set; }
        public bool IsLeftWeaponEquipped { get; private set; }
        
        // [TODO] 실제 Data로 가져올 수 있어야 하겠지만 우선은 단독 데이터로 관리하는 상태
        public WeaponData CurrentWeapon { get; private set; }

        public WeaponType GetLeftWeaponType() => _leftWeaponType;
        public WeaponType GetRightWeaponType() => _rightWeaponType;
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
                    constraint = _leftConstraint;
                    _currentLeftWeaponObj = newWeapon;
                    break;
                case EquipPosition.RightHand: 
                    constraint = _rightConstraint;
                    _currentRightWeaponObj = newWeapon;
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
            _rightWeaponType = type;
            switch (type)
            {
                case WeaponType.Sword: _rightConstraint = swordConstraint; break;
                case WeaponType.GreatSword: _rightConstraint = greatSwordRightConstraint; break;
                case WeaponType.Staff: _rightConstraint = staffRightConstraint; break;
                case WeaponType.Bow: _rightConstraint = bowRightConstraint; break;
            }
        }

        public void SetLeftWeaponType(WeaponType type)
        {
            switch (type)
            {
                case WeaponType.Shield: _leftConstraint = shieldLeftConstraint; break;
                case WeaponType.Arrow: _leftConstraint = arrowLeftConstraint; break;
                default:
                    _leftWeaponType = WeaponType.NoWeapon;
                    return;
            }

            _leftWeaponType = type;
        }
        // 애니메이션 이벤트 콜백
        private void OnEquipRightWeapon()
        {
            if (_rightConstraint == null)
            {
                return;
            }
            var rightHand = _rightConstraint.GetSource(0);
            var back = _rightConstraint.GetSource(1);
    
            if (IsRightWeaponEquipped)
            {
                // UnEquip - 등으로
                rightHand.weight = 0;
                back.weight = 1;
                
                IsRightWeaponEquipped = false;
            }
            else
            {
                // Equip - 손으로
                rightHand.weight = 1;
                back.weight = 0;

                IsRightWeaponEquipped = true;
            }
    
            // weight 수정 후 다시 설정
            _rightConstraint.SetSource(0, rightHand);
            _rightConstraint.SetSource(1, back);
        }
        // 애니메이션 이벤트 콜백
        private void OnEquipLeftWeapon()
        {           
            if (_leftConstraint == null)
            {
                return;
            }
            var rightHand = _leftConstraint.GetSource(0);
            var back = _leftConstraint.GetSource(1);
    
            if (IsLeftWeaponEquipped)
            {
                // UnEquip - 등으로
                rightHand.weight = 0;
                back.weight = 1;
                
                IsLeftWeaponEquipped = false;
            }
            else
            {
                // Equip - 손으로
                rightHand.weight = 1;
                back.weight = 0;

                IsLeftWeaponEquipped = true;
            }
    
            // weight 수정 후 다시 설정
            _leftConstraint.SetSource(0, rightHand);
            _leftConstraint.SetSource(1, back);
        }

        private void DestroyEquippedWeapon(EquipPosition equipPosition)
        {
            if (equipPosition == EquipPosition.LeftHand)
            {
                if (_currentLeftWeaponObj != null)
                {
                    Destroy(_currentLeftWeaponObj);
                    _currentLeftWeaponObj = null;
                }
            }
            else if (equipPosition == EquipPosition.RightHand)
            {
                if (_currentRightWeaponObj != null)
                {
                    Destroy(_currentRightWeaponObj);
                    _currentRightWeaponObj = null;
                }
            }
        }
    }

}