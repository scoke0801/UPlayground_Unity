using System;
using System.IO;
using System.Security.Cryptography;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UPlayGround.WorldBuilder.Core;
using UPlayGround.WorldBuilder.Data;
using UPlayGround.EditorTools;

namespace UPlayGround.WorldBuilder.Editor
{
    /// <summary>고정 샘플을 검증하고 기존 월드와 분리된 Terrain 미리보기를 저장한다.</summary>
    public static class WorldBuilderSampleImporter
    {
        public const string HandoffRoot = "docs/전달용/OpenWorld_HeightMap_Design_Regional_v1_1_Handoff";
        public const string RegionalRaw = HandoffRoot + "/atlas/01_Regional_Heightmap_2049_R16LE_SouthFirst.raw";
        public const string TechnicalRaw = HandoffRoot + "/core/samples/openworld_2049_r16le.raw";
        public const string OrientationRaw = HandoffRoot + "/core/samples/orientation_test_33_r16le.raw";
        public const string RegionalSha256 = "59c7d2f4625cdaad5fae142c0133b54b4c4e4bb6e65264bd28bc647cacb85181";

        /// <summary>C 샘플은 지정된 SouthFirst RAW와 고정 checksum만 허용한다.</summary>
        public static HeightField LoadRegionalSource(string path)
        {
            if (!string.Equals(Path.GetFullPath(path), Path.GetFullPath(RegionalRaw), StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException("지역 높이 입력은 지정된 SouthFirst RAW만 허용합니다. 이미지는 참조용입니다.");
            var manifest = JsonUtility.FromJson<AtlasManifest>(File.ReadAllText(HandoffRoot + "/atlas/Regional_Atlas_Manifest.json"));
            if (manifest?.heightmap == null || manifest.heightmap.width != 2049 || manifest.heightmap.height != 2049 ||
                manifest.heightmap.encoding != "R16_UNORM" || manifest.heightmap.byteOrder != "little-endian" ||
                manifest.heightmap.canonicalImportRaw != Path.GetFileName(RegionalRaw) ||
                manifest.heightmap.canonicalRow0 != "south (+Z=0)" || manifest.heightmap.sha256?.canonicalSouthFirstRaw != RegionalSha256)
                throw new InvalidDataException("지역 RAW manifest 계약이 다릅니다.");
            return LoadRaw(path, 2049, RegionalSha256);
        }

        /// <summary>RAW 확장자·길이·선택 checksum을 검사한다. PNG 자동 변환은 하지 않는다.</summary>
        public static HeightField LoadRaw(string path, int sampleCount, string expectedSha256 = null)
        {
            if (!string.Equals(Path.GetExtension(path), ".raw", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("높이 입력은 R16 RAW여야 합니다.");
            if (string.Equals(Path.GetFileName(path), "01_World_Heightmap_2049_R16LE.raw", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("북쪽 첫 행의 지역 RAW는 참조용이며 Terrain 입력이 아닙니다.");
            byte[] bytes = File.ReadAllBytes(path);
            if (expectedSha256 != null && !string.Equals(ComputeSha256(bytes), expectedSha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("RAW SHA-256이 다릅니다.");
            return Raw16HeightmapCodec.Decode(bytes, sampleCount, sampleCount, HeightmapRowOrigin.South);
        }

        /// <summary>뒤집힘·전치 진단을 모든 샘플 임포트에 선행한다.</summary>
        public static void ValidateOrientationFixture()
        {
            var field = LoadRaw(OrientationRaw, 33);
            if (field[0, 0] != 6554 || field[0, 32] != 19661 || field[32, 0] != 39321 || field[32, 32] != 52428)
                throw new InvalidDataException("방향 진단 RAW의 남서/남동/북서/북동 값이 다릅니다.");
        }

        /// <summary>기술 fixture B를 독립 미리보기로 가져온다.</summary>
        [UPlaygroundTool("WorldBuilder.ImportTechnicalSample")]
        public static void ImportTechnicalSample() => ImportSample(false);

        /// <summary>지역 C 원본을 700m 프로필의 독립 미리보기로 가져온다.</summary>
        [UPlaygroundTool("WorldBuilder.ImportRegionalSample")]
        public static void ImportRegionalSample() => ImportSample(true);

        private static void ImportSample(bool regional)
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit Mode에서 가져오세요.");
            ValidateOrientationFixture();
            var field = regional ? LoadRegionalSource(RegionalRaw) : LoadRaw(TechnicalRaw, 2049);
            var profile = ScriptableObject.CreateInstance<WorldProfile>();
            if (!regional) profile.Configure(2048, 2049, -80, 480, 512);
            string worldId = Guid.NewGuid().ToString("N");
            string folder = "Assets/WorldBuilder/Worlds/" + worldId;
            string profilePath = "Assets/10.Datas/WorldBuilder/" + worldId + ".asset";
            var previousScene = SceneManager.GetActiveScene();
            Scene preview = default;
            Undo.IncrementCurrentGroup();
            int undoGroup = Undo.GetCurrentGroup();
            Undo.SetCurrentGroupName("WorldBuilder 샘플 미리보기");
            try
            {
                Directory.CreateDirectory(folder + "/Source");
                Directory.CreateDirectory(folder + "/Preview/Terrain");
                Directory.CreateDirectory("Assets/10.Datas/WorldBuilder");
                AssetDatabase.Refresh();
                AssetDatabase.CreateAsset(profile, profilePath);
                Undo.RegisterCreatedObjectUndo(profile, "월드 프로필");
                byte[] canonical = Raw16HeightmapCodec.Encode(field, HeightmapRowOrigin.South);
                File.WriteAllBytes(folder + "/Source/BaseHeight.bytes", canonical);
                var metadata = new SourceMetadata
                {
                    worldGuid = worldId, sourcePath = regional ? RegionalRaw : TechnicalRaw,
                    width = field.Width, height = field.Height, sampleSpacing = profile.CreateGrid().SampleSpacing,
                    originY = profile.OriginY, verticalRange = profile.VerticalRange,
                    originalVerticalRange = regional ? 2800 : 480, contentHash = ComputeSha256(canonical), profilePath = profilePath
                };
                File.WriteAllText(folder + "/Source/BaseHeight.json", JsonUtility.ToJson(metadata, true));
                preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(preview);
                var terrains = CreateTerrains(field, profile, preview, folder + "/Preview/Terrain");
                ApplyPreviewSurface(terrains, folder + "/Preview/Terrain");
                var light = new GameObject("미리보기 햇빛").AddComponent<Light>();
                Undo.RegisterCreatedObjectUndo(light.gameObject, "미리보기 조명");
                light.type = LightType.Directional;
                light.transform.rotation = Quaternion.Euler(45, -30, 0);
                CreateCornerLabels(field, profile);
                AssetDatabase.SaveAssetIfDirty(profile);
                foreach (var terrain in terrains) AssetDatabase.SaveAssetIfDirty(terrain.terrainData);
                AssetDatabase.Refresh();
                string scenePath = folder + "/Preview/WorldPreview.unity";
                if (!EditorSceneManager.SaveScene(preview, scenePath)) throw new IOException("미리보기 씬 저장에 실패했습니다.");
                Undo.CollapseUndoOperations(undoGroup);
                Debug.Log($"WorldBuilder {(regional ? "C" : "B")} 미리보기 저장: {scenePath}, Terrain {terrains.Length}개");
            }
            catch
            {
                Undo.RevertAllDownToGroup(undoGroup);
                if (preview.IsValid() && preview.isLoaded) EditorSceneManager.CloseScene(preview, true);
                // 이번 호출의 새 GUID 경로만 지운다. 기존 원본과 Published에는 접근하지 않는다.
                if (AssetDatabase.LoadMainAssetAtPath(profilePath) != null) AssetDatabase.DeleteAsset(profilePath);
                if (Directory.Exists(folder)) AssetDatabase.DeleteAsset(folder);
                throw;
            }
            finally
            {
                if (profile != null && !EditorUtility.IsPersistent(profile)) UnityEngine.Object.DestroyImmediate(profile);
                if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene);
            }
        }

        /// <summary>숫자 원본을 정사각 Terrain 타일과 같은 방향의 이웃 관계로 적용한다.</summary>
        public static Terrain[,] CreateTerrains(HeightField field, WorldProfile profile, Scene scene, string assetFolder = null)
        {
            if (!scene.IsValid() || !scene.isLoaded) throw new ArgumentException("열린 대상 씬이 필요합니다.", nameof(scene));
            var previousScene = SceneManager.GetActiveScene();
            if (!EditorSceneManager.IsPreviewScene(scene)) SceneManager.SetActiveScene(scene);
            try { return CreateTerrainsInActiveScene(field, profile, scene, assetFolder); }
            finally { if (previousScene.IsValid() && previousScene.isLoaded) SceneManager.SetActiveScene(previousScene); }
        }

        private static Terrain[,] CreateTerrainsInActiveScene(HeightField field, WorldProfile profile, Scene scene, string assetFolder)
        {
            if (field == null || profile == null) throw new ArgumentNullException(nameof(field));
            var grid = profile.CreateGrid();
            if (field.Width != profile.SampleCount || field.Height != profile.SampleCount) throw new InvalidDataException("프로필과 높이 규격이 다릅니다.");
            if (!scene.IsValid() || !scene.isLoaded) throw new ArgumentException("열린 대상 씬이 필요합니다.", nameof(scene));
            if (assetFolder != null && !AssetDatabase.IsValidFolder(assetFolder)) throw new ArgumentException("저장 폴더가 없습니다.", nameof(assetFolder));
            int count = profile.TileCount;
            string profileGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(profile));
            int groupingId = string.IsNullOrEmpty(profileGuid) ? scene.handle : Convert.ToInt32(profileGuid.Substring(0, 7), 16);
            // 저장 충돌은 오브젝트를 만들기 전에 검사한다.
            if (assetFolder != null)
                for (int z = 0; z < count; z++)
                for (int x = 0; x < count; x++)
                    if (File.Exists($"{assetFolder}/Terrain_{x}_{z}.asset")) throw new IOException("기존 TerrainData를 덮어쓸 수 없습니다.");
            var terrains = new Terrain[count, count];
            for (int z = 0; z < count; z++)
            for (int x = 0; x < count; x++)
            {
                var data = new TerrainData { name = $"Terrain_{x}_{z}", heightmapResolution = profile.TileCells + 1 };
                data.size = new Vector3(profile.WorldSize / count, profile.VerticalRange, profile.WorldSize / count);
                data.SetHeights(0, 0, ToNormalized(field.ExtractTile(x, z, profile.TileCells)));
                if (assetFolder != null) AssetDatabase.CreateAsset(data, $"{assetFolder}/{data.name}.asset");
                // 미리보기 씬은 활성화할 수 없으므로 저장 제외 객체로 만든 뒤 대상 씬으로 옮긴다.
                var gameObject = EditorUtility.CreateGameObjectWithHideFlags(data.name, HideFlags.HideAndDontSave);
                SceneManager.MoveGameObjectToScene(gameObject, scene);
                if (!EditorSceneManager.IsPreviewScene(scene)) gameObject.hideFlags = HideFlags.None;
                var terrain = gameObject.AddComponent<Terrain>();
                terrain.terrainData = data;
                gameObject.AddComponent<TerrainCollider>().terrainData = data;
                if (!EditorSceneManager.IsPreviewScene(scene)) Undo.RegisterCreatedObjectUndo(gameObject, "월드 지형 타일");
                gameObject.transform.position = new Vector3((float)grid.SampleToWorld(x * profile.TileCells), profile.OriginY, (float)grid.SampleToWorld(z * profile.TileCells));
                terrains[z, x] = terrain;
                // SetNeighbors는 직렬화되지 않으므로 재로드 때 같은 월드 그룹으로 다시 연결한다.
                terrains[z, x].groupingID = groupingId;
                terrains[z, x].allowAutoConnect = true;
            }
            for (int z = 0; z < count; z++)
            for (int x = 0; x < count; x++)
                terrains[z, x].SetNeighbors(x > 0 ? terrains[z, x - 1] : null, z + 1 < count ? terrains[z + 1, x] : null,
                    x + 1 < count ? terrains[z, x + 1] : null, z > 0 ? terrains[z - 1, x] : null);
            return terrains;
        }

        /// <summary>Unity heights[z,x]에 맞춰서만 정규화한다.</summary>
        public static float[,] ToNormalized(HeightField field)
        {
            var heights = new float[field.Height, field.Width];
            for (int z = 0; z < field.Height; z++)
            for (int x = 0; x < field.Width; x++) heights[z, x] = field[z, x] / 65535f;
            return heights;
        }

        /// <summary>URP에서 기본 지형이 오류 셰이더로 보이지 않도록 중립 미리보기 표면을 저장한다.</summary>
        public static void ApplyPreviewSurface(Terrain[,] terrains, string assetFolder)
        {
            var shader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
            if (shader == null || !shader.isSupported) throw new InvalidOperationException("URP Terrain Lit 셰이더를 사용할 수 없습니다.");
            string materialPath = assetFolder + "/PreviewSurface.mat", layerPath = assetFolder + "/PreviewSurface.terrainlayer";
            string texturePath = assetFolder + "/PreviewAlbedo.asset";
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture == null)
            {
                // whiteTexture는 씬 재로드 때 참조가 유지되지 않으므로 실제 자산으로 보관한다.
                texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true) { name = "PreviewAlbedo" };
                var color = new Color(.35f, .35f, .35f, 1);
                texture.SetPixels(new[] { color, color, color, color });
                texture.Apply();
                AssetDatabase.CreateAsset(texture, texturePath);
            }
            var material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null) { material = new Material(shader); AssetDatabase.CreateAsset(material, materialPath); }
            var layer = AssetDatabase.LoadAssetAtPath<TerrainLayer>(layerPath);
            if (layer == null)
            {
                layer = new TerrainLayer();
                AssetDatabase.CreateAsset(layer, layerPath);
            }
            layer.diffuseTexture = texture;
            // RGBA의 알파 1이 최대 광택으로 해석되지 않도록 무광 상수를 사용한다.
            layer.smoothnessSource = TerrainLayerSmoothnessSource.Constant;
            layer.smoothness = 0;
            EditorUtility.SetDirty(layer);
            var weights = new float[16, 16, 1];
            for (int z = 0; z < 16; z++)
            for (int x = 0; x < 16; x++) weights[z, x, 0] = 1;
            foreach (var terrain in terrains)
            {
                terrain.materialTemplate = material;
                terrain.terrainData.alphamapResolution = 16;
                terrain.terrainData.terrainLayers = new[] { layer };
                terrain.terrainData.SetAlphamaps(0, 0, weights);
                EditorUtility.SetDirty(terrain);
                AssetDatabase.SaveAssetIfDirty(terrain.terrainData);
            }
            AssetDatabase.SaveAssetIfDirty(material);
            AssetDatabase.SaveAssetIfDirty(texture);
            AssetDatabase.SaveAssetIfDirty(layer);
        }

        /// <summary>원본 무결성 식별자를 소문자 SHA-256으로 계산한다.</summary>
        public static string ComputeSha256(byte[] bytes)
        {
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        private static void CreateCornerLabels(HeightField field, WorldProfile profile)
        {
            string[] names = { "남서 SW", "남동 SE", "북서 NW", "북동 NE" };
            for (int index = 0; index < 4; index++)
            {
                int x = index % 2 * (field.Width - 1), z = index / 2 * (field.Height - 1);
                var grid = profile.CreateGrid();
                var marker = new GameObject(names[index]);
                Undo.RegisterCreatedObjectUndo(marker, "방향 표식");
                marker.transform.position = new Vector3((float)grid.SampleToWorld(x), (float)grid.SampleToHeight(field[z, x]), (float)grid.SampleToWorld(z));
            }
        }

        [Serializable] private sealed class AtlasManifest { public AtlasHeightmap heightmap; }
        [Serializable] private sealed class AtlasHeightmap
        {
            public int width, height;
            public string encoding, byteOrder, canonicalImportRaw, canonicalRow0;
            public AtlasHashes sha256;
        }
        [Serializable] private sealed class AtlasHashes { public string canonicalSouthFirstRaw; }
        [Serializable] private sealed class SourceMetadata
        {
            public int schemaVersion = 1;
            public string worldGuid, sourcePath, profilePath, contentHash;
            public string encoding = "R16_UNORM", byteOrder = "little-endian", rowOrigin = "south";
            public int width, height;
            public double sampleSpacing, originY, verticalRange, originalVerticalRange;
        }
    }
}
