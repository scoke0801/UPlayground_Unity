using UnityEditor;
using UnityEngine;
using Animancer;
using UPlayGround.Data.Event;

namespace UPlayGround.Animation.Editor
{
    public class MotionSetEditorWindow : EditorWindow
    {
        MotionSetAsset  _asset;
        MotionSetDrawer _drawer;
        Vector2         _scrollPos;
        
        // 테스트 씬 설정
        string          _testScenePath = "Assets/01.Scenes/MotionTestMap.unity"; // 기본 경로
        string          _testActorName = "TestPlayer"; // 기본 액터 이름
        public AnimationClip   _idleAnimation; // 기본 Idle 애니메이션
        
        // 플레이 관련
        GameObject      _targetActor;
        AnimancerComponent _animancer;
        bool            _isPlaying;
        bool            _isPaused;
        float           _playbackTime;
        float           _previousTime;
        float           _playbackSpeed = 1f; 
        
        // 이벤트 재생 관리
        System.Collections.Generic.HashSet<MotionEventBase> _executedEvents;   
        System.Collections.Generic.HashSet<MotionEventBase> _activeEvents;
        
        // 임시 MotionSet (에셋이 없을 때)
        bool            _useTemporarySet;
        MotionSet       _temporarySet;

        [MenuItem("UPlayGround/모션 셋 에디터")]
        static void Open()
        {
            var window = GetWindow<MotionSetEditorWindow>();
            window.titleContent = new GUIContent("모션 셋 에디터");
            window.minSize      = new Vector2(600, 400);
            window.Show();
        }

        void OnEnable()
        {
            _drawer = new MotionSetDrawer(() => _asset, Repaint);

            // Selection이 MotionSetAsset이면 자동 바인딩
            TryBindFromSelection();
            
            // 플레이 업데이트 등록
            EditorApplication.update += OnEditorUpdate;
            
            // 플레이 모드 변경 감지
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
        
        void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            StopPlayback();
        }
        
        void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // 플레이 모드 진입 완료 시
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                // 잠시 대기 후 대상 액터 찾기 (씬 로딩 완료 대기)
                EditorApplication.delayCall += () =>
                {
                    FindAndSetTargetActor();
                };
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
            
            _animancer.Play(_idleAnimation);
            Debug.Log($"Idle 애니메이션 재생: {_idleAnimation.name}");
        }
        
        void OnEditorUpdate()
        {
            if (_isPlaying && !_isPaused && Application.isPlaying && _animancer != null)
            {
                float deltaTime = Time.deltaTime * _playbackSpeed;
                _playbackTime += deltaTime;
                
                var currentSet = GetCurrentMotionSet();
                if (currentSet != null)
                {
                    float totalDuration = currentSet.TotalDuration;
                    if (_playbackTime >= totalDuration)
                    {
                        _playbackTime = totalDuration;
                        StopPlayback();
                    }
                    
                    // 이벤트 실행
                    ExecuteActiveEvents(currentSet);
                    
                    _drawer.cursorTime = _playbackTime;
                    _previousTime = _playbackTime;
                    Repaint();
                }
            }
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
                    
                    // 대상 액터 변경 시 Idle 재생
                    if (!_isPlaying)
                    {
                        PlayIdleAnimation();
                    }
                }
            }
            
            Repaint();
        }

        void TryBindFromSelection()
        {
            if (Selection.activeObject is MotionSetAsset selected)
            {
                SetAsset(selected);
                _useTemporarySet = false;
            }
        }

        void SetAsset(MotionSetAsset asset)
        {
            if (_asset == asset) return;
            _asset  = asset;
            _drawer = new MotionSetDrawer(() => _asset, Repaint);
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
            DrawPlaybackControls();

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
                    _animancer = _targetActor?.GetComponent<AnimancerComponent>();
                    
                    if (_targetActor != null && _animancer == null)
                    {
                        Debug.LogWarning($"{_targetActor.name}에 AnimancerComponent가 없습니다!");
                    }
                    else if (_animancer != null)
                    {
                        PlayIdleAnimation();
                    }
                }
                
                // Hierarchy에서 선택한 오브젝트 사용 버튼
                if (GUILayout.Button("선택 사용", GUILayout.Width(70)))
                {
                    if (Selection.activeGameObject != null)
                    {
                        var animancer = Selection.activeGameObject.GetComponent<AnimancerComponent>();
                        if (animancer != null)
                        {
                            _targetActor = Selection.activeGameObject;
                            _animancer = animancer;
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
                
                EditorGUI.EndDisabledGroup();
                
                EditorGUI.BeginDisabledGroup(!_isPlaying);
                if (GUILayout.Button("리셋", GUILayout.Width(50)))
                {
                    _playbackTime = 0f;
                    _previousTime = 0f;
                    _drawer.cursorTime = 0f;
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
            
            // 재생 속도 컨트롤
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
            }
            EditorGUILayout.EndHorizontal();
            
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
            
            _isPlaying = true;
            _isPaused = false;
            _playbackTime = 0f;
            _previousTime = -0.001f;
            _drawer.cursorTime = 0f;
            
            // 이벤트 실행 기록 초기화
            _executedEvents = new System.Collections.Generic.HashSet<MotionEventBase>();
            _activeEvents = new System.Collections.Generic.HashSet<MotionEventBase>();
            
            var motionSet = GetCurrentMotionSet();
            
            // Animancer로 모션 셋 재생 시작
            if (motionSet.motions != null && motionSet.motions.Count > 0)
            {
                PlayMotionSet(motionSet);
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
        }
        
        void PlayMotionSet(MotionSet motionSet)
        {
            if (_animancer == null || motionSet.motions == null || motionSet.motions.Count == 0)
                return;
                
            // 첫 번째 모션부터 순차 재생
            PlayMotionAtIndex(motionSet, 0);
        }
        
        void PlayMotionAtIndex(MotionSet motionSet, int index)
        {
            if (index >= motionSet.motions.Count)
            {
                StopPlayback();
                return;
            }
            
            var motion = motionSet.motions[index];
            if (motion == null || !motion.IsValid())
            {
                PlayMotionAtIndex(motionSet, index + 1);
                return;
            }
            
            var state = _animancer.Play(motion.motionClip);
            state.Speed = _playbackSpeed;  // 재생 속도 적용

            // 다음 모션으로 자동 전환
            if (index + 1 < motionSet.motions.Count)
            {
                state.Events(this).OnEnd = () => PlayMotionAtIndex(motionSet, index + 1);
            }
            else
            {
                state.Events(this).OnEnd = () => StopPlayback();
            }
        }
        // 재생 속도 업데이트
        void UpdateAnimancerPlaybackSpeed()
        {
            if (_animancer != null && _animancer.States.Current != null && !_isPaused)
            {
                _animancer.States.Current.Speed = _playbackSpeed;
            }
        }
            
        // 특정 시간으로 재생 위치 이동
        void SeekToTime(float time)
        {
            if (_animancer == null || GetCurrentMotionSet() == null) return;
      
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
        
        // Animancer 재생 위치 업데이트
        void UpdateAnimancerPlayback()
        {
            if (_animancer == null || GetCurrentMotionSet() == null) return;
            
            var motionSet = GetCurrentMotionSet();
            
            // 현재 시간에 해당하는 모션 찾기
            if (motionSet.GetMotionAtTime(_playbackTime, out int motionIndex, out float localTime))
            {
                if (motionIndex >= 0 && motionIndex < motionSet.motions.Count)
                {
                    var motion = motionSet.motions[motionIndex];
                    if (motion != null && motion.IsValid())
                    {
                        var state = _animancer.Play(motion.motionClip);
                        state.Time = localTime;
                        state.Speed = _isPaused ? 0f : _playbackSpeed;
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

            float tOff = 0f; // 현재 모션이 시작되는 절대 시간 오프셋
            foreach (var motion in motionSet.motions)
            {
                if (motion == null) continue;

                foreach (var evt in motion.events)
                {
                    if (evt == null) continue;

                    // 이벤트의 절대 시작 시간 계산
                    float eventGlobalStart = tOff + evt.startTime;

                    // 이벤트의 '절대 시작 시간'이 '이전 프레임'과 '현재 프레임' 사이에 있는가?
                    bool justStarted = eventGlobalStart > _previousTime && eventGlobalStart <= _playbackTime;

                    if (justStarted && !_executedEvents.Contains(evt))
                    {
                        evt.Execute(_targetActor);
                        _executedEvents.Add(evt);
                        _activeEvents.Add(evt);
                    }
                }
                tOff += motion.Duration; // 다음 모션을 위해 길이 누적
            }
    
            ProcessCompletedEvents(motionSet);
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
                        Debug.Log($"[MotionEvent] Complete: {evt.GetDisplayName()} at {_playbackTime:F2}s");
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
                    Debug.Log($"[MotionEvent] Start: {evt.GetDisplayName()} at {currTime:F2}s");
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
            _drawer = new MotionSetDrawer(() => null, Repaint);
            
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