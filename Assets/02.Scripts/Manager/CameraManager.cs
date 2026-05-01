using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UPlayGround.CameraSystem;
using UPlayGround.Data;
using UPlayGround.Data.Config;
using UPlayGround.Data.Path;
using UPlayGround.InputDefine;
using UPlayGround.State;

namespace UPlayGround.Manager
{
    /// <summary>
    /// TPS 카메라 오케스트레이터.
    /// 실제 로직은 서브시스템(CameraLockOn, CameraCollision 등)에 위임한다.
    /// </summary>
    public class CameraManager : BaseManager<CameraManager>, IManager, ICameraStateAccessor
    {
        [SerializeField] private CameraSettings settings;
        private const string SETTINGS_ADDRESSABLE_KEY = "CameraSettings";
        private const string CAMERA_SHAKE_DB_KEY      = "CameraShakeDatabase";
        private const string KILL_CAM_DATA_KEY         = "KillCamData";
        private const string PERFECT_GUARD_FOV_KEY     = "PerfectGuardFOV";
        private const string DIALOGUE_CAMERA_SETTINGS_KEY = "DialogueCameraSettings";

        private CameraLockOn             _lockOn;
        private CameraCollision          _collision;
        private CameraDistanceController _distanceCtrl;
        private CameraRotationTransition _rotTransition;
        private CameraEffectManager      _effectManager;
        private CameraShaker             _shaker;
        private KillCamController        _killCamController;
        private CameraRigState           _rigState = new CameraRigState();
        private CameraRuntimeContext     _cameraContext;
        private CameraModeController     _modeController;

        private Camera    _mainCamera;
        private Transform _target;
        private Transform _cameraPivot;

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

        // 전방 카메라 블렌딩 (충돌로 후방이 막힐 때 앞으로 전환)
        private float            _frontCameraBlend;
        private float            _frontCameraBlendVel;
        private CapsuleCollider  _characterCapsule;
        private const float      FRONT_BLEND_RETURN_SPEED = 0.12f; // 복귀: 부드럽게
        private const float      FRONT_BLEND_PULL_SPEED   = 0f;    // 당김: 즉시

        // 경사 지형 피치 보정
        private float _slopePitchOffset;
        private float _slopePitchVelocity;

        private Transform _lookAtOverride;
        private Vector3   _lookAtOverrideOffset;

        private bool _isInputLocked;

        private System.Func<bool> _combatStateProvider;

        private CameraShakeDatabase _cameraShakeDatabase;
        private DialogueCameraSettingsSO _dialogueCameraSettings;
        private LayerMask           _lockOnLayerMask;
        private LayerMask           _collisionLayers;

        #region IManager

        public void Init()
        {
            Debug.Log("[CameraManager] 초기화 시작");

            if (settings == null)
                LoadSettingsSync();

            InitializeCamera();
            LoadCameraShakeDatabase();
            LoadDialogueCameraSettings();

            _lockOnLayerMask = CameraConfig.GetLockOnLayerMask();
            _collisionLayers = CameraConfig.GetCollisionLayerMask();

            if (_target != null)
            {
                _lockOn       = new CameraLockOn(settings, _target, _mainCamera, _lockOnLayerMask);
                _collision    = new CameraCollision(settings, _target, _collisionLayers, settings.defaultDistance);
                _distanceCtrl = new CameraDistanceController(settings, _target, _lockOnLayerMask, settings.fovExplore);
            }

            _rotTransition = new CameraRotationTransition();
            _effectManager = new CameraEffectManager(this);
            InitializeCameraModes();

            LoadKillCamData();
            LoadPerfectGuardFOVData();

            Debug.Log("[CameraManager] 초기화 완료");
        }

        public void AfterInit()
        {
            var input = InputManager.Instance;
            input.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOn,
                null, OnLockOnPerformed, null, null, null, InputLayer.Level_1);
            input.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchLeft,
                null, OnLockOnSwitchLeft, null, null, null, InputLayer.Level_1);
            input.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchRight,
                null, OnLockOnSwitchRight, null, null, null, InputLayer.Level_1);
        }

        public void Dispose()
        {
            Debug.Log("[CameraManager] 정리 시작");

            _effectManager?.DisposeAll();
            _killCamController?.ForceStop();
            if (_dialogueCameraSettings != null)
            {
                Addressables.Release(_dialogueCameraSettings);
                _dialogueCameraSettings = null;
            }

            if (_cameraPivot != null) Destroy(_cameraPivot.gameObject);

            if (InputManager.Instance != null)
            {
                var input = InputManager.Instance;
                input.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOn,
                    null, OnLockOnPerformed, null);
                input.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchLeft,
                    null, OnLockOnSwitchLeft, null);
                input.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchRight,
                    null, OnLockOnSwitchRight, null);
            }

            Debug.Log("[CameraManager] 정리 완료");
        }

        public void OnSceneChanged(string sceneType)
        {
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

            if (_target != null)
            {
                _lockOn       = new CameraLockOn(settings, _target, _mainCamera, _lockOnLayerMask);
                _collision    = new CameraCollision(settings, _target, _collisionLayers, settings.defaultDistance);
                _distanceCtrl = new CameraDistanceController(settings, _target, _lockOnLayerMask, settings.fovExplore);
            }

            SyncCameraContext();
            SyncRigStateFromFields();
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

            SyncCameraContext();
            SyncRigStateFromFields();
            CameraEffectState fx = _effectManager.UpdateAndComputeState(Time.deltaTime);
            CameraRigPose pose = _modeController != null
                ? _modeController.EvaluatePose(Time.deltaTime, fx)
                : CameraRigPose.FromCamera(_mainCamera, _cameraPivot, _currentYaw, _currentPitch, _targetDistance);

            SyncFieldsFromRigState();
            SyncFieldsFromCameraContext();
            ApplyCameraPose(pose);
            SyncRigStateFromFields();
        }

        #endregion

        #region 입력

        private void OnLockOnPerformed(InputAction.CallbackContext ctx)
        {
            if (_target == null || _lockOn == null) return;
            if (_lockOn.IsActive)
            {
                _lockOn.Release();
                _targetDistance = settings.defaultDistance;
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

        #region 카메라 위치 / 회전

        private void UpdateCameraPosition(float smoothTime, float desiredDistance)
        {
            Vector3 pivotBase = _lookAtOverride != null
                ? _lookAtOverride.position + _lookAtOverrideOffset
                : _target.position + _cameraOffset;

            _smoothPosition      = Vector3.SmoothDamp(_smoothPosition, pivotBase, ref _positionVelocity, smoothTime);
            _cameraPivot.position = _smoothPosition;

            Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            Vector3    camDir   = rotation * Vector3.back;

            float finalDist = _collision != null
                ? _collision.Evaluate(_cameraPivot.position, camDir, desiredDistance)
                : desiredDistance;

            Vector3 backPos = _cameraPivot.position + camDir * finalDist;

            // 안전장치: 스무딩된 pivot이 지형 내부로 밀릴 때 SphereCast가 실패할 수 있으므로
            // KCC가 보장하는 pivotBase에서 다시 SphereCast로 경로를 재확인한다.
            Vector3 toCam     = backPos - pivotBase;
            float   toCamDist = toCam.magnitude;
            if (toCamDist > 0.01f)
            {
                Vector3 toCamDir = toCam / toCamDist;
                if (Physics.SphereCast(pivotBase, settings.cameraRadius, toCamDir,
                        out RaycastHit safeHit, toCamDist, _collisionLayers))
                {
                    if (safeHit.transform != _target && !safeHit.transform.IsChildOf(_target))
                    {
                        float safeDist = Mathf.Max(safeHit.distance - settings.collisionOffset, 0f);
                        backPos = pivotBase + toCamDir * safeDist;
                    }
                }
            }

            // 지형 관통 방지: camDir 직선 위에서 minY를 만족하는 거리로 클램프
            const float CHECK_HEIGHT = 20f;
            const float CHECK_DIST   = 40f;
            Vector3 checkOrigin = new Vector3(backPos.x, backPos.y + CHECK_HEIGHT, backPos.z);
            if (Physics.Raycast(checkOrigin, Vector3.down, out RaycastHit groundHit, CHECK_DIST, _collisionLayers))
            {
                float minY = groundHit.point.y + settings.collisionOffset;
                if (backPos.y < minY)
                {
                    if (Mathf.Abs(camDir.y) > 0.001f)
                    {
                        float groundDist = (minY - _cameraPivot.position.y) / camDir.y;
                        float curDist    = Vector3.Distance(_cameraPivot.position, backPos);
                        if (groundDist >= settings.minDistance && groundDist <= curDist)
                            backPos = _cameraPivot.position + camDir * groundDist;
                    }
                    else
                    {
                        backPos.y = minY;
                    }
                }
            }

            // 캡슐 콜라이더 기반: 카메라가 몸 안으로 들어가는 임계 거리 / 전방 배치 거리 계산
            ComputeCapsuleClearance(_cameraPivot.position, camDir,
                out float backClearance, out float frontClearance);

            float backDist    = Vector3.Distance(_cameraPivot.position, backPos);
            float targetBlend = backDist < backClearance ? 1f : 0f;
            float blendSpeed  = targetBlend > _frontCameraBlend
                ? FRONT_BLEND_PULL_SPEED
                : FRONT_BLEND_RETURN_SPEED;
            _frontCameraBlend = blendSpeed > 0f
                ? Mathf.SmoothDamp(_frontCameraBlend, targetBlend, ref _frontCameraBlendVel, blendSpeed)
                : targetBlend;

            float   frontDist = Mathf.Max(frontClearance, 0.3f);
            Vector3 frontPos  = _cameraPivot.position + (-camDir) * frontDist;

            _mainCamera.transform.position = Vector3.Lerp(backPos, frontPos, _frontCameraBlend);
        }

        private void UpdateCameraRotation(float smoothTime)
        {
            Quaternion targetRot = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            if (smoothTime > 0f)
                _mainCamera.transform.rotation = Quaternion.Slerp(
                    _mainCamera.transform.rotation, targetRot,
                    1f - Mathf.Exp(-10f / smoothTime));
            else
                _mainCamera.transform.rotation = targetRot;
        }

        #endregion

        #region 전방 카메라 (충돌 회피)

        private void CacheCapsule()
        {
            _characterCapsule = _target != null
                ? _target.GetComponentInChildren<CapsuleCollider>()
                : null;
        }

        /// <summary>
        /// 캡슐 콜라이더 기하학으로 후방/전방 클리어런스를 계산.
        /// backClearance  : 카메라가 캡슐 밖에 있으려면 pivot에서 camDir 방향으로 필요한 최소 거리
        /// frontClearance : 전방 카메라가 캡슐 밖에 있으려면 pivot에서 -camDir 방향으로 필요한 최소 거리
        /// 캡슐의 중심선 위 가장 가까운 점을 구(球)로 근사해 직선-구 교점 공식으로 계산.
        /// </summary>
        private void ComputeCapsuleClearance(Vector3 pivotPos, Vector3 camDir,
            out float backClearance, out float frontClearance)
        {
            backClearance  = 0f;
            frontClearance = 0f;

            if (_characterCapsule == null) return;

            Transform t           = _characterCapsule.transform;
            Vector3   worldCenter = t.TransformPoint(_characterCapsule.center);
            Vector3   scale       = t.lossyScale;

            // 캡슐 방향 축과 스케일
            Vector3 axisLocal;
            float   rScale, hScale;
            switch (_characterCapsule.direction)
            {
                case 0:  // X
                    axisLocal = Vector3.right;
                    rScale    = Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                    hScale    = Mathf.Abs(scale.x);
                    break;
                case 2:  // Z
                    axisLocal = Vector3.forward;
                    rScale    = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
                    hScale    = Mathf.Abs(scale.z);
                    break;
                default: // Y (캐릭터 기본)
                    axisLocal = Vector3.up;
                    rScale    = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
                    hScale    = Mathf.Abs(scale.y);
                    break;
            }

            Vector3 axisWorld = t.TransformDirection(axisLocal).normalized;
            float   radius    = _characterCapsule.radius * rScale;
            float   halfCyl   = Mathf.Max(0f, _characterCapsule.height * hScale * 0.5f - radius);

            // pivot에서 캡슐 중심선 위의 가장 가까운 점 → 구(球) 근사 중심
            Vector3 p2c           = pivotPos - worldCenter;
            float   tOnAxis       = Mathf.Clamp(Vector3.Dot(p2c, axisWorld), -halfCyl, halfCyl);
            Vector3 nearestCenter = worldCenter + tOnAxis * axisWorld;

            // 직선-구 교점 (구 반지름 = 캡슐 radius + 카메라 SphereCast radius)
            float   effectiveR = radius + settings.cameraRadius;
            Vector3 oc         = pivotPos - nearestCenter;
            float   halfB      = Vector3.Dot(oc, camDir);
            float   cVal       = oc.sqrMagnitude - effectiveR * effectiveR;
            float   disc       = halfB * halfB - cVal;

            if (disc < 0f) return;  // 피벗이 캡슐과 완전히 동떨어진 경우

            float sqrtDisc = Mathf.Sqrt(disc);
            float t1       = -halfB - sqrtDisc;  // camDir 방향 진입점
            float t2       = -halfB + sqrtDisc;  // camDir 방향 탈출점

            // 카메라(후방)는 t2 이상이어야 캡슐 밖
            backClearance  = Mathf.Max(t2, 0f);
            // 전방 카메라는 -camDir 방향으로 -t1 이상이어야 캡슐 밖
            frontClearance = Mathf.Max(-t1, 0f);
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

            _alignTimer -= Time.deltaTime;
            if (_alignTimer <= 0f) { _isAligning = false; return; }

            Vector3 fwd        = _target.forward;
            float   targetYaw   = Mathf.Atan2(fwd.x, fwd.z) * Mathf.Rad2Deg;
            float   targetPitch = isCombat ? settings.combatPitch : settings.explorePitch;

            _currentYaw   = Mathf.LerpAngle(_currentYaw, targetYaw, Time.deltaTime * settings.alignSpeed);
            _currentPitch = Mathf.Lerp(_currentPitch, targetPitch, Time.deltaTime * settings.alignSpeed);
            float alignDynamicMin = settings.minVerticalAngle + ComputeSlopePitchOffset();
            _currentPitch = Mathf.Clamp(_currentPitch, alignDynamicMin, settings.maxVerticalAngle);
        }

        #endregion

        #region 초기화 / 리소스 로드

        private void InitializeCamera()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) { _target = player.transform; CacheCapsule(); }

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
                _cameraContext = new CameraRuntimeContext(_rigState);

            SyncCameraContext();

            _modeController = new CameraModeController(_cameraContext);
            _modeController.Register(new InGameCameraMode());
            _modeController.Register(new DialogueCameraMode());
            _modeController.SetMode(CameraModeType.InGame);
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
            _cameraContext.CombatStateProvider = _combatStateProvider;
            _cameraContext.ComputeSlopePitchOffset = ComputeSlopePitchOffset;
            _cameraContext.StartCameraAlign = StartCameraAlign;
            _cameraContext.LookAtOverride = _lookAtOverride;
            _cameraContext.LookAtOverrideOffset = _lookAtOverrideOffset;
            _cameraContext.CollisionLayers = _collisionLayers;
            _cameraContext.CharacterCapsule = _characterCapsule;
            _cameraContext.IsInputLocked = _isInputLocked;
            _cameraContext.IsAligning = _isAligning;
            _cameraContext.AlignTimer = _alignTimer;
            _cameraContext.HasActiveEffects = _effectManager?.HasActiveEffects ?? false;
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

        private void ApplyCameraPose(CameraRigPose pose)
        {
            if (_cameraPivot != null)
                _cameraPivot.position = pose.PivotPosition;

            if (_mainCamera == null) return;

            _mainCamera.transform.position = pose.CameraPosition;
            _mainCamera.transform.rotation = pose.CameraRotation;
            _mainCamera.fieldOfView = pose.FieldOfView;
        }

        private void LoadSettingsSync()
        {
            var handle = Addressables.LoadAssetAsync<CameraSettings>(SETTINGS_ADDRESSABLE_KEY);
            settings   = handle.WaitForCompletion();
            if (settings == null)
                Debug.LogError("[CameraManager] CameraSettings SO를 로드할 수 없습니다.");
        }

        private async void LoadCameraShakeDatabase()
        {
            try
            {
                _cameraShakeDatabase = await Addressables.LoadAssetAsync<CameraShakeDatabase>(CAMERA_SHAKE_DB_KEY).Task;
                _cameraShakeDatabase?.Initialize();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CameraManager] CameraShakeDatabase 로드 실패: {e.Message}");
            }
        }

        private async void LoadDialogueCameraSettings()
        {
            try
            {
                _dialogueCameraSettings = await Addressables.LoadAssetAsync<DialogueCameraSettingsSO>(DIALOGUE_CAMERA_SETTINGS_KEY).Task;
                SyncCameraContext();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CameraManager] DialogueCameraSettings 로드 실패 또는 미등록: {e.Message}");
            }
        }

        private async void LoadKillCamData()
        {
            try
            {
                var data = await Addressables.LoadAssetAsync<KillCamData>(KILL_CAM_DATA_KEY).Task;
                if (data != null) _killCamController = new KillCamController(this, data);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CameraManager] KillCamData 로드 실패: {e.Message}");
            }
        }

        private async void LoadPerfectGuardFOVData()
        {
            try
            {
                var data = await Addressables.LoadAssetAsync<FOVCameraEffectData>(PERFECT_GUARD_FOV_KEY).Task;
                PlayerGuardState.SetPerfectGuardFOVData(data);
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
            _target = newTarget;
            CacheCapsule();
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
            return PushCameraMode(CameraModeType.Dialogue, new CameraModeEnterParams
            {
                PrimaryTarget = speaker,
                SecondaryTarget = listener,
                Offset = offset
            });
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
        public void SetCombatStateProvider(System.Func<bool> p)
        {
            _combatStateProvider = p;
            SyncCameraContext();
        }

        // ── Shake / Punch ──────────────────────────────────────────────
        public void StartShake(CameraShakeData data)
        {
            if (data == null || _shaker == null) return;
            _shaker.SetShakeData(data);
            _shaker.StartShake();
        }

        public void StartShake(string key)
        {
            if (_cameraShakeDatabase != null)
                StartShake(_cameraShakeDatabase.GetShakeData(key));
        }

        public void StartShake(CameraShakeIdType key) => StartShake(key.ToKey());

        public void StopShake() => _shaker?.StopShake();

        public void Punch(Vector3 direction, float strength, float duration = 0.15f) =>
            _shaker?.Punch(direction, strength, duration);

        // ── KillCam ────────────────────────────────────────────────────
        public bool TryKillCam(Transform victim) =>
            _killCamController != null && _killCamController.TryExecute(victim);

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
