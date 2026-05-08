using System.Collections.Generic;

namespace Game.Editor.P09Builder
{
    public enum BuilderArmorSlot
    {
        Head = 0,
        Chest = 1,
        Arm = 2,
        Waist = 3,
        Leg = 4
    }

    public static class BuilderArmorSlotExtensions
    {
        private static readonly BuilderArmorSlot[] _all =
        {
            BuilderArmorSlot.Head,
            BuilderArmorSlot.Chest,
            BuilderArmorSlot.Arm,
            BuilderArmorSlot.Waist,
            BuilderArmorSlot.Leg
        };

        public static IEnumerable<BuilderArmorSlot> All => _all;
    }
}
