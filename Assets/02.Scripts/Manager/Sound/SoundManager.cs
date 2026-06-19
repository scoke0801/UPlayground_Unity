using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UPlayGround.Data.Sound;

namespace UPlayGround.Manager
{
    public sealed class SoundManager : BaseManager<SoundManager>, IManager, IAsyncInitializableManager,
        IUpdatableManager
    {
        private const string SoundDatabaseKey = "SoundDatabase";
        private const string AudioMixerKey = "AudioMixer";

        [Header("Database")]
        [SerializeField] private SoundDatabaseSO _soundDatabase;

        // 믹서 그룹은 Addressable로 로드한 AudioMixer에서 이름으로 자동 매핑한다(아래 LoadAudioMixerAsync).
        // 씬/프리팹 인스턴스에서 직접 할당하면 그 값이 우선되고 Addressable 로드는 건너뛴다(UIManager._uiRootPrefab 폴백과 동일).
        [Header("Mixer Groups (선택: 직접 할당 시 Addressable 로드보다 우선)")]
        [SerializeField] private AudioMixer _audioMixer;
        [SerializeField] private AudioMixerGroup _masterGroup;
        [SerializeField] private AudioMixerGroup _bgmGroup;
        [SerializeField] private AudioMixerGroup _sfxGroup;
        [SerializeField] private AudioMixerGroup _uiGroup;
        [SerializeField] private AudioMixerGroup _voiceGroup;
        [SerializeField] private AudioMixerGroup _ambienceGroup;

        [Header("Pool")]
        [SerializeField] private int _initial2DSourceCount = 16;
        [SerializeField] private int _initial3DSourceCount = 24;

        private AudioSourcePool _source2DPool;
        private AudioSourcePool _source3DPool;
        private readonly List<ActiveSoundHandle> _activeSounds = new();
        private readonly Dictionary<string, float> _lastPlayTimes = new();
        private readonly Dictionary<string, int> _activeCounts = new();
        private readonly HashSet<string> _missingEntryWarnings = new();

        private Transform _listenerTransform;
        private AudioSource _bgmSourceA;
        private AudioSource _bgmSourceB;
        private AudioSource _currentBgmSource;
        private AudioSource _nextBgmSource;
        private Coroutine _bgmFadeRoutine;
        private string _currentBgmKey;
        private bool _databaseNotReadyWarningLogged;

        public bool IsDatabaseLoaded => _soundDatabase != null;

        /// <summary>로드된 AudioMixer. SettingsManager 등에서 믹서 볼륨 제어에 재사용할 수 있다.</summary>
        public AudioMixer Mixer => _audioMixer;

        public void Init()
        {
            EnsureRuntimeObjects();
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            UniTask databaseTask = LoadSoundDatabaseAsync(cancellationToken);
            UniTask mixerTask = LoadAudioMixerAsync(cancellationToken);
            await UniTask.WhenAll(databaseTask, mixerTask);
        }

        public void AfterInit() { }

        public void Dispose()
        {
            StopAllCoroutines();
            StopAllSounds();

            _soundDatabase = null;
            _audioMixer = null;
            _listenerTransform = null;
        }

        public void OnUpdate()
        {
            ProcessActiveSounds();
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void OnSceneChanged(string sceneType)
        {
            _listenerTransform = null;
        }

        public void Play(string key, Vector3? position = null, float volumeScale = 1f)
        {
            if (!TryGetEntry(key, out var entry))
                return;

            PlayEntry(entry, position, volumeScale);
        }

        public void PlaySfx(string key, Vector3 position, float volumeScale = 1f)
        {
            Play(key, position, volumeScale);
        }

        public void PlayUi(string key, float volumeScale = 1f)
        {
            Play(key, null, volumeScale);
        }

        public void PlayVoice(string key, float volumeScale = 1f)
        {
            Play(key, null, volumeScale);
        }

        public void PlayClip(AudioClip clip, SoundBusType bus, Vector3? position = null, float volumeScale = 1f)
        {
            if (clip == null)
                return;

            EnsureRuntimeObjects();

            bool is3D = position.HasValue;
            var pool = is3D ? _source3DPool : _source2DPool;
            var source = pool.Rent();

            source.clip = clip;
            source.volume = Mathf.Clamp01(volumeScale);
            source.pitch = 1f;
            source.priority = 128;
            source.loop = false;
            source.outputAudioMixerGroup = GetMixerGroup(bus);
            source.ignoreListenerPause = bus == SoundBusType.UI || bus == SoundBusType.BGM;

            ApplyDistance(source, is3D, position, SoundDistanceMode.Logarithmic3D, 1.5f, 24f, null);
            source.Play();

            _activeSounds.Add(new ActiveSoundHandle
            {
                source = source,
                pool = pool,
                key = null,
                bus = bus
            });
        }

        public void PlayBgm(string key, float fadeTime = 1f)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (_currentBgmKey == key && _currentBgmSource != null && _currentBgmSource.isPlaying)
                return;

            if (!TryGetEntry(key, out var entry))
                return;

            if (entry.clip == null)
            {
                Debug.LogWarning($"[SoundManager] BGM clip이 비어 있습니다: {key}");
                return;
            }

            EnsureRuntimeObjects();

            _nextBgmSource = _currentBgmSource == _bgmSourceA ? _bgmSourceB : _bgmSourceA;
            _nextBgmSource.clip = entry.clip;
            _nextBgmSource.loop = true;
            _nextBgmSource.outputAudioMixerGroup = GetMixerGroup(SoundBusType.BGM);
            _nextBgmSource.ignoreListenerPause = true;
            _nextBgmSource.spatialBlend = 0f;
            _nextBgmSource.pitch = RandomPitch(entry);
            _nextBgmSource.volume = 0f;
            _nextBgmSource.Play();

            if (_bgmFadeRoutine != null)
                StopCoroutine(_bgmFadeRoutine);

            _bgmFadeRoutine = StartCoroutine(CrossFadeBgm(_currentBgmSource, _nextBgmSource, entry.volume, fadeTime));
            _currentBgmSource = _nextBgmSource;
            _currentBgmKey = key;
        }

        public void StopBgm(float fadeTime = 1f)
        {
            if (_currentBgmSource == null)
                return;

            if (_bgmFadeRoutine != null)
                StopCoroutine(_bgmFadeRoutine);

            _bgmFadeRoutine = StartCoroutine(FadeOutBgm(_currentBgmSource, fadeTime));
            _currentBgmKey = null;
        }

        private async UniTask LoadSoundDatabaseAsync(CancellationToken cancellationToken)
        {
            if (_soundDatabase != null)
            {
                _soundDatabase.Initialize();
                return;
            }

            try
            {
                _soundDatabase = await AssetManager.Instance.LoadGlobalAsync<SoundDatabaseSO>(
                    SoundDatabaseKey,
                    nameof(SoundManager),
                    cancellationToken);

                if (_soundDatabase == null)
                {
                    Debug.LogWarning($"[SoundManager] '{SoundDatabaseKey}' Addressable을 찾을 수 없습니다. key 기반 재생은 비활성화됩니다.");
                    return;
                }

                _soundDatabase.Initialize();
                Debug.Log("[SoundManager] SoundDatabase 로드 완료");
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SoundManager] SoundDatabase 로드 실패: {e.Message}");
            }
        }

        private async UniTask LoadAudioMixerAsync(CancellationToken cancellationToken)
        {
            // 씬/프리팹에서 그룹을 직접 할당했다면 Addressable 로드를 건너뛴다(직접 할당 우선).
            if (HasSerializedMixerGroups())
            {
                // 그룹만 할당하고 믹서 필드는 비운 경우에도 .Mixer가 유효하도록 그룹에서 역참조한다.
                if (_audioMixer == null)
                    _audioMixer = (_masterGroup ?? _sfxGroup ?? _bgmGroup)?.audioMixer;

                SettingsManager.Instance?.ReapplyAudio();
                return;
            }

            try
            {
                _audioMixer = await AssetManager.Instance.LoadGlobalAsync<AudioMixer>(
                    AudioMixerKey,
                    nameof(SoundManager),
                    cancellationToken);

                if (_audioMixer == null)
                {
                    Debug.LogWarning($"[SoundManager] '{AudioMixerKey}' AudioMixer를 찾을 수 없습니다. 믹서 버스 라우팅이 비활성화됩니다(기본 출력으로 재생).");
                    return;
                }

                ResolveMixerGroupsFromMixer();

                // 믹서가 SettingsManager보다 늦게 준비되는 경우를 대비해 저장된 볼륨 설정을 재적용한다.
                SettingsManager.Instance?.ReapplyAudio();
                Debug.Log("[SoundManager] AudioMixer 로드 및 그룹 매핑 완료");
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[SoundManager] AudioMixer 로드 실패: {e.Message}");
            }
        }

        private bool HasSerializedMixerGroups()
        {
            return _masterGroup != null || _bgmGroup != null || _sfxGroup != null
                   || _uiGroup != null || _voiceGroup != null || _ambienceGroup != null;
        }

        // AudioMixerGroup의 이름(= SoundBusType enum 이름)으로 그룹을 매핑한다.
        // 믹서에 Master/BGM/SFX/UI/Voice/Ambience 그룹이 동일 이름으로 존재해야 한다.
        private void ResolveMixerGroupsFromMixer()
        {
            _masterGroup   = FindMixerGroup(SoundBusType.Master.ToString());
            _bgmGroup      = FindMixerGroup(SoundBusType.BGM.ToString());
            _sfxGroup      = FindMixerGroup(SoundBusType.SFX.ToString());
            _uiGroup       = FindMixerGroup(SoundBusType.UI.ToString());
            _voiceGroup    = FindMixerGroup(SoundBusType.Voice.ToString());
            _ambienceGroup = FindMixerGroup(SoundBusType.Ambience.ToString());
        }

        private AudioMixerGroup FindMixerGroup(string groupName)
        {
            // FindMatchingGroups는 경로 부분 일치이므로 정확한 이름 매칭을 우선 선택한다.
            var groups = _audioMixer.FindMatchingGroups(groupName);
            if (groups == null || groups.Length == 0)
                return null;

            for (int i = 0; i < groups.Length; i++)
                if (groups[i] != null && groups[i].name == groupName)
                    return groups[i];

            return groups[0];
        }

        private bool TryGetEntry(string key, out SoundEntry entry)
        {
            if (_soundDatabase == null)
            {
                if (!_databaseNotReadyWarningLogged)
                {
                    Debug.LogWarning($"[SoundManager] SoundDatabase가 아직 준비되지 않았습니다. key 기반 재생을 건너뜁니다. 최초 key: {key}");
                    _databaseNotReadyWarningLogged = true;
                }

                entry = null;
                return false;
            }

            if (!_soundDatabase.TryGet(key, out entry))
            {
                if (_missingEntryWarnings.Add(key))
                    Debug.LogWarning($"[SoundManager] 사운드 key를 찾을 수 없습니다: {key}");

                return false;
            }

            return true;
        }

        private void PlayEntry(SoundEntry entry, Vector3? position, float volumeScale)
        {
            if (entry == null || entry.clip == null)
                return;

            bool is3D = entry.distanceMode != SoundDistanceMode.None2D && position.HasValue;

            if (is3D && entry.preCullByMaxDistance && !IsAudible(entry, position.Value))
                return;

            if (!CanPlayByCooldown(entry))
                return;

            if (!CanPlayBySimultaneousLimit(entry))
                return;

            EnsureRuntimeObjects();

            var pool = is3D ? _source3DPool : _source2DPool;
            var source = pool.Rent();

            source.clip = entry.clip;
            source.volume = Mathf.Clamp01(entry.volume * volumeScale);
            source.pitch = RandomPitch(entry);
            source.priority = entry.priority;
            source.loop = false;
            source.outputAudioMixerGroup = GetMixerGroup(entry.bus);
            source.ignoreListenerPause = entry.bus == SoundBusType.UI || entry.bus == SoundBusType.BGM;

            ApplyDistance(source, is3D, position, entry.distanceMode, entry.minDistance, entry.maxDistance, entry.customRolloff);
            source.Play();

            RegisterPlayback(entry.key);
            _activeSounds.Add(new ActiveSoundHandle
            {
                source = source,
                pool = pool,
                key = entry.key,
                bus = entry.bus
            });
        }

        private void EnsureRuntimeObjects()
        {
            if (_source2DPool != null && _source3DPool != null)
                return;

            var poolRoot = new GameObject("Sound Source Pools").transform;
            poolRoot.SetParent(transform, false);

            _source2DPool = new AudioSourcePool(poolRoot, "Pooled 2D AudioSource", Mathf.Max(1, _initial2DSourceCount));
            _source3DPool = new AudioSourcePool(poolRoot, "Pooled 3D AudioSource", Mathf.Max(1, _initial3DSourceCount));

            _bgmSourceA = CreateBgmSource("BGM AudioSource A");
            _bgmSourceB = CreateBgmSource("BGM AudioSource B");
            _currentBgmSource = _bgmSourceA;
        }

        private AudioSource CreateBgmSource(string sourceName)
        {
            var obj = new GameObject(sourceName);
            obj.transform.SetParent(transform, false);

            var source = obj.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f;
            source.priority = 0;
            source.outputAudioMixerGroup = GetMixerGroup(SoundBusType.BGM);
            source.ignoreListenerPause = true;
            return source;
        }

        private void ApplyDistance(
            AudioSource source,
            bool is3D,
            Vector3? position,
            SoundDistanceMode mode,
            float minDistance,
            float maxDistance,
            AnimationCurve customRolloff)
        {
            source.spatialBlend = is3D ? 1f : 0f;
            source.dopplerLevel = 0f;

            if (!is3D)
            {
                source.transform.localPosition = Vector3.zero;
                return;
            }

            source.transform.position = position.Value;
            source.minDistance = Mathf.Max(0.01f, minDistance);
            source.maxDistance = Mathf.Max(source.minDistance, maxDistance);

            switch (mode)
            {
                case SoundDistanceMode.Linear3D:
                    source.rolloffMode = AudioRolloffMode.Linear;
                    break;
                case SoundDistanceMode.Custom3D:
                    source.rolloffMode = AudioRolloffMode.Custom;
                    if (customRolloff != null && customRolloff.length > 0)
                        source.SetCustomCurve(AudioSourceCurveType.CustomRolloff, customRolloff);
                    break;
                default:
                    source.rolloffMode = AudioRolloffMode.Logarithmic;
                    break;
            }
        }

        private bool IsAudible(SoundEntry entry, Vector3 position)
        {
            var listener = GetAudioListenerTransform();
            if (listener == null)
                return true;

            float maxDistance = Mathf.Max(entry.minDistance, entry.maxDistance);
            return (listener.position - position).sqrMagnitude <= maxDistance * maxDistance;
        }

        private Transform GetAudioListenerTransform()
        {
            if (_listenerTransform != null)
                return _listenerTransform;

            var listener = FindFirstObjectByType<AudioListener>();
            _listenerTransform = listener != null ? listener.transform : null;
            return _listenerTransform;
        }

        private bool CanPlayByCooldown(SoundEntry entry)
        {
            if (entry.cooldown <= 0f || string.IsNullOrWhiteSpace(entry.key))
                return true;

            if (_lastPlayTimes.TryGetValue(entry.key, out float lastPlayTime))
            {
                if (Time.unscaledTime - lastPlayTime < entry.cooldown)
                    return false;
            }

            return true;
        }

        private bool CanPlayBySimultaneousLimit(SoundEntry entry)
        {
            if (entry.maxSimultaneous <= 0 || string.IsNullOrWhiteSpace(entry.key))
                return true;

            return !_activeCounts.TryGetValue(entry.key, out int count) || count < entry.maxSimultaneous;
        }

        private void RegisterPlayback(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            _lastPlayTimes[key] = Time.unscaledTime;
            _activeCounts.TryGetValue(key, out int count);
            _activeCounts[key] = count + 1;
        }

        private void UnregisterPlayback(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (!_activeCounts.TryGetValue(key, out int count))
                return;

            count--;
            if (count <= 0)
                _activeCounts.Remove(key);
            else
                _activeCounts[key] = count;
        }

        private void ProcessActiveSounds()
        {
            for (int i = _activeSounds.Count - 1; i >= 0; i--)
            {
                var active = _activeSounds[i];
                if (active.source != null && active.source.isPlaying)
                    continue;

                UnregisterPlayback(active.key);
                active.pool?.Return(active.source);
                _activeSounds.RemoveAt(i);
            }
        }

        private void StopAllSounds()
        {
            for (int i = _activeSounds.Count - 1; i >= 0; i--)
            {
                var active = _activeSounds[i];
                UnregisterPlayback(active.key);
                active.pool?.Return(active.source);
            }

            _activeSounds.Clear();
            _activeCounts.Clear();
            _lastPlayTimes.Clear();

            if (_bgmSourceA != null) _bgmSourceA.Stop();
            if (_bgmSourceB != null) _bgmSourceB.Stop();
            _currentBgmKey = null;
        }

        private IEnumerator CrossFadeBgm(AudioSource from, AudioSource to, float targetVolume, float fadeTime)
        {
            targetVolume = Mathf.Clamp01(targetVolume);
            fadeTime = Mathf.Max(0f, fadeTime);

            float fromStartVolume = from != null ? from.volume : 0f;

            if (fadeTime <= 0f)
            {
                if (from != null)
                {
                    from.Stop();
                    from.volume = 0f;
                }

                to.volume = targetVolume;
                _bgmFadeRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeTime);

                if (from != null)
                    from.volume = Mathf.Lerp(fromStartVolume, 0f, t);
                to.volume = Mathf.Lerp(0f, targetVolume, t);

                yield return null;
            }

            if (from != null)
            {
                from.Stop();
                from.volume = 0f;
            }

            to.volume = targetVolume;
            _bgmFadeRoutine = null;
        }

        private IEnumerator FadeOutBgm(AudioSource source, float fadeTime)
        {
            fadeTime = Mathf.Max(0f, fadeTime);
            float startVolume = source.volume;

            if (fadeTime <= 0f)
            {
                source.Stop();
                source.volume = 0f;
                _bgmFadeRoutine = null;
                yield break;
            }

            float elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / fadeTime);
                source.volume = Mathf.Lerp(startVolume, 0f, t);
                yield return null;
            }

            source.Stop();
            source.volume = 0f;
            _bgmFadeRoutine = null;
        }

        private AudioMixerGroup GetMixerGroup(SoundBusType bus)
        {
            return bus switch
            {
                SoundBusType.BGM => _bgmGroup != null ? _bgmGroup : _masterGroup,
                SoundBusType.SFX => _sfxGroup != null ? _sfxGroup : _masterGroup,
                SoundBusType.UI => _uiGroup != null ? _uiGroup : (_sfxGroup != null ? _sfxGroup : _masterGroup),
                SoundBusType.Voice => _voiceGroup != null ? _voiceGroup : _masterGroup,
                SoundBusType.Ambience => _ambienceGroup != null ? _ambienceGroup : _masterGroup,
                _ => _masterGroup
            };
        }

        private static float RandomPitch(SoundEntry entry)
        {
            float min = Mathf.Min(entry.pitchMin, entry.pitchMax);
            float max = Mathf.Max(entry.pitchMin, entry.pitchMax);

            if (Mathf.Approximately(min, max))
                return Mathf.Approximately(min, 0f) ? 1f : min;

            return Random.Range(min, max);
        }
    }
}
