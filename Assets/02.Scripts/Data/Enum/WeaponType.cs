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
}
