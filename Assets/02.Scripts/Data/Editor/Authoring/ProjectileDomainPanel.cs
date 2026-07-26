#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Projectile;

namespace UPlayGround.Data.Editor.Authoring
{
    [InitializeOnLoad]
    internal static class ProjectileDomainRegistration
    {
        static ProjectileDomainRegistration()
        {
            DataAuthoringDomainRegistry.Register(
                ProjectileDomainPanel.DomainKey,
                "투사체",
                () => new ProjectileDomainPanel(),
                80);
        }

        [OnOpenAsset]
        private static bool OpenDefinitionAsset(int instanceId, int line)
        {
            if (EditorUtility.InstanceIDToObject(instanceId) is not ProjectileDefinitionSO definition)
                return false;

            DataAuthoringHubWindow.Open(ProjectileDomainPanel.DomainKey, definition);
            return true;
        }
    }

    /// <summary>
    /// ProjectileDefinition의 목록, 조합 편집, 검증, 비용 분석과 궤적 프리뷰를
    /// 데이터 저작 허브 안에서 일관되게 제공합니다.
    /// </summary>
    public sealed class ProjectileDomainPanel : DataDomainPanel<ProjectileDefinitionSO>
    {
        public const string DomainKey = "projectiles";
        private const string DefaultPath = "Assets/10.Datas/Projectile";

        private static readonly (string Label, Type Type)[] MotionTypes =
        {
            ("직선 Linear", typeof(LinearProjectileMotion)),
            ("포물선 Arc", typeof(ArcProjectileMotion)),
            ("유도 Homing", typeof(HomingProjectileMotion)),
            ("고정 Stationary", typeof(StationaryProjectileMotion)),
            ("궤도 Orbit", typeof(OrbitProjectileMotion)),
            ("히트스캔 Hitscan", typeof(HitscanProjectileMotion)),
        };

        private static readonly (string Label, Type Type)[] BehaviorTypes =
        {
            ("관통 Pierce", typeof(PierceProjectileBehavior)),
            ("튕김 Bounce", typeof(BounceProjectileBehavior)),
            ("분열 Split", typeof(SplitProjectileBehavior)),
            ("기폭 Detonate", typeof(DetonateProjectileBehavior)),
            ("범위 틱 Area Tick", typeof(AreaTickProjectileBehavior)),
            ("부착 Attach", typeof(AttachProjectileBehavior)),
            ("반사 가능 Reflectable", typeof(ReflectableProjectileBehavior)),
        };

        public override string DomainId => DomainKey;
        public override string DisplayName => "투사체";
        public override Texture2D Icon =>
            EditorGUIUtility.IconContent("d_PreMatSphere").image as Texture2D;

        protected override float ListPanelWidth => 350f;
        protected override string CreateButtonLabel => "+ 새 투사체";
        protected override bool CanCreate => true;
        protected override bool CanDuplicate(ProjectileDefinitionSO asset) => asset != null;
        protected override bool CanDelete(ProjectileDefinitionSO asset) => asset != null;

        protected override IEnumerable<ProjectileDefinitionSO> LoadAssets()
        {
            return AssetDatabase.FindAssets("t:ProjectileDefinitionSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ProjectileDefinitionSO>)
                .Where(asset => asset != null)
                .OrderBy(asset => asset.name, StringComparer.CurrentCultureIgnoreCase);
        }

        protected override string KeyOf(ProjectileDefinitionSO asset)
        {
            return asset != null ? asset.name : string.Empty;
        }

        protected override string LabelOf(ProjectileDefinitionSO asset)
        {
            if (asset == null)
                return string.Empty;

            string motion = FriendlyTypeName(asset.motion);
            int behaviorCount = asset.behaviors?.Count ?? 0;
            return $"{asset.name}  ·  {motion}  ·  Behavior {behaviorCount}";
        }

        protected override void AddToolbarActions(Toolbar toolbar)
        {
            var actions = new ToolbarMenu { text = "투사체 작업" };
            actions.menu.AppendAction(
                "선택 Definition 사용처 검색",
                _ => FindReferences(Selected),
                _ => Selected != null
                    ? DropdownMenuAction.Status.Normal
                    : DropdownMenuAction.Status.Disabled);
            actions.menu.AppendAction(
                "전체 Definition 검증",
                _ => ValidateAllDefinitions());
            toolbar.Add(actions);
        }

        protected override void CreateNew()
        {
            ProjectileDefinitionSO created = AssetCrudService.CreateAsset<ProjectileDefinitionSO>(
                DefaultPath,
                "ProjectileDefinition",
                definition =>
                {
                    definition.motion = new LinearProjectileMotion();
                    definition.behaviors = new List<ProjectileBehaviorData>();
                    definition.lifetime = 5f;
                    definition.collisionRadius = 0.25f;
                    definition.prewarmCount = 4;
                    definition.maxPoolSize = 32;
                },
                "투사체 정의 생성");
            EditorGUIUtility.PingObject(created);
            RefreshAssets(created);
        }

        protected override ProjectileDefinitionSO Duplicate(ProjectileDefinitionSO asset)
        {
            ProjectileDefinitionSO copy = AssetCrudService.DuplicateAsset(
                asset,
                null,
                "투사체 정의 복제");
            EditorGUIUtility.PingObject(copy);
            return copy;
        }

        protected override bool Delete(ProjectileDefinitionSO asset)
        {
            if (!EditorUtility.DisplayDialog(
                    "투사체 정의 삭제",
                    $"'{asset.name}'을 삭제할까요?\nAbility와 MotionSet 사용처는 자동으로 변경되지 않습니다.",
                    "삭제",
                    "취소"))
            {
                return false;
            }

            return AssetCrudService.DeleteAsset(asset, "투사체 정의 삭제");
        }

        protected override IEnumerable<DataAuthoringIssue> GetIssues(ProjectileDefinitionSO asset)
        {
            var errors = new List<string>();
            asset.CollectValidationErrors(errors);
            foreach (string error in errors)
                yield return new DataAuthoringIssue(DataAuthoringIssueSeverity.Error, error, asset);
        }

        protected override VisualElement BuildDetail(ProjectileDefinitionSO asset)
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
            header.Add(new ToolbarButton(() =>
            {
                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
            }) { text = "Project에서 열기" });
            header.Add(new ToolbarButton(() => FindReferences(asset)) { text = "사용처" });
            detail.Add(header);

            VisualElement visual = MakeSection("비주얼");
            AddProperty(visual, "visualPrefab", "비주얼 프리팹");
            AddProperty(visual, "hitEffectKey", "피격 이펙트 키");
            AddProperty(visual, "detachTrailOnReturn", "회수 시 Trail 분리");
            detail.Add(visual);

            VisualElement simulation = MakeSection("시뮬레이션");
            AddProperty(simulation, "lifetime", "수명");
            AddProperty(simulation, "collisionRadius", "충돌 반경");
            AddProperty(simulation, "destroyOnHit", "피격 시 소멸");
            AddProperty(simulation, "inheritOwnerTimeScale", "소유자 TimeScale 상속");
            detail.Add(simulation);

            detail.Add(BuildMotionSection(asset, serializedObject));
            detail.Add(BuildBehaviorSection(asset, serializedObject));

            VisualElement pool = MakeSection("풀과 안전 제한");
            AddProperty(pool, "prewarmCount", "Prewarm");
            AddProperty(pool, "maxPoolSize", "최대 풀 크기");
            AddProperty(pool, "maxGeneration", "분열 최대 세대");
            detail.Add(pool);

            VisualElement analysis = MakeSection("분석과 궤적 프리뷰");
            var analysisLabel = new Label();
            analysisLabel.style.whiteSpace = WhiteSpace.Normal;
            analysisLabel.style.marginBottom = 8f;
            analysis.Add(analysisLabel);

            var preview = new ProjectileTrajectoryPreviewElement();
            preview.style.height = 220f;
            preview.style.marginBottom = 8f;
            preview.style.backgroundColor = DataAuthoringTheme.Window;
            DataAuthoringTheme.SetBorder(preview);
            DataAuthoringTheme.Round(preview, 4f);
            analysis.Add(preview);

            var validationRoot = new VisualElement();
            analysis.Add(validationRoot);
            detail.Add(analysis);

            void RefreshAnalysis()
            {
                var errors = new List<string>();
                asset.CollectValidationErrors(errors);
                analysisLabel.text =
                    $"전략  {FriendlyTypeName(asset.motion)}\n"
                    + $"예상 이동 거리  {EstimateTravelDistance(asset):0.##} m\n"
                    + $"풀  {asset.prewarmCount} prewarm / {asset.maxPoolSize} max\n"
                    + $"분열 트리 최대  약 {EstimateSplitPeak(asset)}개";
                preview.SetDefinition(asset);
                validationRoot.Clear();
                if (errors.Count == 0)
                {
                    validationRoot.Add(new HelpBox(
                        "저장 가능한 조합입니다.",
                        HelpBoxMessageType.Info));
                    return;
                }

                foreach (string error in errors)
                    validationRoot.Add(new HelpBox(error, HelpBoxMessageType.Error));
            }

            detail.TrackSerializedObjectValue(serializedObject, _ =>
            {
                title.text = asset.name;
                EditorUtility.SetDirty(asset);
                NotifyAssetChanged(asset);
                RefreshAnalysis();
            });
            detail.Bind(serializedObject);
            RefreshAnalysis();
            return detail;

            void AddProperty(VisualElement parent, string path, string label)
            {
                SerializedProperty property = serializedObject.FindProperty(path);
                if (property != null)
                    parent.Add(new PropertyField(property, label));
            }
        }

        private VisualElement BuildMotionSection(
            ProjectileDefinitionSO asset,
            SerializedObject serializedObject)
        {
            VisualElement section = MakeSection("이동 전략");
            SerializedProperty motion = serializedObject.FindProperty("motion");
            Type currentType = motion.managedReferenceValue?.GetType();
            int currentIndex = Math.Max(
                0,
                Array.FindIndex(MotionTypes, entry => entry.Type == currentType));
            List<string> choices = MotionTypes.Select(entry => entry.Label).ToList();
            var popup = new PopupField<string>("이동 전략", choices, currentIndex);
            popup.AddToClassList(BaseField<string>.alignedFieldUssClassName);
            popup.RegisterValueChangedCallback(evt =>
            {
                int index = choices.IndexOf(evt.newValue);
                if (index < 0)
                    return;

                Undo.RecordObject(asset, "투사체 이동 전략 변경");
                serializedObject.Update();
                serializedObject.FindProperty("motion").managedReferenceValue =
                    Activator.CreateInstance(MotionTypes[index].Type);
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(asset);
                RefreshAssets(asset);
            });
            section.Add(popup);

            if (motion.managedReferenceValue != null)
                section.Add(new PropertyField(motion.Copy(), "이동 세부 설정"));
            return section;
        }

        private VisualElement BuildBehaviorSection(
            ProjectileDefinitionSO asset,
            SerializedObject serializedObject)
        {
            VisualElement section = MakeSection("Behavior 조합");
            SerializedProperty behaviors = serializedObject.FindProperty("behaviors");

            for (int i = 0; i < behaviors.arraySize; i++)
            {
                int index = i;
                SerializedProperty element = behaviors.GetArrayElementAtIndex(i);
                var card = new VisualElement();
                card.style.marginBottom = 7f;
                card.style.paddingLeft = 8f;
                card.style.paddingRight = 8f;
                card.style.paddingTop = 6f;
                card.style.paddingBottom = 7f;
                card.style.backgroundColor = DataAuthoringTheme.Surface;
                DataAuthoringTheme.SetBorder(card);
                DataAuthoringTheme.Round(card, 3f);

                var cardHeader = new VisualElement();
                cardHeader.style.flexDirection = FlexDirection.Row;
                cardHeader.style.alignItems = Align.Center;
                var cardTitle = new Label(
                    $"{i + 1:00}  {FriendlyTypeName(element.managedReferenceValue)}");
                cardTitle.style.unityFontStyleAndWeight = FontStyle.Bold;
                cardHeader.Add(cardTitle);
                var cardSpacer = new VisualElement();
                cardSpacer.style.flexGrow = 1f;
                cardHeader.Add(cardSpacer);
                cardHeader.Add(new Button(() => MoveBehavior(asset, index, index - 1))
                {
                    text = "↑",
                    tooltip = "위로 이동"
                });
                cardHeader.Add(new Button(() => MoveBehavior(asset, index, index + 1))
                {
                    text = "↓",
                    tooltip = "아래로 이동"
                });
                var remove = new Button(() => RemoveBehavior(asset, index)) { text = "삭제" };
                remove.style.color = DataAuthoringTheme.Error;
                cardHeader.Add(remove);
                card.Add(cardHeader);

                if (element.managedReferenceValue != null)
                    card.Add(new PropertyField(element.Copy(), null));
                section.Add(card);
            }

            var add = new Button { text = "+ Behavior 추가" };
            add.clicked += () => ShowBehaviorMenu(add, asset);
            section.Add(add);
            return section;
        }

        private void ShowBehaviorMenu(Button anchor, ProjectileDefinitionSO asset)
        {
            var menu = new GenericMenu();
            foreach ((string label, Type type) in BehaviorTypes)
            {
                Type capturedType = type;
                menu.AddItem(
                    new GUIContent(label),
                    false,
                    () => AddBehavior(asset, capturedType));
            }
            menu.DropDown(anchor.worldBound);
        }

        private void AddBehavior(ProjectileDefinitionSO asset, Type type)
        {
            var serializedObject = new SerializedObject(asset);
            Undo.RecordObject(asset, "투사체 Behavior 추가");
            SerializedProperty behaviors = serializedObject.FindProperty("behaviors");
            int index = behaviors.arraySize;
            behaviors.arraySize++;
            behaviors.GetArrayElementAtIndex(index).managedReferenceValue =
                Activator.CreateInstance(type);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            RefreshAssets(asset);
        }

        private void RemoveBehavior(ProjectileDefinitionSO asset, int index)
        {
            var serializedObject = new SerializedObject(asset);
            SerializedProperty behaviors = serializedObject.FindProperty("behaviors");
            if (index < 0 || index >= behaviors.arraySize)
                return;

            Undo.RecordObject(asset, "투사체 Behavior 삭제");
            behaviors.DeleteArrayElementAtIndex(index);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            RefreshAssets(asset);
        }

        private void MoveBehavior(ProjectileDefinitionSO asset, int from, int to)
        {
            var serializedObject = new SerializedObject(asset);
            SerializedProperty behaviors = serializedObject.FindProperty("behaviors");
            if (from < 0 || from >= behaviors.arraySize || to < 0 || to >= behaviors.arraySize)
                return;

            Undo.RecordObject(asset, "투사체 Behavior 순서 변경");
            behaviors.MoveArrayElement(from, to);
            serializedObject.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            RefreshAssets(asset);
        }

        private void FindReferences(ProjectileDefinitionSO asset)
        {
            if (asset == null)
                return;

            string selectedPath = AssetDatabase.GetAssetPath(asset);
            var references = new List<UnityEngine.Object>();
            string[] candidates = AssetDatabase
                .FindAssets("t:UPlayGroundMotionAbilityPayloadSO")
                .Concat(AssetDatabase.FindAssets("t:MotionSetAsset"))
                .Distinct()
                .ToArray();
            foreach (string guid in candidates)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!AssetDatabase.GetDependencies(path, false).Contains(selectedPath))
                    continue;

                UnityEngine.Object reference = AssetDatabase.LoadMainAssetAtPath(path);
                if (reference != null)
                    references.Add(reference);
            }

            Selection.objects = references.ToArray();
            Debug.Log(references.Count == 0
                ? $"[ProjectileAuthoring] {asset.name} 사용처가 없습니다."
                : $"[ProjectileAuthoring] {asset.name} 사용처 {references.Count}개를 선택했습니다.");
        }

        private void ValidateAllDefinitions()
        {
            var errors = new List<string>();
            foreach (ProjectileDefinitionSO asset in Assets)
                asset?.CollectValidationErrors(errors);

            EditorUtility.DisplayDialog(
                "ProjectileDefinition 전체 검증",
                errors.Count == 0
                    ? $"{Assets.Count}개 Definition이 모두 유효합니다."
                    : $"오류 {errors.Count}개\n\n{string.Join("\n", errors.Take(20))}"
                      + (errors.Count > 20 ? "\n…" : string.Empty),
                "확인");
        }

        private static float EstimateTravelDistance(ProjectileDefinitionSO definition)
        {
            float lifetime = Mathf.Max(0f, definition.lifetime);
            return definition.motion switch
            {
                LinearProjectileMotion linear => linear.speed * lifetime
                    + 0.5f * linear.acceleration * lifetime * lifetime,
                ArcProjectileMotion arc => arc.speed * lifetime,
                HomingProjectileMotion homing => homing.speed * lifetime,
                OrbitProjectileMotion orbit => 2f * Mathf.PI * orbit.radius
                    * Mathf.Abs(orbit.angularSpeed) / 360f * lifetime,
                HitscanProjectileMotion hitscan => hitscan.range,
                _ => 0f,
            };
        }

        private static int EstimateSplitPeak(ProjectileDefinitionSO root)
        {
            int total = 1;
            ProjectileDefinitionSO current = root;
            int multiplier = 1;
            var visited = new HashSet<ProjectileDefinitionSO>();
            for (int generation = 0;
                 current != null && generation < Mathf.Max(0, root.maxGeneration);
                 generation++)
            {
                if (!visited.Add(current))
                    break;
                SplitProjectileBehavior split = current.GetBehavior<SplitProjectileBehavior>();
                if (split?.childDefinition == null)
                    break;
                multiplier *= Mathf.Max(1, split.count);
                total += multiplier;
                current = split.childDefinition;
            }
            return total;
        }

        private static string FriendlyTypeName(object value)
        {
            if (value == null)
                return "None";
            return value.GetType().Name
                .Replace("ProjectileMotion", string.Empty)
                .Replace("ProjectileBehavior", string.Empty);
        }

        private static VisualElement MakeSection(string title)
        {
            var section = new VisualElement();
            section.style.marginBottom = 10f;
            section.style.paddingLeft = 10f;
            section.style.paddingRight = 10f;
            section.style.paddingTop = 8f;
            section.style.paddingBottom = 8f;
            DataAuthoringTheme.SetBorder(section);
            DataAuthoringTheme.Round(section, 4f);

            var heading = new Label(title);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 6f;
            section.Add(heading);
            return section;
        }
    }
}
#endif
