using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UPlayGround.Tool.Editor
{
    /// <summary>수동 캡처와 맵 생성에서 동일한 렌더·임포트 품질을 보장한다.</summary>
    internal static class MinimapCaptureUtility
    {
        /// <summary>게임 품질에 의한 축소를 막고, 촬영 중 변경한 설정은 실패해도 복원한다.</summary>
        public static Texture2D Render(Camera camera, Vector2Int size, bool forceLod, float lodBias = 1000f)
        {
            int limit = Mathf.Min(16384, SystemInfo.maxTextureSize);
            if (size.x < 1 || size.y < 1 || size.x > limit || size.y > limit)
                throw new ArgumentOutOfRangeException(nameof(size), $"캡처 크기는 1~{limit}px 범위여야 합니다.");

            var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
            float savedRenderScale = pipeline != null ? pipeline.renderScale : 1f;
            int savedSamples = pipeline != null ? pipeline.msaaSampleCount : 1;
            float savedLodBias = QualitySettings.lodBias;
            int savedMaxLod = QualitySettings.maximumLODLevel;
            bool savedFog = RenderSettings.fog;
            var terrains = UnityEngine.Object.FindObjectsByType<Terrain>(FindObjectsSortMode.None);
            var errors = new float[terrains.Length];
            var distances = new float[terrains.Length];
            for (int i = 0; i < terrains.Length; i++)
            {
                errors[i] = terrains[i].heightmapPixelError;
                distances[i] = terrains[i].basemapDistance;
            }

            RenderTexture previous = RenderTexture.active;
            RenderTexture previousTarget = camera.targetTexture;
            bool savedMsaa = camera.allowMSAA;
            bool savedDynamicResolution = camera.allowDynamicResolution;
            RenderTexture target = null;
            Texture2D texture = null;
            try
            {
                var descriptor = new RenderTextureDescriptor(size.x, size.y, RenderTextureFormat.ARGB32, 24)
                {
                    msaaSamples = 4,
                    useMipMap = false,
                    useDynamicScale = false,
                    sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear
                };
                descriptor.msaaSamples = SystemInfo.GetRenderTextureSupportedMSAASampleCount(descriptor);
                if (pipeline != null)
                {
                    pipeline.renderScale = 1f;
                    pipeline.msaaSampleCount = descriptor.msaaSamples;
                }
                if (forceLod)
                {
                    QualitySettings.lodBias = Mathf.Max(1f, lodBias);
                    QualitySettings.maximumLODLevel = 0;
                    for (int i = 0; i < terrains.Length; i++)
                    {
                        terrains[i].heightmapPixelError = 1f;
                        // 높은 촬영 카메라 때문에 저해상도 지형 합성 텍스처로 전환되지 않게 한다.
                        terrains[i].basemapDistance = float.MaxValue;
                    }
                }
                RenderSettings.fog = false;
                camera.allowMSAA = true;
                camera.allowDynamicResolution = false;
                target = RenderTexture.GetTemporary(descriptor);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                texture = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false);
                texture.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                texture.Apply(updateMipmaps: false);
                return texture;
            }
            catch
            {
                if (texture != null) UnityEngine.Object.DestroyImmediate(texture);
                throw;
            }
            finally
            {
                camera.targetTexture = previousTarget;
                camera.allowMSAA = savedMsaa;
                camera.allowDynamicResolution = savedDynamicResolution;
                RenderTexture.active = previous;
                if (target != null) RenderTexture.ReleaseTemporary(target);
                RenderSettings.fog = savedFog;
                QualitySettings.lodBias = savedLodBias;
                QualitySettings.maximumLODLevel = savedMaxLod;
                if (pipeline != null)
                {
                    pipeline.renderScale = savedRenderScale;
                    pipeline.msaaSampleCount = savedSamples;
                }
                for (int i = 0; i < terrains.Length; i++)
                {
                    if (terrains[i] == null) continue;
                    terrains[i].heightmapPixelError = errors[i];
                    terrains[i].basemapDistance = distances[i];
                }
            }
        }

        /// <summary>이미지 원본 크기를 유지하고 Windows에서 지도 세부를 보존하는 BC7으로 가져온다.</summary>
        public static Sprite ImportSprite(string assetPath, Vector2Int size, bool hasAlpha)
        {
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("지도 이미지 임포터가 없습니다: " + assetPath);
            int maxSize = Mathf.Clamp(Mathf.NextPowerOfTwo(Mathf.Max(size.x, size.y)), 32, 16384);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = maxSize;
            // Unity는 NPOT 이미지의 밉 체인과 BC7을 함께 쓰면 RGBA32로 풀어 메모리가 크게 증가한다.
            bool canUseMipmaps = Mathf.IsPowerOfTwo(size.x) && Mathf.IsPowerOfTwo(size.y);
            importer.mipmapEnabled = canUseMipmaps;
            importer.ignoreMipmapLimit = true;
            importer.streamingMipmaps = false;
            importer.filterMode = canUseMipmaps ? FilterMode.Trilinear : FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.isReadable = false;
            importer.alphaIsTransparency = hasAlpha;
            importer.alphaSource = hasAlpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.crunchedCompression = false;
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            var platform = importer.GetPlatformTextureSettings("Standalone");
            platform.overridden = true;
            platform.maxTextureSize = maxSize;
            platform.format = TextureImporterFormat.BC7;
            platform.textureCompression = TextureImporterCompression.CompressedHQ;
            platform.compressionQuality = 100;
            platform.crunchedCompression = false;
            importer.SetPlatformTextureSettings(platform);
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath)
                ?? throw new InvalidOperationException("지도 Sprite 생성 실패: " + assetPath);
        }
    }
}
