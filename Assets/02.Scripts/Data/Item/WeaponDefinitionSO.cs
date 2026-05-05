using System;
using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Item
{
    public enum WeaponEquipStyle
    {
        SingleRight = 0,
        SingleLeft = 1,
        RightWithSub = 2,
        PairedBothHands = 3,
    }

    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "UPlayGround/SO/WeaponDefinition")]
    public class WeaponDefinitionSO : ScriptableObject
    {
        [Serializable]
        public class Alias
        {
            public string value;
        }

        [Header("Identity")]
        public WeaponType weaponType = WeaponType.NoWeapon;

        [Header("Equip")]
        public WeaponEquipStyle equipStyle = WeaponEquipStyle.SingleRight;

        [Header("Motion")]
        public WeaponType motionWeaponType = WeaponType.NoWeapon;
        public bool requiresDrawStateForAttackMotion = true;

        [Header("Constraint")]
        public List<Alias> constraintAliases = new List<Alias>();

        public WeaponType MotionWeaponType => motionWeaponType == WeaponType.NoWeapon ? weaponType : motionWeaponType;

        public bool MatchesAlias(string normalizedName)
        {
            if (string.IsNullOrEmpty(normalizedName))
                return false;

            for (int i = 0; i < constraintAliases.Count; i++)
            {
                string alias = NormalizeName(constraintAliases[i]?.value);
                if (!string.IsNullOrEmpty(alias) && normalizedName.Contains(alias))
                    return true;
            }

            return false;
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
    }
}
