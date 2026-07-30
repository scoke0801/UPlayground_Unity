using System;
using System.Collections.Generic;
using System.Linq;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    public sealed class PlayerActorAnimationMotionSetCatalog :
        IMotionSetCatalog,
        IMotionSetCatalogVariants
    {
        private const string WeaponAxis = "weapon";
        private readonly PlayerActorAnimationMotionSet _source;
        private readonly Func<WeaponType> _weaponType;
        private WeaponType _selectedWeaponType;
        private bool _weaponTypeExplicit;
        private ActorAnimationMotionSet _resolvedSource;
        private ActorAnimationMotionSetCatalog _resolved;
        private readonly IReadOnlyList<MotionPreviewAxis> _axes;

        public PlayerActorAnimationMotionSetCatalog(
            PlayerActorAnimationMotionSet source,
            Func<WeaponType> weaponType)
        {
            _source = source;
            _weaponType = weaponType;
            _selectedWeaponType = weaponType?.Invoke() ?? WeaponType.NoWeapon;
            _axes = new[]
            {
                new MotionPreviewAxis
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
                },
            };
            Refresh();
        }

        public UnityEngine.Object SourceAsset => _source;
        public ActorAnimationMotionSet ResolvedSource => _resolvedSource;
        public IReadOnlyList<MotionSetSlot> Slots =>
            _resolved?.Slots ?? Array.Empty<MotionSetSlot>();
        public IReadOnlyList<MotionSetSlot> AssignableSlots =>
            _resolved?.AssignableSlots ?? Array.Empty<MotionSetSlot>();
        public IReadOnlyList<MotionPreviewAxis> Axes => _axes;

        public MotionSetAsset Resolve(string slotId) => _resolved?.Resolve(slotId);
        public bool Assign(string slotId, MotionSetAsset asset) =>
            _resolved != null && _resolved.Assign(slotId, asset);
        public MotionSetAsset CreateAndAssign(string slotId, string directory) =>
            _resolved?.CreateAndAssign(slotId, directory);

        public void Refresh()
        {
            // 사용자가 축에서 직접 고른 무기는 대상의 실제 장비 상태로 되돌리지 않는다.
            if (_weaponType != null && !_weaponTypeExplicit)
                _selectedWeaponType = _weaponType();
            ActorAnimationMotionSet next = _source != null
                ? _source.GetActorAnimationMotionSet(_selectedWeaponType)
                  ?? _source.GetDefaultMotionSet()
                : null;
            if (next != _resolvedSource)
            {
                _resolvedSource = next;
                _resolved = next != null
                    ? new ActorAnimationMotionSetCatalog(next)
                    : null;
            }
            else
            {
                _resolved?.Refresh();
            }
        }

        public string GetSelected(string axisId) =>
            axisId == WeaponAxis ? _selectedWeaponType.ToString() : null;

        public bool Select(string axisId, string optionId)
        {
            if (axisId != WeaponAxis ||
                !Enum.TryParse(optionId, out WeaponType weaponType))
                return false;

            _selectedWeaponType = weaponType;
            _weaponTypeExplicit = true;
            Refresh();
            return true;
        }
    }
}
