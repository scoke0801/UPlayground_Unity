using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;
using UPlayGround.Data.Save;
using UPlayGround.Manager;

namespace UPlayGround.Tool.Editor.Map
{
    public static partial class ScenarioMapAuthoring
    {
        private static void CheckAcceptanceRespawnMigration()
        {
            MethodInfo resolve = typeof(MonsterRespawnManager).GetMethod("ResolveRespawnPose", BindingFlags.Instance | BindingFlags.NonPublic);
            if (resolve == null) throw new InvalidOperationException("재스폰 위치 복구 계약이 없습니다.");
            var ids = new HashSet<string>();
            foreach (AcceptanceLeg leg in s_acceptance.legs)
                foreach (string id in leg.observeIds) ids.Add(id);
            var authored = JsonUtility.FromJson<TraversalSurvey>(File.ReadAllText(TraversalDirectory + "/Survey.json"));
            var positions = new List<string>();
            foreach (string id in ids)
            {
                var saved = new MonsterRespawnState { guid = id, position = new SerializableVector3(Vector3.zero),
                    rotation = new SerializableQuaternion(Quaternion.identity), waitingRespawn = true,
                    respawnCount = 7, nextRespawnGameMinute = 99999 };
                Pose pose = (Pose)resolve.Invoke(MonsterRespawnManager.Instance, new object[] { saved });
                TraversalObject source = authored.objects.Find(item => item.sceneEntityId == id);
                positions.Add($"{id}: source={source?.position}, registry={pose.position}, live={s_acceptanceEnemies[id].transform.position}");
                File.WriteAllLines(TraversalDirectory + "/RespawnPositions.txt", positions);
                if (source == null || Vector3.Distance(pose.position, source.position) > 0.5f
                    || saved.respawnCount != 7 || saved.nextRespawnGameMinute != 99999 || !saved.waitingRespawn)
                    throw new InvalidOperationException("현재 배치 복구 또는 저장 진행도 보존 실패: " + positions[positions.Count - 1]);
            }
            var fallback = new MonsterRespawnState { guid = Guid.NewGuid().ToString("N"),
                position = new SerializableVector3(new Vector3(1, 2, 3)),
                rotation = new SerializableQuaternion(Quaternion.Euler(0, 70, 0)) };
            Pose fallbackPose = (Pose)resolve.Invoke(MonsterRespawnManager.Instance, new object[] { fallback });
            if (Vector3.Distance(fallbackPose.position, fallback.position.ToVector3()) > 0.001f
                || Quaternion.Angle(fallbackPose.rotation, fallback.rotation.ToQuaternion()) > 0.01f)
                throw new InvalidOperationException("씬 배치 없는 저장 좌표 폴백 실패");
            File.WriteAllText(TraversalDirectory + "/RespawnMigration.json",
                "{\"placements\":" + ids.Count + ",\"fallbackPreserved\":true,\"progressPreserved\":true}");
        }

        private static void CheckAcceptanceWaterBoundary()
        {
            GameObject boundary = GameObject.Find("AuthoredEnvironment/PlayBoundaries/DeepLake");
            if (boundary == null || !boundary.TryGetComponent(out MeshCollider collider)
                || !collider.enabled || collider.sharedMesh == null || collider.convex)
                throw new InvalidOperationException("섬 탐험 물 경계 누락");
            Vector3[] vertices = collider.sharedMesh.vertices;
            if (vertices.Length == 0 || vertices.Length % 4 != 0)
                throw new InvalidOperationException("물 경계의 사각 면 구조가 올바르지 않습니다.");
            int checkedSegments = 0, shortSegments = 0;
            float height = ReadLayout().lake.center.y + 1;
            for (int index = 0; index < vertices.Length; index += 4)
            {
                Vector3 first = boundary.transform.TransformPoint(vertices[index]);
                Vector3 second = boundary.transform.TransformPoint(vertices[index + 1]);
                Vector3 edge = Vector3.ProjectOnPlane(second - first, Vector3.up);
                // 부동소수점 교선이 만든 2cm 미만 면은 정밀도 오차를 피하려고 별도 집계한다.
                if (edge.magnitude < 0.02f) { shortSegments++; continue; }
                Vector3 normal = Vector3.Cross(Vector3.up, edge).normalized;
                Vector3 center = (first + second) * 0.5f;
                center.y = height;
                if (!collider.Raycast(new Ray(center - normal, normal), out _, 2)
                    || !collider.Raycast(new Ray(center + normal, -normal), out _, 2))
                    throw new InvalidOperationException("양면 물 경계 충돌 누락: " + index / 4);
                checkedSegments++;
            }
            File.WriteAllText(TraversalDirectory + "/WaterBoundary.json", "{\"checkedSegments\":"
                + checkedSegments + ",\"shortSegments\":" + shortSegments + ",\"errors\":0}");
        }
    }
}
