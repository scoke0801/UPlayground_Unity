using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Animancer;
using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Data.Enum;
using UPlayGround.Data.Event;
using UPlayGround.Component;
using UPlayGround.Manager;

namespace UPlayGround
{
    public class PlayerPreviewActor : MonoBehaviour
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

        [Space(5)]
        [SerializeField] ClipTransition _idleTransition;
        
        // 가지고 있는 무기
        private GameObject _currentMainWeaponObj = null;
        private GameObject _currentSubWeaponObj = null;
        
        private AnimancerComponent _animator;
        
        private WeaponType _subWeaponType = WeaponType.NoWeapon;
        private WeaponType _mainWeaponType = WeaponType.NoWeapon;
        
        private ParentConstraint _subWeaponConstraint = null;
        private ParentConstraint _mainWeaponConstraint = null;

        private PlayerEquipment _cachedPlayerEquipment;
        
        // [부위, [아머인덱스, 게임오브젝트]]
        private Dictionary<EquipArmorType, Dictionary<int, GameObject>> partLibrary = 
            new Dictionary<EquipArmorType, Dictionary<int, GameObject>>();
        
        private void Awake()
        {
            _animator = GetComponent<AnimancerComponent>();

            _animator.Play(_idleTransition);
            
            InitPartLibrary();

            PlayerActor playerActor = GameObjectManager.Instance.Player?.GetComponent<PlayerActor>();
            if (playerActor != null)
            {
                _cachedPlayerEquipment = playerActor.GetPlayerEquipment();
            }
        }

        private void Start()
        {
            EventManager.Instance.Subscribe<PlayerEvent, PlayerEquipChangeEvent>(
                PlayerEvent.EquipItem, 
                OnEquipItem
            );

            InitCurrentEquipments();
        }

        private void InitCurrentEquipments()
        {
            // 방어구 동기화
            foreach (EquipArmorType type in System.Enum.GetValues(typeof(EquipArmorType)))
            {
                if (type == EquipArmorType.None) continue;

                int index = _cachedPlayerEquipment.GetActiveEquipment(type);

                if (index < 0)
                {
                    continue;
                }
                foreach (var pair in partLibrary[type])
                {
                    pair.Value.SetActive(pair.Key == index);
                }
            }
            
            // 무기 동기화
            EquipWeapon(_cachedPlayerEquipment.MainWeaponKey, EquipPosition.RightHand, _cachedPlayerEquipment.GetMainWeaponType());
            EquipWeapon(_cachedPlayerEquipment.SubWeaponKey, EquipPosition.LeftHand, _cachedPlayerEquipment.GetSubWeaponType());
        }

        private void OnDestroy()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.Unsubscribe<PlayerEvent, PlayerEquipChangeEvent>(
                    PlayerEvent.EquipItem, 
                    OnEquipItem
                );
            }
        }

        void InitPartLibrary()
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
        // 특정 부위의 특정 번호를 활성화 (예: Chest 부위의 3번 장비)
        public void EquipPart(EquipArmorType part, int armorIndex)
        {
            if (!partLibrary.ContainsKey(part)) return;

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
        
        /// <summary>
        /// 특정 무기 장착 (아이템 시스템 연동)
        /// </summary>
        public void EquipWeapon(int itemKey, EquipPosition equipPosition, WeaponType weaponType)
        {
            if (itemKey < 0)
            {
                return;
            }
            
            DestroyEquippedWeapon(equipPosition);

            GameObject newWeapon = GameObjectManager.Instance.CreateWeapon(itemKey);

            SetLayerRecursively(newWeapon, "CharacterPreview");
            
            ParentConstraint constraint = null;
            switch (equipPosition)
            {
                case EquipPosition.LeftHand: 
                    SetLeftWeaponType(weaponType);
                    constraint = _subWeaponConstraint;
                    _currentSubWeaponObj = newWeapon;
                    break;
                case EquipPosition.RightHand: 
                    SetRightWeaponType(weaponType);
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

        private void SetLayerRecursively(GameObject obj, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            obj.layer = layer;
        
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layerName);
            }
        }
    }
}