#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Editor.Authoring
{
    [InitializeOnLoad]
    internal static class NpcDomainRegistration
    {
        static NpcDomainRegistration()
        {
            DataAuthoringDomainRegistry.Register(
                NpcDomainPanel.DomainKey,
                "NPC",
                () => new NpcDomainPanel(),
                410);
        }
    }

    /// <summary>
    /// NpcActorSO를 직접 편집하고 ActorDefinition 연동 생성기로 연결합니다.
    /// </summary>
    public sealed class NpcDomainPanel : DataDomainPanel<NpcActorSO>
    {
        public const string DomainKey = "npcs";
        private const string DefaultPath = "Assets/10.Datas/Actor/Npc";

        public override string DomainId => DomainKey;
        public override string DisplayName => "NPC";
        public override Texture2D Icon => EditorGUIUtility.IconContent("d_UnityEditor.SceneHierarchyWindow").image as Texture2D;
        protected override string CreateButtonLabel => "+ 새 NPC 데이터";
        protected override bool CanCreate => true;
        protected override bool CanDuplicate(NpcActorSO asset) => asset != null;
        protected override bool CanDelete(NpcActorSO asset) => asset != null;

        protected override IEnumerable<NpcActorSO> LoadAssets()
        {
            return AssetDatabase.FindAssets("t:NpcActorSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<NpcActorSO>)
                .Where(asset => asset != null)
                .OrderBy(LabelOf, StringComparer.CurrentCulture);
        }

        protected override string KeyOf(NpcActorSO asset) => asset?.actorName;

        protected override string LabelOf(NpcActorSO asset)
        {
            if (asset == null)
                return string.Empty;
            string name = string.IsNullOrWhiteSpace(asset.actorName) ? asset.name : asset.actorName;
            string dialogue = asset.dialogueGraph != null ? asset.dialogueGraph.name : "대화 없음";
            return $"{name}  ·  HP {asset.hp}  ·  {dialogue}";
        }

        protected override IEnumerable<DataDomainFilter<NpcActorSO>> CreateFilters()
        {
            yield return new DataDomainFilter<NpcActorSO>("대화 연결", asset => asset.dialogueGraph != null);
            yield return new DataDomainFilter<NpcActorSO>("대화 없음", asset => asset.dialogueGraph == null);
        }

        protected override void AddToolbarActions(Toolbar toolbar)
        {
            var generator = new ToolbarButton(() => DataAuthoringToolBridge.Execute(
                DataAuthoringToolBridge.NpcGenerator,
                "NPC+Definition 생성기")) { text = "NPC+Definition 생성기" };
            generator.tooltip = "NpcActorSO와 ActorDefinitionSO를 함께 생성·연결합니다.";
            toolbar.Add(generator);
        }

        protected override void CreateNew()
        {
            NpcActorSO created = AssetCrudService.CreateAsset<NpcActorSO>(
                DefaultPath,
                "NPC_New",
                npc =>
                {
                    npc.actorName = "새 NPC";
                    npc.hp = 1;
                    npc.interactionObjectType = InteractionObjectType.NPC;
                    npc.showInfoUI = false;
                    npc.showShakeEffect = false;
                },
                "NPC 데이터 생성");
            EditorGUIUtility.PingObject(created);
            RefreshAssets(created);
        }

        protected override NpcActorSO Duplicate(NpcActorSO asset)
        {
            NpcActorSO copy = AssetCrudService.DuplicateAsset(
                asset,
                duplicated => duplicated.actorName = (asset.actorName ?? asset.name) + " (복사)",
                "NPC 데이터 복제");
            EditorGUIUtility.PingObject(copy);
            return copy;
        }

        protected override bool Delete(NpcActorSO asset)
        {
            if (!EditorUtility.DisplayDialog(
                    "NPC 데이터 삭제",
                    $"'{LabelOf(asset)}' 자산을 삭제할까요?\nActorDefinitionSO의 npcData 참조가 남을 수 있습니다.",
                    "삭제",
                    "취소"))
            {
                return false;
            }

            return AssetCrudService.DeleteAsset(asset, "NPC 데이터 삭제");
        }

        protected override IEnumerable<DataAuthoringIssue> GetIssues(NpcActorSO asset)
        {
            if (HasDuplicateKey(asset))
            {
                yield return new DataAuthoringIssue(
                    DataAuthoringIssueSeverity.Warning,
                    $"NPC 이름 '{asset.actorName}'이 중복됩니다.",
                    asset);
            }

            if (asset.dialogueGraph == null)
            {
                yield return new DataAuthoringIssue(
                    DataAuthoringIssueSeverity.Info,
                    "기본 Dialogue Graph가 연결되지 않았습니다.",
                    asset);
            }
        }

        protected override VisualElement BuildDetail(NpcActorSO asset)
        {
            var detail = new VisualElement();
            var serializedObject = new SerializedObject(asset);

            var header = new Toolbar();
            var title = new Label(string.IsNullOrWhiteSpace(asset.actorName) ? asset.name : asset.actorName);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            header.Add(spacer);
            var path = new Label(AssetDatabase.GetAssetPath(asset));
            path.style.fontSize = 10f;
            header.Add(path);
            header.Add(new ToolbarButton(() => EditorGUIUtility.PingObject(asset)) { text = "Project에서 열기" });
            detail.Add(header);

            var basic = MakeSection("NPC 기본 데이터");
            AddProperty(basic, "actorName", "표시 이름");
            AddProperty(basic, "description", "설명");
            AddProperty(basic, "hp", "HP");
            AddProperty(basic, "dialogueGraph", "기본 대화");
            detail.Add(basic);

            var interaction = MakeSection("상호작용");
            AddProperty(interaction, "interactionCompleteDuration", "완료 유지 시간");
            AddProperty(interaction, "interactionMotionSlot", "플레이어 모션");
            detail.Add(interaction);

            var advanced = MakeSection("추가 설정");
            AddProperty(advanced, "fishingDepleteCatchCount", "낚시 소진 횟수");
            AddProperty(advanced, "reviveDowned", "휴식 시 전투불능 부활");
            detail.Add(advanced);

            var duplicate = new HelpBox(string.Empty, HelpBoxMessageType.Warning);
            detail.Add(duplicate);
            void RefreshState()
            {
                title.text = string.IsNullOrWhiteSpace(asset.actorName) ? asset.name : asset.actorName;
                duplicate.text = $"NPC 이름 '{asset.actorName}'이 다른 자산과 중복됩니다.";
                duplicate.style.display = HasDuplicateKey(asset) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            RefreshState();
            detail.TrackSerializedObjectValue(serializedObject, _ =>
            {
                NotifyAssetChanged(asset);
                RefreshState();
            });
            detail.Bind(serializedObject);
            return detail;

            void AddProperty(VisualElement parent, string propertyPath, string label)
            {
                SerializedProperty property = serializedObject.FindProperty(propertyPath);
                if (property != null)
                    parent.Add(new PropertyField(property, label));
            }
        }

        private static VisualElement MakeSection(string title)
        {
            var section = new VisualElement();
            section.style.marginTop = 10f;
            var heading = new Label(title);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 5f;
            section.Add(heading);
            return section;
        }
    }
}
#endif
