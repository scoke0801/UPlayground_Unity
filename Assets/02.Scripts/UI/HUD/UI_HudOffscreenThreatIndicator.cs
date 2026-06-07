using System.Collections.Generic;
using UnityEngine;
using UPlayGround.CameraSystem;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.UI;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    /// <summary>
    /// 오프스크린 적 공격 인디케이터 (명조/원신 식 방향 화살표).
    /// 플레이어를 인식한 적이 카메라 화면 밖에 있으면서 공격하려 할 때, 화면 가장자리에 방향 화살표를 띄운다.
    ///
    /// 설계 문서: Assets/docs/TODO/OFFSCREEN_THREAT_INDICATOR_DESIGN.md (1차 MVP 범위)
    ///
    /// 트리거(2단계 면):
    ///   1면 인식   : monster.Detection.HasTarget && CurrentTarget == 현재 플레이어
    ///   2면 오프스크린: WorldToViewportPoint z가 0 이하(카메라 뒤) 또는 x/y가 0~1 밖
    ///   등급 분기  : 인식만(흰색), 일반 공격(노랑), 강한 공격(빨강)
    ///
    /// 표시: 화면 중앙 기준 가상 원형 테두리(Config.ringRadius) 위에 적 방향을 가리키는 화살표를 배치한다.
    /// 1차 범위: 프러스텀 밖 판정만. 시야 내 가려짐(occlusion)·클러스터링·SFX는 future work.
    /// </summary>
    public class UI_HudOffscreenThreatIndicator : UI_Base
    {
        // EnemyAttackState.StateNameValue 와 동일. 공격 임박 판정 기준.
        private const string AttackStateName = "Attack";

        [Header("Config")]
        [SerializeField] private OffscreenThreatConfigSO _config;

        [Header("References")]
        [Tooltip("링의 중심이 될 RectTransform. 화면 중앙에 두고(anchor/pivot 0.5) 마커를 ringRadius 반경의 원 위에 배치한다.")]
        [SerializeField] private RectTransform _markerContainer;

        [Tooltip("풀링할 마커 프리팹.")]
        [SerializeField] private UIOffscreenThreatMarker _markerPrefab;

        private readonly List<MonsterActor> _trackedMonsters = new List<MonsterActor>();
        private readonly List<UIOffscreenThreatMarker> _markerPool = new List<UIOffscreenThreatMarker>();

        private PlayerActor _player;
        private Camera _camera;

        #region UI_Base

        protected override void OnInit()
        {
            base.OnInit();
            _layer = CanvasLayer.HUD;
        }

        protected override void OnShow()
        {
            base.OnShow();

            _player = GameObjectManager.Instance?.Player;
            RebuildTrackedMonsters();

            var gom = GameObjectManager.Instance;
            if (gom != null)
            {
                gom.OnActorRegistered   += OnActorRegistered;
                gom.OnActorUnregistered += OnActorUnregistered;
            }

            var party = PartyManager.Instance;
            if (party != null)
                party.OnSwapCompleted += OnSwapCompleted;
        }

        protected override void OnHide()
        {
            var gom = GameObjectManager.Instance;
            if (gom != null)
            {
                gom.OnActorRegistered   -= OnActorRegistered;
                gom.OnActorUnregistered -= OnActorUnregistered;
            }

            var party = PartyManager.Instance;
            if (party != null)
                party.OnSwapCompleted -= OnSwapCompleted;

            _trackedMonsters.Clear();
            HideAllMarkers();

            base.OnHide();
        }

        #endregion

        #region 이벤트 콜백

        // 파티 스왑으로 활성 캐릭터(플레이어)가 바뀌면 비교 대상 갱신
        private void OnSwapCompleted(PlayerActor newPlayer) => _player = newPlayer;

        private void OnActorRegistered(GameActor actor)
        {
            if (actor is MonsterActor monster && !_trackedMonsters.Contains(monster))
                _trackedMonsters.Add(monster);
        }

        private void OnActorUnregistered(GameActor actor)
        {
            if (actor is MonsterActor monster)
                _trackedMonsters.Remove(monster);
        }

        private void RebuildTrackedMonsters()
        {
            _trackedMonsters.Clear();

            var gom = GameObjectManager.Instance;
            if (gom == null)
                return;

            var all = gom.AllActors;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] is MonsterActor monster)
                    _trackedMonsters.Add(monster);
            }
        }

        #endregion

        #region 갱신

        private void LateUpdate()
        {
            if (!IsVisible)
                return;

            // 카메라 모드 게이트: 인게임이 아닐 때(대화/킬캠/시네마틱 등) 표시하지 않는다.
            var cameraManager = CameraManager.Instance;
            if (cameraManager == null || cameraManager.CurrentCameraMode != CameraModeType.InGame)
            {
                HideAllMarkers();
                return;
            }

            // 스왑 없는 고정 플레이어(Bokusei)가 HUD 표시 시점 이후에 스폰되는 경우를 대비해
            // _player가 비어 있으면 매 프레임 지연 재획득한다. (OnSwapCompleted는 스왑 시에만 발생)
            if (_player == null)
                _player = GameObjectManager.Instance?.Player;

            _camera = cameraManager.GetMainCamera();
            if (_camera == null || _player == null || _config == null
                || _markerContainer == null || _markerPrefab == null)
            {
                HideAllMarkers();
                return;
            }

            // 핫패스: 설정/플레이어/화면 값은 루프 밖에서 1회만 읽는다.
            Transform playerTr = _player.transform;
            Vector3 playerPos  = playerTr.position;
            float maxDistSqr   = _config.maxDistance > 0f ? _config.maxDistance * _config.maxDistance : float.MaxValue;
            float ringRadius   = _config.ringRadius;
            float forwardOffset = _config.markerForwardAngleOffset;
            float screenW      = Screen.width;
            float screenH      = Screen.height;
            bool  showDetected = _config.showDetectedOnly;
            Color attackColor   = _config.attackImminentColor;
            Color strongColor   = _config.strongAttackColor;
            Color detectedColor = _config.detectedColor;
            float attackScale   = _config.attackImminentScale;
            float strongScale   = _config.strongAttackScale;
            float detectedScale = _config.detectedScale;
            float pulseSpeed    = _config.pulseSpeed;
            float pulseAmount   = _config.pulseAmount;
            Camera cam = _camera;
            int used = 0;

            for (int i = 0; i < _trackedMonsters.Count; i++)
            {
                MonsterActor monster = _trackedMonsters[i];
                if (monster == null)
                    continue;

                // 사망(디졸브 등)했지만 아직 unregister 되지 않은 적은 위협이 아니므로 제외
                if (!monster.IsAlive())
                    continue;

                // 1면: 인식 — 적이 현재 플레이어를 타겟으로 잡고 있어야 한다. (가장 싼 필터 먼저)
                var detection = monster.Detection;
                if (detection == null || !detection.HasTarget)
                    continue;
                if (detection.CurrentTarget != playerTr)
                    continue;

                Vector3 worldPos = monster.transform.position;

                // 거리 필터 — 투영보다 싼 sqr 비교를 먼저 수행한다.
                if ((worldPos - playerPos).sqrMagnitude > maxDistSqr)
                    continue;

                // 2면: 오프스크린 판정 — 카메라 투영은 후보당 1회(WorldToViewportPoint)만 수행한다.
                Vector3 viewport = cam.WorldToViewportPoint(worldPos);
                bool behind = viewport.z <= 0f;
                if (!behind
                    && viewport.x >= 0f && viewport.x <= 1f
                    && viewport.y >= 0f && viewport.y <= 1f)
                    continue; // 화면 안 → 표시하지 않음

                // 등급 판정 — 오프스크린 후보에 한해서만 상태/스킬을 조회한다.
                bool attackImminent = monster.ActorController?.CurrentState?.StateName == AttackStateName;
                if (!attackImminent && !showDetected)
                    continue;

                bool strongAttack = attackImminent && IsStrongAttack(monster);
                Color markerColor = strongAttack ? strongColor : attackImminent ? attackColor : detectedColor;
                float markerScale = strongAttack ? strongScale : attackImminent ? attackScale : detectedScale;

                // 가상 원형 테두리 위 배치 — 뷰포트 델타를 화면 비율로 환산해 방향각만 구한다.
                // (두 번째 투영/RectTransform 변환 없이 뷰포트 결과를 재사용)
                float dx = (viewport.x - 0.5f) * screenW;
                float dy = (viewport.y - 0.5f) * screenH;
                if (behind) { dx = -dx; dy = -dy; } // 카메라 뒤는 좌표가 반전됨

                float angle = (dx * dx + dy * dy) > 0.0001f ? Mathf.Atan2(dy, dx) : Mathf.PI * 0.5f;
                float cos = Mathf.Cos(angle);
                float sin = Mathf.Sin(angle);

                Vector2 anchored = new Vector2(cos, sin) * ringRadius;
                // 화살표 회전. 스프라이트 기본 향함 보정각을 더한다(+X 스프라이트면 offset=0).
                float angleDeg = angle * Mathf.Rad2Deg + forwardOffset;

                UIOffscreenThreatMarker marker = GetMarker(used++);
                marker.SetActiveMarker(true);
                marker.Apply(anchored, angleDeg,
                    markerColor,
                    markerScale,
                    attackImminent, pulseSpeed, pulseAmount);
            }

            // 사용하지 않은 풀 마커는 비활성화
            for (int i = used; i < _markerPool.Count; i++)
                _markerPool[i].SetActiveMarker(false);
        }

        private UIOffscreenThreatMarker GetMarker(int index)
        {
            while (_markerPool.Count <= index)
            {
                UIOffscreenThreatMarker marker = Instantiate(_markerPrefab, _markerContainer);
                marker.SetActiveMarker(false);
                _markerPool.Add(marker);
            }
            return _markerPool[index];
        }

        private void HideAllMarkers()
        {
            for (int i = 0; i < _markerPool.Count; i++)
                _markerPool[i].SetActiveMarker(false);
        }

        private static bool IsStrongAttack(MonsterActor monster)
        {
            var skill = monster?.Combat?.CurrentSkill;
            if (skill == null)
                return false;

            return skill.useDangerRing
                   || skill.attackCategory is EnemyAttackCategory.Heavy or EnemyAttackCategory.Skill;
        }

        #endregion
    }
}
