using System.Collections.Generic;
using System.Linq;
using P09.Modular.Humanoid.Data;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder
{
    public static class P09WeaponEditPartDataGenerator
    {
        private const string MenuPath = "UPlayGround/캐릭터/P09/무기 EditPartData 생성·갱신";

        private sealed class WeaponTypeSpec
        {
            public readonly string FolderName;
            public readonly string AssetPrefix;
            public readonly string[] RootNames;

            public WeaponTypeSpec(string folderName, string assetPrefix, params string[] rootNames)
            {
                FolderName = folderName;
                AssetPrefix = assetPrefix;
                RootNames = rootNames;
            }
        }

        private static readonly WeaponTypeSpec[] Specs =
        {
            new("SubSword", "SubSwordEditPartData", "SubSword"),
            new("GreatSword", "GreatSwordEditPartData", "GreatSword"),
            new("Spear", "SpearEditPartData", "Spear"),
            new("DualAxe", "DualAxeEditPartData", "DualAxe", "DoubleAxe"),
            new("Whip", "WhipEditPartData", "Whip"),
        };

        [UPlayGround.EditorTools.UPlaygroundTool(MenuPath, priority = 1101)]
        public static void Generate()
        {
            var generated = 0;
            var updated = 0;
            var skipped = new List<string>();

            AssetDatabase.StartAssetEditing();
            try
            {
                PathConfig.EnsureFolderExists($"{PathConfig.CatalogRoot}/Weapon");

                foreach (var spec in Specs)
                {
                    var meshNames = CollectMeshNames(spec);
                    if (meshNames.Count == 0)
                    {
                        skipped.Add(spec.FolderName);
                        continue;
                    }

                    var folder = $"{PathConfig.CatalogRoot}/Weapon/{spec.FolderName}";
                    PathConfig.EnsureFolderExists(folder);

                    for (int i = 0; i < meshNames.Count; i++)
                    {
                        var meshName = meshNames[i];
                        var contentId = ExtractTrailingNumber(meshName, i + 1);
                        var assetPath = $"{folder}/{spec.AssetPrefix}_{contentId:00}.asset";
                        var asset = AssetDatabase.LoadAssetAtPath<WeaponEditPartData>(assetPath);

                        if (asset == null)
                        {
                            asset = ScriptableObject.CreateInstance<WeaponEditPartData>();
                            SetFields(asset, contentId, spec.FolderName, meshName);
                            AssetDatabase.CreateAsset(asset, assetPath);
                            generated++;
                        }
                        else
                        {
                            SetFields(asset, contentId, spec.FolderName, meshName);
                            EditorUtility.SetDirty(asset);
                            updated++;
                        }
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            var skipText = skipped.Count > 0
                ? $"\n스킵: {string.Join(", ", skipped)} 루트를 기반 프리팹에서 찾지 못했습니다."
                : string.Empty;

            Debug.Log($"[P09Builder] WeaponEditPartData 생성 완료. 생성 {generated}개, 갱신 {updated}개.{skipText}");
            EditorUtility.DisplayDialog(
                "P09 WeaponEditPartData 생성",
                $"생성 {generated}개\n갱신 {updated}개{skipText}",
                "확인");
        }

        private static List<string> CollectMeshNames(WeaponTypeSpec spec)
        {
            var result = new HashSet<string>();
            foreach (var prefabPath in GetBasePrefabPaths())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (prefab == null)
                    continue;

                var weaponRoot = FindChild(prefab.transform, "Weapon");
                if (weaponRoot == null)
                    continue;

                foreach (var rootName in spec.RootNames)
                {
                    var typeRoot = FindChild(weaponRoot, rootName);
                    if (typeRoot == null)
                        continue;

                    foreach (var renderer in typeRoot.GetComponentsInChildren<Renderer>(includeInactive: true))
                    {
                        if (renderer == null || renderer.transform == typeRoot)
                            continue;

                        result.Add(renderer.transform.name);
                    }
                }
            }

            return result
                .OrderBy(ExtractTrailingNumberForSort)
                .ThenBy(n => n)
                .ToList();
        }

        private static string[] GetBasePrefabPaths()
        {
            return new[]
            {
                PathConfig.BasePrefabMaleMagica,
                PathConfig.BasePrefabFemaleMagica,
                PathConfig.BasePrefabMaleNoPhysics,
                PathConfig.BasePrefabFemaleNoPhysics,
            };
        }

        private static Transform FindChild(Transform root, string childName)
        {
            if (root == null)
                return null;

            if (root.name == childName)
                return root;

            foreach (Transform child in root)
            {
                var found = FindChild(child, childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void SetFields(WeaponEditPartData asset, int contentId, string displayTypeName, string meshName)
        {
            var serialized = new SerializedObject(asset);
            SetInt(serialized, "_weaponGroupId", 0);
            SetInt(serialized, "_contentId", contentId);
            SetString(serialized, "_displayName", $"{displayTypeName} {contentId:00}");
            SetString(serialized, "_meshName", meshName);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetInt(SerializedObject serialized, string propertyName, int value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
                property.intValue = value;
        }

        private static void SetString(SerializedObject serialized, string propertyName, string value)
        {
            var property = serialized.FindProperty(propertyName);
            if (property != null)
                property.stringValue = value;
        }

        private static int ExtractTrailingNumberForSort(string value)
        {
            return ExtractTrailingNumber(value, int.MaxValue);
        }

        private static int ExtractTrailingNumber(string value, int fallback)
        {
            if (string.IsNullOrEmpty(value))
                return fallback;

            var end = value.Length - 1;
            while (end >= 0 && char.IsDigit(value[end]))
                end--;

            if (end == value.Length - 1)
                return fallback;

            var number = value.Substring(end + 1);
            return int.TryParse(number, out var parsed) ? parsed : fallback;
        }
    }
}
