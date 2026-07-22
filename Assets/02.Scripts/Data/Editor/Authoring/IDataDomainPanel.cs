#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace UPlayGround.Data.Editor.Authoring
{
    /// <summary>
    /// 데이터 저작 허브가 도메인별 편집 패널을 호스팅하기 위한 최소 계약입니다.
    /// </summary>
    public interface IDataDomainPanel
    {
        string DomainId { get; }
        string DisplayName { get; }
        Texture2D Icon { get; }
        VisualElement Root { get; }

        void OnActivate();
        void OnDeactivate();
        void OnReload();
        void SelectAsset(Object asset);
        IEnumerable<DataAuthoringSearchEntry> Search(string query);
        void SelectSearchEntry(DataAuthoringSearchEntry entry);
        IEnumerable<DataAuthoringValidationResult> Validate();
        bool OwnsAsset(Object asset);
        IEnumerable<DataAuthoringIssue> IssuesFor(Object asset);
    }

    /// <summary>
    /// 작업 복사본처럼 명시적 저장이 필요한 도메인이 허브에 dirty 상태를 알리는 선택 계약입니다.
    /// </summary>
    public interface IDataDomainUnsavedChanges
    {
        bool HasUnsavedChanges { get; }
        event System.Action UnsavedChangesChanged;
        bool SaveChanges();
        void DiscardChanges();
    }

    public readonly struct DataAuthoringSearchEntry
    {
        public DataAuthoringSearchEntry(
            IDataDomainPanel panel,
            string key,
            string label,
            object value,
            Sprite icon = null,
            Object context = null)
        {
            Panel = panel;
            Key = key;
            Label = label;
            Value = value;
            Icon = icon;
            Context = context;
        }

        public IDataDomainPanel Panel { get; }
        public string Key { get; }
        public string Label { get; }
        public object Value { get; }
        public Sprite Icon { get; }
        public Object Context { get; }
    }

    public enum DataAuthoringIssueSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// 검증 허브와 연결하기 전에도 도메인 패널이 동일한 형태로 이슈를 노출하도록 하는 값 형식입니다.
    /// </summary>
    public readonly struct DataAuthoringIssue
    {
        public DataAuthoringIssue(
            DataAuthoringIssueSeverity severity,
            string message,
            Object context = null)
        {
            Severity = severity;
            Message = message;
            Context = context;
        }

        public DataAuthoringIssueSeverity Severity { get; }
        public string Message { get; }
        public Object Context { get; }
    }

    public readonly struct DataAuthoringValidationResult
    {
        public DataAuthoringValidationResult(
            IDataDomainPanel panel,
            string domain,
            string key,
            string label,
            DataAuthoringIssue issue,
            object value = null)
        {
            Panel = panel;
            Domain = domain;
            Key = key;
            Label = label;
            Issue = issue;
            Value = value;
        }

        public IDataDomainPanel Panel { get; }
        public string Domain { get; }
        public string Key { get; }
        public string Label { get; }
        public DataAuthoringIssue Issue { get; }
        public object Value { get; }
    }
}
#endif
