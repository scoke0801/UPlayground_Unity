using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround;
using UPlayGround.Data.Config;
using UPlayGround.InputDefine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 적 타겟팅 및 락온 상태를 관리하는 매니저
    /// </summary>
    public class TargetingManager : BaseManager<TargetingManager>, IManager
    {
        private Transform _playerTransform;
        private Transform _currentTarget;
        private List<Transform> _availableTargets = new List<Transform>();
        private int _currentTargetIndex = -1;
        
        private LayerMask _lockOnLayerMask;
        private float _lockOnRange = 15f;
        private float _targetSwitchCooldown = 0.2f;
        private float _lastSwitchTime;

        public Transform CurrentTarget => _currentTarget;
        public bool IsLockOnActive => _currentTarget != null;

        #region IManager Implementation

        public void Init()
        {
            _lockOnLayerMask = CameraConfig.GetLockOnLayerMask();
            FindPlayer();
        }

        public void AfterInit()
        {
            var input = InputManager.Instance;
            input.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOn,
                null, OnInputLockOn, null, null, null, InputLayer.Level_1);
            input.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchLeft,
                null, OnInputSwitchLeft, null, null, null, InputLayer.Level_1);
            input.RegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchRight,
                null, OnInputSwitchRight, null, null, null, InputLayer.Level_1);
        }

        public void OnUpdate()
        {
            if (IsLockOnActive)
            {
                if (!IsValidTarget(_currentTarget))
                {
                    if (!TryFindNextTarget())
                    {
                        ClearLockOn();
                    }
                }
            }
        }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void Dispose()
        {
            if (InputManager.Instance != null)
            {
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOn, null, OnInputLockOn, null);
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchLeft, null, OnInputSwitchLeft, null);
                InputManager.Instance.UnRegisterInputEvent(InputMapNames.PlayerAction, PlayerAction.LockOnSwitchRight, null, OnInputSwitchRight, null);
            }
        }

        #endregion

        private void FindPlayer()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) _playerTransform = player.transform;
        }

        private void OnInputLockOn(InputAction.CallbackContext context)
        {
            if (IsLockOnActive)
            {
                ClearLockOn();
            }
            else
            {
                TryLockOn();
            }
        }

        private void OnInputSwitchLeft(InputAction.CallbackContext context) => SwitchTarget(-1);
        private void OnInputSwitchRight(InputAction.CallbackContext context) => SwitchTarget(1);

        public bool TryLockOn()
        {
            CollectTargets();
            if (_availableTargets.Count > 0)
            {
                SetTarget(_availableTargets[0]);
                return true;
            }
            return false;
        }

        private void CollectTargets()
        {
            if (_playerTransform == null) FindPlayer();
            if (_playerTransform == null) return;

            Collider[] hits = Physics.OverlapSphere(_playerTransform.position, _lockOnRange, _lockOnLayerMask);
            _availableTargets.Clear();

            List<(Transform trans, float distSq)> targets = new List<(Transform, float)>();
            Vector3 playerPos = _playerTransform.position;

            foreach (var hit in hits)
            {
                Transform t = hit.transform;
                if (t == _playerTransform || t.IsChildOf(_playerTransform)) continue;

                var damageable = t.GetComponent<IDamageable>() ?? t.GetComponentInParent<IDamageable>();
                if (damageable == null || !damageable.CanTakeDamage()) continue;

                float distSq = (t.position - playerPos).sqrMagnitude;
                targets.Add((t, distSq));
            }

            targets.Sort((a, b) => a.distSq.CompareTo(b.distSq));
            foreach (var item in targets) _availableTargets.Add(item.trans);
        }

        private void SetTarget(Transform target)
        {
            if (_currentTarget != null) _currentTarget.GetComponent<IDamageable>()?.UnLockOn();
            _currentTarget = target;
            if (_currentTarget != null)
            {
                _currentTarget.GetComponent<IDamageable>()?.LockOn();
                _currentTargetIndex = _availableTargets.IndexOf(_currentTarget);
            }
            else
            {
                _currentTargetIndex = -1;
            }
        }

        private void SwitchTarget(int direction)
        {
            if (!IsLockOnActive || Time.time - _lastSwitchTime < _targetSwitchCooldown) return;

            CollectTargets();
            if (_availableTargets.Count <= 1) return;

            // Sort by screen position for intuitive switching
            SortTargetsByScreenPosition();

            int nextIndex = _currentTargetIndex + direction;
            if (nextIndex < 0) nextIndex = _availableTargets.Count - 1;
            else if (nextIndex >= _availableTargets.Count) nextIndex = 0;

            SetTarget(_availableTargets[nextIndex]);
            _lastSwitchTime = Time.time;
        }

        private void SortTargetsByScreenPosition()
        {
            Camera mainCam = Camera.main;
            if (mainCam == null) return;

            _availableTargets.Sort((a, b) =>
            {
                float screenXA = mainCam.WorldToScreenPoint(a.position).x;
                float screenXB = mainCam.WorldToScreenPoint(b.position).x;
                return screenXA.CompareTo(screenXB);
            });
            _currentTargetIndex = _availableTargets.IndexOf(_currentTarget);
        }

        private bool IsValidTarget(Transform t)
        {
            if (t == null) return false;
            if (Vector3.Distance(_playerTransform.position, t.position) > _lockOnRange) return false;
            var damageable = t.GetComponent<IDamageable>() ?? t.GetComponentInParent<IDamageable>();
            return damageable != null && damageable.CanTakeDamage();
        }

        private bool TryFindNextTarget()
        {
            CollectTargets();
            if (_availableTargets.Count > 0)
            {
                SetTarget(_availableTargets[0]);
                return true;
            }
            return false;
        }

        public void ClearLockOn()
        {
            SetTarget(null);
        }
    }
}
