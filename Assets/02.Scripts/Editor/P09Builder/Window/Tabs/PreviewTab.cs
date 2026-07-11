using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor.P09Builder
{
    internal sealed class PreviewTab : IBuilderTab
    {
        public string Title => "미리보기";

        private P09CharacterPrefabBuilderWindow _window;

        public void Initialize(P09CharacterPrefabBuilderWindow window, P09AssetCatalog catalog)
        {
            _window = window;
        }

        public void OnGUI(CharacterBuildConfig config, P09AssetCatalog catalog, IconResolver iconResolver)
        {
            if (_window == null)
            {
                EditorGUILayout.HelpBox("미리보기 컨트롤러를 초기화할 수 없습니다.", MessageType.Warning);
                return;
            }

            _window.DrawPreviewControls();
        }

        public IEnumerable<string> Validate(CharacterBuildConfig config)
        {
            yield break;
        }
    }
}
