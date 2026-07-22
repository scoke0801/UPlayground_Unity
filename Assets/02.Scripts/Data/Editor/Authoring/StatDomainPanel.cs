#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Stat;

namespace UPlayGround.Data.Editor.Authoring
{
    [InitializeOnLoad]
    internal static class StatDomainRegistration
    {
        static StatDomainRegistration()
        {
            DataAuthoringDomainRegistry.Register(
                StatDomainPanel.DomainKey,
                "스탯",
                () => new StatDomainPanel(),
                420);
        }
    }

    /// <summary>
    /// ActorStatSO의 명시 값과 기본값 폴백을 한 화면에서 편집합니다.
    /// </summary>
    public sealed class StatDomainPanel : DataDomainPanel<ActorStatSO>
    {
        public const string DomainKey = "stats";
        private const string DefaultPath = "Assets/10.Datas/Stat";
        private const string DatabaseEditorMenuPath = "UPlayGround/게임플레이/스탯/스탯 데이터베이스 에디터";
        private static readonly StatType[] AllTypes = Enum.GetValues(typeof(StatType)).Cast<StatType>().ToArray();

        public override string DomainId => DomainKey;
        public override string DisplayName => "스탯";
        public override Texture2D Icon => EditorGUIUtility.IconContent("d_Profiler.CPU").image as Texture2D;
        protected override string CreateButtonLabel => "+ 새 스탯";
        protected override bool CanCreate => true;
        protected override bool CanDuplicate(ActorStatSO asset) => asset != null;
        protected override bool CanDelete(ActorStatSO asset) => asset != null;

        protected override IEnumerable<ActorStatSO> LoadAssets()
        {
            return AssetDatabase.FindAssets("t:ActorStatSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ActorStatSO>)
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name, StringComparer.CurrentCulture);
        }

        protected override string KeyOf(ActorStatSO asset) => asset != null ? asset.name : string.Empty;

        protected override string LabelOf(ActorStatSO asset)
        {
            if (asset == null)
                return string.Empty;
            int explicitCount = AllTypes.Count(type => asset.TryGetExplicit(type, out _));
            return $"{asset.name}  ·  명시 {explicitCount}/{AllTypes.Length}";
        }

        protected override IEnumerable<DataDomainFilter<ActorStatSO>> CreateFilters()
        {
            yield return new DataDomainFilter<ActorStatSO>("전체 명시", asset => MissingCount(asset) == 0);
            yield return new DataDomainFilter<ActorStatSO>("폴백 있음", asset => MissingCount(asset) > 0);
        }

        protected override void AddToolbarActions(Toolbar toolbar)
        {
            var actions = new ToolbarMenu { text = "스탯 작업" };
            actions.menu.AppendAction("스탯 데이터 생성기...", _ => DataAuthoringToolBridge.Execute(
                DataAuthoringToolBridge.StatGenerator,
                "스탯 데이터 생성기"));
            actions.menu.AppendAction("스탯 커버리지 검증", _ => DataAuthoringToolBridge.Execute(
                DataAuthoringToolBridge.StatCoverage,
                "스탯 데이터 커버리지 검증"));
            actions.menu.AppendAction("스탯 데이터베이스 에디터...", _ => ExecuteMenu(DatabaseEditorMenuPath));
            toolbar.Add(actions);
        }

        protected override void CreateNew()
        {
            ActorStatSO created = AssetCrudService.CreateAsset<ActorStatSO>(
                DefaultPath,
                "ActorStat_New",
                stat => stat.EditorFillMissing(),
                "스탯 데이터 생성");
            EditorGUIUtility.PingObject(created);
            RefreshAssets(created);
        }

        protected override ActorStatSO Duplicate(ActorStatSO asset)
        {
            ActorStatSO copy = AssetCrudService.DuplicateAsset(asset, undoName: "스탯 데이터 복제");
            EditorGUIUtility.PingObject(copy);
            return copy;
        }

        protected override bool Delete(ActorStatSO asset)
        {
            if (!EditorUtility.DisplayDialog(
                    "스탯 데이터 삭제",
                    $"'{asset.name}' 자산을 삭제할까요?\nActorDefinitionSO의 statData 참조가 남을 수 있습니다.",
                    "삭제",
                    "취소"))
            {
                return false;
            }

            return AssetCrudService.DeleteAsset(asset, "스탯 데이터 삭제");
        }

        protected override IEnumerable<DataAuthoringIssue> GetIssues(ActorStatSO asset)
        {
            int missing = MissingCount(asset);
            if (missing > 0)
            {
                yield return new DataAuthoringIssue(
                    DataAuthoringIssueSeverity.Info,
                    $"{missing}개 스탯이 명시되지 않아 기본값 폴백을 사용합니다.",
                    asset);
            }
        }

        protected override VisualElement BuildDetail(ActorStatSO asset)
        {
            var detail = new VisualElement();
            var header = new Toolbar();
            var title = new Label(asset.name);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            header.Add(spacer);
            var coverage = new Label();
            coverage.style.fontSize = 10f;
            header.Add(coverage);
            header.Add(new ToolbarButton(() => EditorGUIUtility.PingObject(asset)) { text = "Project에서 열기" });
            detail.Add(header);

            var rows = new VisualElement();
            rows.style.marginTop = 8f;

            var actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.marginTop = 7f;
            actionRow.Add(new Button(() =>
            {
                Undo.RecordObject(asset, "누락 스탯 채우기");
                asset.EditorFillMissing();
                EditorUtility.SetDirty(asset);
                NotifyAssetChanged(asset);
                RebuildRows();
            }) { text = "누락 스탯 채우기" });
            actionRow.Add(new Button(() =>
            {
                if (!EditorUtility.DisplayDialog("스탯 초기화", "모든 명시 스탯을 제거하고 기본값 폴백으로 전환할까요?", "초기화", "취소"))
                    return;
                Undo.RecordObject(asset, "전체 스탯 초기화");
                foreach (StatType type in AllTypes)
                    asset.EditorRemove(type);
                EditorUtility.SetDirty(asset);
                NotifyAssetChanged(asset);
                RebuildRows();
            }) { text = "전체 명시 해제" });
            detail.Add(actionRow);

            detail.Add(rows);
            RebuildRows();
            return detail;

            void RebuildRows()
            {
                rows.Clear();
                coverage.text = $"명시 {AllTypes.Length - MissingCount(asset)} / {AllTypes.Length}";

                foreach (IGrouping<string, StatType> group in AllTypes.GroupBy(CategoryOf))
                {
                    var heading = new Label(group.Key);
                    heading.style.unityFontStyleAndWeight = FontStyle.Bold;
                    heading.style.marginTop = 8f;
                    heading.style.marginBottom = 3f;
                    rows.Add(heading);

                    foreach (StatType type in group)
                        rows.Add(BuildStatRow(asset, type, RebuildRows));
                }
            }
        }

        private VisualElement BuildStatRow(ActorStatSO asset, StatType type, Action rebuild)
        {
            bool isExplicit = asset.TryGetExplicit(type, out float value);
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 2f;

            var explicitToggle = new Toggle { value = isExplicit, tooltip = "명시 값을 저장할지 여부" };
            explicitToggle.style.width = 22f;
            row.Add(explicitToggle);

            var label = new Label(type.ToString());
            label.style.width = 170f;
            label.style.color = isExplicit
                ? new StyleColor(StyleKeyword.Null)
                : new StyleColor(new Color(0.55f, 0.55f, 0.55f));
            row.Add(label);

            var field = new FloatField { value = value };
            field.style.flexGrow = 1f;
            field.SetEnabled(isExplicit);
            row.Add(field);

            var fallback = new Label(isExplicit ? "명시" : $"기본값 {value:0.###}");
            fallback.style.width = 92f;
            fallback.style.fontSize = 10f;
            row.Add(fallback);

            explicitToggle.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(asset, evt.newValue ? "스탯 명시 추가" : "스탯 명시 해제");
                if (evt.newValue)
                    asset.EditorSet(type, ActorStatSO.GetDefault(type));
                else
                    asset.EditorRemove(type);
                EditorUtility.SetDirty(asset);
                NotifyAssetChanged(asset);
                rebuild();
            });

            field.RegisterValueChangedCallback(evt =>
            {
                Undo.RecordObject(asset, "스탯 값 변경");
                asset.EditorSet(type, evt.newValue);
                EditorUtility.SetDirty(asset);
                NotifyAssetChanged(asset);
            });
            return row;
        }

        private static int MissingCount(ActorStatSO asset)
            => AllTypes.Count(type => !asset.TryGetExplicit(type, out _));

        private static void ExecuteMenu(string path)
        {
            if (!EditorApplication.ExecuteMenuItem(path))
                EditorUtility.DisplayDialog("도구 열기 실패", path, "확인");
        }

        private static string CategoryOf(StatType type) => type switch
        {
            StatType.MaxHealth or StatType.HealthRegenRate => "생존",
            StatType.AttackPower or StatType.Defense or StatType.CritRate or StatType.CritMultiplier or StatType.AttackSpeed => "전투",
            StatType.MoveSpeed or StatType.DashDistance => "이동",
            StatType.MaxPoise or StatType.PoiseRecoveryRate or StatType.PoiseRecoveryDelay => "강인도",
            StatType.SkillGaugeRate or StatType.InvincibleDuration => "스킬",
            StatType.GatheringPower => "생활",
            _ => "기타"
        };
    }
}
#endif
