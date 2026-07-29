using System;
using System.Collections.Generic;
using System.Linq;
using Animancer;
using UPlayGround.Components;
using UPlayGround.Data.EnumType;
using UPlayGround.Manager;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public sealed class PlayerActorPreviewBinder : IMotionPreviewSubjectBinder
    {
        public int Priority => 100;

        public IMotionPreviewSubject TryBind(GameObject root)
        {
            PlayerActor player = root != null
                ? root.GetComponentInChildren<PlayerActor>(true)
                : null;
            return player != null
                ? new PlayerActorPreviewSubject(root, player)
                : null;
        }
    }

    public sealed class PlayerActorPreviewSubject :
        GameActorPreviewSubject,
        IMotionPreviewInputLock,
        IMotionPreviewVariants,
        IMotionPreviewStatusOverlay
    {
        private const string CharacterAxis = "character";
        private const string WeaponAxis = "weapon";
        private const string ToolAxis = "tool";

        private readonly PlayerActor _player;
        private PlayerEquipment _equipment;
        private PlayerSwapBehaviour _swap;
        private PlayerActorAnimationMotionSetCatalog _playerCatalog;
        private AnimancerComponent _activeAnimancer;
        private IReadOnlyList<MotionPreviewAxis> _axes = Array.Empty<MotionPreviewAxis>();
        private InteractionObjectType _selectedTool = InteractionObjectType.NONE;
        private bool _inputStateCaptured;
        private bool _playerInputWasSuppressed;
        private bool _actionInputWasSuppressed;
        private bool _lookWasAllowed;
        private InputManager _capturedInputManager;

        public PlayerActorPreviewSubject(GameObject root, PlayerActor player)
            : base(
                root,
                player.GetComponentInChildren<UPlayGround.Animation.ActorAnimator>(true))
        {
            _player = player;
            Refresh();
        }

        public override IMotionSetCatalog Catalog => _playerCatalog ?? base.Catalog;
        public override AnimancerComponent Animancer =>
            _activeAnimancer ?? base.Animancer;
        public IReadOnlyList<MotionPreviewAxis> Axes => _axes;

        public override void Refresh()
        {
            base.Refresh();
            if (_player == null)
                return;

            _equipment = _player.GetPlayerEquipment();
            _swap = _player.GetComponent<PlayerSwapBehaviour>();
            CharacterModelData modelData = _swap?.GetModelData(
                _swap.ActiveCharacterType);
            _activeAnimancer = modelData != null
                ? modelData.AnimancerComponent ??
                  modelData.GetComponent<AnimancerComponent>()
                : null;
            UPlayGround.Animation.PlayerActorAnimator playerAnimator =
                modelData != null
                    ? modelData.GetComponentInChildren<
                        UPlayGround.Animation.PlayerActorAnimator>(true)
                    : _player.GetComponentInChildren<
                        UPlayGround.Animation.PlayerActorAnimator>(true);
            if (playerAnimator != null)
                ActorAnimator = playerAnimator;
            RefreshPreviewOwnership();
            _playerCatalog = playerAnimator != null &&
                             playerAnimator.PlayerMotionSet != null
                ? new PlayerActorAnimationMotionSetCatalog(
                    playerAnimator.PlayerMotionSet,
                    GetWeaponType)
                : null;
            _axes = BuildAxes();
        }

        public string GetSelected(string axisId)
        {
            return axisId switch
            {
                CharacterAxis => (_swap?.ActiveCharacterType
                                  ?? CharacterActorType.None).ToString(),
                WeaponAxis => GetWeaponType().ToString(),
                ToolAxis => _selectedTool.ToString(),
                _ => null,
            };
        }

        public bool Select(string axisId, string optionId)
        {
            switch (axisId)
            {
                case CharacterAxis:
                    if (_swap == null ||
                        !Enum.TryParse(optionId, out CharacterActorType character) ||
                        !_swap.SwapTo(
                            character,
                            preserveAnimation: false,
                            spawnResidualAttack: false))
                        return false;
                    Refresh();
                    ReapplyTool();
                    ApplyPreviewWeaponState();
                    return true;

                case WeaponAxis:
                    if (_equipment == null ||
                        !Enum.TryParse(optionId, out WeaponType weapon))
                        return false;
                    _equipment.SetWeaponType(weapon);
                    _equipment.ForceSyncMainWeaponState(
                        weapon != WeaponType.NoWeapon);
                    _playerCatalog?.Refresh();
                    return true;

                case ToolAxis:
                    if (_equipment == null ||
                        !Enum.TryParse(optionId, out InteractionObjectType tool))
                        return false;
                    _selectedTool = tool;
                    ReapplyTool();
                    return true;
            }

            return false;
        }

        public override void OnPreviewLoaded(bool spawned)
        {
            base.OnPreviewLoaded(spawned);
            if (!spawned || _swap == null)
                return;

            // PartyManager를 거치지 않고 프리팹을 직접 생성하면 활성 모델 추적값이
            // 비어 있다. 이 상태에서 첫 SwapTo를 호출하면 프리팹 대표 모델이
            // 비활성화되지 않아 두 캐릭터가 겹쳐 보인다.
            if (_swap.ActiveCharacterType == CharacterActorType.None)
            {
                IReadOnlyList<CharacterActorType> types =
                    _swap.GetAllCharacterTypes();
                CharacterActorType initialType = types
                    .FirstOrDefault(type =>
                        _swap.GetModelData(type)?.gameObject.activeSelf == true);
                if (initialType == CharacterActorType.None)
                    initialType = types.FirstOrDefault();
                if (initialType != CharacterActorType.None)
                    _swap.InitializeTo(initialType);
            }

            Refresh();
            ApplyPreviewWeaponState();
        }

        public void SetInputSuppressed(bool suppressed, bool allowCameraLook)
        {
            if (suppressed)
            {
                if (!_inputStateCaptured)
                {
                    _capturedInputManager = InputManager.Instance;
                    _playerInputWasSuppressed =
                        _player != null && _player.IsInputSuppressed;
                    _actionInputWasSuppressed =
                        _capturedInputManager != null &&
                        _capturedInputManager.IsPlayerActionInputSuppressed;
                    _lookWasAllowed =
                        _capturedInputManager != null &&
                        _capturedInputManager.IsPlayerActionLookAllowed;
                    _inputStateCaptured = true;
                }

                if (_player != null)
                    _player.SetInputSuppressed(true);
                if (_capturedInputManager != null)
                {
                    _capturedInputManager.SetPlayerActionInputSuppressed(true);
                    _capturedInputManager.SetPlayerActionLookAllowed(allowCameraLook);
                }
                return;
            }

            if (!_inputStateCaptured)
                return;

            if (_player != null)
                _player.SetInputSuppressed(_playerInputWasSuppressed);
            if (_capturedInputManager != null)
            {
                _capturedInputManager.SetPlayerActionInputSuppressed(
                    _actionInputWasSuppressed);
                _capturedInputManager.SetPlayerActionLookAllowed(_lookWasAllowed);
            }

            _capturedInputManager = null;
            _inputStateCaptured = false;
        }

        public void ClearBufferedInput()
        {
            InputManager.Instance?.InputBuffer?.Clear();
        }

        public string GetSceneStatusText()
        {
            return $"캐릭터: {GetSelected(CharacterAxis)} · " +
                   $"무기: {GetSelected(WeaponAxis)} · " +
                   $"도구: {GetSelected(ToolAxis)}";
        }

        private WeaponType GetWeaponType() =>
            _equipment != null
                ? _equipment.GetMainWeaponType()
                : WeaponType.NoWeapon;

        private IReadOnlyList<MotionPreviewAxis> BuildAxes()
        {
            List<MotionPreviewAxis> result = new();
            if (_swap != null)
            {
                result.Add(new MotionPreviewAxis
                {
                    Id = CharacterAxis,
                    DisplayName = "캐릭터",
                    AffectsCatalog = true,
                    Options = _swap.GetAllCharacterTypes()
                        .Distinct()
                        .Select(value => new MotionPreviewAxisOption
                        {
                            Id = value.ToString(),
                            DisplayName = value.ToString(),
                        })
                        .ToArray(),
                });
            }

            if (_equipment != null)
            {
                result.Add(new MotionPreviewAxis
                {
                    Id = WeaponAxis,
                    DisplayName = "무기",
                    AffectsCatalog = true,
                    Options = Enum.GetValues(typeof(WeaponType))
                        .Cast<WeaponType>()
                        .Select(value => new MotionPreviewAxisOption
                        {
                            Id = value.ToString(),
                            DisplayName = value.ToString(),
                        })
                        .ToArray(),
                });
                result.Add(new MotionPreviewAxis
                {
                    Id = ToolAxis,
                    DisplayName = "생활도구",
                    AffectsCatalog = false,
                    Options = new[]
                    {
                        CreateOption(InteractionObjectType.NONE, "도구 없음"),
                        CreateOption(InteractionObjectType.STONE, "곡괭이"),
                        CreateOption(InteractionObjectType.TREE, "도끼"),
                        CreateOption(InteractionObjectType.FISHING_ZONE, "낚싯대"),
                    },
                });
            }

            return result;
        }

        private void ReapplyTool()
        {
            if (_equipment == null)
                return;

            if (_selectedTool == InteractionObjectType.NONE)
                _equipment.EndInteractionEquipment();
            else
                _equipment.BeginInteractionEquipment(_selectedTool);
        }

        private void ApplyPreviewWeaponState()
        {
            if (_equipment == null ||
                _selectedTool != InteractionObjectType.NONE)
                return;

            _equipment.RefreshWeaponConstraintsFromModel();
            _equipment.ForceSyncMainWeaponState(
                _equipment.GetMainWeaponType() != WeaponType.NoWeapon);
        }

        private static MotionPreviewAxisOption CreateOption(
            InteractionObjectType type,
            string displayName)
        {
            return new MotionPreviewAxisOption
            {
                Id = type.ToString(),
                DisplayName = displayName,
            };
        }
    }
}
