using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UPlayGround.CameraEffects;
using UPlayGround.Data;
using UPlayGround.Data.Config;
using UPlayGround.Data.Path;
using UPlayGround.InputDefine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// TPS 카메라 시스템 관리 매니저
    /// 
    /// 기능:
    /// - 타겟 추적: LateUpdate에서 부드럽게 타겟을 따라감
    /// - 마우스 우클릭 + 드래그: 카메라 회전
    /// - 마우스 스크롤: 줌인/줌아웃
    /// - 충돌 감지: 벽에 막히면 자동으로 카메라 당김
    /// 
    /// </summary>
    public class CameraManager : BaseManager<CameraManager>, IManager
    {
        // 대상 정보를 담는 헬퍼 클래스
        private class TargetInfo
        {
            public Transform transform;
            public float distance;
            public Vector3 direction;
        }

        //Target
        private Transform target; // 추적할 타겟 (플레이어)

        //Camera Settings
        private Vector3 cameraOffset = new Vector3(0f, 1f, 0f); // 타겟 기준 카메라 피벗 오프셋

        private float defaultDistance = 5f; // 기본 거리
        private float minDistance = 1.5f; // 최소 거리
        private float maxDistance = 10f; // 최대 거리

        //Rotation Settings
        private float rotationSpeed = 20f; // 카메라 회전 속도
        private float minVerticalAngle = -30f; // 최소 수직 각도
        private float maxVerticalAngle = 70f; // 최대 수직 각도

        //Zoom Settings
        private float zoomSpeed = 0.5f; // 줌 속도
        private float zoomSmoothTime = 0.1f; // 줌 부드러움

        //Smooth Settings
        private float positionSmoothTime = 0.1f; // 위치 부드러움
        private float rotationSmoothTime = 0.1f; // 회전 부드러움

        //Collision Settings
        private LayerMask collisionLayers = -1; // 충돌 레이어
        private float collisionOffset = 0.2f; // 충돌 오프셋
        private float cameraRadius = 0.3f; // 카메라 반지름 (SphereCast용)
        
        //LockOn Settings
        private LayerMask lockOnLayerMask; // LockOn 대상 레이어
        private float lockOnRange = 15f; // LockOn 최대 거리

        private float targetSwitchCooldown = 0.2f; // 전환 쿨다운 (연타 방지)

        // Camera Align
        private float cameraAlignSpeed = 5f; // 보정 속도
        private float cameraAlignDuration = 0.5f; // 보정 지속 시간

        // 내부 캐싱 & 계산
        private Camera mainCamera;
        private Transform cameraPivot; // 카메라가 회전할 피벗 포인트

        private float currentDistance;
        private float targetDistance;
        private float distanceVelocity;

        private float currentYaw; // 좌우 회전 (Y축)
        private float currentPitch; // 상하 회전 (X축)

        // LockOn 관련
        private List<Transform> availableTargets = new List<Transform>(); // 사용 가능한 대상 리스트
        private int currentTargetIndex = -1; // 현재 선택된 대상 인덱스
        private float lastSwitchTime; // 마지막 전환 시간

        private Transform lockOnTarget; // 현재 LockOn 대상
        private CapsuleCollider lockOnTargetCollider; // LockOn 대상 Collider
        private bool isLockOnActive; // LockOn 활성화 상태

        // LockOn 대상 Y축 스무딩 (점프 등으로 인한 카메라 튀김 방지)
        private float lockOnTargetSmoothY;
        private float lockOnTargetYVelocity;
        private float lockOnYSmoothTime = 0.3f; // Y축 추적 부드러움 (높을수록 느리게 따라감)

        private bool isCameraAligning; // 카메라 보정 중인지
        private float cameraAlignTimer; // 보정 타이머

        // LockOn 해제 전환 연출 (대상 사망/소실 시 부드러운 복귀)
        private bool _isLockOnTransitioning;      // 전환 중인지
        private float _lockOnTransitionTimer;      // 전환 남은 시간
        private float _lockOnTransitionDuration = 0.3f; // 전환 지속 시간 (현재 방향 유지)
        private float _lockOnTransitionYaw;        // 전환 시작 시점의 Yaw
        private float _lockOnTransitionPitch;      // 전환 시작 시점의 Pitch

        private Vector3 positionVelocity;
        private Vector3 smoothPosition;

        private CameraShaker _shaker;
        
        private const string CAMERA_SHAKE_DATABASE_PATH = "CameraShakeDatabase";
        private CameraShakeDatabase _cameraShakeDatabase;
        
        private const string CAMERA_IMPACT_PRESET_DATABASE_PATH = "CameraImpactPresetDatabase";
        private CameraImpactPresetDatabase _impactPresetDatabase;

        // 킬캠
        private const string KILL_CAM_DATA_PATH = "KillCamData";
        private KillCamController _killCamController;
        
        // 입력 잠금 (킬캠 등 연출 중 카메라 조작 차단)
        private bool _isInputLocked;

        // 컨텍스트 기반 카메라 오프셋 (전투/비전투 숄더 전환)
        private Vector3 _defaultOffset = new Vector3(0f, 1f, 0f);       // 비전투 (센터)
        private Vector3 _combatOffset = new Vector3(0.5f, 1.2f, 0f);   // 전투 (숄더 뷰)
        private Vector3 _offsetVelocity;
        private float _offsetSmoothTime = 0.35f;  // 오프셋 전환 부드러움
        private System.Func<bool> _combatStateProvider;  // 전투 상태 조회 함수
        
        private readonly CameraEffectStack _effectStack = new CameraEffectStack();
        private CameraEffectOutput _effectOutput;
        private float _defaultFov = 60f;
        private bool _isTimeScaleControlledByEffects;

        #region IManager 구현

        public void Init()
        {
            Debug.Log("[CameraManager] 초기화 시작");

            InitializeCamera();
            LoadCameraShakeDatabase();
            LoadImpactPresetDatabase();
            
            // 메인 카메라 찾기
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[CameraManager] 메인 카메라를 찾을 수 없습니다!");
                return;
            }
            _defaultFov = mainCamera.fieldOfView;

            // 카메라 피벗 생성 (타겟을 중심으로 회전)
            GameObject pivotObj = new GameObject("CameraPivot");
            cameraPivot = pivotObj.transform;
            cameraPivot.SetParent(transform);

            // 초기값 설정
            currentDistance = defaultDistance;
            targetDistance = defaultDistance;
            currentYaw = 0f;
            currentPitch = 20f; // 기본 각도

            collisionLayers = CameraConfig.GetCollisionLayerMask();

            if (target != null)
            {
                // 타겟이 있으면 타겟 위치로 초기화
                cameraPivot.position = target.position + cameraOffset;
                smoothPosition = cameraPivot.position;

                // 카메라 초기 위치 설정
                SetInitialCameraPosition();
            }
            else
            {
                // 타겟이 없으면 원점 기준으로 설정
                cameraPivot.position = cameraOffset;
                smoothPosition = cameraPivot.position;

                Debug.LogWarning("[CameraManager] 타겟이 설정되지 않았습니다. CameraInitializer를 사용하여 타겟을 설정하세요.");

                // 카메라 초기 위치 설정
                SetInitialCameraPosition();
            }

            if (lockOnLayerMask == 0)
            {
                lockOnLayerMask = CameraConfig.GetLockOnLayerMask();
            }
            
            GameObject shakerGO = new GameObject("CameraShaker");
            shakerGO.hideFlags = HideFlags.HideAndDontSave;
            
            _shaker = shakerGO.AddComponent<CameraShaker>();
            _shaker.hideFlags = HideFlags.HideAndDontSave;
            
            LoadKillCamData();
            
            Debug.Log("[CameraManager] 초기화 완료");
        }

        public void AfterInit()
        {
            InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOn,
                null, OnInputPerformedLockOn, null, null, null, InputLayer.Level_1);

            InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchLeft,
                null, OnInputPerformedLockOnSwitchLeft, null, null, null, InputLayer.Level_1);

            InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchRight,
                null, OnInputPerformedLockOnRight, null, null, null, InputLayer.Level_1);
        }

        /// <summary>
        /// 카메라 초기 위치 설정
        /// </summary>
        private void SetInitialCameraPosition()
        {
            // 초기 회전 계산
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -currentDistance);

            // 카메라 위치 및 회전 설정
            mainCamera.transform.position = cameraPivot.position + offset;
            mainCamera.transform.rotation = rotation;
        }

        public void Dispose()
        {
            Debug.Log("[CameraManager] 정리 시작");

            _killCamController?.ForceStop();
            StopAllEffects();

            if (cameraPivot != null)
            {
                Destroy(cameraPivot.gameObject);
            }

            if (InputManager.Instance != null)
            {
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOn,
                    null, OnInputPerformedLockOn, null);

                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchLeft,
                    null, OnInputPerformedLockOnSwitchLeft, null);

                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchRight,
                    null, OnInputPerformedLockOnRight, null);
            }

            Debug.Log("[CameraManager] 정리 완료");
        }

        public void OnUpdate()
        {
            if (target == null || mainCamera == null || cameraPivot == null)
                return;

            HandleInput();
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
            if (mainCamera == null || cameraPivot == null)
                return;

            UpdateCameraEffects();
            
            if (target == null)
            {
                ApplyPostCameraEffects();
                return;
            }

            UpdateLockOnTransition();
            UpdateLockOnRotation();
            UpdateCameraAlign();
            UpdateContextOffset();
            
            UpdateCameraPosition();
            UpdateCameraRotation();
            ApplyPostCameraEffects();
        }

        #endregion

        #region 입력 처리

        /// <summary>
        /// 카메라 입력 처리
        /// </summary>
        private void HandleInput()
        {
            if (Cursor.visible || _isInputLocked)
                return;

            // 입력 시스템 선택
            if (InputManager.Instance != null)
            {
                HandleNewInputSystem();
            }
        }

        /// <summary>
        /// New Input System 입력 처리
        /// </summary>
        private void HandleNewInputSystem()
        {
            var inputManager = InputManager.Instance;

            // LockOn 활성화 시에는 수동 회전 입력 무시
            if (isLockOnActive == false && isCameraAligning == false)
            {
                // 카메라 회전 (Look 액션 사용)
                if (inputManager.GetAction(InputMapNames.PlayerAction, PlayerAction.Look, out InputAction lookAction))
                {
                    Vector2 lookInput = lookAction.ReadValue<Vector2>();

                    currentYaw += lookInput.x * rotationSpeed * 0.01f;
                    currentPitch -= lookInput.y * rotationSpeed * 0.01f;

                    // 상하 각도 제한
                    currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
                }
            }

            // 마우스 스크롤로 줌인/줌아웃 
            if (inputManager.GetAction(InputMapNames.PlayerAction, PlayerAction.Zoom, out InputAction zoomAction))
            {
                Vector2 scrollValue = zoomAction.ReadValue<Vector2>();
                float scrollInput = scrollValue.y;

                if (Mathf.Abs(scrollInput) > 0.01f)
                {
                    targetDistance -= scrollInput * zoomSpeed;
                    targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
                }
            }
        }

        #endregion

        #region 카메라 업데이트

        /// <summary>
        /// 카메라 위치 업데이트
        /// </summary>
        private void UpdateCameraPosition()
        {
            // 타겟 위치 + 오프셋으로 피벗 이동
            Vector3 targetPivotPosition = target.position + cameraOffset;
            smoothPosition = Vector3.SmoothDamp(smoothPosition, targetPivotPosition, ref positionVelocity,
                positionSmoothTime);
            cameraPivot.position = smoothPosition;

            // 거리 부드럽게 조정
            float effectDistance = targetDistance + _effectOutput.DistanceOffset;
            effectDistance = Mathf.Clamp(effectDistance, minDistance, maxDistance);
            currentDistance = Mathf.SmoothDamp(currentDistance, effectDistance, ref distanceVelocity, zoomSmoothTime);

            // 회전을 적용한 카메라 위치 계산
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 desiredPosition = cameraPivot.position + rotation * new Vector3(0f, 0f, -currentDistance);

            // 충돌 감지
            desiredPosition = HandleCollision(cameraPivot.position, desiredPosition);
            desiredPosition += _effectOutput.WorldPositionOffset;

            // 카메라 위치 적용
            mainCamera.transform.position = desiredPosition;
        }

        /// <summary>
        /// 카메라 회전 업데이트
        /// </summary>
        private void UpdateCameraRotation()
        {
            // 카메라가 피벗을 바라보도록 회전
            Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

            if (rotationSmoothTime > 0f)
            {
                mainCamera.transform.rotation = Quaternion.Slerp(
                    mainCamera.transform.rotation,
                    targetRotation,
                    1f - Mathf.Exp(-10f / rotationSmoothTime)
                );
            }
            else
            {
                mainCamera.transform.rotation = targetRotation;
            }
        }

        private void UpdateCameraEffects()
        {
            _effectOutput.Reset();
            CameraEffectContext context =
                new CameraEffectContext(this, mainCamera, target, targetDistance, _defaultFov);

            _effectStack.Evaluate(context, Time.unscaledDeltaTime, ref _effectOutput);

            if (_effectOutput.HasTimeScale)
            {
                Time.timeScale = _effectOutput.TimeScale;
                _isTimeScaleControlledByEffects = true;
            }
            else if (_isTimeScaleControlledByEffects)
            {
                Time.timeScale = 1f;
                _isTimeScaleControlledByEffects = false;
            }
        }

        private void ApplyPostCameraEffects()
        {
            mainCamera.transform.position += mainCamera.transform.TransformVector(_effectOutput.LocalPositionOffset);
            mainCamera.transform.rotation *= Quaternion.Euler(_effectOutput.LocalEulerOffset);
            mainCamera.fieldOfView = Mathf.Max(1f, _defaultFov + _effectOutput.FovOffset);
        }

        /// <summary>
        /// 컨텍스트 기반 카메라 오프셋 보간
        /// 전투 시 숄더 뷰, 비전투 시 센터 뷰로 부드럽게 전환
        /// 킬캠 등 연출 중에는 보간을 건너뛴다 (연출이 오프셋을 직접 제어하므로)
        /// </summary>
        private void UpdateContextOffset()
        {
            // 킬캠 등 연출이 오프셋을 직접 제어 중이면 보간 건너뜀
            if (_isInputLocked)
                return;

            bool isCombat = _combatStateProvider?.Invoke() ?? false;
            Vector3 targetOffset = isCombat ? _combatOffset : _defaultOffset;
            cameraOffset = Vector3.SmoothDamp(cameraOffset, targetOffset, ref _offsetVelocity, _offsetSmoothTime);
        }

        /// <summary>
        /// 충돌 처리
        /// </summary>
        private Vector3 HandleCollision(Vector3 origin, Vector3 desiredPosition)
        {
            Vector3 direction = desiredPosition - origin;
            float distance = direction.magnitude;
            
            // 안전 거리 = 충돌 오프셋 + 카메라 반지름
            float safetyMargin = collisionOffset + cameraRadius;

            // SphereCast로 충돌 체크 (캐릭터 자신과 자식 오브젝트는 무시)
            RaycastHit[] hits = Physics.SphereCastAll(origin, cameraRadius, direction.normalized, distance, collisionLayers);
            
            float closestDistance = distance;
            Vector3 closestHitPoint = desiredPosition;
            Vector3 closestHitNormal = -direction.normalized;
            
            foreach (RaycastHit hit in hits)
            {
                // 가장 가까운 충돌 지점 찾기
                if (hit.distance < closestDistance)
                {
                    closestDistance = hit.distance;
                    closestHitPoint = hit.point;
                    closestHitNormal = hit.normal;
                }
            }

            // 충돌이 발생했다면 안전 거리만큼 카메라를 뒤로 당김
            if (closestDistance < distance)
            {
                // 충돌 지점에서 안전 거리만큼 뒤로 당김 (origin 방향 유지)
                float safeDistance = Mathf.Max(closestDistance - safetyMargin, minDistance);
                return origin + direction.normalized * safeDistance;
            }

            return desiredPosition;
        }

        #endregion

        #region Public API

        /// <summary>
        /// 타겟 설정
        /// </summary>
        public void SetTarget(Transform newTarget)
        {
            target = newTarget;

            if (target != null && cameraPivot != null)
            {
                cameraPivot.position = target.position + cameraOffset;
                smoothPosition = cameraPivot.position;

                // 카메라 위치 즉시 업데이트
                if (mainCamera != null)
                {
                    Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
                    Vector3 offset = rotation * new Vector3(0f, 0f, -currentDistance);
                    mainCamera.transform.position = cameraPivot.position + offset;
                    mainCamera.transform.rotation = rotation;
                }
            }
        }

        /// <summary>
        /// 현재 타겟 가져오기
        /// </summary>
        public Transform GetTarget()
        {
            return target;
        }

        /// <summary>
        /// 카메라 거리 설정
        /// </summary>
        public void SetDistance(float distance)
        {
            targetDistance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        /// <summary>
        /// 카메라 회전 설정
        /// </summary>
        public void SetRotation(float yaw, float pitch)
        {
            currentYaw = yaw;
            currentPitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
        }

        /// <summary>
        /// 카메라 오프셋 설정
        /// </summary>
        public void SetCameraOffset(Vector3 offset)
        {
            cameraOffset = offset;
        }

        public void StartShake(CameraShakeData cameraShakeData)
        {
            if (cameraShakeData == null)
            {
                return;
            }
            _shaker.SetShakeData(cameraShakeData);
            _shaker.StartShake();
        }

        public void StartShake(string shakeDataKey)
        {
            if (_cameraShakeDatabase == null)
            {
                return;
            }

            StartShake(_cameraShakeDatabase.GetShakeData(shakeDataKey));
        }

        public void StopShake()
        {
            _shaker.StopShake();
        }

        public void PlayEffect(ICameraEffect effect)
        {
            if (mainCamera == null)
            {
                return;
            }

            CameraEffectContext context =
                new CameraEffectContext(this, mainCamera, target, targetDistance, _defaultFov);
            _effectStack.Play(effect, context);
        }

        public void StopEffect(string effectId)
        {
            _effectStack.Stop(effectId);
        }

        public void StopAllEffects()
        {
            _effectStack.StopAll();
            _effectOutput.Reset();
            if (_isTimeScaleControlledByEffects)
            {
                Time.timeScale = 1f;
                _isTimeScaleControlledByEffects = false;
            }

            if (mainCamera != null)
            {
                mainCamera.fieldOfView = _defaultFov;
            }
        }

        public void PlayRotationEffect(string effectId, Vector3 rotationEuler, float holdDuration, float blendIn = 0.1f,
            float blendOut = 0.12f)
        {
            PlayEffect(new RotationCameraEffect(effectId, rotationEuler, holdDuration, blendIn, blendOut));
        }

        public void PlaySmoothDampEffect(string effectId, Vector3 localOffset, float holdDuration, float smoothTime = 0.12f,
            float blendIn = 0.1f, float blendOut = 0.12f)
        {
            PlayEffect(new SmoothDampCameraEffect(effectId, localOffset, holdDuration, smoothTime, blendIn, blendOut));
        }

        public void PlaySpringDampEffect(string effectId, Vector3 localOffset, float holdDuration, float stiffness = 90f,
            float damping = 16f, float blendIn = 0.05f, float blendOut = 0.15f)
        {
            PlayEffect(new SpringDampCameraEffect(effectId, localOffset, holdDuration, stiffness, damping, blendIn,
                blendOut));
        }

        public void PlayZoomEffect(string effectId, float distanceOffset, float holdDuration, float blendIn = 0.1f,
            float blendOut = 0.12f)
        {
            PlayEffect(new ZoomCameraEffect(effectId, distanceOffset, holdDuration, blendIn, blendOut));
        }

        public void PlayFovEffect(string effectId, float fovOffset, float holdDuration, float blendIn = 0.1f,
            float blendOut = 0.15f)
        {
            PlayEffect(new FovCameraEffect(effectId, fovOffset, holdDuration, blendIn, blendOut));
        }

        public void PlayTimeScaleEffect(string effectId, float timeScale, float holdDuration, float blendIn = 0.02f,
            float blendOut = 0.08f)
        {
            PlayEffect(new TimeScaleCameraEffect(effectId, timeScale, holdDuration, blendIn, blendOut));
        }

        public void PlayProceduralShakeEffect(string effectId, Vector3 amplitude, float frequency, float holdDuration,
            float blendIn = 0.02f, float blendOut = 0.15f)
        {
            PlayEffect(new ProceduralShakeCameraEffect(effectId, amplitude, frequency, holdDuration, blendIn, blendOut));
        }

        public void PlayLegacyShakeEffect(string effectId, string shakeKey, float holdDuration, float blendOut = 0.1f)
        {
            PlayEffect(new LegacyShakeCameraEffect(effectId, shakeKey, holdDuration, blendOut));
        }

        public void PlayImpactPreset(CameraImpactPreset preset, string effectGroupId = "impact")
        {
            CameraEffectPresetRunner.Play(this, preset, effectGroupId);
        }

        public void StopImpactPreset(string effectGroupId = "impact")
        {
            CameraEffectPresetRunner.Stop(this, effectGroupId);
        }

        /// <summary>
        /// 방향성 카메라 펀치 (타격 방향에 따라 카메라를 밀어내는 연출)
        /// </summary>
        /// <param name="direction">타격 방향 (월드 스페이스)</param>
        /// <param name="strength">펀치 강도</param>
        /// <param name="duration">펀치 지속 시간</param>
        public void Punch(Vector3 direction, float strength, float duration = 0.15f)
        {
            _shaker.Punch(direction, strength, duration);
        }

        /// <summary>
        /// 현재 카메라 거리 반환
        /// </summary>
        public float GetCurrentDistance()
        {
            return targetDistance;
        }

        /// <summary>
        /// 현재 카메라 오프셋 반환
        /// </summary>
        public Vector3 GetCurrentOffset()
        {
            return cameraOffset;
        }

        /// <summary>
        /// 카메라 입력 잠금/해제 (연출 중 조작 차단)
        /// </summary>
        public void SetInputLock(bool locked)
        {
            _isInputLocked = locked;
        }

        /// <summary>
        /// 킬캠 연출 시도
        /// </summary>
        /// <param name="victim">사망한 적의 Transform</param>
        /// <returns>연출이 실행됐으면 true</returns>
        public bool TryKillCam(Transform victim)
        {
            return _killCamController != null && _killCamController.TryExecute(victim);
        }

        /// <summary>
        /// 킬캠 연출 중인지 확인
        /// </summary>
        public bool IsKillCamPlaying => _killCamController?.IsPlaying ?? false;

        /// <summary>
        /// 전투 상태 조회 함수 등록
        /// 매 프레임 이 함수를 호출하여 전투 여부를 판단한다.
        /// </summary>
        /// <param name="provider">전투 중이면 true를 반환하는 함수</param>
        public void SetCombatStateProvider(System.Func<bool> provider)
        {
            _combatStateProvider = provider;
        }

        /// <summary>
        /// 비전투 기본 오프셋 설정 (외부에서 튜닝 가능)
        /// </summary>
        public void SetDefaultOffset(Vector3 offset)
        {
            _defaultOffset = offset;
        }

        /// <summary>
        /// 전투 시 숄더 오프셋 설정 (외부에서 튜닝 가능)
        /// </summary>
        public void SetCombatOffset(Vector3 offset)
        {
            _combatOffset = offset;
        }
        #endregion

        #region Gizmos

        private void OnDrawGizmosSelected()
        {
            if (target == null || !Application.isPlaying)
                return;

            // 피벗 위치 표시
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(target.position + cameraOffset, 0.3f);

            // 카메라와 피벗 연결선
            if (mainCamera != null)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawLine(target.position + cameraOffset, mainCamera.transform.position);
            }

            // LockOn 범위 표시
            Gizmos.color = isLockOnActive ? Color.red : Color.green;
            Gizmos.DrawWireSphere(target.position, lockOnRange);

            // LockOn 대상 표시
            if (isLockOnActive && lockOnTarget != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(target.position, lockOnTarget.position);
                Gizmos.DrawWireSphere(lockOnTarget.position, 0.5f);
            }
        }

        #endregion

        private async void LoadCameraShakeDatabase()
        {
            var handle = Addressables.LoadAssetAsync<CameraShakeDatabase>(CAMERA_SHAKE_DATABASE_PATH);

            try
            {
                _cameraShakeDatabase = await handle.Task;

                if (_cameraShakeDatabase == null)
                {
                    Debug.LogError($"[CameraManager] CameraShakeDatabase '{CAMERA_SHAKE_DATABASE_PATH}' 경로에서 찾을 수 없습니다.");
                    return;
                }

                _cameraShakeDatabase.Initialize();
                Debug.Log($"[CameraManager] CameraShakeDatabase 로드 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CameraManager] CameraShakeDatabase 로드 실패: {e.Message}");
            }
        }

        private async void LoadImpactPresetDatabase()
        {
            var handle = Addressables.LoadAssetAsync<CameraImpactPresetDatabase>(CAMERA_IMPACT_PRESET_DATABASE_PATH);

            try
            {
                _impactPresetDatabase = await handle.Task;
                _impactPresetDatabase?.Initialize();
            }
            catch
            {
                _impactPresetDatabase = null;
            }
        }

        public CameraImpactPresetDatabase GetImpactPresetDatabase()
        {
            return _impactPresetDatabase;
        }

        private async void LoadKillCamData()
        {
            var handle = Addressables.LoadAssetAsync<KillCamData>(KILL_CAM_DATA_PATH);

            try
            {
                var killCamData = await handle.Task;

                if (killCamData == null)
                {
                    Debug.LogWarning($"[CameraManager] KillCamData '{KILL_CAM_DATA_PATH}' 경로에서 찾을 수 없습니다. 킬캠 비활성.");
                    return;
                }

                _killCamController = new KillCamController(this, killCamData);
                Debug.Log($"[CameraManager] KillCamData 로드 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[CameraManager] KillCamData 로드 실패 (킬캠 비활성): {e.Message}");
            }
        }
        private void InitializeCamera()
        {
            string playerTag = "Player";
            Transform playerTarget = null;

            // 타겟 찾기
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                playerTarget = player.transform;
            }

            // 타겟 설정 (마지막에 설정하여 위치가 즉시 업데이트되도록)
            if (playerTarget != null)
            {
                CameraManager.Instance.SetTarget(playerTarget);
            }
        }

        private void OnInputPerformedLockOn(InputAction.CallbackContext obj)
        {
            if (target == null)
                return;

            if (isLockOnActive)
            {
                // LockOn 해제
                ReleaseLockOn();
            }
            else
            {
                // LockOn 실패 시 캐릭터 방향으로 카메라 보정
                if (TryLockOn() == false)
                {
                    StartCameraAlign();
                }
            }
        }

        private void OnInputPerformedLockOnRight(InputAction.CallbackContext obj)
        {
            if (!isLockOnActive || availableTargets.Count <= 1)
                return;

            // 쿨다운 체크
            if (Time.time - lastSwitchTime < targetSwitchCooldown)
                return;

            CollectLockOnTarget();
            
            SwitchTarget(1); // 오른쪽 = 인덱스 증가
        }

        private void OnInputPerformedLockOnSwitchLeft(InputAction.CallbackContext obj)
        { 
            if (isLockOnActive == false || availableTargets.Count <= 1)
                return;

            // 쿨다운 체크
            if (Time.time - lastSwitchTime < targetSwitchCooldown)
                return;

            CollectLockOnTarget();

            SwitchTarget(-1); // 왼쪽 = 인덱스 감소
        }

        #region LockOn 시스템

        /// <summary>
        /// LockOn 시도 - IDamageable을 가진 대상 검색
        /// </summary>
        private bool TryLockOn()
        {
            CollectLockOnTarget();
            
            if (availableTargets.Count > 0)
            {
                currentTargetIndex = 0;
                lockOnTarget = availableTargets[currentTargetIndex];
                isLockOnActive = true;

                lockOnTarget.GetComponent<IDamageable>()?.LockOn();
                lockOnTargetCollider = lockOnTarget.GetComponent<CapsuleCollider>();
                
                // Y축 스무딩 즉시 초기화 (락온 시작 시 튀김 방지)
                InitLockOnSmoothY();
                
                return true;
            }

            return false;
        }

        private void CollectLockOnTarget()
        {
            Vector3 origin = target.position;
            Vector3 originPlanar = new Vector3(origin.x, 0, origin.z);

            // 범위 내 모든 Collider 검출
            Collider[] hits = Physics.OverlapSphere(origin, lockOnRange, lockOnLayerMask);

            availableTargets.Clear();

            List<TargetInfo> targetInfos = new List<TargetInfo>();

            foreach (var hit in hits)
            {
                // 자기 자신 제외
                if (hit.transform == target || hit.transform.IsChildOf(target))
                    continue;

                // IDamageable 확인
                var damageable = hit.GetComponent<IDamageable>();
                if (damageable == null)
                    damageable = hit.GetComponentInParent<IDamageable>();

                if (damageable == null || !damageable.CanTakeDamage())
                    continue;

                // 각도 체크
                Vector3 targetPos = hit.transform.position;
                Vector3 targetPosPlanar = new Vector3(targetPos.x, 0, targetPos.z);
        
                float distanceSq = (targetPosPlanar - originPlanar).sqrMagnitude;
                
                targetInfos.Add(new TargetInfo
                {
                    transform = hit.transform,
                    distance = distanceSq // TargetInfo에 distance 필드 추가 필요
                });
            }
            
            // 거리 순(오름차순)으로 정렬
            targetInfos.Sort((a, b) => a.distance.CompareTo(b.distance));
            
            // Transform 리스트로 변환
            foreach (var info in targetInfos)
            {
                availableTargets.Add(info.transform);
            }
        }
        /// <summary>
        /// 캐릭터 뒷통수 방향으로 카메라 보정 시작
        /// </summary>
        private void StartCameraAlign()
        {
            isCameraAligning = true;
            cameraAlignTimer = cameraAlignDuration;
        }

        /// <summary>
        /// 카메라 보정 업데이트 (OnLateUpdate에서 호출)
        /// </summary>
        private void UpdateCameraAlign()
        {
            if (!isCameraAligning || target == null)
                return;

            // 타이머 감소
            cameraAlignTimer -= Time.deltaTime;

            if (cameraAlignTimer <= 0f)
            {
                isCameraAligning = false;
                return;
            }

            // 캐릭터의 forward 방향 (뒷통수 방향)
            Vector3 targetForward = target.forward;
            float targetPitch = 15f;
            
            // 목표 Yaw 계산 (캐릭터가 바라보는 방향)
            float targetYaw = Mathf.Atan2(targetForward.x, targetForward.z) * Mathf.Rad2Deg;

            // 부드럽게 회전
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * cameraAlignSpeed);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * cameraAlignSpeed);
    
            // Pitch 각도 제한
            currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
        }

        /// <summary>
        /// 다음 락온 가능한 대상 탐색
        /// </summary>
        /// <returns>새로운 대상을 찾으면 true</returns>
        private bool TryFindNextLockOnTarget()
        {
            // 현재 대상 해제
            if (lockOnTarget != null)
            {
                lockOnTarget.GetComponent<IDamageable>()?.UnLockOn();
            }
            
            // 대상 리스트 갱신
            CollectLockOnTarget();
            
            // 유효한 대상이 없으면 실패
            if (availableTargets.Count == 0)
                return false;
            
            // 가장 가까운 대상(리스트 첫 번째)으로 락온
            lockOnTarget = availableTargets[0];
            lockOnTargetCollider = lockOnTarget.GetComponent<CapsuleCollider>();
            currentTargetIndex = 0;
            
            lockOnTarget.GetComponent<IDamageable>()?.LockOn();
            
            // Y축 스무딩 즉시 초기화
            InitLockOnSmoothY();
            
            return true;
        }

        /// <summary>
        /// LockOn 해제
        /// </summary>
        private void ReleaseLockOn()
        {
            if (lockOnTarget != null)
            {
                IDamageable target = lockOnTarget.GetComponent<IDamageable>();
                if (target != null)
                {
                    target.UnLockOn();
                }
            }
            
            lockOnTarget = null;
            lockOnTargetCollider = null;
            isLockOnActive = false;
            availableTargets.Clear();
            currentTargetIndex = -1;
            lockOnTargetYVelocity = 0f;
            _isLockOnTransitioning = false;
        }

        /// <summary>
        /// 락온 대상 소실 시 부드러운 전환 연출 시작
        /// 현재 카메라 방향을 잠시 유지한 뒤, 캐릭터 후방으로 서서히 복귀
        /// </summary>
        private void StartLockOnTransition()
        {
            _isLockOnTransitioning = true;
            _lockOnTransitionTimer = _lockOnTransitionDuration;
            _lockOnTransitionYaw = currentYaw;
            _lockOnTransitionPitch = currentPitch;
        }

        /// <summary>
        /// 락온 해제 전환 연출 업데이트
        /// Phase 1: 현재 방향 유지 (멈칫)
        /// Phase 2: CameraAlign으로 캐릭터 후방으로 복귀
        /// </summary>
        private void UpdateLockOnTransition()
        {
            if (!_isLockOnTransitioning)
                return;

            _lockOnTransitionTimer -= Time.deltaTime;

            // Phase 1: 현재 방향 유지 (타이머 동안 Yaw/Pitch 고정)
            if (_lockOnTransitionTimer > 0f)
            {
                currentYaw = _lockOnTransitionYaw;
                currentPitch = _lockOnTransitionPitch;
                return;
            }

            // Phase 2: 전환 완료 → 락온 해제 + 캐릭터 후방으로 카메라 보정
            _isLockOnTransitioning = false;
            ReleaseLockOn();
            StartCameraAlign();
        }

        /// <summary>
        /// LockOn Y축 스무딩 초기화 (대상 전환/시작 시 즉시 현재 위치로 세팅)
        /// </summary>
        private void InitLockOnSmoothY()
        {
            if (lockOnTarget == null) return;
            
            float heightOffset = (lockOnTargetCollider != null) 
                ? lockOnTargetCollider.height * 0.25f : 1f;
            
            lockOnTargetSmoothY = lockOnTarget.position.y - heightOffset;
            lockOnTargetYVelocity = 0f;
        }

        /// <summary>
        /// LockOn 대상 추적 회전
        /// </summary>
        private void UpdateLockOnRotation()
        {
            if (isLockOnActive == false || lockOnTarget == null)
                return;

            // 전환 연출 중이면 추적 회전 건너뜀
            if (_isLockOnTransitioning)
                return;
            
            // 대상 유효성 체크 실패 시, 다른 대상 탐색
            if (IsValidTarget(lockOnTarget) == false)
            {
                if (TryFindNextLockOnTarget() == false)
                {
                    // 즉시 해제 대신 부드러운 전환 연출 시작
                    StartLockOnTransition();
                    return;
                }
            }
            
            // 대상이 너무 멀어지면 전환 연출 후 해제
            float distance = Vector3.Distance(target.position, lockOnTarget.position);
            if (distance > lockOnRange)
            {
                if (TryFindNextLockOnTarget() == false)
                {
                    StartLockOnTransition();
                }
                return;
            }

            // [TODO] 옵션으로 빼고 싶다
            float lockOnHeightOffset = (lockOnTargetCollider != null) 
                ? lockOnTargetCollider.height * 0.25f : 1f;
            
            // 대상의 실제 Y 위치
            float rawTargetY = lockOnTarget.position.y - lockOnHeightOffset;
            
            // Y축만 부드럽게 추적 (점프/낙하 시 카메라 튀김 방지)
            lockOnTargetSmoothY = Mathf.SmoothDamp(
                lockOnTargetSmoothY, rawTargetY, ref lockOnTargetYVelocity, lockOnYSmoothTime);
            
            // XZ는 즉시 추적, Y만 스무딩 적용
            Vector3 targetLockOnPosition = new Vector3(
                lockOnTarget.position.x, lockOnTargetSmoothY, lockOnTarget.position.z);
            Vector3 directionToTarget = (targetLockOnPosition - target.position).normalized;

            // Yaw, Pitch 계산
            float targetYaw = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
            float targetPitch = Mathf.Asin(-directionToTarget.y) * Mathf.Rad2Deg;

            // 거리에 따른 Pitch 제한 (가까울수록 제한)
            float pitchLimitByDistance = Mathf.Lerp(maxVerticalAngle * 0.5f, maxVerticalAngle, 
                Mathf.Clamp01((distance - 3f) / 7f)); // 3m 이하에서는 제한, 10m 이상에서는 풀 각도
            targetPitch = Mathf.Clamp(targetPitch, minVerticalAngle, pitchLimitByDistance);

            // 부드럽게 회전
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * rotationSpeed);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * rotationSpeed);
            currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);
        }

        /// <summary>
        /// LockOn 상태 확인
        /// </summary>
        public bool IsLockOnActive()
        {
            return isLockOnActive;
        }

        /// <summary>
        /// 현재 LockOn 대상 가져오기
        /// </summary>
        public Transform GetLockOnTarget()
        {
            return lockOnTarget;
        }

        /// <summary>
        /// LockOn 대상 전환
        /// </summary>
        /// <param name="direction">1: 오른쪽, -1: 왼쪽</param>
        private void SwitchTarget(int direction)
        {
            if (availableTargets.Count == 0)
                return;

            // 카메라 기준 좌우로 대상 정렬
            SortTargetsByScreenPosition();

            currentTargetIndex = Mathf.Clamp(currentTargetIndex + direction, 0, availableTargets.Count - 1);

            if (lockOnTarget != null)
            {
                lockOnTarget.GetComponent<IDamageable>()?.UnLockOn();
            }
            // 대상 전환
            lockOnTarget = availableTargets[currentTargetIndex];
            lockOnTargetCollider = lockOnTarget.GetComponent<CapsuleCollider>();
            
            lastSwitchTime = Time.time; 
            
            lockOnTarget.GetComponent<IDamageable>()?.LockOn();
            
            // Y축 스무딩 즉시 초기화
            InitLockOnSmoothY();
        }

        /// <summary>
        /// 카메라 화면 좌우 기준으로 대상 정렬
        /// </summary>
        private void SortTargetsByScreenPosition()
        {
            if (mainCamera == null || target == null)
                return;

            // 유효하지 않은 대상 제거
            availableTargets.RemoveAll(t => t == null || !IsValidTarget(t));

            if (availableTargets.Count == 0)
            {
                ReleaseLockOn();
                return;
            }

            // 현재 대상 저장
            Transform currentTarget = lockOnTarget;

            // 카메라 기준 화면 X 좌표로 정렬 (왼쪽 → 오른쪽)
            availableTargets.Sort((a, b) =>
            {
                Vector3 screenPosA = mainCamera.WorldToScreenPoint(a.position);
                Vector3 screenPosB = mainCamera.WorldToScreenPoint(b.position);
                return screenPosA.x.CompareTo(screenPosB.x);
            });

            // 현재 대상의 새 인덱스 찾기
            currentTargetIndex = availableTargets.IndexOf(currentTarget);
            if (currentTargetIndex == -1 && availableTargets.Count > 0)
            {
                currentTargetIndex = 0;
                lockOnTarget = availableTargets[0];
                lockOnTargetCollider = lockOnTarget.GetComponent<CapsuleCollider>();
            }
        }

        /// <summary>
        /// 대상이 여전히 유효한지 확인
        /// </summary>
        private bool IsValidTarget(Transform targetTransform)
        {
            if (targetTransform == null)
                return false;

            // 거리 체크
            float distance = Vector3.Distance(target.position, targetTransform.position);
            if (distance > lockOnRange)
                return false;

            // IDamageable 확인
            var damageable = targetTransform.GetComponent<IDamageable>();
            if (damageable == null)
                damageable = targetTransform.GetComponentInParent<IDamageable>();

            return damageable != null && damageable.CanTakeDamage();
        }

        #endregion
    }
}
