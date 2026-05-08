using System;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace Game.Editor.P09Builder
{
    [Serializable]
    public class StatsAssignment
    {
        // ---------- Enemy ----------
        public bool createNewStats = true;
        public ScriptableObject existingStatsSo;
        public float defaultHp = 100f;
        public float defaultWalkSpeed = 2f;
        public float defaultRunSpeed = 4f;
        public float defaultDetectionRadius = 10f;
        public MonsterActorGrade grade = MonsterActorGrade.Normal;

        public bool createNewBehavior = true;
        public ScriptableObject existingBehaviorSo;
        public float optimalCombatDistance = 2.5f;

        public ScriptableObject attackDataSo;
        public EnemyCombatStyle combatStyle = EnemyCombatStyle.Melee;

        public bool recruitableOnDefeat = false;
        public CharacterActorType recruitableAs = CharacterActorType.None;

        // ---------- Player ----------
        public ScriptableObject playerAttackDataSo;
        public bool addToStartingParty = false;
        public int partyOrder = 0;

        // ---------- NPC ----------
        public ScriptableObject dialogueSo;
        public float wanderRadius = 5f;
    }
}
