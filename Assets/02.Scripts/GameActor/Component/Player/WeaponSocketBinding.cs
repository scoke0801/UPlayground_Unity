using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Components
{
    public class WeaponSocketBinding : MonoBehaviour
    {
        [Header("Socket")]
        public EquipPosition equipPosition = EquipPosition.RightHand;
        public WeaponType weaponType = WeaponType.NoWeapon;
        public bool matchAnyWeaponType = false;

        [Header("Constraint")]
        public ParentConstraint constraint;

        [Header("Alias")]
        public string[] aliases;

        private ParentConstraint _resolvedConstraint;
        public ParentConstraint Constraint
        {
            get
            {
                if (_resolvedConstraint == null)
                    _resolvedConstraint = constraint != null ? constraint : GetComponent<ParentConstraint>();

                return _resolvedConstraint;
            }
        }

        private void Awake()
        {
            _resolvedConstraint = constraint != null ? constraint : GetComponent<ParentConstraint>();
        }

        public bool Matches(EquipPosition position, WeaponType targetWeaponType, string normalizedWeaponName)
        {
            if (equipPosition != position)
                return false;

            if (matchAnyWeaponType || weaponType == targetWeaponType)
                return true;

            string bindingWeaponName = WeaponAttachmentResolverUtility.NormalizeName(weaponType.ToString());
            if (!string.IsNullOrEmpty(bindingWeaponName) && normalizedWeaponName.Contains(bindingWeaponName))
                return true;

            if (aliases == null)
                return false;

            for (int i = 0; i < aliases.Length; i++)
            {
                string alias = WeaponAttachmentResolverUtility.NormalizeName(aliases[i]);
                if (!string.IsNullOrEmpty(alias) && normalizedWeaponName.Contains(alias))
                    return true;
            }

            return false;
        }
    }
}
