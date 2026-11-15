using System;

namespace UPlayGround
{
    public partial class PlayerActor
    {
        /// <summary>성립한 상호작용·채집만 동행 활동으로 알린다.</summary>
        public event Action PetBondActivity;
        public event Action PetBondCombatAction;
        public void NotifyPetBondActivity() => PetBondActivity?.Invoke();
        internal void NotifyPetBondCombatAction() => PetBondCombatAction?.Invoke();
    }
}
