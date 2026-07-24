using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UPlayGround.UI
{
    /// <summary>
    /// 인게임 월드 마커 1개의 화면 표시 요소. <see cref="UI_HudWorldMarker"/>가 풀링해서 사용한다.
    ///
    /// 프리팹 구성 권장:
    ///   - 루트: RectTransform (anchor/pivot = 중앙 0.5,0.5)
    ///   - 자식 Image: 마커 아이콘 (_icon에 연결)
    ///   - 자식 TextMeshProUGUI: 거리 라벨 (_distanceText에 연결, 아이콘 아래 배치 권장)
    /// </summary>
    public class UIWorldMarkerIcon : MonoBehaviour
    {
        [Tooltip("마커 아이콘 이미지")]
        [SerializeField] private Image _icon;

        [Tooltip("거리 라벨. 없어도 동작한다(거리 표시 생략).")]
        [SerializeField] private TextMeshProUGUI _distanceText;

        private RectTransform _rect;
        public RectTransform Rect => _rect != null ? _rect : (_rect = (RectTransform)transform);

        /// <summary>
        /// 마커 1개의 위치/아이콘/색/크기/거리 라벨을 한 번에 적용한다.
        /// </summary>
        /// <param name="anchoredPos">마커 컨테이너 기준 로컬 좌표.</param>
        /// <param name="sprite">아이콘 스프라이트.</param>
        /// <param name="color">아이콘 틴트 색상.</param>
        /// <param name="scale">크기 배율.</param>
        /// <param name="distanceLabel">거리 텍스트. null/빈 문자열이면 거리 라벨을 숨긴다.</param>
        public void Apply(Vector2 anchoredPos, Sprite sprite, Color color, float scale, string distanceLabel)
        {
            Rect.anchoredPosition = anchoredPos;
            Rect.localScale = Vector3.one * scale;

            if (_icon != null)
            {
                if (_icon.sprite != sprite) _icon.sprite = sprite;
                _icon.color = color;
            }

            if (_distanceText != null)
            {
                bool show = !string.IsNullOrEmpty(distanceLabel);
                if (_distanceText.gameObject.activeSelf != show)
                    _distanceText.gameObject.SetActive(show);
                if (show) _distanceText.text = distanceLabel;
            }
        }

        public void SetActiveMarker(bool active)
        {
            if (gameObject.activeSelf != active)
                gameObject.SetActive(active);
        }
    }
}
