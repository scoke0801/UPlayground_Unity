using UnityEngine;
using UnityEngine.AddressableAssets;
using UPlayGround.Data.Config;

namespace UPlayGround.Manager
{
    /// <summary>
    /// SettingsData(SO)를 Addressable로 로드하고 전역에서 접근 가능하게 관리.
    /// 게임 시작 시 자동으로 저장된 값을 로드하여 시스템에 반영한다.
    /// </summary>
    public class SettingsManager : BaseManager<SettingsManager>, IManager
    {
        // Addressable 키 — Inspector나 AddressableGroups에서 이 이름으로 등록
        private const string SETTINGS_DATA_KEY = "SettingsData";

        public SettingsData Data { get; private set; }
        public bool IsLoaded { get; private set; } = false;

        // AudioMixer는 씬 의존이 없으므로 여기서 들고 있어도 무방.
        // 없으면 오디오 반영만 스킵된다.
        [SerializeField] private UnityEngine.Audio.AudioMixer _audioMixer;

        public void Init()
        {
            LoadSettingsDataAsync();
        }

        public void AfterInit() { }
        public void Dispose() { }
        public void OnUpdate() { }
        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }
        public void OnSceneChanged(string sceneType) { }

        private async void LoadSettingsDataAsync()
        {
            var handle = Addressables.LoadAssetAsync<SettingsData>(SETTINGS_DATA_KEY);
            try
            {
                Data = await handle.Task;

                if (Data == null)
                {
                    Debug.LogError($"[SettingsManager] '{SETTINGS_DATA_KEY}' Addressable 로드 실패: null 반환");
                    return;
                }

                // 저장된 PlayerPrefs 값을 SO에 덮어쓴 뒤 시스템에 반영
                Data.Load();
                SettingsApplier.ApplyAll(Data, _audioMixer);

                IsLoaded = true;
                Debug.Log("[SettingsManager] SettingsData 로드 및 초기 적용 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SettingsManager] SettingsData 로드 중 예외: {e.Message}");
            }
        }
    }
}
