using System.Collections.Generic;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data
{
    [CreateAssetMenu(fileName = "CombatCameraProfileDatabase", menuName = "UPlayGround/카메라/Combat Profile Database")]
    public class CombatCameraProfileDatabaseSO : ScriptableObject
    {
        public const string AddressableKey = "CombatCameraProfileDatabase";

        public List<CombatCameraProfileSO> profiles = new List<CombatCameraProfileSO>();

        public CombatCameraProfileSO GetProfile(CombatCameraIntentType intentType)
            => GetProfile(intentType, null, null);

        public CombatCameraProfileSO GetProfile(CombatCameraIntentType intentType, Transform attacker, Transform victim)
        {
            if (profiles == null)
                return null;

            bool hasAttackerGrade = TryGetMonsterGrade(attacker, out MonsterActorGrade attackerGrade);
            bool hasVictimGrade = TryGetMonsterGrade(victim, out MonsterActorGrade victimGrade);
            CombatCameraProfileSO selected = null;
            for (int i = 0; i < profiles.Count; i++)
            {
                CombatCameraProfileSO profile = profiles[i];
                if (profile == null || profile.intentType != intentType)
                    continue;

                if (profile.requireAttackerMonsterGrade)
                {
                    if (!hasAttackerGrade || profile.attackerMonsterGrade != attackerGrade)
                        continue;
                }

                if (profile.requireVictimMonsterGrade)
                {
                    if (!hasVictimGrade || profile.victimMonsterGrade != victimGrade)
                        continue;
                }

                if (selected == null || profile.priority > selected.priority)
                    selected = profile;
            }

            return selected;
        }

        private static bool TryGetMonsterGrade(Transform target, out MonsterActorGrade grade)
        {
            if (target != null
                && CameraRuntimeServices.Adapter.TryResolveTarget(
                    target,
                    out CameraTargetInfo cameraTarget)
                && cameraTarget.IsMonster)
            {
                grade = cameraTarget.Grade;
                return true;
            }

            grade = MonsterActorGrade.Normal;
            return false;
        }
    }
}
