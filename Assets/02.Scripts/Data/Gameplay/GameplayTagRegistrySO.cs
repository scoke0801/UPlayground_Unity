using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.Gameplay.Tag
{
    /// <summary>
    /// 프로젝트에서 사용하는 GameplayTag 하나의 데이터 정의.
    /// </summary>
    [Serializable]
    public sealed class GameplayTagDefinition
    {
        [Tooltip("계층형 태그 이름. '.'으로 계층을 구분합니다.")]
        public string tagName = string.Empty;

        [Tooltip("검색 UI와 문서에 표시할 설명")]
        public string description = string.Empty;

        [Tooltip("에디터 시각화 색상")]
        public Color color = new(0.4f, 0.8f, 1f);

        public bool IsValid() => !string.IsNullOrWhiteSpace(tagName);
    }

    /// <summary>
    /// 프로젝트 전역 GameplayTag 데이터 원본.
    /// Resources/GameplayTagRegistry.asset을 런타임과 에디터가 함께 읽는다.
    /// </summary>
    [CreateAssetMenu(
        fileName = "GameplayTagRegistry",
        menuName = "UPlayGround/게임플레이 태그/Registry")]
    public sealed class GameplayTagRegistrySO : ScriptableObject
    {
        [Tooltip("프로젝트에서 사용할 모든 GameplayTag 정의")]
        public List<GameplayTagDefinition> tags = new();

        private HashSet<string> _registeredNames;

        public bool IsRegistered(string tagName)
        {
            EnsureLookup();
            return !string.IsNullOrWhiteSpace(tagName)
                   && _registeredNames.Contains(tagName);
        }

        public bool TryGetDefinition(
            string tagName,
            out GameplayTagDefinition definition)
        {
            if (tags != null)
            {
                for (int i = 0; i < tags.Count; i++)
                {
                    GameplayTagDefinition candidate = tags[i];
                    if (candidate != null
                        && string.Equals(
                            candidate.tagName,
                            tagName,
                            StringComparison.Ordinal))
                    {
                        definition = candidate;
                        return true;
                    }
                }
            }

            definition = null;
            return false;
        }

        public void RebuildLookup()
        {
            _registeredNames = new HashSet<string>(StringComparer.Ordinal);
            if (tags == null) return;

            for (int i = 0; i < tags.Count; i++)
            {
                GameplayTagDefinition definition = tags[i];
                if (definition?.IsValid() == true)
                    _registeredNames.Add(definition.tagName);
            }
        }

        private void EnsureLookup()
        {
            if (_registeredNames == null)
                RebuildLookup();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            RebuildLookup();
            GameplayTagRegistry.SetEditorRegistry(this);
        }
#endif
    }

    /// <summary>
    /// 코드 생성 없이 Registry SO를 직접 조회하는 GameplayTag 진입점.
    /// </summary>
    public static class GameplayTagRegistry
    {
        private const string ResourcePath = "GameplayTagRegistry";
        private static GameplayTagRegistrySO s_Registry;

        public static GameplayTagRegistrySO Registry
        {
            get
            {
                if (s_Registry == null)
                    s_Registry = Resources.Load<GameplayTagRegistrySO>(
                        ResourcePath);
                return s_Registry != null
                    ? s_Registry
                    : throw new InvalidOperationException(
                        $"Resources/{ResourcePath}.asset을 찾지 못했습니다.");
            }
        }

        public static IReadOnlyList<GameplayTagDefinition> Definitions =>
            Registry.tags;

        public static bool IsRegistered(string tagName) =>
            Registry.IsRegistered(tagName);

        public static bool TryResolve(
            string tagName,
            out GameplayTag tag)
        {
            string normalized = tagName?.Trim() ?? string.Empty;
            if (IsRegistered(normalized))
            {
                tag = GameplayTag.CreateRegistered(normalized);
                return true;
            }

            tag = default;
            return false;
        }

        public static GameplayTag GetRequired(string tagName)
        {
            if (TryResolve(tagName, out GameplayTag tag))
                return tag;
            throw new ArgumentException(
                $"GameplayTagRegistry에 등록되지 않은 태그입니다: '{tagName}'",
                nameof(tagName));
        }

#if UNITY_EDITOR
        internal static void SetEditorRegistry(GameplayTagRegistrySO registry)
        {
            s_Registry = registry;
        }
#endif
    }
}
