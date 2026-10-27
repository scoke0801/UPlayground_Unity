using System;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace UPlayGround.Editor
{
    public static partial class OpenWorldSliceAuthoring
    {
        private static void BuildNavigationPreferences(Transform environment, CityRecipe recipe)
        {
            if (string.IsNullOrEmpty(recipe.navigationOffRoadArea)) return;
            int area = NavMesh.GetAreaFromName(recipe.navigationOffRoadArea);
            if (area < 0 || NavMesh.GetAreaCost(area) <= NavMesh.GetAreaCost(0)
                || recipe.navigationPreferredWidth <= 0)
                throw new InvalidOperationException("길 밖 NavMesh 영역·비용과 우선 이동 폭을 확인하세요.");

            var previous = environment.Find("NavigationPreferences");
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            var root = new GameObject("NavigationPreferences").transform;
            root.SetParent(environment, false);
            foreach (var terrain in environment.GetComponentsInChildren<Terrain>())
            {
                var modifier = terrain.GetComponent<NavMeshModifier>();
                if (modifier == null) modifier = terrain.gameObject.AddComponent<NavMeshModifier>();
                modifier.overrideArea = true;
                modifier.area = area;
            }

            // 넓은 야지의 동등 비용 탐색이 노드 풀을 소진하지 않도록 저작한 길을 우선한다.
            // 길 밖을 막거나 장거리 조회를 여러 개의 짧은 조회로 바꾸지는 않는다.
            float height = recipe.landscape.worldSize.y;
            float centerY = recipe.landscape.worldOrigin.y + height * 0.5f;
            foreach (var line in GetRoadLines(recipe))
            for (int i = 1; i < line.Length; i++)
            {
                Vector3 delta = line[i] - line[i - 1];
                delta.y = 0;
                Vector3 center = (line[i] + line[i - 1]) * 0.5f;
                center.y = centerY;
                CreatePreferredNavigationVolume(root, center, Quaternion.LookRotation(delta),
                    new Vector3(recipe.navigationPreferredWidth, height, delta.magnitude + recipe.roadHalfWidth));
            }
            foreach (var city in recipe.cities) AddOpenArea(city.bounds.Value);
            foreach (var bounds in recipe.navigationOpenAreas) AddOpenArea(bounds.Value);

            void AddOpenArea(Rect bounds) => CreatePreferredNavigationVolume(root,
                new Vector3(bounds.center.x, centerY, bounds.center.y), Quaternion.identity,
                new Vector3(bounds.width, height, bounds.height));
        }

        private static void CreatePreferredNavigationVolume(Transform parent, Vector3 position,
            Quaternion rotation, Vector3 size)
        {
            var go = new GameObject("PreferredGround");
            go.transform.SetParent(parent, false);
            go.transform.SetPositionAndRotation(position, rotation);
            var volume = go.AddComponent<NavMeshModifierVolume>();
            volume.area = 0;
            volume.size = size;
        }
    }
}
