using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Data.Config;
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
        [Header("Target")]
        [SerializeField] private Transform target; // 추적할 타겟 (플레이어)

        [Header("Camera Settings")] 
        [SerializeField] private Vector3 cameraOffset = new Vector3(0f, 1f, 0f); // 타겟 기준 카메라 피벗 오프셋

        [SerializeField] private float defaultDistance = 5f; // 기본 거리
        [SerializeField] private float minDistance = 2f; // 최소 거리
        [SerializeField] private float maxDistance = 10f; // 최대 거리

        [Header("Rotation Settings")] 
        [SerializeField] private float rotationSpeed = 20f; // 카메라 회전 속도

        [SerializeField] private float minVerticalAngle = -30f; // 최소 수직 각도
        [SerializeField] private float maxVerticalAngle = 70f; // 최대 수직 각도

        [Header("Zoom Settings")] 
        [SerializeField] private float zoomSpeed = 0.5f; // 줌 속도
        [SerializeField] private float zoomSmoothTime = 0.1f; // 줌 부드러움

        [Header("Smooth Settings")] 
        [SerializeField] private float positionSmoothTime = 0.1f; // 위치 부드러움
        [SerializeField] private float rotationSmoothTime = 0.1f; // 회전 부드러움

        [Header("Collision Settings")] 
        [SerializeField] private bool enableCollision = true; // 충돌 감지 활성화
        [SerializeField] private LayerMask collisionLayers = -1; // 충돌 레이어
        [SerializeField] private float collisionOffset = 0.2f; // 충돌 오프셋

        [Header("LockOn Settings")] 
        [SerializeField] private bool enableLockOn = true; // LockOn 활성화
        [SerializeField] private LayerMask lockOnLayerMask; // LockOn 대상 레이어
        [SerializeField] private float lockOnRange = 15f; // LockOn 최대 거리
        [SerializeField] private float lockOnAngle = 60f; // LockOn 시야각 (전방 기준)
        [SerializeField] private float lockOnSwitchDistance = 2f; // 대상 전환 최소 거리

        [SerializeField] private bool enableCameraAlign = true; // 캐릭터 방향 보정 활성화
        [SerializeField] private float cameraAlignSpeed = 5f; // 보정 속도
        [SerializeField] private float cameraAlignDuration = 0.5f; // 보정 지속 시간

        // 내부 변수
        private Camera mainCamera;
        private Transform cameraPivot; // 카메라가 회전할 피벗 포인트

        private float currentDistance;
        private float targetDistance;
        private float distanceVelocity;

        private float currentYaw; // 좌우 회전 (Y축)
        private float currentPitch; // 상하 회전 (X축)
        
        // LockOn 관련
        private Transform lockOnTarget; // 현재 LockOn 대상
        private bool isLockOnActive; // LockOn 활성화 상태

        private bool isCameraAligning; // 카메라 보정 중인지
        private float cameraAlignTimer; // 보정 타이머
        
        private Vector3 positionVelocity;
        private Vector3 smoothPosition;

        #region IManager 구현

        public void Init()
        {
            Debug.Log("[CameraManager] 초기화 시작");

            InitializeCamera();

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
            currentPitch = 20f; // 기본 각도

            collisionLayers &= ~(1 << LayerMask.NameToLayer("Player"));

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

            Debug.Log("[CameraManager] 초기화 완료");
        }

        public void AfterInit()
        {
            InputManager.Instance.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOn,
                null, OnInputPerformedLockOn, null, null, null, InputLayer.Level_1);
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

            if (cameraPivot != null)
            {
                Destroy(cameraPivot.gameObject);
            }

            if (InputManager.Instance != null)
            {
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOn,
                    null, OnInputPerformedLockOn, null);
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
            if (target == null || mainCamera == null || cameraPivot == null)
                return;

            UpdateCameraPosition();
            UpdateCameraRotation();
            UpdateLockOnRotation();
            UpdateCameraAlign(); // 추가
        }

        #endregion

        #region 입력 처리

        /// <summary>
        /// 카메라 입력 처리
        /// </summary>
        private void HandleInput()
        {
            if (Cursor.visible)
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
            currentDistance = Mathf.SmoothDamp(currentDistance, targetDistance, ref distanceVelocity, zoomSmoothTime);

            // 회전을 적용한 카메라 위치 계산
            Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
            Vector3 desiredPosition = cameraPivot.position + rotation * new Vector3(0f, 0f, -currentDistance);

            // 충돌 감지
            if (enableCollision)
            {
                desiredPosition = HandleCollision(cameraPivot.position, desiredPosition);
            }

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
                    1f - Mathf.Exp(-10f * Time.deltaTime / rotationSmoothTime)
                );
            }
            else
            {
                mainCamera.transform.rotation = targetRotation;
            }
        }

        /// <summary>
        /// 충돌 처리
        /// </summary>
        private Vector3 HandleCollision(Vector3 origin, Vector3 desiredPosition)
        {
            Vector3 direction = desiredPosition - origin;
            float distance = direction.magnitude;

            // 충돌 체크
            if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, collisionLayers))
            {
                // 충돌 지점으로 카메라 당기기
                return hit.point + hit.normal * collisionOffset;
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

                Debug.Log($"[CameraManager] 타겟 설정: {target.name}");
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
            if (enableLockOn)
            {
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
        }

        #endregion

        private void InitializeCamera()
        {
            string playerTag = "Player";
            Transform playerTarget = null;

            // 타겟 찾기
            GameObject player = GameObject.FindGameObjectWithTag(playerTag);
            if (player != null)
            {
                playerTarget = player.transform;
                Debug.Log($"[CameraInitializer] '{playerTag}' 태그로 플레이어를 찾았습니다: {player.name}");
            }
            else
            {
                Debug.LogWarning($"[CameraInitializer] '{playerTag}' 태그를 가진 오브젝트를 찾을 수 없습니다!");
            }

            // 타겟 설정 (마지막에 설정하여 위치가 즉시 업데이트되도록)
            if (playerTarget != null)
            {
                CameraManager.Instance.SetTarget(playerTarget);
                Debug.Log("[CameraInitializer] 카메라 타겟 설정 완료");
            }
        }
        
        private void OnInputPerformedLockOn(InputAction.CallbackContext obj)
        {
            if (enableLockOn == false || target == null)
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
        
        #region LockOn 시스템

        /// <summary>
        /// LockOn 시도 - IDamageable을 가진 대상 검색
        /// </summary>
        private bool TryLockOn()
        {
            Vector3 origin = target.position;
            Vector3 forward = mainCamera.transform.forward;

            // 범위 내 모든 Collider 검출
            Collider[] hits = Physics.OverlapSphere(origin, lockOnRange, lockOnLayerMask);

            Transform bestTarget = null;
            float closestAngle = lockOnAngle;

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
                Vector3 directionToTarget = (hit.transform.position - origin).normalized;
                float angle = Vector3.Angle(forward, directionToTarget);

                if (angle < closestAngle)
                {
                    closestAngle = angle;
                    bestTarget = hit.transform;
                }
            }

            if (bestTarget != null)
            {
                lockOnTarget = bestTarget;
                isLockOnActive = true;
                Debug.Log($"[CameraManager] LockOn 활성화: {bestTarget.name}");
                return true;
            }

            return false;
        }
        /// <summary>
        /// 캐릭터 뒷통수 방향으로 카메라 보정 시작
        /// </summary>
        private void StartCameraAlign()
        {
            isCameraAligning = true;
            cameraAlignTimer = cameraAlignDuration;
            Debug.Log("[CameraManager] 캐릭터 방향으로 카메라 보정 시작");
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
    
            // 목표 Yaw 계산 (캐릭터가 바라보는 방향)
            float targetYaw = Mathf.Atan2(targetForward.x, targetForward.z) * Mathf.Rad2Deg;

            // 부드럽게 회전
            currentYaw = Mathf.LerpAngle(currentYaw, targetYaw, Time.deltaTime * cameraAlignSpeed);
        }
        
        /// <summary>
        /// LockOn 해제
        /// </summary>
        private void ReleaseLockOn()
        {
            lockOnTarget = null;
            isLockOnActive = false;
            Debug.Log("[CameraManager] LockOn 해제");
        }

        /// <summary>
        /// LockOn 대상 추적 회전
        /// </summary>
        private void UpdateLockOnRotation()
        {
            if (isLockOnActive == false || lockOnTarget == null)
                return;

            // 대상이 너무 멀어지면 LockOn 해제
            float distance = Vector3.Distance(target.position, lockOnTarget.position);
            if (distance > lockOnRange)
            {
                ReleaseLockOn();
                return;
            }

            // 대상을 향한 방향 계산
            Vector3 directionToTarget = (lockOnTarget.position - target.position).normalized;
            
            // Yaw, Pitch 계산
            float targetYaw = Mathf.Atan2(directionToTarget.x, directionToTarget.z) * Mathf.Rad2Deg;
            float targetPitch = Mathf.Asin(-directionToTarget.y) * Mathf.Rad2Deg;

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
        #endregion
    }
}