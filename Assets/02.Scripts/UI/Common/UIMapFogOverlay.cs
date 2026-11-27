using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 지도 배경의 자식으로 안개를 그린다. 배경의 크기·줌·패닝을 그대로 물려받으므로
    /// 지도 화면은 안개 위치를 따로 맞추지 않는다. 아이콘 컨테이너는 배경의 뒤 형제라 안개 위에 남는다.
    /// </summary>
    public sealed class UIMapFogOverlay
    {
        private static readonly int UvOriginId = Shader.PropertyToID("_MaskUvOrigin");
        private static readonly int UvAxisUId = Shader.PropertyToID("_MaskUvAxisU");
        private static readonly int UvAxisVId = Shader.PropertyToID("_MaskUvAxisV");
        private static readonly int MaskTexelId = Shader.PropertyToID("_MaskTexel");
        private static readonly int MapAspectId = Shader.PropertyToID("_MapAspect");

        private RawImage _image;
        private Material _material;
        private MinimapIconConfigSO _boundConfig;
        private Texture2D _boundMask;
        private ExplorationGrid _activeGrid;

        /// <summary>
        /// 지도 좌표의 아이콘이 안개 밖에 있는지 알려준다. 마지막 <see cref="Refresh"/>에서 안개를 그리지 않았으면 true.
        /// </summary>
        public bool IsRevealed(Vector3 mapPosition) => IsRevealed(_activeGrid, mapPosition);

        /// <summary><paramref name="grid"/>가 null이면 안개가 없는 지도다.</summary>
        public static bool IsRevealed(ExplorationGrid grid, Vector3 mapPosition) =>
            grid == null || grid.IsExploredAt(new Vector2(mapPosition.x, mapPosition.z));

        /// <summary>매 프레임 호출한다. 지도나 마스크가 바뀔 때만 재질을 다시 만든다.</summary>
        public void Refresh(IUIMapExplorationService service, Image background, string mapId,
            MinimapIconConfigSO config, bool isWorldMap)
        {
            MapFogView view = default;
            bool isVisible = background != null && background.enabled && service != null
                             && service.TryGetFogView(mapId, config, isWorldMap, out view);
            _activeGrid = isVisible ? view.Grid : null;
            if (!isVisible)
            {
                if (_image != null) _image.enabled = false;
                return;
            }

            EnsureImage(background.rectTransform);
            if (_material == null || _boundConfig != config || _boundMask != view.Mask)
                Bind(config, view);
            _image.enabled = true;
        }

        public void Release()
        {
            if (_image != null) _image.enabled = false;
            DestroyMaterial();
            _boundConfig = null;
            _boundMask = null;
            _activeGrid = null;
        }

        private void EnsureImage(RectTransform parent)
        {
            if (_image != null) return;

            var go = new GameObject("MapFog", typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.layer = parent.gameObject.layer;
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            _image = go.GetComponent<RawImage>();
            _image.raycastTarget = false;
        }

        /// <summary>
        /// Mask(스텐실) 아래에서는 Unity가 재질 사본을 캐시하므로 값을 바꾸지 않고 새 재질을 만든다.
        /// </summary>
        private void Bind(MinimapIconConfigSO config, MapFogView view)
        {
            DestroyMaterial();
            _boundConfig = config;
            _boundMask = view.Mask;

            Vector2 origin = ImageUvToMaskUv(config, view, Vector2.zero);
            Vector2 axisU = ImageUvToMaskUv(config, view, Vector2.right) - origin;
            Vector2 axisV = ImageUvToMaskUv(config, view, Vector2.up) - origin;

            _material = new Material(view.Material) { hideFlags = HideFlags.HideAndDontSave };
            _material.SetVector(UvOriginId, origin);
            _material.SetVector(UvAxisUId, axisU);
            _material.SetVector(UvAxisVId, axisV);
            _material.SetVector(MaskTexelId, new Vector2(1f / view.Mask.width, 1f / view.Mask.height));
            _material.SetFloat(MapAspectId, config.GetBackgroundAspect());

            _image.texture = view.Mask;
            _image.material = _material;
        }

        /// <summary>지도 이미지 UV(0~1)가 가리키는 마스크 UV를 구한다.</summary>
        public static Vector2 ImageUvToMaskUv(MinimapIconConfigSO config, MapFogView view, Vector2 imageUv)
        {
            Vector3 world = config.MapImagePosToWorld(imageUv - new Vector2(0.5f, 0.5f), Vector2.one);
            return new Vector2(
                (world.x - view.WindowMin.x) / view.WindowSize.x,
                (world.z - view.WindowMin.y) / view.WindowSize.y);
        }

        private void DestroyMaterial()
        {
            if (_material == null) return;
            if (_image != null) _image.material = null;
            if (Application.isPlaying) UnityEngine.Object.Destroy(_material);
            else UnityEngine.Object.DestroyImmediate(_material);
            _material = null;
        }
    }
}
