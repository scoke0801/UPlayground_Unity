using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Enum;

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

        
        // 현재 장착 상태
        public bool IsRightWeaponEquipped { get; private set; }
        public bool IsLeftWeaponEquipped { get; private set; }
        public WeaponData CurrentWeapon { get; private set; }

        private WeaponType _leftWeaponType = WeaponType.Shield;

        private WeaponType _rightWeaponType = WeaponType.Sword;
        
        private ParentConstraint _leftConstraint;
        private ParentConstraint _rightConstraint;

        /// <summary>
        /// 특정 무기 장착 (아이템 시스템 연동)
        /// </summary>
        public void EquipWeapon(string itemKey)
        {
            GameObject newWeapon = GameObjectManager.Instance.CreateWeapon(itemKey);

            if (newWeapon != null)
            {
                // 1. 부모 설정: swordConstraint가 붙은 오브젝트의 자식으로 설정
                newWeapon.transform.SetParent(greatSwordRightConstraint.transform, false);

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
        
    }

}