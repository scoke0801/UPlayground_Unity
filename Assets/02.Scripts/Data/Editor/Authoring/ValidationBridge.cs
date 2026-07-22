#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace UPlayGround.Data.Editor.Authoring
{
    /// <summary>
    /// 하위 Data.Editor가 상위 Editor 검증 구현을 직접 참조하지 않도록 공급자 등록 경계를 제공합니다.
    /// </summary>
    public static class ValidationBridge
    {
        private static readonly Dictionary<string, Func<IEnumerable<DataAuthoringValidationResult>>> Providers =
            new Dictionary<string, Func<IEnumerable<DataAuthoringValidationResult>>>(StringComparer.Ordinal);

        public static void Register(
            string providerId,
            Func<IEnumerable<DataAuthoringValidationResult>> provider)
        {
            if (string.IsNullOrWhiteSpace(providerId))
                throw new ArgumentException("검증 공급자 ID는 비어 있을 수 없습니다.", nameof(providerId));
            Providers[providerId] = provider ?? throw new ArgumentNullException(nameof(provider));
        }

        public static IReadOnlyList<DataAuthoringValidationResult> Collect(
            IEnumerable<IDataDomainPanel> panels)
        {
            var results = new List<DataAuthoringValidationResult>();
            foreach (IDataDomainPanel panel in panels ?? Array.Empty<IDataDomainPanel>())
            {
                try
                {
                    results.AddRange(panel.Validate() ?? Array.Empty<DataAuthoringValidationResult>());
                }
                catch (Exception exception)
                {
                    results.Add(ProviderFailure(panel.DisplayName, exception));
                }
            }

            foreach (KeyValuePair<string, Func<IEnumerable<DataAuthoringValidationResult>>> pair in Providers)
            {
                try
                {
                    results.AddRange(pair.Value?.Invoke() ?? Array.Empty<DataAuthoringValidationResult>());
                }
                catch (Exception exception)
                {
                    results.Add(ProviderFailure(pair.Key, exception));
                }
            }

            return results
                .OrderByDescending(result => result.Issue.Severity)
                .ThenBy(result => result.Domain, StringComparer.CurrentCulture)
                .ThenBy(result => result.Label, StringComparer.CurrentCulture)
                .ToArray();
        }

        private static DataAuthoringValidationResult ProviderFailure(string provider, Exception exception)
        {
            return new DataAuthoringValidationResult(
                null,
                "검증",
                provider,
                provider,
                new DataAuthoringIssue(
                    DataAuthoringIssueSeverity.Error,
                    $"검증 실행 중 예외가 발생했습니다: {exception.Message}"));
        }
    }
}
#endif
