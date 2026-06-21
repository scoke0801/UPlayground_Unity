using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.CameraSystem;
using UPlayGround.Data;
using UPlayGround.Data.Config;
using UPlayGround.Data.Path;
using UPlayGround.InputDefine;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Manager
{
    /// <summary>
    /// TPS 카메라 오케스트레이터.
    /// 실제 로직은 서브시스템(CameraLockOn, CameraCollision 등)에 위임한다.
    /// </summary>
    public class CameraManager : BaseManager<CameraManager>, IManager, ICameraStateAccessor,
        IAsyncInitializableManager, IUpdatableManager, ILateUpdatableManager
    {
        [SerializeField] private CameraSettings settings;

        // 등록할 카메라 모드는 CameraSettings SO(settings.enabledModes)에서 읽는다.
        // CameraManager는 런타임 생성될 수 있어 자체 SerializeField로는 설정할 수 없으므로,
        // Addressable로 로드되는 SO를 단일 설정 소스로 사용한다. 비어 있으면 아래 기본값을 쓴다.
        private static readonly CameraModeType[] DefaultEnabledModes =
        {
            CameraModeType.InGame,
            CameraModeType.Free,
            CameraModeType.Dialogue,
            CameraModeType.CameraSnapshotSequence,
            CameraModeType.DialogueCameraReplay,
        };

        private const string SETTINGS_ADDRESSABLE_KEY = "CameraSettings";
        private const string CAMERA_SHAKE_DB_KEY      = "CameraShakeDatabase";
        private const string KILL_CAM_DATA_KEY         = "KillCamData";
        private const string COMBAT_CAMERA_PROFILE_DB_KEY = "CombatCameraProfileDatabase";
        private const string PERFECT_GUARD_FOV_KEY     = "PerfectGuardFOV";
        private const string DIALOGUE_CAMERA_SETTINGS_KEY = "DialogueCameraSettings";

        private CameraLockOn             _lockOn;
        private CameraCollision          _collision;
        private CameraDistanceController _distanceCtrl;
        private CameraRotationTransition _rotTransition;
        private CameraEffectManager      _effectManager;
        private CameraShaker             _shaker;
        private KillCamController        _killCamController;
        private CombatCameraEventRouter     _combatCameraEventRouter;
        private readonly CameraResolver  _cameraResolver = new CameraResolver();
        private CameraState           _rigState = new CameraState();
        private CameraContext     _cameraContext;
        private CameraDirector     _modeController;

        private Camera    _mainCamera;
        private Transform _target;
        private Transform _cameraPivot;
        private ActorMovementController _targetMovement;

        private float _currentYaw;
        private float _currentPitch;
        private float _currentDistance;
        private float _targetDistance;

        private Vector3 _cameraOffset;
        private Vector3 _smoothPosition;
        private Vector3 _positionVelocity;
        private Vector3 _offsetVelocity;

        private bool  _isAligning;
        private float _alignTimer;

        private CapsuleCollider  _characterCapsule;

        // 경사 지형 피치 보정
        private float _slopePitchOffset;
        private float _slopePitchVelocity;

        private Transform _lookAtOverride;
        private Vector3   _lookAtOverrideOffset;

        private bool _isInputLocked;
        private float _lastManualCameraInputTime = -999f;
        private bool _isCameraInputRegistered;
        private int _lastLockOnToggleFrame = -1;
        private float _lastLockOnToggleTime = -999f;
        private int _suppressCapsuleClearanceUntilFrame = -1;
        private bool _heldCombatStateForSwap;
        private float _holdCombatStateUntilTime = -999f;
        private bool _isSceneCameraInitialized;
        private bool _isSceneCameraReady;
        private bool _isScenePoseApplyPending;
        private Transform _sceneCameraExpectedTarget;
        private int _sceneAlignedRenderFrames;
        private const int REQUIRED_SCENE_ALIGNED_RENDER_FRAMES = 3;
        private const float SCENE_PIVOT_ALIGNMENT_TOLERANCE = 0.01f;
        private const float LOCK_ON_TOGGLE_DEBOUNCE_TIME = 0.08f;

        private System.Func<bool> _combatStateProvider;
        private System.Func<Vector3> _playerVelocityProvider;

        private CameraShakeDatabase _cameraShakeDatabase;
        private DialogueCameraSettingsSO _dialogueCameraSettings;
        private LayerMask           _lockOnLayerMask;
        private LayerMask           _collisionLayers;

        #region IManager

        public void Init()
        {
            Debug.Log("[CameraManager] 초기화 시작");
        }

        public async UniTask InitializeAsync(CancellationToken cancellationToken)
        {
            if (settings == null)
            {
                settings = await AssetManager.Instance.LoadGlobalAsync<CameraSettings>(
                    SETTINGS_ADDRESSABLE_KEY,
                    nameof(CameraManager),
                    cancellationToken);
            }

            InitializeCamera();

            _lockOnLayerMask = CameraConfig.GetLockOnLayerMask();
            _collisionLayers = CameraConfig.GetCollisionLayerMask();

            RebuildTargetSubsystems(preserveLockOnTarget: false);

            _rotTransition = new CameraRotationTransition();
            _effectManager = new CameraEffectManager(this);
            _combatCameraEventRouter = new CombatCameraEventRouter(this);
            InitializeCameraModes();

            UniTask cameraShakeTask = LoadCameraShakeDatabase(cancellationToken);
            UniTask dialogueTask = LoadDialogueCameraSettings(cancellationToken);
            UniTask killCamTask = LoadKillCamData(cancellationToken);
            UniTask profileTask = LoadCombatCameraProfileDatabase(cancellationToken);
            UniTask guardFovTask = LoadPerfectGuardFOVData(cancellationToken);
            await UniTask.WhenAll(
                cameraShakeTask,
                dialogueTask,
                killCamTask,
                profileTask,
                guardFovTask);

            Debug.Log("[CameraManager] 초기화 완료");
        }

        public void AfterInit()
        {
            RegisterCameraInputEvents();
        }

        private void RegisterCameraInputEvents()
        {
            if (_isCameraInputRegistered)
                return;

            var input = InputManager.Instance;
            if (input == null)
                return;

            input.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOn,
                null, OnLockOnPerformed, null, null, null, InputLayer.Level_1);
            input.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchLeft,
                null, OnLockOnSwitchLeft, null, null, null, InputLayer.Level_1);
            input.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchRight,
                null, OnLockOnSwitchRight, null, null, null, InputLayer.Level_1);
            _isCameraInputRegistered = true;
        }

        private void UnregisterCameraInputEvents()
        {
            if (!_isCameraInputRegistered || InputManager.Instance == null)
                return;

            var input = InputManager.Instance;
            input.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOn,
                null, OnLockOnPerformed, null);
            input.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchLeft,
                null, OnLockOnSwitchLeft, null);
            input.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchRight,
                null, OnLockOnSwitchRight, null);
            _isCameraInputRegistered = false;
        }

        public void Dispose()
        {
            Debug.Log("[CameraManager] 정리 시작");

            _effectManager?.DisposeAll();
            _killCamController?.ForceStop();
            settings = null;
            _cameraShakeDatabase = null;
            _dialogueCameraSettings = null;
            _killCamController = null;

            if (_cameraPivot != null) Destroy(_cameraPivot.gameObject);

            UnregisterCameraInputEvents();

            Debug.Log("[CameraManager] 정리 완료");
        }

        public void OnSceneChanged(string sceneType)
        {
            _isSceneCameraInitialized = false;
            _isSceneCameraReady = false;
            _isScenePoseApplyPending = false;
            _sceneCameraExpectedTarget = null;
            _sceneAlignedRenderFrames = 0;
            _lockOn?.Release();
            _effectManager?.StopAll(immediate: true);
            _killCamController?.ForceStop();
            _isInputLocked = false;
            _modeController?.ForceMode(CameraModeType.InGame);

            StartCoroutine(CoInitializeCameraOnSceneChanged());
        }

        private System.Collections.IEnumerator CoInitializeCameraOnSceneChanged()
        {
            // Camera.main은 씬 전환 직후 한 프레임 늦게 등록되는 경우가 있어 1프레임 대기
            yield return null;

            InitializeCamera();

            RebuildTargetSubsystems(preserveLockOnTarget: false);

            SyncCameraContext();
            SyncRigStateFromFields();
            _isSceneCameraInitialized =
                _target != null && _mainCamera != null && _cameraPivot != null;
        }

        public void OnUpdate()
        {
            if (_target == null || _mainCamera == null || _cameraPivot == null) return;

            // HideAndDontSave 오브젝트는 Unity 업데이트 루프에서 제외되므로
            // CameraManager가 직접 매 프레임 호출해 Shake/Punch 타이머를 진행한다.
            _shaker?.ManualUpdate(Time.deltaTime);
            SyncCameraContext();
            SyncRigStateFromFields();
            _modeController?.HandleInput(Time.deltaTime);
            SyncFieldsFromRigState();
        }

        public void OnFixedUpdate() { }

        public void OnLateUpdate()
        {
            if (_target == null || _mainCamera == null || _cameraPivot == null) return;

            if (_isScenePoseApplyPending && _target == _sceneCameraExpectedTarget)
            {
                Vector3 pivotBase = _target.position + _cameraOffset;
                _cameraPivot.position = pivotBase;
                _smoothPosition = pivotBase;
                _positionVelocity = Vector3.zero;
                _offsetVelocity = Vector3.zero;
            }

            SyncCameraContext();
            SyncRigStateFromFields();
            CameraEffectState fx = _effectManager.UpdateAndComputeState(Time.deltaTime);
            CameraPose pose = _modeController != null
                ? _modeController.EvaluatePose(Time.deltaTime, fx)
                : CameraPose.FromCamera(_mainCamera, _cameraPivot, _currentYaw, _currentPitch, _targetDistance);

            SyncFieldsFromRigState();
            SyncFieldsFromCameraContext();
            _cameraResolver.Apply(pose, _mainCamera, _cameraPivot);
            SyncRigStateFromFields();

            if (_isScenePoseApplyPending
                && _target == _sceneCameraExpectedTarget
                && _mainCamera != null
                && _cameraPivot != null)
            {
                Vector3 expectedPivot = _sceneCameraExpectedTarget.position + _cameraOffset;
                bool aligned = (_cameraPivot.position - expectedPivot).sqrMagnitude
                               <= SCENE_PIVOT_ALIGNMENT_TOLERANCE
                               * SCENE_PIVOT_ALIGNMENT_TOLERANCE;

                _sceneAlignedRenderFrames = aligned
                    ? _sceneAlignedRenderFrames + 1
                    : 0;

                if (_sceneAlignedRenderFrames >= REQUIRED_SCENE_ALIGNED_RENDER_FRAMES)
                {
                    _isScenePoseApplyPending = false;
                    _isSceneCameraReady = true;
                }
            }
        }

        #endregion

        #region 입력

        private void OnLockOnPerformed(InputAction.CallbackContext ctx)
        {
            if (_target == null || _lockOn == null) return;
            if (Time.frameCount == _lastLockOnToggleFrame)
                return;
            if (Time.unscaledTime - _lastLockOnToggleTime < LOCK_ON_TOGGLE_DEBOUNCE_TIME)
                return;

            _lastLockOnToggleFrame = Time.frameCount;
            _lastLockOnToggleTime = Time.unscaledTime;

            if (_lockOn.IsActive)
            {
                _lockOn.Release();
            }
            else
            {
                if (!_lockOn.TryActivate()) StartCameraAlign();
            }
        }

        private void OnLockOnSwitchRight(InputAction.CallbackContext ctx)
        {
            if (_lockOn == null || !_lockOn.IsActive) return;
            _lockOn.SwitchTarget(1);
        }

        private void OnLockOnSwitchLeft(InputAction.CallbackContext ctx)
        {
            if (_lockOn == null || !_lockOn.IsActive) return;
            _lockOn.SwitchTarget(-1);
        }

        #endregion

        #region 전방 카메라 (충돌 회피)

        private void CacheCapsule()
        {
            _characterCapsule = _target != null
                ? _target.GetComponentInChildren<CapsuleCollider>()
                : null;
        }

        private void CacheMovementController()
        {
            _targetMovement = _target != null
                ? _target.GetComponent<ActorMovementController>()
                  ?? _target.GetComponentInParent<ActorMovementController>()
                  ?? _target.GetComponentInChildren<ActorMovementController>()
                : null;
        }

        #endregion

        #region 경사 보정

        /// <summary>
        /// 캐릭터 발밑 레이캐스트로 경사각을 구하고, 피치 하한 오프셋을 반환.
        /// 오르막에서는 카메라가 땅 아래로 잘리지 않도록 하한을 올려주고,
        /// 내리막에서는 자연스럽게 아래를 보도록 풀어준다.
        /// </summary>
        private float ComputeSlopePitchOffset()
        {
            if (_target == null || settings.slopePitchCorrectionStrength <= 0f)
                return 0f;

            // 발밑 레이캐스트 (캐릭터 콜라이더를 제외하려면 Player 레이어는 _collisionLayers에서 이미 제외돼 있음)
            var ray = new Ray(_target.position + Vector3.up * 0.1f, Vector3.down);
            if (!Physics.Raycast(ray, out RaycastHit hit, settings.slopeCheckDistance, _collisionLayers))
                return 0f;

            // 법선과 Up 벡터의 각도 = 경사각
            float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
            if (slopeAngle < 1f) return 0f; // 평지는 무시

            // 경사 방향이 카메라 진행방향 기준 오르막/내리막인지 판단
            // 내리막: 카메라가 앞을 보면 발밑이 내려가는 방향 → 피치 하한을 낮춰야 위를 볼 수 있음
            // 여기서는 단순히 경사각 비례로 offset을 스무딩해서 반환
            float targetOffset = -slopeAngle * settings.slopePitchCorrectionStrength;

            _slopePitchOffset = Mathf.SmoothDamp(
                _slopePitchOffset, targetOffset,
                ref _slopePitchVelocity, settings.slopeCorrectionSmoothTime);

            return _slopePitchOffset;
        }

        #endregion

        #region 카메라 정렬

        private void StartCameraAlign()
        {
            _isAligning = true;
            _alignTimer = settings.alignDuration;
        }

        private void UpdateCameraAlign(bool isCombat)
        {
            if (_isInputLocked || !_isAligning || _target == null) return;

            Vector3 fwd        = ResolveTargetForwardXZ();
            float   targetYaw   = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
            float   targetPitch = isCombat ? settings.combatPitch : settings.explorePitch;

            if (_alignTimer <= Time.deltaTime)
            {
                _currentYaw = targetYaw;
                _currentPitch = targetPitch;
                _alignTimer = 0f;
                _isAligning = false;
            }
            else
            {
                float remainingTime = Mathf.Max(_alignTimer, 0.001f);
                float yawStep = Mathf.Abs(Mathf.DeltaAngle(_currentYaw, targetYaw)) / remainingTime * Time.deltaTime;
                float pitchStep = Mathf.Abs(targetPitch - _currentPitch) / remainingTime * Time.deltaTime;

                _currentYaw = Mathf.MoveTowardsAngle(_currentYaw, targetYaw, yawStep);
                _currentPitch = Mathf.MoveTowards(_currentPitch, targetPitch, pitchStep);
                _alignTimer -= Time.deltaTime;
            }

            float alignDynamicMin = settings.minVerticalAngle + ComputeSlopePitchOffset();
            _currentPitch = Mathf.Clamp(_currentPitch, alignDynamicMin, settings.maxVerticalAngle);
        }

        private Vector3 ResolveTargetForwardXZ()
        {
            if (_target != null)
            {
                Vector3 targetForward = _target.forward;
                targetForward.y = 0f;
                if (targetForward.sqrMagnitude > 0.001f)
                    return targetForward.normalized;
            }

            return Vector3.forward;
        }

        #endregion

        #region 초기화 / 리소스 로드

        private void InitializeCamera()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _target = player.transform;
                CacheCapsule();
                CacheMovementController();
            }

            _mainCamera = UnityEngine.Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogWarning("[CameraManager] 메인 카메라를 찾을 수 없습니다. 카메라가 없는 씬이거나 아직 초기화 전입니다.");
                return;
            }

            if (_cameraPivot == null)
            {
                _cameraPivot = new GameObject("CameraPivot").transform;
                _cameraPivot.SetParent(transform);
            }

            _currentDistance = settings.defaultDistance;
            _targetDistance  = settings.defaultDistance;
            _currentYaw      = 0f;
            _currentPitch    = settings.explorePitch;
            _cameraOffset    = settings.defaultOffset;

            if (_target != null)
            {
                _cameraPivot.position = _target.position + _cameraOffset;
                _smoothPosition       = _cameraPivot.position;
            }
            else
            {
                _cameraPivot.position = _cameraOffset;
                _smoothPosition       = _cameraPivot.position;
                Debug.LogWarning("[CameraManager] 타겟이 설정되지 않았습니다.");
            }

            Quaternion rot = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            _mainCamera.transform.position = _cameraPivot.position + rot * new Vector3(0f, 0f, -_currentDistance);
            _mainCamera.transform.rotation = rot;

            // CameraShaker를 CameraManager 자식으로 붙인다.
            // HideAndDontSave 오브젝트는 Unity 업데이트 루프가 돌지 않으므로
            // Update()를 비활성화하고 CameraManager.OnUpdate()에서 ManualUpdate()를 직접 호출한다.
            if (_shaker == null)
            {
                var shakerGO = new GameObject("CameraShaker");
                shakerGO.transform.SetParent(transform);
                shakerGO.hideFlags = HideFlags.HideInHierarchy; // 하이어라키 노출만 숨김, 업데이트는 정상 동작
                _shaker = shakerGO.AddComponent<CameraShaker>();
                _shaker.SetAutoUpdate(false); // CameraManager가 수동으로 틱을 준다
            }

            _mainCamera.fieldOfView = settings.fovExplore;

            SyncCameraContext();
            SyncRigStateFromFields();
        }

        private void InitializeCameraModes()
        {
            if (_cameraContext == null)
                _cameraContext = new CameraContext(_rigState);

            SyncCameraContext();

            _modeController = new CameraDirector(_cameraContext);

            CameraModeType[] modes = settings != null && settings.enabledModes != null && settings.enabledModes.Length > 0
                ? settings.enabledModes
                : DefaultEnabledModes;

            for (int i = 0; i < modes.Length; i++)
            {
                ICameraBehavior behavior = CreateBehavior(modes[i]);
                if (behavior != null)
                    _modeController.Register(behavior);
            }

            // InGame은 기본 진입 모드이자 다른 모드 종료 시 복귀 대상이므로 누락되지 않도록 보강한다.
            if (!_modeController.IsRegistered(CameraModeType.InGame))
            {
                Debug.LogWarning("[CameraManager] CameraSettings.enabledModes에 InGame이 없어 강제로 등록합니다. SO 설정을 확인하세요.");
                _modeController.Register(new InGameCameraBehavior());
            }

            _modeController.SetMode(CameraModeType.InGame);
        }

        // 인스펙터에서 선택한 모드 타입을 실제 Behavior 인스턴스로 생성한다(타입 안전 팩토리).
        private static ICameraBehavior CreateBehavior(CameraModeType modeType)
        {
            switch (modeType)
            {
                case CameraModeType.InGame:                return new InGameCameraBehavior();
                case CameraModeType.Free:                 return new FreeCameraBehavior();
                case CameraModeType.Dialogue:             return new DialogueCameraBehavior();
                case CameraModeType.CameraSnapshotSequence: return new CameraSnapshotSequenceBehavior();
                case CameraModeType.DialogueCameraReplay: return new DialogueCameraReplayBehavior();
                default:
                    Debug.LogWarning($"[CameraManager] 팩토리에 등록되지 않은 카메라 모드입니다: {modeType}");
                    return null;
            }
        }

        private void RebuildTargetSubsystems(bool preserveLockOnTarget)
        {
            CameraLockOn previousLockOn = _lockOn;
            CameraDistanceController previousDistanceCtrl = _distanceCtrl;
            Transform previousLockOnTarget = preserveLockOnTarget && previousLockOn?.IsActive == true
                ? previousLockOn.CurrentTarget
                : null;

            if (!preserveLockOnTarget)
                previousLockOn?.Release();

            _lockOn = null;
            _collision = null;
            _distanceCtrl = null;

            if (settings == null || _target == null || _mainCamera == null)
            {
                if (preserveLockOnTarget)
                    previousLockOn?.Release();
                return;
            }

            float initialDistance = Mathf.Clamp(
                _targetDistance > 0f ? _targetDistance : settings.defaultDistance,
                settings.minDistance,
                settings.maxDistance);
            float initialFOV = previousDistanceCtrl?.BaseFOV ?? settings.fovExplore;

            _lockOn = new CameraLockOn(settings, _target, _mainCamera, _lockOnLayerMask, _collisionLayers);
            _lockOn.SetPlayerVelocityProvider(_playerVelocityProvider ?? GetPlayerVelocity);
            _collision = new CameraCollision(settings, _target, _collisionLayers, initialDistance);
            _distanceCtrl = new CameraDistanceController(settings, _target, _lockOnLayerMask, initialFOV);
            _distanceCtrl.SetPlayerVelocityProvider(_playerVelocityProvider ?? GetPlayerVelocity);

            if (preserveLockOnTarget && previousLockOnTarget != null && !_lockOn.TryRestoreTarget(previousLockOnTarget))
                previousLockOn?.Release();
        }

        private void SyncCameraContext()
        {
            if (_cameraContext == null) return;

            _cameraContext.MainCamera = _mainCamera;
            _cameraContext.Target = _target;
            _cameraContext.CameraPivot = _cameraPivot;
            _cameraContext.Settings = settings;
            _cameraContext.DialogueSettings = _dialogueCameraSettings;
            _cameraContext.LockOn = _lockOn;
            _cameraContext.Collision = _collision;
            _cameraContext.DistanceController = _distanceCtrl;
            _cameraContext.RotationTransition = _rotTransition;
            _cameraContext.CombatStateProvider = ResolveCombatState;
            _cameraContext.PlayerVelocityProvider = _playerVelocityProvider ?? GetPlayerVelocity;
            _cameraContext.ComputeSlopePitchOffset = ComputeSlopePitchOffset;
            _cameraContext.StartCameraAlign = StartCameraAlign;
            _cameraContext.NotifyManualCameraInput = NotifyManualCameraInput;
            _cameraContext.PopCameraMode = PopCameraMode;
            _cameraContext.LookAtOverride = _lookAtOverride;
            _cameraContext.LookAtOverrideOffset = _lookAtOverrideOffset;
            _cameraContext.CollisionLayers = _collisionLayers;
            _cameraContext.CharacterCapsule = _characterCapsule;
            _cameraContext.IsInputLocked = _isInputLocked;
            _cameraContext.IsAligning = _isAligning;
            _cameraContext.AlignTimer = _alignTimer;
            _cameraContext.HasActiveEffects = _effectManager?.HasActiveEffects ?? false;
            _cameraContext.SuppressCapsuleClearanceUntilFrame = _suppressCapsuleClearanceUntilFrame;
        }

        private void SyncRigStateFromFields()
        {
            _rigState.CurrentYaw = _currentYaw;
            _rigState.CurrentPitch = _currentPitch;
            _rigState.CurrentDistance = _currentDistance;
            _rigState.TargetDistance = _targetDistance;
            _rigState.CameraOffset = _cameraOffset;
            _rigState.SmoothPosition = _smoothPosition;
            _rigState.PositionVelocity = _positionVelocity;
            _rigState.OffsetVelocity = _offsetVelocity;
        }

        private void SyncFieldsFromCameraContext()
        {
            if (_cameraContext == null) return;

            _isInputLocked = _cameraContext.IsInputLocked;
            _isAligning = _cameraContext.IsAligning;
            _alignTimer = _cameraContext.AlignTimer;
        }

        private void SyncFieldsFromRigState()
        {
            _currentYaw = _rigState.CurrentYaw;
            _currentPitch = _rigState.CurrentPitch;
            _currentDistance = _rigState.CurrentDistance;
            _targetDistance = _rigState.TargetDistance;
            _cameraOffset = _rigState.CameraOffset;
            _smoothPosition = _rigState.SmoothPosition;
            _positionVelocity = _rigState.PositionVelocity;
            _offsetVelocity = _rigState.OffsetVelocity;
        }

        private Vector3 GetPlayerVelocity()
        {
            if (_target == null) return Vector3.zero;

            if (_targetMovement == null)
                CacheMovementController();

            return _targetMovement?.Motor != null ? _targetMovement.Motor.Velocity : Vector3.zero;
        }

        private bool ResolveCombatState()
        {
            if (Time.unscaledTime <= _holdCombatStateUntilTime)
                return _heldCombatStateForSwap;

            return _combatStateProvider?.Invoke() ?? false;
        }

        private async UniTask LoadCameraShakeDatabase(CancellationToken cancellationToken)
        {
            try
            {
                _cameraShakeDatabase =
                    await AssetManager.Instance.LoadGlobalAsync<CameraShakeDatabase>(
                        CAMERA_SHAKE_DB_KEY,
                        nameof(CameraManager),
                        cancellationToken);
                _cameraShakeDatabase?.Initialize();
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CameraManager] CameraShakeDatabase 로드 실패: {e.Message}");
            }
        }

        private async UniTask LoadDialogueCameraSettings(CancellationToken cancellationToken)
        {
            try
            {
                _dialogueCameraSettings =
                    await AssetManager.Instance.LoadGlobalAsync<DialogueCameraSettingsSO>(
                        DIALOGUE_CAMERA_SETTINGS_KEY,
                        nameof(CameraManager),
                        cancellationToken);
                SyncCameraContext();
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CameraManager] DialogueCameraSettings 로드 실패 또는 미등록: {e.Message}");
            }
        }

        private async UniTask LoadKillCamData(CancellationToken cancellationToken)
        {
            try
            {
                var data = await AssetManager.Instance.LoadGlobalAsync<KillCamData>(
                    KILL_CAM_DATA_KEY,
                    nameof(CameraManager),
                    cancellationToken);
                if (data != null) _killCamController = new KillCamController(this, data);
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CameraManager] KillCamData 로드 실패: {e.Message}");
            }
        }

        private async UniTask LoadCombatCameraProfileDatabase(CancellationToken cancellationToken)
        {
            try
            {
                var data =
                    await AssetManager.Instance.LoadGlobalAsync<CombatCameraProfileDatabaseSO>(
                        COMBAT_CAMERA_PROFILE_DB_KEY,
                        nameof(CameraManager),
                        cancellationToken);
                _combatCameraEventRouter?.SetProfileDatabase(data);
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CameraManager] CombatCameraProfileDatabase 로드 실패 또는 미등록: {e.Message}");
            }
        }

        private async UniTask LoadPerfectGuardFOVData(CancellationToken cancellationToken)
        {
            try
            {
                var data = await AssetManager.Instance.LoadGlobalAsync<FOVCameraEffectData>(
                    PERFECT_GUARD_FOV_KEY,
                    nameof(CameraManager),
                    cancellationToken);
                PlayerGuardState.SetPerfectGuardFOVData(data);
            }
            catch (System.OperationCanceledException)
            {
                throw;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CameraManager] PerfectGuardFOV 로드 실패: {e.Message}");
            }
        }

        #endregion

        #region Public API

        public void SetTarget(Transform newTarget)
        {
            if (_target == newTarget && newTarget != null)
            {
                RefreshTargetCollisionReference();
                return;
            }

            _target = newTarget;
            CacheCapsule();
            CacheMovementController();
            RebuildTargetSubsystems(preserveLockOnTarget: true);
            SyncCameraContext();
            if (_target == null || _cameraPivot == null) return;

            _cameraPivot.position = _target.position + _cameraOffset;
            _smoothPosition       = _cameraPivot.position;

            if (_mainCamera != null)
            {
                Quaternion rot = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
                _mainCamera.transform.position = _cameraPivot.position + rot * new Vector3(0f, 0f, -_currentDistance);
                _mainCamera.transform.rotation = rot;
            }

            SyncRigStateFromFields();
        }

        /// <summary>
        /// 플레이어 루트는 유지한 채 활성 모델/콜라이더만 바뀐 경우 카메라 충돌 참조를 갱신한다.
        /// 캐릭터 스왑 순간의 캡슐 클리어런스 재계산이 카메라를 전방/근거리로 당기는 것을 짧게 억제한다.
        /// </summary>
        public void RefreshTargetCollisionReference(int suppressCapsuleClearanceFrames = 2)
        {
            CacheCapsule();
            CacheMovementController();
            _suppressCapsuleClearanceUntilFrame = Mathf.Max(
                _suppressCapsuleClearanceUntilFrame,
                Time.frameCount + Mathf.Max(0, suppressCapsuleClearanceFrames));
            SyncCameraContext();
        }

        /// <summary>
        /// 캐릭터 스왑 중 컴포넌트 참조 갱신으로 전투 상태 판정이 잠깐 흔들려도
        /// 카메라가 일반/전투 오프셋 사이를 왕복하지 않도록 현재 전투 상태를 짧게 고정한다.
        /// </summary>
        public void PreserveCombatStateForCharacterSwap(bool isInCombat, float duration = 0.35f)
        {
            if (!isInCombat)
                return;

            _heldCombatStateForSwap = true;
            _holdCombatStateUntilTime = Mathf.Max(
                _holdCombatStateUntilTime,
                Time.unscaledTime + Mathf.Max(0f, duration));
            SyncCameraContext();
        }

        /// <summary>
        /// 순간이동 시 카메라를 지정 위치로 즉시 스냅한다. SmoothDamp 속도를 초기화해 튐 현상을 방지.
        /// </summary>
        public void SnapToTarget(Vector3 snappedPosition)
        {
            if (_cameraPivot == null) return;
            Vector3 pivotBase     = snappedPosition + _cameraOffset;
            _cameraPivot.position = pivotBase;
            _smoothPosition       = pivotBase;
            _positionVelocity     = Vector3.zero;
            _offsetVelocity       = Vector3.zero;
            if (_mainCamera != null)
            {
                Quaternion rot = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
                _mainCamera.transform.position = _cameraPivot.position + rot * new Vector3(0f, 0f, -_currentDistance);
                _mainCamera.transform.rotation = rot;
            }

            SyncRigStateFromFields();
        }

        /// <summary>
        /// 씬 전환 해제 전에 플레이어 기준 카메라 포즈를 강제로 준비한다.
        /// 이 요청 이후 LateUpdate 카메라 파이프라인이 실제 적용돼야 준비 완료로 판정한다.
        /// </summary>
        public bool PrepareSceneCamera(Transform expectedTarget)
        {
            if (!_isSceneCameraInitialized || expectedTarget == null)
                return false;

            _isSceneCameraReady = false;
            _isScenePoseApplyPending = true;
            _sceneCameraExpectedTarget = expectedTarget;
            _sceneAlignedRenderFrames = 0;

            SetTarget(expectedTarget);
            _modeController?.ForceMode(CameraModeType.InGame);
            SnapToTarget(expectedTarget.position);
            SyncCameraContext();
            SyncRigStateFromFields();
            return true;
        }

        public bool IsSceneCameraReadyFor(Transform expectedTarget)
        {
            return _isSceneCameraReady
                   && expectedTarget != null
                   && _target == expectedTarget
                   && _sceneCameraExpectedTarget == expectedTarget;
        }

        public CameraModeType CurrentCameraMode => _modeController?.CurrentModeType ?? CameraModeType.InGame;

        public bool SetCameraMode(CameraModeType modeType, CameraModeEnterParams enterParams = null)
        {
            SyncCameraContext();
            return _modeController != null && _modeController.SetMode(modeType, enterParams);
        }

        public bool PushCameraMode(CameraModeType modeType, CameraModeEnterParams enterParams = null)
        {
            SyncCameraContext();
            return _modeController != null && _modeController.PushMode(modeType, enterParams);
        }

        public bool PopCameraMode(CameraModeEnterParams enterParams = null)
        {
            SyncCameraContext();
            return _modeController != null && _modeController.PopMode(enterParams);
        }

        public bool ForceCameraMode(CameraModeType modeType, CameraModeEnterParams enterParams = null)
        {
            SyncCameraContext();
            return _modeController != null && _modeController.ForceMode(modeType, enterParams);
        }

        public bool PushDialogueCamera(Transform speaker, Transform listener = null, Vector3 offset = default)
        {
            // 동일 화자 재진입은 OnEnter 재호출로 보간 상태가 끊기지 않도록 no-op 처리
            if (_modeController != null
                && _modeController.CurrentMode is DialogueCameraBehavior currentDialogue
                && currentDialogue.IsSameSpeaker(speaker, listener))
            {
                return true;
            }

            return EnterDialogueLayerMode(CameraModeType.Dialogue, new CameraModeEnterParams
            {
                PrimaryTarget = speaker,
                SecondaryTarget = listener,
                Offset = offset
            });
        }

        /// <summary>
        /// 대화 계층(Dialogue/DialogueCameraReplay) 내부 전환은 스택을 쌓지 않고 교체(SetMode)한다.
        /// 그렇지 않으면 노드마다 Dialogue↔Replay push가 누적돼 대화 종료 시 1회 Pop으로 InGame까지 못 돌아온다.
        /// 대화 계층 밖(InGame 등)에서 진입할 때만 PushMode로 계층을 올린다.
        /// </summary>
        private bool EnterDialogueLayerMode(CameraModeType modeType, CameraModeEnterParams enterParams)
        {
            if (_modeController == null)
                return false;

            bool inDialogueLayer = _modeController.CurrentMode is DialogueCameraBehavior
                                   || _modeController.CurrentMode is DialogueCameraReplayBehavior;

            return inDialogueLayer
                ? _modeController.SetMode(modeType, enterParams)
                : _modeController.PushMode(modeType, enterParams);
        }

        public bool PushFreeCamera(float moveSpeed = 6f, float lookSensitivity = 0.12f)
        {
            return PushCameraMode(CameraModeType.Free, new CameraModeEnterParams
            {
                FreeCameraMoveSpeed = moveSpeed,
                FreeCameraLookSensitivity = lookSensitivity
            });
        }

        public bool IsFreeCameraActive => CurrentCameraMode == CameraModeType.Free;
        public bool IsSceneCameraInitialized => _isSceneCameraInitialized;
        public bool IsSceneCameraReady => _isSceneCameraReady;
        public CombatCameraEventRouter CombatCamera => _combatCameraEventRouter;
        public float TimeSinceLastManualCameraInput => Time.unscaledTime - _lastManualCameraInputTime;
        public float SettingsCombatCameraShakeScale => settings != null ? settings.combatCameraShakeScale : 1f;
        public float SettingsCombatCameraAutoCorrectionScale => settings != null ? settings.combatCameraAutoCorrectionScale : 1f;
        public float SettingsCombatCameraSequenceIntensity => settings != null ? settings.combatCameraSequenceIntensity : 1f;

        public void NotifyManualCameraInput()
        {
            _lastManualCameraInputTime = Time.unscaledTime;
        }

        public bool PushCameraSnapshotSequence(CameraSnapshotProfile profile, System.Action onComplete = null)
        {
            return PushCameraSnapshotSequence(profile, null, null, onComplete);
        }

        public bool PushCameraSnapshotSequence(
            CameraSnapshotProfile profile,
            CameraSnapshotActorReference? actorAnchor,
            CameraSnapshotActorReference? lookAtTarget,
            System.Action onComplete = null)
        {
            if (profile == null)
            {
                Debug.LogWarning("[CameraManager] CameraSnapshotProfile이 null입니다.");
                return false;
            }

            if (!CanPushCameraSnapshotSequence(profile))
                return false;

            return PushCameraMode(CameraModeType.CameraSnapshotSequence, new CameraModeEnterParams
            {
                SnapshotProfile = profile,
                HasSnapshotActorAnchorOverride = actorAnchor.HasValue,
                SnapshotActorAnchor = actorAnchor.GetValueOrDefault(),
                HasSnapshotLookAtTargetOverride = lookAtTarget.HasValue,
                SnapshotLookAtTarget = lookAtTarget.GetValueOrDefault(),
                RestorePreviousOnExit = profile.restorePreviousModeOnFinish,
                OnComplete = onComplete
            });
        }

        public bool IsCameraSnapshotSequenceActive(CameraSnapshotProfile profile = null)
        {
            if (_modeController?.CurrentMode is not CameraSnapshotSequenceBehavior snapshotMode)
                return false;

            return profile == null || snapshotMode.ActiveProfile == profile;
        }

        public bool StopCameraSnapshotSequence(CameraSnapshotProfile profile = null)
        {
            if (!IsCameraSnapshotSequenceActive(profile))
                return false;

            return PopCameraMode();
        }

        /// <param name="restorePreviousOnFinish">완료 시 이전 모드 복귀 여부. null이면 녹화 에셋 설정을 따른다.
        /// 대화 중 재생은 false로 호출해 마지막 프레임을 유지(다음 노드가 카메라를 교체)한다.</param>
        public bool PushDialogueCameraRecording(
            DialogueCameraRecordingSO recording,
            CameraSnapshotActorReference? anchorOverride = null,
            System.Action onComplete = null,
            bool? restorePreviousOnFinish = null)
        {
            if (recording == null)
            {
                Debug.LogWarning("[CameraManager] DialogueCameraRecordingSO이 null입니다.");
                return false;
            }

            if (recording.SampleCount == 0)
            {
                Debug.LogWarning($"[CameraManager] 빈 녹화입니다: {recording.name}");
                return false;
            }

            // 같은 녹화를 재생 중이고 아직 완료 전이면 재진입을 no-op 처리한다.
            // → 대화 "장면"의 여러 노드가 같은 녹화를 가리키면 처음부터 재시작하지 않고 한 번에 연속 재생.
            //   (완료 후엔 가드가 풀려 재진입 시 다시 처음부터 재생)
            if (_modeController != null
                && _modeController.CurrentMode is DialogueCameraReplayBehavior currentReplay
                && currentReplay.ActiveRecording == recording
                && !currentReplay.IsCompleted)
            {
                return true;
            }

            // 대화 계층 내부에서는 스택 누적을 막기 위해 교체 진입한다.
            return EnterDialogueLayerMode(CameraModeType.DialogueCameraReplay, new CameraModeEnterParams
            {
                DialogueRecording = recording,
                HasSnapshotActorAnchorOverride = anchorOverride.HasValue,
                SnapshotActorAnchor = anchorOverride.GetValueOrDefault(),
                RestorePreviousOnExit = restorePreviousOnFinish ?? recording.restorePreviousModeOnFinish,
                OnComplete = onComplete
            });
        }

        public bool IsDialogueCameraRecordingActive(DialogueCameraRecordingSO recording = null)
        {
            if (_modeController?.CurrentMode is not DialogueCameraReplayBehavior replayMode)
                return false;

            return recording == null || replayMode.ActiveRecording == recording;
        }

        public bool StopDialogueCameraRecording(DialogueCameraRecordingSO recording = null)
        {
            if (!IsDialogueCameraRecordingActive(recording))
                return false;

            return PopCameraMode();
        }

        private bool CanPushCameraSnapshotSequence(CameraSnapshotProfile profile)
        {
            if (_modeController?.CurrentMode is not CameraSnapshotSequenceBehavior currentSnapshot)
                return true;

            switch (profile.interruptPolicy)
            {
                case CameraSnapshotInterruptPolicy.Ignore:
                    return false;
                case CameraSnapshotInterruptPolicy.OverrideIfHigherPriority:
                    return profile.priority > currentSnapshot.ActivePriority;
                case CameraSnapshotInterruptPolicy.Restart:
                default:
                    return true;
            }
        }

        public Transform           GetTarget()         => _target;
        public float               GetCurrentYaw()     => _currentYaw;
        public float               GetCurrentPitch()   => _currentPitch;
        public float               GetCurrentDistance() => _targetDistance;
        public Vector3             GetCurrentOffset()  => _cameraOffset;
        public UnityEngine.Camera  GetMainCamera()     => _mainCamera;
        public float               GetCurrentFOV()     => _mainCamera != null ? _mainCamera.fieldOfView : settings.fovExplore;
        public float               GetBaseFOV()        => _distanceCtrl?.BaseFOV ?? settings.fovExplore;
        public float               GetTargetFOV()      => settings.fovExplore;
        public float               GetLockOnRange()    => settings != null ? settings.lockOnRange : 13f;

        public void SetDistance(float distance) =>
            _targetDistance = Mathf.Clamp(distance, settings.minDistance, settings.maxDistance);

        public void SetRotation(float yaw, float pitch)
        {
            _rotTransition.Cancel();
            _currentYaw   = yaw;
            _currentPitch = Mathf.Clamp(pitch, settings.minVerticalAngle, settings.maxVerticalAngle);
        }

        public void SetRotationSmooth(float yaw, float pitch, float duration, bool unlockOnComplete = false) =>
            SetRotationSmooth(yaw, pitch, duration, null, unlockOnComplete);

        public void SetRotationSmooth(float yaw, float pitch, float duration, AnimationCurve curve, bool unlockOnComplete = false)
        {
            if (duration <= 0f)
            {
                SetRotation(yaw, pitch);
                if (unlockOnComplete) _isInputLocked = false;
                return;
            }
            _rotTransition.Start(_currentYaw, _currentPitch, yaw, pitch, duration,
                settings.minVerticalAngle, settings.maxVerticalAngle, curve, unlockOnComplete);
        }

        public void SetCameraOffset(Vector3 offset)            => _cameraOffset  = offset;
        public void SetInputLock(bool locked)                  => _isInputLocked = locked;
        public bool IsInputLocked()                            => _isInputLocked;
        public void ReleaseLockOn()                            => _lockOn?.Release();
        public void SetCombatStateProvider(System.Func<bool> p)
        {
            _combatStateProvider = p;
            SyncCameraContext();
        }

        public void SetPlayerVelocityProvider(System.Func<Vector3> provider)
        {
            _playerVelocityProvider = provider;
            _distanceCtrl?.SetPlayerVelocityProvider(provider ?? GetPlayerVelocity);
            _lockOn?.SetPlayerVelocityProvider(provider ?? GetPlayerVelocity);
            SyncCameraContext();
        }

        // ── Shake / Punch ──────────────────────────────────────────────
        // 쉐이크는 가산형 보이스 — 호출마다 보이스가 추가되어 합산된다.
        // strength: 설정 슬라이더 × 카덴스 등 외부 강도 배율. hitWorldPos: 거리 감쇠용(없으면 null).
        public void StartShake(CameraShakeData data) => StartShake(data, Vector3.zero, 1f, null);

        /// <summary>hitDirection을 주면 Rotation 모드에서 Pitch/Yaw 방향 매칭을 적용한다.</summary>
        public void StartShake(CameraShakeData data, Vector3 hitDirection) => StartShake(data, hitDirection, 1f, null);

        public void StartShake(CameraShakeData data, Vector3 hitDirection, float strength, Vector3? hitWorldPos)
        {
            if (data == null || _shaker == null) return;
            float scaled = strength * ComputeDistanceAttenuation(data, hitWorldPos);
            if (scaled <= 0f) return;
            _shaker.PlayShake(data, hitDirection, scaled);
        }

        public void StartShake(string key) => StartShake(key, Vector3.zero, 1f, null);

        public void StartShake(string key, Vector3 hitDirection) => StartShake(key, hitDirection, 1f, null);

        public void StartShake(string key, Vector3 hitDirection, float strength, Vector3? hitWorldPos)
        {
            if (_cameraShakeDatabase != null)
                StartShake(_cameraShakeDatabase.GetShakeData(key), hitDirection, strength, hitWorldPos);
        }

        public void StartShake(CameraShakeIdType key) => StartShake(key.ToKey(), Vector3.zero, 1f, null);

        public void StartShake(CameraShakeIdType key, Vector3 hitDirection) => StartShake(key.ToKey(), hitDirection, 1f, null);

        public void StartShake(CameraShakeIdType key, Vector3 hitDirection, float strength, Vector3? hitWorldPos) =>
            StartShake(key.ToKey(), hitDirection, strength, hitWorldPos);

        /// <summary>거리 감쇠(Tier 3-H): 발생원이 멀수록 강도 감소. 옵트인 SO + 유효 위치에서만.</summary>
        private float ComputeDistanceAttenuation(CameraShakeData data, Vector3? hitWorldPos)
        {
            if (data == null || !data.AttenuateByDistance) return 1f;
            if (data.AttenuationRange <= 0f) return 1f;
            if (!hitWorldPos.HasValue || _mainCamera == null) return 1f; // 위치 불명이면 감쇠 생략

            float dist = Vector3.Distance(_mainCamera.transform.position, hitWorldPos.Value);
            return Mathf.Clamp01(1f - dist / data.AttenuationRange);
        }

        public void StopShake() => _shaker?.StopShake();

        public void Punch(Vector3 direction, float strength, float duration = 0.15f) =>
            _shaker?.Punch(direction, strength, duration);

        // ── KillCam ────────────────────────────────────────────────────
        public bool TryKillCam(Transform victim) =>
            _killCamController != null && _killCamController.TryExecute(victim);

        public bool CanStartKillCamWithoutChance(Transform victim) =>
            _killCamController != null && _killCamController.CanExecuteWithoutChance(victim);

        public bool TryKillCamWithoutChance(Transform victim) =>
            _killCamController != null && _killCamController.TryExecuteWithoutChance(victim);

        public bool IsKillCamPlaying => _killCamController?.IsPlaying ?? false;

        // ── LockOn ─────────────────────────────────────────────────────
        public bool      IsLockOnActive()  => _lockOn?.IsActive ?? false;
        public Transform GetLockOnTarget() => _lockOn?.CurrentTarget;

        // ── LookAt Override ────────────────────────────────────────────
        public void SetLookAtOverride(Transform lookAt, Vector3 offset = default)
        {
            _lookAtOverride       = lookAt;
            _lookAtOverrideOffset = offset;
        }

        public void ClearLookAtOverride()
        {
            _lookAtOverride       = null;
            _lookAtOverrideOffset = Vector3.zero;
        }

        // ── Effect ─────────────────────────────────────────────────────
        public ICameraEffect PlayEffect(CameraEffectData data)                    => _effectManager.PlayEffect(data);
        public void StopEffect(ICameraEffect effect, bool immediate = false)      => _effectManager?.StopEffect(effect, immediate);
        public void StopEffect(string effectId, bool immediate = false)           => _effectManager?.StopEffectById(effectId, immediate);
        public void StopAllEffects(bool immediate = false)                        => _effectManager?.StopAll(immediate);
        public bool HasActiveEffects                                               => _effectManager?.HasActiveEffects ?? false;

        // ── 런타임 튜닝 ────────────────────────────────────────────────
        public void SetDefaultOffset(Vector3 offset)  => settings.defaultOffset = offset;
        public void SetCombatOffset(Vector3 offset)   => settings.combatOffset  = offset;
        public void SetFOVSettings(float explore, float combat, float lockOn)
        {
            settings.fovExplore = explore;
            settings.fovCombat  = combat;
            settings.fovLockOn  = lockOn;
        }
        public void SetLockOnDistance(float distance) => settings.lockOnDistance = distance;
        public void SetCrowdZoomSettings(float zoomOutDist, float detectRadius, int threshold)
        {
            settings.crowdZoomOutDistance = zoomOutDist;
            settings.crowdDetectRadius    = detectRadius;
            settings.crowdEnemyThreshold  = threshold;
        }
        public void SetLockOnHeightDampSettings(float dampFactor, float pitchMin, float pitchMax, float pitchSpeed)
        {
            settings.lockOnHeightDampFactor = Mathf.Clamp01(dampFactor);
            settings.lockOnPitchMin         = pitchMin;
            settings.lockOnPitchMax         = pitchMax;
            settings.lockOnPitchSpeed       = pitchSpeed;
        }

        #endregion

        #region ICameraStateAccessor

        float              ICameraStateAccessor.CurrentYaw      => _currentYaw;
        float              ICameraStateAccessor.CurrentPitch     => _currentPitch;
        float              ICameraStateAccessor.CurrentDistance  => _currentDistance;
        float              ICameraStateAccessor.TargetDistance   => _targetDistance;
        Vector3            ICameraStateAccessor.CurrentOffset    => _cameraOffset;
        float              ICameraStateAccessor.CurrentFOV       => _mainCamera != null ? _mainCamera.fieldOfView : 60f;
        UnityEngine.Camera ICameraStateAccessor.MainCamera       => _mainCamera;
        Transform          ICameraStateAccessor.Target           => _target;

        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (_target == null || !Application.isPlaying) return;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(_target.position + _cameraOffset, 0.3f);

            if (_mainCamera != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(_target.position + _cameraOffset, _mainCamera.transform.position);
            }

            bool isLockOn = _lockOn?.IsActive ?? false;
            Gizmos.color = isLockOn ? Color.red : Color.green;
            Gizmos.DrawWireSphere(_target.position, settings.lockOnRange);

            if (isLockOn && _lockOn.CurrentTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(_target.position, _lockOn.CurrentTarget.position);
                Gizmos.DrawWireSphere(_lockOn.CurrentTarget.position, 0.5f);
            }
        }

        #endregion
    }
}
