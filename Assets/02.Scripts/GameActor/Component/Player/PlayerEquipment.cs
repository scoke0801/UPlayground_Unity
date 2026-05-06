using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Animation;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Item;
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
        [Header("StartItem")]
        [SerializeField] private List<EquipmentSO> _startEquipItemList;

        [Header("Weapon Dissolve")]
        [SerializeField, Min(0f)] private float _weaponDissolveDuration = 0.6f;

        [Header("Weapon Definition")]
        [SerializeField] private List<WeaponDefinitionSO> _weaponDefinitions = new List<WeaponDefinitionSO>();
        
        private WeaponType _subWeaponType = WeaponType.NoWeapon;
        private WeaponType _mainWeaponType = WeaponType.NoWeapon;
        
        private ParentConstraint _subWeaponConstraint = null;
        private ParentConstraint _mainWeaponConstraint = null;
        private bool? _requestedMainWeaponDrawn = null;
        private bool? _requestedSubWeaponDrawn = null;
        private int _mainWeaponDrawRequestVersion = 0;
        private readonly Dictionary<EquipArmorType, int> _equippedArmorItemKeys = new Dictionary<EquipArmorType, int>();
        private readonly List<ParentConstraint> _weaponConstraints = new List<ParentConstraint>();
        private readonly List<WeaponSocketBinding> _weaponSocketBindings = new List<WeaponSocketBinding>();
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

        public WeaponType GetSubWeaponType() => _subWeaponType;
        public WeaponType GetMainWeaponType() => _mainWeaponType;
        
        // [TODO] 테스트 기능
        public void SetWeaponType(WeaponType type)
        {
            SetRightWeaponType(type);
            if (WeaponAttachmentResolver.IsPairedWeaponType(type, _weaponDefinitions))
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
            StartCoroutine(CoEquipStartItem());
        }

        private void OnDestroy()
        {
            // OnDisable에서 이미 해제되므로 추가 처리 불필요
        }

        public int GetActiveEquipmentKey(EquipArmorType type)
        {
            return _equippedArmorItemKeys.TryGetValue(type, out int itemKey) ? itemKey : -1;
        }
        
        private void OnWeaponChanged(PlayerEquipChangeEvent data)
        {
            if (data == null)
            {
                return;
            }

            EquipWeapon(data.itemKey, data.equipPosition,data.weaponType);
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
            
            EquipArmorType armorType = ToArmorType(itemData.equipSlot);
            if (armorType == EquipArmorType.None)
                return;

            if (eventData.isEquip)
                _equippedArmorItemKeys[armorType] = itemData.itemId;
            else
                _equippedArmorItemKeys.Remove(armorType);
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
            _mainWeaponConstraint = WeaponAttachmentResolver.Resolve(
                EquipPosition.RightHand,
                type,
                transform,
                _weaponRoot,
                _weaponConstraints,
                _weaponSocketBindings,
                _weaponDefinitions,
                this);
        }

        public void SetLeftWeaponType(WeaponType type)
        {
            if (_weaponConstraints.Count == 0)
                RefreshWeaponConstraintsFromModel();

            _subWeaponType = type;
            _subWeaponConstraint = WeaponAttachmentResolver.Resolve(
                EquipPosition.LeftHand,
                type,
                transform,
                _weaponRoot,
                _weaponConstraints,
                _weaponSocketBindings,
                _weaponDefinitions,
                this);
        }

        public void RefreshWeaponConstraintsFromModel()
        {
            _mainWeaponConstraint = null;
            _subWeaponConstraint = null;
            _weaponRoot = WeaponAttachmentResolver.FindWeaponRoot(transform);
            WeaponAttachmentResolver.CollectBindings(transform, _weaponRoot, _weaponConstraints, _weaponSocketBindings);
        }

        private static EquipArmorType ToArmorType(EquipPosition equipPosition)
        {
            switch (equipPosition)
            {
                case EquipPosition.Chest: return EquipArmorType.Chest;
                case EquipPosition.Head: return EquipArmorType.Head;
                case EquipPosition.Gloves: return EquipArmorType.Arm;
                case EquipPosition.Pants: return EquipArmorType.Waist;
                case EquipPosition.Shoes: return EquipArmorType.Leg;
                default: return EquipArmorType.None;
            }
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
                return;

            if (drawn)
                RecreateWeapons();

            SetWeaponDrawn(_mainWeaponConstraint, drawn);
            IsMainWeaponEquipped = drawn;

            if (WeaponAttachmentResolver.IsPairedWeaponType(_mainWeaponType, _weaponDefinitions))
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
                return false;

            if (IsMainWeaponEquipped == drawn)
            {
                onComplete?.Invoke();
                return true;
            }

            // 납도: 애니메이션 대신 무기 디졸브
            if (!drawn)
            {
                DissolveDrawnWeapons();
                IsMainWeaponEquipped = false;
                _requestedMainWeaponDrawn = null;
                onComplete?.Invoke();
                return true;
            }

            // 발도: 디졸브로 제거된 무기가 있으면 재생성
            if (_currentMainWeaponObj == null && MainWeaponKey != -1)
                RecreateWeapons();

            int requestVersion = ++_mainWeaponDrawRequestVersion;
            _requestedMainWeaponDrawn = drawn;
            var animState = animator != null ? animator.PlayMotion(AnimKey.Equip_Weapon, 0.25f) : null;
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
                    return;

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
            if (drawn)
            {
                RecreateWeapons();
                ForceSyncWeaponState(EquipPosition.RightHand, true);
                if (_subWeaponConstraint != null)
                    ForceSyncWeaponState(EquipPosition.LeftHand, true);
            }
            else
            {
                CompleteHideDrawnWeapons();
            }
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

        private void DissolveAndRelease(GameObject weaponObj)
        {
            if (weaponObj == null) return;

            weaponObj.transform.SetParent(null, true);

            foreach (var col in weaponObj.GetComponentsInChildren<Collider>(true))
                col.enabled = false;

            var dissolve = weaponObj.GetComponent<DissolveController>();
            if (dissolve == null)
                dissolve = weaponObj.AddComponent<DissolveController>();

            dissolve.StartDissolve(_weaponDissolveDuration);
        }

        private void DissolveDrawnWeapons()
        {
            if (_currentMainWeaponObj != null)
            {
                DissolveAndRelease(_currentMainWeaponObj);
                _currentMainWeaponObj = null;
            }
            else if (_mainWeaponConstraint != null)
            {
                DissolveInPlace(_mainWeaponConstraint.gameObject);
            }
            IsMainWeaponEquipped = false;

            if (_currentSubWeaponObj != null)
            {
                DissolveAndRelease(_currentSubWeaponObj);
                _currentSubWeaponObj = null;
                IsSubWeaponEquipped = false;
            }
            else if (_subWeaponConstraint != null)
            {
                DissolveInPlace(_subWeaponConstraint.gameObject);
                IsSubWeaponEquipped = false;
            }
            else
            {
                // ParentConstraint 없는 내장 서브 무기(방패 등) — weapon root 직계 자식 탐색
                DissolveBuiltInSubWeapons();
            }
        }

        private void CompleteHideDrawnWeapons()
        {
            if (_currentMainWeaponObj != null)
            {
                CompleteHideAndRelease(_currentMainWeaponObj);
                _currentMainWeaponObj = null;
            }
            else if (_mainWeaponConstraint != null)
            {
                CompleteHideInPlace(_mainWeaponConstraint.gameObject);
            }
            IsMainWeaponEquipped = false;

            if (_currentSubWeaponObj != null)
            {
                CompleteHideAndRelease(_currentSubWeaponObj);
                _currentSubWeaponObj = null;
                IsSubWeaponEquipped = false;
            }
            else if (_subWeaponConstraint != null)
            {
                CompleteHideInPlace(_subWeaponConstraint.gameObject);
                IsSubWeaponEquipped = false;
            }
            else
            {
                CompleteHideBuiltInSubWeapons();
            }
        }

        private void CompleteHideAndRelease(GameObject weaponObj)
        {
            if (weaponObj == null) return;

            weaponObj.transform.SetParent(null, true);

            foreach (var col in weaponObj.GetComponentsInChildren<Collider>(true))
                col.enabled = false;

            var dissolve = weaponObj.GetComponent<DissolveController>();
            if (dissolve == null)
                dissolve = weaponObj.AddComponent<DissolveController>();

            dissolve.CompleteDissolve();
        }

        private void CompleteHideBuiltInSubWeapons()
        {
            if (_weaponRoot == null) return;

            foreach (Transform child in _weaponRoot)
            {
                if (_mainWeaponConstraint != null && child == _mainWeaponConstraint.transform) continue;
                if (child.GetComponentInChildren<Renderer>() == null) continue;
                CompleteHideInPlace(child.gameObject);
            }
        }

        private void CompleteHideInPlace(GameObject weaponObj)
        {
            if (weaponObj == null) return;

            var dissolve = weaponObj.GetComponent<DissolveController>();
            if (dissolve == null)
                dissolve = weaponObj.AddComponent<DissolveController>();

            dissolve.CompleteDissolve(destroyOnComplete: false, onComplete: () =>
            {
                foreach (var r in weaponObj.GetComponentsInChildren<Renderer>(true))
                    r.enabled = false;
            });
        }

        private void DissolveBuiltInSubWeapons()
        {
            if (_weaponRoot == null) return;

            foreach (Transform child in _weaponRoot)
            {
                if (_mainWeaponConstraint != null && child == _mainWeaponConstraint.transform) continue;
                if (child.GetComponentInChildren<Renderer>() == null) continue;
                DissolveInPlace(child.gameObject);
            }
        }

        private void DissolveInPlace(GameObject weaponObj)
        {
            if (weaponObj == null) return;
            if (weaponObj.GetComponent<DissolveController>() != null) return; // 이미 진행 중 또는 완료

            var dissolve = weaponObj.AddComponent<DissolveController>();
            dissolve.StartDissolve(_weaponDissolveDuration, destroyOnComplete: false, onComplete: () =>
            {
                foreach (var r in weaponObj.GetComponentsInChildren<Renderer>(true))
                    r.enabled = false;
            });
        }

        private void RestoreBuiltInWeapon(GameObject weaponObj)
        {
            if (weaponObj == null) return;

            var dissolve = weaponObj.GetComponent<DissolveController>();
            if (dissolve == null) return; // 디졸브된 적 없음 — 복원 불필요

            dissolve.ResetDissolve();
            Destroy(dissolve);

            foreach (var r in weaponObj.GetComponentsInChildren<Renderer>(true))
                r.enabled = true;
        }

        private void RecreateWeapons()
        {
            if (MainWeaponKey != -1)
            {
                var newMain = GameObjectManager.Instance.CreateWeapon(MainWeaponKey);
                if (newMain != null && _mainWeaponConstraint != null)
                {
                    newMain.transform.SetParent(_mainWeaponConstraint.transform, false);
                    newMain.transform.localPosition = Vector3.zero;
                }
                _currentMainWeaponObj = newMain;
                ForceSyncWeaponState(EquipPosition.RightHand, false);
            }
            else if (_mainWeaponConstraint != null)
            {
                RestoreBuiltInWeapon(_mainWeaponConstraint.gameObject);
                ForceSyncWeaponState(EquipPosition.RightHand, false);
            }

            if (SubWeaponKey != -1)
            {
                var newSub = GameObjectManager.Instance.CreateWeapon(SubWeaponKey);
                if (newSub != null && _subWeaponConstraint != null)
                {
                    newSub.transform.SetParent(_subWeaponConstraint.transform, false);
                    newSub.transform.localPosition = Vector3.zero;
                }
                _currentSubWeaponObj = newSub;
                ForceSyncWeaponState(EquipPosition.LeftHand, false);
            }
            else if (_subWeaponConstraint != null)
            {
                RestoreBuiltInWeapon(_subWeaponConstraint.gameObject);
                ForceSyncWeaponState(EquipPosition.LeftHand, false);
            }
            else
            {
                RestoreBuiltInSubWeapons();
            }
        }

        private void RestoreBuiltInSubWeapons()
        {
            if (_weaponRoot == null) return;

            foreach (Transform child in _weaponRoot)
            {
                if (_mainWeaponConstraint != null && child == _mainWeaponConstraint.transform) continue;
                if (child.GetComponentInChildren<Renderer>() == null) continue;
                RestoreBuiltInWeapon(child.gameObject);
            }
        }
    }

}
