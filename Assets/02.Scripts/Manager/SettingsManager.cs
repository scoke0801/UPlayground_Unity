using UnityEngine;
using UnityEngine.AddressableAssets;
using UPlayGround.Data.Config;
using System;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace UPlayGround.Manager
{
    /// <summary>
    /// SettingsData(SO)를 Addressable로 로드하고 전역에서 접근 가능하게 관리.
    /// 게임 시작 시 자동으로 저장된 값을 로드하여 시스템에 반영한다.
    /// </summary>
    public class SettingsManager : BaseManager<SettingsManager>, IManager, IAsyncInitializableManager
    {
        // Addressable 키 — Inspector나 AddressableGroups에서 이 이름으로 등록
        private const string SETTINGS_DATA_KEY = "SettingsData";

        public SettingsData Data { get; private set; }
        public bool IsLoaded { get; private set; } = false;

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
            Data = null;
            IsLoaded = false;
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
    }
}
