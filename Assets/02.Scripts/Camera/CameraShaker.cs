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
    /// 쉐이크는 가산형 보이스(voice) 모델 — 각 히트가 독립 보이스로 추가되어
    /// 여러 보이스의 회전/위치 기여가 합산된다. 빠른 콤보일수록 보이스가 겹쳐
    /// 진동이 누적되고(명조식 빌드업), 막타의 큰 SO가 그 위에 얹혀 버스트가 된다.
    /// 합산 결과는 멀미 방지를 위해 세이프밸브로 클램프한다.
    ///
    /// [런타임] CameraManager.OnUpdate() → ManualUpdate(dt) → 보이스 갱신
    /// [에디터] CameraShakerDataEditor → Animate(time) → 단일 프리뷰 보이스
    /// </summary>
    public class CameraShaker : MonoBehaviour
    {
        public enum ShakeSpace { Screen, World }

        private const float GLOBAL_SHAKE_MULTIPLIER = 1.0f;

        // 세이프밸브(Tier 3-I): 다중 히트 누적 시 멀미 방지용 합산 상한.
        private const float MAX_PITCH = 6f;   // 도
        private const float MAX_YAW   = 5f;   // 도
        private const float MAX_ROLL  = 3f;   // 도
        private const float MAX_POS   = 0.6f; // 미터 (레거시 Position 모드)
        private const int   MAX_ACTIVE_VOICES = 32;

        public static bool EditorPreview = true;

        // 에디터 프리뷰 전용 — CameraShakerDataEditor가 리플렉션으로 주입한다.
        [SerializeField] private CameraShakeData _shakeData;

        // ── 쉐이크 보이스 ─────────────────────────────────────────────
        /// <summary>활성 쉐이크 1건. 히트마다 추가되어 합산된다.</summary>
        private sealed class ShakeVoice
        {
            public CameraShakeData Data;
            public float           Elapsed;
            public float           DelaysTimer;
            public Vector3         NoiseSeed;
            public float           DirPitchWeight = 1f;
            public float           DirYawWeight   = 1f;
            public float           Strength       = 1f; // 외부 스케일 (설정 × 카덴스 × 거리감쇠)
            public Vector3         CurrentEuler;        // 이번 프레임 회전 기여 (도)
            public Vector3         CurrentPos;          // 이번 프레임 위치 기여 (미터, 레거시)
        }

        private readonly List<ShakeVoice>  _active  = new List<ShakeVoice>();
        private readonly Stack<ShakeVoice> _pool    = new Stack<ShakeVoice>();
        private ShakeVoice                 _previewVoice; // 에디터 프리뷰 전용 (Animate가 구동)

        // 회전 합산은 카메라 독립 → 프레임당 1회만 계산(클램프 포함)해 캐시.
        // 위치 합산은 스크린공간 의존이라 onPreRenderCamera에서 카메라별 계산한다.
        private Vector3 _frameEulerSum;

        // ── Punch ─────────────────────────────────────────────────────
        private bool           _isPunching;
        private Vector3        _punchLocalOffset;
        private float          _punchDuration;
        private float          _punchElapsed;
        private AnimationCurve _punchDecayCurve;

        // Pre/PostRender 카메라 원래 위치/회전 저장
        private readonly Dictionary<Camera, Vector3>    _savedPositions = new Dictionary<Camera, Vector3>();
        private readonly Dictionary<Camera, Quaternion> _savedRotations = new Dictionary<Camera, Quaternion>();

        // 수동 틱 모드 플래그
        private bool _autoUpdate = true;

        // ─────────────────────────────────────────────────────────────

        #region Public API

        public void SetAutoUpdate(bool enabled) => _autoUpdate = enabled;

        /// <summary>CameraManager.OnUpdate()에서 매 프레임 호출</summary>
        public void ManualUpdate(float deltaTime) => Tick(deltaTime);

        /// <summary>
        /// 쉐이크 보이스를 하나 추가한다(기존 보이스를 끊지 않고 합산).
        /// </summary>
        /// <param name="hitDirection">월드 타격 방향 — Rotation 모드에서 Pitch/Yaw 가중에 반영</param>
        /// <param name="strength">외부 강도 배율 (설정 슬라이더 × 카덴스 × 거리감쇠)</param>
        public void PlayShake(CameraShakeData data, Vector3 hitDirection, float strength = 1f)
        {
            if (data == null || strength <= 0f) return;

            ShakeVoice v = NewVoice(data, hitDirection, strength);
            if (_active.Count >= MAX_ACTIVE_VOICES)
            {
                Recycle(_active[0]);
                _active.RemoveAt(0);
            }
            _active.Add(v);

            RegisterShakeCameras(data);
            s_Shakers.AddUnique(this);
            EnsureCallbacks();
        }

        /// <summary>모든 쉐이크 보이스(런타임 + 에디터 프리뷰)를 즉시 중단한다.</summary>
        public void StopShake()
        {
            for (int i = 0; i < _active.Count; i++)
                Recycle(_active[i]);
            _active.Clear();
            _previewVoice  = null;
            _frameEulerSum = Vector3.zero;
            TryCleanup();
        }

        /// <summary>
        /// 타격 방향으로 카메라를 순간 밀어낸 뒤 감쇠 복귀.
        /// 짧은 방향성 킥이라 위치 기반을 유지한다(벽 클리핑 위험 낮음).
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

        #region 보이스 생성/회수

        private ShakeVoice NewVoice(CameraShakeData data, Vector3 hitDirection, float strength)
        {
            ShakeVoice v = _pool.Count > 0 ? _pool.Pop() : new ShakeVoice();
            v.Data           = data;
            v.Elapsed        = 0f;
            v.DelaysTimer    = 0f;
            v.Strength       = strength;
            v.DirPitchWeight = 1f;
            v.DirYawWeight   = 1f;
            v.CurrentEuler   = Vector3.zero;
            v.CurrentPos     = Vector3.zero;

            // 단일 base + 고정 오프셋으로 축 간 분리를 보장(독립 난수는 드물게 근접해 상관됨).
            float baseSeed = Random.value * 1000f;
            v.NoiseSeed = new Vector3(baseSeed, baseSeed + 137.13f, baseSeed + 311.71f);

            ApplyDirection(v, hitDirection);
            return v;
        }

        private void Recycle(ShakeVoice v)
        {
            v.Data = null;
            _pool.Push(v);
        }

        /// <summary>타격 월드 방향 → Pitch/Yaw 축 가중 (내려치기=Pitch, 횡베기=Yaw).</summary>
        private void ApplyDirection(ShakeVoice v, Vector3 worldDirection)
        {
            v.DirPitchWeight = 1f;
            v.DirYawWeight   = 1f;

            CameraShakeData d = v.Data;
            if (d == null || d.Mode != CameraShakeData.ShakeMode.Rotation) return;

            float bias = d.DirectionalBias;
            if (bias <= 0f || worldDirection.sqrMagnitude < 0.0001f) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            worldDirection.Normalize();
            float vert  = Mathf.Abs(Vector3.Dot(worldDirection, cam.transform.up));    // 0..1
            float horiz = Mathf.Abs(Vector3.Dot(worldDirection, cam.transform.right));  // 0..1

            // 0.4 바닥값을 둬 한쪽 축이 완전히 죽지 않게 한다.
            v.DirPitchWeight = Mathf.Lerp(1f, 0.4f + vert,  bias);
            v.DirYawWeight   = Mathf.Lerp(1f, 0.4f + horiz, bias);
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
            UpdateVoices(deltaTime);
            AggregateRotation();
            UpdatePunch(deltaTime);

            if (_active.Count == 0 && _previewVoice == null && !_isPunching)
            {
                s_Shakers.Remove(this);
                TryCleanup();
                _savedPositions.Clear();
                _savedRotations.Clear();
            }
        }

        private void UpdateVoices(float deltaTime)
        {
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ShakeVoice v = _active[i];
                if (!UpdateVoice(v, deltaTime))
                {
                    _active.RemoveAt(i);
                    Recycle(v);
                }
            }
        }

        /// <summary>보이스 1건 갱신. 종료되면 false.</summary>
        private bool UpdateVoice(ShakeVoice v, float deltaTime)
        {
            CameraShakeData d = v.Data;
            if (d == null) return false;

            v.Elapsed += deltaTime;
            float total = d.Duration + d.Delay;

            if (v.Elapsed >= total) return false;
            if (v.Elapsed < d.Delay)
            {
                v.CurrentEuler = Vector3.zero;
                v.CurrentPos   = Vector3.zero;
                return true;
            }

            ComputeVoice(v, Mathf.Clamp01(v.Elapsed / total), deltaTime, v.Elapsed);
            return true;
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
            if (!EditorPreview && !EditorApplication.isPlaying) { _previewVoice = null; return; }
            if (_shakeData == null) return;

            float total = _shakeData.Duration + _shakeData.Delay;
            if (time >= total) { StopShake(); return; }
            if (time < _shakeData.Delay) return;

            if (_previewVoice == null || _previewVoice.Data != _shakeData)
                _previewVoice = NewVoice(_shakeData, Vector3.zero, 1f);
            _previewVoice.Elapsed = time;

            RegisterEditorCameras();
            s_Shakers.AddUnique(this);
            EnsureCallbacks();
            ComputeVoice(_previewVoice, Mathf.Clamp01(time / total), 1f / 60f, time);
            AggregateRotation();
#endif
        }

        #endregion

        #region Shake Vector

        private void ComputeVoice(ShakeVoice v, float t, float deltaTime, float absoluteTime)
        {
            CameraShakeData d = v.Data;
            if (d == null) return;

            bool    rotationMode = d.Mode == CameraShakeData.ShakeMode.Rotation;
            Vector3 amplitude    = rotationMode ? d.RotationStrength : d.ShakeStrength;

            Vector3 noise;
            if (d.Noise == CameraShakeData.NoiseType.Perlin)
            {
                // 정합 노이즈: 축별 시드를 크게 벌려 0.5 군집을 피하고 [-1,1]로 재매핑.
                float coord = absoluteTime * Mathf.Max(0.01f, d.Frequency);
                noise = new Vector3(
                    Mathf.PerlinNoise(coord, v.NoiseSeed.x) * 2f - 1f,
                    Mathf.PerlinNoise(coord, v.NoiseSeed.y) * 2f - 1f,
                    Mathf.PerlinNoise(coord, v.NoiseSeed.z) * 2f - 1f);
            }
            else
            {
                // 레거시 Random: ShakesDelay 간격마다 새 난수 (계단식, 갱신 전까지 직전 값 유지)
                if (d.ShakesDelay > 0f)
                {
                    v.DelaysTimer += deltaTime;
                    if (v.DelaysTimer < d.ShakesDelay) return;
                    while (v.DelaysTimer >= d.ShakesDelay)
                        v.DelaysTimer -= d.ShakesDelay;
                }
                var rand = new Vector3(Random.value, Random.value, Random.value);
                noise = rand * (Random.value > 0.5f ? -1f : 1f);
            }

            // 방향 매칭 가중 (Rotation 모드. Position 모드는 가중이 항상 1)
            noise.x *= v.DirPitchWeight;
            noise.y *= v.DirYawWeight;

            Vector3 vec = GLOBAL_SHAKE_MULTIPLIER * d.ShakeCurve.Evaluate(t) * v.Strength
                          * Vector3.Scale(noise, amplitude);

            if (rotationMode)
            {
                v.CurrentEuler = vec;
                v.CurrentPos   = Vector3.zero;
            }
            else
            {
                v.CurrentPos   = vec;
                v.CurrentEuler = Vector3.zero;
            }
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

            _savedPositions[cam] = cam.transform.localPosition;
            _savedRotations[cam] = cam.transform.localRotation;

            if (Application.isPlaying && Time.timeScale <= 0f) return;

            // 위치 기여는 스크린공간 의존이라 카메라별 합산(레거시 Position 모드).
            Vector3 posSum = Vector3.zero;
            AccumulatePosition(_previewVoice, cam, ref posSum);
            for (int i = 0; i < _active.Count; i++)
                AccumulatePosition(_active[i], cam, ref posSum);
            posSum = Vector3.ClampMagnitude(posSum, MAX_POS);

            Vector3 punch = _isPunching ? GetPunchOffset() : Vector3.zero;

            cam.transform.localPosition += posSum + punch;

            // 회전은 카메라 독립 — Tick/Animate에서 캐시·클램프한 합산을 그대로 적용.
            if (_frameEulerSum != Vector3.zero)
                cam.transform.localRotation = _savedRotations[cam]
                                              * Quaternion.Euler(_frameEulerSum.x, _frameEulerSum.y, _frameEulerSum.z);
        }

        /// <summary>회전 보이스 합산(카메라 독립). 프레임당 1회 호출해 세이프밸브까지 적용.</summary>
        private void AggregateRotation()
        {
            Vector3 sum = Vector3.zero;
            AddRotation(_previewVoice, ref sum);
            for (int i = 0; i < _active.Count; i++)
                AddRotation(_active[i], ref sum);

            // 세이프밸브(Tier 3-I): 누적 합산 클램프로 멀미 방지
            sum.x = Mathf.Clamp(sum.x, -MAX_PITCH, MAX_PITCH);
            sum.y = Mathf.Clamp(sum.y, -MAX_YAW,   MAX_YAW);
            sum.z = Mathf.Clamp(sum.z, -MAX_ROLL,  MAX_ROLL);
            _frameEulerSum = sum;
        }

        private static void AddRotation(ShakeVoice v, ref Vector3 sum)
        {
            if (v?.Data != null && v.Data.Mode == CameraShakeData.ShakeMode.Rotation)
                sum += v.CurrentEuler;
        }

        private void AccumulatePosition(ShakeVoice v, Camera cam, ref Vector3 posSum)
        {
            if (v?.Data == null || v.Data.Mode == CameraShakeData.ShakeMode.Rotation) return;

            Vector3 p = v.CurrentPos;
            if (v.Data.ShakeSpace == ShakeSpace.Screen)
                p = cam.transform.rotation * p;
            posSum += p;
        }

        private void onPostRenderCamera(Camera cam)
        {
            if (_savedPositions.ContainsKey(cam))
                cam.transform.localPosition = _savedPositions[cam];
            if (_savedRotations.ContainsKey(cam))
                cam.transform.localRotation = _savedRotations[cam];
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

        private void RegisterShakeCameras(CameraShakeData data)
        {
            if (data == null) return;

            // SO의 Cameras 리스트는 읽기만 한다 — 런타임 카메라를 Add하면
            // 에디터에서 "Type mismatch" 오염이 생기므로 _savedPositions에만 등록한다.
            foreach (var c in data.Cameras)
                RegisterCamera(c);

            if (data.UseMainCamera)
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
