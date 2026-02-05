using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Serialization;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Enum;
using UPlayGround.Data.Event;
using UPlayGround.Manager;

namespace UPlayGround.Component
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
        
        [Header("underwear")]
        [SerializeField] private GameObject _underwear_chest;

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

        public int MainWeaponKey { get; private set; } = -1;
        public int SubWeaponKey { get; private set; } = -1;
        
        // [TODO] 실제 Data로 가져올 수 있어야 하겠지만 우선은 단독 데이터로 관리하는 상태
        public WeaponData CurrentWeapon { get; private set; }
        
        // [부위, [아머인덱스, 게임오브젝트]]
        private Dictionary<EquipArmorType, Dictionary<int, GameObject>> partLibrary = 
            new Dictionary<EquipArmorType, Dictionary<int, GameObject>>();

        public WeaponType GetSubWeaponType() => _subWeaponType;
        public WeaponType GetMainWeaponType() => _mainWeaponType;
        private void Start()
        {
            EventManager.Instance.Subscribe<PlayerEvent, PlayerEquipChangeEvent>(
                PlayerEvent.ChangeWeapon, 
                OnWeaponChanged
            );
            EventManager.Instance.Subscribe<PlayerEvent, PlayerEquipChangeEvent>(
                PlayerEvent.EquipItem, 
                OnEquipItem
            );
            InitPartLibrary();
        }
        private void OnDestroy()
        {
            if (EventManager.Instance == null)
                return;
            
            EventManager.Instance.Unsubscribe<PlayerEvent, PlayerEquipChangeEvent>(
                PlayerEvent.ChangeWeapon, 
                OnWeaponChanged
            );
            EventManager.Instance.Unsubscribe<PlayerEvent, PlayerEquipChangeEvent>(
                PlayerEvent.EquipItem, 
                OnEquipItem
            );
        }

        public int GetActiveEquipment(EquipArmorType type)
        {
            // 해당 부위의 모든 아머를 순회하며 인덱스가 일치하는 것만 활성화
            foreach (var pair in partLibrary[type])
            {
                if (pair.Value.activeSelf)
                    return pair.Key;
            }
            return -1;
        }
        
        private void OnWeaponChanged(PlayerEquipChangeEvent data)
        {
            if (data == null)
            {
                return;
            }

            EquipWeapon(data.itemKey, data.equipPosition,data.weaponType);
        }

        // 특정 부위의 특정 번호를 활성화 (예: Chest 부위의 3번 장비)
        public void EquipPart(EquipArmorType part, int armorIndex)
        {
            if (!partLibrary.ContainsKey(part)) return;

            bool isAnyPantsActive = false;
            bool isAnyChestActive = false;

            // 해당 부위의 모든 아머를 순회하며 인덱스가 일치하는 것만 활성화
            foreach (var pair in partLibrary[part])
            {
                bool isActive = pair.Key == armorIndex;

                if (part == EquipArmorType.Chest && isActive)
                    isAnyChestActive = true;
                pair.Value.SetActive(isActive);
            }

            if (part == EquipArmorType.Chest)
            {
                _underwear_chest.SetActive(!isAnyChestActive);
            }
        }
        
        private void OnEquipItem(PlayerEquipChangeEvent eventData)
        {
            EquipmentSO itemData = ItemManager.Instance.GetItemData(eventData.itemKey) as EquipmentSO;
            if (itemData == null)
            {
                return;
            }
            
            if (eventData.equipPosition == EquipPosition.LeftHand)
            {
                SetLeftWeaponType(eventData.weaponType);
                EquipWeapon(eventData.itemKey, eventData.equipPosition, eventData.weaponType);
                return;
            }
            else if (eventData.equipPosition == EquipPosition.RightHand)
            {
                SetRightWeaponType(eventData.weaponType);
                EquipWeapon(eventData.itemKey, eventData.equipPosition, eventData.weaponType);
                return;
            }
            
            EquipArmorType armorType = EquipArmorType.None;
            switch (itemData.equipSlot)
            {
                case EquipPosition.Chest: armorType = EquipArmorType.Chest; break;
                case EquipPosition.Head: armorType = EquipArmorType.Head; break;
                case EquipPosition.Gloves: armorType = EquipArmorType.Arm; break;
                case EquipPosition.Pants: armorType = EquipArmorType.Waist; break;
                case EquipPosition.Shoes: armorType = EquipArmorType.Leg; break;
                default: return;
            }

            int armorIndex = itemData.itemId % 100;
            EquipPart(armorType, armorIndex);
        }

        /// <summary>
        /// 특정 무기 장착 (아이템 시스템 연동)
        /// </summary>
        public void EquipWeapon(int itemKey, EquipPosition equipPosition, WeaponType weaponType)
        {
            DestroyEquippedWeapon(equipPosition);
            
            GameObject newWeapon = GameObjectManager.Instance.CreateWeapon(itemKey);

            ParentConstraint constraint = null;
            switch (equipPosition)
            {
                case EquipPosition.LeftHand: 
                    SetLeftWeaponType(weaponType);
                    constraint = _subWeaponConstraint;
                    _currentSubWeaponObj = newWeapon;
                    SubWeaponKey = itemKey;
                    break;
                case EquipPosition.RightHand: 
                    SetRightWeaponType(weaponType);
                    constraint = _mainWeaponConstraint;
                    _currentMainWeaponObj = newWeapon;
                    MainWeaponKey = itemKey;
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
                    SubWeaponKey = -1;
                    IsSubWeaponEquipped = false;
                }
            }
            else if (equipPosition == EquipPosition.RightHand)
            {
                if (_currentMainWeaponObj != null)
                {
                    Destroy(_currentMainWeaponObj);
                    _currentMainWeaponObj = null;
                    MainWeaponKey = -1;
                    IsMainWeaponEquipped = false;
                }
            }
        }
        
        private void InitPartLibrary()
        {
            // "Female" 하위의 모든 Armor_XXX를 탐색하며 부위별로 분류
            Transform meshRoot = transform.Find("Mesh/Female");
            
            // Enum 순회하며 딕셔너리 초기화
            foreach (EquipArmorType type in System.Enum.GetValues(typeof(EquipArmorType)))
            {
                if (type == EquipArmorType.None) continue;
                partLibrary[type] = new Dictionary<int, GameObject>();
            }
            
            // 모든 Armor_XXX 자식 순회
            foreach (Transform armorSet in meshRoot)
            {
                if (!armorSet.name.StartsWith("Armor_")) continue;

                // 이름에서 숫자만 추출 (예: "Armor_001" -> 1)
                int armorIndex = ExtractIndexFromName(armorSet.name);

                foreach (Transform piece in armorSet)
                {
                    EquipArmorType pieceType = DeterminePartType(piece.name);
                
                    if (pieceType != EquipArmorType.None)
                    {
                        // 부위별 딕셔너리에 인덱스를 키값으로 등록
                        partLibrary[pieceType][armorIndex] = piece.gameObject;
                        piece.gameObject.SetActive(false); // 초기 비활성화
                    }
                }
            }
            //"Armor_001" 등의 문자열에서 숫자 '1'을 추출하는 헬퍼 함수
            int ExtractIndexFromName(string name)
            {
                string resultString = Regex.Match(name, @"\d+").Value;
                return int.TryParse(resultString, out int result) ? result : -1;
            }

            // 이름의 끝부분을 확인하여 부위(Type) 결정 (중복 매칭 방지)
            EquipArmorType DeterminePartType(string name)
            {
                if (name.EndsWith("_Head")) return EquipArmorType.Head;
                if (name.EndsWith("_Chest")) return EquipArmorType.Chest;
                if (name.EndsWith("_Arm")) return EquipArmorType.Arm;
                if (name.EndsWith("_Waist")) return EquipArmorType.Waist;
                if (name.EndsWith("_Leg")) return EquipArmorType.Leg;
                return EquipArmorType.None;
            }
        }
    }

}