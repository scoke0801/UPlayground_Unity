using System.Collections.Generic;
using UnityEngine;
using UPlayGround.MovementController;
using UPlayGround.State;

namespace UPlayGround.Group
{
    public class MonsterGroupMemory : MonoBehaviour
    {
        private const float DodgeWindow = 5f;
        private const float GuardWindow = 6f;
        private const float AttackWindow = 5f;
        private const float RecoverWindow = 7f;

        private readonly Dictionary<string, SkillHitStats> _skillStats = new();

        private Transform _playerTransform;
        private ActorMovementController _playerController;
        private Vector3 _lastPlayerPosition;
        private float _playerIdleTimer;
        private string _lastObservedPlayerStateName = "";
        private bool _wasPlayerRecovering;

        private int _playerDodgeCount;
        private float _dodgeWindowTimer;
        private int _playerGuardCount;
        private float _guardWindowTimer;
        private int _playerAttackCount;
        private float _attackWindowTimer;
        private int _playerRecoverCount;
        private float _recoverWindowTimer;

        private int _totalHitsLanded;
        private int _totalHitsMissed;

        public int PlayerDodgeCountInWindow => _playerDodgeCount;
        public int PlayerGuardCountInWindow => _playerGuardCount;
        public int PlayerAttackCountInWindow => _playerAttackCount;
        public int PlayerRecoverCountInWindow => _playerRecoverCount;
        public float LastHitOnGroupTime { get; private set; } = -999f;

        public float HitAccuracyAgainstPlayer
        {
            get
            {
                var total = _totalHitsLanded + _totalHitsMissed;
                return total > 0 ? (float)_totalHitsLanded / total : 0.5f;
            }
        }

        public bool IsPlayerStaggered => GetPlayerStateName() == "Hit";
        public bool IsPlayerAttacking => IsPlayerInCombatState();
        public bool IsPlayerGuarding => IsPlayerGuardState(GetPlayerStateName());
        public bool IsPlayerRecovering => GetPlayerStateName() == "Idle" && _playerIdleTimer >= 1.2f;

        private void Update()
        {
            var dt = Time.deltaTime;
            UpdateWindow(ref _dodgeWindowTimer, ref _playerDodgeCount, DodgeWindow, dt);
            UpdateWindow(ref _guardWindowTimer, ref _playerGuardCount, GuardWindow, dt);
            UpdateWindow(ref _attackWindowTimer, ref _playerAttackCount, AttackWindow, dt);
            UpdateWindow(ref _recoverWindowTimer, ref _playerRecoverCount, RecoverWindow, dt);
            UpdatePlayerObservation(dt);
        }

        public void SetPlayerTarget(Transform player)
        {
            if (player == null)
            {
                _playerTransform = null;
                _playerController = null;
                _lastObservedPlayerStateName = "";
                _wasPlayerRecovering = false;
                return;
            }

            if (_playerTransform == player)
                return;

            _playerTransform = player;
            _playerController = player.GetComponent<ActorMovementController>();
            _lastPlayerPosition = player.position;
            _playerIdleTimer = 0f;
            _lastObservedPlayerStateName = GetPlayerStateName();
            _wasPlayerRecovering = false;
        }

        public void NotifyMemberTookDamage()
        {
            LastHitOnGroupTime = Time.time;
        }

        public void NotifyAttackLanded(string skillId = null)
        {
            _totalHitsLanded++;
            if (!string.IsNullOrWhiteSpace(skillId))
                GetSkillStats(skillId).landed++;
        }

        public void NotifyAttackMissed(string skillId = null)
        {
            _totalHitsMissed++;
            if (!string.IsNullOrWhiteSpace(skillId))
                GetSkillStats(skillId).missed++;
        }

        public float GetSkillHitAccuracy(string skillId)
        {
            if (string.IsNullOrWhiteSpace(skillId) || !_skillStats.TryGetValue(skillId, out var stats))
                return 0.5f;

            var total = stats.landed + stats.missed;
            return total > 0 ? (float)stats.landed / total : 0.5f;
        }

        public bool IsPlayerDodgingFrequently(int threshold = 2) => _playerDodgeCount >= threshold;
        public bool IsPlayerGuardingFrequently(int threshold = 2) => _playerGuardCount >= threshold;
        public bool IsPlayerAttackingFrequently(int threshold = 3) => _playerAttackCount >= threshold;
        public bool IsPlayerRecoveringFrequently(int threshold = 2) => _playerRecoverCount >= threshold;

        public string BuildPlayerReadSummary()
            => $"Dodge={_playerDodgeCount}, Guard={_playerGuardCount}, Attack={_playerAttackCount}, Recover={_playerRecoverCount}";

        private void UpdatePlayerObservation(float dt)
        {
            if (_playerTransform == null)
                return;

            var moved = Vector3.Distance(_playerTransform.position, _lastPlayerPosition);
            _lastPlayerPosition = _playerTransform.position;
            _playerIdleTimer = moved < 0.05f ? _playerIdleTimer + dt : 0f;

            var stateName = GetPlayerStateName();
            if (stateName != _lastObservedPlayerStateName)
            {
                if (IsPlayerDodgeState(stateName))
                    NotifyPlayerDodgeObserved();
                if (IsPlayerGuardState(stateName))
                    NotifyPlayerGuardObserved();
                if (IsPlayerInCombatState())
                    NotifyPlayerAttackObserved();

                _lastObservedPlayerStateName = stateName;
            }

            var isRecovering = IsPlayerRecovering;
            if (isRecovering && !_wasPlayerRecovering)
                NotifyPlayerRecoverObserved();

            _wasPlayerRecovering = isRecovering;
        }

        private string GetPlayerStateName()
            => _playerController?.CurrentState?.StateName ?? "";

        /// <summary>
        /// 플레이어가 공격(Combat 태그) 상태인가.
        /// 상태명 문자열 목록 대신 ActorStateTag.Combat으로 판별해, 새 공격 상태 추가 시 누락되지 않게 한다.
        /// </summary>
        private bool IsPlayerInCombatState()
        {
            var state = _playerController?.CurrentState;
            return state != null && (state.StateTags & ActorStateTag.Combat) != 0;
        }

        private static void UpdateWindow(ref float timer, ref int count, float window, float dt)
        {
            timer += dt;
            if (timer < window)
                return;

            timer = 0f;
            count = 0;
        }

        private void NotifyPlayerDodgeObserved()
        {
            _playerDodgeCount++;
            _dodgeWindowTimer = 0f;
        }

        private void NotifyPlayerGuardObserved()
        {
            _playerGuardCount++;
            _guardWindowTimer = 0f;
        }

        private void NotifyPlayerAttackObserved()
        {
            _playerAttackCount++;
            _attackWindowTimer = 0f;
        }

        private void NotifyPlayerRecoverObserved()
        {
            _playerRecoverCount++;
            _recoverWindowTimer = 0f;
        }

        private SkillHitStats GetSkillStats(string skillId)
        {
            if (!_skillStats.TryGetValue(skillId, out var stats))
            {
                stats = new SkillHitStats();
                _skillStats[skillId] = stats;
            }

            return stats;
        }

        private static bool IsPlayerDodgeState(string stateName)
            => stateName == "Dodge";

        private static bool IsPlayerGuardState(string stateName)
            => stateName == "Guard";

        private sealed class SkillHitStats
        {
            public int landed;
            public int missed;
        }
    }
}
