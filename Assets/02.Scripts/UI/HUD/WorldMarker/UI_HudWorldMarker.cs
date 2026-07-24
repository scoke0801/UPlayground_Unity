using System.Collections.Generic;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 인게임 월드 마커(원신식 HUD 웨이포인트) 표시 패널.
    /// <see cref="WorldMarkerRegistry"/>에 등록된 마커를 매 프레임 화면에 투영해,
    /// 타겟 월드 위치 위에 아이콘 + 남은 거리("94m")를 띄운다.
    ///
    /// 온스크린: 타겟 위치에 아이콘을 앵커한다.
    /// 오프스크린: Config.clampToScreenEdge가 켜져 있으면 아이콘을 화면 가장자리에 붙인다(방향 화살표는 별도 미구현).
    ///
    /// 카메라 모드가 InGame이 아닐 때(대화/킬캠/시네마틱)는 표시하지 않는다.
    /// 성능: LateUpdate 핫패스에서 마커당 카메라 투영 1회, 설정/플레이어/화면 값은 루프 밖에서 1회만 읽는다.
    /// </summary>
    public class UI_HudWorldMarker : UI_Base
    {
        [Header("Config")]
        [SerializeField] private WorldMarkerConfigSO _config;

        [Header("References")]
        [Tooltip("마커가 배치될 컨테이너. 화면 전체를 덮는 스트레치 RectTransform 권장(pivot 0.5).")]
        [SerializeField] private RectTransform _markerContainer;

        [Tooltip("풀링할 마커 아이콘 프리팹.")]
        [SerializeField] private UIWorldMarkerIcon _markerPrefab;

        // 활성 아이콘: 마커 id → 화면 요소
        private readonly Dictionary<string, UIWorldMarkerIcon> _active = new();
        private readonly Stack<UIWorldMarkerIcon> _pool = new();
        // 이번 프레임에 유효했던 마커 id (mark-and-sweep용, 재사용 버퍼)
        private readonly List<string> _sweep = new();

        private Camera _camera;

        #region UI_Base

        protected override void OnInit()
        {
            base.OnInit();
            _layer = CanvasLayer.HUD;
        }

        protected override void OnHide()
        {
            HideAll();
            base.OnHide();
        }

        #endregion

        private void LateUpdate()
        {
            if (!IsVisible)
                return;

            // 카메라 모드 게이트: 인게임이 아닐 때는 표시하지 않는다.
            var cameraManager = Svc.Camera;
            if (cameraManager == null || cameraManager.CurrentCameraMode != CameraModeType.InGame)
            {
                HideAll();
                return;
            }

            _camera = cameraManager.GetMainCamera();
            var player = UISvc.Actors?.Player;
            if (_camera == null || _config == null || _markerContainer == null || _markerPrefab == null
                || WorldMarkerRegistry.Count == 0)
            {
                HideAll();
                return;
            }

            // 핫패스: 설정/플레이어/화면 값은 루프 밖에서 1회만 읽는다.
            Vector3 playerPos     = player != null ? player.transform.position : _camera.transform.position;
            Camera cam            = _camera;
            float maxDistSqr      = _config.maxDistance > 0f ? _config.maxDistance * _config.maxDistance : float.MaxValue;
            float heightOffset    = _config.worldHeightOffset;
            bool showDistance     = _config.showDistanceLabel;
            string distanceFormat = _config.distanceFormat;
            float hideWithin      = _config.hideDistanceLabelWithin;
            float baseScale       = _config.baseScale;
            bool scaleByDistance  = _config.scaleByDistance;
            float scaleFalloff    = _config.scaleFalloffDistance;
            float minScale        = _config.minScale;
            bool clampEdge        = _config.clampToScreenEdge;
            float edgeMargin      = _config.edgeMargin;
            float screenW         = Screen.width;
            float screenH         = Screen.height;
            float halfW           = screenW * 0.5f - edgeMargin;
            float halfH           = screenH * 0.5f - edgeMargin;

            _sweep.Clear();

            var markers = WorldMarkerRegistry.Active;
            for (int m = markers.Count - 1; m >= 0; m--)
            {
                WorldMarkerData marker = markers[m];

                // 추종 대상이 파괴된 마커는 레지스트리에서 정리한다. 역순 순회라 Remove가 안전하다.
                if (marker.IsFollowLost)
                {
                    WorldMarkerRegistry.Remove(marker.id);
                    continue;
                }

                Vector3 worldPos = marker.WorldPosition;

                // 거리 필터 — 투영보다 싼 sqr 비교 먼저.
                Vector3 toTarget = worldPos - playerPos;
                float sqrDist = toTarget.sqrMagnitude;
                if (sqrDist > maxDistSqr)
                    continue;

                Vector3 projPos = worldPos + Vector3.up * heightOffset;
                Vector3 vp = cam.WorldToViewportPoint(projPos);
                bool behind = vp.z <= 0f;
                bool onScreen = !behind && vp.x >= 0f && vp.x <= 1f && vp.y >= 0f && vp.y <= 1f;

                if (!onScreen && !clampEdge)
                    continue; // 오프스크린 + 클램프 미사용 → 숨김

                Vector3 screenPos;
                if (onScreen)
                {
                    screenPos = new Vector3(vp.x * screenW, vp.y * screenH, 0f);
                }
                else
                {
                    // 오프스크린: 화면 중앙 기준 방향으로 사각형 가장자리에 클램프.
                    float dx = vp.x - 0.5f;
                    float dy = vp.y - 0.5f;
                    if (behind) { dx = -dx; dy = -dy; }
                    dx *= screenW;
                    dy *= screenH;
                    if (dx * dx + dy * dy < 0.0001f) dy = -1f;

                    float t = Mathf.Min(
                        halfW / Mathf.Max(Mathf.Abs(dx), 0.0001f),
                        halfH / Mathf.Max(Mathf.Abs(dy), 0.0001f));
                    screenPos = new Vector3(screenW * 0.5f + dx * t, screenH * 0.5f + dy * t, 0f);
                }

                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _markerContainer, screenPos, null, out Vector2 localPoint);

                // 크기: 거리 페이드 옵션.
                float scale = baseScale;
                float dist = Mathf.Sqrt(sqrDist);
                if (scaleByDistance && scaleFalloff > 0f)
                    scale = baseScale * Mathf.Lerp(1f, minScale, Mathf.Clamp01(dist / scaleFalloff));

                // 거리 라벨.
                string label = null;
                if (showDistance && !marker.hideDistance && dist >= hideWithin)
                    label = string.Format(distanceFormat, Mathf.RoundToInt(dist));

                UIWorldMarkerIcon icon = GetOrCreate(marker.id);
                icon.Apply(localPoint, marker.icon, marker.color, scale, label);
                _sweep.Add(marker.id);
            }

            SweepInactive();
        }

        #region 풀링

        private UIWorldMarkerIcon GetOrCreate(string id)
        {
            if (_active.TryGetValue(id, out UIWorldMarkerIcon existing) && existing != null)
                return existing;

            UIWorldMarkerIcon icon = null;
            while (_pool.Count > 0 && icon == null) // 파괴된 풀 엔트리(씬 전환 등)는 건너뛴다
                icon = _pool.Pop();
            if (icon == null)
                icon = Instantiate(_markerPrefab, _markerContainer);
            icon.SetActiveMarker(true);
            _active[id] = icon;
            return icon;
        }

        // 이번 프레임에 갱신되지 않은(범위 밖/오프스크린 숨김/제거된) 활성 아이콘을 풀로 반환한다.
        private void SweepInactive()
        {
            if (_active.Count == _sweep.Count)
                return;

            // _sweep에 없는 활성 id를 찾아 반환. (활성 수가 적어 선형 탐색으로 충분)
            _reusableKeys.Clear();
            foreach (var kv in _active)
                if (!_sweep.Contains(kv.Key))
                    _reusableKeys.Add(kv.Key);

            for (int i = 0; i < _reusableKeys.Count; i++)
                Release(_reusableKeys[i]);
        }
        private readonly List<string> _reusableKeys = new();

        private void Release(string id)
        {
            if (_active.TryGetValue(id, out UIWorldMarkerIcon icon))
            {
                _active.Remove(id);
                if (icon != null)
                {
                    icon.SetActiveMarker(false);
                    _pool.Push(icon);
                }
            }
        }

        private void HideAll()
        {
            if (_active.Count == 0)
                return;

            foreach (var kv in _active)
            {
                if (kv.Value != null)
                {
                    kv.Value.SetActiveMarker(false);
                    _pool.Push(kv.Value);
                }
            }
            _active.Clear();
        }

        #endregion
    }
}
