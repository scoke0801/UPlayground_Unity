using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;
using UPlayGround.Data.Actor;

namespace UPlayGround.GameActor.Component
{

    /// <summary>
    /// 플레이어의 장비 착용/해제를 관리
    /// State는 "언제" 장비를 착용할지 결정하고
    /// Component는 "어떻게" 장비를 착용하는지 처리
    /// </summary>
    public class PlayerEquipment : PlayerActorComponent
    {
        [Header("References")] 
        [SerializeField]  private ParentConstraint rightConstraint;
        [SerializeField]  private ParentConstraint LeftConstraint;

        // 현재 장착 상태
        public bool IsRightWeaponEquipped { get; private set; }
        public bool IsLeftWeaponEquipped { get; private set; }
        public WeaponData CurrentWeapon { get; private set; }
        
        // 애니메이션 이벤트 콜백
        private void OnEquipRightWeapon()
        {
            var rightHand = rightConstraint.GetSource(0);
            var back = rightConstraint.GetSource(1);
    
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
            rightConstraint.SetSource(0, rightHand);
            rightConstraint.SetSource(1, back);
        }
        // 애니메이션 이벤트 콜백
        private void OnEquipLeftWeapon()
        {
            var rightHand = LeftConstraint.GetSource(0);
            var back = LeftConstraint.GetSource(1);
    
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
            LeftConstraint.SetSource(0, rightHand);
            LeftConstraint.SetSource(1, back);
        }
        
        /// <summary>
        /// 특정 무기 장착 (아이템 시스템 연동)
        /// </summary>
        public void EquipWeapon(WeaponData weaponData)
        {
            CurrentWeapon = weaponData;
            // 무기 프리팹 생성 등 추가 로직
        }
    }

}