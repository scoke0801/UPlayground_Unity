#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace UPlayGround.UI
{
    /// <summary>
    /// UI_Scene_Map 디버그 텔레포트 확장 (에디터 전용).
    /// Ctrl + 좌클릭한 지도 지점의 지면으로 플레이어를 즉시 옮겨 맵 검수 이동 시간을 줄인다.
    /// </summary>
    public partial class UI_Scene_Map
    {
        // 지도는 XZ만 알려주므로 높이는 플레이어 현재 높이 기준 위아래로 훑어 찾는다.
        // 맵 전체 고저차를 덮기 위한 넉넉한 값이며 튜닝 대상이 아니다.
        private const float DebugTeleportGroundProbeRange = 1000f;

        /// <summary>Ctrl이 눌린 주 입력이면 텔레포트를 시도하고 입력을 소비한다.</summary>
        private bool TryHandleDebugTeleportClick(PointerEventData e)
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.ctrlKey.isPressed)
                return false;

            // 텔레포트가 실패해도 입력은 소비한다. 실패한 자리에 마커가 찍히면 의도와 다른 결과가 남는다.
            TeleportPlayerToMapPoint(e);
            return true;
        }

        private void TeleportPlayerToMapPoint(PointerEventData e)
        {
            // 브라우즈 모드의 지도는 다른 씬이므로 좌표를 현재 월드에 대응시킬 수 없다.
            if (IsBrowsing || _config == null || _player == null)
                return;
            if (!TryResolveMapLocalPoint(e, out Vector2 localPoint))
                return;

            Vector3 target = MapLocalPosToWorld(localPoint);
            target.y = _player.transform.position.y;

            if (!ActorStagePlacement.TryProbeGround(
                    target,
                    referenceHeight: target.y,
                    maxHeightDelta: 0f,
                    probeUp: DebugTeleportGroundProbeRange,
                    probeDown: DebugTeleportGroundProbeRange,
                    ignoreRoot: _player.transform,
                    out Vector3 grounded))
            {
                Debug.LogWarning(
                    $"[UI_Scene_Map] 디버그 텔레포트 실패: ({target.x:0.#}, {target.z:0.#}) 지점에서 지면을 찾지 못했습니다.",
                    this);
                return;
            }

            _player.TeleportTo(grounded, _player.transform.rotation);
        }
    }
}
#endif
