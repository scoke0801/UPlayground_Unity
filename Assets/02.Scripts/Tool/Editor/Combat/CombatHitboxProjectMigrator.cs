#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UPlayGround.Combat;
using UPlayGround.Component;

namespace UPlayGround.Tool.Editor.Combat
{
    /// <summary>
    /// Legacy 구형/각도 판정 제거 후 프로젝트 프리팹에 부착형 HitBox를 일괄 생성한다.
    /// 외부 에셋과 FBX는 수정하지 않고 프로젝트 전용 Weapon/Actor Prefab만 처리한다.
    /// </summary>
    public static class CombatHitboxProjectMigrator
    {
        private const string WeaponRoot = "Assets/03.Prefabs/Weapon";
        private const string ActorRoot = "Assets/03.Prefabs/Actor";
        [MenuItem("UPlayGround/게임플레이/전투/도구/HitBox 마이그레이션/전체 부착형 HitBox 생성", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.GameplayCombatTools + 50)]
        public static void MigrateAll()
        {
            if (!EditorUtility.DisplayDialog(
                    "부착형 HitBox 마이그레이션",
                    "Weapon 및 전투 Actor Prefab에 HitBox를 자동 생성합니다.\n계속하시겠습니까?",
                    "실행",
                    "취소"))
            {
                return;
            }

            MigrateAllInternal(showDialog: true);
        }

        public static void MigrateAllBatch()
        {
            MigrateAllInternal(showDialog: false);
        }

        private static void MigrateAllInternal(bool showDialog)
        {
            int migrated = 0;
            int skipped = 0;
            var messages = new List<string>();

            try
            {
                string[] weaponPaths = FindWeaponPrefabPaths();
                string[] actorPaths = FindPrefabPaths(ActorRoot);
                int total = weaponPaths.Length + actorPaths.Length;
                int index = 0;

                foreach (string path in weaponPaths)
                {
                    EditorUtility.DisplayProgressBar("Combat HitBox 마이그레이션", path, index++ / (float)Mathf.Max(1, total));
                    if (MigratePrefab(path, CombatHitboxSetupMode.WeaponAutoFit, messages))
                        migrated++;
                    else
                        skipped++;
                }

                foreach (string path in actorPaths)
                {
                    EditorUtility.DisplayProgressBar("Combat HitBox 마이그레이션", path, index++ / (float)Mathf.Max(1, total));
                    GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                    if (prefab == null
                        || prefab.GetComponentInChildren<GameActor>(true) == null
                        && prefab.GetComponentInChildren<CharacterModelData>(true) == null)
                    {
                        skipped++;
                        continue;
                    }

                    Animator animator = prefab.GetComponentInChildren<Animator>(true);
                    CombatHitboxSetupMode mode = animator != null && animator.isHuman
                        ? CombatHitboxSetupMode.HumanoidBodySetup
                        : CombatHitboxSetupMode.GenericBodySetup;
                    if (MigratePrefab(path, mode, messages))
                        migrated++;
                    else
                        skipped++;
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            Debug.Log(
                $"[CombatHitboxMigration] 완료. 변경 Prefab={migrated}, 건너뜀={skipped}\n" +
                string.Join("\n", messages.Take(30)));
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "마이그레이션 완료",
                    $"변경 Prefab: {migrated}\n건너뜀: {skipped}\n상세 내용은 Console을 확인하세요.",
                    "확인");
            }
        }

        [MenuItem("UPlayGround/게임플레이/전투/도구/HitBox 마이그레이션/마이그레이션 결과 검증", priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.GameplayCombatTools + 51)]
        public static void ValidateMigrated()
        {
            int errors = 0;
            int warnings = 0;
            foreach (string path in FindPrefabPaths(ActorRoot).Concat(FindWeaponPrefabPaths()).Distinct())
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null)
                    continue;

                foreach (string issue in CombatHitboxSetupValidator.Validate(prefab))
                {
                    if (issue.StartsWith("Error:", StringComparison.Ordinal))
                    {
                        errors++;
                        Debug.LogError($"[{path}] {issue}", prefab);
                    }
                    else if (issue.StartsWith("Warning:", StringComparison.Ordinal))
                    {
                        warnings++;
                        Debug.LogWarning($"[{path}] {issue}", prefab);
                    }
                }
            }

            EditorUtility.DisplayDialog(
                "HitBox 검증 결과",
                $"Error: {errors}\nWarning: {warnings}",
                "확인");
        }

        private static bool MigratePrefab(
            string path,
            CombatHitboxSetupMode mode,
            ICollection<string> messages)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            try
            {
                int before = contents.GetComponentsInChildren<CombatHitbox>(true).Length;
                CombatHitboxSetupResult result =
                    CombatHitboxAutoFitter.Apply(contents, mode, profile: null, forceRefit: false);
                int after = contents.GetComponentsInChildren<CombatHitbox>(true).Length;
                if (after <= before && result.Updated == 0)
                    return false;

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                messages.Add($"{path}: {before} → {after}");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static string[] FindPrefabPaths(string root)
        {
            if (!AssetDatabase.IsValidFolder(root))
                return Array.Empty<string>();

            return AssetDatabase.FindAssets("t:Prefab", new[] { root })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToArray();
        }

        private static string[] FindWeaponPrefabPaths()
        {
            var paths = new HashSet<string>(FindPrefabPaths(WeaponRoot));
            foreach (string guid in AssetDatabase.FindAssets("t:EquipmentSO"))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                EquipmentSO equipment = AssetDatabase.LoadAssetAtPath<EquipmentSO>(assetPath);
                if (equipment == null || equipment.equipmentPrefab == null)
                    continue;

                string prefabPath = AssetDatabase.GetAssetPath(equipment.equipmentPrefab);
                if (!string.IsNullOrWhiteSpace(prefabPath)
                    && prefabPath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    paths.Add(prefabPath);
                }
            }
            return paths.ToArray();
        }
    }
}
#endif
