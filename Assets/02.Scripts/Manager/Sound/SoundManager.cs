using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Event;
using UPlayGround.Data.Sound;

namespace UPlayGround.Manager
{
    public sealed class SoundManager : BaseManager<SoundManager>, IManager, IAsyncInitializableManager,
        IUpdatableManager, ISoundService
    {
        private const string SoundDatabaseKey = "SoundDatabase";
        private const string AudioMixerKey = "AudioMixer";
        private const string BgmRoutingKey = "BgmRouting";

        [Header("Database")]
        [SerializeField] private SoundDatabaseSO _soundDatabase;
        [SerializeField] private BgmRoutingSO _bgmRouting;

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

        // 이벤트 기반 BGM override(보스전 등): 직전 평시 곡을 기억했다가 Restore 시 복귀.
        private string _bgmKeyBeforeOverride;
        private bool _bgmOverrideActive;
        private readonly List<IDisposable> _bgmEventSubscriptions = new();

        // 플레이리스트(한 씬에서 여러 곡 번갈아 재생): 한 트랙을 끝까지 재생 → 무음 간격 → 다음 트랙.
        private enum PlaylistPhase { Inactive, Playing, Gap }
        private BgmPlaylistSO _activePlaylist;
        private PlaylistPhase _playlistPhase = PlaylistPhase.Inactive;
        private int _playlistIndex = -1;
        private float _playlistTrackEndTime;   // 현재 트랙의 예상 종료 시각(Time.unscaledTime 기준)
        private float _playlistGapTimer;        // 곡 사이 남은 무음 시간(초)

        // override 진입 시 직전 플레이리스트도 기억했다가 Restore 시 복귀.
        private BgmPlaylistSO _playlistBeforeOverride;
        private int _playlistIndexBeforeOverride = -1;
        // 플레이리스트 내부 트랙 시작은 같은 key 연속 재생도 허용해야 하므로 dedup 가드를 우회한다.
        private bool _bypassBgmDedup;

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
            UniTask routingTask = LoadBgmRoutingAsync(cancellationToken);
            await UniTask.WhenAll(databaseTask, mixerTask, routingTask);
        }

        public void AfterInit()
        {
            SubscribeBgmEvents();
        }

        public void Dispose()
        {
            StopAllCoroutines();
            StopAllSounds();

            UnsubscribeBgmEvents();

            _soundDatabase = null;
            _bgmRouting = null;
            _audioMixer = null;
            _listenerTransform = null;
        }

        public void OnUpdate()
        {
            ProcessActiveSounds();
            UpdatePlaylist();
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void OnSceneChanged(string sceneType)
        {
            _listenerTransform = null;

            // 씬 전환 중 보스 override가 남아 있으면 직전 키/플레이리스트가 무의미해지므로 초기화한다.
            _bgmOverrideActive = false;
            _bgmKeyBeforeOverride = null;
            _playlistBeforeOverride = null;
            _playlistIndexBeforeOverride = -1;

            ApplySceneBgm(sceneType);
        }

        /// <summary>
        /// BgmRouting 테이블로 현재 씬/맵의 평시 BGM을 결정해 적용한다.
        /// 매칭되는 라우트가 없으면 현재 BGM을 그대로 유지한다(Loading/Boot 등에서 음악 끊김 방지).
        /// </summary>
        private void ApplySceneBgm(string sceneType)
        {
            if (_bgmRouting == null)
                return;

            string mapId = SceneManager.Instance?.CurrentMapID;
            if (!_bgmRouting.TryResolve(sceneType, mapId, out var route))
                return;

            if (route.HasPlaylist)
                PlayBgmPlaylist(route.Playlist);
            else if (route.IsStop)
                StopBgm();
            else
                PlayBgm(route.BgmKey);
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

        /// <summary>
        /// key 등록 여부만 조회한다. TryGetEntry와 달리 미등록 경고를 남기지 않는다
        /// (폴백 판정용 질의이므로 미등록이 정상 흐름이다).
        /// </summary>
        public bool HasSound(string key)
        {
            return !string.IsNullOrWhiteSpace(key)
                   && _soundDatabase != null
                   && _soundDatabase.TryGet(key, out _);
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

        /// <summary>단일 곡을 무한 반복 재생한다. 진행 중인 플레이리스트가 있으면 종료한다.</summary>
        public void PlayBgm(string key, float fadeTime = 1f)
        {
            StopPlaylist();
            PlayBgmTrack(key, fadeTime, loop: true);
        }

        /// <summary>
        /// 실제 BGM 트랙을 크로스페이드로 재생한다. 플레이리스트 진행은 loop=false로 호출한다.
        /// 반환값은 재생 시작 성공 여부(플레이리스트 advance가 실패를 감지하는 데 사용).
        /// </summary>
        private bool PlayBgmTrack(string key, float fadeTime, bool loop)
        {
            if (string.IsNullOrWhiteSpace(key))
                return false;

            // 플레이리스트 내부 트랙 시작은 같은 key 연속 재생도 의도이므로 dedup 가드를 우회한다.
            if (!_bypassBgmDedup
                && _currentBgmKey == key && _currentBgmSource != null && _currentBgmSource.isPlaying)
                return true;

            if (!TryGetEntry(key, out var entry))
                return false;

            if (entry.clip == null)
            {
                Debug.LogWarning($"[SoundManager] BGM clip이 비어 있습니다: {key}");
                return false;
            }

            EnsureRuntimeObjects();

            _nextBgmSource = _currentBgmSource == _bgmSourceA ? _bgmSourceB : _bgmSourceA;
            _nextBgmSource.clip = entry.clip;
            _nextBgmSource.loop = loop;
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

            // 비반복 트랙(플레이리스트)의 예상 종료 시각을 unscaled 타이머로 기록한다.
            // isPlaying 폴링보다 일시정지/히트스톱(timeScale 0)에서 안정적이다.
            if (!loop)
            {
                float pitch = Mathf.Max(0.01f, _nextBgmSource.pitch);
                _playlistTrackEndTime = Time.unscaledTime + entry.clip.length / pitch;
            }

            return true;
        }

        public void StopBgm(float fadeTime = 1f)
        {
            StopPlaylist();

            if (_currentBgmSource == null)
                return;

            if (_bgmFadeRoutine != null)
                StopCoroutine(_bgmFadeRoutine);

            _bgmFadeRoutine = StartCoroutine(FadeOutBgm(_currentBgmSource, fadeTime));
            _currentBgmKey = null;
        }

        /// <summary>
        /// 현재 BGM을 임시로 덮어쓴다(보스전 진입 등). 직전 곡 key를 기억해 PopBgm으로 복귀할 수 있다.
        /// 이미 override 중이면 직전 곡은 그대로 유지하고 곡만 교체한다(중첩 override는 단일 키로 평탄화).
        /// </summary>
        public void PushBgm(string key, float fadeTime = 1.5f)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            if (!_bgmOverrideActive)
            {
                // 직전 평시 곡 또는 플레이리스트를 기억해 둔다(PlayBgm이 플레이리스트를 종료하기 전에 캡처).
                _bgmKeyBeforeOverride = _currentBgmKey;
                _playlistBeforeOverride = _activePlaylist;
                _playlistIndexBeforeOverride = _playlistIndex;
                _bgmOverrideActive = true;
            }

            PlayBgm(key, fadeTime);
        }

        /// <summary>
        /// PushBgm으로 건 override를 해제하고 직전 BGM으로 복귀한다(보스전 종료 등).
        /// 직전 곡이 없었다면 정지한다. override 중이 아니면 아무 것도 하지 않는다.
        /// </summary>
        public void PopBgm(float fadeTime = 1.5f)
        {
            if (!_bgmOverrideActive)
                return;

            _bgmOverrideActive = false;
            string restoreKey = _bgmKeyBeforeOverride;
            BgmPlaylistSO restorePlaylist = _playlistBeforeOverride;
            int restoreIndex = _playlistIndexBeforeOverride;
            _bgmKeyBeforeOverride = null;
            _playlistBeforeOverride = null;
            _playlistIndexBeforeOverride = -1;

            if (restorePlaylist != null)
                ResumeBgmPlaylist(restorePlaylist, restoreIndex, fadeTime);
            else if (string.IsNullOrWhiteSpace(restoreKey))
                StopBgm(fadeTime);
            else
                PlayBgm(restoreKey, fadeTime);
        }

        /// <summary>
        /// 여러 BGM을 번갈아 재생하는 플레이리스트를 시작한다.
        /// 한 트랙을 끝까지(loop=false) 재생한 뒤, 곡 사이에 무음 간격을 두고 다음 트랙으로 넘어간다.
        /// 동일 플레이리스트가 이미 재생 중이면 무시한다(씬 재진입 시 끊김 방지).
        /// </summary>
        public void PlayBgmPlaylist(BgmPlaylistSO playlist, float? fadeTime = null)
        {
            if (playlist == null || playlist.Count == 0)
            {
                StopBgm(fadeTime ?? 1f);
                return;
            }

            if (_activePlaylist == playlist && _playlistPhase != PlaylistPhase.Inactive)
                return;

            _activePlaylist = playlist;
            _playlistIndex = -1;
            AdvancePlaylist(fadeTime ?? playlist.TrackFadeTime);
        }

        /// <summary>override 종료 후 직전 플레이리스트를 재개한다. 저장된 인덱스의 다음 트랙부터 이어간다.</summary>
        private void ResumeBgmPlaylist(BgmPlaylistSO playlist, int fromIndex, float fadeTime)
        {
            _activePlaylist = playlist;
            _playlistIndex = fromIndex;
            AdvancePlaylist(fadeTime);
        }

        /// <summary>플레이리스트의 다음 트랙을 재생한다.</summary>
        private void AdvancePlaylist(float fadeTime)
        {
            if (_activePlaylist == null)
                return;

            _playlistIndex = _activePlaylist.GetNextIndex(_playlistIndex);
            string key = _activePlaylist.GetKey(_playlistIndex);

            _bypassBgmDedup = true;
            bool started = PlayBgmTrack(key, fadeTime, loop: false);
            _bypassBgmDedup = false;

            if (started)
            {
                _playlistPhase = PlaylistPhase.Playing;
            }
            else
            {
                // 잘못된 key 등으로 재생 실패: 다음 트랙으로 즉시 넘어가기보다 짧은 무음 후 재시도(무한루프 방지).
                _playlistPhase = PlaylistPhase.Gap;
                _playlistGapTimer = 1f;
            }
        }

        /// <summary>플레이리스트 진행 상태머신. OnUpdate에서 매 프레임 폴링한다.</summary>
        private void UpdatePlaylist()
        {
            if (_activePlaylist == null)
                return;

            switch (_playlistPhase)
            {
                case PlaylistPhase.Playing:
                    // 트랙 예상 종료 시각(항상 fade-in 이후)이 지나면 다음 단계로.
                    // 주의: fade 코루틴 핸들(_bgmFadeRoutine)을 게이트로 쓰지 않는다 —
                    // CrossFadeBgm의 fadeTime<=0 분기는 동기 완료되며 자신이 찍은 null이
                    // StartCoroutine 반환 핸들로 덮어써져 non-null로 남기 때문(TrackFadeTime=0에서 영구 정지 유발).
                    if (Time.unscaledTime >= _playlistTrackEndTime)
                    {
                        float gap = _activePlaylist.GetRandomGap();
                        if (gap > 0f)
                        {
                            // 곡 사이 무음 간격(상용 게임 탐험 BGM 패턴).
                            _playlistPhase = PlaylistPhase.Gap;
                            _playlistGapTimer = gap;
                        }
                        else
                        {
                            // gap==0: 곧바로 다음 트랙으로 크로스페이드(1프레임 갭 — sample-accurate 아님).
                            AdvancePlaylist(_activePlaylist.TrackFadeTime);
                        }
                    }
                    break;

                case PlaylistPhase.Gap:
                    _playlistGapTimer -= Time.unscaledDeltaTime;
                    if (_playlistGapTimer <= 0f)
                        AdvancePlaylist(_activePlaylist.TrackFadeTime);
                    break;
            }
        }

        /// <summary>플레이리스트 진행을 중단한다(단일 곡 재생/정지/씬 전환 시). 현재 재생 중인 소스는 호출부가 처리.</summary>
        private void StopPlaylist()
        {
            _activePlaylist = null;
            _playlistPhase = PlaylistPhase.Inactive;
            _playlistIndex = -1;
            _playlistGapTimer = 0f;
        }

        private void SubscribeBgmEvents()
        {
            if (_bgmEventSubscriptions.Count > 0)
                return;

            if (EventManager.Instance == null)
            {
                // 부트 순서상 EventManager가 SoundManager보다 먼저 등록되므로 정상 경로에선 도달하지 않는다.
                // 순서가 바뀌면 BGM 이벤트(Change/Override/Restore/Stop)가 영구 무반응이 되므로 명시적으로 경고한다.
                Debug.LogWarning(
                    "[SoundManager] EventManager가 준비되지 않아 BGM 이벤트 구독을 건너뜁니다. " +
                    "BGM 전환 이벤트가 동작하지 않습니다(매니저 등록 순서 확인 필요).");
                return;
            }

            _bgmEventSubscriptions.Add(EventManager.Instance.Subscribe<BgmEvent, BgmRequestData>(
                BgmEvent.Change, OnBgmChangeRequested, EventSubscriptionScope.Global));
            _bgmEventSubscriptions.Add(EventManager.Instance.Subscribe<BgmEvent, BgmRequestData>(
                BgmEvent.Override, OnBgmOverrideRequested, EventSubscriptionScope.Global));
            _bgmEventSubscriptions.Add(EventManager.Instance.Subscribe<BgmEvent, BgmRequestData>(
                BgmEvent.Restore, OnBgmRestoreRequested, EventSubscriptionScope.Global));
            _bgmEventSubscriptions.Add(EventManager.Instance.Subscribe<BgmEvent, BgmRequestData>(
                BgmEvent.Stop, OnBgmStopRequested, EventSubscriptionScope.Global));
        }

        private void UnsubscribeBgmEvents()
        {
            foreach (var subscription in _bgmEventSubscriptions)
                subscription?.Dispose();

            _bgmEventSubscriptions.Clear();
        }

        private void OnBgmChangeRequested(BgmRequestData data)
        {
            if (data == null) return;

            if (data.playlist != null)
                PlayBgmPlaylist(data.playlist, data.fadeTime);
            else
                PlayBgm(data.bgmKey, data.fadeTime);
        }

        private void OnBgmOverrideRequested(BgmRequestData data)
        {
            if (data == null) return;
            PushBgm(data.bgmKey, data.fadeTime);
        }

        private void OnBgmRestoreRequested(BgmRequestData data)
        {
            PopBgm(data?.fadeTime ?? 1.5f);
        }

        private void OnBgmStopRequested(BgmRequestData data)
        {
            _bgmOverrideActive = false;
            _bgmKeyBeforeOverride = null;
            StopBgm(data?.fadeTime ?? 1.5f);
        }

        private async UniTask LoadBgmRoutingAsync(CancellationToken cancellationToken)
        {
            // 인스펙터에서 직접 할당했다면 Addressable 로드를 건너뛴다(직접 할당 우선).
            if (_bgmRouting != null)
                return;

            try
            {
                _bgmRouting = await AssetManager.Instance.LoadGlobalAsync<BgmRoutingSO>(
                    BgmRoutingKey,
                    nameof(SoundManager),
                    cancellationToken);

                if (_bgmRouting == null)
                    Debug.LogWarning($"[SoundManager] '{BgmRoutingKey}' Addressable을 찾을 수 없습니다. 씬 기반 BGM 자동 전환은 비활성화됩니다(PlayBgm/이벤트는 정상 동작).");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SoundManager] BgmRouting 로드 실패: {e.Message}");
            }
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

        private bool TryGetEntry(string key, out SoundEntrySO entry)
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

        private void PlayEntry(SoundEntrySO entry, Vector3? position, float volumeScale)
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

        private bool IsAudible(SoundEntrySO entry, Vector3 position)
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

        private bool CanPlayByCooldown(SoundEntrySO entry)
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

        private bool CanPlayBySimultaneousLimit(SoundEntrySO entry)
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

            StopPlaylist();
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

        private static float RandomPitch(SoundEntrySO entry)
        {
            float min = Mathf.Min(entry.pitchMin, entry.pitchMax);
            float max = Mathf.Max(entry.pitchMin, entry.pitchMax);

            if (Mathf.Approximately(min, max))
                return Mathf.Approximately(min, 0f) ? 1f : min;

            return UnityEngine.Random.Range(min, max);
        }
    }
}
