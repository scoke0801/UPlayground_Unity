using System.Collections.Generic;

namespace UPlayGround.Editor.P09Builder
{
    /// <summary>
    /// P09CharacterPrefabBuilderWindow의 탭 1개를 표현하는 인터페이스.
    /// </summary>
    public interface IBuilderTab
    {
        string Title { get; }

        void Initialize(P09CharacterPrefabBuilderWindow window, P09AssetCatalog catalog);
        void OnGUI(CharacterBuildConfig config, P09AssetCatalog catalog, IconResolver iconResolver);
        IEnumerable<string> Validate(CharacterBuildConfig config);
    }
}
