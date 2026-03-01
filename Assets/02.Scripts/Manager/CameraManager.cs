using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
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
    public class CameraManager : BaseManager<CameraManager>, IManager, ICameraStateAccessor
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

        // 킬캠
        private const string KILL_CAM_DATA_PATH = "KillCamData";
        private KillCamController _killCamController;
        
        // 입력 잠금 (킬캠 등 연출 중 카메라 조작 차단)
        private bool _isInputLocked;

        // 스무스 회전 전환 (연출용 절대 각도 전환)
        private bool _rotTransitionActive;
        private float _rotTransitionStartYaw;
        private float _rotTransitionStartPitch;
        private float _rotTransitionTargetYaw;
        private float _rotTransitionTargetPitch;
        private float _rotTransitionElapsed;
        private float _rotTransitionDuration;
        private bool _rotTransitionUnlockOnComplete;   // 전환 완료 시 입력 잠금 자동 해제

        // 컨텍스트 기반 카메라 오프셋 (전투/비전투 숄더 전환)
        private Vector3 _defaultOffset = new Vector3(0f, 1f, 0f);       // 비전투 (센터)
        private Vector3 _combatOffset = new Vector3(0.5f, 1.2f, 0f);   // 전투 (숄더 뷰)
        private Vector3 _offsetVelocity;
        private float _offsetSmoothTime = 0.35f;  // 오프셋 전환 부드러움
        private System.Func<bool> _combatStateProvider;  // 전투 상태 조회 함수

        // 카메라 이펙트 시스템
        private CameraEffectManager _effectManager;
        private float _baseFOV;

        // FOV 시스템 (상태별 FOV 전환)
        private float _fovExplore = 55f;        // 비전투 탐색
        private float _fovCombat = 65f;         // 전투 진입 (시야 확보)
        private float _fovLockOn = 50f;         // 락온 (타겟 집중)
        private float _currentTargetFOV;        // 목표 FOV
        private float _fovVelocity;             // SmoothDamp 속도
        private float _fovSmoothTime = 0.25f;   // FOV 전환 부드러움

        // 전투 상태 Pitch 보정
        private float _explorePitch = 20f;       // 비전투 기본 Pitch (약간 내려다봄)
        private float _combatPitch = 12f;        // 전투 기본 Pitch (수평에 가깝게)

        // 락온 시 카메라 거리/오프셋
        private float _lockOnDistance = 4.5f;    // 락온 시 카메라 거리
        private Vector3 _lockOnOffset = new Vector3(0.6f, 1.0f, 0f); // 락온 숄더 오프셋

        // 다수 적 자동 줌아웃
        private float _crowdZoomOutDistance = 7f;     // 다수 적 감지 시 줌아웃 거리
        private float _crowdDetectRadius = 10f;       // 적 감지 반경
        private int _crowdEnemyThreshold = 3;         // 줌아웃 트리거 적 수
        private float _crowdZoomSmoothTime = 0.4f;    // 줌아웃 전환 속도
        private float _crowdZoomVelocity;
        private float _crowdTargetDistance;            // 다수 적 보정 거리
        private bool _isCrowdZoomActive;

        // 락온 중점 피벗 (Mid-Point Camera)
        private float _lockOnMidPointWeight = 0.35f;  // 중점 가중치 (0=플레이어, 1=적, 0.35=적 쪽으로 35%)
        private float _lockOnPivotSmoothTime = 0.15f; // 피벗 전환 부드러움
        private Vector3 _lockOnPivotVelocity;
        
        // 락온 고저차 감쇠
        private float _lockOnHeightDampFactor = 0.4f;  // 고저차 감쇠 비율 (1=그대로, 0.4=40%만 반영)
        private float _lockOnPitchMin = -15f;          // 락온 전용 Pitch 하한 (일반 -30보다 좁게)
        private float _lockOnPitchMax = 25f;           // 락온 전용 Pitch 상한 (일반 70보다 좁게)
        private float _lockOnPitchSpeed = 8f;          // 락온 Pitch 전환 속도 (Yaw보다 느리게)

        #region IManager 구현

        public void Init()
        {
            Debug.Log("[CameraManager] 초기화 시작");

            InitializeCamera();
            LoadCameraShakeDatabase();
            
            if (lockOnLayerMask == 0)
            {
                lockOnLayerMask = CameraConfig.GetLockOnLayerMask();
            }
            LoadKillCamData();

            // 카메라 이펙트 시스템 초기화
            _effectManager = new CameraEffectManager(this);

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

            _effectManager?.DisposeAll();
            _killCamController?.ForceStop();

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

        public void OnSceneChanged(string sceneType)
        {
            // LockOn 상태 초기화
            ReleaseLockOn();

            // 진행 중 이펙트 즉시 종료
            _effectManager?.StopAll(immediate: true);
            _killCamController?.ForceStop();
            _isInputLocked = false;

            // Player 레퍼런스 재수집
            InitializeCamera();
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
            if (target == null || mainCamera == null || cameraPivot == null)
                return;

            UpdateLockOnTransition();
            UpdateLockOnRotation();
            UpdateCameraAlign();
            UpdateContextOffset();
            UpdateRotationTransition();
            
            UpdateFOVSystem();
            UpdateCrowdZoomOut();
            UpdateCombatDistance();

            // === 카메라 이펙트 시스템 ===
            CameraEffectState fx = _effectManager.UpdateAndComputeState(Time.deltaTime);

            // 이펙트 델타 적용
            currentYaw += fx.yawDelta;
            currentPitch += fx.pitchDelta;
            currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);

            targetDistance += fx.distanceDelta;
            targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);

            cameraOffset += fx.offsetDelta;

            float effectivePosSmoothTime = fx.positionSmoothTimeOverride ?? positionSmoothTime;
            float effectiveRotSmoothTime = fx.rotationSmoothTimeOverride ?? rotationSmoothTime;

            UpdateCameraPosition(effectivePosSmoothTime);
            UpdateCameraRotation(effectiveRotSmoothTime);

            // 포지션 델타 (Shake, SpringDamp 등)
            mainCamera.transform.position += fx.positionDelta;

            // FOV 델타
            if (Mathf.Abs(fx.fovDelta) > 0.001f)
            {
                mainCamera.fieldOfView = _baseFOV + fx.fovDelta;
            }
            else if (_effectManager.HasActiveEffects == false)
            {
                // 활성 이펙트가 없으면 기본 FOV 복원
                mainCamera.fieldOfView = _baseFOV;
            }
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
        private void UpdateCameraPosition(float smoothTime)
        {
            // 타겟 위치 + 오프셋으로 피벗 이동
            Vector3 targetPivotPosition = target.position + cameraOffset;
            smoothPosition = Vector3.SmoothDamp(smoothPosition, targetPivotPosition, ref positionVelocity,
                smoothTime);
            cameraPivot.position = smoothPosition;

            // 거리 부드럽게 조정
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);

            // 회전을 적용한 카메라 위치 계산
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 desiredPosition = cameraPivot.position + rotation * new Vector3(0f, 0f, -currentDistance);

            // 충돌 감지
            desiredPosition = HandleCollision(cameraPivot.position, desiredPosition);

            // 카메라 위치 적용
            mainCamera.transform.position = desiredPosition;
        }

        /// <summary>
        /// 카메라 회전 업데이트
        /// </summary>
        private void UpdateCameraRotation(float smoothTime)
        {
            // 카메라가 피벗을 바라보도록 회전
            Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

            if (smoothTime > 0f)
            {
                mainCamera.transform.rotation = Quaternion.Slerp(
                    mainCamera.transform.rotation,
                    targetRotation,
                    1f - Mathf.Exp(-10f / smoothTime)
                );
            }
            else
            {
                mainCamera.transform.rotation = targetRotation;
            }
        }

        #region

        /// <summary>
        /// 상태별 FOV 전환 (비전투/전투/락온)
        /// 상황에 따라 시야각이 동적으로 변한다.
        /// </summary>
        private void UpdateFOVSystem()
        {
            if (_isInputLocked) return;

            bool isCombat = _combatStateProvider?.Invoke() ?? false;

            // 우선순위: 락온 > 전투 > 탐색
            if (isLockOnActive)
                _currentTargetFOV = _fovLockOn;
            else if (isCombat)
                _currentTargetFOV = _fovCombat;
            else
                _currentTargetFOV = _fovExplore;

            // _baseFOV를 부드럽게 목표값으로 전환
            // (CameraEffectManager의 fovDelta가 _baseFOV 기준으로 적용되므로 _baseFOV를 변경)
            _baseFOV = Mathf.SmoothDamp(_baseFOV, _currentTargetFOV, ref _fovVelocity, _fovSmoothTime);
        }

        /// <summary>
        /// 다수 적 자동 줌아웃
        /// 주변에 적이 일정 수 이상이면 카메라를 자동으로 빼서 상황 파악을 돕는다.
        /// </summary>
        private void UpdateCrowdZoomOut()
        {
            if (_isInputLocked || target == null) return;

            // 락온 중이면 줌아웃 비활성화 (락온 거리가 우선)
            if (isLockOnActive)
            {
                _isCrowdZoomActive = false;
                return;
            }

            bool isCombat = _combatStateProvider?.Invoke() ?? false;
            if (!isCombat)
            {
                _isCrowdZoomActive = false;
                return;
            }

            // 주변 적 수 카운트
            int nearbyEnemyCount = CountNearbyEnemies();

            if (nearbyEnemyCount >= _crowdEnemyThreshold)
            {
                _isCrowdZoomActive = true;
                _crowdTargetDistance = Mathf.SmoothDamp(
                    _crowdTargetDistance, _crowdZoomOutDistance, ref _crowdZoomVelocity, _crowdZoomSmoothTime);
            }
            else
            {
                _isCrowdZoomActive = false;
                _crowdTargetDistance = Mathf.SmoothDamp(
                    _crowdTargetDistance, defaultDistance, ref _crowdZoomVelocity, _crowdZoomSmoothTime);
            }
        }

        /// <summary>
        /// 전투/락온 상태에 따른 카메라 거리 보정
        /// 락온 > 다수 적 줌아웃 > 전투 > 탐색 순으로 우선순위 적용
        /// </summary>
        private void UpdateCombatDistance()
        {
            if (_isInputLocked) return;

            float desiredDistance;

            if (isLockOnActive)
            {
                // 락온 시 가까이 당김
                desiredDistance = _lockOnDistance;
            }
            else if (_isCrowdZoomActive)
            {
                // 다수 적 줌아웃
                desiredDistance = _crowdTargetDistance;
            }
            else
            {
                // 기본 거리 (수동 줌은 유저 입력으로 targetDistance가 바뀜)
                return; // 유저 줌을 존중
            }

            targetDistance = Mathf.Clamp(desiredDistance, minDistance, maxDistance);
        }

        /// <summary>
        /// 주변 적 수 카운트 (다수 적 줌아웃용)
        /// </summary>
        private int CountNearbyEnemies()
        {
            Collider[] hits = Physics.OverlapSphere(target.position, _crowdDetectRadius, lockOnLayerMask);
            int count = 0;

            foreach (var hit in hits)
            {
                if (hit.transform == target || hit.transform.IsChildOf(target))
                    continue;

                var damageable = hit.GetComponent<IDamageable>();
                if (damageable == null)
                    damageable = hit.GetComponentInParent<IDamageable>();

                if (damageable != null && damageable.CanTakeDamage())
                    count++;
            }

            return count;
        }

        #endregion

        /// <summary>
        /// 컨텍스트 기반 카메라 오프셋 보간
        /// 전투 시 숄더 뷰, 비전투 시 센터 뷰로 부드럽게 전환
        /// 락온 시에는 전용 오프셋을 사용
        /// </summary>
        private void UpdateContextOffset()
        {
            // 킬캠 등 연출이 오프셋을 직접 제어 중이면 보간 건너뜀
            if (_isInputLocked)
                return;

            bool isCombat = _combatStateProvider?.Invoke() ?? false;
            
            // 우선순위: 락온 > 전투 > 탐색
            Vector3 targetOffset;
            if (isLockOnActive)
                targetOffset = _lockOnOffset;
            else if (isCombat)
                targetOffset = _combatOffset;
            else
                targetOffset = _defaultOffset;
            
            cameraOffset = Vector3.SmoothDamp(cameraOffset, targetOffset, ref _offsetVelocity, _offsetSmoothTime);
        }

        /// <summary>
        /// 스무스 회전 전환 업데이트 (SetRotationSmooth로 시작된 전환을 매 프레임 보간)
        /// - _isInputLocked 상태와 무관하게 항상 실행 (내부 연출 전환이므로)
        /// - SmoothStep 커브로 ease-in/out 적용
        /// - 전환 완료 시 _rotTransitionUnlockOnComplete == true이면 입력 잠금 자동 해제
        /// </summary>
        private void UpdateRotationTransition()
        {
            if (!_rotTransitionActive)
                return;

            _rotTransitionElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_rotTransitionElapsed / _rotTransitionDuration);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);  // ease-in/out

            currentYaw   = Mathf.LerpAngle(_rotTransitionStartYaw,   _rotTransitionTargetYaw,   smoothT);
            currentPitch = Mathf.Lerp     (_rotTransitionStartPitch, _rotTransitionTargetPitch, smoothT);
            currentPitch = Mathf.Clamp(currentPitch, minVerticalAngle, maxVerticalAngle);

            if (t >= 1f)
            {
                _rotTransitionActive = false;

                // 복원 전환 완료 시 입력 잠금 자동 해제
                if (_rotTransitionUnlockOnComplete)
                {
                    _isInputLocked = false;
                    _rotTransitionUnlockOnComplete = false;
                }
            }
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
        /// 카메라 회전 즉시 설정 (스냅)
        /// 진행 중인 스무스 전환이 있으면 취소된다.
        /// </summary>
        public void SetRotation(float yaw, float pitch)
        {
            _rotTransitionActive = false;   // 진행 중 전환 취소
            currentYaw   = yaw;
            currentPitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
        }

        /// <summary>
        /// 카메라 회전을 duration 동안 부드럽게 전환한다 (SmoothStep 커브).
        /// duration == 0 이면 즉시 SetRotation과 동일하게 동작한다.
        /// unlockOnComplete == true 이면 전환 완료 시 입력 잠금(_isInputLocked)을 자동 해제한다.
        /// </summary>
        public void SetRotationSmooth(float yaw, float pitch, float duration, bool unlockOnComplete = false)
        {
            if (duration <= 0f)
            {
                SetRotation(yaw, pitch);
                if (unlockOnComplete)
                    _isInputLocked = false;
                return;
            }

            _rotTransitionStartYaw   = currentYaw;
            _rotTransitionStartPitch = currentPitch;
            _rotTransitionTargetYaw   = yaw;
            _rotTransitionTargetPitch = Mathf.Clamp(pitch, minVerticalAngle, maxVerticalAngle);
            _rotTransitionElapsed     = 0f;
            _rotTransitionDuration    = duration;
            _rotTransitionActive      = true;
            _rotTransitionUnlockOnComplete = unlockOnComplete;
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

        /// <summary>
        /// 락온 시 카메라 오프셋 설정 (외부에서 튜닝 가능)
        /// </summary>
        public void SetLockOnOffset(Vector3 offset)
        {
            _lockOnOffset = offset;
        }

        /// <summary>
        /// 상태별 FOV 설정 (외부에서 튜닝 가능)
        /// </summary>
        public void SetFOVSettings(float explore, float combat, float lockOn)
        {
            _fovExplore = explore;
            _fovCombat = combat;
            _fovLockOn = lockOn;
        }

        /// <summary>
        /// 락온 시 카메라 거리 설정
        /// </summary>
        public void SetLockOnDistance(float distance)
        {
            _lockOnDistance = distance;
        }

        /// <summary>
        /// 다수 적 줌아웃 설정
        /// </summary>
        public void SetCrowdZoomSettings(float zoomOutDistance, float detectRadius, int enemyThreshold)
        {
            _crowdZoomOutDistance = zoomOutDistance;
            _crowdDetectRadius = detectRadius;
            _crowdEnemyThreshold = enemyThreshold;
        }

        /// <summary>
        /// 락온 고저차 감쇠 설정
        /// </summary>
        /// <param name="dampFactor">고저차 반영 비율 (0~1, 낮을수록 고저차 무시)</param>
        /// <param name="pitchMin">락온 Pitch 하한 (기본 -15)</param>
        /// <param name="pitchMax">락온 Pitch 상한 (기본 25)</param>
        /// <param name="pitchSpeed">Pitch 전환 속도 (기본 8)</param>
        public void SetLockOnHeightDampSettings(float dampFactor, float pitchMin, float pitchMax, float pitchSpeed)
        {
            _lockOnHeightDampFactor = Mathf.Clamp01(dampFactor);
            _lockOnPitchMin = pitchMin;
            _lockOnPitchMax = pitchMax;
            _lockOnPitchSpeed = pitchSpeed;
        }

        /// <summary>
        /// 현재 카메라 FOV 반환
        /// </summary>
        public float GetCurrentFOV()
        {
            return mainCamera != null ? mainCamera.fieldOfView : _baseFOV;
        }

        /// <summary>
        /// 현재 FOV 상태 (디버그용)
        /// </summary>
        public float GetBaseFOV() => _baseFOV;
        public float GetTargetFOV() => _currentTargetFOV;

        #endregion

        #region Camera Effect API

        /// <summary>
        /// ScriptableObject 데이터로 카메라 이펙트를 재생한다.
        /// 반환된 핸들로 수동 Stop이 가능하다.
        /// </summary>
        public ICameraEffect PlayEffect(Data.CameraEffectData data)
        {
            return _effectManager.PlayEffect(data);
        }

        /// <summary>
        /// 특정 카메라 이펙트를 정지한다 (BlendOut 시작).
        /// </summary>
        public void StopEffect(ICameraEffect effect, bool immediate = false)
        {
            _effectManager.StopEffect(effect, immediate);
        }

        /// <summary>
        /// effectId가 일치하는 모든 이펙트를 정지한다.
        /// </summary>
        public void StopEffect(string effectId, bool immediate = false)
        {
            _effectManager.StopEffectById(effectId, immediate);
        }

        /// <summary>
        /// 모든 활성 카메라 이펙트를 정지한다.
        /// </summary>
        public void StopAllEffects(bool immediate = false)
        {
            _effectManager.StopAll(immediate);
        }

        /// <summary>
        /// 활성 카메라 이펙트 존재 여부
        /// </summary>
        public bool HasActiveEffects => _effectManager?.HasActiveEffects ?? false;

        #endregion

        #region ICameraStateAccessor 구현

        float ICameraStateAccessor.CurrentYaw => currentYaw;
        float ICameraStateAccessor.CurrentPitch => currentPitch;
        float ICameraStateAccessor.CurrentDistance => currentDistance;
        float ICameraStateAccessor.TargetDistance => targetDistance;
        Vector3 ICameraStateAccessor.CurrentOffset => cameraOffset;
        float ICameraStateAccessor.CurrentFOV => mainCamera != null ? mainCamera.fieldOfView : 60f;
        Camera ICameraStateAccessor.MainCamera => mainCamera;
        Transform ICameraStateAccessor.Target => target;

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
            
            // 메인 카메라 찾기
            mainCamera = Camera.main;
            if (mainCamera == null)
            {
                Debug.LogError("[CameraManager] 메인 카메라를 찾을 수 없습니다!");
                return;
            }

            // 카메라 피벗 생성 (타겟을 중심으로 회전)
            GameObject pivotObj = new GameObject("CameraPivot");
            cameraPivot = pivotObj.transform;
            cameraPivot.SetParent(transform);

            // 초기값 설정
            currentDistance = defaultDistance;
            targetDistance = defaultDistance;
            currentYaw = 0f;
            currentPitch = _explorePitch;

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
            
            GameObject shakerGO = new GameObject("CameraShaker");
            shakerGO.hideFlags = HideFlags.HideAndDontSave;
            
            _shaker = shakerGO.AddComponent<CameraShaker>();
            _shaker.hideFlags = HideFlags.HideAndDontSave;

            _baseFOV = mainCamera.fieldOfView;
            
            // FOV 초기화
            _currentTargetFOV = _fovExplore;
            mainCamera.fieldOfView = _fovExplore;
            _baseFOV = _fovExplore;
            _crowdTargetDistance = defaultDistance;
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
            // 입력 잠금 중(연출 등)에는 자동 회전 건너뜀
            if (_isInputLocked)
                return;

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
            
            // 전투 상태에 따라 Pitch 변경 (전투 시 더 수평에 가깝게)
            bool isCombat = _combatStateProvider?.Invoke() ?? false;
            float targetPitch = isCombat ? _combatPitch : _explorePitch;
            
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
            
            // 락온 해제 시 거리를 기본으로 복원
            targetDistance = defaultDistance;
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
            // 입력 잠금 중(연출 등)에는 자동 회전 건너뜀
            if (_isInputLocked)
                return;

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
        /// LockOn 대상 추적 회전 (Mid-Point Camera)
        /// 단순히 적을 바라보는 대신, 플레이어-타겟 중간 지점을 기준으로
        /// 카메라를 배치하여 양쪽 모두 화면에 잘 잡히도록 한다.
        /// 고저차가 클 때도 Pitch가 극단적으로 변하지 않도록 감쇠 처리한다.
        /// </summary>
        private void UpdateLockOnRotation()
        {
            // 입력 잠금 중(연출 등)에는 자동 회전 건너뜀
            if (_isInputLocked)
                return;

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

            // === Mid-Point Camera ===
            Vector3 midPoint = Vector3.Lerp(target.position, targetLockOnPosition, _lockOnMidPointWeight);
            
            // === 고저차 감쇠 ===
            // Yaw 계산은 XZ 평면에서만 (고저차 무관)
            Vector3 dirToMidXZ = new Vector3(
                midPoint.x - target.position.x, 0f, midPoint.z - target.position.z);
            
            float targetYaw;
            if (dirToMidXZ.sqrMagnitude > 0.001f)
            {
                targetYaw = Mathf.Atan2(dirToMidXZ.x, dirToMidXZ.z) * Mathf.Rad2Deg;
            }
            else
            {
                targetYaw = currentYaw; // 너무 가까우면 현재 Yaw 유지
            }

            // Pitch 계산: 고저차를 감쇠하여 극단적인 카메라 각도 변화를 방지
            // heightDiff > 0이면 적이 위에 있음, < 0이면 아래에 있음
            float heightDiff = midPoint.y - target.position.y;
            float horizontalDist = dirToMidXZ.magnitude;
            
            // 수평 거리 대비 높이차의 비율로 각도 계산 (순수 방향 벡터 대신)
            float rawPitchAngle = 0f;
            if (horizontalDist > 0.5f)
            {
                // 높이차를 감쇠: 실제 높이차의 일부만 반영
                float dampedHeight = heightDiff * _lockOnHeightDampFactor;
                rawPitchAngle = Mathf.Atan2(-dampedHeight, horizontalDist) * Mathf.Rad2Deg;
            }
            
            // Pitch 범위를 락온 전용으로 제한 (일반 Pitch 제한보다 좁게)
            float targetPitch = Mathf.Clamp(rawPitchAngle, _lockOnPitchMin, _lockOnPitchMax);

            // 거리에 따른 Pitch 추가 제한 (가까울수록 더 제한)
            float pitchLimitByDistance = Mathf.Lerp(_lockOnPitchMax * 0.5f, _lockOnPitchMax, 
                Mathf.Clamp01((distance - 3f) / 7f));
            targetPitch = Mathf.Clamp(targetPitch, _lockOnPitchMin, pitchLimitByDistance);

            // 부드럽게 회전
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * rotationSpeed);
            currentPitch = Mathf.Lerp(currentPitch, targetPitch, Time.deltaTime * _lockOnPitchSpeed);
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