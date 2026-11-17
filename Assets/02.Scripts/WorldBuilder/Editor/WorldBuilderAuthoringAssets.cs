using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Data;

namespace UPlayGround.WorldBuilder.Editor
{
    /// <summary>P01 미리보기의 원본 GUID를 유지하면서 P02 저작 정의를 별도로 만든다.</summary>
    public static class WorldBuilderAuthoringAssets
    {
        /// <summary>디스크 폴더와 Unity 폴더 GUID를 동시에 생성한다.</summary>
        public static void EnsureFolder(string path)
        {
            string[] parts = path.Replace('\\', '/').Split('/');
            if (parts[0] != "Assets") throw new ArgumentException("Assets 내부 경로가 필요합니다.", nameof(path));
            string current = "Assets";
            for (int i = 1; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]) || parts[i] == "." || parts[i] == "..") throw new ArgumentException("정규 자산 경로가 필요합니다.", nameof(path));
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next) && string.IsNullOrEmpty(AssetDatabase.CreateFolder(current, parts[i]))) throw new IOException("저작 폴더를 만들지 못했습니다: " + next);
                current = next;
            }
        }

        /// <summary>선택한 신규 미리보기만 연결한다. 기존 게임 씬에서는 생성하지 않는다.</summary>
        public static WorldDefinition CreateForPreview(Scene scene)
        {
            string path = scene.path;
            const string prefix = "Assets/WorldBuilder/Worlds/";
            if (!scene.IsValid() || !scene.isLoaded || !path.StartsWith(prefix, StringComparison.Ordinal)) throw new InvalidOperationException("P01의 WorldBuilder 미리보기 씬을 먼저 여세요.");
            string id = path.Substring(prefix.Length).Split('/')[0];
            if (!Guid.TryParseExact(id, "N", out _)) throw new InvalidDataException("미리보기 월드 ID가 다릅니다.");
            string definitionPath = "Assets/10.Datas/WorldBuilder/" + id + "_Authoring.asset";
            string existingGuid = AssetDatabase.AssetPathToGUID(definitionPath);
            if (!string.IsNullOrEmpty(existingGuid))
            {
                var existing = AssetDatabase.LoadAssetAtPath<WorldDefinition>(AssetDatabase.GUIDToAssetPath(existingGuid));
                if (existing == null || existing.WorldGuid != id || AssetDatabase.GUIDToAssetPath(existing.PreviewSceneGuid) != path) throw new InvalidDataException("기존 저작 정의의 GUID 또는 타입이 다릅니다.");
                _ = existing.ReadSource();
                return existing;
            }
            string worldFolder = prefix + id;
            var metadata = JsonUtility.FromJson<SourceMetadata>(File.ReadAllText(worldFolder + "/Source/BaseHeight.json"));
            if (metadata == null || string.IsNullOrEmpty(metadata.profilePath)) throw new InvalidDataException("P01 원본 메타데이터가 없습니다.");
            var source = AssetDatabase.LoadAssetAtPath<TextAsset>(worldFolder + "/Source/BaseHeight.bytes");
            var profile = AssetDatabase.LoadAssetAtPath<WorldProfile>(metadata.profilePath);
            if (metadata.worldGuid != id || source == null || profile == null || WorldDefinition.HashBytes(source.bytes) != metadata.contentHash ||
                profile.SampleCount != metadata.width || metadata.width != metadata.height || profile.OriginY != metadata.originY || profile.VerticalRange != metadata.verticalRange || Math.Abs(profile.WorldSize - (metadata.width - 1) * metadata.sampleSpacing) > .0001)
                throw new InvalidDataException("P01 원본·프로필 참조·좌표 규격 또는 SHA가 다릅니다.");
            var definition = ScriptableObject.CreateInstance<WorldDefinition>();
            try
            {
                definition.Initialize(id, profile, source, AssetDatabase.AssetPathToGUID(path));
                AssetDatabase.CreateAsset(definition, definitionPath);
                using (var session = new WorldTerrainAuthoringSession(definition, FindTerrains(definition))) session.EnsureReadyForBake();
                return definition;
            }
            catch
            {
                if (AssetDatabase.Contains(definition)) AssetDatabase.DeleteAsset(definitionPath);
                else UnityEngine.Object.DestroyImmediate(definition);
                throw;
            }
        }

        /// <summary>저장된 씬 GUID와 좌표로 타일을 식별하며 이름으로 폴백하지 않는다.</summary>
        public static Terrain[,] FindTerrains(WorldDefinition definition)
        {
            string path = AssetDatabase.GUIDToAssetPath(definition.PreviewSceneGuid);
            var scene = SceneManager.GetSceneByPath(path);
            if (string.IsNullOrEmpty(path) || !scene.IsValid() || !scene.isLoaded) throw new InvalidOperationException("정의에 연결된 미리보기 씬을 여세요.");
            var profile = definition.Profile;
            var terrains = new Terrain[profile.TileCount, profile.TileCount];
            float tileSize = profile.WorldSize / profile.TileCount;
            foreach (var root in scene.GetRootGameObjects())
            foreach (var terrain in root.GetComponentsInChildren<Terrain>(true))
            {
                int x = Mathf.RoundToInt(terrain.transform.position.x / tileSize), z = Mathf.RoundToInt(terrain.transform.position.z / tileSize);
                if ((uint)x >= profile.TileCount || (uint)z >= profile.TileCount || terrains[z, x] != null) throw new InvalidDataException("지형 타일의 좌표가 겹치거나 범위를 벗어납니다.");
                terrains[z, x] = terrain;
            }
            return terrains;
        }

        [Serializable] private sealed class SourceMetadata { public string worldGuid, profilePath, contentHash; public int width, height; public double sampleSpacing; public float originY, verticalRange; }
    }
}
