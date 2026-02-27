using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Unity.Cinemachine;
using UPlayGround.Data;
using UPlayGround.Data.Config;
using UPlayGround.Data.Path;
using UPlayGround.InputDefine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// Cinemachine 3.x 기반 액션 카메라 매니저
    /// </summary>
    public class CameraManager : BaseManager<CameraManager>, IManager, ICameraStateAccessor
    {
        [Header("Cinemachine References")]
        private CinemachineCamera _freeLookCamera;
        private CinemachineCamera _lockOnCamera;
        private CinemachineTargetGroup _targetGroup;

        [Header("Settings")]
        private float _baseFOV = 60f;
        private Vector3 _cameraOffset = new Vector3(0f, 1f, 0f);
        
        // 내부 필드 (ICameraStateAccessor 구현 및 이펙트 적용용)
        private Camera _mainCamera;
        private Transform _playerTarget;
        private CameraEffectManager _effectManager;
        private CameraShaker _shaker;
        private CameraShakeDatabase _cameraShakeDatabase;
        private KillCamController _killCamController;

        private bool _isInputLocked;
        private System.Func<bool> _combatStateProvider;

        private const string CAMERA_SHAKE_DATABASE_PATH = "CameraShakeDatabase";
        private const string KILL_CAM_DATA_PATH = "KillCamData";

        #region IManager Implementation

        public void Init()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null)
            {
                Debug.LogError("[CameraManager] Main Camera not found!");
                return;
            }

            _baseFOV = _mainCamera.fieldOfView;

            // Cinemachine 컴포넌트 찾기 (씬에 미리 배치되어 있어야 함)
            FindCinemachineComponents();
            
            InitializePlayerTarget();
            LoadAssets();

            _effectManager = new CameraEffectManager(this);
            
            // Shaker setup
            GameObject shakerGO = new GameObject("CameraShaker");
            shakerGO.transform.SetParent(transform);
            _shaker = shakerGO.AddComponent<CameraShaker>();

            Debug.Log("[CameraManager] Initialized with Cinemachine 3.x");
        }

        public void AfterInit()
        {
            // TargetingManager가 이미 입력을 처리하므로 여기선 상태 반영만 함
        }

        public void OnUpdate()
        {
            if (_playerTarget == null) return;
            
            UpdateCameraStates();
        }

        public void OnFixedUpdate() { }

        public void OnLateUpdate()
        {
            if (_mainCamera == null) return;

            // 카메라 이펙트 시스템 업데이트
            CameraEffectState fx = _effectManager.UpdateAndComputeState(Time.deltaTime);

            // 이펙트 적용 (Cinemachine 카메라 위에 오버레이로 적용하거나 CM Offset 수정)
            // 여기서는 단순함을 위해 MainCamera에 직접 델타를 더함 (CM이 매 프레임 위치를 덮어쓰므로 CM 이후에 실행되어야 함)
            _mainCamera.transform.position += fx.positionDelta;
            _mainCamera.transform.Rotate(new Vector3(fx.pitchDelta, fx.yawDelta, 0f));
            
            if (Mathf.Abs(fx.fovDelta) > 0.001f)
            {
                _mainCamera.fieldOfView = _baseFOV + fx.fovDelta;
            }
        }

        public void Dispose()
        {
            _effectManager?.DisposeAll();
            _killCamController?.ForceStop();
        }

        #endregion

        private void FindCinemachineComponents()
        {
            // 씬에서 Cinemachine 카메라들을 찾습니다. 
            // UnityEngine.Object를 명시하여 네임스페이스 충돌 방지
            var cams = UnityEngine.Object.FindObjectsByType<CinemachineCamera>(FindObjectsSortMode.None);
            foreach (var cam in cams)
            {
                if (cam.gameObject.name.Contains("Free")) _freeLookCamera = cam;
                else if (cam.gameObject.name.Contains("Lock")) _lockOnCamera = cam;
            }

            _targetGroup = UnityEngine.Object.FindAnyObjectByType<CinemachineTargetGroup>();
            
            if (_freeLookCamera == null || _lockOnCamera == null)
            {
                Debug.LogWarning("[CameraManager] Cinemachine Cameras (Free/Lock) not found in scene.");
            }
        }

        private void InitializePlayerTarget()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                _playerTarget = player.transform;
                SetCameraTargets(_playerTarget);
            }
        }

        private void SetCameraTargets(Transform target)
        {
            if (_freeLookCamera != null)
            {
                _freeLookCamera.Follow = target;
                _freeLookCamera.LookAt = target;
            }
            
            if (_targetGroup != null)
            {
                // TargetGroup의 0번 멤버는 항상 플레이어
                if (_targetGroup.Targets.Count == 0)
                {
                    _targetGroup.Targets.Add(new CinemachineTargetGroup.Target { Object = target, Weight = 1f, Radius = 1f });
                }
                else
                {
                    var t = _targetGroup.Targets[0];
                    t.Object = target;
                    _targetGroup.Targets[0] = t;
                }
            }
        }

        private void UpdateCameraStates()
        {
            bool isLockedOn = TargetingManager.Instance.IsLockOnActive;
            Transform currentTarget = TargetingManager.Instance.CurrentTarget;

            if (isLockedOn && currentTarget != null)
            {
                // 락온 상태: LockOnCamera 우선순위 높임
                if (_lockOnCamera != null) _lockOnCamera.Priority = 20;
                if (_freeLookCamera != null) _freeLookCamera.Priority = 10;

                // TargetGroup 업데이트 (플레이어와 적)
                UpdateTargetGroup(currentTarget);
            }
            else
            {
                // 자유 시점: FreeLookCamera 우선순위 높임
                if (_freeLookCamera != null) _freeLookCamera.Priority = 20;
                if (_lockOnCamera != null) _lockOnCamera.Priority = 10;
            }
        }

        private void UpdateTargetGroup(Transform enemyTarget)
        {
            if (_targetGroup == null) return;

            // 0: Player, 1: Enemy
            if (_targetGroup.Targets.Count < 2)
            {
                _targetGroup.Targets.Add(new CinemachineTargetGroup.Target { Object = enemyTarget, Weight = 1f, Radius = 1f });
            }
            else
            {
                var t = _targetGroup.Targets[1];
                t.Object = enemyTarget;
                _targetGroup.Targets[1] = t;
            }
        }

        private async void LoadAssets()
        {
            // Shake Database
            try {
                var handle = Addressables.LoadAssetAsync<CameraShakeDatabase>(CAMERA_SHAKE_DATABASE_PATH);
                _cameraShakeDatabase = await handle.Task;
                _cameraShakeDatabase?.Initialize();
            } catch { }

            // Kill Cam
            try {
                var handle = Addressables.LoadAssetAsync<KillCamData>(KILL_CAM_DATA_PATH);
                var data = await handle.Task;
                if (data != null) _killCamController = new KillCamController(this, data);
            } catch { }
        }

        #region Public API (Compatibility)

        public void SetTarget(Transform newTarget)
        {
            _playerTarget = newTarget;
            SetCameraTargets(newTarget);
        }

        public Transform GetTarget() => _playerTarget;

        public Transform GetLockOnTarget() => TargetingManager.Instance.CurrentTarget;
        public bool IsLockOnActive() => TargetingManager.Instance.IsLockOnActive;

        public void SetInputLock(bool locked)
        {
            _isInputLocked = locked;
            // Cinemachine InputProvider 등을 비활성화하는 로직 추가 가능
        }

        public void StartShake(string key)
        {
            if (_cameraShakeDatabase != null)
            {
                var data = _cameraShakeDatabase.GetShakeData(key);
                StartShake(data);
            }
        }

        public void StartShake(CameraShakeData data)
        {
            if (data != null)
            {
                _shaker.SetShakeData(data);
                _shaker.StartShake();
            }
        }

        public void StopShake()
        {
            _shaker.StopShake();
        }

        public void Punch(Vector3 dir, float strength, float duration = 0.15f) => _shaker.Punch(dir, strength, duration);

        public bool TryKillCam(Transform victim) => _killCamController != null && _killCamController.TryExecute(victim);

        public void SetCombatStateProvider(System.Func<bool> provider) => _combatStateProvider = provider;

        public float GetCurrentDistance() => CurrentDistance;
        public Vector3 GetCurrentOffset() => CurrentOffset;

        public void SetDistance(float dist)
        {
            if (_freeLookCamera != null)
            {
                var component = _freeLookCamera.GetComponent<CinemachinePositionComposer>();
                if (component != null) component.CameraDistance = dist;
            }
        }

        public void SetCameraOffset(Vector3 offset)
        {
            _cameraOffset = offset;
            if (_freeLookCamera != null)
            {
                var component = _freeLookCamera.GetComponent<CinemachinePositionComposer>();
                if (component != null) component.TargetOffset = offset;
            }
            if (_lockOnCamera != null)
            {
                var component = _lockOnCamera.GetComponent<CinemachinePositionComposer>();
                if (component != null) component.TargetOffset = offset;
            }
        }

        public ICameraEffect PlayEffect(Data.CameraEffectData data) => _effectManager.PlayEffect(data);
        public void StopEffect(ICameraEffect effect, bool immediate = false) => _effectManager.StopEffect(effect, immediate);

        // Legacy methods for compatibility - might need actual CM implementation if used for logic
        public void SetRotation(float yaw, float pitch) { }
        public void SetRotationSmooth(float yaw, float pitch, float duration, bool unlockOnComplete = false) { }
        
        #endregion

        #region ICameraStateAccessor

        public float CurrentYaw => _mainCamera.transform.eulerAngles.y;
        public float CurrentPitch => _mainCamera.transform.eulerAngles.x;
        public float CurrentDistance => Vector3.Distance(_mainCamera.transform.position, _playerTarget != null ? _playerTarget.position : Vector3.zero);
        public float TargetDistance => CurrentDistance; // CM이 제어하므로 현재와 동일하게 취급
        public Vector3 CurrentOffset => _cameraOffset;
        public float CurrentFOV => _mainCamera.fieldOfView;
        public Camera MainCamera => _mainCamera;
        public Transform Target => _playerTarget;

        #endregion
    }
}
