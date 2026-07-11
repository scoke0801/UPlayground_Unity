using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;

namespace UPlayGround.Components
{
    public static class WeaponAttachmentResolverUtility
    {
        public static string NormalizeName(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            return value
                .Replace(" ", string.Empty)
                .Replace("_", string.Empty)
                .Replace("-", string.Empty)
                .Replace("(", string.Empty)
                .Replace(")", string.Empty)
                .ToLowerInvariant();
        }
    }

    public static class WeaponAttachmentResolver
    {
        public static Transform FindWeaponRoot(Transform root)
        {
            if (root == null)
                return null;

            Transform directWeaponRoot = FindDirectWeaponRoot(root, requireConstraint: true);
            if (directWeaponRoot != null)
                return directWeaponRoot;

            directWeaponRoot = FindDirectWeaponRoot(root, requireConstraint: false);
            if (directWeaponRoot != null)
                return directWeaponRoot;

            Transform recursiveWeaponRoot = FindChildRecursive(root, "Weapon", requireConstraint: true);
            if (recursiveWeaponRoot != null)
                return recursiveWeaponRoot;

            return FindChildRecursive(root, "Weapon", requireConstraint: false);
        }

        public static void CollectBindings(
            Transform ownerRoot,
            Transform weaponRoot,
            List<ParentConstraint> constraints,
            List<WeaponSocketBinding> bindings)
        {
            constraints.Clear();
            bindings.Clear();

            if (ownerRoot != null)
                ownerRoot.GetComponentsInChildren(true, bindings);

            if (weaponRoot != null)
                weaponRoot.GetComponentsInChildren(true, constraints);
        }

        public static ParentConstraint Resolve(
            EquipPosition equipPosition,
            WeaponType weaponType,
            Transform ownerRoot,
            Transform weaponRoot,
            List<ParentConstraint> constraints,
            List<WeaponSocketBinding> bindings,
            IReadOnlyList<WeaponDefinitionSO> definitions,
            UnityEngine.Object logContext = null,
            bool logFailure = true)
        {
            if (weaponType == WeaponType.NoWeapon)
                return null;

            ParentConstraint bindingConstraint = ResolveFromBinding(equipPosition, weaponType, bindings, definitions);
            if (bindingConstraint != null)
                return bindingConstraint;

            ParentConstraint nameConstraint = ResolveFromName(equipPosition, weaponType, weaponRoot, constraints, definitions);
            if (nameConstraint != null)
                return nameConstraint;

            if (logFailure)
                LogResolveFailure(equipPosition, weaponType, ownerRoot, weaponRoot, constraints, bindings, logContext);
            return null;
        }

        public static bool IsPairedWeaponType(WeaponType weaponType, IReadOnlyList<WeaponDefinitionSO> definitions)
        {
            WeaponDefinitionSO definition = FindDefinition(weaponType, definitions);
            if (definition != null)
                return definition.equipStyle == WeaponEquipStyle.PairedBothHands;

            return weaponType == WeaponType.DualBlade;
        }

        public static WeaponDefinitionSO FindDefinition(WeaponType weaponType, IReadOnlyList<WeaponDefinitionSO> definitions)
        {
            if (definitions == null)
                return null;

            for (int i = 0; i < definitions.Count; i++)
            {
                WeaponDefinitionSO definition = definitions[i];
                if (definition != null && definition.weaponType == weaponType)
                    return definition;
            }

            return null;
        }

        private static ParentConstraint ResolveFromBinding(
            EquipPosition equipPosition,
            WeaponType weaponType,
            List<WeaponSocketBinding> bindings,
            IReadOnlyList<WeaponDefinitionSO> definitions)
        {
            if (bindings == null || bindings.Count == 0)
                return null;

            string normalizedWeaponName = NormalizeWeaponName(weaponType, definitions);
            ParentConstraint fallback = null;

            for (int i = 0; i < bindings.Count; i++)
            {
                WeaponSocketBinding binding = bindings[i];
                if (binding == null)
                    continue;

                ParentConstraint constraint = binding.Constraint;
                if (constraint == null)
                    continue;

                if (binding.Matches(equipPosition, weaponType, normalizedWeaponName))
                    return constraint;

                if (fallback == null &&
                    binding.equipPosition == equipPosition &&
                    binding.matchAnyWeaponType)
                {
                    fallback = constraint;
                }
            }

            return fallback;
        }

        private static ParentConstraint ResolveFromName(
            EquipPosition equipPosition,
            WeaponType weaponType,
            Transform weaponRoot,
            List<ParentConstraint> constraints,
            IReadOnlyList<WeaponDefinitionSO> definitions)
        {
            if (constraints == null || constraints.Count == 0)
                return null;

            ParentConstraint alias = null;
            ParentConstraint fallback = null;

            for (int i = 0; i < constraints.Count; i++)
            {
                ParentConstraint constraint = constraints[i];
                if (constraint == null)
                    continue;

                // constraint 이름이 weapon type과 정확히 매칭되면 GuessEquipPosition 추정을 우회한다.
                // source bone 이름(예: "Hand_L")이 합쳐져 "handl"로 LeftHand 오판되는 케이스 대응.
                if (MatchesExactWeaponType(constraint, weaponType))
                    return constraint;

                if (GuessEquipPosition(constraint, weaponType) != equipPosition)
                    continue;

                if (alias == null && MatchesWeaponAlias(constraint, weaponType, definitions))
                    alias = constraint;

                if (fallback == null && IsGenericWeaponConstraint(constraint, weaponRoot))
                    fallback = constraint;
            }

            return alias ?? fallback ?? GetSingleConstraintForPosition(equipPosition, weaponType, constraints);
        }

        private static Transform FindDirectWeaponRoot(Transform root, bool requireConstraint)
        {
            foreach (Transform child in root)
            {
                if (child.name != "Weapon")
                    continue;

                if (!requireConstraint || child.GetComponentInChildren<ParentConstraint>(true) != null)
                    return child;
            }

            return null;
        }

        private static Transform FindChildRecursive(Transform root, string childName, bool requireConstraint)
        {
            if (root == null)
                return null;

            if (root.name == childName &&
                (!requireConstraint || root.GetComponentInChildren<ParentConstraint>(true) != null))
            {
                return root;
            }

            foreach (Transform child in root)
            {
                Transform found = FindChildRecursive(child, childName, requireConstraint);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static EquipPosition GuessEquipPosition(ParentConstraint constraint, WeaponType weaponType)
        {
            if (weaponType == WeaponType.Arrow)
                return EquipPosition.LeftHand;

            string normalizedName = WeaponAttachmentResolverUtility.NormalizeName(GetConstraintSearchName(constraint));
            if (normalizedName.Contains("left") || normalizedName.Contains("handl") || normalizedName.EndsWith("l"))
                return EquipPosition.LeftHand;

            return EquipPosition.RightHand;
        }

        private static string GetConstraintSearchName(ParentConstraint constraint)
        {
            string searchName = constraint.name;
            for (int i = 0; i < constraint.sourceCount; i++)
            {
                Transform sourceTransform = constraint.GetSource(i).sourceTransform;
                if (sourceTransform != null)
                    searchName += sourceTransform.name;
            }

            return searchName;
        }

        private static bool MatchesExactWeaponType(ParentConstraint constraint, WeaponType weaponType)
        {
            string constraintName = WeaponAttachmentResolverUtility.NormalizeName(constraint.name);
            string typeName = WeaponAttachmentResolverUtility.NormalizeName(weaponType.ToString());

            return constraintName.Contains(typeName);
        }

        private static bool MatchesWeaponAlias(
            ParentConstraint constraint,
            WeaponType weaponType,
            IReadOnlyList<WeaponDefinitionSO> definitions)
        {
            string constraintName = WeaponAttachmentResolverUtility.NormalizeName(constraint.name);
            WeaponDefinitionSO definition = FindDefinition(weaponType, definitions);
            if (definition != null && definition.MatchesAlias(constraintName))
                return true;

            return weaponType switch
            {
                WeaponType.Sword => constraintName.Contains("sword"),
                WeaponType.SwordShield => constraintName.Contains("sword") || constraintName.Contains("shield"),
                WeaponType.GreatSword => constraintName.Contains("greatsword") || constraintName.Contains("claymore"),
                WeaponType.Staff => constraintName.Contains("staff"),
                WeaponType.Bow => constraintName.Contains("bow"),
                WeaponType.Arrow => constraintName.Contains("arrow"),
                WeaponType.Katana => constraintName.Contains("sword"),
                WeaponType.DoubleAxe => constraintName.Contains("axe"),
                WeaponType.Whip => constraintName.Contains("whip"),
                WeaponType.Spear => constraintName.Contains("spear") || constraintName.Contains("lance"),
                WeaponType.DualBlade => constraintName.Contains("dualblade") ||
                                        constraintName.Contains("doubleblade") ||
                                        constraintName.Contains("blade") ||
                                        constraintName.Contains("sword"),
                _ => false,
            };
        }

        private static bool IsGenericWeaponConstraint(ParentConstraint constraint, Transform weaponRoot)
        {
            return constraint.transform == weaponRoot ||
                   WeaponAttachmentResolverUtility.NormalizeName(constraint.name) == "weapon";
        }

        private static ParentConstraint GetSingleConstraintForPosition(
            EquipPosition equipPosition,
            WeaponType weaponType,
            List<ParentConstraint> constraints)
        {
            ParentConstraint result = null;
            for (int i = 0; i < constraints.Count; i++)
            {
                ParentConstraint constraint = constraints[i];
                if (constraint == null)
                    continue;

                if (GuessEquipPosition(constraint, weaponType) != equipPosition)
                    continue;

                if (result != null)
                    return null;

                result = constraint;
            }

            return result;
        }

        private static string NormalizeWeaponName(WeaponType weaponType, IReadOnlyList<WeaponDefinitionSO> definitions)
        {
            string result = WeaponAttachmentResolverUtility.NormalizeName(weaponType.ToString());
            WeaponDefinitionSO definition = FindDefinition(weaponType, definitions);
            if (definition == null)
                return result;

            for (int i = 0; i < definition.constraintAliases.Count; i++)
            {
                string alias = WeaponAttachmentResolverUtility.NormalizeName(definition.constraintAliases[i]?.value);
                if (!string.IsNullOrEmpty(alias))
                    result += alias;
            }

            return result;
        }

        private static void LogResolveFailure(
            EquipPosition equipPosition,
            WeaponType weaponType,
            Transform ownerRoot,
            Transform weaponRoot,
            List<ParentConstraint> constraints,
            List<WeaponSocketBinding> bindings,
            UnityEngine.Object logContext)
        {
            string reason;
            if (ownerRoot == null)
                reason = "ownerRoot가 null입니다.";
            else if (weaponRoot == null)
                reason = "'Weapon' 루트를 찾지 못했습니다.";
            else if ((bindings == null || bindings.Count == 0) && (constraints == null || constraints.Count == 0))
                reason = "'Weapon' 루트 하위에 WeaponSocketBinding 또는 ParentConstraint 후보가 없습니다.";
            else
                reason = "무기 타입/장착 위치에 맞는 binding 또는 constraint 후보를 찾지 못했습니다.";

            Debug.LogWarning(
                $"[WeaponAttachmentResolver] {equipPosition}/{weaponType} 매핑 실패: {reason} " +
                $"bindingCount={bindings?.Count ?? 0}, constraintCount={constraints?.Count ?? 0}",
                logContext);
        }
    }
}
