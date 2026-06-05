using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.EnumType;

namespace Game.Editor.P09Builder
{
    internal sealed class BasicInfoTab : IBuilderTab
    {
        public string Title => "기본정보";

        private P09CharacterPrefabBuilderWindow _window;

        private static readonly string[] _actorKindLabels = { "Enemy", "Player", "NPC" };

        public void Initialize(P09CharacterPrefabBuilderWindow window, P09AssetCatalog catalog)
        {
            _window = window;
        }

        public void OnGUI(CharacterBuildConfig config, P09AssetCatalog catalog, IconResolver iconResolver)
        {
            if (config == null) return;

            // ---------- Actor 타입 ----------
            EditorGUILayout.LabelField("Actor 타입", EditorStyles.boldLabel);
            int kindIdx = (int)config.ActorKind;
            int newKindIdx = GUILayout.SelectionGrid(kindIdx, _actorKindLabels, 3, GUILayout.Height(24f));
            if (newKindIdx != kindIdx)
                config.ActorKind = (BuilderActorKind)newKindIdx;

            EditorGUILayout.Space();

            // ---------- 캐릭터 슬롯 (Player 전용) ----------
            using (new EditorGUI.DisabledGroupScope(config.ActorKind != BuilderActorKind.Player))
            {
                config.PlayerCharacterType = (CharacterActorType)EditorGUILayout.EnumPopup(
                    "캐릭터 슬롯", config.PlayerCharacterType);
            }

            // ---------- 성별 ----------
            config.Sex = (BuilderSex)EditorGUILayout.EnumPopup("성별", config.Sex);
            config.IsRandomAppearance = EditorGUILayout.Toggle("랜덤 외형 태그", config.IsRandomAppearance);

            // ---------- BustSize (Female 전용) ----------
            using (new EditorGUI.DisabledGroupScope(config.Sex != BuilderSex.Female))
            {
                config.BustSizeSo = EditorGUILayout.ObjectField(
                    "체형(Bust)", config.BustSizeSo, typeof(ScriptableObject), false) as ScriptableObject;
            }

            EditorGUILayout.Space();

            // ---------- 명명 ----------
            EditorGUILayout.LabelField("명명", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                var previewName = _window != null ? _window.PreviewName : "(unknown)";
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUILayout.TextField("자동 이름", previewName);
                }
                if (GUILayout.Button("재생성", GUILayout.Width(60f)))
                {
                    _window?.RegeneratePreviewName();
                }
            }
            config.UseManualName = EditorGUILayout.Toggle("수동 이름", config.UseManualName);
            if (config.UseManualName)
            {
                config.ManualName = EditorGUILayout.TextField("이름", config.ManualName);
            }

            EditorGUILayout.Space();

            // ---------- 저장 경로 ----------
            EditorGUILayout.LabelField("저장 경로", EditorStyles.boldLabel);
            config.SaveBaseFolder = EditorGUILayout.TextField("Base Folder", config.SaveBaseFolder);

            var kindFolder = CharacterNameGenerator.GetKindFolderName(config.ActorKind);
            var previewFolder = PathConfig.GetPrefabFolder(
                config.SaveBaseFolder,
                kindFolder,
                _window != null ? _window.PreviewName : "<name>");
            EditorGUILayout.LabelField("Resolved", previewFolder, EditorStyles.miniLabel);

            EditorGUILayout.Space();

            // ---------- MagicaCloth ----------
            config.UseMagicaCloth = EditorGUILayout.Toggle("MagicaCloth 물리", config.UseMagicaCloth);
        }

        public IEnumerable<string> Validate(CharacterBuildConfig config)
        {
            if (config == null) yield break;
            if (config.UseManualName && string.IsNullOrWhiteSpace(config.ManualName))
                yield return "[기본정보] 수동 이름이 비어있습니다.";
            if (string.IsNullOrWhiteSpace(config.SaveBaseFolder))
                yield return "[기본정보] 저장 폴더가 비어있습니다.";
        }
    }
}
