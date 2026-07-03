namespace UPlayGround.Data.EnumType
{
    public enum WeaponType
    {
        NoWeapon = 0,
        Sword = 1,
        SwordShield = 2,
        GreatSword = 3,
        Staff = 4,
        Bow = 5,
        Arrow = 6,
        
        Katana = 10,
        DoubleAxe = 11,
        Whip = 12,
        Spear = 13,
        DualBlade = 14,
        
    }

    public enum EquipPosition
    {
        None = 0,
        LeftHand,
        RightHand,
        
        Head,
        Chest,
        Pants,
        Shoes,
        Gloves
    }

    public enum EquipArmorType
    {
        None = 0,

        Head,
        Chest,
        Arm,
        Waist,
        Leg
    }

    public static class EquipDisplayExtensions
    {
        /// <summary> 무기 종류 한글 표기. </summary>
        public static string ToDisplayString(this WeaponType type)
        {
            return type switch
            {
                WeaponType.Sword       => "검",
                WeaponType.SwordShield => "검+방패",
                WeaponType.GreatSword  => "대검",
                WeaponType.Staff       => "지팡이",
                WeaponType.Bow         => "활",
                WeaponType.Arrow       => "화살",
                WeaponType.Katana      => "카타나",
                WeaponType.DoubleAxe   => "쌍도끼",
                WeaponType.Whip        => "채찍",
                WeaponType.Spear       => "창",
                WeaponType.DualBlade   => "쌍검",
                _                      => string.Empty
            };
        }

        /// <summary> 장착 부위 한글 표기. 무기 슬롯이면 무기 종류를 괄호로 덧붙인다. </summary>
        public static string ToDisplayString(this EquipPosition slot, WeaponType weaponType = WeaponType.NoWeapon)
        {
            string baseName = slot switch
            {
                EquipPosition.RightHand => "주 무기",
                EquipPosition.LeftHand  => "보조 무기",
                EquipPosition.Head      => "머리",
                EquipPosition.Chest     => "상의",
                EquipPosition.Pants     => "하의",
                EquipPosition.Shoes     => "신발",
                EquipPosition.Gloves    => "장갑",
                _                       => "없음"
            };

            bool isWeaponSlot = slot == EquipPosition.RightHand || slot == EquipPosition.LeftHand;
            if (isWeaponSlot && weaponType != WeaponType.NoWeapon)
                return $"{baseName} ({weaponType.ToDisplayString()})";

            return baseName;
        }
    }
}
