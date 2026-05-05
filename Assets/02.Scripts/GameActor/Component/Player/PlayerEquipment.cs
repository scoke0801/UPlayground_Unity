using System.Collections;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Animation;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
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
        [Header("underwear")]
        [SerializeField] private GameObject _underwear_chest;

        [Header("StartItem")]
        [SerializeField] private List<EquipmentSO> _startEquipItemList;
        
        private WeaponType _subWeaponType = WeaponType.NoWeapon;
        private WeaponType _mainWeaponType = WeaponType.NoWeapon;
        
        private ParentConstraint _subWeaponConstraint = null;
        private ParentConstraint _mainWeaponConstraint = null;
        private bool? _requestedMainWeaponDrawn = null;
        private bool? _requestedSubWeaponDrawn = null;
        private int _mainWeaponDrawRequestVersion = 0;
        private readonly List<ParentConstraint> _weaponConstraints = new List<ParentConstraint>();
        private Transform _weaponRoot;

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

        private Dictionary<EquipArmorType, int> _equipedItemKeyDict = new Dictionary<EquipArmorType, int>();
        
        public WeaponType GetSubWeaponType() => _subWeaponType;
        public WeaponType GetMainWeaponType() => _mainWeaponType;
        
        // [TODO] 테스트 기능
        public void SetWeaponType(WeaponType type)
        {
            SetRightWeaponType(type);
            if (IsPairedWeaponType(type))
                SetLeftWeaponType(type);
            else
                SetLeftWeaponType(WeaponType.NoWeapon);
        }

        private void OnEnable()
        {
            if (EventManager.Instance == null) return;
            EventManager.Instance.Subscribe<PlayerEvent, PlayerEquipChangeEvent>(PlayerEvent.ChangeWeapon, OnWeaponChanged);
            EventManager.Instance.Subscribe<PlayerEvent, PlayerEquipChangeEvent>(PlayerEvent.EquipItem,    OnEquipItem);
        }

        private void OnDisable()
        {
            if (EventManager.Instance == null) return;
            EventManager.Instance.Unsubscribe<PlayerEvent, PlayerEquipChangeEvent>(PlayerEvent.ChangeWeapon, OnWeaponChanged);
            EventManager.Instance.Unsubscribe<PlayerEvent, PlayerEquipChangeEvent>(PlayerEvent.EquipItem,    OnEquipItem);
        }

        private void Start()
        {
            RefreshWeaponConstraintsFromModel();
            InitPartLibrary();
            StartCoroutine(CoEquipStartItem());
        }

        private void OnDestroy()
        {
            // OnDisable에서 이미 해제되므로 추가 처리 불필요
        }

        public int GetActiveEquipment(EquipArmorType type)
        {
            if (partLibrary.ContainsKey(type) == false)
                return -1;
            
            // 해당 부위의 모든 아머를 순회하며 인덱스가 일치하는 것만 활성화
            foreach (var pair in partLibrary[type])
            {
                if (pair.Value.activeSelf)
                    return pair.Key;
            }
            return -1;
        }

        public int GetActiveEquipmentKey(EquipArmorType type)
        {
            if (_equipedItemKeyDict.TryGetValue(type, out var key))
            {
                return key;
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
        private IEnumerator CoEquipStartItem()
        {
            yield return new WaitUntil(() => ItemManager.Instance != null && ItemManager.Instance.IsItemDBLoaded);

            if (_startEquipItemList == null || _startEquipItemList.Count == 0)
            {
                yield break;
            }

            for (int i = 0; i < _startEquipItemList.Count; i++)
            {
                var itemData = _startEquipItemList[i];
        
                OnEquipItem(new PlayerEquipChangeEvent()
                {
                    equipPosition = itemData.equipSlot,
                    isEquip = true,
                    itemKey = itemData.itemId,
                    weaponType = itemData.weaponType
                });
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
            _equipedItemKeyDict[armorType] = itemData.itemId;
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
                if (constraint == null)
                {
                    Debug.LogWarning($"[PlayerEquipment] {equipPosition}/{weaponType}에 매핑된 ParentConstraint가 없습니다.");
                    Destroy(newWeapon);
                    if (equipPosition == EquipPosition.LeftHand)
                    {
                        _currentSubWeaponObj = null;
                        SubWeaponKey = -1;
                    }
                    else if (equipPosition == EquipPosition.RightHand)
                    {
                        _currentMainWeaponObj = null;
                        MainWeaponKey = -1;
                    }
                    return;
                }

                // 1. 부모 설정: constraint가 붙은 오브젝트의 자식으로 설정
                newWeapon.transform.SetParent(constraint.transform, false);

                // 2. 위치 및 회전 초기화: 부모 오브젝트의 위치에 맞게 정렬
                newWeapon.transform.localPosition = Vector3.zero;
                //newWeapon.transform.localRotation = Quaternion.identity;
                //newWeapon.transform.localScale = Vector3.one; // 크기도 1,1,1로 초기화 (필요시)
            }

            // 시작/교체 시 weight와 플래그가 어긋난 채 출발하면 발도/납도 가드가 잘못 작동한다.
            // 항상 sheath 상태로 강제 동기화하고, 전투 진입 시 정상 발도 사이클이 돌도록 한다.
            ForceSyncWeaponState(equipPosition, false);
        }
        public void SetRightWeaponType(WeaponType type)
        {
            if (_weaponConstraints.Count == 0)
                RefreshWeaponConstraintsFromModel();

            _mainWeaponType = type;
            _mainWeaponConstraint = GetWeaponConstraint(EquipPosition.RightHand, type);
        }

        public void SetLeftWeaponType(WeaponType type)
        {
            if (_weaponConstraints.Count == 0)
                RefreshWeaponConstraintsFromModel();

            _subWeaponType = type;
            _subWeaponConstraint = GetWeaponConstraint(EquipPosition.LeftHand, type);
        }

        public void RefreshWeaponConstraintsFromModel()
        {
            _weaponConstraints.Clear();
            _mainWeaponConstraint = null;
            _subWeaponConstraint = null;

            _weaponRoot = FindChildRecursive(transform, "Weapon");
            if (_weaponRoot == null)
                return;

            _weaponRoot.GetComponentsInChildren(true, _weaponConstraints);
        }

        private ParentConstraint GetWeaponConstraint(EquipPosition equipPosition, WeaponType weaponType)
        {
            if (weaponType == WeaponType.NoWeapon)
                return null;

            ParentConstraint alias = null;
            ParentConstraint fallback = null;

            for (int i = 0; i < _weaponConstraints.Count; i++)
            {
                var constraint = _weaponConstraints[i];
                if (constraint == null) continue;
                if (GuessEquipPosition(constraint, weaponType) != equipPosition) continue;

                if (MatchesExactWeaponType(constraint, weaponType))
                    return constraint;

                if (alias == null && MatchesWeaponAlias(constraint, weaponType))
                    alias = constraint;

                if (fallback == null && IsGenericWeaponConstraint(constraint))
                    fallback = constraint;
            }

            return alias ?? fallback ?? GetSingleConstraintForPosition(equipPosition, weaponType);
        }

        private static Transform FindChildRecursive(Transform root, string childName)
        {
            if (root == null)
                return null;

            if (root.name == childName)
                return root;

            foreach (Transform child in root)
            {
                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private EquipPosition GuessEquipPosition(ParentConstraint constraint, WeaponType weaponType)
        {
            if (weaponType == WeaponType.Arrow)
                return EquipPosition.LeftHand;

            string normalizedName = NormalizeName(GetConstraintSearchName(constraint));
            if (normalizedName.Contains("left") || normalizedName.Contains("handl") || normalizedName.EndsWith("l"))
                return EquipPosition.LeftHand;

            return EquipPosition.RightHand;
        }

        private static string GetConstraintSearchName(ParentConstraint constraint)
        {
            string searchName = constraint.name;
            for (int i = 0; i < constraint.sourceCount; i++)
            {
                Transform sourceTransform = constraint.GetSource(i).sourceTransform;
                if (sourceTransform != null)
                    searchName += sourceTransform.name;
            }

            return searchName;
        }

        private static bool MatchesExactWeaponType(ParentConstraint constraint, WeaponType weaponType)
        {
            string constraintName = NormalizeName(constraint.name);
            string typeName = NormalizeName(weaponType.ToString());

            return constraintName.Contains(typeName);
        }

        private static bool MatchesWeaponAlias(ParentConstraint constraint, WeaponType weaponType)
        {
            string constraintName = NormalizeName(constraint.name);
            return weaponType switch
            {
                WeaponType.Sword => constraintName.Contains("sword"),
                WeaponType.SwordShield => constraintName.Contains("sword") || constraintName.Contains("shield"),
                WeaponType.GreatSword => constraintName.Contains("greatsword") || constraintName.Contains("claymore"),
                WeaponType.Staff => constraintName.Contains("staff"),
                WeaponType.Bow => constraintName.Contains("bow"),
                WeaponType.Arrow => constraintName.Contains("arrow"),
                WeaponType.Katana => constraintName.Contains("sword"),
                WeaponType.DoubleAxe => constraintName.Contains("axe"),
                WeaponType.Whip => constraintName.Contains("whip"),
                WeaponType.Spear => constraintName.Contains("spear") || constraintName.Contains("lance"),
                WeaponType.DualBlade => constraintName.Contains("dualblade") ||
                                        constraintName.Contains("doubleblade") ||
                                        constraintName.Contains("blade") ||
                                        constraintName.Contains("sword"),
                _ => false,
            };
        }

        private static bool IsPairedWeaponType(WeaponType weaponType)
        {
            return weaponType == WeaponType.DualBlade;
        }

        private bool IsGenericWeaponConstraint(ParentConstraint constraint)
        {
            return constraint.transform == _weaponRoot ||
                   NormalizeName(constraint.name) == "weapon";
        }

        private ParentConstraint GetSingleConstraintForPosition(EquipPosition equipPosition, WeaponType weaponType)
        {
            ParentConstraint result = null;
            for (int i = 0; i < _weaponConstraints.Count; i++)
            {
                var constraint = _weaponConstraints[i];
                if (constraint == null) continue;
                if (GuessEquipPosition(constraint, weaponType) != equipPosition) continue;

                if (result != null)
                    return null;

                result = constraint;
            }

            return result;
        }

        private static string NormalizeName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty)
                .ToLowerInvariant();
        }

        public bool CanToggleMainWeapon()
        {
            return _mainWeaponType != WeaponType.NoWeapon &&
                   _mainWeaponConstraint != null &&
                   _mainWeaponConstraint.sourceCount >= 2;
        }

        public void SetMainWeaponDrawn(bool drawn)
        {
            if (!CanToggleMainWeapon() || IsMainWeaponEquipped == drawn)
            {
                return;
            }

            SetWeaponDrawn(_mainWeaponConstraint, drawn);
            IsMainWeaponEquipped = drawn;

            if (IsPairedWeaponType(_mainWeaponType))
                SetSubWeaponDrawn(drawn);
        }

        public void SetSubWeaponDrawn(bool drawn)
        {
            if (_subWeaponConstraint == null ||
                _subWeaponConstraint.sourceCount < 2 ||
                IsSubWeaponEquipped == drawn)
            {
                return;
            }

            SetWeaponDrawn(_subWeaponConstraint, drawn);
            IsSubWeaponEquipped = drawn;
        }

        public bool TryPlayMainWeaponDrawMotion(bool drawn, ActorAnimator animator, Action onComplete = null)
        {
            if (!CanToggleMainWeapon())
            {
                return false;
            }

            if (IsMainWeaponEquipped == drawn)
            {
                onComplete?.Invoke();
                return true;
            }

            AnimKey animKey = drawn ? AnimKey.Equip_Weapon : AnimKey.UnEquip_Weapon;
            int requestVersion = ++_mainWeaponDrawRequestVersion;
            _requestedMainWeaponDrawn = drawn;
            var animState = animator != null ? animator.PlayMotion(animKey, 0.25f) : null;
            if (animState == null)
            {
                SetMainWeaponDrawn(drawn);
                _requestedMainWeaponDrawn = null;
                onComplete?.Invoke();
                return false;
            }

            void OnCompleted()
            {
                animator.OnMotionSetCompleted -= OnCompleted;
                if (requestVersion != _mainWeaponDrawRequestVersion)
                {
                    return;
                }

                SetMainWeaponDrawn(drawn);
                _requestedMainWeaponDrawn = null;
                onComplete?.Invoke();
            }

            animator.OnMotionSetCompleted += OnCompleted;
            return true;
        }

        public void CancelMainWeaponDrawMotionRequest()
        {
            _mainWeaponDrawRequestVersion++;
            _requestedMainWeaponDrawn = null;
        }

        private void SetWeaponDrawn(ParentConstraint constraint, bool drawn)
        {
            var rightHand = constraint.GetSource(0);
            var back = constraint.GetSource(1);

            rightHand.weight = drawn ? 1 : 0;
            back.weight = drawn ? 0 : 1;

            constraint.SetSource(0, rightHand);
            constraint.SetSource(1, back);
        }

        /// <summary>
        /// 캐릭터 교체 시 현재 전투 상태에 맞춰 메인 무기 weight와 플래그를 가드 없이 강제 동기화.
        /// </summary>
        public void ForceSyncMainWeaponState(bool drawn)
        {
            ForceSyncWeaponState(EquipPosition.RightHand, drawn);
            if (IsPairedWeaponType(_mainWeaponType))
                ForceSyncWeaponState(EquipPosition.LeftHand, drawn);
        }

        private void ForceSyncWeaponState(EquipPosition equipPosition, bool drawn)
        {
            ParentConstraint constraint = equipPosition == EquipPosition.LeftHand
                ? _subWeaponConstraint
                : _mainWeaponConstraint;

            if (constraint == null || constraint.sourceCount < 2)
                return;

            SetWeaponDrawn(constraint, drawn);
            if (equipPosition == EquipPosition.RightHand)
                IsMainWeaponEquipped = drawn;
            else
                IsSubWeaponEquipped = drawn;
        }

        // 애니메이션 이벤트 콜백
        private void OnEquipRightWeapon()
        {
            if (!CanToggleMainWeapon())
            {
                return;
            }

            SetMainWeaponDrawn(_requestedMainWeaponDrawn ?? !IsMainWeaponEquipped);
        }
        // 애니메이션 이벤트 콜백
        private void OnEquipLeftWeapon()
        {           
            if (_subWeaponConstraint == null || _subWeaponConstraint.sourceCount < 2)
            {
                return;
            }

            SetSubWeaponDrawn(_requestedSubWeaponDrawn ?? !IsSubWeaponEquipped);
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
            if (meshRoot == null)
            {
                return;
            }
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
