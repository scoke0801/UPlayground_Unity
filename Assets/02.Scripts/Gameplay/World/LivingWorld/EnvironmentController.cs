using System;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround.Gameplay.World.LivingWorld
{
    /// <summary>맵 시간표의 단일 생산자. 퀘스트·물길·저장 사실을 작성하지 않는다.</summary>
    public sealed class EnvironmentController : MonoBehaviour
    {
        [SerializeField] private string _mapId;
        [SerializeField] private TextAsset _profile;
        private EnvironmentSchedule _schedule;
        [SerializeField] private EnvironmentEffects _effects;
        [SerializeField] private Transform _observer;
        [SerializeField] private Transform _rainAnchor;
        [SerializeField] private Vector3 _rainOffset = new(0, 6, 0);
        [SerializeField] private Texture _windNoise;
        [SerializeField, Min(0)] private float _vegetationStrength = 1;
        [SerializeField, Min(0)] private float _vegetationSpeed = 1;
        [SerializeField, Min(0.01f)] private float _vegetationScale = 1;
        [SerializeField, Min(0)] private float _vegetationJitter = 0.2f;
        [SerializeField, Range(0, 1)] private float _overcastLightMultiplier = 0.75f;
        [SerializeField, Range(-1, 0)] private float _overcastExposure = -0.1f;
        private readonly EnvironmentShaderState _shaderState = new();
        private bool _hasOwnership, _hasFocus;
        private long _publishedMinute = -1;

        public string MapId => _mapId;
        public int ProfileVersion => _schedule != null ? _schedule.version : 0;
        public bool IsReady { get; private set; }
        public uint Revision { get; private set; }
        public EnvironmentSchedule.Sample Current { get; private set; }
        public float LightMultiplier => IsReady ? Mathf.Lerp(1, _overcastLightMultiplier, Current.Cloud) : 1;
        public float ExposureOffset => IsReady ? Current.Cloud * _overcastExposure : 0;
        public event Action<EnvironmentController> OnStateChanged;

        private void OnEnable()
        {
            try { _schedule = _profile != null ? JsonUtility.FromJson<EnvironmentSchedule>(_profile.text) : null; }
            catch (ArgumentException) { _schedule = null; }
            if (string.IsNullOrWhiteSpace(_mapId) || _schedule == null || !_schedule.IsValid())
            {
                Debug.LogError("환경 mapId 또는 시간표 프로필이 유효하지 않습니다.", this);
                return;
            }
            // 외부 원본 코드를 고치지 않고, 씬에서 중복 작성자를 제거하도록 실패를 드러낸다.
            foreach (MonoBehaviour writer in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
            {
                if (writer != null && writer.isActiveAndEnabled && writer.GetType().FullName == "SICS.WindSettings")
                {
                    Debug.LogError("환경 바람을 켜기 전에 씬의 SICS.WindSettings를 비활성화해야 합니다.", this);
                    return;
                }
            }
            _hasOwnership = _shaderState.TryAcquire();
            if (!_hasOwnership) Debug.LogError("환경 전역값의 작성자가 중복되었습니다.", this);
            if (_effects != null) _effects.Initialize();
        }

        private void Update()
        {
            if (!_hasOwnership) return;
            if (!Services.TryGet<IGameTimeService>(out var time))
            {
                ClearPresentation();
                return;
            }
            if (!EvaluateAt(time.CurrentDay, time.MinuteOfDay)) return;
            bool hasDialogue = Services.TryGet<IDialogueService>(out var dialogue) && dialogue.IsDialogueActive;
            if (_observer != null && _rainAnchor != null) _rainAnchor.position = _observer.position + _rainOffset;
            if (_effects != null) _effects.Apply(Current, _observer != null ? _observer.position : default,
                _observer != null, _hasFocus || hasDialogue);
        }

        private void LateUpdate()
        {
            if (_hasOwnership && IsReady && _effects != null) _effects.CullShelteredParticles();
        }

        /// <summary>카메라 또는 청취자의 실제 Transform을 I가 재바인딩한다.</summary>
        public void BindObserver(Transform observer) => _observer = observer;

        /// <summary>조사·전투 시작과 종료 시 I가 호출한다. 대화는 기존 서비스로 자동 감쇠한다.</summary>
        public void SetFocus(bool hasFocus) => _hasFocus = hasFocus;

        /// <summary>NPC 작업점의 실내 여부를 I의 공용 어댑터가 조회한다.</summary>
        public bool IsSheltered(Vector3 position) => _effects != null && _effects.IsSheltered(position);

        /// <summary>독립 검사에서도 실제 저장과 동일한 day/minute 입력을 사용한다.</summary>
        public bool EvaluateAt(int day, float minuteOfDay)
        {
            if (!_hasOwnership || !isActiveAndEnabled) return false;
            double minute = (double)day * 1440 + minuteOfDay;
            if (day < 0 || minuteOfDay < 0 || minuteOfDay >= 1440 || !_schedule.TryEvaluate(minute, out var sample))
            {
                ClearPresentation();
                return false;
            }
            Current = sample;
            _shaderState.Apply(sample, _vegetationStrength, _vegetationSpeed, _vegetationScale, _vegetationJitter, _windNoise);
            bool wasReady = IsReady;
            IsReady = true;
            long wholeMinute = (long)minute;
            if (!wasReady || _publishedMinute != wholeMinute)
            {
                _publishedMinute = wholeMinute;
                Revision++;
                OnStateChanged?.Invoke(this);
            }
            return true;
        }

        private void ClearPresentation()
        {
            Current = default;
            _shaderState.Apply(Current, _vegetationStrength, _vegetationSpeed, _vegetationScale, _vegetationJitter, _windNoise);
            if (_effects != null) _effects.StopEffects();
            if (!IsReady) return;
            IsReady = false;
            Revision++;
            OnStateChanged?.Invoke(this);
        }

        private void OnDisable()
        {
            if (!_hasOwnership) return;
            ClearPresentation();
            _shaderState.Release();
            _hasOwnership = false;
            _publishedMinute = -1;
        }
    }
}
