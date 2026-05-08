using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Game.Editor.P09Builder
{
    internal sealed class WeaponTab : IBuilderTab
    {
        public string Title => "무기";

        public void Initialize(P09CharacterPrefabBuilderWindow window, P09AssetCatalog catalog) { }

        public void OnGUI(CharacterBuildConfig config, P09AssetCatalog catalog, IconResolver iconResolver)
        {
            if (config == null || catalog == null) return;

            config.UseWeaponGroup = EditorGUILayout.Toggle("그룹 사용", config.UseWeaponGroup);

            EditorGUILayout.Space();

            if (config.UseWeaponGroup)
            {
                EditorGUILayout.LabelField("무기 그룹", EditorStyles.boldLabel);
                config.WeaponGroupSo = IconGridDrawer.Draw(
                    ToReadOnly(catalog.WeaponGroups), config.WeaponGroupSo, iconResolver, columns: 4);
            }
            else
            {
                EditorGUILayout.LabelField("개별 무기 지정", EditorStyles.boldLabel);

                EditorGUILayout.LabelField("검 (Sword)", EditorStyles.miniBoldLabel);
                config.SwordSo = IconGridDrawer.Draw(
                    ToReadOnly(catalog.Swords), config.SwordSo, iconResolver);

                EditorGUILayout.Space(4);

                EditorGUILayout.LabelField("방패 (Shield)", EditorStyles.miniBoldLabel);
                config.ShieldSo = IconGridDrawer.Draw(
                    ToReadOnly(catalog.Shields), config.ShieldSo, iconResolver);

                EditorGUILayout.Space(6);
                config.ShowArrows = EditorGUILayout.Toggle("화살 표시", config.ShowArrows);
            }
        }

        public IEnumerable<string> Validate(CharacterBuildConfig config)
        {
            yield break;
        }

        private static IReadOnlyList<ScriptableObject> ToReadOnly(List<ScriptableObject> list)
        {
            return list != null
                ? (IReadOnlyList<ScriptableObject>)list
                : System.Array.Empty<ScriptableObject>();
        }
    }
}
