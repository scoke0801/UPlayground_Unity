using System.Collections;
using System;
using System.Collections.Generic;
using INab.Common;
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

        [Header("Weapon Draw FX")]
        [SerializeField] private string _weaponDrawFxKey = string.Empty;
        [SerializeField] private Vector3 _weaponDrawFxOffset = Vector3.zero;
        [SerializeField, Min(0f)] private float _weaponDrawFxDuration = 5f;
        [SerializeField] private bool _parentWeaponDrawFxToWeapon = false;

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
        private readonly Dictionary<Renderer, Material[]> _builtInWeaponSharedMaterials = new Dictionary<Renderer, Material[]>();
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

        /// <summary> 이 모델의 기본(시작) 장비 목록. 장비 레지스트리 시딩용 읽기 접근자. </summary>
        public IReadOnlyList<EquipmentSO> StartEquipItems => _startEquipItemList;

        public bool IsWeaponTrailDrawable(WeaponTrailEffect trail)
        {
            if (trail == null) return false;

            if (IsMainWeaponEquipped &&
                IsWeaponSlotVisible(_currentMainWeaponObj, _mainWeaponConstraint) &&
                IsTrailUnderWeaponSlot(trail, _currentMainWeaponObj, _mainWeaponConstraint))
            {
                return true;
            }

            if (IsSubWeaponEquipped &&
                IsWeaponSlotVisible(_currentSubWeaponObj, _subWeaponConstraint) &&
                IsTrailUnderWeaponSlot(trail, _currentSubWeaponObj, _subWeaponConstraint))
            {
                return true;
            }

            return false;
        }
        
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

            // RefreshForCharacter가 swap 시 미리 세팅한 weapon type이 있으면 constraint 재해결.
            // (RefreshWeaponConstraintsFromModel이 _mainWeaponConstraint를 null로 리셋하기 때문에
            //  start item이 비어있는 모델은 이 보정이 없으면 constraint가 영구 null로 남는다.)
            if (_mainWeaponType != WeaponType.NoWeapon)
                SetRightWeaponType(_mainWeaponType);
            if (_subWeaponType != WeaponType.NoWeapon)
                SetLeftWeaponType(_subWeaponType);

            // 시작 장비 착용은 장비 레지스트리(InventoryManager) → RefreshForCharacter의
            // ApplyEquipmentSnapshot 경로로 일원화한다. 여기서 직접 착용하면 이중 착용/레이스가 발생.
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

            bool succeeded = EquipWeapon(data.itemKey, data.equipPosition, data.weaponType);
            data.MarkHandled(succeeded, succeeded ? null : "무기 변경 실패");
        }

        private void OnEquipItem(PlayerEquipChangeEvent eventData)
        {
            EquipmentSO itemData = ItemManager.Instance.GetItemData(eventData.itemKey) as EquipmentSO;
            if (itemData == null)
            {
                eventData.MarkHandled(false, "장비 데이터를 찾을 수 없음");
                return;
            }
            
            if (eventData.equipPosition == EquipPosition.LeftHand)
            {
                bool succeeded = EquipWeapon(eventData.itemKey, eventData.equipPosition, eventData.weaponType);
                eventData.MarkHandled(succeeded, succeeded ? null : "왼손 무기 장착 실패");
                return;
            }
            else if (eventData.equipPosition == EquipPosition.RightHand)
            {
                bool succeeded = EquipWeapon(eventData.itemKey, eventData.equipPosition, eventData.weaponType);
                eventData.MarkHandled(succeeded, succeeded ? null : "오른손 무기 장착 실패");
                return;
            }
            
            EquipArmorType armorType = ToArmorType(itemData.equipSlot);
            if (armorType == EquipArmorType.None)
            {
                eventData.MarkHandled(false, "지원하지 않는 장비 슬롯");
                return;
            }

            if (eventData.isEquip)
                _equippedArmorItemKeys[armorType] = itemData.itemId;
            else
                _equippedArmorItemKeys.Remove(armorType);

            eventData.MarkHandled(true);
        }

        /// <summary>
        /// 특정 무기 장착 (아이템 시스템 연동)
        /// </summary>
        public bool EquipWeapon(int itemKey, EquipPosition equipPosition, WeaponType weaponType)
        {
            ParentConstraint constraint = null;
            switch (equipPosition)
            {
                case EquipPosition.LeftHand: 
                    SetLeftWeaponType(weaponType);
                    constraint = _subWeaponConstraint;
                    break;
                case EquipPosition.RightHand: 
                    SetRightWeaponType(weaponType);
                    constraint = _mainWeaponConstraint;
                    break;
                default:
                    return false;
            }

            DestroyEquippedWeapon(equipPosition);

            GameObject newWeapon = GameObjectManager.Instance.CreateWeapon(itemKey);
            if (newWeapon == null)
            {
                // 슬롯은 DestroyEquippedWeapon으로 이미 비었으므로(key=-1) 타입도 NoWeapon으로 맞춘다.
                // 그러지 않으면 stale 타입이 모션셋/Idle 분기/발도 시 빌트인 무기 복원을 오작동시킨다.
                ResetWeaponType(equipPosition);
                return false;
            }

            if (constraint == null)
            {
                Debug.LogWarning($"[PlayerEquipment] {equipPosition}/{weaponType}에 매핑된 ParentConstraint가 없습니다.");
                Destroy(newWeapon);
                ResetWeaponType(equipPosition);
                return false;
            }

            if (equipPosition == EquipPosition.LeftHand)
            {
                _currentSubWeaponObj = newWeapon;
                SubWeaponKey = itemKey;
            }
            else if (equipPosition == EquipPosition.RightHand)
            {
                _currentMainWeaponObj = newWeapon;
                MainWeaponKey = itemKey;
            }

            // 1. 부모 설정: constraint가 붙은 오브젝트의 자식으로 설정
            newWeapon.transform.SetParent(constraint.transform, false);

            // 2. 위치 및 회전 초기화: 부모 오브젝트의 위치에 맞게 정렬
            newWeapon.transform.localPosition = Vector3.zero;

            // 시작/교체 시 weight와 플래그가 어긋난 채 출발하면 발도/납도 가드가 잘못 작동한다.
            // 항상 sheath 상태로 강제 동기화하고, 전투 진입 시 정상 발도 사이클이 돌도록 한다.
            ForceSyncWeaponState(equipPosition, false);

            // 주 무기를 교체했을 때 기존 보조 무기와 타입이 맞지 않으면 자동 해제
            if (equipPosition == EquipPosition.RightHand)
                UnequipIncompatibleSubWeapon(weaponType);

            ActorWeaponTrailController.RefreshAttackTrails(this);
            GetComponentInParent<UPlayGround.Combat.CombatHitboxSet>()?.Refresh();
            return true;
        }

        // 주 무기 타입이 바뀌어 현재 보조 무기와 호환되지 않으면 보조 무기를 제거하고 타입을 초기화한다.
        private void UnequipIncompatibleSubWeapon(WeaponType newMainType)
        {
            if (SubWeaponKey < 0 || _subWeaponType == newMainType)
                return;

            DestroyEquippedWeapon(EquipPosition.LeftHand);
            ResetWeaponType(EquipPosition.LeftHand);
        }

        private Coroutine _applySnapshotCo;

        /// <summary>
        /// 장비 레지스트리의 주/보조 무기 itemId대로 외형을 동기화한다. (방어구는 외형 반영 없음)
        /// 현재 장착 키와 동일한 슬롯은 재생성하지 않는다. item DB 로드 전이면 로드 후 적용.
        /// 활성(enabled) 모델에서만 동작 — 벤치 모델은 레지스트리가 소스이므로 시각 반영 불필요.
        /// </summary>
        public void ApplyEquipmentSnapshot(int mainKey, int subKey)
        {
            if (!isActiveAndEnabled)
                return;

            if (_applySnapshotCo != null)
                StopCoroutine(_applySnapshotCo);
            _applySnapshotCo = StartCoroutine(CoApplyEquipmentSnapshot(mainKey, subKey));
        }

        private IEnumerator CoApplyEquipmentSnapshot(int mainKey, int subKey)
        {
            yield return new WaitUntil(() => ItemManager.Instance != null && ItemManager.Instance.IsItemDBLoaded);

            // 주 무기 먼저 적용 — 주 무기 교체가 EquipWeapon 내부에서 비호환 보조를 정리할 수 있어,
            // 보조는 그 이후에 최신 상태(live 키)로 판정한다.
            ApplyWeaponSlot(EquipPosition.RightHand, mainKey);
            ApplyWeaponSlot(EquipPosition.LeftHand,  subKey);

            _applySnapshotCo = null;
        }

        private void ApplyWeaponSlot(EquipPosition slot, int targetKey)
        {
            int currentKey = slot == EquipPosition.RightHand ? MainWeaponKey : SubWeaponKey;
            if (targetKey == currentKey)
                return; // 이미 일치 — 재생성 불필요 (방어구 변경 등으로 인한 불필요한 무기 재생성 방지)

            if (targetKey < 0)
            {
                DestroyEquippedWeapon(slot);
                ResetWeaponType(slot);
                ActorWeaponTrailController.RefreshAttackTrails(this);
                GetComponentInParent<UPlayGround.Combat.CombatHitboxSet>()?.Refresh();
                return;
            }

            WeaponType type = ItemManager.Instance.GetItemData(targetKey) is EquipmentSO eq
                ? eq.weaponType
                : WeaponType.NoWeapon;
            EquipWeapon(targetKey, slot, type);
        }

        // 장착 실패 슬롯의 무기 타입을 NoWeapon으로 되돌려 key(-1)/obj(null)와 일관시킨다.
        private void ResetWeaponType(EquipPosition equipPosition)
        {
            if (equipPosition == EquipPosition.LeftHand)
                SetLeftWeaponType(WeaponType.NoWeapon);
            else if (equipPosition == EquipPosition.RightHand)
                SetRightWeaponType(WeaponType.NoWeapon);
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
            CacheBuiltInWeaponSharedMaterials();
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
            if (!CanToggleMainWeapon())
                return;

            // 발도 시: 무기 GameObject가 없거나 디졸브로 사라졌으면 플래그와 무관하게 항상 재생성
            if (drawn && _currentMainWeaponObj == null)
                RecreateWeapons();

            if (IsMainWeaponEquipped == drawn)
                return;

            SetWeaponDrawn(_mainWeaponConstraint, drawn);
            IsMainWeaponEquipped = drawn;
            if (!drawn)
                ActorWeaponTrailController.SuppressAttackTrails(this);

            if (drawn)
                PlayWeaponDrawFx(_currentMainWeaponObj, _mainWeaponConstraint);

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
            if (!drawn)
                ActorWeaponTrailController.SuppressAttackTrails(this);

            if (drawn)
                PlayWeaponDrawFx(_currentSubWeaponObj, _subWeaponConstraint);
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

            // 납도: 애니메이션 없이 무기 디졸브.
            // 사라지는 연출 중에는 Constraint weight를 전환하지 않는다.
            if (!drawn)
            {
                DissolveDrawnWeapons();
                ActorWeaponTrailController.SuppressAttackTrails(this);
                IsMainWeaponEquipped = false;
                _requestedMainWeaponDrawn = null;
                onComplete?.Invoke();
                return true;
            }

            // 발도: 애니메이션 없이 즉시 무기 장착 (SetMainWeaponDrawn 내부에서 RecreateWeapons 처리)
            _mainWeaponDrawRequestVersion++;
            _requestedMainWeaponDrawn = null;
            SetMainWeaponDrawn(drawn);
            onComplete?.Invoke();
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

        private void PlayWeaponDrawFx(GameObject weaponObj, ParentConstraint constraint)
        {
            if (string.IsNullOrWhiteSpace(_weaponDrawFxKey) || GameObjectManager.Instance == null)
                return;

            Transform fxParent = null;
            Vector3 position;
            Quaternion rotation = transform.rotation;

            if (weaponObj != null)
            {
                fxParent = _parentWeaponDrawFxToWeapon ? weaponObj.transform : null;
                position = weaponObj.transform.TransformPoint(_weaponDrawFxOffset);
                rotation = weaponObj.transform.rotation;
            }
            else if (constraint != null)
            {
                fxParent = _parentWeaponDrawFxToWeapon ? constraint.transform : null;
                position = constraint.transform.TransformPoint(_weaponDrawFxOffset);
                rotation = constraint.transform.rotation;
            }
            else
            {
                position = transform.TransformPoint(_weaponDrawFxOffset);
            }

            GameObjectManager.Instance.ShowFX(_weaponDrawFxKey, position, rotation, fxParent, _weaponDrawFxDuration);
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
                ActorWeaponTrailController.SuppressAttackTrails(this);
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

            RestoreBuiltInWeaponSharedMaterials(weaponObj);

            var dissolve = weaponObj.GetComponent<DissolveController>();
            if (dissolve == null)
                dissolve = weaponObj.AddComponent<DissolveController>();

            dissolve.CompleteDissolve(destroyOnComplete: false, onComplete: () =>
            {
                RestoreBuiltInWeaponSharedMaterials(weaponObj);
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

            RestoreBuiltInWeaponSharedMaterials(weaponObj);

            var dissolve = weaponObj.AddComponent<DissolveController>();
            dissolve.StartDissolve(_weaponDissolveDuration, destroyOnComplete: false, onComplete: () =>
            {
                RestoreBuiltInWeaponSharedMaterials(weaponObj);
                foreach (var r in weaponObj.GetComponentsInChildren<Renderer>(true))
                    r.enabled = false;
            });
        }

        private void RestoreBuiltInWeapon(GameObject weaponObj)
        {
            if (weaponObj == null) return;

            var dissolve = weaponObj.GetComponent<DissolveController>();
            if (dissolve != null)
            {
                dissolve.ResetDissolve();
                Destroy(dissolve);
            }

            RestoreBuiltInWeaponSharedMaterials(weaponObj);

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

            ActorWeaponTrailController.RefreshAttackTrails(this);
        }

        private static bool IsTrailUnderWeaponSlot(WeaponTrailEffect trail, GameObject weaponObj, ParentConstraint constraint)
        {
            if (trail == null) return false;

            if (IsTransformUnderWeaponSlot(trail.transform, weaponObj, constraint))
                return true;

            if (IsTransformUnderWeaponSlot(trail.lineTipTransform, weaponObj, constraint))
                return true;

            return IsTransformUnderWeaponSlot(trail.lineBottomTransform, weaponObj, constraint);
        }

        private static bool IsWeaponSlotVisible(GameObject weaponObj, ParentConstraint constraint)
        {
            if (weaponObj != null)
                return HasVisibleRenderer(weaponObj.transform);

            return constraint != null && HasVisibleRenderer(constraint.transform);
        }

        private static bool HasVisibleRenderer(Transform root)
        {
            if (root == null || !root.gameObject.activeInHierarchy) return false;

            var renderers = root.GetComponentsInChildren<Renderer>(false);
            for (int i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                if (renderer != null && renderer.enabled && renderer.gameObject.activeInHierarchy)
                    return true;
            }

            return false;
        }

        private static bool IsTransformUnderWeaponSlot(
            Transform target,
            GameObject weaponObj,
            ParentConstraint constraint)
        {
            if (target == null) return false;

            if (weaponObj != null && target.IsChildOf(weaponObj.transform))
                return true;

            return constraint != null && target.IsChildOf(constraint.transform);
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

        private void CacheBuiltInWeaponSharedMaterials()
        {
            _builtInWeaponSharedMaterials.Clear();
            if (_weaponRoot == null) return;

            foreach (var renderer in _weaponRoot.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;

                var materials = renderer.sharedMaterials;
                if (materials == null || materials.Length == 0) continue;

                bool hasValidMaterial = false;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null) continue;
                    hasValidMaterial = true;
                    break;
                }

                if (hasValidMaterial)
                    _builtInWeaponSharedMaterials[renderer] = (Material[])materials.Clone();
            }
        }

        private void RestoreBuiltInWeaponSharedMaterials(GameObject weaponObj)
        {
            if (weaponObj == null || _builtInWeaponSharedMaterials.Count == 0) return;

            foreach (var renderer in weaponObj.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer == null) continue;
                if (!_builtInWeaponSharedMaterials.TryGetValue(renderer, out var materials)) continue;

                renderer.sharedMaterials = materials;
            }
        }
    }

}
