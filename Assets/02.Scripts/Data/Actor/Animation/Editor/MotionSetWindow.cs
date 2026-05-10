using UnityEditor;
using UnityEngine;
using Animancer;
using UPlayGround.Data.Event;
using UPlayGround.Data.Actor.Animation;
using UPlayGround.Data.EnumType;
using UPlayGround.Debugging;
using UPlayGround.MovementController;
using UPlayGround.Component;
using UPlayGround.InputDefine;
using UPlayGround.Manager;
using ActorAnimatorType = UPlayGround.Animation.ActorAnimator;
using PlayerActorAnimatorType = UPlayGround.Animation.PlayerActorAnimator;

namespace UPlayGround.Animation.Editor
{
    public class MotionSetEditorWindow : EditorWindow
    {
        MotionSetAsset  _asset;
        ActorAnimationMotionSet _actorAnimationSet;
        PlayerActorAnimationMotionSet _playerActorAnimationSet;
        WeaponType      _selectedPlayerWeaponType = WeaponType.NoWeapon;
        AnimKey         _selectedActorMotionKey = AnimKey.None;
        MotionSetDrawer _drawer;
        Vector2         _scrollPos;
        Vector2         _actorMotionListScroll;
        string          _actorMotionSearch = "";
        
        // 테스트 씬 설정
        string          _testScenePath = "Assets/01.Scenes/Test/MotionTestMap.unity"; // 기본 경로
        string          _testActorName = "Player"; // 기본 액터 이름
        public AnimationClip   _idleAnimation; // 기본 Idle 애니메이션
        
        // 플레이 관련
        GameObject      _targetActor;
        AnimancerComponent _animancer;
        bool            _isPlaying;
        bool            _isPaused;
        bool            _isLooping;
        float           _playbackTime;
        float           _previousTime;
        float           _playbackSpeed = 1f;
        float           _startTime     = 0f;
        float           _endTime       = -1f; // -1 = 전체 길이 사용
        int             _currentMotionIndex = -1; // 현재 재생 중인 모션 인덱스 (전환 감지용)
        bool            _isMotionToolInputLocked;
        InputLayer      _previousInputLayerBeforeMotionTool = InputLayer.Level_0;
        PlayerActor     _suppressedPlayerActor;

        // _targetActor 가 바뀔 때만 GetComponent 재실행하기 위한 캐시
        GameObject       _cachedActorKey;
        PlayerActor      _cachedPlayerActor;
        PlayerEquipment  _cachedPlayerEquipment;
        
        // 이벤트 재생 관리
        System.Collections.Generic.HashSet<MotionEventBase> _executedEvents;   
        System.Collections.Generic.HashSet<MotionEventBase> _activeEvents;
        readonly System.Collections.Generic.List<string> _eventLog = new System.Collections.Generic.List<string>();
        bool _showSceneEventOverlay = true;
        bool _autoAttachDebugOverlay = true;

        // 캐릭터 모델 전환 (Player 전용)
        PlayerSwapBehaviour _playerSwapBehaviour;
        CharacterActorType  _selectedCharacterType = CharacterActorType.None;
        System.Collections.Generic.List<CharacterActorType> _availableCharacterTypes;
        string[]            _characterTypeNames;

        // 테스트 액터 레지스트리
        enum TestActorMode { Player, Other }
        TestActorMode        _testActorMode = TestActorMode.Player;
        GameObject           _scenePlayer;
        MotionTestRegistrySO _testRegistry;
        int                  _selectedRegistryIndex = -1;
        GameObject           _spawnedTestActor;
        string[]             _registryNames;

        // 임시 MotionSet (에셋이 없을 때)
        bool            _useTemporarySet;
        MotionSet       _temporarySet;

        [MenuItem("UPlayGround/Character/Actor/애니메이션 에디터")]
        static void OpenWindow()
        {
            var window = GetWindow<MotionSetEditorWindow>();
            window.titleContent = new GUIContent("애니메이션 에디터");
            window.minSize      = new Vector2(600, 400);
            window.Show();
        }

        public static void Open(MotionSetAsset asset)
        {
            var window = GetWindow<MotionSetEditorWindow>();
            window.titleContent = new GUIContent("애니메이션 에디터");
            window.minSize      = new Vector2(600, 400);
            window.Show();
            window.SetAsset(asset);
            window._useTemporarySet = false;
        }

        public static void Open(ActorAnimationMotionSet actorAnimationSet)
        {
            var window = GetWindow<MotionSetEditorWindow>();
            window.titleContent = new GUIContent("애니메이션 에디터");
            window.minSize      = new Vector2(600, 400);
            window.Show();
            window.SetActorAnimationSet(actorAnimationSet);
        }

        public static void Open(PlayerActorAnimationMotionSet playerActorAnimationSet)
        {
            var window = GetWindow<MotionSetEditorWindow>();
            window.titleContent = new GUIContent("애니메이션 에디터");
            window.minSize      = new Vector2(600, 400);
            window.Show();
            window.SetPlayerActorAnimationSet(playerActorAnimationSet);
        }

        public static void Open(ActorAnimationMotionSet actorAnimationSet, AnimKey key, MotionSetAsset asset)
        {
            var window = GetWindow<MotionSetEditorWindow>();
            window.titleContent = new GUIContent("애니메이션 에디터");
            window.minSize      = new Vector2(600, 400);
            window.Show();
            window.SetActorAnimationSet(actorAnimationSet);
            window._selectedActorMotionKey = key;
            window.SetAsset(asset);
            window._useTemporarySet = false;
        }

        // ⑤ EditorPrefs 키
        const string PREFS_ZOOM        = "MotionSetWindow_Zoom";
        const string PREFS_SCROLL      = "MotionSetWindow_ScrollX";
        const string PREFS_SCENE_PATH  = "MotionSetWindow_ScenePath";
        const string PREFS_ACTOR_NAME  = "MotionSetWindow_ActorName";
        const string PREFS_SHOW_FRAMES = "MotionSetWindow_ShowFrames";
        const string PREFS_FPS         = "MotionSetWindow_Fps";
        const string PREFS_SPEED       = "MotionSetWindow_Speed";
        const string PREFS_LOOP        = "MotionSetWindow_Loop";
        const string PREFS_EVENT_SCENE_OVERLAY = "MotionSetWindow_EventSceneOverlay";
        const string PREFS_EVENT_AUTO_ATTACH   = "MotionSetWindow_EventAutoAttach";
        const string PREFS_REGISTRY_PATH       = "MotionSetWindow_RegistryPath";
        const string PREFS_REGISTRY_IDX        = "MotionSetWindow_RegistryIdx";
        const string PREFS_TEST_MODE           = "MotionSetWindow_TestMode";
        const string PREFS_PLAYER_SET_PATH     = "MotionSetWindow_PlayerSetPath";
        const string PREFS_PLAYER_WEAPON       = "MotionSetWindow_PlayerWeapon";

        void OnEnable()
        {
            _drawer = new MotionSetDrawer(() => _asset, Repaint, OnSelectedMotionChanged);

            // ⑤ EditorPrefs 복원
            LoadEditorPrefs();

            // Selection이 MotionSetAsset이면 자동 바인딩
            TryBindFromSelection();

            // 플레이 업데이트 등록
            EditorApplication.update += OnEditorUpdate;

            // 플레이 모드 변경 감지
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            SceneView.duringSceneGui += OnSceneGUI;
        }

        void OnDisable()
        {
            // ⑤ EditorPrefs 저장
            SaveEditorPrefs();

            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            SceneView.duringSceneGui -= OnSceneGUI;
            ReleaseMotionToolInputLock();
            StopPlayback();
        }

        // ⑤ 상태 저장
        void SaveEditorPrefs()
        {
            if (_drawer != null)
            {
                EditorPrefs.SetFloat(PREFS_ZOOM,        _drawer.zoom);
                EditorPrefs.SetFloat(PREFS_SCROLL,      _drawer.scrollX);
                EditorPrefs.SetBool (PREFS_SHOW_FRAMES, _drawer.showFrames);
                EditorPrefs.SetInt  (PREFS_FPS,         _drawer.fps);
            }
            EditorPrefs.SetString(PREFS_SCENE_PATH, _testScenePath);
            EditorPrefs.SetString(PREFS_ACTOR_NAME, _testActorName);
            EditorPrefs.SetFloat (PREFS_SPEED,      _playbackSpeed);
            EditorPrefs.SetBool  (PREFS_LOOP,       _isLooping);
            EditorPrefs.SetBool  (PREFS_EVENT_SCENE_OVERLAY, _showSceneEventOverlay);
            EditorPrefs.SetBool  (PREFS_EVENT_AUTO_ATTACH,   _autoAttachDebugOverlay);
            if (_testRegistry != null)
                EditorPrefs.SetString(PREFS_REGISTRY_PATH, AssetDatabase.GetAssetPath(_testRegistry));
            EditorPrefs.SetInt(PREFS_REGISTRY_IDX, _selectedRegistryIndex);
            EditorPrefs.SetInt(PREFS_TEST_MODE, (int)_testActorMode);
            if (_playerActorAnimationSet != null)
                EditorPrefs.SetString(PREFS_PLAYER_SET_PATH, AssetDatabase.GetAssetPath(_playerActorAnimationSet));
            EditorPrefs.SetInt(PREFS_PLAYER_WEAPON, (int)_selectedPlayerWeaponType);
        }

        // ⑤ 상태 복원
        void LoadEditorPrefs()
        {
            if (_drawer != null)
            {
                _drawer.zoom        = EditorPrefs.GetFloat(PREFS_ZOOM,        1f);
                _drawer.scrollX     = EditorPrefs.GetFloat(PREFS_SCROLL,      0f);
                _drawer.showFrames  = EditorPrefs.GetBool (PREFS_SHOW_FRAMES, false);
                _drawer.fps         = EditorPrefs.GetInt  (PREFS_FPS,         30);
            }
            _testScenePath = EditorPrefs.GetString(PREFS_SCENE_PATH, _testScenePath);
            _testActorName = EditorPrefs.GetString(PREFS_ACTOR_NAME, _testActorName);
            _playbackSpeed = EditorPrefs.GetFloat (PREFS_SPEED,      1f);
            _isLooping     = EditorPrefs.GetBool  (PREFS_LOOP,       false);
            _showSceneEventOverlay = EditorPrefs.GetBool(PREFS_EVENT_SCENE_OVERLAY, true);
            _autoAttachDebugOverlay = EditorPrefs.GetBool(PREFS_EVENT_AUTO_ATTACH, true);
            string registryPath = EditorPrefs.GetString(PREFS_REGISTRY_PATH, "");
            if (!string.IsNullOrEmpty(registryPath))
                _testRegistry = AssetDatabase.LoadAssetAtPath<MotionTestRegistrySO>(registryPath);
            _selectedRegistryIndex = EditorPrefs.GetInt(PREFS_REGISTRY_IDX, -1);
            _testActorMode = (TestActorMode)EditorPrefs.GetInt(PREFS_TEST_MODE, 0);
            string playerSetPath = EditorPrefs.GetString(PREFS_PLAYER_SET_PATH, "");
            if (!string.IsNullOrEmpty(playerSetPath))
                _playerActorAnimationSet = AssetDatabase.LoadAssetAtPath<PlayerActorAnimationMotionSet>(playerSetPath);
            _selectedPlayerWeaponType = (WeaponType)EditorPrefs.GetInt(PREFS_PLAYER_WEAPON, (int)WeaponType.NoWeapon);
            if (_testActorMode == TestActorMode.Player && _playerActorAnimationSet != null)
                SetActorAnimationSet(ResolveSelectedPlayerActorAnimationSet());
        }
        
        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.delayCall += () =>
                {
                    AutoFindPlayer();
                    if (_testActorMode == TestActorMode.Other &&
                        _testRegistry != null && _selectedRegistryIndex >= 0 &&
                        _selectedRegistryIndex < _testRegistry.entries.Count)
                        SpawnRegistryActor(_selectedRegistryIndex);
                };
            }
            else if (state == PlayModeStateChange.ExitingPlayMode)
            {
                _spawnedTestActor = null;
                _scenePlayer = null;
                _targetActor = null;
                _animancer = null;
                _playerSwapBehaviour = null;
                _availableCharacterTypes = null;
                _characterTypeNames = null;
                _selectedCharacterType = CharacterActorType.None;
                Repaint();
            }
        }
        
        void FindAndSetTargetActor()
        {
            if (string.IsNullOrEmpty(_testActorName)) return;
            
            // 이름으로 GameObject 찾기
            var actor = GameObject.Find(_testActorName);
            
            if (actor != null)
            {
                var animancer = actor.GetComponent<AnimancerComponent>();
                if (animancer != null)
                {
                    _targetActor = actor;
                    _animancer = animancer;
                    if (actor.GetComponent<PlayerSwapBehaviour>() != null)
                        _scenePlayer = actor;
                    UpdatePlayerSwapBehaviour();
                    EnsureDebugOverlay();
                    Debug.Log($"대상 액터 자동 설정: {_testActorName}");

                    // Idle 애니메이션 자동 재생
                    PlayIdleAnimation();
                    
                    Repaint();
                }
                else
                {
                    Debug.LogWarning($"{_testActorName}에 AnimancerComponent가 없습니다!");
                }
            }
            else
            {
                Debug.LogWarning($"씬에서 '{_testActorName}' 오브젝트를 찾을 수 없습니다.");
            }
        }
        
        void PlayIdleAnimation()
        {
            if (_animancer == null || _idleAnimation == null) return;

            ForceDrawPlayerWeapons();
            _animancer.Play(_idleAnimation);
            ForceDrawPlayerWeapons();
            Debug.Log($"Idle 애니메이션 재생: {_idleAnimation.name}");
        }

        void UpdatePlayerSwapBehaviour()
        {
            _playerSwapBehaviour = _targetActor != null
                ? _targetActor.GetComponent<PlayerSwapBehaviour>()
                : null;

            if (_playerSwapBehaviour != null)
            {
                _availableCharacterTypes = _playerSwapBehaviour.GetAllCharacterTypes();
                _characterTypeNames = new string[_availableCharacterTypes.Count];
                for (int i = 0; i < _availableCharacterTypes.Count; i++)
                    _characterTypeNames[i] = _availableCharacterTypes[i].ToString();

                _selectedCharacterType = _playerSwapBehaviour.ActiveCharacterType;
                RefreshAnimancerFromActiveModel();
            }
            else
            {
                _availableCharacterTypes = null;
                _characterTypeNames = null;
                _selectedCharacterType = CharacterActorType.None;
            }
        }

        void RefreshAnimancerFromActiveModel()
        {
            if (_playerSwapBehaviour == null) return;
            var modelData = _playerSwapBehaviour.GetModelData(_selectedCharacterType);
            if (modelData?.AnimancerComponent != null)
                _animancer = modelData.AnimancerComponent;
            ForceDrawPlayerWeapons();
        }

        void RefreshTargetActorCache()
        {
            if (_cachedActorKey == _targetActor) return;

            _cachedActorKey = _targetActor;
            if (_targetActor == null)
            {
                _cachedPlayerActor = null;
                _cachedPlayerEquipment = null;
                return;
            }

            _cachedPlayerActor = _targetActor.GetComponent<PlayerActor>()
                              ?? _targetActor.GetComponentInChildren<PlayerActor>(true);
            _cachedPlayerEquipment = _cachedPlayerActor != null
                ? _cachedPlayerActor.GetPlayerEquipment()
                : _targetActor.GetComponentInChildren<PlayerEquipment>(true);
        }

        void ForceDrawPlayerWeapons()
        {
            if (!Application.isPlaying || _targetActor == null) return;

            RefreshTargetActorCache();
            if (_cachedPlayerEquipment == null) return;

            _cachedPlayerEquipment.SetMainWeaponDrawn(true);
            _cachedPlayerEquipment.SetSubWeaponDrawn(true);
        }

        void AcquireMotionToolInputLock()
        {
            if (!Application.isPlaying || !InputManager.Instance)
                return;

            RefreshTargetActorCache();
            var currentPlayerActor = _cachedPlayerActor;

            if (_suppressedPlayerActor != currentPlayerActor)
            {
                _suppressedPlayerActor?.SetInputSuppressed(false);
                _suppressedPlayerActor = currentPlayerActor;
                _suppressedPlayerActor?.SetInputSuppressed(true);
            }

            if (_isMotionToolInputLocked)
            {
                InputManager.Instance.InputBuffer?.Clear();
                InputManager.Instance.SetPlayerActionInputSuppressed(true);
                return;
            }

            _previousInputLayerBeforeMotionTool = InputManager.Instance.CurrentLayer;
            InputManager.Instance.SetPlayerActionInputSuppressed(true);
            InputManager.Instance.InputBuffer?.Clear();
            InputManager.Instance.SetInputLayer(InputLayer.Level_3);
            _isMotionToolInputLocked = true;
        }

        void ReleaseMotionToolInputLock()
        {
            if (!_isMotionToolInputLocked || !InputManager.Instance)
                return;

            _suppressedPlayerActor?.SetInputSuppressed(false);
            _suppressedPlayerActor = null;
            InputManager.Instance.SetPlayerActionInputSuppressed(false);
            InputManager.Instance.InputBuffer?.Clear();
            InputManager.Instance.SetInputLayer(_previousInputLayerBeforeMotionTool);
            _isMotionToolInputLocked = false;
        }

        void SwapCharacterModel(CharacterActorType type)
        {
            if (_playerSwapBehaviour == null) return;
            if (!_playerSwapBehaviour.SwapTo(type)) return;

            _selectedCharacterType = type;
            RefreshAnimancerFromActiveModel();
            EnsureDebugOverlay();
            TryAutoSelectMotionSet(_targetActor);

            if (_isPlaying)
                UpdateAnimancerPlayback();
            else
                PlayIdleAnimation();
        }

        void TryAutoSelectMotionSet(GameObject actorGo)
        {
            if (actorGo == null) return;

            // Player: 활성 캐릭터 모델의 PlayerActorAnimator 우선 탐색
            // Player 분기에 진입하면 성공·실패 모두 여기서 종료 (비플레이어 fallback 차단)
            if (_playerSwapBehaviour != null && _selectedCharacterType != CharacterActorType.None)
            {
                var modelData = _playerSwapBehaviour.GetModelData(_selectedCharacterType);
                if (modelData != null)
                {
                    var playerAnimator = modelData.GetComponentInChildren<PlayerActorAnimatorType>(true);
                    if (playerAnimator?.PlayerMotionSet != null)
                        SetPlayerActorAnimationSet(playerAnimator.PlayerMotionSet);
                }
                return;
            }

            // 비플레이어(Monster, NPC 등): ActorAnimator.MotionSet 직접 사용
            var animator = actorGo.GetComponentInChildren<ActorAnimatorType>(true);
            if (animator?.MotionSet != null)
                SetActorAnimationSet(animator.MotionSet);
        }

        // ── Loop/Freeze 에디터 프리뷰 상태 ──
        private LoopEvent _editorActiveLoopEvent;
        private int _editorLoopRemainingCount;
        private float _editorFreezeTimer;
        private bool _editorIsFrozen;
        private bool _editorIsInfiniteLooping;
        private float _editorInfiniteLoopElapsed;
        
        void OnEditorUpdate()
        {
            if (_isPlaying && !_isPaused && Application.isPlaying && _animancer != null)
            {
                AcquireMotionToolInputLock();
                ForceDrawPlayerWeapons();

                var currentSet = GetCurrentMotionSet();
                if (currentSet == null) return;

                float deltaTime = Time.deltaTime * _playbackSpeed;

                // ── Freeze 처리: 시간을 흘리지 않고 타이머만 소모 ──
                if (_editorIsFrozen)
                {
                    _editorFreezeTimer -= deltaTime;
                    if (_editorFreezeTimer <= 0f)
                    {
                        _editorIsFrozen = false;
                        // 애니메이션 속도 복원
                        if (_animancer.States.Current != null)
                        {
                            float motionSpd = GetMotionSpeedAtTime(currentSet, _playbackTime);
                            _animancer.States.Current.Speed = motionSpd * _playbackSpeed;
                        }
                    }
                    else
                    {
                        // Freeze 중에도 커서 위치와 이벤트는 갱신
                        _drawer.cursorTime = _playbackTime;
                        ExecuteActiveEvents(currentSet);
                        Repaint();
                        return;
                    }
                }

                _playbackTime += deltaTime;

                float effectiveEnd = GetEffectiveEndTime(currentSet);

                if (_playbackTime >= effectiveEnd)
                {
                    float savedPrev   = _previousTime;
                    _playbackTime     = effectiveEnd;
                    _previousTime     = savedPrev;
                    ExecuteActiveEvents(currentSet);

                    if (_isLooping)
                    {
                        ResetEditorLoopState();
                        LoopPlayback();
                    }
                    else
                    {
                        _drawer.cursorTime = effectiveEnd;
                        Repaint();
                        StopPlayback();
                    }
                    return;
                }

                // 모션 인덱스 전환
                if (currentSet.GetMotionAtTime(_playbackTime, out int newIdx, out float localTime))
                {
                    if (newIdx != _currentMotionIndex)
                    {
                        ResetEditorLoopState();
                        _currentMotionIndex = newIdx;
                        if (newIdx >= 0 && newIdx < currentSet.motions.Count)
                            PlayMotionClip(currentSet.motions[newIdx]);
                    }

                    // ── Loop/Freeze 이벤트 처리 ──
                    ProcessEditorLoopEvents(currentSet, newIdx, localTime);
                }

                ExecuteActiveEvents(currentSet);
                ForceDrawPlayerWeapons();

                _drawer.cursorTime = _playbackTime;
                _previousTime      = _playbackTime;
                Repaint();
            }
        }

        /// <summary>
        /// 에디터 프리뷰에서 Loop/Freeze 이벤트를 처리한다.
        /// ActorAnimator.ProcessLoopEvents와 동일한 로직.
        /// </summary>
        void ProcessEditorLoopEvents(MotionSet motionSet, int motionIndex, float localTime)
        {
            if (motionSet?.motions == null) return;
            if (motionIndex < 0 || motionIndex >= motionSet.motions.Count) return;

            var motion = motionSet.motions[motionIndex];
            if (motion?.events == null) return;

            foreach (var evt in motion.events)
            {
                if (evt is not LoopEvent loopEvt) continue;

                switch (loopEvt.mode)
                {
                    case LoopEventMode.Loop:
                        HandleEditorLoopMode(loopEvt, localTime, motion);
                        break;
                    case LoopEventMode.InfiniteLoop:
                        HandleEditorInfiniteLoopMode(loopEvt, localTime, motion);
                        break;
                    case LoopEventMode.Freeze:
                        HandleEditorFreezeMode(loopEvt, localTime, motion);
                        break;
                }
            }
        }

        void HandleEditorLoopMode(LoopEvent loopEvt, float localTime, Motion motion)
        {
            if (localTime < loopEvt.endTime) return;

            if (_editorActiveLoopEvent != loopEvt)
            {
                _editorActiveLoopEvent = loopEvt;
                _editorLoopRemainingCount = loopEvt.loopCount;
            }

            if (_editorLoopRemainingCount <= 0) return;

            // globalTime 되감기
            float loopDuration = loopEvt.endTime - loopEvt.startTime;
            _playbackTime -= loopDuration;
            _editorLoopRemainingCount--;

            // Animancer 클립 시간도 되감기
            if (_animancer != null && _animancer.States.Current != null)
            {
                float spd = motion.playbackSpeed > 0f ? motion.playbackSpeed : 1f;
                float clipTime = motion.ClipStartTime + loopEvt.startTime * spd;
                _animancer.States.Current.Time = clipTime;
            }
        }

        void HandleEditorFreezeMode(LoopEvent loopEvt, float localTime, Motion motion)
        {
            if (_editorIsFrozen) return;
            if (_editorActiveLoopEvent == loopEvt) return;
            if (localTime < loopEvt.startTime) return;

            _editorActiveLoopEvent = loopEvt;
            _editorIsFrozen = true;
            _editorFreezeTimer = loopEvt.freezeDuration;

            if (_animancer != null && _animancer.States.Current != null)
                _animancer.States.Current.Speed = 0f;
        }

        void HandleEditorInfiniteLoopMode(LoopEvent loopEvt, float localTime, Motion motion)
        {
            if (!_editorIsInfiniteLooping && localTime >= loopEvt.endTime)
            {
                _editorActiveLoopEvent = loopEvt;
                _editorIsInfiniteLooping = true;
                _editorInfiniteLoopElapsed = 0f;
            }

            if (!_editorIsInfiniteLooping || _editorActiveLoopEvent != loopEvt) return;

            // Duration 경과 시 자동 해제
            _editorInfiniteLoopElapsed += Time.deltaTime * _playbackSpeed;
            if (motion != null && _editorInfiniteLoopElapsed >= motion.Duration)
            {
                _editorIsInfiniteLooping = false;
                _editorActiveLoopEvent = null;
                return;
            }

            if (localTime >= loopEvt.endTime)
            {
                float loopDuration = loopEvt.endTime - loopEvt.startTime;
                _playbackTime -= loopDuration;

                if (_animancer != null && _animancer.States.Current != null)
                {
                    float spd = motion.playbackSpeed > 0f ? motion.playbackSpeed : 1f;
                    _animancer.States.Current.Time = motion.ClipStartTime + loopEvt.startTime * spd;
                }
            }
        }

        void ResetEditorLoopState()
        {
            _editorActiveLoopEvent = null;
            _editorLoopRemainingCount = 0;
            _editorFreezeTimer = 0f;
            _editorIsFrozen = false;
            _editorIsInfiniteLooping = false;
            _editorInfiniteLoopElapsed = 0f;
        }

        float GetMotionSpeedAtTime(MotionSet motionSet, float time)
        {
            if (motionSet.GetMotionAtTime(time, out int idx, out _) &&
                idx >= 0 && idx < motionSet.motions.Count)
            {
                var m = motionSet.motions[idx];
                if (m != null) return m.playbackSpeed;
            }
            return 1f;
        }

        float GetEffectiveEndTime(MotionSet motionSet)
        {
            float totalDuration = motionSet.TotalDuration;
            if (_endTime > 0f && _endTime <= totalDuration)
                return _endTime;
            return totalDuration;
        }

        void LoopPlayback()
        {
            var motionSet = GetCurrentMotionSet();
            if (motionSet == null) return;

            // Loop/Freeze 상태 리셋
            ResetEditorLoopState();

            // 이벤트 상태 리셋
            if (_activeEvents != null && _targetActor != null)
            {
                foreach (var evt in _activeEvents)
                    evt.OnCompleteEvent(_targetActor);
                _activeEvents.Clear();
            }
            _executedEvents?.Clear();

            float loopStart = Mathf.Max(0f, _startTime);
            _currentMotionIndex = -1;
            _playbackTime  = loopStart;
            _previousTime  = loopStart - 0.001f;
            _drawer.cursorTime = _playbackTime;

            UpdateAnimancerPlayback();

            // 루프 시작 시 모션 인덱스 재기록
            if (motionSet.GetMotionAtTime(_playbackTime, out int startIdx, out _))
                _currentMotionIndex = startIdx;

            Repaint();
        }

        void OnSelectionChange()
        {
            TryBindFromSelection();
            
            // Hierarchy에서 GameObject 선택 시 자동으로 대상 액터로 설정
            if (Selection.activeGameObject != null && Application.isPlaying)
            {
                var animancer = Selection.activeGameObject.GetComponent<AnimancerComponent>();
                if (animancer != null)
                {
                    _targetActor = Selection.activeGameObject;
                    _animancer = animancer;
                    UpdatePlayerSwapBehaviour();
                    EnsureDebugOverlay();

                    // 대상 액터 변경 시 Idle 재생
                    if (!_isPlaying)
                    {
                        PlayIdleAnimation();
                    }
                }
            }
            
            Repaint();
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (!_showSceneEventOverlay) return;
            if (!_isPlaying || _targetActor == null) return;

            string active = _activeEvents != null && _activeEvents.Count > 0
                ? string.Join(", ", GetEventLabels(_activeEvents))
                : "-";

            string last = _eventLog.Count > 0 ? _eventLog[0] : "-";
            string warpStatus = BuildWarpDebugText();
            Handles.Label(
                _targetActor.transform.position + Vector3.up * 2.25f,
                $"MotionSet {_playbackTime:F2}s\n{warpStatus}\nActive: {active}\nLast: {last}");
        }

        string BuildWarpDebugText()
        {
            if (_targetActor == null) return "Warp: -";

            var actorController = _targetActor.GetComponent<MotionWarpController>()
                                ?? _targetActor.GetComponentInParent<MotionWarpController>()
                                ?? _targetActor.GetComponentInChildren<MotionWarpController>();
            if (actorController == null)
                return "Warp: 컨트롤러 없음";

            if (actorController.IsApplicable)
                return $"Warp: 적용 / 오차 {actorController.LastArrivalError:F2}m";

            if (!string.IsNullOrEmpty(actorController.LastFailureReason))
                return $"Warp: {actorController.LastFailureReason}";

            return "Warp: 대기";
        }

        void TryBindFromSelection()
        {
            if (Selection.activeObject is ActorAnimationMotionSet actorSet)
            {
                SetActorAnimationSet(actorSet);
            }
            else if (Selection.activeObject is PlayerActorAnimationMotionSet playerSet)
            {
                SetPlayerActorAnimationSet(playerSet);
            }
            else if (Selection.activeObject is MotionSetAsset selected)
            {
                SetAsset(selected);
                _useTemporarySet = false;
                SelectActorMotionKeyForAsset(selected);
            }
        }

        void SetAsset(MotionSetAsset asset)
        {
            if (_asset == asset) return;

            ResetPlaybackStateForMotionChange(true);
            _asset  = asset;

            // 모션 전환 시 zoom/scroll/표시 옵션은 보존
            float prevZoom       = _drawer != null ? _drawer.zoom       : 1f;
            float prevScrollX    = _drawer != null ? _drawer.scrollX    : 0f;
            bool  prevShowFrames = _drawer != null ? _drawer.showFrames : false;
            int   prevFps        = _drawer != null ? _drawer.fps        : 30;

            _drawer = new MotionSetDrawer(() => _asset, Repaint, OnSelectedMotionChanged)
            {
                zoom       = prevZoom,
                scrollX    = prevScrollX,
                showFrames = prevShowFrames,
                fps        = prevFps,
            };
        }

        void OnSelectedMotionChanged(int previousIndex, int selectedIndex)
        {
            if (selectedIndex < 0 || previousIndex == selectedIndex)
                return;

            ResetPlaybackStateForMotionChange(true);
        }

        void ResetPlaybackStateForMotionChange(bool playIdle)
        {
            bool hadPlaybackState = _isPlaying || _isPaused || _playbackTime > 0f || _currentMotionIndex >= 0;
            ReleaseMotionToolInputLock();

            if (_activeEvents != null && _targetActor != null)
            {
                foreach (var evt in _activeEvents)
                    evt?.OnCompleteEvent(_targetActor);
            }

            _isPlaying = false;
            _isPaused = false;
            _currentMotionIndex = -1;
            _playbackTime = 0f;
            _previousTime = -0.001f;
            _drawer.cursorTime = 0f;

            ResetEditorLoopState();
            _executedEvents?.Clear();
            _activeEvents?.Clear();
            _eventLog.Clear();
            MotionSetEventDebugOverlay.Clear();

            if (hadPlaybackState && playIdle && Application.isPlaying && _animancer != null)
                PlayIdleAnimation();
        }

        void SetActorAnimationSet(ActorAnimationMotionSet actorSet)
        {
            if (_actorAnimationSet == actorSet && _asset != null) return;

            _actorAnimationSet = actorSet;
            _useTemporarySet = false;

            if (_actorAnimationSet == null)
            {
                _selectedActorMotionKey = AnimKey.None;
                return;
            }

            if (_asset != null && SelectActorMotionKeyForAsset(_asset))
                return;

            var first = GetActorMotionEntries(_actorAnimationSet, true)
                .Find(e => e.asset != null);

            _selectedActorMotionKey = first.key;
            SetAsset(first.asset);
        }

        void SetPlayerActorAnimationSet(PlayerActorAnimationMotionSet playerSet)
        {
            if (_playerActorAnimationSet == playerSet && _actorAnimationSet == ResolveSelectedPlayerActorAnimationSet())
                return;

            _testActorMode = TestActorMode.Player;
            _playerActorAnimationSet = playerSet;
            SetActorAnimationSet(ResolveSelectedPlayerActorAnimationSet());
        }

        ActorAnimationMotionSet ResolveSelectedPlayerActorAnimationSet()
        {
            return _playerActorAnimationSet != null
                ? _playerActorAnimationSet.GetActorAnimationMotionSet(_selectedPlayerWeaponType)
                : null;
        }

        void SetSelectedPlayerWeaponType(WeaponType weaponType)
        {
            if (_selectedPlayerWeaponType == weaponType) return;
            _selectedPlayerWeaponType = weaponType;
            SetActorAnimationSet(ResolveSelectedPlayerActorAnimationSet());
        }

        void AssignPlayerWeaponActorSet(ActorAnimationMotionSet actorSet)
        {
            if (_playerActorAnimationSet == null) return;

            var sObj = new SerializedObject(_playerActorAnimationSet);
            var listProp = sObj.FindProperty("motionSets").FindPropertyRelative("_serializedList");
            int idx = FindPlayerWeaponTypeIndex(listProp, _selectedPlayerWeaponType);

            if (idx < 0)
            {
                listProp.InsertArrayElementAtIndex(listProp.arraySize);
                idx = listProp.arraySize - 1;
            }

            var elem = listProp.GetArrayElementAtIndex(idx);
            elem.FindPropertyRelative("Key").intValue = (int)_selectedPlayerWeaponType;
            elem.FindPropertyRelative("Value").objectReferenceValue = actorSet;

            sObj.ApplyModifiedProperties();
            EditorUtility.SetDirty(_playerActorAnimationSet);
            AssetDatabase.SaveAssets();

            SetActorAnimationSet(actorSet);
        }

        static int FindPlayerWeaponTypeIndex(SerializedProperty listProp, WeaponType weaponType)
        {
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if ((WeaponType)listProp.GetArrayElementAtIndex(i).FindPropertyRelative("Key").intValue == weaponType)
                    return i;
            }
            return -1;
        }

        struct ActorMotionEntry
        {
            public AnimKey key;
            public ActorAnimationMotionSet source;
            public MotionSetAsset asset;
            public bool isOwn;
        }

        static readonly (string label, int min, int max)[] ACTOR_KEY_RANGES =
        {
            ("이동",       0,   29),
            ("공격",       100, 199),
            ("강공격",     200, 299),
            ("대시 공격",  300, 399),
            ("점프 공격",  400, 499),
            ("스킬",       500, 619),
            ("차지/피니시",620, 699),
            ("피격/사망",  700, 919),
            ("기타",       920, int.MaxValue),
        };

        static AnimKey[] _allAnimKeys;
        static AnimKey[] AllAnimKeys => _allAnimKeys ??= (AnimKey[])System.Enum.GetValues(typeof(AnimKey));

        static System.Collections.Generic.List<ActorMotionEntry> GetActorMotionEntries(
            ActorAnimationMotionSet root, bool includeFallback)
        {
            var result = new System.Collections.Generic.List<ActorMotionEntry>();
            var seen = new System.Collections.Generic.HashSet<AnimKey>();
            var visited = new System.Collections.Generic.HashSet<ActorAnimationMotionSet>();
            var current = root;

            while (current != null && !visited.Contains(current))
            {
                visited.Add(current);

                if (current.motionSets != null)
                {
                    foreach (var kv in current.motionSets)
                    {
                        if (!seen.Add(kv.Key)) continue;
                        result.Add(new ActorMotionEntry
                        {
                            key = kv.Key,
                            source = current,
                            asset = kv.Value,
                            isOwn = current == root
                        });
                    }
                }

                if (!includeFallback) break;
                current = current.fallbackMotionSet;
            }

            result.Sort((a, b) => ((int)a.key).CompareTo((int)b.key));
            return result;
        }

        bool SelectActorMotionKeyForAsset(MotionSetAsset asset)
        {
            if (_actorAnimationSet == null || asset == null) return false;

            var entries = GetActorMotionEntries(_actorAnimationSet, true);
            foreach (var entry in entries)
            {
                if (entry.asset != asset) continue;
                _selectedActorMotionKey = entry.key;
                return true;
            }
            return false;
        }

        void ShowAddActorMotionMenu()
        {
            if (_actorAnimationSet == null) return;

            var existing = new System.Collections.Generic.HashSet<AnimKey>();
            if (_actorAnimationSet.motionSets != null)
            {
                foreach (var key in _actorAnimationSet.motionSets.Keys)
                    existing.Add(key);
            }

            var menu = new GenericMenu();
            foreach (var key in AllAnimKeys)
            {
                if (key == AnimKey.None || existing.Contains(key)) continue;

                AnimKey captured = key;
                menu.AddItem(new GUIContent(GetActorKeyGroupLabel(key) + "/" + key), false, () =>
                {
                    var asset = CreateActorMotionSetAsset(captured);
                    if (asset == null) return;

                    AddOrAssignActorMotionAsset(captured, asset);
                    _selectedActorMotionKey = captured;
                    SetAsset(asset);
                    _useTemporarySet = false;
                    Selection.activeObject = _actorAnimationSet;
                    EditorGUIUtility.PingObject(asset);
                    Repaint();
                });
            }

            menu.ShowAsContext();
        }

        MotionSetAsset CreateActorMotionSetAsset(AnimKey key)
        {
            string actorSetPath = AssetDatabase.GetAssetPath(_actorAnimationSet);
            string dir = string.IsNullOrEmpty(actorSetPath)
                ? "Assets"
                : System.IO.Path.GetDirectoryName(actorSetPath)?.Replace("\\", "/");
            string suggestedName = $"{_actorAnimationSet.name}_{key}.asset";

            string path = EditorUtility.SaveFilePanelInProject(
                "Actor MotionSet 에셋 생성", suggestedName, "asset", "저장 위치를 선택하세요.", dir);
            if (string.IsNullOrEmpty(path)) return null;

            var asset = CreateInstance<MotionSetAsset>();
            asset.motionSet = new MotionSet
            {
                motionSetName = System.IO.Path.GetFileNameWithoutExtension(path),
                motions = new System.Collections.Generic.List<Motion>()
            };

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return asset;
        }

        void AddOrAssignActorMotionAsset(AnimKey key, MotionSetAsset asset)
        {
            var sObj = new SerializedObject(_actorAnimationSet);
            var listProp = sObj.FindProperty("motionSets").FindPropertyRelative("_serializedList");
            int idx = FindActorMotionKeyIndex(listProp, key);

            if (idx < 0)
            {
                listProp.InsertArrayElementAtIndex(listProp.arraySize);
                idx = listProp.arraySize - 1;
            }

            var elem = listProp.GetArrayElementAtIndex(idx);
            elem.FindPropertyRelative("Key").intValue = (int)key;
            elem.FindPropertyRelative("Value").objectReferenceValue = asset;

            sObj.ApplyModifiedProperties();
            EditorUtility.SetDirty(_actorAnimationSet);
            AssetDatabase.SaveAssets();
        }

        static int FindActorMotionKeyIndex(SerializedProperty listProp, AnimKey key)
        {
            for (int i = 0; i < listProp.arraySize; i++)
            {
                if ((AnimKey)listProp.GetArrayElementAtIndex(i).FindPropertyRelative("Key").intValue == key)
                    return i;
            }
            return -1;
        }

        static string GetActorKeyGroupLabel(AnimKey key)
        {
            int value = (int)key;
            foreach (var range in ACTOR_KEY_RANGES)
            {
                if (value >= range.min && value <= range.max)
                    return range.label;
            }
            return "기타";
        }
        
        MotionSet GetCurrentMotionSet()
        {
            if (_useTemporarySet)
                return _temporarySet;
            return _asset?.motionSet;
        }

        void OnGUI()
        {
            DrawToolbar();
            DrawActorAnimationSetBar();
            DrawTestActorRegistry();
            DrawPlaybackControls();
            DrawEventDebugControls();

            if (_actorAnimationSet != null)
            {
                DrawActorSetEditorLayout();
                return;
            }

            DrawMotionSetEditorBody();
        }

        void DrawMotionSetEditorBody()
        {
            var currentSet = GetCurrentMotionSet();
            if (currentSet == null)
            {
                DrawEmptyState();
                return;
            }

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
            {
                _drawer.DrawFullGUI(currentSet);

                if (GUI.changed)
                {
                    if (_asset != null)
                        EditorUtility.SetDirty(_asset);
                }
            }
            EditorGUILayout.EndScrollView(); 
            
            // 타임라인 클릭으로 재생 위치 조절 처리
            HandleTimelineScrubbing();
        }

        void DrawActorSetEditorLayout()
        {
            EditorGUILayout.BeginHorizontal();
            {
                DrawActorMotionSidebar();

                EditorGUILayout.BeginVertical();
                {
                    DrawMotionSetEditorBody();
                }
                EditorGUILayout.EndVertical();
            }
            EditorGUILayout.EndHorizontal();

        }

        void DrawEventDebugControls()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("이벤트 디버그", EditorStyles.boldLabel, GUILayout.Width(90));
                    _showSceneEventOverlay = EditorGUILayout.ToggleLeft("Scene 라벨", _showSceneEventOverlay, GUILayout.Width(90));
                    _autoAttachDebugOverlay = EditorGUILayout.ToggleLeft("Game 오버레이 자동 부착", _autoAttachDebugOverlay, GUILayout.Width(150));

                    EditorGUI.BeginDisabledGroup(_targetActor == null);
                    if (GUILayout.Button("오버레이 부착", GUILayout.Width(90)))
                        EnsureDebugOverlay(true);
                    EditorGUI.EndDisabledGroup();

                    if (GUILayout.Button("로그 지우기", GUILayout.Width(80)))
                    {
                        _eventLog.Clear();
                        MotionSetEventDebugOverlay.Clear();
                    }
                }
                EditorGUILayout.EndHorizontal();

                string activeText = _activeEvents != null && _activeEvents.Count > 0
                    ? string.Join(", ", GetEventLabels(_activeEvents))
                    : "-";
                EditorGUILayout.LabelField(BuildWarpDebugText(), EditorStyles.miniLabel);
                EditorGUILayout.LabelField($"Active: {activeText}", EditorStyles.miniLabel);

                int count = Mathf.Min(5, _eventLog.Count);
                for (int i = 0; i < count; i++)
                    EditorGUILayout.LabelField(_eventLog[i], EditorStyles.miniLabel);
            }
            EditorGUILayout.EndVertical();
        }

        static System.Collections.Generic.IEnumerable<string> GetEventLabels(
            System.Collections.Generic.IEnumerable<MotionEventBase> events)
        {
            foreach (var evt in events)
            {
                if (evt == null) continue;
                yield return evt.GetShortLabel();
            }
        }

        void EnsureDebugOverlay(bool force = false)
        {
            if (_targetActor == null) return;
            if (!force && !_autoAttachDebugOverlay) return;

            if (_targetActor.GetComponent<MotionSetEventDebugOverlay>() == null)
                _targetActor.AddComponent<MotionSetEventDebugOverlay>();
        }

        void RecordEventLog(string message)
        {
            if (string.IsNullOrEmpty(message)) return;

            _eventLog.Insert(0, message);
            while (_eventLog.Count > 30)
                _eventLog.RemoveAt(_eventLog.Count - 1);

            MotionSetEventDebugOverlay.RecordEvent(message);
        }

        void PublishEventDebugState()
        {
            if (_targetActor == null || GetCurrentMotionSet() == null) return;

            MotionSetEventDebugOverlay.Publish(
                _targetActor,
                _playbackTime,
                _activeEvents,
                GetCurrentMotionSet().motionSetName);
        }

        // ── 상단 툴바 ──
        void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            {
                EditorGUILayout.LabelField("에셋", GUILayout.Width(35));

                EditorGUI.BeginDisabledGroup(_useTemporarySet);
                var newAsset = (MotionSetAsset)EditorGUILayout.ObjectField(
                    _asset, typeof(MotionSetAsset), false, GUILayout.Width(250));

                if (newAsset != _asset)
                {
                    SetAsset(newAsset);
                    _useTemporarySet = false;
                }
                EditorGUI.EndDisabledGroup();

                GUILayout.FlexibleSpace();
                
                // 씬 플레이 버튼
                if (!EditorApplication.isPlaying)
                {
                    GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
                    if (GUILayout.Button("▶ 씬 플레이", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    {
                        PlayTestScene();
                    }
                    GUI.backgroundColor = Color.white;
                    
                    // 설정 버튼
                    if (GUILayout.Button("⚙", EditorStyles.toolbarButton, GUILayout.Width(25)))
                    {
                        ShowTestSceneSettings();
                    }
                }

                if (_asset != null && GUILayout.Button("선택", EditorStyles.toolbarButton, GUILayout.Width(50)))
                {
                    Selection.activeObject = _asset;
                    EditorGUIUtility.PingObject(_asset);
                }

                if (GUILayout.Button("새로 만들기", EditorStyles.toolbarButton, GUILayout.Width(80)))
                    CreateNewAsset();
                    
                if (GUILayout.Button("임시 셋", EditorStyles.toolbarButton, GUILayout.Width(60)))
                    CreateTemporarySet();
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawActorAnimationSetBar()
        {
            if (_testActorMode == TestActorMode.Player)
            {
                DrawPlayerActorAnimationSetBar();
                return;
            }

            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Actor Set", GUILayout.Width(70));

                var newSet = (ActorAnimationMotionSet)EditorGUILayout.ObjectField(
                    _actorAnimationSet, typeof(ActorAnimationMotionSet), false, GUILayout.Width(220));

                if (newSet != _actorAnimationSet)
                    SetActorAnimationSet(newSet);

                if (_actorAnimationSet == null)
                {
                    EditorGUILayout.LabelField("ActorAnimationMotionSet을 선택하면 좌측 목록에서 모션을 전환할 수 있습니다.", EditorStyles.miniLabel);
                    EditorGUILayout.EndHorizontal();
                    return;
                }

                EditorGUILayout.LabelField(
                    _asset != null
                        ? $"{_selectedActorMotionKey} / {_asset.name}"
                        : "좌측 목록에서 MotionSet을 선택하세요.",
                    EditorStyles.miniBoldLabel);

                EditorGUI.BeginDisabledGroup(_asset == null);
                if (GUILayout.Button("에셋 선택", GUILayout.Width(70)))
                {
                    Selection.activeObject = _asset;
                    EditorGUIUtility.PingObject(_asset);
                }
                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("+ 키/에셋 추가", GUILayout.Width(100)))
                    ShowAddActorMotionMenu();
            }
            EditorGUILayout.EndHorizontal();
        }

        void DrawPlayerActorAnimationSetBar()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("Player Set", GUILayout.Width(70));

                    var newPlayerSet = (PlayerActorAnimationMotionSet)EditorGUILayout.ObjectField(
                        _playerActorAnimationSet, typeof(PlayerActorAnimationMotionSet), false, GUILayout.Width(220));
                    if (newPlayerSet != _playerActorAnimationSet)
                        SetPlayerActorAnimationSet(newPlayerSet);

                    var newWeapon = (WeaponType)EditorGUILayout.EnumPopup(
                        _selectedPlayerWeaponType, GUILayout.Width(120));
                    if (newWeapon != _selectedPlayerWeaponType)
                        SetSelectedPlayerWeaponType(newWeapon);

                    EditorGUI.BeginDisabledGroup(_playerActorAnimationSet == null);
                    var resolved = ResolveSelectedPlayerActorAnimationSet();
                    var newActorSet = (ActorAnimationMotionSet)EditorGUILayout.ObjectField(
                        resolved, typeof(ActorAnimationMotionSet), false, GUILayout.Width(220));
                    if (newActorSet != resolved)
                        AssignPlayerWeaponActorSet(newActorSet);
                    EditorGUI.EndDisabledGroup();

                    EditorGUI.BeginDisabledGroup(_playerActorAnimationSet == null);
                    if (GUILayout.Button("Player SO 선택", GUILayout.Width(90)))
                    {
                        Selection.activeObject = _playerActorAnimationSet;
                        EditorGUIUtility.PingObject(_playerActorAnimationSet);
                    }
                    EditorGUI.EndDisabledGroup();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.Space(70);
                    if (_playerActorAnimationSet == null)
                    {
                        EditorGUILayout.LabelField("PlayerActorAnimationMotionSet을 선택하세요.", EditorStyles.miniLabel);
                    }
                    else if (_actorAnimationSet == null)
                    {
                        EditorGUILayout.LabelField(
                            $"WeaponType.{_selectedPlayerWeaponType}에 연결된 ActorAnimationMotionSet이 없습니다.",
                            EditorStyles.miniLabel);
                    }
                    else
                    {
                        EditorGUILayout.LabelField(
                            _asset != null
                                ? $"{_selectedActorMotionKey} / {_asset.name}"
                                : "좌측 목록에서 MotionSet을 선택하세요.",
                            EditorStyles.miniBoldLabel);

                        EditorGUI.BeginDisabledGroup(_asset == null);
                        if (GUILayout.Button("에셋 선택", GUILayout.Width(70)))
                        {
                            Selection.activeObject = _asset;
                            EditorGUIUtility.PingObject(_asset);
                        }
                        EditorGUI.EndDisabledGroup();

                        if (GUILayout.Button("+ 키/에셋 추가", GUILayout.Width(100)))
                            ShowAddActorMotionMenu();
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        // ── 테스트 대상 UI ──
        void DrawTestActorRegistry()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            {
                // 모드 토글 헤더
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("테스트 대상", EditorStyles.boldLabel, GUILayout.Width(70));

                    bool wantPlayer = GUILayout.Toggle(
                        _testActorMode == TestActorMode.Player, "Player",
                        EditorStyles.miniButtonLeft, GUILayout.Width(80));
                    bool wantOther = GUILayout.Toggle(
                        _testActorMode == TestActorMode.Other, "기타 액터",
                        EditorStyles.miniButtonRight, GUILayout.Width(80));

                    if (wantPlayer && _testActorMode != TestActorMode.Player)
                        SetTestActorMode(TestActorMode.Player);
                    else if (wantOther && _testActorMode != TestActorMode.Other)
                        SetTestActorMode(TestActorMode.Other);
                }
                EditorGUILayout.EndHorizontal();

                if (_testActorMode == TestActorMode.Player)
                    DrawPlayerTestUI();
                else
                    DrawOtherActorTestUI();
            }
            EditorGUILayout.EndVertical();
        }

        void DrawPlayerTestUI()
        {
            // Player 오브젝트 필드 + 자동 탐색
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("Player", GUILayout.Width(50));

                var newPlayer = (GameObject)EditorGUILayout.ObjectField(
                    _scenePlayer, typeof(GameObject), true, GUILayout.Width(200));
                if (newPlayer != _scenePlayer)
                {
                    _scenePlayer = newPlayer;
                    if (_scenePlayer != null && Application.isPlaying)
                    {
                        _targetActor = _scenePlayer;
                        _animancer   = _scenePlayer.GetComponent<AnimancerComponent>()
                                    ?? _scenePlayer.GetComponentInChildren<AnimancerComponent>();
                        UpdatePlayerSwapBehaviour();
                        TryAutoSelectMotionSet(_scenePlayer);
                        EnsureDebugOverlay();
                        PlayIdleAnimation();
                    }
                }

                EditorGUI.BeginDisabledGroup(!Application.isPlaying);
                if (GUILayout.Button("자동 탐색", GUILayout.Width(70)))
                    AutoFindPlayer();
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();

            // 캐릭터 모델 드롭다운 (PlayerSwapBehaviour가 있을 때만)
            if (_playerSwapBehaviour != null && _availableCharacterTypes != null && _availableCharacterTypes.Count > 0)
            {
                EditorGUILayout.BeginHorizontal();
                {
                    EditorGUILayout.LabelField("캐릭터 모델", GUILayout.Width(80));

                    int currentIdx = _availableCharacterTypes.IndexOf(_selectedCharacterType);
                    int newIdx = EditorGUILayout.Popup(
                        Mathf.Max(0, currentIdx), _characterTypeNames, GUILayout.Width(150));
                    if (newIdx >= 0 && newIdx < _availableCharacterTypes.Count && newIdx != currentIdx)
                        SwapCharacterModel(_availableCharacterTypes[newIdx]);

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.LabelField(
                        $"[{_selectedCharacterType}]", EditorStyles.miniLabel, GUILayout.Width(120));
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        void DrawOtherActorTestUI()
        {
            // 레지스트리 SO 필드
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("레지스트리", GUILayout.Width(60));

                var newRegistry = (MotionTestRegistrySO)EditorGUILayout.ObjectField(
                    _testRegistry, typeof(MotionTestRegistrySO), false, GUILayout.Width(210));
                if (newRegistry != _testRegistry)
                {
                    _testRegistry = newRegistry;
                    _selectedRegistryIndex = -1;
                    _registryNames = null;
                }

                if (GUILayout.Button("생성", EditorStyles.miniButton, GUILayout.Width(40)))
                    CreateTestRegistry();
            }
            EditorGUILayout.EndHorizontal();

            if (_testRegistry == null || _testRegistry.entries == null || _testRegistry.entries.Count == 0)
            {
                EditorGUILayout.LabelField(
                    "MotionTestRegistrySO를 설정하고 ActorDefinitionSO 항목을 추가하세요.",
                    EditorStyles.miniLabel);
                return;
            }

            // 액터 선택 + 스폰/제거
            EditorGUILayout.BeginHorizontal();
            {
                EditorGUILayout.LabelField("액터", GUILayout.Width(40));

                if (_registryNames == null || _registryNames.Length != _testRegistry.entries.Count)
                    RebuildRegistryNames();

                int newIdx = EditorGUILayout.Popup(
                    Mathf.Max(0, _selectedRegistryIndex), _registryNames, GUILayout.MinWidth(200));
                if (newIdx != _selectedRegistryIndex)
                    _selectedRegistryIndex = newIdx;

                EditorGUI.BeginDisabledGroup(!Application.isPlaying || _selectedRegistryIndex < 0);
                GUI.backgroundColor = new Color(0.5f, 0.8f, 1f);
                if (GUILayout.Button("스폰", GUILayout.Width(50)))
                    SpawnRegistryActor(_selectedRegistryIndex);
                GUI.backgroundColor = Color.white;
                EditorGUI.EndDisabledGroup();

                if (_spawnedTestActor != null)
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.ObjectField(
                        _spawnedTestActor, typeof(GameObject), true, GUILayout.Width(130));
                    EditorGUI.EndDisabledGroup();

                    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                    if (GUILayout.Button("제거", GUILayout.Width(40)))
                        DestroySpawnedActor();
                    GUI.backgroundColor = Color.white;
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        void SetTestActorMode(TestActorMode mode)
        {
            if (_testActorMode == mode) return;
            _testActorMode = mode;

            StopPlayback();

            if (mode == TestActorMode.Player)
            {
                // 스폰된 비플레이어 액터 제거
                DestroySpawnedActor();
                // Player 활성화 + 타겟 설정
                if (_scenePlayer != null && Application.isPlaying)
                {
                    _scenePlayer.SetActive(true);
                    _targetActor = _scenePlayer;
                    _animancer   = _scenePlayer.GetComponent<AnimancerComponent>()
                                ?? _scenePlayer.GetComponentInChildren<AnimancerComponent>();
                    UpdatePlayerSwapBehaviour();
                    TryAutoSelectMotionSet(_scenePlayer);
                    EnsureDebugOverlay();
                    PlayIdleAnimation();
                }
            }
            else
            {
                // Player 비활성화
                if (_scenePlayer != null && Application.isPlaying)
                    _scenePlayer.SetActive(false);
                _targetActor = null;
                _animancer   = null;
                UpdatePlayerSwapBehaviour();
            }

            Repaint();
        }

        void AutoFindPlayer()
        {
            if (string.IsNullOrEmpty(_testActorName)) return;
            var go = GameObject.Find(_testActorName);
            if (go == null)
            {
                Debug.LogWarning($"[MotionEditor] '{_testActorName}'을 씬에서 찾을 수 없습니다.");
                return;
            }
            _scenePlayer = go;
            if (_testActorMode == TestActorMode.Player)
            {
                _targetActor = go;
                _animancer   = go.GetComponent<AnimancerComponent>()
                            ?? go.GetComponentInChildren<AnimancerComponent>();
                UpdatePlayerSwapBehaviour();
                TryAutoSelectMotionSet(go);
                EnsureDebugOverlay();
                PlayIdleAnimation();
                Repaint();
            }
        }

        void DestroySpawnedActor()
        {
            if (_spawnedTestActor == null) return;
            bool wasTarget = _targetActor == _spawnedTestActor;
            UnityEngine.Object.Destroy(_spawnedTestActor);
            _spawnedTestActor = null;
            if (wasTarget)
            {
                _targetActor = null;
                _animancer   = null;
                UpdatePlayerSwapBehaviour();
            }
            StopPlayback();
        }

        void RebuildRegistryNames()
        {
            if (_testRegistry?.entries == null)
            {
                _registryNames = System.Array.Empty<string>();
                return;
            }

            _registryNames = new string[_testRegistry.entries.Count];
            for (int i = 0; i < _testRegistry.entries.Count; i++)
            {
                var entry = _testRegistry.entries[i];
                if (entry.actorDef == null)
                {
                    _registryNames[i] = $"[{i}] (없음)";
                    continue;
                }
                string display = !string.IsNullOrEmpty(entry.actorDef.displayName)
                    ? entry.actorDef.displayName
                    : entry.actorDef.actorId;
                _registryNames[i] = $"[{i}] {display}  ·  {entry.actorDef.actorId}";
            }
        }

        void SpawnRegistryActor(int index)
        {
            if (_testRegistry == null || index < 0 || index >= _testRegistry.entries.Count) return;

            var entry = _testRegistry.entries[index];
            if (entry.actorDef?.prefab == null)
            {
                Debug.LogWarning($"[MotionEditor] 레지스트리 [{index}] '{entry.actorDef?.actorId}'에 prefab이 없습니다.");
                return;
            }

            // 이전 스폰 액터 제거
            if (_spawnedTestActor != null)
            {
                UnityEngine.Object.Destroy(_spawnedTestActor);
                _spawnedTestActor = null;
            }

            // 스폰
            var go = UnityEngine.Object.Instantiate(entry.actorDef.prefab, entry.spawnOffset, Quaternion.identity);
            string label = !string.IsNullOrEmpty(entry.actorDef.displayName)
                ? entry.actorDef.displayName
                : entry.actorDef.actorId;
            go.name = $"[MotionTest] {label}";

            _spawnedTestActor = go;
            _targetActor = go;

            // Other 모드 스폰 시 Player 비활성화
            if (_scenePlayer != null)
                _scenePlayer.SetActive(false);

            // Player 여부에 따라 분기
            var swap = go.GetComponent<PlayerSwapBehaviour>();
            if (swap != null)
            {
                // Player: PlayerSwapBehaviour 로직으로 처리 (모델 전환 + Animancer 갱신)
                UpdatePlayerSwapBehaviour();
            }
            else
            {
                // 비플레이어(Monster, NPC 등): AI/물리 동결 후 AnimancerComponent 직접 탐색
                FreezeTestActor(go);
                _animancer = go.GetComponent<AnimancerComponent>()
                          ?? go.GetComponentInChildren<AnimancerComponent>();
                if (_animancer == null)
                    Debug.LogWarning($"[MotionEditor] '{go.name}'에서 AnimancerComponent를 찾을 수 없습니다.");
            }

            // 엔트리에 Idle 클립이 지정된 경우 적용
            if (entry.idleClip != null)
                _idleAnimation = entry.idleClip;

            TryAutoSelectMotionSet(go);
            EnsureDebugOverlay();
            PlayIdleAnimation();

            Debug.Log($"[MotionEditor] 테스트 액터 스폰: {go.name}");
            Repaint();
        }

        void FreezeTestActor(GameObject go)
        {
            // EnemyBrain / EnemyFlyingBrain 비활성화 → AI 의사결정 중단
            foreach (var brain in go.GetComponentsInChildren<EnemyBrain>(true))
                brain.enabled = false;
            foreach (var brain in go.GetComponentsInChildren<EnemyFlyingBrain>(true))
                brain.enabled = false;

            // KinematicCharacterMotor 비활성화 → 물리 이동 및 상태머신 업데이트 중단
            var movCtrl = go.GetComponent<ActorMovementController>()
                       ?? go.GetComponentInChildren<ActorMovementController>(true);
            if (movCtrl?.Motor != null)
                movCtrl.Motor.enabled = false;
        }

        void CreateTestRegistry()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Motion Test Registry 생성", "MotionTestRegistry", "asset", "저장 위치를 선택하세요.");
            if (string.IsNullOrEmpty(path)) return;

            var registry = ScriptableObject.CreateInstance<MotionTestRegistrySO>();
            AssetDatabase.CreateAsset(registry, path);
            AssetDatabase.SaveAssets();
            _testRegistry = registry;
            _registryNames = null;
            Selection.activeObject = registry;
            EditorGUIUtility.PingObject(registry);
        }

        void DrawActorMotionSidebar()
        {
            const float sidebarWidth = 300f;
            EditorGUILayout.BeginVertical(EditorStyles.helpBox, GUILayout.Width(sidebarWidth));
            {
                EditorGUILayout.LabelField("모션 목록", EditorStyles.boldLabel);

                EditorGUILayout.BeginHorizontal();
                {
                    _actorMotionSearch = EditorGUILayout.TextField(_actorMotionSearch, GUI.skin.FindStyle("ToolbarSearchTextField") ?? EditorStyles.textField);
                    if (GUILayout.Button("×", EditorStyles.miniButton, GUILayout.Width(22)))
                    {
                        _actorMotionSearch = "";
                        GUI.FocusControl(null);
                    }
                }
                EditorGUILayout.EndHorizontal();

                var entries = GetActorMotionEntries(_actorAnimationSet, true);
                if (entries.Count == 0)
                {
                    EditorGUILayout.HelpBox("등록된 모션 키가 없습니다. 아래 버튼으로 추가하세요.", MessageType.Info);
                }

                _actorMotionListScroll = EditorGUILayout.BeginScrollView(_actorMotionListScroll, GUILayout.MinHeight(260));
                {
                    string currentGroup = null;
                    foreach (var entry in entries)
                    {
                        if (!MatchesActorMotionSearch(entry)) continue;

                        string group = GetActorKeyGroupLabel(entry.key);
                        if (group != currentGroup)
                        {
                            currentGroup = group;
                            DrawActorMotionGroupHeader(group);
                        }

                        DrawActorMotionListRow(entry);
                    }
                }
                EditorGUILayout.EndScrollView();

                GUILayout.FlexibleSpace();

                if (GUILayout.Button("+ 키/에셋 추가", GUILayout.Height(26)))
                    ShowAddActorMotionMenu();
            }
            EditorGUILayout.EndVertical();
        }

        bool MatchesActorMotionSearch(ActorMotionEntry entry)
        {
            if (string.IsNullOrWhiteSpace(_actorMotionSearch)) return true;

            string search = _actorMotionSearch.Trim();
            if (entry.key.ToString().IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (entry.asset != null && entry.asset.name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (entry.source != null && entry.source.name.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        void DrawActorMotionGroupHeader(string group)
        {
            Rect rect = EditorGUILayout.GetControlRect(false, 20f);
            EditorGUI.DrawRect(rect, new Color(0.18f, 0.19f, 0.21f));
            GUI.Label(new Rect(rect.x + 6f, rect.y + 2f, rect.width - 12f, 16f), group, EditorStyles.miniBoldLabel);
        }

        void DrawActorMotionListRow(ActorMotionEntry entry)
        {
            bool selected = entry.key == _selectedActorMotionKey && entry.asset == _asset;
            Rect row = EditorGUILayout.GetControlRect(false, 36f);

            Color bg = selected
                ? new Color(0.24f, 0.43f, 0.68f, 0.75f)
                : entry.isOwn
                    ? new Color(0.16f, 0.17f, 0.18f, 0.65f)
                    : new Color(0.12f, 0.12f, 0.13f, 0.55f);

            EditorGUI.DrawRect(row, bg);

            Rect buttonRect = new Rect(row.x, row.y, row.width - 46f, row.height);
            if (GUI.Button(buttonRect, GUIContent.none, GUIStyle.none))
                SelectActorMotionEntry(entry);

            string keyText = entry.key.ToString();
            string assetText = entry.asset != null ? entry.asset.name : "(MotionSet 없음)";
            string sourceText = entry.isOwn ? "" : $"상속: {entry.source.name}";

            var keyStyle = new GUIStyle(EditorStyles.miniBoldLabel)
            {
                normal = { textColor = selected ? Color.white : new Color(0.86f, 0.88f, 0.92f) },
                clipping = TextClipping.Clip
            };
            var assetStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                normal = { textColor = selected ? new Color(0.90f, 0.95f, 1f) : new Color(0.62f, 0.65f, 0.70f) },
                clipping = TextClipping.Clip
            };

            GUI.Label(new Rect(row.x + 8f, row.y + 3f, row.width - 58f, 16f), keyText, keyStyle);
            GUI.Label(new Rect(row.x + 8f, row.y + 18f, row.width - 58f, 14f),
                string.IsNullOrEmpty(sourceText) ? assetText : $"{assetText}  ·  {sourceText}",
                assetStyle);

            EditorGUI.BeginDisabledGroup(entry.asset == null);
            if (GUI.Button(new Rect(row.xMax - 42f, row.y + 8f, 36f, 20f), "Ping", EditorStyles.miniButton))
            {
                Selection.activeObject = entry.asset;
                EditorGUIUtility.PingObject(entry.asset);
            }
            EditorGUI.EndDisabledGroup();
        }

        void SelectActorMotionEntry(ActorMotionEntry entry)
        {
            _selectedActorMotionKey = entry.key;
            SetAsset(entry.asset);
            _useTemporarySet = false;
            Repaint();
        }
        
        // ── 플레이백 컨트롤 ──
        void DrawPlaybackControls()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("플레이 모드에서만 애니메이션을 재생할 수 있습니다.", MessageType.Info);
                return;
            }
            
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("대상 액터", GUILayout.Width(70));
                
                var newTarget = (GameObject)EditorGUILayout.ObjectField(
                    _targetActor, typeof(GameObject), true, GUILayout.Width(200));
                    
                if (newTarget != _targetActor)
                {
                    _targetActor = newTarget;
                    _animancer = _targetActor != null
                        ? _targetActor.GetComponent<AnimancerComponent>()
                          ?? _targetActor.GetComponentInChildren<AnimancerComponent>()
                        : null;
                    UpdatePlayerSwapBehaviour();
                    TryAutoSelectMotionSet(_targetActor);

                    if (_targetActor != null && _animancer == null)
                    {
                        Debug.LogWarning($"{_targetActor.name}에 AnimancerComponent가 없습니다!");
                    }
                    else if (_animancer != null)
                    {
                        EnsureDebugOverlay();
                        PlayIdleAnimation();
                    }
                }
                
                // Hierarchy에서 선택한 오브젝트 사용 버튼
                if (GUILayout.Button("선택 사용", GUILayout.Width(70)))
                {
                    if (Selection.activeGameObject != null)
                    {
                        var animancer = Selection.activeGameObject.GetComponent<AnimancerComponent>()
                                     ?? Selection.activeGameObject.GetComponentInChildren<AnimancerComponent>();
                        if (animancer != null)
                        {
                            _targetActor = Selection.activeGameObject;
                            _animancer = animancer;
                            UpdatePlayerSwapBehaviour();
                            TryAutoSelectMotionSet(_targetActor);
                            EnsureDebugOverlay();
                            Debug.Log($"{_targetActor.name}을(를) 대상 액터로 설정했습니다.");
                            PlayIdleAnimation();
                        }
                        else
                        {
                            Debug.LogWarning($"{Selection.activeGameObject.name}에 AnimancerComponent가 없습니다!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Hierarchy에서 GameObject를 선택해주세요!");
                    }
                }
                
                GUILayout.FlexibleSpace();
                
                // 플레이 상태 표시
  if (!_isPlaying)
                {
                    GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
                    if (GUILayout.Button("▶ 재생", GUILayout.Width(60)))
                        StartPlayback();
                    GUI.backgroundColor = Color.white;
                }
                else
                {
                    if (_isPaused)
                    {
                        GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
                        if (GUILayout.Button("▶ 계속", GUILayout.Width(60)))
                            ResumePlayback();
                        GUI.backgroundColor = Color.white;
                    }
                    else
                    {
                        GUI.backgroundColor = new Color(1f, 0.9f, 0.5f);
                        if (GUILayout.Button("|| 일시정지", GUILayout.Width(70)))
                            PausePlayback();
                        GUI.backgroundColor = Color.white;
                    }
                    
                    GUI.backgroundColor = new Color(1f, 0.5f, 0.5f);
                    if (GUILayout.Button("■ 중지", GUILayout.Width(60)))
                        StopPlayback();
                    GUI.backgroundColor = Color.white;
                }

                EditorGUI.BeginDisabledGroup(!_isPlaying);
                if (GUILayout.Button("리셋", GUILayout.Width(50)))
                {
                    float loopStart = Mathf.Max(0f, _startTime);
                    _playbackTime = loopStart;
                    _previousTime = loopStart - 0.001f;
                    _drawer.cursorTime = loopStart;
                    _executedEvents?.Clear();
                    UpdateAnimancerPlayback();
                }
                EditorGUI.EndDisabledGroup();
                
                if (_isPlaying)
                {
                    string statusText = _isPaused ? "일시정지" : "재생 중";
                    EditorGUILayout.LabelField($"{statusText}: {_playbackTime:F2}s", GUILayout.Width(120));
                }
            }
            EditorGUILayout.EndHorizontal();


            // 재생 속도 + 루프 컨트롤
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("재생 속도", GUILayout.Width(70));

                float newSpeed = EditorGUILayout.Slider(_playbackSpeed, 0.1f, 3f);
                if (!Mathf.Approximately(newSpeed, _playbackSpeed))
                {
                    _playbackSpeed = newSpeed;
                    UpdateAnimancerPlaybackSpeed();
                }

                EditorGUILayout.LabelField($"×{_playbackSpeed:F2}", GUILayout.Width(50));

                if (GUILayout.Button("1×", GUILayout.Width(40)))
                {
                    _playbackSpeed = 1f;
                    UpdateAnimancerPlaybackSpeed();
                }

                if (GUILayout.Button("0.5×", GUILayout.Width(50)))
                {
                    _playbackSpeed = 0.5f;
                    UpdateAnimancerPlaybackSpeed();
                }

                if (GUILayout.Button("2×", GUILayout.Width(40)))
                {
                    _playbackSpeed = 2f;
                    UpdateAnimancerPlaybackSpeed();
                }

                GUILayout.Space(10);

                bool newLoop = EditorGUILayout.ToggleLeft("루프", _isLooping, GUILayout.Width(55));
                if (newLoop != _isLooping)
                    _isLooping = newLoop;

                GUILayout.Space(10);

                // ③ 프레임 스텝 버튼
                float frameStep = _drawer.fps > 0 ? 1f / _drawer.fps : 1f / 30f;
                var motionSetForStep = GetCurrentMotionSet();
                float totalDurForStep = motionSetForStep?.TotalDuration ?? 1f;

                EditorGUI.BeginDisabledGroup(motionSetForStep == null);

                // |◀ 처음으로
                if (GUILayout.Button("|◀", GUILayout.Width(30)))
                {
                    _playbackTime  = _startTime;
                    _previousTime  = _startTime - 0.001f;
                    _drawer.cursorTime = _playbackTime;
                    _executedEvents?.Clear();
                    UpdateAnimancerPlayback();
                    Repaint();
                }
                // ◀ 한 프레임 뒤로
                if (GUILayout.Button("◀", GUILayout.Width(26)))
                {
                    _playbackTime  = Mathf.Max(0f, _playbackTime - frameStep);
                    _previousTime  = _playbackTime - 0.001f;
                    _drawer.cursorTime = _playbackTime;
                    _executedEvents?.Clear();
                    UpdateAnimancerPlayback();
                    Repaint();
                }
                // ▶ 한 프레임 앞으로
                if (GUILayout.Button("▶", GUILayout.Width(26)))
                {
                    _playbackTime  = Mathf.Min(totalDurForStep, _playbackTime + frameStep);
                    _previousTime  = _playbackTime - 0.001f;
                    _drawer.cursorTime = _playbackTime;
                    _executedEvents?.Clear();
                    UpdateAnimancerPlayback();
                    Repaint();
                }
                // ▶| 끝으로
                if (GUILayout.Button("▶|", GUILayout.Width(30)))
                {
                    float endPos   = GetEffectiveEndTime(motionSetForStep);
                    _playbackTime  = endPos;
                    _previousTime  = endPos - 0.001f;
                    _drawer.cursorTime = _playbackTime;
                    _executedEvents?.Clear();
                    UpdateAnimancerPlayback();
                    Repaint();
                }

                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();

            // 시작/종료 지점 컨트롤
            {
                var motionSet = GetCurrentMotionSet();
                float totalDuration = motionSet != null ? motionSet.TotalDuration : 1f;
                float maxEnd = totalDuration;

                EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
                {
                    EditorGUILayout.LabelField("구간", GUILayout.Width(35));

                    EditorGUILayout.LabelField("시작", GUILayout.Width(30));
                    float newStart = EditorGUILayout.Slider(_startTime, 0f, maxEnd, GUILayout.MinWidth(80));
                    float effectiveEnd = motionSet != null ? GetEffectiveEndTime(motionSet) : totalDuration;
                    newStart = Mathf.Min(newStart, effectiveEnd - 0.01f);

                    GUILayout.Space(8);

                    // 종료 지점 (-1 = 끝까지)
                    float displayEnd = _endTime > 0f ? _endTime : totalDuration;
                    EditorGUILayout.LabelField("종료", GUILayout.Width(30));
                    float newEnd = EditorGUILayout.Slider(displayEnd, 0f, maxEnd, GUILayout.MinWidth(80));
                    newEnd = Mathf.Max(newEnd, _startTime + 0.01f);
                    // totalDuration에 매우 가까우면 -1 (끝까지)로 처리
                    float rawEnd = Mathf.Approximately(newEnd, totalDuration) ? -1f : newEnd;

                    if (!Mathf.Approximately(newStart, _startTime) || rawEnd != _endTime)
                    {
                        _startTime = newStart;
                        _endTime   = rawEnd;
                        // ④ Drawer에 재생 구간 동기화
                        _drawer.playRangeStart = _startTime;
                        _drawer.playRangeEnd   = _endTime;
                    }

                    GUILayout.Space(8);

                    if (GUILayout.Button("초기화", GUILayout.Width(55)))
                    {
                        _startTime = 0f;
                        _endTime   = -1f;
                        // ④ Drawer에 재생 구간 동기화
                        _drawer.playRangeStart = 0f;
                        _drawer.playRangeEnd   = -1f;
                    }

                    // 현재 구간 표시
                    float shownEnd = _endTime > 0f ? _endTime : totalDuration;
                    EditorGUILayout.LabelField(
                        $"{_startTime:F2}s ~ {shownEnd:F2}s",
                        GUILayout.Width(120));
                }
                EditorGUILayout.EndHorizontal();
            }
            
            // Idle 애니메이션 설정
            EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
            {
                EditorGUILayout.LabelField("Idle 애니메이션", GUILayout.Width(100));
                
                var newIdle = (AnimationClip)EditorGUILayout.ObjectField(
                    _idleAnimation, typeof(AnimationClip), false, GUILayout.Width(200));
                    
                if (newIdle != _idleAnimation)
                {
                    _idleAnimation = newIdle;
                    if (!_isPlaying && _animancer != null)
                    {
                        PlayIdleAnimation();
                    }
                }
                
                GUILayout.FlexibleSpace();
                
                EditorGUI.BeginDisabledGroup(_animancer == null || _idleAnimation == null || _isPlaying);
                if (GUILayout.Button("Idle 재생", GUILayout.Width(80)))
                {
                    PlayIdleAnimation();
                }
                EditorGUI.EndDisabledGroup();
            }
            EditorGUILayout.EndHorizontal();
        }
        
        void StartPlayback()
        {
            if (_animancer == null || GetCurrentMotionSet() == null) return;

            var motionSet = GetCurrentMotionSet();
            AcquireMotionToolInputLock();

            // ④ 재생 구간을 Drawer에 동기화
            _drawer.playRangeStart = _startTime;
            _drawer.playRangeEnd   = _endTime;

            // 시작 지점을 유효 범위로 클램프
            float totalDuration = motionSet.TotalDuration;
            float loopStart = Mathf.Clamp(_startTime, 0f, totalDuration);

            _isPlaying = true;
            _isPaused  = false;
            _currentMotionIndex = -1;
            _playbackTime = loopStart;
            _previousTime = loopStart - 0.001f;
            _drawer.cursorTime = loopStart;
            ResetEditorLoopState();

            // 이벤트 실행 기록 초기화
            _executedEvents = new System.Collections.Generic.HashSet<MotionEventBase>();
            _activeEvents   = new System.Collections.Generic.HashSet<MotionEventBase>();
            _eventLog.Clear();
            EnsureDebugOverlay();
            PublishEventDebugState();

            // Animancer로 모션 셋 재생 시작
            if (motionSet.motions != null && motionSet.motions.Count > 0)
            {
                if (loopStart > 0f)
                    UpdateAnimancerPlayback();
                else
                    PlayMotionSet(motionSet);

                // 초기 모션 인덱스 기록
                if (motionSet.GetMotionAtTime(_playbackTime, out int startIdx, out _))
                    _currentMotionIndex = startIdx;
            }
        }
        void PausePlayback()
        {
            _isPaused = true;
            
            if (_animancer != null && _animancer.States.Current != null)
            {
                _animancer.States.Current.Speed = 0f;
            }
        }
        
        void ResumePlayback()
        {
            _isPaused = false;
            UpdateAnimancerPlaybackSpeed();
        }
        
        void StopPlayback()
        {
            _isPlaying = false;
            _isPaused = false;
            ReleaseMotionToolInputLock();
            ResetEditorLoopState();
            
            // 활성 중인 모든 이벤트 강제 종료 처리
            if (_activeEvents != null && _targetActor != null)
            {
                foreach (var evt in _activeEvents)
                {
                    evt.OnCompleteEvent(_targetActor);
                }
                _activeEvents.Clear();
            }
            
            if (_animancer != null)
            {
                // MotionSet 재생 중지 후 Idle로 전환
                PlayIdleAnimation();
            }

            MotionSetEventDebugOverlay.Clear();
        }
        
        void PlayMotionSet(MotionSet motionSet)
        {
            if (_animancer == null || motionSet.motions == null || motionSet.motions.Count == 0)
                return;

            // 첫 번째 유효한 모션을 찾아 재생 시작 (전환은 OnEditorUpdate가 전담)
            for (int i = 0; i < motionSet.motions.Count; i++)
            {
                var m = motionSet.motions[i];
                if (m == null || !m.IsValid()) continue;
                PlayMotionClip(m);
                break;
            }
        }

        /// <summary>
        /// 특정 Motion 클립을 ClipStartTime부터 재생.
        /// Animancer OnEnd는 등록하지 않음 — 모션 전환/종료는 OnEditorUpdate가 전담.
        /// </summary>
        void PlayMotionClip(Motion motion)
        {
            if (_animancer == null || motion == null || !motion.IsValid()) return;

            ForceDrawPlayerWeapons();
            var state = _animancer.Play(motion.motionClip);
            state.Time  = motion.ClipStartTime;
            state.Speed = motion.playbackSpeed * _playbackSpeed;
            ForceDrawPlayerWeapons();

            // Animancer 자체 OnEnd 완전 제거 — 에디터 타임라인이 종료를 관리
            state.Events(this).OnEnd = null;
        }
        // 재생 속도 업데이트 (글로벌 슬라이더 변경 시)
        void UpdateAnimancerPlaybackSpeed()
        {
            if (_animancer == null || _animancer.States.Current == null) return;

            if (_isPaused)
            {
                _animancer.States.Current.Speed = 0f;
                return;
            }

            // 현재 모션의 개별 속도 반영
            var motionSet = GetCurrentMotionSet();
            float motionSpd = 1f;
            if (motionSet != null &&
                motionSet.GetMotionAtTime(_playbackTime, out int idx, out _) &&
                idx >= 0 && idx < motionSet.motions.Count)
            {
                var m = motionSet.motions[idx];
                if (m != null) motionSpd = m.playbackSpeed;
            }

            _animancer.States.Current.Speed = motionSpd * _playbackSpeed;
        }
            
        // 특정 시간으로 재생 위치 이동
        void SeekToTime(float time)
        {
            if (_animancer == null || GetCurrentMotionSet() == null) return;
      
            ResetEditorLoopState();

            // Seek 시에는 현재 활성 이벤트를 모두 종료하고 상태를 리셋함
            if (_activeEvents != null && _targetActor != null)
            {
                foreach (var evt in _activeEvents)
                {
                    evt.OnCompleteEvent(_targetActor);
                }
                _activeEvents.Clear();
            }
            
            var motionSet = GetCurrentMotionSet();
            float totalDuration = motionSet.TotalDuration;
            
            _playbackTime = Mathf.Clamp(time, 0f, totalDuration);
            _drawer.cursorTime = _playbackTime;
            
            // 이벤트 실행 기록 초기화
            _executedEvents?.Clear();
            
            ExecuteActiveEvents(GetCurrentMotionSet());
            
            // Animancer 상태 업데이트
            UpdateAnimancerPlayback();
            
            Repaint();
        }
        
        // Animancer 재생 위치 업데이트 (Seek / 초기 배치용)
        void UpdateAnimancerPlayback()
        {
            if (_animancer == null || GetCurrentMotionSet() == null) return;

            var motionSet = GetCurrentMotionSet();

            if (motionSet.GetMotionAtTime(_playbackTime, out int motionIndex, out float localTime))
            {
                if (motionIndex >= 0 && motionIndex < motionSet.motions.Count)
                {
                    var motion = motionSet.motions[motionIndex];
                    if (motion != null && motion.IsValid())
                    {
                        var state = _animancer.Play(motion.motionClip);
                        // localTime은 타임라인 상 이 모션 내 오프셋.
                        // 실제 클립 시간 = ClipStartTime + localTime * playbackSpeed
                        float spd      = motion.playbackSpeed > 0f ? motion.playbackSpeed : 1f;
                        state.Time     = motion.ClipStartTime + localTime * spd;
                        state.Speed    = _isPaused ? 0f : motion.playbackSpeed * _playbackSpeed;
                        state.Events(this).OnEnd = null;
                    }
                }
            }
        }
        
        // 타임라인 클릭으로 재생 위치 조절
        void HandleTimelineScrubbing()
        {
            if (!_isPlaying) return;
            
            // Drawer의 커서 시간이 변경되었고, 드래그 중이면
            if (_drawer.isDraggingCursor)
            {
                SeekToTime(_drawer.cursorTime);
            }
        }
        
        // ── 이벤트 실행 ──
        void ExecuteActiveEvents(MotionSet motionSet)
        {
            if (_targetActor == null || motionSet == null) return;

            if (motionSet.globalEvents != null)
            {
                foreach (var evt in motionSet.globalEvents)
                {
                    if (evt == null) continue;
                    TryStartEvent(evt, evt.startTime);
                }
            }

            float tOff = 0f; // 현재 모션이 시작되는 절대 시간 오프셋
            foreach (var motion in motionSet.motions)
            {
                if (motion == null) continue;

                foreach (var evt in motion.events)
                {
                    if (evt == null) continue;
                    TryStartEvent(evt, tOff + evt.startTime);
                }
                tOff += motion.Duration; // 다음 모션을 위해 길이 누적
            }
    
            ProcessCompletedEvents(motionSet);
            PublishEventDebugState();
        }

        void TryStartEvent(MotionEventBase evt, float eventGlobalStart)
        {
            bool justStarted = eventGlobalStart > _previousTime && eventGlobalStart <= _playbackTime;
            if (!justStarted || _executedEvents.Contains(evt)) return;

            try
            {
                evt.Execute(_targetActor);
                _executedEvents.Add(evt);
                _activeEvents.Add(evt);
                RecordEventLog($"Start {evt.GetShortLabel()} @{_playbackTime:F2}s");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"이벤트 실행 중 오류: {evt.GetDisplayName()}\n{e.Message}");
            }
        }
        
        void ProcessCompletedEvents(MotionSet motionSet)
        {
            var toRemove = new System.Collections.Generic.List<MotionEventBase>();
    
            foreach (var evt in _activeEvents)
            {
                // MotionSet의 헬퍼 메서드를 사용하여 해당 이벤트가 현재 '글로벌 시간'에서 활성 상태인지 확인
                // MotionSet.GetActiveEventsAt 내부에서 이미 오프셋 계산을 처리하고 있음
                var currentActiveOnes = motionSet.GetActiveEventsAt(_playbackTime);
        
                if (!currentActiveOnes.Contains(evt))
                {
                    try
                    {
                        evt.OnCompleteEvent(_targetActor);
                        toRemove.Add(evt);
                        RecordEventLog($"Complete {evt.GetShortLabel()} @{_playbackTime:F2}s");
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"이벤트 종료 중 오류: {e.Message}");
                    }
                }
            }

            foreach (var evt in toRemove)
            {
                _activeEvents.Remove(evt);
            }
        }
        
        // 이벤트를 실행하고 기록하는 헬퍼 메서드
        void CheckAndExecuteEvent(MotionEventBase evt, float prevTime, float currTime)
        {
            // 이벤트의 시작 시간이 이전 프레임과 현재 프레임 사이에 있는지 확인
            bool justStarted = evt.startTime >= prevTime && evt.startTime <= currTime;

            if (justStarted && !_executedEvents.Contains(evt))
            {
                try
                {
                    evt.Execute(_targetActor);
                    _executedEvents.Add(evt);
                    _activeEvents.Add(evt);
                    RecordEventLog($"Start {evt.GetShortLabel()} @{currTime:F2}s");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"이벤트 실행 중 오류: {evt.GetDisplayName()}\n{e.Message}");
                }
            }
        }
        
        // ── 에셋 미선택 상태 ──
        void DrawEmptyState()
        {
            GUILayout.FlexibleSpace();
            EditorGUILayout.BeginHorizontal();
            {
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginVertical();
                {
                    var style = new GUIStyle(EditorStyles.largeLabel)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize  = 14,
                        normal    = { textColor = new Color(0.6f, 0.6f, 0.6f) }
                    };
                    EditorGUILayout.LabelField("MotionSetAsset을 선택하거나", style);
                    EditorGUILayout.LabelField("새로 만들어 주세요.", style);

                    EditorGUILayout.Space(12);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();

                    // 드래그 앤 드롭 영역
                    Rect dropRect = GUILayoutUtility.GetRect(260, 60);
                    GUI.Box(dropRect, "여기에 MotionSetAsset 드래그");
                    HandleDragDrop(dropRect);

                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();

                    EditorGUILayout.Space(8);

                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("새 MotionSet 에셋 생성", GUILayout.Width(200), GUILayout.Height(30)))
                        CreateNewAsset();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                    
                    EditorGUILayout.Space(8);
                    
                    EditorGUILayout.BeginHorizontal();
                    GUILayout.FlexibleSpace();
                    if (GUILayout.Button("임시 MotionSet 생성", GUILayout.Width(200), GUILayout.Height(30)))
                        CreateTemporarySet();
                    GUILayout.FlexibleSpace();
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.EndVertical();
                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndHorizontal();
            GUILayout.FlexibleSpace();
        }

        // ── 드래그 앤 드롭 ──
        void HandleDragDrop(Rect rect)
        {
            Event e = Event.current;
            if ((e.type == EventType.DragUpdated || e.type == EventType.DragPerform) && rect.Contains(e.mousePosition))
            {
                bool hasValid = false;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is MotionSetAsset) { hasValid = true; break; }
                }

                if (hasValid)
                {
                    DragAndDrop.visualMode = DragAndDropVisualMode.Link;

                    if (e.type == EventType.DragPerform)
                    {
                        DragAndDrop.AcceptDrag();
                        foreach (var obj in DragAndDrop.objectReferences)
                        {
                            if (obj is MotionSetAsset asset)
                            {
                                SetAsset(asset);
                                _useTemporarySet = false;
                                break;
                            }
                        }
                    }
                }
                e.Use();
            }
        }

        // ── 새 에셋 생성 ──
        void CreateNewAsset()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "MotionSet 에셋 생성", "NewMotionSet", "asset", "저장 위치를 선택하세요.");

            if (string.IsNullOrEmpty(path)) return;

            var asset = CreateInstance<MotionSetAsset>();
            asset.motionSet = new MotionSet { motionSetName = System.IO.Path.GetFileNameWithoutExtension(path) };

            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            SetAsset(asset);
            _useTemporarySet = false;
            Selection.activeObject = asset;
            EditorGUIUtility.PingObject(asset);
        }
        
        // ── 임시 셋 생성 ──
        void CreateTemporarySet()
        {
            _temporarySet = new MotionSet
            {
                motionSetName = "임시 MotionSet",
                motions = new System.Collections.Generic.List<Motion>()
            };
            
            _useTemporarySet = true;
            _asset = null;
            _drawer = new MotionSetDrawer(() => null, Repaint, OnSelectedMotionChanged);
            
            Debug.Log("임시 MotionSet이 생성되었습니다. 에셋으로 저장하려면 '새로 만들기'를 사용하세요.");
        }
        
        // ── 테스트 씬 플레이 ──
        void PlayTestScene()
        {
            // 씬 경로 유효성 검사
            if (string.IsNullOrEmpty(_testScenePath))
            {
                Debug.LogError("테스트 씬 경로가 설정되지 않았습니다!");
                return;
            }
            
            // 씬 파일 존재 확인
            var sceneAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEditor.SceneAsset>(_testScenePath);
            if (sceneAsset == null)
            {
                Debug.LogError($"씬을 찾을 수 없습니다: {_testScenePath}");
                ShowTestSceneSettings();
                return;
            }
            
            // 현재 씬이 저장되지 않았으면 저장 확인
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().isDirty)
            {
                if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    LoadTestSceneAndPlay();
                }
            }
            else
            {
                LoadTestSceneAndPlay();
            }
        }
        
        void LoadTestSceneAndPlay()
        {
            // 씬 로드
            if (UnityEditor.SceneManagement.EditorSceneManager.OpenScene(_testScenePath, 
                UnityEditor.SceneManagement.OpenSceneMode.Single) != null)
            {
                Debug.Log($"테스트 씬 로드: {_testScenePath}");
                
                // 플레이 모드 시작
                EditorApplication.isPlaying = true;
            }
            else
            {
                Debug.LogError($"씬 로드 실패: {_testScenePath}");
            }
        }
        
        void ShowTestSceneSettings()
        {
            TestSceneSettingsWindow.ShowWindow(_testScenePath, _testActorName, _idleAnimation, 
                (scenePath, actorName, idleAnim) =>
            {
                _testScenePath = scenePath;
                _testActorName = actorName;
                _idleAnimation = idleAnim;
                Repaint();
            });
        }
    }
    
    /// <summary>
    /// 테스트 씬 설정 윈도우
    /// </summary>
    public class TestSceneSettingsWindow : EditorWindow
    {
        string _scenePath;
        string _actorName;
        AnimationClip _idleAnimation;
        System.Action<string, string, AnimationClip> _onSave;
        
        public static void ShowWindow(string currentScenePath, string currentActorName, 
            AnimationClip currentIdleAnimation, System.Action<string, string, AnimationClip> onSave)
        {
            var window = GetWindow<TestSceneSettingsWindow>(true, "테스트 씬 설정", true);
            window.minSize = new Vector2(400, 230);
            window.maxSize = new Vector2(400, 230);
            window._scenePath = currentScenePath;
            window._actorName = currentActorName;
            window._idleAnimation = currentIdleAnimation;
            window._onSave = onSave;
            window.ShowUtility();
        }
        
        void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            EditorGUILayout.LabelField("테스트 씬 설정", EditorStyles.boldLabel);
            
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("씬 플레이 버튼을 누르면 지정한 씬을 열고 플레이 모드로 진입합니다.", MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // 씬 경로 설정
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("테스트 씬", GUILayout.Width(80));
            _scenePath = EditorGUILayout.TextField(_scenePath);
            
            if (GUILayout.Button("찾기", GUILayout.Width(50)))
            {
                string path = EditorUtility.OpenFilePanel("테스트 씬 선택", "Assets/Scenes", "unity");
                if (!string.IsNullOrEmpty(path))
                {
                    // 절대 경로를 Assets 상대 경로로 변환
                    if (path.StartsWith(Application.dataPath))
                    {
                        _scenePath = "Assets" + path.Substring(Application.dataPath.Length);
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            
            // 액터 이름 설정
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("대상 액터", GUILayout.Width(80));
            _actorName = EditorGUILayout.TextField(_actorName);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("대상 액터는 씬 내 GameObject 이름입니다. (예: Player, Character)", MessageType.None);
            
            EditorGUILayout.Space(10);
            
            // Idle 애니메이션 설정
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Idle 애니메이션", GUILayout.Width(100));
            _idleAnimation = (AnimationClip)EditorGUILayout.ObjectField(
                _idleAnimation, typeof(AnimationClip), false);
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("MotionSet이 재생되지 않을 때 자동으로 재생되는 애니메이션입니다.", MessageType.None);
            
            EditorGUILayout.Space(15);
            
            // 버튼
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            
            if (GUILayout.Button("저장", GUILayout.Width(100)))
            {
                _onSave?.Invoke(_scenePath, _actorName, _idleAnimation);
                Close();
            }
            
            if (GUILayout.Button("취소", GUILayout.Width(100)))
            {
                Close();
            }
            
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
        }
    }
}
