using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UPlayGround.Data.Actor.Animation;

namespace UPlayGround.Gameplay.Tag.Editor
{
    /// <summary>
    /// Registry와 직렬화된 GameplayTag 값의 일치를 빌드 전에 강제한다.
    /// </summary>
    public sealed class GameplayTagRegistryBuildValidator : IPreprocessBuildWithReport
    {
        private static readonly HashSet<string> SerializedAssetExtensions =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ".asset",
                ".prefab",
                ".unity",
            };

        // 프로젝트 직렬화 데이터 규약에 따른 저장 루트.
        private static readonly string[] SerializedDataRoots =
        {
            "Assets/01.Scenes",
            "Assets/03.Prefabs",
            "Assets/10.Datas",
            "Assets/Resources",
        };

        public int callbackOrder => -900;

        public void OnPreprocessBuild(BuildReport report)
        {
            try
            {
                ValidateProjectOrThrow();
            }
            catch (Exception exception)
            {
                throw new BuildFailedException(exception.Message);
            }
        }

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/게임플레이/게임플레이 태그/등록 무결성 검증",
            priority = 210)]
        public static void ValidateFromMenu()
        {
            try
            {
                ValidateProjectOrThrow();
                Debug.Log("[GameplayTag] Registry/직렬화 데이터 검증 성공.");
            }
            catch (Exception exception)
            {
                Debug.LogError($"[GameplayTag] 등록 무결성 검증 실패\n{exception.Message}");
            }
        }

        public static void ValidateProjectOrThrow()
        {
            GameplayTagRegistrySO registry = LoadSingleRegistry();
            HashSet<string> registered = ValidateRegistry(registry);
            ValidateCodeDefinedTags(registered);
            ValidateSerializedAssets(registered);
        }

        private static GameplayTagRegistrySO LoadSingleRegistry()
        {
            string[] guids = AssetDatabase.FindAssets("t:GameplayTagRegistrySO");
            if (guids.Length != 1)
            {
                throw new InvalidOperationException(
                    $"GameplayTagRegistrySO는 정확히 1개여야 합니다. 현재 {guids.Length}개입니다.");
            }

            string path = AssetDatabase.GUIDToAssetPath(guids[0]);
            GameplayTagRegistrySO registry =
                AssetDatabase.LoadAssetAtPath<GameplayTagRegistrySO>(path);
            return registry != null
                ? registry
                : throw new InvalidOperationException(
                    $"GameplayTagRegistrySO를 로드하지 못했습니다: {path}");
        }

        private static HashSet<string> ValidateRegistry(
            GameplayTagRegistrySO registry)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            var errors = new List<string>();

            if (registry.tags == null || registry.tags.Count == 0)
                errors.Add("Registry가 비어 있습니다.");
            else
            {
                for (int i = 0; i < registry.tags.Count; i++)
                {
                    GameplayTagDefinition definition = registry.tags[i];
                    string tagName = definition?.tagName ?? string.Empty;
                    if (string.IsNullOrWhiteSpace(tagName))
                    {
                        errors.Add($"{i}번 Registry 항목의 tagName이 비어 있습니다.");
                        continue;
                    }
                    if (!string.Equals(tagName, tagName.Trim(), StringComparison.Ordinal))
                        errors.Add($"tagName 앞뒤 공백: \"{tagName}\"");
                    if (!result.Add(tagName))
                        errors.Add($"중복 tagName: \"{tagName}\"");
                }
            }

            ThrowIfAny(errors);
            return result;
        }

        private static void ValidateCodeDefinedTags(
            HashSet<string> registered)
        {
            var errors = new List<string>();
            Type[] codeTagContainers =
            {
                typeof(GameplayTags),
                typeof(MotionTags),
            };

            foreach (Type containerType in codeTagContainers)
            {
                FieldInfo[] fields = containerType.GetFields(
                    BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < fields.Length; i++)
                {
                    FieldInfo field = fields[i];
                    if (field.FieldType != typeof(GameplayTag))
                        continue;

                    GameplayTag tag = (GameplayTag)field.GetValue(null);
                    if (string.IsNullOrEmpty(tag.TagName))
                        continue;
                    if (!registered.Contains(tag.TagName))
                    {
                        errors.Add(
                            $"{containerType.FullName}.{field.Name}: "
                            + $"미등록 코드 태그 \"{tag.TagName}\"");
                    }
                }
            }

            ThrowIfAny(errors);
        }

        private static void ValidateSerializedAssets(
            HashSet<string> registered)
        {
            var errors = new List<string>();
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                                 ?? throw new InvalidOperationException(
                                     "Unity 프로젝트 루트를 확인하지 못했습니다.");

            foreach (string assetRoot in SerializedDataRoots)
            {
                string absoluteRoot = Path.Combine(projectRoot, assetRoot);
                if (!Directory.Exists(absoluteRoot))
                    continue;

                foreach (string filePath in Directory.EnumerateFiles(
                             absoluteRoot,
                             "*",
                             SearchOption.AllDirectories))
                {
                    if (!SerializedAssetExtensions.Contains(
                            Path.GetExtension(filePath)))
                        continue;

                    int lineNumber = 0;
                    foreach (string line in File.ReadLines(filePath))
                    {
                        lineNumber++;
                        string trimmed = line.TrimStart();
                        if (!trimmed.StartsWith(
                                "_tagName:",
                                StringComparison.Ordinal))
                            continue;

                        string tagName =
                            trimmed.Substring("_tagName:".Length).Trim();
                        if (string.IsNullOrEmpty(tagName)
                            || registered.Contains(tagName))
                            continue;

                        string relativePath =
                            filePath.Substring(
                                    projectRoot.Length + 1)
                                .Replace('\\', '/');
                        errors.Add(
                            $"{relativePath}:{lineNumber} 미등록 태그 \"{tagName}\"");
                    }
                }
            }

            ThrowIfAny(errors);
        }

        private static void ThrowIfAny(List<string> errors)
        {
            if (errors.Count == 0) return;
            throw new InvalidOperationException(
                "GameplayTag 등록 무결성 오류:\n- " + string.Join("\n- ", errors));
        }
    }
}
