using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UPlayGround.Data;

namespace UPlayGround
{
    public class CameraShaker : MonoBehaviour
    {
        public enum ShakeSpace
        {
            Screen,
            World,
        }
        const float GLOBAL_CAMERA_SHAKE_MULTIPLIER = 1.0f;
        static public bool EditorPreview = true;

        [SerializeField] private CameraShakeData _shakeData;

        private bool _isShaking;
        private Dictionary<Camera, Vector3> _camerasPreRenderPosition = new Dictionary<Camera, Vector3>();
        private Vector3 _shakeVector;
        private float _delaysTimer;
        private float _elapsedTime = 0.0f;

        // ── 방향성 펀치 ──────────────────────────────────────────────
        // 월드 방향을 카메라 로컬 XY로 투영하여
        // 타격 방향에 따라 카메라가 실제로 다른 축으로 밀린다.
        private Vector3 _punchLocalOffset;   // 카메라 로컬 스페이스 기준 최대 오프셋
        private float   _punchDuration;
        private float   _punchElapsed;
        private bool    _isPunching;
        private AnimationCurve _punchDecayCurve;
        // ──────────────────────────────────────────────────────────────

        private float _shakeXMult = 1f;
        private float _shakeYMult = 1f;

        public void SetShakeData(CameraShakeData cameraShakeData)
        {
            if (_isShaking) StopShake();
            _shakeData  = cameraShakeData;
            _shakeXMult = 1f;
            _shakeYMult = 1f;
        }

        public void SetShakeStrengthMultiplier(float xMult, float yMult)
        {
            _shakeXMult = xMult;
            _shakeYMult = yMult;
        }

        #region Static Callbacks

        static bool s_CallbackRegistered;
        static List<CameraShaker> s_CameraShakes = new List<CameraShaker>();

        static void OnPreRenderCamera_Static_URP(ScriptableRenderContext context, Camera cam)  => OnPreRenderCamera_Static(cam);
        static void OnPostRenderCamera_Static_URP(ScriptableRenderContext context, Camera cam) => OnPostRenderCamera_Static(cam);

        static void OnPreRenderCamera_Static(Camera cam)
        {
            for (int i = 0; i < s_CameraShakes.Count; i++)
                s_CameraShakes[i].onPreRenderCamera(cam);
        }

        static void OnPostRenderCamera_Static(Camera cam)
        {
            for (int i = s_CameraShakes.Count - 1; i >= 0; i--)
                s_CameraShakes[i].onPostRenderCamera(cam);
        }

        static void RegisterStaticCallback(CameraShaker cameraShake)
        {
            s_CameraShakes.Add(cameraShake);
            if (!s_CallbackRegistered)
            {
                if (GraphicsSettings.currentRenderPipeline == null)
                {
                    Camera.onPreRender  += OnPreRenderCamera_Static;
                    Camera.onPostRender += OnPostRenderCamera_Static;
                }
                else
                {
                    RenderPipelineManager.beginCameraRendering += OnPreRenderCamera_Static_URP;
                    RenderPipelineManager.endCameraRendering   += OnPostRenderCamera_Static_URP;
                }
                Camera.onPreRender  += OnPreRenderCamera_Static;
                Camera.onPostRender += OnPostRenderCamera_Static;
                s_CallbackRegistered = true;
            }
        }

        static void UnregisterStaticCallback(CameraShaker cameraShake)
        {
            s_CameraShakes.Remove(cameraShake);
            if (s_CallbackRegistered && s_CameraShakes.Count == 0)
            {
                if (GraphicsSettings.currentRenderPipeline == null)
                {
                    Camera.onPreRender  -= OnPreRenderCamera_Static;
                    Camera.onPostRender -= OnPostRenderCamera_Static;
                }
                else
                {
                    RenderPipelineManager.beginCameraRendering -= OnPreRenderCamera_Static_URP;
                    RenderPipelineManager.endCameraRendering   -= OnPostRenderCamera_Static_URP;
                }
                Camera.onPreRender  -= OnPreRenderCamera_Static;
                Camera.onPostRender -= OnPostRenderCamera_Static;
                s_CallbackRegistered = false;
            }
        }

        #endregion

        public void Update()
        {
            UpdatePunch();

            if (_shakeData == null) return;

            _elapsedTime += Time.deltaTime;
            float totalDuration = _shakeData.Duration + _shakeData.Delay;

            if (_elapsedTime < totalDuration)
            {
                if (_elapsedTime < _shakeData.Delay) return;
                if (!_isShaking) StartShake();

                float delta = Mathf.Clamp01(_elapsedTime / totalDuration);

                if (_shakeData.ShakesDelay > 0)
                {
                    _delaysTimer += Time.deltaTime;
                    if (_delaysTimer < _shakeData.ShakesDelay) return;
                    while (_delaysTimer >= _shakeData.ShakesDelay)
                        _delaysTimer -= _shakeData.ShakesDelay;
                }

                var randomVec = new Vector3(Random.value, Random.value, Random.value);
                var shakeVec  = Vector3.Scale(randomVec, _shakeData.ShakeStrength) * (Random.value > 0.5f ? -1 : 1);
                shakeVec.x   *= _shakeXMult;
                shakeVec.y   *= _shakeYMult;

                _shakeVector = GLOBAL_CAMERA_SHAKE_MULTIPLIER * _shakeData.ShakeCurve.Evaluate(delta) * shakeVec;
            }
            else if (_isShaking)
            {
                StopShake();
            }
        }

        public void Animate(float time)
        {
#if UNITY_EDITOR
            if (!EditorPreview && !EditorApplication.isPlaying)
            {
                _shakeVector = Vector3.zero;
                return;
            }
#endif
            float totalDuration = _shakeData.Duration + _shakeData.Delay;
            if (time < totalDuration)
            {
                if (time < _shakeData.Delay) return;
                if (!_isShaking) StartShake();

                float delta = Mathf.Clamp01(time / totalDuration);

                if (_shakeData.ShakesDelay > 0)
                {
                    _delaysTimer += Time.deltaTime;
                    if (_delaysTimer < _shakeData.ShakesDelay) return;
                    while (_delaysTimer >= _shakeData.ShakesDelay)
                        _delaysTimer -= _shakeData.ShakesDelay;
                }

                var randomVec = new Vector3(Random.value, Random.value, Random.value);
                var shakeVec  = Vector3.Scale(randomVec, _shakeData.ShakeStrength) * (Random.value > 0.5f ? -1 : 1);
                _shakeVector  = GLOBAL_CAMERA_SHAKE_MULTIPLIER * _shakeData.ShakeCurve.Evaluate(delta) * shakeVec;
            }
            else if (_isShaking)
            {
                StopShake();
            }
        }

        public void StartShake()
        {
            if (_isShaking) StopShake();
            FetchCameras();
            _elapsedTime = 0f;
            _isShaking   = true;
            RegisterStaticCallback(this);
            _shakeXMult  = 1f;
            _shakeYMult  = 1f;
        }

        public void StopShake()
        {
            _isShaking   = false;
            _shakeVector = Vector3.zero;
            UnregisterStaticCallback(this);
        }

        /// <summary>
        /// 월드 스페이스 방향으로 카메라를 밀어낸 뒤 감쇠 복귀한다.
        /// 내부에서 월드 방향을 카메라 로컬 XY에 투영하므로
        /// 좌측 타격 → 카메라가 왼쪽으로, 상방 타격 → 카메라가 위로 밀린다.
        /// </summary>
        /// <param name="worldDirection">월드 기준 타격 방향 (정규화 불필요)</param>
        /// <param name="strength">펀치 강도</param>
        /// <param name="duration">복귀까지 걸리는 시간</param>
        /// <param name="decayCurve">감쇠 커브 (null = EaseOut 2차)</param>
        public void Punch(Vector3 worldDirection, float strength, float duration = 0.15f, AnimationCurve decayCurve = null)
        {
            if (worldDirection.sqrMagnitude < 0.001f || strength <= 0f)
                return;

            Camera cam = Camera.main;
            Vector3 localDir = cam != null
                ? ProjectWorldDirToCameraLocal(worldDirection.normalized, cam)
                : worldDirection.normalized;

            // 월드 방향을 카메라 로컬 XY로 압축했으므로 magnitude가 1 이하일 수 있다.
            // strength는 그대로 유지하고 방향만 투영 결과를 쓴다.
            _punchLocalOffset = localDir * strength;
            _punchDuration    = duration;
            _punchElapsed     = 0f;
            _punchDecayCurve  = decayCurve;
            _isPunching       = true;

            if (!_isShaking)
            {
                FetchCameras();
                RegisterStaticCallback(this);
                _isShaking = true;
            }
        }

        /// <summary>
        /// 월드 방향 벡터를 카메라 로컬 XY(화면 좌우/상하)로 투영한다.
        /// Z(전방) 성분은 버린다 — 카메라가 앞뒤로 흔들리는 건 타격감에 기여하지 않는다.
        /// </summary>
        private static Vector3 ProjectWorldDirToCameraLocal(Vector3 worldDir, Camera cam)
        {
            Vector3 camRight = cam.transform.right;
            Vector3 camUp    = cam.transform.up;

            float x = Vector3.Dot(worldDir, camRight); // 좌우
            float y = Vector3.Dot(worldDir, camUp);    // 상하

            // Z는 0: 화면 안쪽 방향 무시
            return new Vector3(x, y, 0f);
        }

        private void UpdatePunch()
        {
            if (!_isPunching) return;

            _punchElapsed += Time.deltaTime;
            if (_punchElapsed >= _punchDuration)
            {
                _isPunching = false;
                if (_shakeData == null || _elapsedTime >= _shakeData.Duration + _shakeData.Delay)
                {
                    if (_isShaking) StopShake();
                }
            }
        }

        /// <summary>현재 프레임의 펀치 오프셋 (카메라 로컬 스페이스)</summary>
        private Vector3 GetPunchOffset()
        {
            if (!_isPunching) return Vector3.zero;

            float t     = Mathf.Clamp01(_punchElapsed / _punchDuration);
            float decay = _punchDecayCurve != null
                ? _punchDecayCurve.Evaluate(t)
                : 1f - (t * t); // EaseOut 2차

            return _punchLocalOffset * decay;
        }

        public void FetchCameras()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode) return;
#endif
            if (_shakeData == null) return;

            foreach (var cam in _shakeData.Cameras)
            {
                if (cam == null) continue;
                _camerasPreRenderPosition.Remove(cam);
            }
            _shakeData.Cameras.Clear();

            if (_shakeData.UseMainCamera && Camera.main != null)
                _shakeData.Cameras.Add(Camera.main);

            foreach (var cam in _shakeData.Cameras)
            {
                if (cam == null) continue;
                if (!_camerasPreRenderPosition.ContainsKey(cam))
                    _camerasPreRenderPosition.Add(cam, Vector3.zero);
            }
        }

        private void onPreRenderCamera(Camera cam)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying && EditorPreview)
            {
                if (SceneView.currentDrawingSceneView != null && SceneView.currentDrawingSceneView.camera == cam
                    && !_camerasPreRenderPosition.ContainsKey(cam))
                {
                    _camerasPreRenderPosition.Add(cam, cam.transform.localPosition);
                }
            }
#endif
            if (!_isShaking || !_camerasPreRenderPosition.ContainsKey(cam)) return;

            _camerasPreRenderPosition[cam] = cam.transform.localPosition;
            if (Time.timeScale <= 0) return;

            // 랜덤 쉐이크는 기존 로컬 스페이스 적용
            // 펀치는 이미 카메라 로컬 XY로 계산되어 있으므로 동일하게 localPosition에 더한다
            Vector3 punchOffset = GetPunchOffset();

            switch (_shakeData?.ShakeSpace ?? ShakeSpace.Screen)
            {
                case ShakeSpace.Screen:
                    cam.transform.localPosition += cam.transform.rotation * _shakeVector + punchOffset;
                    break;
                case ShakeSpace.World:
                    cam.transform.localPosition += _shakeVector + punchOffset;
                    break;
            }
        }

        private void onPostRenderCamera(Camera cam)
        {
            if (_camerasPreRenderPosition.ContainsKey(cam))
                cam.transform.localPosition = _camerasPreRenderPosition[cam];
        }
    }
}
