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
    internal static class ActorDomainRegistration
    {
        static ActorDomainRegistration()
        {
            DataAuthoringDomainRegistry.Register(
                ActorDomainPanel.DomainKey,
                "액터",
                () => new ActorDomainPanel(),
                50);
        }
    }

    /// <summary>
    /// ActorDefinitionSO의 식별·프리팹·스탯·전투·NPC 연결을 한곳에서 편집합니다.
    /// DB 순서 편집과 프리팹 ID 일괄 동기화는 기존 고급 도구로 연결합니다.
    /// </summary>
    public sealed class ActorDomainPanel : DataDomainPanel<ActorDefinitionSO>
    {
        public const string DomainKey = "actors";
        private const string DefaultPath = "Assets/10.Datas/Actor/DataBase";

        private ActorDatabase _database;

        public override string DomainId => DomainKey;
        public override string DisplayName => "액터";
        public override Texture2D Icon => EditorGUIUtility.IconContent("d_UnityEditor.HierarchyWindow").image as Texture2D;
        protected override float ListPanelWidth => 350f;
        protected override string CreateButtonLabel => "+ 새 액터";
        protected override bool CanCreate => true;
        protected override bool CanDuplicate(ActorDefinitionSO asset) => asset != null;
        protected override bool CanDelete(ActorDefinitionSO asset) => asset != null;

        protected override IEnumerable<ActorDefinitionSO> LoadAssets()
        {
            LoadDatabase();
            return FindAllDefinitions().OrderBy(definition => definition.actorId, StringComparer.Ordinal);
        }

        protected override string KeyOf(ActorDefinitionSO asset) => asset?.actorId;

        protected override string LabelOf(ActorDefinitionSO asset)
        {
            if (asset == null)
                return string.Empty;

            string displayName = string.IsNullOrWhiteSpace(asset.displayName) ? asset.name : asset.displayName;
            return $"{displayName}  ·  {asset.actorId}  ·  {ActorTypeLabel(asset.actorType)}";
        }

        protected override IEnumerable<DataDomainFilter<ActorDefinitionSO>> CreateFilters()
        {
            yield return new DataDomainFilter<ActorDefinitionSO>("플레이어", asset => asset.actorType.HasFlag(ActorType.Player));
            yield return new DataDomainFilter<ActorDefinitionSO>("몬스터", asset => asset.actorType.HasFlag(ActorType.Monster));
            yield return new DataDomainFilter<ActorDefinitionSO>("NPC", asset => asset.actorType.HasFlag(ActorType.NPC));
            yield return new DataDomainFilter<ActorDefinitionSO>("기타", asset =>
                !asset.actorType.HasFlag(ActorType.Player)
                && !asset.actorType.HasFlag(ActorType.Monster)
                && !asset.actorType.HasFlag(ActorType.NPC));
        }

        protected override void AddToolbarActions(Toolbar toolbar)
        {
            var actions = new ToolbarMenu { text = "액터 작업" };
            actions.menu.AppendAction("ActorDatabase 동기화", _ => SyncDatabase());
            actions.menu.AppendAction("ActorDatabase 선택", _ => Selection.activeObject = _database,
                _ => _database != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            actions.menu.AppendSeparator();
            actions.menu.AppendAction("고급 액터 DB 도구...", _ => DataAuthoringToolBridge.Execute(
                DataAuthoringToolBridge.ActorDatabaseEditor,
                "액터 데이터베이스 에디터"));
            toolbar.Add(actions);
        }

        protected override void CreateNew()
        {
            string actorId = MakeUniqueId("actor_new");
            ActorDefinitionSO created = AssetCrudService.CreateAsset<ActorDefinitionSO>(
                DefaultPath,
                $"ActorDef_{actorId}",
                definition =>
                {
                    definition.actorId = actorId;
                    definition.displayName = "새 액터";
                    definition.actorType = ActorType.Monster;
                    definition.level = 1;
                },
                "액터 정의 생성");
            SyncDatabase();
            EditorGUIUtility.PingObject(created);
            RefreshAssets(created);
        }

        protected override ActorDefinitionSO Duplicate(ActorDefinitionSO asset)
        {
            string id = MakeUniqueId(string.IsNullOrWhiteSpace(asset.actorId) ? "actor_copy" : $"{asset.actorId}_copy");
            ActorDefinitionSO copy = AssetCrudService.DuplicateAsset(
                asset,
                duplicated =>
                {
                    duplicated.actorId = id;
                    duplicated.displayName = $"{asset.displayName} (복사)";
                },
                "액터 정의 복제");
            SyncDatabase();
            EditorGUIUtility.PingObject(copy);
            return copy;
        }

        protected override bool Delete(ActorDefinitionSO asset)
        {
            if (!EditorUtility.DisplayDialog(
                    "액터 정의 삭제",
                    $"'{LabelOf(asset)}'을 삭제할까요?\n프리팹과 스폰 데이터의 actorId 참조를 먼저 확인하세요.",
                    "삭제",
                    "취소"))
            {
                return false;
            }

            bool deleted = AssetCrudService.DeleteAsset(asset, "액터 정의 삭제");
            if (deleted)
                SyncDatabase();
            return deleted;
        }

        protected override IEnumerable<DataAuthoringIssue> GetIssues(ActorDefinitionSO asset)
        {
            if (string.IsNullOrWhiteSpace(asset.actorId))
                yield return Error("actorId가 비어 있습니다.", asset);
            else if (HasDuplicateKey(asset))
                yield return Error($"Actor ID '{asset.actorId}'가 중복됩니다.", asset);

            if (asset.prefab == null)
                yield return Warning("런타임 스폰 프리팹이 연결되지 않았습니다.", asset);

            if (asset.actorType.HasFlag(ActorType.Monster) && asset.statData == null)
                yield return Error("몬스터 Actor의 Stat Data가 비어 있습니다.", asset);

            if (asset.actorType.HasFlag(ActorType.NPC) && asset.npcData == null)
                yield return Warning("NPC Actor의 NPC Data가 비어 있습니다.", asset);
        }

        protected override VisualElement BuildDetail(ActorDefinitionSO asset)
        {
            var detail = new VisualElement();
            var serializedObject = new SerializedObject(asset);
            detail.Add(BuildHeader(asset));

            VisualElement identity = MakeSection("식별");
            AddProperty(identity, "actorId", "Actor ID");
            AddProperty(identity, "displayName", "표시 이름");
            AddProperty(identity, "description", "설명");
            detail.Add(identity);

            VisualElement basics = MakeSection("Actor 기본 정보");
            AddProperty(basics, "actorType", "Actor 타입");
            AddProperty(basics, "characterType", "캐릭터 타입");
            AddProperty(basics, "targetLayerMask", "공격 대상 레이어");
            AddProperty(basics, "prefab", "런타임 프리팹");
            detail.Add(basics);

            VisualElement stats = MakeSection("스탯");
            AddLinkedProperty(stats, serializedObject, "statData", "Stat Data", StatDomainPanel.DomainKey);
            AddProperty(stats, "poiseData", "Poise Data");
            detail.Add(stats);

            VisualElement monster = MakeSection("몬스터 프로필 · 전투 · AI");
            AddProperty(monster, "monsterProfile", "몬스터 프로필");
            AddProperty(monster, "breakGaugeData", "브레이크 게이지 (레거시)");
            AddProperty(monster, "monsterScaling", "스케일링 (레거시)");
            AddProperty(monster, "grade", "등급");
            AddProperty(monster, "level", "레벨");
            AddProperty(monster, "combatElement", "전투 속성");
            AddProperty(monster, "elementAssignmentMode", "속성 할당 방식");
            AddProperty(monster, "elementalAdvantageMultiplier", "속성 우위 배율");
            AddProperty(monster, "abilitySet", "Ability Set");
            AddProperty(monster, "combatDefensePolicy", "방어 정책");
            AddProperty(monster, "combatReactionPolicy", "리액션 정책");
            AddProperty(monster, "behaviorData", "AI 행동 데이터");
            AddLinkedProperty(monster, serializedObject, "dropTable", "드랍 테이블", DropDomainPanel.DomainKey);
            AddProperty(monster, "recruitableAs", "처치 시 해금 캐릭터");
            AddProperty(monster, "expReward", "경험치 보상");
            AddProperty(monster, "goldReward", "골드 보상");
            detail.Add(monster);

            VisualElement npc = MakeSection("NPC 연결");
            AddLinkedProperty(npc, serializedObject, "npcData", "NPC Data", NpcDomainPanel.DomainKey);
            detail.Add(npc);

            SerializedProperty actorType = serializedObject.FindProperty("actorType");
            void UpdateConditionalSections()
            {
                var value = (ActorType)actorType.intValue;
                monster.style.display = value.HasFlag(ActorType.Monster) ? DisplayStyle.Flex : DisplayStyle.None;
                npc.style.display = value.HasFlag(ActorType.NPC) ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateConditionalSections();
            detail.TrackPropertyValue(actorType, _ => UpdateConditionalSections());
            detail.TrackSerializedObjectValue(serializedObject, _ => NotifyAssetChanged(asset));
            detail.Bind(serializedObject);
            return detail;

            void AddProperty(VisualElement parent, string path, string label)
            {
                SerializedProperty property = serializedObject.FindProperty(path);
                if (property != null)
                    parent.Add(new PropertyField(property, label));
            }
        }

        private static Toolbar BuildHeader(ActorDefinitionSO asset)
        {
            var header = new Toolbar();
            var title = new Label(string.IsNullOrWhiteSpace(asset.displayName) ? asset.name : asset.displayName);
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            header.Add(spacer);
            header.Add(new ToolbarButton(() => EditorGUIUtility.PingObject(asset)) { text = "Project에서 열기" });
            return header;
        }

        private static void AddLinkedProperty(
            VisualElement parent,
            SerializedObject serializedObject,
            string path,
            string label,
            string domainId)
        {
            SerializedProperty property = serializedObject.FindProperty(path);
            if (property == null)
                return;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            var field = new PropertyField(property, label);
            field.style.flexGrow = 1f;
            row.Add(field);
            var open = new Button(() =>
            {
                serializedObject.Update();
                DataAuthoringHubWindow.Open(domainId, serializedObject.FindProperty(path).objectReferenceValue);
            }) { text = "허브에서 열기" };
            open.style.width = 92f;
            row.Add(open);
            parent.Add(row);
        }

        private void SyncDatabase()
        {
            LoadDatabase();
            if (_database == null)
            {
                EditorUtility.DisplayDialog("ActorDatabase 없음", "프로젝트에서 ActorDatabase를 찾을 수 없습니다.", "확인");
                return;
            }

            ActorDefinitionSO[] discovered = FindAllDefinitions().ToArray();
            var discoveredSet = new HashSet<ActorDefinitionSO>(discovered);
            var definitions = _database.All
                .Where(definition => definition != null && discoveredSet.Remove(definition))
                .Concat(discoveredSet.OrderBy(definition => definition.actorId, StringComparer.Ordinal))
                .ToArray();
            Undo.RecordObject(_database, "ActorDatabase 동기화");
            var serializedDatabase = new SerializedObject(_database);
            SerializedProperty actors = serializedDatabase.FindProperty("_actors");
            actors.arraySize = definitions.Length;
            for (int i = 0; i < definitions.Length; i++)
                actors.GetArrayElementAtIndex(i).objectReferenceValue = definitions[i];
            serializedDatabase.ApplyModifiedProperties();
            _database.InvalidateLookup();
            EditorUtility.SetDirty(_database);
            AssetDatabase.SaveAssets();
        }

        private void LoadDatabase()
        {
            if (_database != null)
                return;
            string guid = AssetDatabase.FindAssets("t:ActorDatabase").FirstOrDefault();
            if (!string.IsNullOrEmpty(guid))
                _database = AssetDatabase.LoadAssetAtPath<ActorDatabase>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private string MakeUniqueId(string baseId)
        {
            string candidate = baseId;
            int suffix = 2;
            while (Assets.Any(asset => string.Equals(asset.actorId, candidate, StringComparison.Ordinal)))
                candidate = $"{baseId}_{suffix++}";
            return candidate;
        }

        private static IEnumerable<ActorDefinitionSO> FindAllDefinitions()
        {
            return AssetDatabase.FindAssets("t:ActorDefinitionSO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>)
                .Where(definition => definition != null);
        }

        private static VisualElement MakeSection(string title)
        {
            var section = new VisualElement();
            section.style.marginTop = 10f;
            section.style.paddingLeft = 8f;
            section.style.paddingRight = 8f;
            section.style.paddingTop = 7f;
            section.style.paddingBottom = 7f;
            section.style.backgroundColor = new Color(0.5f, 0.5f, 0.5f, 0.08f);
            var heading = new Label(title);
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            heading.style.marginBottom = 5f;
            section.Add(heading);
            return section;
        }

        private static string ActorTypeLabel(ActorType type)
        {
            if (type.HasFlag(ActorType.Player)) return "플레이어";
            if (type.HasFlag(ActorType.Monster)) return "몬스터";
            if (type.HasFlag(ActorType.NPC)) return "NPC";
            return type.ToString();
        }

        private static DataAuthoringIssue Error(string message, UnityEngine.Object context)
            => new DataAuthoringIssue(DataAuthoringIssueSeverity.Error, message, context);

        private static DataAuthoringIssue Warning(string message, UnityEngine.Object context)
            => new DataAuthoringIssue(DataAuthoringIssueSeverity.Warning, message, context);
    }
}
#endif
