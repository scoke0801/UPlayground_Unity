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

        /// <summary>
        /// 기존 손튜닝 값을 보존하면서 등장/스왑 공격(단일 필드)에 캔슬 마스크를 OR로 추가한다.
        /// MigrateAll(덮어쓰기)과 달리 비파괴적이라 세션에 걸쳐 다듬어온 interruptActions를 날리지 않는다.
        /// 리스트만 순회하는 AddMoveCancelFlag가 닿지 못하는 entry/swap/반격 단일 필드가 대상으로,
        /// 이 필드들이 enum 기본값 0(None=캔슬 불가)으로 방치돼 후딜 캔슬이 막히는 문제를 해소한다.
        /// Counter/ParryCounter도 후딜 캔슬을 부여한다(공격타입은 제외 — 커밋감 유지).
        /// </summary>
        [MenuItem("UPlayGround/게임플레이/전투/PlayerAttackData 등장·스왑·반격 공격 캔슬 플래그 추가", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.GameplayCombat + 22)]
        public static void AddEntrySwapCancelFlags()
        {
            string[] guids = AssetDatabase.FindAssets("t:PlayerAttackDataSO");
            int changed = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var data = AssetDatabase.LoadAssetAtPath<PlayerAttackDataSO>(path);
                if (data == null)
                    continue;

                Undo.RecordObject(data, "Add Entry/Swap Cancel Flags");

                bool localChanged = false;
                localChanged |= OrFlagToAttack(data.entryAttack, EntrySwapInterruptActions);
                localChanged |= OrFlagToAttack(data.entryAttackVsGroggy, EntrySwapInterruptActions);
                localChanged |= OrFlagToAttack(data.entryAttackVsAirborne, EntrySwapInterruptActions);
                localChanged |= OrFlagToAttack(data.swapEvadeCounterAttack, EntrySwapInterruptActions);
                localChanged |= OrFlagToAttack(data.swapSpecialAttack, EntrySwapInterruptActions);
                localChanged |= OrFlagToAttack(data.counterAttack, CounterInterruptActions);
                localChanged |= OrFlagToAttack(data.parryCounterAttack, CounterInterruptActions);

                if (localChanged)
                {
                    EditorUtility.SetDirty(data);
                    changed++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[PlayerAttackDataInterruptMigration] {changed}/{guids.Length}개 PlayerAttackDataSO에 등장·스왑 캔슬 플래그 추가 완료");
        }

        private static bool OrFlagToAttack(PlayerAttackInfo attack, PlayerInterruptAction flags)
        {
            // null(미설정 슬롯)은 건너뛰고, 이미 모든 플래그가 켜져 있으면 변경 없음으로 처리한다.
            if (attack == null || (attack.interruptActions & flags) == flags)
                return false;

            attack.interruptActions |= flags;
            return true;
        }

        private static bool ApplyDefaults(PlayerAttackDataSO data)
        {
            bool changed = false;

            changed |= ApplyToList(data.liteComboAttackList, LightAttackInterruptActions);
            changed |= ApplyToList(data.heavyComboAttackList, HeavyAttackInterruptActions);
            changed |= ApplyToList(data.skillAttackList, SkillAttackInterruptActions);
            changed |= ApplyToList(data.dashAttackList, MobilityAttackInterruptActions);
            changed |= ApplyToList(data.jumpAttackList, MobilityAttackInterruptActions);

            // 등장/스왑/반격 공격(단일 필드)도 마스크를 부여한다. 기존엔 이 도구가 리스트만 순회해
            // entry/swap/counter가 enum 기본값 0(None=캔슬 불가)으로 방치 → 후딜 캔슬 불가의 원인이었다.
            // Counter/ParryCounter도 후딜 캔슬을 부여한다(CounterInterruptActions, 공격타입 제외).
            changed |= ApplyToAttack(data.entryAttack, EntrySwapInterruptActions);
            changed |= ApplyToAttack(data.entryAttackVsGroggy, EntrySwapInterruptActions);
            changed |= ApplyToAttack(data.entryAttackVsAirborne, EntrySwapInterruptActions);
            changed |= ApplyToAttack(data.swapEvadeCounterAttack, EntrySwapInterruptActions);
            changed |= ApplyToAttack(data.swapSpecialAttack, EntrySwapInterruptActions);
            changed |= ApplyToAttack(data.counterAttack, CounterInterruptActions);
            changed |= ApplyToAttack(data.parryCounterAttack, CounterInterruptActions);

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

        // 퍼펙트가드/패리 반격: 현재는 등장·스왑과 동일(=143, Move 포함 후딜 캔슬 + 방어·회피 이탈).
        // 공격타입은 동일하게 제외(반격→연타 선입력 선점 방지). 반격만 따로 조정하려면 여기만 바꾼다.
        // 주의: Guard(8) 포함 — 반격은 가드를 쥔 채 발동되므로 액티브 히트 종료 후(캔슬창 OPEN)
        // 가드 입력이 남아 있으면 리커버리가 즉시 가드로 캔슬될 수 있다. 후딜이 너무 짧게 느껴지면
        // Guard를 빼서(=135) 가드 이탈을 막을 수 있다.
        private const PlayerInterruptAction CounterInterruptActions = EntrySwapInterruptActions;

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
