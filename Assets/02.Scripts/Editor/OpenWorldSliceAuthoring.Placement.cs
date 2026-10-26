using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Editor
{
    public static partial class OpenWorldSliceAuthoring
    {
        private static readonly string[] VegetationNames = { "tree", "pine", "bamboo", "birch", "beech", "shrub", "bush", "grass", "fern", "flower", "orchard" };

        private static bool IsVegetation(Transform item)
        {
            string label = item.name.ToLowerInvariant();
            string asset = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(item.gameObject).ToLowerInvariant();
            return VegetationNames.Any(word => label.Contains(word) || asset.Contains(word));
        }

        private static bool TryGetGround(Terrain[] terrains, Vector3 position, out float height)
        {
            foreach (var terrain in terrains)
            {
                Vector3 p=position-terrain.transform.position;
                if(p.x<0 || p.z<0 || p.x>terrain.terrainData.size.x || p.z>terrain.terrainData.size.z) continue;
                height=terrain.SampleHeight(position)+terrain.transform.position.y;
                return true;
            }
            height=0;
            return false;
        }

        private static bool TryGetRenderBounds(Transform item, out Bounds bounds)
        {
            var renderers=item.GetComponentsInChildren<MeshRenderer>(true);
            bounds=default;
            if(renderers.Length==0)return false;
            bounds=renderers[0].bounds;
            foreach(var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            return true;
        }

        private static void RepairVegetation(Transform environment, Terrain[] terrains, CityRecipe recipe)
        {
            var roots=new HashSet<Transform>();
            foreach(var renderer in environment.GetComponentsInChildren<MeshRenderer>(true))
            {
                var root=PrefabUtility.GetOutermostPrefabInstanceRoot(renderer.gameObject);
                Transform item=root!=null?root.transform:renderer.GetComponentInParent<LODGroup>()?.transform;
                if(item==null) continue;
                if(item.GetComponentInParent<GameActor>()!=null || item.GetComponentsInChildren<Components.SceneEntityId>(true).Length!=0) continue;
                if(IsVegetation(item)) roots.Add(item);
            }
            // 도시의 절차 생성 나무에는 Prefab/LODGroup이 없을 수 있다.
            foreach(var city in recipe.cities)
            {
                Transform art=environment.Find(city.mapId+"_Environment");
                foreach(Transform item in art) if(IsVegetation(item)) roots.Add(item);
            }
            var report=new List<string>();
            var kept=new List<(Vector3 position,float radius)>();
            var solids=environment.GetComponentsInChildren<Collider>(true)
                .Where(c=>!(c is TerrainCollider) && !c.isTrigger && c.enabled && c.gameObject.activeInHierarchy
                    && c.GetComponentInParent<GameActor>()==null && !HasAncestor(c.transform, roots)).ToArray();
            foreach(var item in roots.OrderBy(t=>AnimationUtility.CalculateTransformPath(t,null)))
            {
                if(!item.gameObject.activeInHierarchy || !TryGetRenderBounds(item,out var bounds))continue;
                string path=AnimationUtility.CalculateTransformPath(item,null);
                if(!TryGetGround(terrains,item.position,out float ground))
                {
                    item.gameObject.SetActive(false);
                    report.Add("제외: 지면 범위 밖 | "+path);
                    continue;
                }
                float contact=Mathf.Max(item.position.y,bounds.min.y);
                float correction=ground-contact;
                if(Mathf.Abs(correction)>0.03f)
                {
                    item.position+=Vector3.up*correction;
                    report.Add("접지 | "+path+" | dy="+correction.ToString("F4"));
                }
                bool tall=bounds.size.y>3;
                float radius=tall?Mathf.Clamp(Mathf.Min(bounds.size.x,bounds.size.z)*0.07f,0.3f,1.2f):0.25f;
                bool overlap=RoadDistance(item.position,recipe,out _) < recipe.roadHalfWidth+radius
                    || ConnectorDistance(item.position)<2.4f+radius;
                if(tall)
                {
                    foreach(var other in kept)
                    {
                        Vector2 delta=new Vector2(item.position.x-other.position.x,item.position.z-other.position.z);
                        if(delta.sqrMagnitude<(radius+other.radius)*(radius+other.radius)){overlap=true;break;}
                    }
                    foreach(var solid in solids)
                    {
                        if(!solid.gameObject.activeInHierarchy)continue;
                        // 줄기와 실체가 겹치는 경우만 제거한다. 수관끼리 겹친다는 이유로 숲을 비우지 않는다.
                        Vector3 trunk=item.position+Vector3.up*1.5f;
                        if(solid.bounds.SqrDistance(trunk)>=radius*radius)continue;
                        // 비볼록 MeshCollider의 ClosestPoint는 지원되지 않아 입력점 자체를 돌려줄 수 있다.
                        Vector3 closest=solid is MeshCollider mesh && !mesh.convex
                            ? solid.bounds.ClosestPoint(trunk) : solid.ClosestPoint(trunk);
                        if((closest-trunk).sqrMagnitude<radius*radius){overlap=true;break;}
                    }
                }
                if(overlap)
                {
                    if (TryRelocateVegetation(item, radius, terrains, recipe, solids, kept))
                    {
                        report.Add("재배치: 통행로/줄기/구조물 중첩 | " + path + " | " + item.position);
                        if (tall) kept.Add((item.position, radius));
                    }
                    else
                    {
                        item.gameObject.SetActive(false);
                        report.Add("제외: 인접 재배치 후보 실패 | "+path);
                    }
                }
                else if(tall)kept.Add((item.position,radius));
            }
            File.WriteAllLines(ReportFolder+"/placement-repair.txt",report);
        }

        private static bool TryRelocateVegetation(Transform item, float radius, Terrain[] terrains,
            CityRecipe recipe, Collider[] solids, List<(Vector3 position, float radius)> kept)
        {
            Vector3 origin = item.position;
            // 한 나무씩 가까운 숲 가장자리로 옮긴다. 수관 폭으로 군락 전체를 제거하지 않는다.
            for (int ring = 1; ring <= 4; ring++)
            for (int direction = 0; direction < 8; direction++)
            {
                float angle = direction * Mathf.PI / 4;
                Vector3 candidate = origin + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * (ring * 4);
                if (RoadDistance(candidate, recipe, out _) < recipe.roadHalfWidth + radius + 2
                    || ConnectorDistance(candidate) < 2.4f + radius
                    || !TryGetGround(terrains, candidate, out float height)) continue;
                candidate.y = height;
                bool occupied = kept.Any(other => new Vector2(candidate.x - other.position.x,
                    candidate.z - other.position.z).sqrMagnitude < Mathf.Pow(radius + other.radius, 2));
                if (occupied || solids.Any(s => s != null && s.gameObject.activeInHierarchy
                    && s.bounds.SqrDistance(candidate + Vector3.up * 1.5f) < radius * radius)) continue;
                item.position = candidate;
                return true;
            }
            return false;
        }

        private static bool HasAncestor(Transform item, HashSet<Transform> roots)
        {
            for(var current=item;current!=null;current=current.parent)
                if(roots.Contains(current))return true;
            return false;
        }
    }
}
