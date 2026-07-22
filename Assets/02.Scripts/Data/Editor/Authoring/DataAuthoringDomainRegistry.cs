#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;

namespace UPlayGround.Data.Editor.Authoring
{
    /// <summary>
    /// 도메인 모듈을 허브에 느슨하게 연결하는 에디터 전용 레지스트리입니다.
    /// 각 도메인은 InitializeOnLoad 초기화 코드에서 한 번 등록합니다.
    /// </summary>
    public static class DataAuthoringDomainRegistry
    {
        public readonly struct Registration
        {
            public Registration(string domainId, string displayName, int order, Func<IDataDomainPanel> factory)
            {
                DomainId = domainId;
                DisplayName = displayName;
                Order = order;
                Factory = factory;
            }

            public string DomainId { get; }
            public string DisplayName { get; }
            public int Order { get; }
            public Func<IDataDomainPanel> Factory { get; }
        }

        private static readonly Dictionary<string, Registration> Registrations =
            new Dictionary<string, Registration>(StringComparer.Ordinal);

        public static event Action Changed;

        public static void Register(
            string domainId,
            string displayName,
            Func<IDataDomainPanel> factory,
            int order = 0)
        {
            if (string.IsNullOrWhiteSpace(domainId))
                throw new ArgumentException("도메인 ID는 비어 있을 수 없습니다.", nameof(domainId));
            if (string.IsNullOrWhiteSpace(displayName))
                throw new ArgumentException("도메인 표시 이름은 비어 있을 수 없습니다.", nameof(displayName));
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            Registrations[domainId] = new Registration(domainId, displayName, order, factory);
            Changed?.Invoke();
        }

        public static IReadOnlyList<Registration> GetRegistrations()
        {
            return Registrations.Values
                .OrderBy(entry => entry.Order)
                .ThenBy(entry => entry.DisplayName, StringComparer.CurrentCulture)
                .ToArray();
        }
    }
}
#endif
