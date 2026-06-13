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

        /// <summary>
        /// 기존 손튜닝 값을 보존하면서 약/강/스킬 공격에 Move(이동 후딜 캔슬) 플래그만 OR로 추가한다.
        /// MigrateAll(덮어쓰기)과 달리 비파괴적이라 세션에 걸쳐 다듬어온 interruptActions를 날리지 않는다.
        /// 차지/이동공격(대시·점프)은 커밋감 유지를 위해 기본 제외(필요 시 인스펙터에서 개별 부여).
        /// </summary>
        [MenuItem("UPlayGround/게임플레이/전투/PlayerAttackData 이동 후딜 캔슬(Move) 플래그 추가", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.GameplayCombat + 21)]
        public static void AddMoveCancelFlag()
        {
            string[] guids = AssetDatabase.FindAssets("t:PlayerAttackDataSO");
            int changed = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<PlayerAttackDataSO>(path);
                if (data == null)
                    continue;

                Undo.RecordObject(data, "Add Move Cancel Flag");

                bool localChanged = false;
                localChanged |= OrFlagToList(data.liteComboAttackList, PlayerInterruptAction.Move);
                localChanged |= OrFlagToList(data.heavyComboAttackList, PlayerInterruptAction.Move);
                localChanged |= OrFlagToList(data.skillAttackList, PlayerInterruptAction.Move);

                if (localChanged)
                {
                    EditorUtility.SetDirty(data);
                    changed++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PlayerAttackDataInterruptMigration] {changed}/{guids.Length}개 PlayerAttackDataSO에 Move 플래그 추가 완료");
        }

        private static bool OrFlagToList(System.Collections.Generic.List<PlayerAttackInfo> attacks, PlayerInterruptAction flag)
        {
            if (attacks == null)
                return false;

            bool changed = false;
            foreach (PlayerAttackInfo attack in attacks)
            {
                if (attack == null || (attack.interruptActions & flag) != 0)
                    continue;

                attack.interruptActions |= flag;
                changed = true;
            }

            return changed;
        }

        private static bool ApplyDefaults(PlayerAttackDataSO data)
        {
            bool changed = false;

            changed |= ApplyToList(data.liteComboAttackList, LightAttackInterruptActions);
            changed |= ApplyToList(data.heavyComboAttackList, HeavyAttackInterruptActions);
            changed |= ApplyToList(data.skillAttackList, SkillAttackInterruptActions);
            changed |= ApplyToList(data.dashAttackList, MobilityAttackInterruptActions);
            changed |= ApplyToList(data.jumpAttackList, MobilityAttackInterruptActions);

            // 등장/스왑 공격(단일 필드)도 마스크를 부여한다. 기존엔 이 도구가 리스트만 순회해
            // entry/swap이 enum 기본값 0(None=캔슬 불가)으로 방치 → 스왑 공격 후 후딜 캔슬 불가의 원인이었다.
            // Counter/ParryCounter는 '커밋' 설계라 일부러 건드리지 않는다.
            changed |= ApplyToAttack(data.entryAttack, EntrySwapInterruptActions);
            changed |= ApplyToAttack(data.entryAttackVsGroggy, EntrySwapInterruptActions);
            changed |= ApplyToAttack(data.entryAttackVsAirborne, EntrySwapInterruptActions);
            changed |= ApplyToAttack(data.swapEvadeCounterAttack, EntrySwapInterruptActions);
            changed |= ApplyToAttack(data.swapSpecialAttack, EntrySwapInterruptActions);

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

        private static bool ApplyToAttack(PlayerAttackInfo attack, PlayerInterruptAction value)
        {
            if (attack == null)
                return false;

            return SetIfDifferent(ref attack.interruptActions, value);
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

        // 등장/스왑 공격: 리커버리에서 방어·이동 이탈 허용(스왑 후 후딜 단축).
        // 공격타입 제외 — 연타 버퍼 입력이 윈드업에서 특수 공격을 선점·취소하는 회귀 방지.
        private const PlayerInterruptAction EntrySwapInterruptActions =
            PlayerInterruptAction.Dodge |
            PlayerInterruptAction.Jump |
            PlayerInterruptAction.Dash |
            PlayerInterruptAction.Guard |
            PlayerInterruptAction.Move;

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
