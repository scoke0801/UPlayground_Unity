#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Sound;

namespace UPlayGround.Data.Editor.Authoring
{
    [InitializeOnLoad]
    internal static class SoundDomainRegistration
    {
        static SoundDomainRegistration()
        {
            DataAuthoringDomainRegistry.Register(
                SoundDomainPanel.DomainKey,
                "사운드",
                () => new SoundDomainPanel(),
                500);
        }
    }

    /// <summary>
    /// SoundEntrySO의 재생 설정 편집과 SoundDatabaseSO 동기화를 담당합니다.
    /// </summary>
    public sealed class SoundDomainPanel : DataDomainPanel<SoundEntrySO>
    {
        public const string DomainKey = "sounds";
        private const string DefaultPath = "Assets/10.Datas/Sound/SoundEntry";

        private SoundDatabaseSO _database;

        public override string DomainId => DomainKey;
        public override string DisplayName => "사운드";
        public override Texture2D Icon => EditorGUIUtility.IconContent("AudioSource Icon").image as Texture2D;
        protected override float ListPanelWidth => 340f;
        protected override string CreateButtonLabel => "+ 새 사운드";
        protected override bool CanCreate => true;
        protected override bool CanDuplicate(SoundEntrySO asset) => asset != null;
        protected override bool CanDelete(SoundEntrySO asset) => asset != null;

        protected override IEnumerable<SoundEntrySO> LoadAssets()
        {
            LoadDatabase();
            return FindAllEntries().OrderBy(ResolveKey, StringComparer.OrdinalIgnoreCase);
        }

        protected override string KeyOf(SoundEntrySO asset) => ResolveKey(asset);

        protected override string LabelOf(SoundEntrySO asset)
        {
            if (asset == null)
                return string.Empty;
            string clip = asset.clip != null ? asset.clip.name : "Clip 없음";
            return $"{ResolveKey(asset)}  ·  {asset.bus}  ·  {clip}";
        }

        protected override IEnumerable<DataDomainFilter<SoundEntrySO>> CreateFilters()
        {
            foreach (SoundBusType bus in Enum.GetValues(typeof(SoundBusType)))
            {
                SoundBusType captured = bus;
                yield return new DataDomainFilter<SoundEntrySO>(bus.ToString(), asset => asset.bus == captured);
            }
        }

        protected override void AddToolbarActions(Toolbar toolbar)
        {
            var actions = new ToolbarMenu { text = "사운드 작업" };
            actions.menu.AppendAction("SoundDatabase 동기화", _ => SyncDatabase());
            actions.menu.AppendAction("SoundDatabase 선택", _ => Selection.activeObject = _database,
                _ => _database != null ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            toolbar.Add(actions);
        }

        protected override void CreateNew()
        {
            string key = MakeUniqueKey("Sound_New");
            SoundEntrySO created = AssetCrudService.CreateAsset<SoundEntrySO>(
                DefaultPath,
                key,
                entry =>
                {
                    entry.key = key;
                    entry.bus = SoundBusType.SFX;
                    entry.distanceMode = SoundDistanceMode.Logarithmic3D;
                    entry.volume = 1f;
                    entry.pitchMin = 1f;
                    entry.pitchMax = 1f;
                    entry.minDistance = 1.5f;
                    entry.maxDistance = 24f;
                    entry.maxSimultaneous = 4;
                    entry.priority = 128;
                },
                "사운드 엔트리 생성");
            SyncDatabase();
            EditorGUIUtility.PingObject(created);
            RefreshAssets(created);
        }

        protected override SoundEntrySO Duplicate(SoundEntrySO asset)
        {
            string key = MakeUniqueKey($"{ResolveKey(asset)}_copy");
            SoundEntrySO copy = AssetCrudService.DuplicateAsset(
                asset,
                duplicated => duplicated.key = key,
                "사운드 엔트리 복제");
            SyncDatabase();
            EditorGUIUtility.PingObject(copy);
            return copy;
        }

        protected override bool Delete(SoundEntrySO asset)
        {
            if (!EditorUtility.DisplayDialog(
                    "사운드 엔트리 삭제",
                    $"'{ResolveKey(asset)}'을 삭제할까요?\n문자열 키를 사용하는 호출부는 자동으로 갱신되지 않습니다.",
                    "삭제",
                    "취소"))
            {
                return false;
            }

            bool deleted = AssetCrudService.DeleteAsset(asset, "사운드 엔트리 삭제");
            if (deleted)
                SyncDatabase();
            return deleted;
        }

        protected override IEnumerable<DataAuthoringIssue> GetIssues(SoundEntrySO asset)
        {
            if (string.IsNullOrWhiteSpace(ResolveKey(asset)))
                yield return Error("사운드 key가 비어 있습니다.", asset);
            else if (Assets.Count(other => other != null &&
                         string.Equals(ResolveKey(other), ResolveKey(asset), StringComparison.OrdinalIgnoreCase)) > 1)
                yield return Error($"사운드 key '{ResolveKey(asset)}'가 중복됩니다.", asset);

            if (asset.clip == null)
                yield return Error("AudioClip이 연결되지 않았습니다.", asset);

            if (asset.pitchMin <= 0f || asset.pitchMax <= 0f || asset.pitchMin > asset.pitchMax)
                yield return Warning("Pitch 범위가 올바르지 않습니다.", asset);

            if (asset.maxSimultaneous < 0)
                yield return Warning("최대 동시 재생 수가 음수입니다.", asset);

            if (asset.cooldown < 0f)
                yield return Warning("Cooldown이 음수입니다.", asset);

            if (asset.distanceMode != SoundDistanceMode.None2D)
            {
                if (asset.minDistance <= 0f || asset.maxDistance <= asset.minDistance)
                    yield return Warning("3D 거리 범위는 0 < minDistance < maxDistance여야 합니다.", asset);
            }

            if (asset.distanceMode == SoundDistanceMode.Custom3D
                && (asset.customRolloff == null || asset.customRolloff.length == 0))
            {
                yield return Warning("Custom3D의 Rolloff Curve가 비어 있습니다.", asset);
            }
        }

        protected override VisualElement BuildDetail(SoundEntrySO asset)
        {
            var detail = new VisualElement();
            var serializedObject = new SerializedObject(asset);

            var header = new Toolbar();
            var title = new Label(ResolveKey(asset));
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            header.Add(title);
            var spacer = new VisualElement();
            spacer.style.flexGrow = 1f;
            header.Add(spacer);
            header.Add(new ToolbarButton(() => EditorGUIUtility.PingObject(asset)) { text = "Project에서 열기" });
            detail.Add(header);

            VisualElement identity = MakeSection("식별 · 클립");
            AddProperty(identity, "key", "Key");
            AddProperty(identity, "clip", "Audio Clip");
            AddProperty(identity, "bus", "Bus");
            detail.Add(identity);

            VisualElement playback = MakeSection("재생 설정");
            AddProperty(playback, "volume", "Volume");
            AddProperty(playback, "pitchMin", "Pitch 최소");
            AddProperty(playback, "pitchMax", "Pitch 최대");
            AddProperty(playback, "priority", "Priority");
            detail.Add(playback);

            VisualElement spatial = MakeSection("거리 · 공간화");
            AddProperty(spatial, "distanceMode", "거리 모드");
            AddProperty(spatial, "minDistance", "최소 거리");
            AddProperty(spatial, "maxDistance", "최대 거리");
            AddProperty(spatial, "customRolloff", "Custom Rolloff");
            AddProperty(spatial, "preCullByMaxDistance", "최대 거리 사전 컬링");
            detail.Add(spatial);

            VisualElement limits = MakeSection("재생 제한");
            AddProperty(limits, "cooldown", "Cooldown");
            AddProperty(limits, "maxSimultaneous", "최대 동시 재생");
            detail.Add(limits);

            detail.TrackSerializedObjectValue(serializedObject, _ =>
            {
                title.text = ResolveKey(asset);
                NotifyAssetChanged(asset);
            });
            detail.Bind(serializedObject);
            return detail;

            void AddProperty(VisualElement parent, string path, string label)
            {
                SerializedProperty property = serializedObject.FindProperty(path);
                if (property != null)
                    parent.Add(new PropertyField(property, label));
            }
        }

        private void SyncDatabase()
        {
            LoadDatabase();
            if (_database == null)
            {
                EditorUtility.DisplayDialog("SoundDatabase 없음", "프로젝트에서 SoundDatabaseSO를 찾을 수 없습니다.", "확인");
                return;
            }

            SoundEntrySO[] discovered = FindAllEntries().ToArray();
            var discoveredSet = new HashSet<SoundEntrySO>(discovered);
            var entries = _database.Entries
                .Where(entry => entry != null && discoveredSet.Remove(entry))
                .Concat(discoveredSet.OrderBy(ResolveKey, StringComparer.OrdinalIgnoreCase))
                .ToArray();
            Undo.RecordObject(_database, "SoundDatabase 동기화");
            var serializedDatabase = new SerializedObject(_database);
            SerializedProperty property = serializedDatabase.FindProperty("entries");
            property.arraySize = entries.Length;
            for (int i = 0; i < entries.Length; i++)
                property.GetArrayElementAtIndex(i).objectReferenceValue = entries[i];
            serializedDatabase.ApplyModifiedProperties();
            EditorUtility.SetDirty(_database);
            AssetDatabase.SaveAssets();
        }

        private void LoadDatabase()
        {
            if (_database != null)
                return;
            string guid = AssetDatabase.FindAssets("t:SoundDatabaseSO").FirstOrDefault();
            if (!string.IsNullOrEmpty(guid))
                _database = AssetDatabase.LoadAssetAtPath<SoundDatabaseSO>(AssetDatabase.GUIDToAssetPath(guid));
        }

        private string MakeUniqueKey(string baseKey)
        {
            string candidate = baseKey;
            int suffix = 2;
            while (Assets.Any(asset => string.Equals(ResolveKey(asset), candidate, StringComparison.OrdinalIgnoreCase)))
                candidate = $"{baseKey}_{suffix++}";
            return candidate;
        }

        private static IEnumerable<SoundEntrySO> FindAllEntries()
        {
            return AssetDatabase.FindAssets("t:SoundEntrySO")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<SoundEntrySO>)
                .Where(entry => entry != null);
        }

        private static string ResolveKey(SoundEntrySO entry)
        {
            if (entry == null)
                return string.Empty;
            return string.IsNullOrWhiteSpace(entry.key) ? entry.name : entry.key.Trim();
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

        private static DataAuthoringIssue Error(string message, UnityEngine.Object context)
            => new DataAuthoringIssue(DataAuthoringIssueSeverity.Error, message, context);

        private static DataAuthoringIssue Warning(string message, UnityEngine.Object context)
            => new DataAuthoringIssue(DataAuthoringIssueSeverity.Warning, message, context);
    }
}
#endif
