using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        /// <summary>지정한 생활 소품만 재적용하고 기존 씬 참조를 유지한다.</summary>
        internal static void ApplyWorkbenchProps(Scene scene, string[] names)
        {
            Layout layout = ReadLayout();
            Transform environment = FindQualityTransform(scene, "AuthoredEnvironment");
            Transform parent = environment.Find("LandscapeDistrict");
            Terrain terrain = environment.GetComponentInChildren<Terrain>();
            foreach (string name in names)
            {
                WorldProp prop = layout.district.props.Single(x => x.name == name);
                GameObject existing = parent.Find(name)?.gameObject;
                if (existing != null && UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(existing)
                    != RequireEnvironmentPrefab(prop.prefab))
                    throw new InvalidOperationException("소품 원본 불일치: " + name);
                PlaceWorldProp(prop, terrain, parent, existing);
            }
            Physics.SyncTransforms();
        }
    }
}
