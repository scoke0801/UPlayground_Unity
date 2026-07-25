using UnityEngine;
using UnityEngine.AddressableAssets;
using UPlayGround.Data.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UPlayGround.Manager
{
    /// <summary>
    /// SettingsData(SO)를 Addressable로 로드하고 전역에서 접근 가능하게 관리.
    /// 게임 시작 시 자동으로 저장된 값을 로드하여 시스템에 반영한다.
    /// </summary>
    public class SettingsManager : BaseManager<SettingsManager>, IManager, IAsyncInitializableManager,
        UPlayGround.UI.IUISettingsService,
        ISettingsService
    {
        // Addressable 키 — Inspector나 AddressableGroups에서 이 이름으로 등록
        private const string SETTINGS_DATA_KEY = "SettingsData";

        public SettingsData Data { get; private set; }
        public bool IsLoaded { get; private set; } = false;

        private readonly List<Vector2Int> _resolutions = new();
        private readonly List<string> _resolutionOptions = new();
        private static readonly string[] QualityOptionNames = { "낮음", "중간", "높음", "최고" };

        public IReadOnlyList<string> ResolutionOptions => _resolutionOptions;
        public IReadOnlyList<string> QualityOptions => QualityOptionNames;

        // AudioMixer는 씬 의존이 없으므로 여기서 들고 있어도 무방.
        // 직접 할당하지 않으면 SoundManager가 Addressable로 로드한 믹서를 폴백으로 사용한다.
        [SerializeField] private UnityEngine.Audio.AudioMixer _audioMixer;

        // 직접 할당된 믹서가 없으면 SoundManager가 로드한 믹서를 사용.
        private UnityEngine.Audio.AudioMixer ResolveMixer()
            => _audioMixer != null ? _audioMixer : SoundManager.Instance?.Mixer;

        public void Init()
        {
        }

        public UniTask InitializeAsync(CancellationToken cancellationToken) =>
            LoadSettingsDataAsync(cancellationToken);

        public void AfterInit() { }
        public void Dispose()
        {
            // 세션이 끝나면 적용 캐시를 비워, 다음 세션에서 같은 값이어도 다시 반영되게 한다.
            SettingsApplier.ResetAppliedCache();

            Data = null;
            IsLoaded = false;
        }

        /// <summary>
        /// 현재 Data 값을 즉시 전체 시스템(그래픽/오디오)에 적용한다. 설정 메뉴 '적용' 시 호출.
        /// 믹서는 override가 있으면 사용하고, 없으면 ResolveMixer() 폴백을 타므로
        /// UI 쪽 믹서 연결 누락으로 오디오가 재시작 전까지 반영되지 않던 문제를 방지한다.
        /// (게임플레이 감도/흔들림/타겟보정 등은 각 시스템이 Data를 라이브로 읽으므로 별도 push 불필요.)
        /// </summary>
        public void ApplyCurrentSettings(UnityEngine.Audio.AudioMixer mixerOverride = null)
        {
            if (!IsLoaded || Data == null)
                return;

            var mixer = mixerOverride != null ? mixerOverride : ResolveMixer();
            SettingsApplier.ApplyAll(Data, mixer);
        }

        public int GetCurrentResolutionOptionIndex()
        {
            if (Data == null || _resolutions.Count == 0)
                return 0;

            int index = _resolutions.FindIndex(item =>
                item.x == Data.resolutionWidth && item.y == Data.resolutionHeight);
            return index >= 0 ? index : Mathf.Clamp(Data.resolutionIndex, 0, _resolutions.Count - 1);
        }

        public void SetResolutionOption(int index)
        {
            if (Data == null || _resolutions.Count == 0)
                return;

            index = Mathf.Clamp(index, 0, _resolutions.Count - 1);
            Vector2Int resolution = _resolutions[index];
            Data.resolutionIndex = index;
            Data.resolutionWidth = resolution.x;
            Data.resolutionHeight = resolution.y;
        }

        /// <summary>
        /// 믹서가 늦게 준비된 경우(SoundManager의 Addressable 믹서) 저장된 오디오 설정을 재적용한다.
        /// SoundManager가 믹서 로드 완료 시 호출한다.
        /// </summary>
        public void ReapplyAudio()
        {
            if (!IsLoaded)
                return;

            SettingsApplier.ApplyAudio(Data, ResolveMixer());
        }

        public void OnUpdate() { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }
        public void OnSceneChanged(string sceneType) { }

        private async UniTask LoadSettingsDataAsync(CancellationToken cancellationToken)
        {
            try
            {
                Data = await AssetManager.Instance.LoadGlobalAsync<SettingsData>(
                    SETTINGS_DATA_KEY,
                    nameof(SettingsManager),
                    cancellationToken);

                // 저장된 PlayerPrefs 값을 SO에 덮어쓴 뒤 시스템에 반영
                Data.Load();
                BuildResolutionOptions();
                IsLoaded = true;
                SettingsApplier.ApplyAll(Data, ResolveMixer());

                Debug.Log("[SettingsManager] SettingsData 로드 및 초기 적용 완료");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SettingsManager] SettingsData 로드 중 예외: {e.Message}");
                throw;
            }
        }

        private void BuildResolutionOptions()
        {
            _resolutions.Clear();
            _resolutionOptions.Clear();

            foreach (Resolution resolution in Screen.resolutions)
            {
                var size = new Vector2Int(resolution.width, resolution.height);
                if (!_resolutions.Contains(size))
                    _resolutions.Add(size);
            }

            if (Data.resolutionWidth > 0 && Data.resolutionHeight > 0)
            {
                var savedSize = new Vector2Int(Data.resolutionWidth, Data.resolutionHeight);
                if (!_resolutions.Contains(savedSize))
                    _resolutions.Add(savedSize);
            }

            if (_resolutions.Count == 0)
                _resolutions.Add(new Vector2Int(Screen.width, Screen.height));

            List<Vector2Int> ordered = _resolutions
                .OrderBy(item => item.x * item.y)
                .ThenBy(item => item.x)
                .ThenBy(item => item.y)
                .ToList();
            _resolutions.Clear();
            _resolutions.AddRange(ordered);

            foreach (Vector2Int resolution in _resolutions)
                _resolutionOptions.Add($"{resolution.x} × {resolution.y}");

            int currentIndex = GetCurrentResolutionOptionIndex();
            SetResolutionOption(currentIndex);
        }
    }
}
