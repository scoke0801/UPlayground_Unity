using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Codex;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Party;
using IOPath = System.IO.Path;

namespace UPlayGround.Data.Editor.Codex
{
    /// <summary>몬스터 Actor 프리팹을 도감용 Sprite로 촬영하고 도감 항목에 자동 연결한다.</summary>
    public static class MonsterCodexThumbnailGenerator
    {
        private const string OutputRoot =
            "Assets/04.Images/UI/Portrait/MonsterCodex";
        private const string StagingFolderName = "MonsterCodexThumbnailStaging";
        private const int TextureSize = 512;

        [UPlayGround.EditorTools.UPlaygroundTool(
            "UPlayGround/도감/몬스터 도감 썸네일 생성 및 연결")]
        public static void GenerateAndConnect() =>
            GenerateAndConnect(showDialog: !Application.isBatchMode);

        private static void GenerateAndConnect(bool showDialog)
        {
            string stagingPath = GetStagingPath();
            try
            {
                ActorDatabase actorDatabase = FindFirst<ActorDatabase>();
                if (actorDatabase == null)
                    throw new InvalidOperationException(
                        "ActorDatabase를 찾지 못했습니다.");

                MonsterCodexDatabaseBuilder.Build();
                MonsterCodexDatabaseSO codexDatabase =
                    FindFirst<MonsterCodexDatabaseSO>();
                if (codexDatabase == null)
                    throw new InvalidOperationException(
                        "MonsterCodexDatabase를 생성하거나 찾지 못했습니다.");

                PartyMemberDataSO partyMemberData = FindFirst<PartyMemberDataSO>();
                List<ThumbnailTarget> targets = CollectTargets(
                    actorDatabase,
                    codexDatabase,
                    partyMemberData);
                if (targets.Count == 0)
                    throw new InvalidOperationException(
                        "촬영할 비플레이어 Monster 프리팹이 없습니다.");

                PrepareStagingDirectory(stagingPath);
                CaptureToStaging(targets, stagingPath);
                EnsureAssetFolder(OutputRoot);
                Dictionary<string, Sprite> sprites = ImportThumbnails(
                    targets,
                    stagingPath);

                ConnectionResult result = ConnectPortraits(
                    actorDatabase,
                    codexDatabase,
                    partyMemberData,
                    sprites);
                AssetDatabase.SaveAssets();
                ValidateConnections(actorDatabase, codexDatabase);

                string summary =
                    $"고유 프리팹 {targets.Count}개 촬영, " +
                    $"생성 썸네일 {result.GeneratedCount}개 연결, " +
                    $"기존 Player Portrait {result.ReusedCount}개 연결, " +
                    $"수동 초상화 {result.PreservedCount}개 보존";
                Debug.Log($"[MonsterCodexThumbnail] 완료: {summary}");
                if (showDialog)
                {
                    EditorUtility.DisplayDialog(
                        "몬스터 도감 썸네일",
                        summary,
                        "확인");
                }
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[MonsterCodexThumbnail] 사용자가 생성을 취소했습니다.");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                if (Application.isBatchMode || !showDialog)
                    throw;

                EditorUtility.DisplayDialog(
                    "몬스터 도감 썸네일 생성 실패",
                    exception.Message,
                    "확인");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                DeleteStagingDirectory(stagingPath);
            }
        }

        private static List<ThumbnailTarget> CollectTargets(
            ActorDatabase actorDatabase,
            MonsterCodexDatabaseSO codexDatabase,
            PartyMemberDataSO partyMemberData)
        {
            var targetsByGuid = new Dictionary<string, ThumbnailTarget>();
            foreach (ActorDefinitionSO definition in actorDatabase.All)
            {
                if (!IsCodexMonster(definition))
                    continue;

                MonsterCodexEntrySO entry = codexDatabase.GetEntry(
                    definition.actorId);
                if (entry == null)
                {
                    throw new InvalidOperationException(
                        $"Actor '{definition.actorId}'의 도감 항목이 없습니다.");
                }

                Sprite playerPortrait = ResolvePlayerPortrait(
                    definition,
                    partyMemberData);
                if (!entry.includeInCodex ||
                    playerPortrait != null ||
                    ShouldPreservePortrait(entry.portrait))
                {
                    continue;
                }

                if (definition.prefab == null)
                {
                    throw new InvalidOperationException(
                        $"Actor '{definition.actorId}'의 프리팹이 비어 있습니다.");
                }

                string prefabPath = AssetDatabase.GetAssetPath(definition.prefab);
                string prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
                if (string.IsNullOrWhiteSpace(prefabGuid))
                {
                    throw new InvalidOperationException(
                        $"Actor '{definition.actorId}' 프리팹의 GUID를 찾지 못했습니다.");
                }

                if (!targetsByGuid.TryGetValue(prefabGuid, out ThumbnailTarget target))
                {
                    target = new ThumbnailTarget(
                        prefabGuid,
                        prefabPath,
                        definition.prefab,
                        BuildOutputPath(definition.prefab, prefabGuid));
                    targetsByGuid.Add(prefabGuid, target);
                }

            }

            var targets = new List<ThumbnailTarget>(targetsByGuid.Values);
            targets.Sort((left, right) =>
                string.CompareOrdinal(left.PrefabPath, right.PrefabPath));
            return targets;
        }

        private static bool IsCodexMonster(ActorDefinitionSO definition)
        {
            if (definition == null)
                return false;

            bool isMonster = (definition.actorType & ActorType.Monster) != 0;
            bool isPlayer = (definition.actorType & ActorType.Player) != 0;
            return isMonster && !isPlayer;
        }

        private static void CaptureToStaging(
            IReadOnlyList<ThumbnailTarget> targets,
            string stagingPath)
        {
            using var renderer = new ActorThumbnailRenderer();
            for (int i = 0; i < targets.Count; i++)
            {
                ThumbnailTarget target = targets[i];
                ThrowIfCanceled(
                    "몬스터 도감 썸네일 촬영",
                    target.Prefab.name,
                    i,
                    targets.Count);

                Texture2D captured = null;
                try
                {
                    captured = renderer.Render(target.Prefab, TextureSize);
                    byte[] png = captured.EncodeToPNG();
                    if (png == null || png.Length == 0)
                    {
                        throw new InvalidOperationException(
                            $"'{target.PrefabPath}'의 PNG 인코딩에 실패했습니다.");
                    }

                    File.WriteAllBytes(
                        IOPath.Combine(stagingPath, IOPath.GetFileName(target.OutputPath)),
                        png);
                }
                finally
                {
                    if (captured != null)
                        UnityEngine.Object.DestroyImmediate(captured);
                }
            }
        }

        private static Dictionary<string, Sprite> ImportThumbnails(
            IReadOnlyList<ThumbnailTarget> targets,
            string stagingPath)
        {
            var sprites = new Dictionary<string, Sprite>();
            for (int i = 0; i < targets.Count; i++)
            {
                ThumbnailTarget target = targets[i];
                ThrowIfCanceled(
                    "몬스터 도감 썸네일 임포트",
                    target.Prefab.name,
                    i,
                    targets.Count);

                string stagedFile = IOPath.Combine(
                    stagingPath,
                    IOPath.GetFileName(target.OutputPath));
                string outputFile = ToAbsolutePath(target.OutputPath);
                File.Copy(stagedFile, outputFile, true);

                AssetDatabase.ImportAsset(
                    target.OutputPath,
                    ImportAssetOptions.ForceSynchronousImport |
                    ImportAssetOptions.ForceUpdate);
                ConfigureImporter(target.OutputPath);

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                    target.OutputPath);
                if (sprite == null)
                {
                    throw new InvalidOperationException(
                        $"Sprite 임포트에 실패했습니다: {target.OutputPath}");
                }

                sprites.Add(target.PrefabGuid, sprite);
            }

            return sprites;
        }

        private static void ConfigureImporter(string assetPath)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                throw new InvalidOperationException(
                    $"TextureImporter를 찾지 못했습니다: {assetPath}");

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.maxTextureSize = TextureSize;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.sRGBTexture = true;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteAlignment = (int)SpriteAlignment.Center;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        private static ConnectionResult ConnectPortraits(
            ActorDatabase actorDatabase,
            MonsterCodexDatabaseSO codexDatabase,
            PartyMemberDataSO partyMemberData,
            IReadOnlyDictionary<string, Sprite> sprites)
        {
            int generatedCount = 0;
            int reusedCount = 0;
            int preservedCount = 0;
            foreach (ActorDefinitionSO definition in actorDatabase.All)
            {
                if (!IsCodexMonster(definition))
                    continue;

                MonsterCodexEntrySO entry = codexDatabase.GetEntry(
                    definition.actorId);
                if (entry == null)
                {
                    throw new InvalidOperationException(
                        $"Actor '{definition.actorId}'의 도감 항목이 없습니다.");
                }

                if (!entry.includeInCodex)
                    continue;

                Sprite playerPortrait = ResolvePlayerPortrait(
                    definition,
                    partyMemberData);
                if (playerPortrait != null)
                {
                    if (ShouldPreservePortrait(entry.portrait) &&
                        entry.portrait != playerPortrait)
                    {
                        preservedCount++;
                        continue;
                    }

                    SetPortrait(entry, playerPortrait);
                    reusedCount++;
                    continue;
                }

                if (ShouldPreservePortrait(entry.portrait))
                {
                    preservedCount++;
                    continue;
                }

                string prefabPath = AssetDatabase.GetAssetPath(definition.prefab);
                string prefabGuid = AssetDatabase.AssetPathToGUID(prefabPath);
                if (!sprites.TryGetValue(prefabGuid, out Sprite sprite))
                {
                    throw new InvalidOperationException(
                        $"Actor '{definition.actorId}'의 촬영 Sprite를 찾지 못했습니다.");
                }

                SetPortrait(entry, sprite);
                generatedCount++;
            }

            return new ConnectionResult(
                generatedCount,
                reusedCount,
                preservedCount);
        }

        private static Sprite ResolvePlayerPortrait(
            ActorDefinitionSO definition,
            PartyMemberDataSO partyMemberData)
        {
            if (definition.characterType == CharacterActorType.None)
                return null;

            if (partyMemberData == null)
            {
                throw new InvalidOperationException(
                    "Player 계열 Actor를 제외하는 데 필요한 PartyMemberDataSO를 " +
                    "찾지 못했습니다.");
            }

            Sprite portrait = partyMemberData.GetFullBodySprite(
                definition.characterType);
            if (portrait == null)
            {
                throw new InvalidOperationException(
                    $"Player 계열 Actor '{definition.actorId}'의 기존 Full Portrait가 " +
                    $"없습니다: {definition.characterType}");
            }

            return portrait;
        }

        private static void SetPortrait(
            MonsterCodexEntrySO entry,
            Sprite portrait)
        {
            if (entry.portrait == portrait)
                return;

            Undo.RecordObject(entry, "몬스터 도감 썸네일 연결");
            entry.portrait = portrait;
            EditorUtility.SetDirty(entry);
        }

        private static bool ShouldPreservePortrait(Sprite portrait)
        {
            if (portrait == null)
                return false;

            string currentPath = AssetDatabase.GetAssetPath(portrait);
            return !currentPath.StartsWith(
                OutputRoot + "/",
                StringComparison.OrdinalIgnoreCase);
        }

        private static void ValidateConnections(
            ActorDatabase actorDatabase,
            MonsterCodexDatabaseSO codexDatabase)
        {
            int errorCount = 0;
            foreach (ActorDefinitionSO definition in actorDatabase.All)
            {
                if (!IsCodexMonster(definition))
                    continue;

                MonsterCodexEntrySO entry = codexDatabase.GetEntry(
                    definition.actorId);
                if (entry == null)
                {
                    Debug.LogError(
                        $"[MonsterCodexThumbnail] 도감 항목 누락: {definition.actorId}",
                        definition);
                    errorCount++;
                    continue;
                }

                if (entry.includeInCodex && entry.portrait == null)
                {
                    Debug.LogError(
                        $"[MonsterCodexThumbnail] 초상화 연결 누락: {entry.actorId}",
                        entry);
                    errorCount++;
                }
            }

            if (errorCount > 0)
            {
                throw new InvalidOperationException(
                    $"도감 썸네일 연결 검증에서 {errorCount}개 오류가 발견되었습니다.");
            }
        }

        private static string BuildOutputPath(GameObject prefab, string prefabGuid)
        {
            string safeName = MakeSafeFileName(prefab.name);
            string shortGuid = prefabGuid.Substring(0, Mathf.Min(8, prefabGuid.Length));
            return $"{OutputRoot}/MonsterCodex_{safeName}_{shortGuid}.png";
        }

        private static string MakeSafeFileName(string value)
        {
            foreach (char invalid in IOPath.GetInvalidFileNameChars())
                value = value.Replace(invalid, '_');
            return value;
        }

        private static void ThrowIfCanceled(
            string title,
            string message,
            int index,
            int count)
        {
            if (Application.isBatchMode)
                return;

            float progress = count > 0 ? (float)index / count : 0f;
            if (EditorUtility.DisplayCancelableProgressBar(
                    title,
                    message,
                    progress))
            {
                throw new OperationCanceledException();
            }
        }

        private static T FindFirst<T>() where T : UnityEngine.Object
        {
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}");
            return guids.Length == 0
                ? null
                : AssetDatabase.LoadAssetAtPath<T>(
                    AssetDatabase.GUIDToAssetPath(guids[0]));
        }

        private static void EnsureAssetFolder(string path)
        {
            string current = "Assets";
            string[] parts = path.Split('/');
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }

        private static string GetStagingPath()
        {
            string projectRoot = IOPath.GetDirectoryName(Application.dataPath);
            return IOPath.Combine(projectRoot, "Temp", StagingFolderName);
        }

        private static void PrepareStagingDirectory(string stagingPath)
        {
            DeleteStagingDirectory(stagingPath);
            Directory.CreateDirectory(stagingPath);
        }

        private static void DeleteStagingDirectory(string stagingPath)
        {
            string projectRoot = IOPath.GetDirectoryName(Application.dataPath);
            string tempRoot = IOPath.GetFullPath(IOPath.Combine(projectRoot, "Temp"));
            string resolvedPath = IOPath.GetFullPath(stagingPath);
            string requiredPrefix = tempRoot.TrimEnd(
                IOPath.DirectorySeparatorChar,
                IOPath.AltDirectorySeparatorChar) + IOPath.DirectorySeparatorChar;
            if (!resolvedPath.StartsWith(
                    requiredPrefix,
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    $"스테이징 삭제 경로가 프로젝트 Temp 밖입니다: {resolvedPath}");
            }

            if (Directory.Exists(stagingPath))
                Directory.Delete(stagingPath, true);
        }

        private static string ToAbsolutePath(string assetPath) => IOPath.Combine(
            Application.dataPath,
            assetPath.Substring("Assets".Length).TrimStart('/', '\\'));

        private sealed class ThumbnailTarget
        {
            public readonly string PrefabGuid;
            public readonly string PrefabPath;
            public readonly GameObject Prefab;
            public readonly string OutputPath;
            public ThumbnailTarget(
                string prefabGuid,
                string prefabPath,
                GameObject prefab,
                string outputPath)
            {
                PrefabGuid = prefabGuid;
                PrefabPath = prefabPath;
                Prefab = prefab;
                OutputPath = outputPath;
            }
        }

        private readonly struct ConnectionResult
        {
            public readonly int GeneratedCount;
            public readonly int ReusedCount;
            public readonly int PreservedCount;

            public ConnectionResult(
                int generatedCount,
                int reusedCount,
                int preservedCount)
            {
                GeneratedCount = generatedCount;
                ReusedCount = reusedCount;
                PreservedCount = preservedCount;
            }
        }

        private sealed class ActorThumbnailRenderer : IDisposable
        {
            private const float CameraFieldOfView = 30f;
            private const float CameraYawFromActorFront = 35f;
            private const float CameraPitch = 15f;
            private const float FillRatio = 0.84f;
            private const int Supersampling = 2;
            private const byte VisibleAlphaThreshold = 8;

            private readonly PreviewRenderUtility _preview;
            private GameObject _instance;

            public ActorThumbnailRenderer()
            {
                _preview = new PreviewRenderUtility();
                _preview.cameraFieldOfView = CameraFieldOfView;
                _preview.camera.clearFlags = CameraClearFlags.SolidColor;
                _preview.camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
                _preview.camera.allowHDR = false;
                _preview.camera.allowMSAA = true;
                _preview.lights[0].intensity = 1.25f;
                _preview.lights[0].transform.rotation =
                    Quaternion.Euler(45f, -35f, 0f);
                _preview.lights[0].shadows = LightShadows.Soft;
                _preview.lights[1].intensity = 0.65f;
                _preview.lights[1].transform.rotation =
                    Quaternion.Euler(25f, 145f, 0f);
                _preview.lights[1].shadows = LightShadows.None;
            }

            public Texture2D Render(GameObject prefab, int textureSize)
            {
                ClearInstance();
                _instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                if (_instance == null)
                    _instance = UnityEngine.Object.Instantiate(prefab);
                if (_instance == null)
                    throw new InvalidOperationException(
                        $"프리팹 인스턴스화에 실패했습니다: {prefab.name}");

                if (PrefabUtility.IsPartOfPrefabInstance(_instance))
                {
                    PrefabUtility.UnpackPrefabInstance(
                        _instance,
                        PrefabUnpackMode.Completely,
                        InteractionMode.AutomatedAction);
                }

                _instance.name = prefab.name + "_CodexThumbnailPreview";
                _instance.hideFlags = HideFlags.HideAndDontSave;
                _instance.transform.SetPositionAndRotation(
                    Vector3.zero,
                    Quaternion.identity);
                DisableRuntimeComponents(_instance);
                _preview.AddSingleGO(_instance);

                Bounds bounds = CalculateVisualBounds(_instance);
                PositionCamera(bounds);

                int renderSize = textureSize * Supersampling;
                Texture2D blackPass = null;
                Texture2D whitePass = null;
                Texture2D transparent = null;
                try
                {
                    blackPass = RenderPass(renderSize, Color.black);
                    whitePass = RenderPass(renderSize, Color.white);
                    transparent = ReconstructTransparency(blackPass, whitePass);
                    return CropToVisibleContent(transparent, textureSize, prefab.name);
                }
                finally
                {
                    if (blackPass != null)
                        UnityEngine.Object.DestroyImmediate(blackPass);
                    if (whitePass != null)
                        UnityEngine.Object.DestroyImmediate(whitePass);
                    if (transparent != null)
                        UnityEngine.Object.DestroyImmediate(transparent);
                }
            }

            public void Dispose()
            {
                ClearInstance();
                _preview.Cleanup();
            }

            private void PositionCamera(Bounds bounds)
            {
                Quaternion rotation = Quaternion.Euler(
                    CameraPitch,
                    180f + CameraYawFromActorFront,
                    0f);
                Vector3 right = rotation * Vector3.right;
                Vector3 up = rotation * Vector3.up;
                Vector3 forward = rotation * Vector3.forward;
                Vector3 extents = bounds.extents;

                float horizontalExtent = ProjectExtent(extents, right);
                float verticalExtent = ProjectExtent(extents, up);
                float depthExtent = ProjectExtent(extents, forward);
                float verticalFov = CameraFieldOfView * Mathf.Deg2Rad;
                float distanceByHeight = verticalExtent /
                    Mathf.Tan(verticalFov * 0.5f) / FillRatio;
                float distanceByWidth = horizontalExtent /
                    Mathf.Tan(verticalFov * 0.5f) / FillRatio;
                float distance = Mathf.Max(distanceByHeight, distanceByWidth) +
                                 depthExtent;
                distance = Mathf.Max(distance, 0.5f);

                Camera camera = _preview.camera;
                camera.orthographic = false;
                camera.fieldOfView = CameraFieldOfView;
                camera.aspect = 1f;
                camera.transform.position = bounds.center - forward * distance;
                camera.transform.rotation = rotation;
                camera.nearClipPlane = 0.01f;
                camera.farClipPlane = Mathf.Max(
                    100f,
                    distance + bounds.extents.magnitude * 4f);
            }

            private static float ProjectExtent(Vector3 extents, Vector3 axis) =>
                Mathf.Abs(axis.x) * extents.x +
                Mathf.Abs(axis.y) * extents.y +
                Mathf.Abs(axis.z) * extents.z;

            private Texture2D RenderPass(int textureSize, Color background)
            {
                _preview.camera.backgroundColor = background;
                var previewRect = new Rect(0f, 0f, textureSize, textureSize);
                _preview.BeginStaticPreview(previewRect);
                GL.Clear(true, true, background);
                _preview.camera.Render();
                Texture2D texture = _preview.EndStaticPreview();
                if (texture == null)
                    throw new InvalidOperationException("프리팹 렌더 패스에 실패했습니다.");
                return texture;
            }

            private static Texture2D ReconstructTransparency(
                Texture2D blackPass,
                Texture2D whitePass)
            {
                Color32[] black = blackPass.GetPixels32();
                Color32[] white = whitePass.GetPixels32();
                var output = new Color32[black.Length];
                for (int i = 0; i < black.Length; i++)
                {
                    float transparency = (
                        Mathf.Clamp01((white[i].r - black[i].r) / 255f) +
                        Mathf.Clamp01((white[i].g - black[i].g) / 255f) +
                        Mathf.Clamp01((white[i].b - black[i].b) / 255f)) / 3f;
                    float alpha = 1f - transparency;
                    if (alpha <= 1f / 255f)
                    {
                        output[i] = new Color32(0, 0, 0, 0);
                        continue;
                    }

                    output[i] = new Color32(
                        ToByte(black[i].r / alpha),
                        ToByte(black[i].g / alpha),
                        ToByte(black[i].b / alpha),
                        ToByte(alpha * 255f));
                }

                var texture = new Texture2D(
                    blackPass.width,
                    blackPass.height,
                    TextureFormat.RGBA32,
                    false,
                    false);
                texture.SetPixels32(output);
                texture.Apply(false, false);
                return texture;
            }

            private static Texture2D CropToVisibleContent(
                Texture2D source,
                int outputSize,
                string prefabName)
            {
                Color32[] sourcePixels = source.GetPixels32();
                if (!TryFindVisibleBounds(
                        sourcePixels,
                        source.width,
                        source.height,
                        out RectInt visibleBounds))
                {
                    throw new InvalidOperationException(
                        $"렌더 결과에 보이는 픽셀이 없습니다: {prefabName}");
                }

                float cropSize = Mathf.Max(
                    visibleBounds.width,
                    visibleBounds.height) / FillRatio;
                cropSize = Mathf.Min(cropSize, source.width);
                float centerX = visibleBounds.xMin + visibleBounds.width * 0.5f;
                float centerY = visibleBounds.yMin + visibleBounds.height * 0.5f;
                float cropMinX = Mathf.Clamp(
                    centerX - cropSize * 0.5f,
                    0f,
                    source.width - cropSize);
                float cropMinY = Mathf.Clamp(
                    centerY - cropSize * 0.5f,
                    0f,
                    source.height - cropSize);

                var outputPixels = new Color32[outputSize * outputSize];
                float sourceStep = cropSize / outputSize;
                for (int y = 0; y < outputSize; y++)
                {
                    float sourceY = cropMinY + (y + 0.5f) * sourceStep - 0.5f;
                    for (int x = 0; x < outputSize; x++)
                    {
                        float sourceX = cropMinX + (x + 0.5f) * sourceStep - 0.5f;
                        outputPixels[y * outputSize + x] = SampleBilinear(
                            sourcePixels,
                            source.width,
                            source.height,
                            sourceX,
                            sourceY);
                    }
                }

                var output = new Texture2D(
                    outputSize,
                    outputSize,
                    TextureFormat.RGBA32,
                    false,
                    false);
                output.SetPixels32(outputPixels);
                output.Apply(false, false);
                return output;
            }

            private static bool TryFindVisibleBounds(
                IReadOnlyList<Color32> pixels,
                int width,
                int height,
                out RectInt bounds)
            {
                int minX = width;
                int minY = height;
                int maxX = -1;
                int maxY = -1;
                for (int y = 0; y < height; y++)
                {
                    int row = y * width;
                    for (int x = 0; x < width; x++)
                    {
                        if (pixels[row + x].a <= VisibleAlphaThreshold)
                            continue;
                        minX = Mathf.Min(minX, x);
                        minY = Mathf.Min(minY, y);
                        maxX = Mathf.Max(maxX, x);
                        maxY = Mathf.Max(maxY, y);
                    }
                }

                if (maxX < minX || maxY < minY)
                {
                    bounds = default;
                    return false;
                }

                bounds = new RectInt(
                    minX,
                    minY,
                    maxX - minX + 1,
                    maxY - minY + 1);
                return true;
            }

            private static Color32 SampleBilinear(
                IReadOnlyList<Color32> pixels,
                int width,
                int height,
                float x,
                float y)
            {
                x = Mathf.Clamp(x, 0f, width - 1f);
                y = Mathf.Clamp(y, 0f, height - 1f);
                int x0 = Mathf.FloorToInt(x);
                int y0 = Mathf.FloorToInt(y);
                int x1 = Mathf.Min(x0 + 1, width - 1);
                int y1 = Mathf.Min(y0 + 1, height - 1);
                float tx = x - x0;
                float ty = y - y0;
                Color32 bottom = Lerp(pixels[y0 * width + x0], pixels[y0 * width + x1], tx);
                Color32 top = Lerp(pixels[y1 * width + x0], pixels[y1 * width + x1], tx);
                return Lerp(bottom, top, ty);
            }

            private static Color32 Lerp(Color32 from, Color32 to, float ratio) =>
                new(
                    ToByte(Mathf.Lerp(from.r, to.r, ratio)),
                    ToByte(Mathf.Lerp(from.g, to.g, ratio)),
                    ToByte(Mathf.Lerp(from.b, to.b, ratio)),
                    ToByte(Mathf.Lerp(from.a, to.a, ratio)));

            private static byte ToByte(float value) =>
                (byte)Mathf.Clamp(Mathf.RoundToInt(value), 0, 255);

            private static Bounds CalculateVisualBounds(GameObject root)
            {
                Renderer[] renderers = root.GetComponentsInChildren<Renderer>(false);
                bool hasBounds = false;
                Bounds bounds = default;
                foreach (Renderer renderer in renderers)
                {
                    if (!IsFramingRenderer(renderer))
                        continue;

                    if (!hasBounds)
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                }

                if (!hasBounds || bounds.size.sqrMagnitude <= 0.0001f)
                {
                    throw new InvalidOperationException(
                        $"촬영 가능한 MeshRenderer가 없습니다: {root.name}");
                }

                return bounds;
            }

            private static bool IsFramingRenderer(Renderer renderer) =>
                renderer != null &&
                renderer.enabled &&
                renderer.gameObject.activeInHierarchy &&
                (renderer is MeshRenderer || renderer is SkinnedMeshRenderer);

            private static void DisableRuntimeComponents(GameObject root)
            {
                foreach (MonoBehaviour behaviour in
                         root.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    behaviour.enabled = false;
                }

                foreach (Animator animator in
                         root.GetComponentsInChildren<Animator>(true))
                {
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.Rebind();
                    animator.Update(0f);
                    animator.enabled = false;
                }

                foreach (SkinnedMeshRenderer renderer in
                         root.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    renderer.updateWhenOffscreen = true;
                }

                foreach (Camera camera in root.GetComponentsInChildren<Camera>(true))
                    camera.enabled = false;
                foreach (Light light in root.GetComponentsInChildren<Light>(true))
                    light.enabled = false;
                foreach (Canvas canvas in root.GetComponentsInChildren<Canvas>(true))
                    canvas.enabled = false;
                foreach (AudioSource audio in
                         root.GetComponentsInChildren<AudioSource>(true))
                {
                    audio.enabled = false;
                }

                foreach (ParticleSystem particle in
                         root.GetComponentsInChildren<ParticleSystem>(true))
                {
                    particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                    var renderer = particle.GetComponent<ParticleSystemRenderer>();
                    if (renderer != null)
                        renderer.enabled = false;
                }

                foreach (TrailRenderer trail in
                         root.GetComponentsInChildren<TrailRenderer>(true))
                {
                    trail.enabled = false;
                }

                foreach (LineRenderer line in
                         root.GetComponentsInChildren<LineRenderer>(true))
                {
                    line.enabled = false;
                }
            }

            private void ClearInstance()
            {
                if (_instance == null)
                    return;

                UnityEngine.Object.DestroyImmediate(_instance);
                _instance = null;
            }
        }
    }
}
