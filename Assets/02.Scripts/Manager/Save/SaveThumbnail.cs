using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 세이브 슬롯 썸네일 캡처/저장/로드 유틸.
    ///
    /// - 저장 시점에 게임 카메라를 RenderTexture로 렌더해 PNG로 저장한다.
    ///   Screen Space Overlay UI는 카메라 렌더 결과에 포함되지 않으므로 자동 제외되고,
    ///   월드/카메라 스페이스 UI는 카메라 컬링마스크에서 'UI' 레이어를 제외해 제거한다.
    /// - 로드한 Texture2D/Sprite는 슬롯별로 캐시하며, 저장/삭제 시 무효화한다.
    /// - 캡처 실패는 경고만 남기고 저장 자체는 막지 않는다(썸네일은 부가 기능).
    /// </summary>
    public static class SaveThumbnail
    {
        private const int Width = 384;
        private const int Height = 216;         // 16:9
        private const string FilePrefix = "thumb_slot_";
        private const string Extension = ".png";
        private const string UILayerName = "UI";

        private static readonly Dictionary<int, Sprite> _spriteCache = new Dictionary<int, Sprite>();
        private static readonly Dictionary<int, Texture2D> _textureCache = new Dictionary<int, Texture2D>();

        public static string GetPath(string saveFolder, int slot) =>
            Path.Combine(saveFolder, $"{FilePrefix}{slot}{Extension}");

        /// <summary>
        /// 게임 카메라를 캡처해 슬롯 썸네일 PNG로 저장한다(UI 제외).
        /// 저장 성공 직후 호출한다. 실패해도 예외를 전파하지 않는다.
        /// </summary>
        public static void Capture(string saveFolder, int slot)
        {
            var cam = CameraManager.Instance != null ? CameraManager.Instance.GetMainCamera() : null;
            if (cam == null) cam = Camera.main;
            if (cam == null)
            {
                Debug.LogWarning("[SaveThumbnail] 캡처할 카메라를 찾지 못해 썸네일을 건너뜁니다.");
                return;
            }

            RenderTexture rt = null;
            Texture2D tex = null;
            int prevMask = cam.cullingMask;
            var prevTarget = cam.targetTexture;
            var prevActive = RenderTexture.active;
            try
            {
                // Overlay UI는 애초에 카메라 렌더에 안 잡히고, 월드/카메라 스페이스 UI만 여기서 제외.
                int uiLayer = LayerMask.NameToLayer(UILayerName);
                if (uiLayer >= 0)
                    cam.cullingMask &= ~(1 << uiLayer);

                rt = RenderTexture.GetTemporary(Width, Height, 24, RenderTextureFormat.ARGB32);

                var request = new RenderPipeline.StandardRequest { destination = rt };
                if (RenderPipeline.SupportsRenderRequest(cam, request))
                {
                    // Unity 6 URP 권장 경로.
                    RenderPipeline.SubmitRenderRequest(cam, request);
                }
                else
                {
                    // 빌트인 등 폴백.
                    cam.targetTexture = rt;
                    cam.Render();
                    cam.targetTexture = prevTarget;
                }

                RenderTexture.active = rt;
                tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
                tex.Apply();

                byte[] png = tex.EncodeToPNG();
                Directory.CreateDirectory(saveFolder);
                File.WriteAllBytes(GetPath(saveFolder, slot), png);

                Invalidate(slot);   // 캐시 무효화 → 다음 조회 시 새 파일 로드
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveThumbnail] 슬롯 {slot} 썸네일 캡처 실패: {e.Message}");
            }
            finally
            {
                cam.cullingMask = prevMask;
                cam.targetTexture = prevTarget;
                RenderTexture.active = prevActive;
                if (rt != null) RenderTexture.ReleaseTemporary(rt);
                if (tex != null) UnityEngine.Object.Destroy(tex);
            }
        }

        /// <summary> 슬롯 썸네일 Sprite를 반환한다. 파일이 없으면 null. 결과는 캐시된다. </summary>
        public static Sprite GetSprite(string saveFolder, int slot)
        {
            if (slot < 0) return null;
            if (_spriteCache.TryGetValue(slot, out var cached) && cached != null)
                return cached;

            string path = GetPath(saveFolder, slot);
            if (!File.Exists(path)) return null;

            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                var tex = new Texture2D(2, 2, TextureFormat.RGB24, false);
                if (!tex.LoadImage(bytes))
                {
                    UnityEngine.Object.Destroy(tex);
                    return null;
                }

                var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), 100f);
                _textureCache[slot] = tex;
                _spriteCache[slot] = sprite;
                return sprite;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveThumbnail] 슬롯 {slot} 썸네일 로드 실패: {e.Message}");
                return null;
            }
        }

        /// <summary> 슬롯 썸네일 파일을 삭제하고 캐시를 무효화한다. </summary>
        public static void Delete(string saveFolder, int slot)
        {
            try
            {
                string path = GetPath(saveFolder, slot);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SaveThumbnail] 슬롯 {slot} 썸네일 삭제 실패: {e.Message}");
            }
            Invalidate(slot);
        }

        /// <summary> 슬롯 캐시(스프라이트/텍스처)를 해제한다. </summary>
        public static void Invalidate(int slot)
        {
            if (slot < 0) return;

            if (_spriteCache.TryGetValue(slot, out var sprite) && sprite != null)
                UnityEngine.Object.Destroy(sprite);
            if (_textureCache.TryGetValue(slot, out var texture) && texture != null)
                UnityEngine.Object.Destroy(texture);

            _spriteCache.Remove(slot);
            _textureCache.Remove(slot);
        }
    }
}
