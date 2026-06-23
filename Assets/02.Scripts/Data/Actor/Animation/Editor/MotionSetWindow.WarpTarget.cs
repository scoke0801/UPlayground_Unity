using UnityEditor;
using UnityEngine;
using UPlayGround.Component;
using UPlayGround.Data;
using UPlayGround.MovementController;

namespace UPlayGround.Animation.Editor
{
    /// <summary>
    /// 모션 에디터에서 모션 워핑(MotionEvent_MotionWarp) 동작을 검증하기 위한 더미 타겟 관리.
    /// Player 모드에서만 동작. 액터 정면 거리/각도/높이로 더미 캡슐을 배치하고,
    /// MotionWarpController.SetTarget("primary", ...)을 주입(StartPlayback에서 1회 + 컨트롤러/스냅샷 토글 변경 시).
    /// 더미는 재생 중 완전히 고정된다 — 재생 전 사용자가 배치한 월드 고정점에 머물고, 워프(Snapshot)는
    /// 윈도우 시작 시점에 그 점을 캡처해 액터를 착지시킨다(추격/과도이동 없음).
    /// idle 상태에선 슬라이더/스폰/씬 핸들로 위치를 잡는다(핸들 드래그 시 슬라이더 값으로 역계산).
    /// </summary>
    public partial class MotionSetEditorWindow
    {
        // ── 설정 (EditorPrefs 영속) ──
        bool  _warpTargetEnabled;
        float _warpTargetDistance    = 2.5f;
        float _warpTargetAngle       = 0f;
        float _warpTargetHeight      = 0f;
        // false = follow (anchor 추적). 슬라이더/핸들 변경이 즉시 반영. 기본 권장.
        // true  = snapshot. SetTarget 시점에 위치 고정 — 워프 시작 직후 더미를 움직여도 무시.
        bool  _warpTargetUseSnapshot = false;

        // ── 런타임 상태 ──
        GameObject           _spawnedWarpTarget;
        Transform            _warpTargetTf;
        MeshRenderer         _warpDummyRenderer;       // 워프 상태에 따른 색상 피드백
        MaterialPropertyBlock _warpDummyMpb;            // 머티리얼 instancing 회피
        MotionWarpController _cachedWarpController;
        PlayerCombat         _cachedPlayerCombat;      // CurrentAttackData → 콘 시각화
        GameObject           _cachedWarpControllerOwner;
        bool                 _enemyLayerWarned;

        // SetTarget은 활성 키와 같으면 feasibility/applicable/blend를 매번 0으로 리셋한다.
        // 매 tick 호출하면 워프가 영원히 0% 진행 상태가 됨 — 변경 감지 후 1회만 주입.
        MotionWarpController _injectedController;
        bool                 _injectedUseSnapshot;
        bool                 _hasInjected;

        // URP _BaseColor / Built-in _Color 둘 다 set — 어느 셰이더든 동작.
        static readonly int s_BaseColorProp = Shader.PropertyToID("_BaseColor");
        static readonly int s_ColorProp     = Shader.PropertyToID("_Color");

        // 워프 평가 상태별 색상.
        static readonly Color s_ColorIdle        = new Color(0.55f, 0.55f, 0.55f, 1f);  // 회색: 대기/컨트롤러 없음
        static readonly Color s_ColorApplicable  = new Color(0.30f, 0.90f, 0.35f, 1f);  // 녹색: 적용 가능
        static readonly Color s_ColorWarpingFail = new Color(1.00f, 0.70f, 0.20f, 1f);  // 노랑: 워프 중인데 거부됨

        const string WarpDummyName    = "[MotionEditor] WarpTargetDummy";
        const string PREFS_WT_ENABLED  = "MotionSetWindow_WarpTargetEnabled";
        const string PREFS_WT_DISTANCE = "MotionSetWindow_WarpTargetDistance";
        const string PREFS_WT_ANGLE    = "MotionSetWindow_WarpTargetAngle";
        const string PREFS_WT_HEIGHT   = "MotionSetWindow_WarpTargetHeight";
        const string PREFS_WT_SNAPSHOT = "MotionSetWindow_WarpTargetSnapshot";

        bool WarpTargetGuiAllowed => _testActorMode == TestActorMode.Player;

        void LoadWarpTargetPrefs()
        {
            _warpTargetEnabled     = EditorPrefs.GetBool (PREFS_WT_ENABLED,  false);
            _warpTargetDistance    = EditorPrefs.GetFloat(PREFS_WT_DISTANCE, 2.5f);
            _warpTargetAngle       = EditorPrefs.GetFloat(PREFS_WT_ANGLE,    0f);
            _warpTargetHeight      = EditorPrefs.GetFloat(PREFS_WT_HEIGHT,   0f);
            _warpTargetUseSnapshot = EditorPrefs.GetBool (PREFS_WT_SNAPSHOT, false);
        }

        void SaveWarpTargetPrefs()
        {
            EditorPrefs.SetBool (PREFS_WT_ENABLED,  _warpTargetEnabled);
            EditorPrefs.SetFloat(PREFS_WT_DISTANCE, _warpTargetDistance);
            EditorPrefs.SetFloat(PREFS_WT_ANGLE,    _warpTargetAngle);
            EditorPrefs.SetFloat(PREFS_WT_HEIGHT,   _warpTargetHeight);
            EditorPrefs.SetBool (PREFS_WT_SNAPSHOT, _warpTargetUseSnapshot);
        }

        // ── GUI ──────────────────────────────────────────────────────
        void DrawWarpTargetControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            // 헤더 제목은 탭 스트립(DrawControlPanelTabs)이 표시한다.

            EditorGUI.BeginDisabledGroup(!WarpTargetGuiAllowed);

            using (var c = new EditorGUI.ChangeCheckScope())
            {
                _warpTargetEnabled = EditorGUILayout.ToggleLeft(
                    new GUIContent("활성",
                        "체크 시 더미 캡슐을 액터 앞쪽에 스폰하고 MotionWarpController에 1회 주입.\n" +
                        "follow 모드(기본)에선 더미 transform 갱신만으로 워프가 추적된다.\n" +
                        "Play 모드 + Player 모드 + 타겟 액터 필요."),
                    _warpTargetEnabled);
                if (c.changed)
                {
                    if (_warpTargetEnabled) TryEnsureWarpTargetSpawned();
                    else                    DestroyWarpTarget();
                }
            }

            EditorGUI.BeginDisabledGroup(!_warpTargetEnabled);
            using (var c = new EditorGUI.ChangeCheckScope())
            {
                _warpTargetDistance = EditorGUILayout.Slider("거리 (m)", _warpTargetDistance, 0f, 10f);
                _warpTargetAngle    = EditorGUILayout.Slider("각도 (°)", _warpTargetAngle, -180f, 180f);
                _warpTargetHeight   = EditorGUILayout.Slider("높이 (m)", _warpTargetHeight, -2f, 3f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("정면", GUILayout.Width(50)))
                    {
                        _warpTargetAngle = 0f;
                        _warpTargetHeight = 0f;
                    }
                    if (GUILayout.Button("좌45°",  GUILayout.Width(56))) _warpTargetAngle = -45f;
                    if (GUILayout.Button("우45°",  GUILayout.Width(56))) _warpTargetAngle =  45f;
                    if (GUILayout.Button("뒤(180°)", GUILayout.Width(72))) _warpTargetAngle = 180f;
                    GUILayout.FlexibleSpace();
                }

                _warpTargetUseSnapshot = EditorGUILayout.ToggleLeft(
                    new GUIContent("Snapshot 모드",
                        "해제(기본): follow — 슬라이더/핸들 변경을 즉시 추적.\n" +
                        "체크: 워프 윈도우 시작 시점 위치로 고정."),
                    _warpTargetUseSnapshot);

                if (c.changed)
                    UpdateWarpTargetTransform();
            }
            EditorGUI.EndDisabledGroup();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (_spawnedWarpTarget != null && _warpTargetTf != null)
                    EditorGUILayout.LabelField($"위치: {_warpTargetTf.position.x:F2}, {_warpTargetTf.position.y:F2}, {_warpTargetTf.position.z:F2}");
                else if (!WarpTargetGuiAllowed)
                    EditorGUILayout.LabelField("Player 모드 전용");
                else if (!Application.isPlaying)
                    EditorGUILayout.LabelField("Play 모드 필요");
                else if (_targetActor == null)
                    EditorGUILayout.LabelField("타겟 액터 필요");
                else
                    EditorGUILayout.LabelField("활성 토글 OFF");
            }

            DrawWarpRuntimeStatus();

            if (ShowPanelHelp)
                EditorGUILayout.HelpBox(
                    "MotionEvent_MotionWarp 이벤트의 resolverPolicy=UseExisting 경로에서 이 더미가 그대로 사용됩니다. " +
                    "ConeNearest/LockOnFirst/Hybrid는 이벤트 자체가 타겟을 다시 결정하므로 더미가 덮어쓰일 수 있습니다.",
                    MessageType.None);

            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndVertical();
        }

        // ── Spawn / Destroy ──────────────────────────────────────────
        void TryEnsureWarpTargetSpawned()
        {
            if (!WarpTargetGuiAllowed) return;
            if (!Application.isPlaying) return;
            if (_targetActor == null) return;

            if (_spawnedWarpTarget != null)
            {
                UpdateWarpTargetTransform();
                return;
            }
            SpawnWarpTarget();
        }

        void SpawnWarpTarget()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = WarpDummyName;

            // KCC가 더미에 막혀 워프 도달 위치가 짧아지지 않도록 trigger로 전환.
            // Physics.OverlapSphere 기본은 trigger 감지이므로 resolver 경로도 정상 동작.
            var col = go.GetComponent<Collider>();
            if (col != null) col.isTrigger = true;

            // 기본 캡슐(1m radius × 2m height)은 액터 대비 과도하게 큼 — 표준 액터 비율로 축소.
            go.transform.localScale = new Vector3(0.5f, 1f, 0.5f);

            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (enemyLayer < 0)
            {
                if (!_enemyLayerWarned)
                {
                    Debug.LogWarning("[MotionEditor] 'Enemy' 레이어 부재 — 기본 레이어로 폴백. ConeNearest resolver가 더미를 못 찾을 수 있음.");
                    _enemyLayerWarned = true;
                }
            }
            else
            {
                go.layer = enemyLayer;
            }

            _spawnedWarpTarget = go;
            _warpTargetTf      = go.transform;
            _warpDummyRenderer = go.GetComponent<MeshRenderer>();
            UpdateWarpTargetTransform();
            UpdateWarpDummyColor();
        }

        void DestroyWarpTarget()
        {
            // 캐시된 컨트롤러에서도 타겟 끊기 (Unity null overload는 destroyed 객체에 false)
            if (_cachedWarpController != null && _hasInjected)
                _cachedWarpController.ClearTarget(MotionWarpController.DefaultTargetKey);

            if (_spawnedWarpTarget != null)
                UnityEngine.Object.Destroy(_spawnedWarpTarget);

            _spawnedWarpTarget   = null;
            _warpTargetTf        = null;
            _warpDummyRenderer   = null;
            _injectedController  = null;
            _hasInjected         = false;
        }

        void UpdateWarpTargetTransform()
        {
            if (_spawnedWarpTarget == null || _warpTargetTf == null || _targetActor == null) return;

            Transform actorTf = _targetActor.transform;
            Quaternion yaw    = Quaternion.AngleAxis(_warpTargetAngle, Vector3.up);
            Vector3 forwardYaw = yaw * actorTf.forward;
            Vector3 pos = actorTf.position
                        + forwardYaw * _warpTargetDistance
                        + Vector3.up * _warpTargetHeight;
            _warpTargetTf.position = pos;

            // 더미를 액터 쪽으로 향하게 (시각용)
            Vector3 lookDir = actorTf.position - pos;
            lookDir.y = 0f;
            if (lookDir.sqrMagnitude > 1e-6f)
                _warpTargetTf.rotation = Quaternion.LookRotation(lookDir.normalized, Vector3.up);
        }

        // ── MotionWarp 주입 ──────────────────────────────────────────
        // OnEditorUpdate 재생 분기에서 매 tick 호출 + StartPlayback에서 루프 전 1회(t=0 윈도우 대응).
        // 더미 위치는 재생 중 고정(따라가지 않음) — 재생 전 배치 위치 유지.
        // SetTarget 자체는 컨트롤러/스냅샷토글 변경 시에만 1회 호출(평가 상태 리셋 방지).
        void InjectWarpTarget()
        {
            if (!_warpTargetEnabled) return;

            // 모드 외 전환·플레이 종료 시 자동 정리
            if (!WarpTargetGuiAllowed || !Application.isPlaying)
            {
                if (_spawnedWarpTarget != null) DestroyWarpTarget();
                return;
            }
            if (_targetActor == null) return;

            bool justSpawned = false;
            if (_spawnedWarpTarget == null)
            {
                SpawnWarpTarget();   // 스폰 시 1회 위치 잡음
                if (_spawnedWarpTarget == null) return;
                justSpawned = true;
            }

            RefreshWarpControllerCache();

            // 재생 중에는 더미를 액터에 따라 움직이지 않고 완전히 고정한다 — 재생 전 사용자가 슬라이더/씬핸들로
            // 배치한 월드 고정점에 머문다. 워프(Snapshot 정책)는 윈도우 시작 시점에 이 고정점을 캡처해 액터를
            // 그 지점으로 착지시키므로, 타겟이 멀어지지 않아 추격/과도이동이 없다.
            // (idle 위치잡기는 GUI 변경/스폰/씬핸들에서 처리. 스폰 직후엔 SpawnWarpTarget이 1회 배치했다.)

            UpdateWarpDummyColor();
            if (_cachedWarpController == null) return;

            bool needsInject = justSpawned
                            || !_hasInjected
                            || _injectedController != _cachedWarpController
                            || _injectedUseSnapshot != _warpTargetUseSnapshot;
            if (!needsInject) return;

            _cachedWarpController.SetTarget(
                MotionWarpController.DefaultTargetKey,
                _warpTargetTf,
                _warpTargetUseSnapshot);
            _injectedController  = _cachedWarpController;
            _injectedUseSnapshot = _warpTargetUseSnapshot;
            _hasInjected         = true;
        }

        // ── 색상 피드백 ──────────────────────────────────────────────
        // OnEditorUpdate 매 tick에서 호출(InjectWarpTarget 호출 후) — MPB 한 번 alloc 후 재사용.
        void UpdateWarpDummyColor()
        {
            if (_warpDummyRenderer == null) return;
            if (_warpDummyMpb == null) _warpDummyMpb = new MaterialPropertyBlock();

            Color c = ComputeWarpDummyColor();
            _warpDummyRenderer.GetPropertyBlock(_warpDummyMpb);
            _warpDummyMpb.SetColor(s_BaseColorProp, c);
            _warpDummyMpb.SetColor(s_ColorProp,     c);
            _warpDummyRenderer.SetPropertyBlock(_warpDummyMpb);
        }

        Color ComputeWarpDummyColor()
        {
            if (_cachedWarpController == null) return s_ColorIdle;
            if (_cachedWarpController.IsApplicable) return s_ColorApplicable;
            if (_cachedWarpController.IsMotionWarping) return s_ColorWarpingFail;
            return s_ColorIdle;
        }

        void RefreshWarpControllerCache()
        {
            if (_targetActor == null)
            {
                _cachedWarpController      = null;
                _cachedPlayerCombat        = null;
                _cachedWarpControllerOwner = null;
                return;
            }
            if (_cachedWarpControllerOwner == _targetActor && _cachedWarpController != null) return;

            _cachedWarpController = _targetActor.GetComponent<MotionWarpController>()
                                 ?? _targetActor.GetComponentInParent<MotionWarpController>()
                                 ?? _targetActor.GetComponentInChildren<MotionWarpController>();
            _cachedPlayerCombat = _targetActor.GetComponent<PlayerCombat>()
                               ?? _targetActor.GetComponentInChildren<PlayerCombat>();
            _cachedWarpControllerOwner = _targetActor;
        }

        // ── 워프 런타임 상태 라벨 ────────────────────────────────────
        // BuildWarpDebugText와 같은 데이터를 워프 타겟 박스 안에서 한 흐름에 본다.
        void DrawWarpRuntimeStatus()
        {
            if (_cachedWarpController == null) return;

            string applic = _cachedWarpController.IsApplicable ? "✓ 적용 중" : "✗ 미적용";
            string warp   = _cachedWarpController.IsMotionWarping
                ? $"워프 {_cachedWarpController.WarpRemainingTime:F2}s / {_cachedWarpController.WarpDuration:F2}s"
                : "워프 대기";
            string err    = _cachedWarpController.LastArrivalError > 0f
                ? $"오차 {_cachedWarpController.LastArrivalError:F2}m"
                : "—";
            EditorGUILayout.LabelField($"{warp} | {applic} | {err}", EditorStyles.miniLabel);

            string reason = _cachedWarpController.LastFailureReason;
            if (!string.IsNullOrEmpty(reason))
                EditorGUILayout.LabelField($"실패: {reason}", EditorStyles.miniLabel);
        }

        // ── Scene 핸들 ───────────────────────────────────────────────
        void DrawWarpTargetSceneHandle()
        {
            if (!_warpTargetEnabled) return;
            if (_spawnedWarpTarget == null || _warpTargetTf == null) return;
            if (_targetActor == null) return;

            DrawAttackCone();

            // 더미 마커 색상도 워프 상태에 연동.
            Color markerColor = _cachedWarpController != null && _cachedWarpController.IsApplicable
                ? new Color(0.3f, 0.9f, 0.35f, 0.95f)
                : new Color(1f, 0.5f, 0.25f, 0.9f);
            using (new Handles.DrawingScope(markerColor))
            {
                Handles.SphereHandleCap(0, _warpTargetTf.position, Quaternion.identity, 0.18f, EventType.Repaint);
                Handles.DrawDottedLine(_warpTargetTf.position, _targetActor.transform.position, 4f);
            }

            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(_warpTargetTf.position, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                ApplyHandleWorldPosition(newPos);
                UpdateWarpTargetTransform();
                Repaint();
            }
        }

        // 캐릭터 공통 호밍 reach(HomingReachRange/HomingReachAngle)를 액터 forward 기준 콘으로 가시화.
        // reach 각도는 ConeNearestResolver(`Vector3.Angle(forward, dir) > targetingAngle`)와 동일 정의 — half-angle.
        // 공격별 데이터가 아니라 캐릭터 공통값이므로, 공격이 활성(CurrentAttackData != null)일 때만 표시한다.
        void DrawAttackCone()
        {
            if (_cachedPlayerCombat == null) return;
            if (_cachedPlayerCombat.CurrentAttackData == null) return;
            float reachRange = _cachedPlayerCombat.HomingReachRange;
            float reachAngle = _cachedPlayerCombat.HomingReachAngle;
            if (reachRange <= 0f || reachAngle <= 0f) return;

            Transform actorTf = _targetActor.transform;
            Vector3 origin = actorTf.position;
            Vector3 fwd = actorTf.forward;
            Vector3 leftEdgeDir  = Quaternion.AngleAxis(-reachAngle, Vector3.up) * fwd;
            Vector3 rightEdgeDir = Quaternion.AngleAxis( reachAngle, Vector3.up) * fwd;

            bool inCone = IsTargetInAttackCone(reachRange, reachAngle);
            Color coneColor = inCone
                ? new Color(0.30f, 0.90f, 0.35f, 0.85f)
                : new Color(0.55f, 0.55f, 0.55f, 0.70f);

            using (new Handles.DrawingScope(coneColor))
            {
                Handles.DrawWireArc(origin, Vector3.up, leftEdgeDir, reachAngle * 2f, reachRange);
                Handles.DrawLine(origin, origin + leftEdgeDir  * reachRange);
                Handles.DrawLine(origin, origin + rightEdgeDir * reachRange);
            }
        }

        bool IsTargetInAttackCone(float reachRange, float reachAngle)
        {
            if (_warpTargetTf == null || _targetActor == null) return false;
            Transform actorTf = _targetActor.transform;
            Vector3 dir = _warpTargetTf.position - actorTf.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > reachRange * reachRange) return false;
            if (dir.sqrMagnitude < 1e-6f) return true;
            Vector3 fwdXZ = actorTf.forward;
            fwdXZ.y = 0f;
            if (fwdXZ.sqrMagnitude < 1e-6f) return false;
            return Vector3.Angle(fwdXZ, dir) <= reachAngle;
        }

        // 핸들로 옮긴 월드 좌표 → 액터 forward 기준 distance/angle/height 역계산.
        void ApplyHandleWorldPosition(Vector3 worldPos)
        {
            if (_targetActor == null) return;
            Transform actorTf = _targetActor.transform;
            Vector3 delta = worldPos - actorTf.position;
            _warpTargetHeight = delta.y;

            Vector3 horiz = new Vector3(delta.x, 0f, delta.z);
            _warpTargetDistance = horiz.magnitude;
            if (horiz.sqrMagnitude > 1e-6f)
            {
                Vector3 forwardXZ = actorTf.forward;
                forwardXZ.y = 0f;
                if (forwardXZ.sqrMagnitude > 1e-6f)
                {
                    forwardXZ.Normalize();
                    _warpTargetAngle = Vector3.SignedAngle(forwardXZ, horiz.normalized, Vector3.up);
                }
            }
        }
    }
}
