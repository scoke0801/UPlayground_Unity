using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UPlayGround.Data.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 미니맵 위에 표시되는 개별 아이콘.
    /// 액터 아이콘과 정적 마커(퀘스트 목표 등) 모두 이 클래스를 사용합니다.
    /// </summary>
    public class MinimapEntityIcon : MonoBehaviour, IPointerClickHandler
    {
        private Image         _image;
        private RectTransform _rectTransform;
        private GameActor     _trackedActor;  // 정적 마커는 null
        private TextMeshProUGUI _displayLabel;

        public GameActor TrackedActor  => _trackedActor;
        public bool      IsStaticMarker => _trackedActor == null;

        public event System.Action<MinimapEntityIcon> OnClickEvent;

        void IPointerClickHandler.OnPointerClick(PointerEventData e)
        {
            if (e.button == PointerEventData.InputButton.Left)
                OnClickEvent?.Invoke(this);
        }

        // ── 팩토리: 액터 아이콘 ─────────────────────────────────
        public static MinimapEntityIcon Create(Transform parent, GameActor actor,
                                               MinimapIconConfigSO.IconEntry entry)
        {
            var icon = CreateBase(parent, $"Icon_{actor.name}", entry);
            icon._trackedActor = actor;
            return icon;
        }

        // ── 팩토리: 정적 마커 (퀘스트 목표·위치 등) ──────────────
        public static MinimapEntityIcon CreateStatic(Transform parent, string label,
                                                     MinimapIconConfigSO.IconEntry entry)
        {
            return CreateBase(parent, $"Marker_{label}", entry);
        }

        private static MinimapEntityIcon CreateBase(Transform parent, string name,
                                                    MinimapIconConfigSO.IconEntry entry)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);

            var icon        = go.AddComponent<MinimapEntityIcon>();
            icon._image         = go.AddComponent<Image>();   // Image RequireComponent → RectTransform 자동 생성
            icon._rectTransform = icon._image.rectTransform;  // 생성된 RectTransform 획득

            icon._image.sprite = entry.sprite != null ? entry.sprite : CreateDotSprite();
            icon._image.color  = entry.color;
            icon._rectTransform.sizeDelta = new Vector2(entry.size, entry.size);

            return icon;
        }

        // ── 원형 점 스프라이트 생성 ──────────────────────────────────

        private static Sprite _cachedDotSprite;

        /// <summary>스프라이트가 없을 때 사용하는 16×16 안티앨리어싱 원형 점 스프라이트. 최초 1회만 생성하고 이후 캐시를 반환한다.</summary>
        private static Sprite CreateDotSprite()
        {
            if (_cachedDotSprite != null)
                return _cachedDotSprite;

            const int size   = 16;              // 텍스처 해상도 고정 — 실제 표시 크기는 entry.size(sizeDelta)로 제어
            float     center = size * 0.5f;
            float     radius = size * 0.5f - 1f;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist  = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(center, center));
                float alpha = Mathf.Clamp01(radius - dist + 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
            tex.Apply();
            _cachedDotSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
            return _cachedDotSprite;
        }

        // ── 위치·가시성 갱신 ─────────────────────────────────────

        /// <summary>
        /// 미니맵 앵커드 좌표로 아이콘을 이동하고 가시성을 설정합니다.
        /// </summary>
        public void UpdateIcon(Vector2 minimapPos, bool isVisible)
        {
            _rectTransform.anchoredPosition = minimapPos;

            bool alive = IsStaticMarker || _trackedActor != null;
            gameObject.SetActive(isVisible && alive);
        }

        // ── 런타임 외관 변경 ─────────────────────────────────────

        /// <summary>적 감지 상태 등 동적 색상 변경에 사용합니다.</summary>
        public void SetColor(Color color)
        {
            if (_image != null) _image.color = color;
        }

        public void SetEntry(MinimapIconConfigSO.IconEntry entry)
        {
            if (_image == null) return;
            _image.sprite = entry.sprite;
            _image.color  = entry.color;
            _rectTransform.sizeDelta = new Vector2(entry.size, entry.size);
            PositionDisplayLabel(entry.size);
        }

        /// <summary>사이클 상대처럼 이름 확인이 필요한 정적 마커에만 표시 문구를 붙인다.</summary>
        public void SetDisplayLabel(string label)
        {
            if (string.IsNullOrWhiteSpace(label))
            {
                if (_displayLabel != null)
                    _displayLabel.gameObject.SetActive(false);
                return;
            }

            if (_displayLabel == null)
            {
                var labelObject = new GameObject("DisplayLabel");
                labelObject.transform.SetParent(transform, false);
                _displayLabel = labelObject.AddComponent<TextMeshProUGUI>();
                _displayLabel.alignment = TextAlignmentOptions.Center;
                _displayLabel.fontSize = 14f;
                _displayLabel.color = Color.white;
                _displayLabel.raycastTarget = false;
                _displayLabel.textWrappingMode = TextWrappingModes.NoWrap;
                _displayLabel.rectTransform.sizeDelta = new Vector2(160f, 24f);
            }

            _displayLabel.text = label;
            _displayLabel.gameObject.SetActive(true);
            PositionDisplayLabel(_rectTransform != null ? _rectTransform.sizeDelta.y : 0f);
        }

        private void PositionDisplayLabel(float iconSize)
        {
            if (_displayLabel != null)
                _displayLabel.rectTransform.anchoredPosition = new Vector2(0f, iconSize * 0.5f + 12f);
        }
    }
}
