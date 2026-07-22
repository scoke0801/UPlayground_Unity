#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Data.Editor.Authoring
{
    /// <summary>
    /// 데이터 도메인에서 상위 에디터 어셈블리의 보조 도구를 직접 참조하지 않고 실행하기 위한 브리지입니다.
    /// </summary>
    public static class DataAuthoringToolBridge
    {
        public const string ItemGenerator = "item-generator";
        public const string RecipeGenerator = "recipe-generator";
        public const string StatGenerator = "stat-generator";
        public const string StatCoverage = "stat-coverage";
        public const string NpcGenerator = "npc-generator";
        public const string ActorDatabaseEditor = "actor-database-editor";

        private static readonly Dictionary<string, Action> Actions =
            new Dictionary<string, Action>(StringComparer.Ordinal);

        public static void Register(string actionId, Action action)
        {
            if (string.IsNullOrWhiteSpace(actionId))
                throw new ArgumentException("도구 액션 ID는 비어 있을 수 없습니다.", nameof(actionId));
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            Actions[actionId] = action;
        }

        public static bool Execute(string actionId, string displayName)
        {
            if (!Actions.TryGetValue(actionId, out Action action))
            {
                EditorUtility.DisplayDialog(
                    "도구 열기 실패",
                    $"'{displayName}' 실행 연결을 찾을 수 없습니다. 스크립트 컴파일 상태를 확인하세요.",
                    "확인");
                return false;
            }

            try
            {
                action();
                return true;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog("도구 열기 실패", $"'{displayName}' 실행 중 오류가 발생했습니다.", "확인");
                return false;
            }
        }
    }
}
#endif
