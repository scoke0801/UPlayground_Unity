using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UPlayGround.Data;

namespace UPlayGround
{
    /// <summary>
    /// 카메라 쉐이크 + 방향성 펀치.
    ///
    /// [런타임] CameraManager.OnUpdate() → ManualUpdate(dt)
    /// [에디터] CameraShakerDataEditor → Animate(time)
    /// </summary>
    public class CameraShaker : MonoBehaviour
    {
        public enum ShakeSpace { Screen, World }

        private const float GLOBAL_SHAKE_MULTIPLIER = 1.0f;
        public static bool  EditorPreview            = true;

        [SerializeField] private CameraShakeData _shakeData;

        // ── Shake ─────────────────────────────────────────────────────
        private bool    _isShaking;
        private Vector3 _shakeVector;
        private float   _shakeElapsed;
        private float   _delaysTimer;
        private float   _shakeXMult = 1f;
        private float   _shakeYMult = 1f;

        // ── Punch ─────────────────────────────────────────────────────
        private bool           _isPunching;
        private Vector3        _punchLocalOffset;
        private float          _punchDuration;
        private float          _punchElapsed;
        private AnimationCurve _punchDecayCurve;

        // Pre/PostRender 카메라 원래 위치 저장
        private readonly Dictionary<Camera, Vector3> _savedPositions = new Dictionary<Camera, Vector3>();

        // 수동 틱 모드 플래그
        private bool _autoUpdate = true;

        // ─────────────────────────────────────────────────────────────

        #region Public API

        public void SetAutoUpdate(bool enabled) => _autoUpdate = enabled;

        /// <summary>CameraManager.OnUpdate()에서 매 프레임 호출</summary>
        public void ManualUpdate(float deltaTime) => Tick(deltaTime);

        public void SetShakeData(CameraShakeData data)
        {
            if (_isShaking) StopShake();
            _shakeData  = data;
            _shakeXMult = 1f;
            _shakeYMult = 1f;
        }

        public void SetShakeStrengthMultiplier(float xMult, float yMult)
        {
            _shakeXMult = xMult;
            _shakeYMult = yMult;
        }

        public void StartShake()
        {
            if (_shakeData == null) return;
            if (_isShaking) StopShake();

            _shakeElapsed = 0f;
            _isShaking    = true;
            _shakeXMult   = 1f;
            _shakeYMult   = 1f;

            RegisterShakeCameras();
            s_Shakers.AddUnique(this);
            EnsureCallbacks();
        }

        public void StopShake()
        {
            _isShaking   = false;
            _shakeVector = Vector3.zero;
            TryCleanup();
        }

        /// <summary>
        /// 타격 방향으로 카메라를 순간 밀어낸 뒤 감쇠 복귀.
        /// StartShake() 없이 단독 호출 가능.
        /// </summary>
        public void Punch(Vector3 worldDirection, float strength, float duration = 0.15f,
                          AnimationCurve decayCurve = null)
        {
            if (worldDirection.sqrMagnitude < 0.001f || strength <= 0f) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            _punchLocalOffset = ProjectToLocalXY(worldDirection.normalized, cam) * strength;
            _punchDuration    = Mathf.Max(duration, 0.01f);
            _punchElapsed     = 0f;
            _punchDecayCurve  = decayCurve;
            _isPunching       = true;

            RegisterCamera(cam);
            s_Shakers.AddUnique(this);
            EnsureCallbacks();
        }

        #endregion

        #region Unity 자동 Update

        private void Update()
        {
            if (!_autoUpdate) return;
            Tick(Time.deltaTime);
        }

        #endregion

        #region 틱

        private void Tick(float deltaTime)
        {
            UpdateShake(deltaTime);
            UpdatePunch(deltaTime);

            if (!_isShaking && !_isPunching)
            {
                s_Shakers.Remove(this);
                TryCleanup();
                _savedPositions.Clear();
            }
        }

        private void UpdateShake(float deltaTime)
        {
            if (_shakeData == null || !_isShaking) return;

            _shakeElapsed += deltaTime;
            float total    = _shakeData.Duration + _shakeData.Delay;

            if (_shakeElapsed >= total) { StopShake(); return; }
            if (_shakeElapsed < _shakeData.Delay) return;

            ComputeShakeVector(Mathf.Clamp01(_shakeElapsed / total), deltaTime);
        }

        private void UpdatePunch(float deltaTime)
        {
            if (!_isPunching) return;
            _punchElapsed += deltaTime;
            if (_punchElapsed >= _punchDuration)
                _isPunching = false;
        }

        #endregion

        #region Animate (에디터 프리뷰)

        public void Animate(float time)
        {
#if UNITY_EDITOR
            if (!EditorPreview && !EditorApplication.isPlaying) { _shakeVector = Vector3.zero; return; }
            if (_shakeData == null) return;

            float total = _shakeData.Duration + _shakeData.Delay;
            if (time >= total) { StopShake(); return; }
            if (time < _shakeData.Delay) return;

            RegisterEditorCameras();
            s_Shakers.AddUnique(this);
            EnsureCallbacks();
            ComputeShakeVector(Mathf.Clamp01(time / total), 1f / 60f);
#endif
        }

        #endregion

        #region Shake Vector

        private void ComputeShakeVector(float t, float deltaTime)
        {
            if (_shakeData == null) return;

            if (_shakeData.ShakesDelay > 0f)
            {
                _delaysTimer += deltaTime;
                if (_delaysTimer < _shakeData.ShakesDelay) return;
                while (_delaysTimer >= _shakeData.ShakesDelay)
                    _delaysTimer -= _shakeData.ShakesDelay;
            }

            var rand = new Vector3(Random.value, Random.value, Random.value);
            var vec  = Vector3.Scale(rand, _shakeData.ShakeStrength) * (Random.value > 0.5f ? -1 : 1);
            vec.x   *= _shakeXMult;
            vec.y   *= _shakeYMult;
            _shakeVector = GLOBAL_SHAKE_MULTIPLIER * _shakeData.ShakeCurve.Evaluate(t) * vec;
        }

        #endregion

        #region Pre/Post Render

        private void onPreRenderCamera(Camera cam)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying && EditorPreview
                && SceneView.currentDrawingSceneView?.camera == cam)
            {
                RegisterCamera(cam);
            }
#endif
            if (!_savedPositions.ContainsKey(cam)) return;
            if (Application.isPlaying && Time.timeScale <= 0f) return;

            _savedPositions[cam] = cam.transform.localPosition;

            Vector3 shake = (_isShaking || _shakeVector != Vector3.zero) ? GetShakeOffset(cam) : Vector3.zero;
            Vector3 punch = _isPunching ? GetPunchOffset() : Vector3.zero;

            cam.transform.localPosition += shake + punch;
        }

        private void onPostRenderCamera(Camera cam)
        {
            if (_savedPositions.ContainsKey(cam))
                cam.transform.localPosition = _savedPositions[cam];
        }

        private Vector3 GetShakeOffset(Camera cam)
        {
            if (_shakeData == null) return _shakeVector;
            return _shakeData.ShakeSpace == ShakeSpace.Screen
                ? cam.transform.rotation * _shakeVector
                : _shakeVector;
        }

        private Vector3 GetPunchOffset()
        {
            float t     = Mathf.Clamp01(_punchElapsed / _punchDuration);
            float decay = _punchDecayCurve != null
                ? _punchDecayCurve.Evaluate(t)
                : 1f - t * t; // EaseOut 2차
            return _punchLocalOffset * decay;
        }

        #endregion

        #region Camera Registration

        private void RegisterShakeCameras()
        {
            if (_shakeData == null) return;

            // SO의 Cameras 리스트는 읽기만 한다 — 런타임 카메라를 Add하면
            // 에디터에서 "Type mismatch" 오염이 생기므로 _savedPositions에만 등록한다.
            foreach (var c in _shakeData.Cameras)
                RegisterCamera(c);

            if (_shakeData.UseMainCamera)
                RegisterCamera(Camera.main);
        }

        private void RegisterEditorCameras()
        {
#if UNITY_EDITOR
            if (_shakeData != null)
                foreach (var c in _shakeData.Cameras)
                    RegisterCamera(c);

            if (SceneView.lastActiveSceneView != null)
                RegisterCamera(SceneView.lastActiveSceneView.camera);
#endif
        }

        private void RegisterCamera(Camera cam)
        {
            if (cam == null || _savedPositions.ContainsKey(cam)) return;
            _savedPositions.Add(cam, Vector3.zero);
        }

        #endregion

        #region Static Callback Management

        private static readonly List<CameraShaker> s_Shakers       = new List<CameraShaker>();
        private static          bool               s_CallbacksActive;

        private static void EnsureCallbacks()
        {
            if (s_CallbacksActive) return;
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                RenderPipelineManager.beginCameraRendering += OnBeginCamera;
                RenderPipelineManager.endCameraRendering   += OnEndCamera;
            }
            Camera.onPreRender  += OnPreStatic;
            Camera.onPostRender += OnPostStatic;
            s_CallbacksActive = true;
        }

        private static void TryCleanup()
        {
            if (!s_CallbacksActive || s_Shakers.Count > 0) return;
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                RenderPipelineManager.beginCameraRendering -= OnBeginCamera;
                RenderPipelineManager.endCameraRendering   -= OnEndCamera;
            }
            Camera.onPreRender  -= OnPreStatic;
            Camera.onPostRender -= OnPostStatic;
            s_CallbacksActive = false;
        }

        private static void OnBeginCamera(ScriptableRenderContext ctx, Camera cam) => OnPreStatic(cam);
        private static void OnEndCamera(ScriptableRenderContext ctx, Camera cam)   => OnPostStatic(cam);

        private static void OnPreStatic(Camera cam)
        {
            for (int i = 0; i < s_Shakers.Count; i++)
                s_Shakers[i].onPreRenderCamera(cam);
        }

        private static void OnPostStatic(Camera cam)
        {
            for (int i = s_Shakers.Count - 1; i >= 0; i--)
                s_Shakers[i].onPostRenderCamera(cam);
        }

        #endregion

        #region Utility

        private static Vector3 ProjectToLocalXY(Vector3 worldDir, Camera cam)
        {
            return new Vector3(
                Vector3.Dot(worldDir, cam.transform.right),
                Vector3.Dot(worldDir, cam.transform.up),
                0f);
        }

        private void OnDestroy()
        {
            s_Shakers.Remove(this);
            TryCleanup();
        }

        #endregion
    }

    internal static class CameraShakerListExtensions
    {
        public static void AddUnique<T>(this List<T> list, T item)
        {
            if (!list.Contains(item)) list.Add(item);
        }
    }
}
