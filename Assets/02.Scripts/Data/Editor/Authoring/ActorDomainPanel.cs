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

            if (asset.attributeProfile == null)
            {
                yield return Error(
                    "Attribute Profile이 비어 있습니다.",
                    asset);
            }

            if (asset.actorType.HasFlag(ActorType.NPC) && asset.npcData == null)
                yield return Warning("NPC Actor의 NPC Data가 비어 있습니다.", asset);
        }

        protected override VisualElement BuildDetail(ActorDefinitionSO asset)
        {
            var serializedObject = new SerializedObject(asset);

            // 섹션 구성·디자인은 ActorDefinitionDetailView가 단일 소스다.
            // Inspector, 액터 데이터베이스 에디터와 동일한 화면을 공유한다.
            VisualElement detail = UPlayGround.Data.Editor.Actor.ActorDefinitionDetailView.Build(
                serializedObject,
                new UPlayGround.Data.Editor.Actor.ActorDefinitionDetailOptions
                {
                    ShowOpenHubButton = false,
                    ShowAssetHeader   = true,
                    ShowHubLinks      = true,
                });

            detail.TrackSerializedObjectValue(serializedObject, _ => NotifyAssetChanged(asset));
            return detail;
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
