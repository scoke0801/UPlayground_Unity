using UnityEditor;
using UnityEngine;
using KinematicCharacterController;
using ActorAnimatorType = UPlayGround.Animation.ActorAnimator;

namespace UPlayGround.Animation.Editor
{
    public partial class MotionSetEditorWindow
    {
        // ── 설정 ──
        bool    _rootMotionEnabled         = true;
        float   _rootMotionUniformScale    = 1f;
        bool    _rootMotionAxisAdvanced;
        Vector3 _rootMotionAxisScale       = Vector3.one;
        bool    _rootMotionApplyRotation   = true;
        bool    _rootMotionDrawTrail       = true;

        // ── 런타임 상태 ──
        ActorAnimatorType        _cachedActorAnimator;
        KinematicCharacterMotor  _cachedKccMotor;
        Animator                 _cachedAnimatorRef;        // applyRootMotion 강제 ON 대상 — 모델 스왑 감지용
        bool                     _rootMotionPreviewActive;
        bool                     _rootMotionPrevApplyValue;
        Vector3                  _rootMotionInitialPosition;
        Quaternion               _rootMotionInitialRotation;

        // 트레일은 ring buffer로 보관. List.RemoveAt(0)의 O(n) 시프트와 매 Repaint ToArray() 할당을 피한다.
        const int                kRootMotionTrailMax = 1024;
        readonly Vector3[]       _rootMotionTrail = new Vector3[kRootMotionTrailMax];
        int                      _rootMotionTrailHead;
        int                      _rootMotionTrailCount;
        Vector3[]                _rootMotionTrailDrawBuffer; // count가 바뀔 때만 재할당, max 도달 후 0 alloc
        bool                     _rootMotionTrailDirty;

        // ── EditorPrefs ──
        const string PREFS_RM_ENABLED = "MotionSetWindow_RootMotionEnabled";
        const string PREFS_RM_SCALE   = "MotionSetWindow_RootMotionScale";
        const string PREFS_RM_AXISADV = "MotionSetWindow_RootMotionAxisAdv";
        const string PREFS_RM_AXIS_X  = "MotionSetWindow_RootMotionAxisX";
        const string PREFS_RM_AXIS_Y  = "MotionSetWindow_RootMotionAxisY";
        const string PREFS_RM_AXIS_Z  = "MotionSetWindow_RootMotionAxisZ";
        const string PREFS_RM_ROT     = "MotionSetWindow_RootMotionApplyRot";
        const string PREFS_RM_TRAIL   = "MotionSetWindow_RootMotionTrail";

        void LoadRootMotionPrefs()
        {
            _rootMotionEnabled       = EditorPrefs.GetBool (PREFS_RM_ENABLED, true);
            _rootMotionUniformScale  = EditorPrefs.GetFloat(PREFS_RM_SCALE,   1f);
            _rootMotionAxisAdvanced  = EditorPrefs.GetBool (PREFS_RM_AXISADV, false);
            _rootMotionAxisScale     = new Vector3(
                EditorPrefs.GetFloat(PREFS_RM_AXIS_X, 1f),
                EditorPrefs.GetFloat(PREFS_RM_AXIS_Y, 1f),
                EditorPrefs.GetFloat(PREFS_RM_AXIS_Z, 1f));
            _rootMotionApplyRotation = EditorPrefs.GetBool (PREFS_RM_ROT,     true);
            _rootMotionDrawTrail     = EditorPrefs.GetBool (PREFS_RM_TRAIL,   true);
        }

        void SaveRootMotionPrefs()
        {
            EditorPrefs.SetBool (PREFS_RM_ENABLED, _rootMotionEnabled);
            EditorPrefs.SetFloat(PREFS_RM_SCALE,   _rootMotionUniformScale);
            EditorPrefs.SetBool (PREFS_RM_AXISADV, _rootMotionAxisAdvanced);
            EditorPrefs.SetFloat(PREFS_RM_AXIS_X,  _rootMotionAxisScale.x);
            EditorPrefs.SetFloat(PREFS_RM_AXIS_Y,  _rootMotionAxisScale.y);
            EditorPrefs.SetFloat(PREFS_RM_AXIS_Z,  _rootMotionAxisScale.z);
            EditorPrefs.SetBool (PREFS_RM_ROT,     _rootMotionApplyRotation);
            EditorPrefs.SetBool (PREFS_RM_TRAIL,   _rootMotionDrawTrail);
        }

        /// 타겟 액터/모델 스왑 등으로 ActorAnimator·KCC가 바뀔 때 호출.
        public void RefreshRootMotionCache()
        {
            if (_targetActor == null)
            {
                _cachedActorAnimator = null;
                _cachedKccMotor      = null;
                // 활성 중 타겟이 사라졌다면 ref도 끊는다(복원 대상 부재).
                _cachedAnimatorRef = null;
                return;
            }

            _cachedActorAnimator = _targetActor.GetComponent<ActorAnimatorType>()
                                ?? _targetActor.GetComponentInChildren<ActorAnimatorType>(true);
            _cachedKccMotor      = _targetActor.GetComponent<KinematicCharacterMotor>()
                                ?? _targetActor.GetComponentInChildren<KinematicCharacterMotor>(true);

            // 프리뷰 활성 중 Animator가 스왑되었으면, 옛 ref엔 원래값 복원·새 ref엔 prev 캡처 후 강제 ON.
            if (_rootMotionPreviewActive)
            {
                var newAnim = _animancer != null ? _animancer.Animator : null;
                if (newAnim != _cachedAnimatorRef)
                {
                    if (_cachedAnimatorRef != null)
                        _cachedAnimatorRef.applyRootMotion = _rootMotionPrevApplyValue;
                    _cachedAnimatorRef = newAnim;
                    if (newAnim != null)
                    {
                        _rootMotionPrevApplyValue = newAnim.applyRootMotion;
                        newAnim.applyRootMotion = true;
                    }
                }
            }
        }

        void BeginRootMotionPreview()
        {
            if (_targetActor == null) return;

            // 이미 활성이면 옛 초기 포즈로 텔레포트하지 않고 *현재* 위치를 새 시작점으로 재캡처한다.
            // (pause → 위치 이동 → resume 시 사용자의 의도에 부합)
            if (_rootMotionPreviewActive)
            {
                RefreshRootMotionCache();
                _rootMotionInitialPosition = _targetActor.transform.position;
                _rootMotionInitialRotation = _targetActor.transform.rotation;
                ClearTrail();
                AddTrailPoint(_rootMotionInitialPosition);
                return;
            }

            RefreshRootMotionCache();
            if (_animancer == null || _animancer.Animator == null) return;

            _rootMotionInitialPosition = _targetActor.transform.position;
            _rootMotionInitialRotation = _targetActor.transform.rotation;
            _cachedAnimatorRef         = _animancer.Animator;
            _rootMotionPrevApplyValue  = _cachedAnimatorRef.applyRootMotion;
            _cachedAnimatorRef.applyRootMotion = true;

            ClearTrail();
            AddTrailPoint(_rootMotionInitialPosition);
            _rootMotionPreviewActive = true;
        }

        void TickRootMotionPreview()
        {
            if (!_rootMotionPreviewActive || !_rootMotionEnabled) return;
            if (_cachedActorAnimator == null || _targetActor == null) return;

            Vector3 deltaPos = _cachedActorAnimator.DeltaPosition;
            Quaternion deltaRot = _cachedActorAnimator.DeltaRotation;

            bool hasPos = deltaPos.sqrMagnitude > 1e-10f;
            // |w| = cos(θ/2) → 1-|w| ≈ θ²/8. acos 호출 회피용 데드밴드(편집기 핫패스).
            bool hasRot = 1f - Mathf.Abs(deltaRot.w) > 1e-9f;
            if (!hasPos && !hasRot) return;

            float scale = Mathf.Max(0f, _rootMotionUniformScale);
            Vector3 scaledDelta = _rootMotionAxisAdvanced
                ? Vector3.Scale(deltaPos, _rootMotionAxisScale) * scale
                : deltaPos * scale;

            Quaternion scaledRot = !_rootMotionApplyRotation
                ? Quaternion.identity
                : (Mathf.Approximately(scale, 1f)
                    ? deltaRot
                    : Quaternion.SlerpUnclamped(Quaternion.identity, deltaRot, scale));

            ApplyRootMotionDelta(scaledDelta, scaledRot);
        }

        void ApplyRootMotionDelta(Vector3 deltaPos, Quaternion deltaRot)
        {
            var t = _targetActor.transform;
            Vector3 newPos = t.position + deltaPos;
            // 런타임 컨벤션과 동일: rotation *= delta (PlayerAttackState 등 모든 상태가 이 순서).
            Quaternion newRot = t.rotation * deltaRot;

            if (_cachedKccMotor != null)
                _cachedKccMotor.SetPositionAndRotation(newPos, newRot);
            else
                t.SetPositionAndRotation(newPos, newRot);

            if (_rootMotionDrawTrail)
                AddTrailPoint(newPos);
        }

        void EndRootMotionPreview()
        {
            if (!_rootMotionPreviewActive) return;

            // Begin 시점에 ON으로 만든 *그* Animator 인스턴스에 원래값을 복원.
            if (_cachedAnimatorRef != null)
                _cachedAnimatorRef.applyRootMotion = _rootMotionPrevApplyValue;
            _cachedAnimatorRef = null;

            if (_targetActor != null)
            {
                if (_cachedKccMotor != null)
                    _cachedKccMotor.SetPositionAndRotation(_rootMotionInitialPosition, _rootMotionInitialRotation);
                else
                    _targetActor.transform.SetPositionAndRotation(_rootMotionInitialPosition, _rootMotionInitialRotation);
            }

            ClearTrail();
            _rootMotionPreviewActive = false;
        }

        void ResetRootMotionPreviewPose()
        {
            if (!_rootMotionPreviewActive || _targetActor == null) return;

            if (_cachedKccMotor != null)
                _cachedKccMotor.SetPositionAndRotation(_rootMotionInitialPosition, _rootMotionInitialRotation);
            else
                _targetActor.transform.SetPositionAndRotation(_rootMotionInitialPosition, _rootMotionInitialRotation);

            ClearTrail();
            AddTrailPoint(_rootMotionInitialPosition);
        }

        // ── 트레일 ring buffer 헬퍼 ──────────────────────────────────
        void AddTrailPoint(Vector3 p)
        {
            int writeIdx = (_rootMotionTrailHead + _rootMotionTrailCount) % kRootMotionTrailMax;
            _rootMotionTrail[writeIdx] = p;
            if (_rootMotionTrailCount < kRootMotionTrailMax)
                _rootMotionTrailCount++;
            else
                _rootMotionTrailHead = (_rootMotionTrailHead + 1) % kRootMotionTrailMax;
            _rootMotionTrailDirty = true;
        }

        void ClearTrail()
        {
            _rootMotionTrailHead = 0;
            _rootMotionTrailCount = 0;
            _rootMotionTrailDirty = true;
        }

        // count가 바뀔 때만 draw 버퍼를 재할당. count가 max로 안정되면 0 alloc.
        void EnsureTrailDrawBuffer()
        {
            if (!_rootMotionTrailDirty &&
                _rootMotionTrailDrawBuffer != null &&
                _rootMotionTrailDrawBuffer.Length == _rootMotionTrailCount) return;

            if (_rootMotionTrailDrawBuffer == null || _rootMotionTrailDrawBuffer.Length != _rootMotionTrailCount)
                _rootMotionTrailDrawBuffer = new Vector3[_rootMotionTrailCount];

            if (_rootMotionTrailHead == 0)
            {
                System.Array.Copy(_rootMotionTrail, 0, _rootMotionTrailDrawBuffer, 0, _rootMotionTrailCount);
            }
            else
            {
                int firstHalf = kRootMotionTrailMax - _rootMotionTrailHead;
                System.Array.Copy(_rootMotionTrail, _rootMotionTrailHead, _rootMotionTrailDrawBuffer, 0, firstHalf);
                System.Array.Copy(_rootMotionTrail, 0, _rootMotionTrailDrawBuffer, firstHalf, _rootMotionTrailHead);
            }
            _rootMotionTrailDirty = false;
        }

        void DrawRootMotionControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("루트 모션 프리뷰", EditorStyles.boldLabel);

            using (var c = new EditorGUI.ChangeCheckScope())
            {
                _rootMotionEnabled = EditorGUILayout.ToggleLeft("루트 모션 적용", _rootMotionEnabled);

                EditorGUI.indentLevel++;
                EditorGUI.BeginDisabledGroup(!_rootMotionEnabled);

                _rootMotionUniformScale = EditorGUILayout.Slider("스케일", _rootMotionUniformScale, 0f, 3f);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("0×", GUILayout.Width(36))) _rootMotionUniformScale = 0f;
                    if (GUILayout.Button("0.5×", GUILayout.Width(44))) _rootMotionUniformScale = 0.5f;
                    if (GUILayout.Button("1×", GUILayout.Width(36))) _rootMotionUniformScale = 1f;
                    if (GUILayout.Button("1.5×", GUILayout.Width(44))) _rootMotionUniformScale = 1.5f;
                    if (GUILayout.Button("2×", GUILayout.Width(36))) _rootMotionUniformScale = 2f;
                    GUILayout.FlexibleSpace();
                }

                _rootMotionAxisAdvanced = EditorGUILayout.ToggleLeft("축별 스케일", _rootMotionAxisAdvanced);
                if (_rootMotionAxisAdvanced)
                {
                    EditorGUI.indentLevel++;
                    _rootMotionAxisScale = EditorGUILayout.Vector3Field("XYZ 배율", _rootMotionAxisScale);
                    EditorGUI.indentLevel--;
                }

                _rootMotionApplyRotation = EditorGUILayout.ToggleLeft("회전 루트 모션 적용", _rootMotionApplyRotation);
                _rootMotionDrawTrail     = EditorGUILayout.ToggleLeft("궤적 표시", _rootMotionDrawTrail);

                EditorGUI.EndDisabledGroup();
                EditorGUI.indentLevel--;

                if (c.changed) SceneView.RepaintAll();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(!_rootMotionPreviewActive);
                if (GUILayout.Button("위치 리셋", GUILayout.Width(80)))
                    ResetRootMotionPreviewPose();
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();

                if (_rootMotionPreviewActive && _targetActor != null)
                {
                    float dist = Vector3.Distance(_rootMotionInitialPosition, _targetActor.transform.position);
                    EditorGUILayout.LabelField($"누적 이동: {dist:F2}m", GUILayout.Width(120));
                }
                else
                {
                    EditorGUILayout.LabelField("정지 상태", GUILayout.Width(120));
                }
            }

            // Player 액터는 라이브 상태머신이 DeltaPosition을 함께 소비할 수 있어,
            // 스케일 1×에서 클립 의도보다 더 멀리 이동하면 이중 적용이다.
            // 의심되면 비플레이어 액터(레지스트리 스폰)로 다시 측정한다.
            EditorGUILayout.HelpBox(
                "스케일 1×가 클립 의도보다 길게 이동하면 상태머신이 루트모션을 함께 소비 중입니다. 검증 시에는 레지스트리에서 비플레이어 액터를 스폰해 테스트하세요.",
                MessageType.None);

            DrawWarpBakeControls();

            EditorGUILayout.EndVertical();
        }

        void DrawRootMotionGizmo()
        {
            if (!_rootMotionPreviewActive || !_rootMotionDrawTrail) return;
            if (_rootMotionTrailCount < 2) return;

            EnsureTrailDrawBuffer();

            using (new Handles.DrawingScope(new Color(0.3f, 1f, 0.6f, 0.9f)))
            {
                Handles.DrawAAPolyLine(2f, _rootMotionTrailDrawBuffer);
                if (_targetActor != null)
                {
                    Handles.SphereHandleCap(0,
                        _targetActor.transform.position, Quaternion.identity,
                        0.05f, EventType.Repaint);
                    Handles.ArrowHandleCap(0,
                        _targetActor.transform.position,
                        Quaternion.LookRotation(_targetActor.transform.forward),
                        0.4f, EventType.Repaint);
                }
            }
        }
    }
}
