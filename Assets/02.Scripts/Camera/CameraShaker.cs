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
        
        [Space]
        [SerializeField] private CameraShakeData _shakeData;
        
        private bool _isShaking;
        
        private Dictionary<Camera, Vector3> _camerasPreRenderPosition = new Dictionary<Camera, Vector3>();
        private Vector3 _shakeVector;
        private float _delaysTimer;
        private float _elapsedTime = 0.0f;

        // --- 방향성 카메라 펀치 ---
        private Vector3 _punchDirection;
        private float _punchStrength;
        private float _punchDuration;
        private float _punchElapsed;
        private bool _isPunching;
        private AnimationCurve _punchDecayCurve;

        public void SetShakeData(CameraShakeData cameraShakeData)
        {
            if (_isShaking)
            {
                StopShake();
            }
            _shakeData = cameraShakeData;
        }
        #region Static
        //--------------------------------------------------------------------------------------------------------------------------------
        // STATIC
        // 카메라 콜백을 전달하는 데 정적 메서드를 사용하여
        // PreRender에서는 ScreenShake 구성 요소가 순서대로 호출되고,
        // PostRender에서는 역순으로 호출되도록 합니다.
        // 이렇게 하면 최종 카메라 위치가 원래 위치와 동일하게 유지되어
        // 동시 화면 흔들림 효과를 활성화할 수 있습니다.

        static bool s_CallbackRegistered;
        static List<CameraShaker> s_CameraShakes = new List<CameraShaker>();
        
        static void OnPreRenderCamera_Static_URP(ScriptableRenderContext context, Camera cam)
        {
            OnPreRenderCamera_Static(cam);
        }
        static void OnPostRenderCamera_Static_URP(ScriptableRenderContext context, Camera cam)
        {
            OnPostRenderCamera_Static(cam);
        }
        
        static void OnPreRenderCamera_Static(Camera cam)
        {
            int count = s_CameraShakes.Count;
            for (int i = 0; i < count; i++)
            {
                var ss = s_CameraShakes[i];
                ss.onPreRenderCamera(cam);
            }
        }

        static void OnPostRenderCamera_Static(Camera cam)
        {
            int count = s_CameraShakes.Count;
            for (int i = count-1; i >= 0; i--)
            {
                var ss = s_CameraShakes[i];
                ss.onPostRenderCamera(cam);
            }
        }

        static void RegisterStaticCallback(CameraShaker cameraShake)
        {
            s_CameraShakes.Add(cameraShake);

            if (!s_CallbackRegistered)
            {
                if (GraphicsSettings.currentRenderPipeline == null)
                {
                    // Built-in Render Pipeline
                    Camera.onPreRender += OnPreRenderCamera_Static;
                    Camera.onPostRender += OnPostRenderCamera_Static;
                }
                else
                {
                    // URP
                    RenderPipelineManager.beginCameraRendering += OnPreRenderCamera_Static_URP;
                    RenderPipelineManager.endCameraRendering += OnPostRenderCamera_Static_URP;
                }

                Camera.onPreRender += OnPreRenderCamera_Static;
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
                    // Built-in Render Pipeline
                    Camera.onPreRender -= OnPreRenderCamera_Static;
                    Camera.onPostRender -= OnPostRenderCamera_Static;
                }
                else
                {
                    // URP
                    RenderPipelineManager.beginCameraRendering -= OnPreRenderCamera_Static_URP;
                    RenderPipelineManager.endCameraRendering -= OnPostRenderCamera_Static_URP;
                }

                Camera.onPreRender -= OnPreRenderCamera_Static;
                Camera.onPostRender -= OnPostRenderCamera_Static;
                
                s_CallbackRegistered = false;
            }
        }
        #endregion
        
        public void Update()
        {
            UpdatePunch();
            
            if (_shakeData == null)
            {
                return;
            }
            _elapsedTime += Time.deltaTime;

            float totalDuration = _shakeData.Duration + _shakeData.Delay;
            if (_elapsedTime < totalDuration)
            {
                if (_elapsedTime < _shakeData.Delay)
                {
                    return;
                }

                if (!_isShaking)
                {
                    this.StartShake();
                }

                // duration of the camera shake
                float delta = Mathf.Clamp01(_elapsedTime/totalDuration);

                // delay between each camera move
                if (_shakeData.ShakesDelay > 0)
                {
                    _delaysTimer += Time.deltaTime;
                    if (_delaysTimer < _shakeData.ShakesDelay)
                    {
                        return;
                    }
                    else
                    {
                        while (_delaysTimer >= _shakeData.ShakesDelay)
                        {
                            _delaysTimer -= _shakeData.ShakesDelay;
                        }
                    }
                }

                var randomVec = new Vector3(Random.value, Random.value, Random.value);
                var shakeVec = Vector3.Scale(randomVec, _shakeData.ShakeStrength) * (Random.value > 0.5f ? -1 : 1);
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
                if (time < _shakeData.Delay)
                {
                    return;
                }

                if (!_isShaking)
                {
                    this.StartShake();
                }

                // duration of the camera shake
                float delta = Mathf.Clamp01(time/totalDuration);

                // delay between each camera move
                if (_shakeData.ShakesDelay > 0)
                {
                    _delaysTimer += Time.deltaTime;
                    if (_delaysTimer < _shakeData.ShakesDelay)
                    {
                        return;
                    }
                    else
                    {
                        while (_delaysTimer >= _shakeData.ShakesDelay)
                        {
                            _delaysTimer -= _shakeData.ShakesDelay;
                        }
                    }
                }

                var randomVec = new Vector3(Random.value, Random.value, Random.value);
                var shakeVec = Vector3.Scale(randomVec, _shakeData.ShakeStrength) * (Random.value > 0.5f ? -1 : 1);
                _shakeVector = GLOBAL_CAMERA_SHAKE_MULTIPLIER * _shakeData.ShakeCurve.Evaluate(delta) * shakeVec;
            }
            else if (_isShaking)
            {
                StopShake();
            }
        }

        public void StartShake()
        {
            if (_isShaking)
            {
                StopShake();
            }

            FetchCameras();
            
            _elapsedTime = 0.0f;
            _isShaking = true;
            RegisterStaticCallback(this);
        }

        public void StopShake()
        {
            _isShaking = false;
            _shakeVector = Vector3.zero;
            UnregisterStaticCallback(this);
        }

        /// <summary>
        /// 방향성 카메라 펀치 실행
        /// 타격 방향으로 카메라를 밀어낸 뒤 감쇠 커브를 따라 원위치로 복귀
        /// </summary>
        /// <param name="direction">월드 스페이스 기준 펀치 방향 (정규화 불필요, 내부에서 처리)</param>
        /// <param name="strength">펀치 강도 (카메라 이동량)</param>
        /// <param name="duration">펀치 지속 시간</param>
        /// <param name="decayCurve">감쇠 커브 (null이면 기본 EaseOut 사용)</param>
        public void Punch(Vector3 direction, float strength, float duration = 0.15f, AnimationCurve decayCurve = null)
        {
            if (direction.sqrMagnitude < 0.001f || strength <= 0f)
                return;

            _punchDirection = direction.normalized;
            _punchStrength = strength;
            _punchDuration = duration;
            _punchElapsed = 0f;
            _punchDecayCurve = decayCurve;
            _isPunching = true;

            // 펀치 시작 시 카메라 콜백이 등록되어 있지 않으면 등록
            if (!_isShaking)
            {
                FetchCameras();
                RegisterStaticCallback(this);
                _isShaking = true;
            }
        }

        /// <summary>
        /// 펀치 상태 업데이트
        /// </summary>
        private void UpdatePunch()
        {
            if (!_isPunching)
                return;

            _punchElapsed += Time.deltaTime;

            if (_punchElapsed >= _punchDuration)
            {
                _isPunching = false;

                // shake도 없고 punch도 끝났으면 콜백 해제
                if (_shakeData == null || _elapsedTime >= _shakeData.Duration + _shakeData.Delay)
                {
                    if (_isShaking)
                    {
                        StopShake();
                    }
                }
            }
        }

        /// <summary>
        /// 현재 프레임의 펀치 오프셋 벡터 계산
        /// </summary>
        private Vector3 GetPunchOffset()
        {
            if (!_isPunching)
                return Vector3.zero;

            float t = Mathf.Clamp01(_punchElapsed / _punchDuration);
            
            // 감쇠: 커브가 있으면 사용, 없으면 기본 EaseOut (1 - t²)
            float decay = _punchDecayCurve != null 
                ? _punchDecayCurve.Evaluate(t) 
                : 1f - (t * t);

            return _punchDirection * (_punchStrength * decay);
        }
        public void FetchCameras()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }
#endif
            if (_shakeData == null)
            {
                return;
            }
            foreach (var cam in _shakeData.Cameras)
            {
                if (cam == null) continue;

                _camerasPreRenderPosition.Remove(cam);
            }

            _shakeData.Cameras.Clear();

            if (_shakeData.UseMainCamera && Camera.main != null)
            {
                _shakeData.Cameras.Add(Camera.main);
            }

            foreach (var cam in _shakeData.Cameras)
            {
                if (cam == null) continue;

                if (!_camerasPreRenderPosition.ContainsKey(cam))
                {
                    _camerasPreRenderPosition.Add(cam, Vector3.zero);
                }
            }
        }
        
        
        private void onPreRenderCamera(Camera cam)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying && EditorPreview)
            {
                //add scene view camera if necessary
                if (SceneView.currentDrawingSceneView != null && SceneView.currentDrawingSceneView.camera == cam &&
                    !_camerasPreRenderPosition.ContainsKey(cam))
                {
                    _camerasPreRenderPosition.Add(cam, cam.transform.localPosition);
                }
            }
#endif

            if (_isShaking && _camerasPreRenderPosition.ContainsKey(cam))
            {
                _camerasPreRenderPosition[cam] = cam.transform.localPosition;

                if (Time.timeScale <= 0) return;

                // 랜덤 쉐이크 + 방향성 펀치를 합산
                Vector3 punchOffset = GetPunchOffset();

                switch (_shakeData?.ShakeSpace ?? ShakeSpace.Screen)
                {
                    case ShakeSpace.Screen:
                        cam.transform.localPosition += cam.transform.rotation * (_shakeVector + punchOffset);
                        break;
                    case ShakeSpace.World:
                        cam.transform.localPosition += _shakeVector + punchOffset;
                        break;
                }
            }
        }

        private void onPostRenderCamera(Camera cam)
        {
            if (_camerasPreRenderPosition.ContainsKey(cam))
            {
                cam.transform.localPosition = _camerasPreRenderPosition[cam];
            }
        }
    }
}