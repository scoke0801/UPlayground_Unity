using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.EditorTools
{
    /// <summary>
    /// 등록 특성을 기반으로 도구를 발견하고 유효성 검사 후 실행한다.
    /// </summary>
    public static class UPlaygroundToolCatalog
    {
        private sealed class Registration
        {
            public MethodInfo Execute;
            public MethodInfo Validate;
        }

        private static Dictionary<string, Registration> _registrations;

        public static IReadOnlyCollection<string> ToolIds
        {
            get
            {
                EnsureInitialized();
                return _registrations.Keys;
            }
        }

        public static bool Contains(string toolId)
        {
            EnsureInitialized();
            return _registrations.ContainsKey(NormalizeId(toolId));
        }

        public static bool CanExecute(string toolId)
        {
            EnsureInitialized();
            if (!_registrations.TryGetValue(NormalizeId(toolId), out Registration registration) || registration.Execute == null)
                return false;

            if (registration.Validate == null)
                return true;

            try
            {
                object result = registration.Validate.Invoke(null, null);
                return result is bool valid && valid;
            }
            catch (Exception exception)
            {
                Debug.LogException(Unwrap(exception));
                return false;
            }
        }

        public static bool TryExecute(string toolId, out string error)
        {
            EnsureInitialized();
            string normalizedId = NormalizeId(toolId);
            if (!_registrations.TryGetValue(normalizedId, out Registration registration) || registration.Execute == null)
            {
                error = $"등록된 실행 메서드를 찾지 못했습니다: {normalizedId}";
                return false;
            }

            if (!CanExecute(normalizedId))
            {
                error = "현재 선택이나 에디터 상태에서는 이 도구를 실행할 수 없습니다.";
                return false;
            }

            try
            {
                registration.Execute.Invoke(null, null);
                error = null;
                return true;
            }
            catch (Exception exception)
            {
                Exception actual = Unwrap(exception);
                Debug.LogException(actual);
                error = actual.Message;
                return false;
            }
        }

        public static string NormalizeId(string toolId)
        {
            if (string.IsNullOrWhiteSpace(toolId))
                return string.Empty;

            string normalized = toolId.Trim();
            int shortcutIndex = normalized.IndexOf(" %", StringComparison.Ordinal);
            return shortcutIndex >= 0 ? normalized.Substring(0, shortcutIndex) : normalized;
        }

        [InitializeOnLoadMethod]
        private static void Rebuild()
        {
            _registrations = null;
            EnsureInitialized();
            ValidateTopLevelMenuPolicy();
        }

        private static void EnsureInitialized()
        {
            if (_registrations != null)
                return;

            _registrations = new Dictionary<string, Registration>(StringComparer.Ordinal);
            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<UPlaygroundToolAttribute>())
            {
                if (!method.IsStatic || method.GetParameters().Length != 0)
                {
                    Debug.LogWarning($"[ToolCatalog] 정적 매개변수 없는 메서드만 등록할 수 있습니다: {method.DeclaringType?.FullName}.{method.Name}");
                    continue;
                }

                foreach (UPlaygroundToolAttribute attribute in method.GetCustomAttributes<UPlaygroundToolAttribute>())
                {
                    string id = NormalizeId(attribute.Id);
                    if (!_registrations.TryGetValue(id, out Registration registration))
                    {
                        registration = new Registration();
                        _registrations.Add(id, registration);
                    }

                    if (attribute.IsValidateFunction)
                        registration.Validate = method;
                    else if (registration.Execute == null)
                        registration.Execute = method;
                    else
                        Debug.LogWarning($"[ToolCatalog] 중복 실행 도구 ID를 무시합니다: {id}");
                }
            }
        }

        private static Exception Unwrap(Exception exception)
        {
            return exception is TargetInvocationException { InnerException: not null } invocation
                ? invocation.InnerException
                : exception;
        }

        private static void ValidateTopLevelMenuPolicy()
        {
            const string launcherPath = "UPlayGround/툴 런처";
            const string uiEditorPath = "UPlayGround/UI 에디터";

            foreach (MethodInfo method in TypeCache.GetMethodsWithAttribute<MenuItem>())
            {
                foreach (MenuItem attribute in method.GetCustomAttributes<MenuItem>())
                {
                    string path = NormalizeId(attribute.menuItem);
                    if (!path.StartsWith("UPlayGround/", StringComparison.Ordinal)
                        || path == launcherPath
                        || path == uiEditorPath)
                    {
                        continue;
                    }

                    Debug.LogWarning(
                        $"[ToolCatalog] UPlayGround 상단 메뉴 정책 위반: {path}\n" +
                        $"{method.DeclaringType?.FullName}.{method.Name}에 UPlaygroundToolAttribute를 사용하세요.");
                }
            }
        }
    }
}
