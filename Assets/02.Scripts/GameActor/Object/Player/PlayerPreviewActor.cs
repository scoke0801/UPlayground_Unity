using System.Collections.Generic;
using Animancer;
using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Item;
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

        [Header("Weapon Definition")]
        [SerializeField] private List<WeaponDefinitionSO> _weaponDefinitions = new List<WeaponDefinitionSO>();

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
        private readonly List<ParentConstraint> _weaponConstraints = new List<ParentConstraint>();
        private readonly List<WeaponSocketBinding> _weaponSocketBindings = new List<WeaponSocketBinding>();
        private Transform _weaponRoot;

        private PlayerEquipment _cachedPlayerEquipment;
        
        private void Awake()
        {
            _animator = GetComponent<AnimancerComponent>();

            _animator.Play(_idleTransition);
            
            RefreshWeaponConstraintsFromModel();

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

        public void SetRightWeaponType(WeaponType type)
        {
            _mainWeaponType = type;
            _mainWeaponConstraint = ResolveWeaponConstraint(EquipPosition.RightHand, type);
            if (_mainWeaponConstraint != null)
                return;

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
            _subWeaponConstraint = ResolveWeaponConstraint(EquipPosition.LeftHand, type);
            if (_subWeaponConstraint != null)
            {
                _subWeaponType = type;
                return;
            }

            switch (type)
            {
                case WeaponType.SwordShield: _subWeaponConstraint = shieldLeftConstraint; break;
                case WeaponType.Arrow: _subWeaponConstraint = arrowLeftConstraint; break;
                default:
                    _subWeaponType = WeaponType.NoWeapon;
                    return;
            }

            _subWeaponType = type;
        }

        private void RefreshWeaponConstraintsFromModel()
        {
            _weaponRoot = WeaponAttachmentResolver.FindWeaponRoot(transform);
            WeaponAttachmentResolver.CollectBindings(transform, _weaponRoot, _weaponConstraints, _weaponSocketBindings);
        }

        private ParentConstraint ResolveWeaponConstraint(EquipPosition equipPosition, WeaponType weaponType)
        {
            if (_weaponConstraints.Count == 0 && _weaponSocketBindings.Count == 0)
                RefreshWeaponConstraintsFromModel();

            return WeaponAttachmentResolver.Resolve(
                equipPosition,
                weaponType,
                transform,
                _weaponRoot,
                _weaponConstraints,
                _weaponSocketBindings,
                _weaponDefinitions,
                this,
                false);
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

            if (newWeapon == null)
                return;

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
                if (constraint == null)
                {
                    Debug.LogWarning($"[PlayerPreviewActor] {equipPosition}/{weaponType}에 매핑된 ParentConstraint가 없습니다.", this);
                    Destroy(newWeapon);
                    if (equipPosition == EquipPosition.LeftHand)
                        _currentSubWeaponObj = null;
                    else if (equipPosition == EquipPosition.RightHand)
                        _currentMainWeaponObj = null;
                    return;
                }

                // 1. 부모 설정: swordConstraint가 붙은 오브젝트의 자식으로 설정
                newWeapon.transform.SetParent(constraint.transform, false);

                // 2. 위치 및 회전 초기화: 부모 오브젝트(Sword)의 위치에 딱 맞게 정렬
                newWeapon.transform.localPosition = Vector3.zero;
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

            // 방어구 장착은 프리뷰 외형을 변경하지 않는다.
        }

        private void SetLayerRecursively(GameObject obj, string layerName)
        {
            if (obj == null)
                return;

            int layer = LayerMask.NameToLayer(layerName);
            obj.layer = layer;
        
            foreach (Transform child in obj.transform)
            {
                SetLayerRecursively(child.gameObject, layerName);
            }
        }
    }
}
