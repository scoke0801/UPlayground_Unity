#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UPlayGround.Data;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Tool.Editor.Combat
{
    public enum CombatValidationSeverity
    {
        Info,
        Warning,
        Error,
    }

    public readonly struct CombatValidationIssue
    {
        public readonly CombatValidationSeverity Severity;
        public readonly string AssetPath;
        public readonly string Context;
        public readonly string Message;

        public CombatValidationIssue(
            CombatValidationSeverity severity,
            string assetPath,
            string context,
            string message)
        {
            Severity = severity;
            AssetPath = assetPath;
            Context = context;
            Message = message;
        }
    }

    public static class CombatDataValidator
    {
        public static List<CombatValidationIssue> ValidateAll()
        {
            var issues = new List<CombatValidationIssue>();
            ValidatePlayerAttackData(issues);
            ValidateEnemyAttackData(issues);
            return issues;
        }

        private static void ValidatePlayerAttackData(List<CombatValidationIssue> issues)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:PlayerAttackDataSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<PlayerAttackDataSO>(path);
                if (asset == null)
                    continue;

                ValidatePlayerList(issues, path, "liteComboAttackList", asset.liteComboAttackList);
                ValidatePlayerList(issues, path, "heavyComboAttackList", asset.heavyComboAttackList);
                ValidatePlayerList(issues, path, "jumpAttackList", asset.jumpAttackList);
                ValidatePlayerList(issues, path, "dashAttackList", asset.dashAttackList);
                ValidatePlayerList(issues, path, "skillAttackList", asset.skillAttackList);
                ValidatePlayerAttack(issues, path, "counterAttack", asset.counterAttack);
                ValidatePlayerAttack(issues, path, "entryAttack", asset.entryAttack);
                ValidatePlayerAttack(issues, path, "swapSpecialAttack", asset.swapSpecialAttack);
                ValidatePlayerAttack(issues, path, "swapEvadeCounterAttack", asset.swapEvadeCounterAttack);
                ValidatePlayerAttack(issues, path, "parryCounterAttack", asset.parryCounterAttack);
            }
        }

        private static void ValidateEnemyAttackData(List<CombatValidationIssue> issues)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:EnemyAttackDataSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<EnemyAttackDataSO>(path);
                if (asset == null)
                    continue;

                if (asset.skills == null || asset.skills.Count == 0)
                {
                    AddIssue(issues, CombatValidationSeverity.Warning, path, "skills", "사용 가능한 몬스터 스킬이 없습니다.");
                    continue;
                }

                for (int i = 0; i < asset.skills.Count; i++)
                    ValidateEnemyAttack(issues, path, $"skills[{i}]", asset.skills[i]);
            }
        }

        private static void ValidatePlayerList(
            List<CombatValidationIssue> issues,
            string path,
            string context,
            List<PlayerAttackInfo> attacks)
        {
            if (attacks == null)
                return;

            for (int i = 0; i < attacks.Count; i++)
                ValidatePlayerAttack(issues, path, $"{context}[{i}]", attacks[i]);
        }

        private static void ValidatePlayerAttack(
            List<CombatValidationIssue> issues,
            string path,
            string context,
            PlayerAttackInfo attack)
        {
            if (attack == null)
                return;

            ValidateAttackInfoBase(issues, path, context, attack.baseInfo, requireMeleeHitPhase: true);
        }

        private static void ValidateEnemyAttack(
            List<CombatValidationIssue> issues,
            string path,
            string context,
            EnemyAttackInfo attack)
        {
            if (attack == null)
            {
                AddIssue(issues, CombatValidationSeverity.Error, path, context, "EnemyAttackInfo가 null입니다.");
                return;
            }

            ValidateAttackInfoBase(
                issues,
                path,
                context,
                attack.baseInfo,
                attack.skillType == SkillType.Attack);

            if (attack.selectionWeight <= 0f)
                AddIssue(issues, CombatValidationSeverity.Warning, path, context, "selectionWeight가 0 이하라 선택되지 않을 수 있습니다.");
        }

        private static void ValidateAttackInfoBase(
            List<CombatValidationIssue> issues,
            string path,
            string context,
            AttackInfoBase baseInfo,
            bool requireMeleeHitPhase)
        {
            if (baseInfo == null)
            {
                AddIssue(issues, CombatValidationSeverity.Error, path, context, "baseInfo가 null입니다.");
                return;
            }

            if (baseInfo.animKey == AnimKey.None)
                AddIssue(issues, CombatValidationSeverity.Error, path, context, "animKey가 None입니다.");

            if (baseInfo.hitPhases == null || baseInfo.hitPhases.Count == 0)
            {
                if (requireMeleeHitPhase)
                    AddIssue(issues, CombatValidationSeverity.Error, path, context, "hitPhases가 비어 있습니다.");
                return;
            }

            for (int i = 0; i < baseInfo.hitPhases.Count; i++)
            {
                HitPhaseData phase = baseInfo.hitPhases[i];
                if (phase == null)
                {
                    AddIssue(issues, CombatValidationSeverity.Error, path, $"{context}.hitPhases[{i}]", "HitPhaseData가 null입니다.");
                    continue;
                }

                if (phase.attackRadius <= 0f && baseInfo.attackType == AttackType.Melee)
                    AddIssue(issues, CombatValidationSeverity.Warning, path, $"{context}.hitPhases[{i}]", "근접 공격의 attackRadius가 0 이하입니다.");
                if (phase.damage < 0f)
                    AddIssue(issues, CombatValidationSeverity.Error, path, $"{context}.hitPhases[{i}]", "damage가 음수입니다.");
            }
        }

        private static void AddIssue(
            List<CombatValidationIssue> issues,
            CombatValidationSeverity severity,
            string path,
            string context,
            string message)
        {
            issues.Add(new CombatValidationIssue(severity, path, context, message));
        }
    }
}
#endif
