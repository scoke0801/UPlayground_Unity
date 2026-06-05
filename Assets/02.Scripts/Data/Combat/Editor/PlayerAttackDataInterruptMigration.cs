#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Editor
{
    public static class PlayerAttackDataInterruptMigration
    {
        [MenuItem("UPlayGround/게임플레이/전투/PlayerAttackData 캔슬 기본값 마이그레이션", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.GameplayCombat + 20)]
        public static void MigrateAll()
        {
            string[] guids = AssetDatabase.FindAssets("t:PlayerAttackDataSO");
            int changed = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<PlayerAttackDataSO>(path);
                if (data == null)
                    continue;

                Undo.RecordObject(data, "Migrate PlayerAttackData Interrupt Defaults");
                if (ApplyDefaults(data))
                {
                    EditorUtility.SetDirty(data);
                    changed++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PlayerAttackDataInterruptMigration] {changed}/{guids.Length}개 PlayerAttackDataSO 갱신 완료");
        }

        private static bool ApplyDefaults(PlayerAttackDataSO data)
        {
            bool changed = false;

            changed |= ApplyToList(data.liteComboAttackList, LightAttackInterruptActions);
            changed |= ApplyToList(data.heavyComboAttackList, HeavyAttackInterruptActions);
            changed |= ApplyToList(data.skillAttackList, SkillAttackInterruptActions);
            changed |= ApplyToList(data.dashAttackList, MobilityAttackInterruptActions);
            changed |= ApplyToList(data.jumpAttackList, MobilityAttackInterruptActions);

            changed |= SetIfDifferent(ref data.chargeInterruptActions, ChargeHoldInterruptActions);

            if (data.chargeStages != null)
            {
                int stageCount = data.chargeStages.Count;
                for (int i = 0; i < stageCount; i++)
                {
                    ChargeStageData stage = data.chargeStages[i];
                    if (stage == null)
                        continue;

                    PlayerInterruptAction value = i >= stageCount - 1
                        ? ChargeFinalStageInterruptActions
                        : ChargeReleaseInterruptActions;
                    changed |= SetIfDifferent(ref stage.interruptActions, value);
                }
            }

            return changed;
        }

        private static bool ApplyToList(System.Collections.Generic.List<PlayerAttackInfo> attacks, PlayerInterruptAction value)
        {
            if (attacks == null)
                return false;

            bool changed = false;
            foreach (PlayerAttackInfo attack in attacks)
            {
                if (attack == null)
                    continue;

                changed |= SetIfDifferent(ref attack.interruptActions, value);
            }

            return changed;
        }

        private static bool SetIfDifferent(ref PlayerInterruptAction target, PlayerInterruptAction value)
        {
            if (target == value)
                return false;

            target = value;
            return true;
        }

        private const PlayerInterruptAction LightAttackInterruptActions =
            PlayerInterruptAction.Dodge |
            PlayerInterruptAction.Jump |
            PlayerInterruptAction.Dash |
            PlayerInterruptAction.Guard |
            PlayerInterruptAction.HeavyAttack |
            PlayerInterruptAction.Skill;

        private const PlayerInterruptAction HeavyAttackInterruptActions =
            PlayerInterruptAction.Dodge |
            PlayerInterruptAction.Dash |
            PlayerInterruptAction.Guard |
            PlayerInterruptAction.LightAttack |
            PlayerInterruptAction.Skill;

        private const PlayerInterruptAction SkillAttackInterruptActions =
            PlayerInterruptAction.Dodge |
            PlayerInterruptAction.Dash |
            PlayerInterruptAction.Guard |
            PlayerInterruptAction.LightAttack |
            PlayerInterruptAction.HeavyAttack;

        private const PlayerInterruptAction MobilityAttackInterruptActions =
            PlayerInterruptAction.Dodge |
            PlayerInterruptAction.Dash |
            PlayerInterruptAction.Skill;

        private const PlayerInterruptAction ChargeHoldInterruptActions =
            PlayerInterruptAction.Dodge |
            PlayerInterruptAction.Dash;

        private const PlayerInterruptAction ChargeReleaseInterruptActions =
            PlayerInterruptAction.Dodge |
            PlayerInterruptAction.Dash |
            PlayerInterruptAction.Skill;

        private const PlayerInterruptAction ChargeFinalStageInterruptActions =
            PlayerInterruptAction.Dodge;
    }
}
#endif
