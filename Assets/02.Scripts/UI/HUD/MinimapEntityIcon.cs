using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 미니맵 위에 표시되는 개별 아이콘.
    /// 액터 아이콘과 정적 마커(퀘스트 목표 등) 모두 이 클래스를 사용합니다.
    /// </summary>
    public class MinimapEntityIcon : MonoBehaviour
    {
        private Image         _image;
        private RectTransform _rectTransform;
        private GameActor     _trackedActor;  // 정적 마커는 null

        public GameActor TrackedActor => _trackedActor;
        public bool      IsStaticMarker => _trackedActor == null;

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

            var icon          = go.AddComponent<MinimapEntityIcon>();
            icon._rectTransform = go.GetComponent<RectTransform>();
            icon._image       = go.AddComponent<Image>();

            icon._image.sprite = entry.sprite;
            icon._image.color  = entry.color;
            icon._rectTransform.sizeDelta = new Vector2(entry.size, entry.size);

            return icon;
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
        }
    }
}
