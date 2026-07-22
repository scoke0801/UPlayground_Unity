#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.UI;
using UPlayGround.Data.UI.EditorTools;

namespace UPlayGround.Data.Editor.Authoring
{
    [InitializeOnLoad]
    internal static class GuidePopupDomainRegistration
    {
        static GuidePopupDomainRegistration()
        {
            DataAuthoringDomainRegistry.Register(
                GuidePopupDomainPanel.DomainKey,
                "가이드",
                () => new GuidePopupDomainPanel(),
                510);
        }
    }

    /// <summary>
    /// GuidePopupDataSO 목록·페이지 데이터를 편집하고 미디어 누락을 검증합니다.
    /// </summary>
    public sealed class GuidePopupDomainPanel : DataDomainPanel<GuidePopupDataSO>
    {
        public const string DomainKey = "guides";
        private const string DefaultPath = "Assets/10.Datas/Guide";

        public override string DomainId => DomainKey;
        public override string DisplayName => "가이드";
        public override Texture2D Icon => EditorGUIUtility.IconContent("d_UnityEditor.InspectorWindow").image as Texture2D;
        protected override float ListPanelWidth => 330f;
        protected override string CreateButtonLabel => "+ 새 가이드";
        protected override bool CanCreate => true;
        protected override bool CanDuplicate(GuidePopupDataSO asset) => asset != null;
        protected override bool CanDelete(GuidePopupDataSO asset) => asset != null;

        protected override IEnumerable<GuidePopupDataSO> LoadAssets()
        {
            return AssetDatabase.FindAssets("t:GuidePopupDataSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GuidePopupDataSO>)
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name, StringComparer.CurrentCulture);
        }

        protected override string KeyOf(GuidePopupDataSO asset) => asset != null ? asset.name : string.Empty;

        protected override string LabelOf(GuidePopupDataSO asset)
            => asset != null ? $"{asset.name}  ·  페이지 {asset.Pages.Count}" : string.Empty;

        protected override Sprite IconOf(GuidePopupDataSO asset)
        {
            return asset?.Pages.FirstOrDefault(page => page != null && page.MediaType == GuidePopupMediaType.Image)?.Image;
        }

        protected override IEnumerable<DataDomainFilter<GuidePopupDataSO>> CreateFilters()
        {
            yield return new DataDomainFilter<GuidePopupDataSO>("이미지", asset =>
                asset.Pages.Any(page => page != null && page.MediaType == GuidePopupMediaType.Image));
            yield return new DataDomainFilter<GuidePopupDataSO>("동영상", asset =>
                asset.Pages.Any(page => page != null && page.MediaType == GuidePopupMediaType.Video));
            yield return new DataDomainFilter<GuidePopupDataSO>("빈 가이드", asset => asset.Pages.Count == 0);
        }

        protected override void AddToolbarActions(Toolbar toolbar)
        {
            toolbar.Add(new ToolbarButton(() => GuidePopupDataEditorWindow.Open()) { text = "미디어 미리보기 편집기" });
        }

        protected override void CreateNew()
        {
            GuidePopupDataSO created = AssetCrudService.CreateAsset<GuidePopupDataSO>(
                DefaultPath,
                "GuidePopup_New",
                undoName: "가이드 팝업 생성");
            EditorGUIUtility.PingObject(created);
            RefreshAssets(created);
        }

        protected override GuidePopupDataSO Duplicate(GuidePopupDataSO asset)
        {
            GuidePopupDataSO copy = AssetCrudService.DuplicateAsset(asset, undoName: "가이드 팝업 복제");
            EditorGUIUtility.PingObject(copy);
            return copy;
        }

        protected override bool Delete(GuidePopupDataSO asset)
        {
            if (!EditorUtility.DisplayDialog(
                    "가이드 팝업 삭제",
                    $"'{asset.name}'을 삭제할까요?\nUI와 GameGuide 데이터 참조를 먼저 확인하세요.",
                    "삭제",
                    "취소"))
            {
                return false;
            }

            return AssetCrudService.DeleteAsset(asset, "가이드 팝업 삭제");
        }

        protected override IEnumerable<DataAuthoringIssue> GetIssues(GuidePopupDataSO asset)
        {
            if (asset.Pages.Count == 0)
            {
                yield return new DataAuthoringIssue(
                    DataAuthoringIssueSeverity.Warning,
                    "가이드 페이지가 없습니다.",
                    asset);
                yield break;
            }

            for (int i = 0; i < asset.Pages.Count; i++)
            {
                GuidePopupPage page = asset.Pages[i];
                if (page == null)
                {
                    yield return Error($"페이지 {i + 1} 데이터가 비어 있습니다.", asset);
                    continue;
                }

                if (page.MediaType == GuidePopupMediaType.Image && page.Image == null)
                    yield return Error($"페이지 {i + 1}의 이미지가 비어 있습니다.", asset);
                if (page.MediaType == GuidePopupMediaType.Video && page.Video == null)
                    yield return Error($"페이지 {i + 1}의 동영상이 비어 있습니다.", asset);
                if (string.IsNullOrWhiteSpace(page.Title))
                {
                    yield return new DataAuthoringIssue(
                        DataAuthoringIssueSeverity.Info,
                        $"페이지 {i + 1}의 제목이 비어 있습니다.",
                        asset);
                }
            }
        }

        protected override VisualElement BuildDetail(GuidePopupDataSO asset)
        {
            var detail = new VisualElement();
            var serializedObject = new SerializedObject(asset);

            var header = new Toolbar();
            var title = new Label(asset.name);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            header.Add(spacer);
            header.Add(new ToolbarButton(() => GuidePopupDataEditorWindow.Open(asset)) { text = "미리보기 편집" });
            header.Add(new ToolbarButton(() => EditorGUIUtility.PingObject(asset)) { text = "Project에서 열기" });
            detail.Add(header);

            var description = new HelpBox(
                "페이지 목록은 여기서 직접 편집할 수 있습니다. 이미지·동영상 실제 화면 확인과 순서 편집은 '미리보기 편집'을 사용하세요.",
                HelpBoxMessageType.Info);
            description.style.marginTop = 8f;
            detail.Add(description);

            SerializedProperty pages = serializedObject.FindProperty("_pages");
            var pagesField = new PropertyField(pages, "가이드 페이지");
            pagesField.style.marginTop = 8f;
            detail.Add(pagesField);

            detail.TrackSerializedObjectValue(serializedObject, _ => NotifyAssetChanged(asset));
            detail.Bind(serializedObject);
            return detail;
        }

        private static DataAuthoringIssue Error(string message, UnityEngine.Object context)
            => new DataAuthoringIssue(DataAuthoringIssueSeverity.Error, message, context);
    }
}
#endif
