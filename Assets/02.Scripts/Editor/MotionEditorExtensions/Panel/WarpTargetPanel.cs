using UPlayGround.MovementController;
using UnityEditor;
using UnityEngine;

namespace UPlayGround.Animation.Editor
{
    public sealed class WarpTargetPanel :
        IMotionEditorPanel,
        IMotionEditorPanelLifecycle
    {
        private const string DummyName = "[MotionEditor] WarpTargetDummy";
        private bool _enabled;
        private bool _snapshot;
        private float _distance = 2.5f;
        private float _angle;
        private float _height;
        private float _targetRadius = 0.55f;
        private GameObject _dummy;
        private MotionWarpController _controller;
        private GameObject _owner;
        private bool _injected;

        public string Title => "워프 타겟";
        public int Order => 200;

        public bool IsAvailable(IMotionEditorContext context) =>
            context?.Subject?.Root != null;

        public void OnGUI(IMotionEditorContext context)
        {
            GameObject root = context.Subject?.Root;
            CacheController(root);
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                bool nextEnabled = EditorGUILayout.ToggleLeft(
                    "워프 타겟 활성",
                    _enabled);
                if (nextEnabled != _enabled)
                {
                    _enabled = nextEnabled;
                    if (_enabled)
                        EnsureDummy(root);
                    else
                        ReleaseDummy();
                }

                using (new EditorGUI.DisabledScope(!_enabled))
                {
                    EditorGUI.BeginChangeCheck();
                    _distance = EditorGUILayout.Slider("거리", _distance, 0.1f, 12f);
                    _angle = EditorGUILayout.Slider("각도", _angle, -180f, 180f);
                    _height = EditorGUILayout.Slider("높이", _height, -3f, 5f);
                    _targetRadius = EditorGUILayout.Slider("타겟 반경", _targetRadius, 0.2f, 2.5f);
                    bool nextSnapshot = EditorGUILayout.ToggleLeft(
                        "주입 시점 위치 스냅샷",
                        _snapshot);
                    if (EditorGUI.EndChangeCheck())
                    {
                        _snapshot = nextSnapshot;
                        if (!context.IsPlaying)
                            PositionDummy(root);
                        if (_dummy != null)
                            _dummy.transform.localScale = new Vector3(_targetRadius, 1f, _targetRadius);
                        _injected = false;
                        SceneView.RepaintAll();
                    }

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        if (GUILayout.Button("액터 기준 재배치"))
                            PositionDummy(root);
                        if (GUILayout.Button("지금 주입"))
                            Inject();
                    }
                }

                if (_controller == null)
                {
                    EditorGUILayout.HelpBox(
                        "선택 대상에서 MotionWarpController를 찾지 못했습니다.",
                        MessageType.Warning);
                }
                else
                {
                    string state = _controller.IsMotionWarping
                        ? $"워프 {_controller.WarpRemainingTime:F2}s / {_controller.WarpDuration:F2}s"
                        : "워프 대기";
                    string applicable = _controller.IsApplicable
                        ? "✓ 적용 중"
                        : "✗ 미적용";
                    EditorGUILayout.LabelField(
                        $"{state} | {applicable} | 오차 {_controller.LastArrivalError:F2}m",
                        EditorStyles.miniLabel);
                    if (!string.IsNullOrEmpty(_controller.LastFailureReason))
                        EditorGUILayout.LabelField(
                            $"실패: {_controller.LastFailureReason}",
                            EditorStyles.miniLabel);
                }
            }

            if (_enabled)
                EnsureDummy(root);
        }

        public void OnSceneGUI(IMotionEditorContext context)
        {
            if (!_enabled || _dummy == null)
                return;

            if (!context.IsPlaying)
            {
                EditorGUI.BeginChangeCheck();
                Vector3 next = Handles.PositionHandle(
                    _dummy.transform.position,
                    Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_dummy.transform, "워프 타겟 이동");
                    _dummy.transform.position = next;
                    UpdatePolarFromDummy(context.Subject.Root);
                    _injected = false;
                    context.Repaint();
                }
            }

            DrawWarpPreview(context.Subject.Root);
        }

        public void OnPlaybackStateChanged(
            IMotionEditorContext context,
            MotionPreviewPlaybackState state)
        {
            if (state == MotionPreviewPlaybackState.Playing && _enabled)
            {
                EnsureDummy(context.Subject?.Root);
                Inject();
            }
        }

        public void OnEditorClosed(IMotionEditorContext context)
        {
            ReleaseDummy();
        }

        private void CacheController(GameObject root)
        {
            if (root == _owner)
                return;
            if (_controller != null && _injected)
                _controller.ClearTarget(MotionWarpController.DefaultTargetKey);
            _owner = root;
            _controller = root != null
                ? root.GetComponent<MotionWarpController>() ??
                  root.GetComponentInParent<MotionWarpController>() ??
                  root.GetComponentInChildren<MotionWarpController>()
                : null;
            _injected = false;
        }

        private void EnsureDummy(GameObject root)
        {
            if (_dummy == null)
            {
                _dummy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                _dummy.name = DummyName;
                _dummy.hideFlags = HideFlags.HideAndDontSave;
                _dummy.transform.localScale = new Vector3(_targetRadius, 1f, _targetRadius);
                if (_dummy.TryGetComponent(out Collider collider))
                    collider.isTrigger = true;
                PositionDummy(root);
            }
        }

        private void PositionDummy(GameObject root)
        {
            if (_dummy == null || root == null)
                return;
            Quaternion yaw = Quaternion.AngleAxis(_angle, Vector3.up);
            Vector3 direction = yaw * root.transform.forward;
            _dummy.transform.position =
                root.transform.position +
                direction * _distance +
                Vector3.up * _height;
            _dummy.transform.rotation = Quaternion.LookRotation(
                -direction,
                Vector3.up);
            _injected = false;
        }

        private void UpdatePolarFromDummy(GameObject root)
        {
            Vector3 delta = _dummy.transform.position - root.transform.position;
            _height = delta.y;
            Vector3 flat = Vector3.ProjectOnPlane(delta, Vector3.up);
            _distance = flat.magnitude;
            if (flat.sqrMagnitude > 0.0001f)
                _angle = Vector3.SignedAngle(root.transform.forward, flat, Vector3.up);
        }

        private void Inject()
        {
            if (_controller == null || _dummy == null || _injected)
                return;
            _controller.SetTarget(
                MotionWarpController.DefaultTargetKey,
                _dummy.transform,
                _snapshot);
            _injected = true;
        }

        private void DrawWarpPreview(GameObject root)
        {
            if (_controller == null || root == null || !_controller.HasActiveWindow)
                return;

            MotionWarpWindowSettings settings = _controller.ActiveWindowSettings;
            Vector3 rootPosition = root.transform.position;
            Vector3 targetCenter = _controller.CurrentTargetCenter;
            Vector3 desired = _controller.CurrentDesiredArrival;
            Vector3 originalEnd = rootPosition + root.transform.rotation * settings.bakedLocalTotal;
            Vector3 correction = desired - originalEnd;
            correction.y = 0f;
            Vector3 limited = MotionWarpArrivalUtility.LimitCorrection(
                correction,
                settings.bakedPathLen,
                settings.maxCorrectionDistance,
                settings.maxCorrectionRatio);

            Handles.color = Color.gray;
            Handles.DrawAAPolyLine(3f, rootPosition, originalEnd);
            Handles.color = new Color(0.2f, 0.55f, 1f);
            Handles.DrawAAPolyLine(4f, rootPosition, originalEnd + limited);
            Handles.color = new Color(0.2f, 0.9f, 0.35f);
            Handles.DrawWireDisc(targetCenter, Vector3.up, _controller.CurrentArrivalShellRadius);
            Handles.color = Color.yellow;
            Handles.SphereHandleCap(0, desired, Quaternion.identity, 0.14f, EventType.Repaint);
            Handles.color = Color.red;
            Handles.DrawAAPolyLine(3f, originalEnd, originalEnd + correction);
            Handles.color = Color.cyan;
            Handles.DrawAAPolyLine(4f, originalEnd, originalEnd + limited);

            float ratio = settings.bakedPathLen > 0.0001f
                ? limited.magnitude / settings.bakedPathLen
                : 0f;
            Handles.Label(
                desired + Vector3.up * 0.25f,
                $"도착: {settings.arrivalMode}\n" +
                $"보정 {limited.magnitude:F2}m / {ratio:P0}\n" +
                $"Translation 종료 {settings.translationEndLeadTime:F2}s 전\n" +
                $"Bake {(settings.bakedValid ? "유효" : "무효")}");
        }

        private void ReleaseDummy()
        {
            if (_controller != null && _injected)
                _controller.ClearTarget(MotionWarpController.DefaultTargetKey);
            _injected = false;
            if (_dummy != null)
            {
                if (Application.isPlaying)
                    Object.Destroy(_dummy);
                else
                    Object.DestroyImmediate(_dummy);
            }
            _dummy = null;
        }
    }
}
