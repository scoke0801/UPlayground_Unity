using UnityEngine;
using UPlayGround.Data.Cycle;
using UPlayGround.Manager;

namespace UPlayGround.Cycle
{
    /// <summary>사이클 보스의 발견/처치 신호만 전달하는 얇은 런타임 컴포넌트.</summary>
    public sealed class CycleBossRuntimeHandle : MonoBehaviour
    {
        private MonsterActor _monster;
        private string _spawnId;
        private bool _isCentral;
        private bool _discovered;
        private PlayerActor _player;
        private float _lastPlayerHealth;
        private bool _playerTookDamage;

        public string SpawnId => _spawnId;
        public bool IsCentral => _isCentral;

        public void Initialize(MonsterActor monster, CycleBossPlacement placement, float encounterRadius = 12f)
        {
            _monster = monster;
            _spawnId = placement.spawnId;
            _isCentral = placement.isCentral;
            _discovered = placement.discovered;
            _playerTookDamage = placement.playerTookDamageAfterDiscovery;
            _monster.OnDied -= OnMonsterDied;
            _monster.OnDied += OnMonsterDied;

            if (!_discovered)
            {
                GameObject triggerObject = new($"Encounter_{_spawnId}");
                triggerObject.transform.SetParent(transform, false);
                SphereCollider trigger = triggerObject.AddComponent<SphereCollider>();
                trigger.isTrigger = true;
                trigger.radius = Mathf.Max(1f, encounterRadius);
                CycleBossEncounterTrigger relay = triggerObject.AddComponent<CycleBossEncounterTrigger>();
                relay.Initialize(this);
            }
            else
            {
                BeginPlayerDamageTracking();
            }
        }

        public void Discover()
        {
            if (_discovered) return;
            _discovered = CycleRunManager.Instance?.DiscoverBoss(_spawnId) ?? false;
            if (_discovered) BeginPlayerDamageTracking();
        }

        private void BeginPlayerDamageTracking()
        {
            _player = GameObjectManager.Instance?.Player;
            if (_player == null) return;
            _lastPlayerHealth = _player.GetCurrentHealth();
            _player.OnHpChanged -= OnPlayerHpChanged;
            _player.OnHpChanged += OnPlayerHpChanged;
        }

        private void OnPlayerHpChanged(float current, float max)
        {
            if (!_playerTookDamage && current < _lastPlayerHealth - 0.001f)
            {
                _playerTookDamage = true;
                CycleRunManager.Instance?.ReportPlayerDamageDuringBossEncounter(_spawnId);
            }
            _lastPlayerHealth = current;
        }

        private void OnMonsterDied(MonsterActor monster)
        {
            Discover();
            CycleRunManager.Instance?.ReportBossDefeatContext(
                _spawnId,
                monster.LastDeathWasSpecialBreak,
                !_playerTookDamage);
            BossAssistManager.Instance?.ReportBossDefeatContext(_spawnId, monster.LastDeathWasSpecialBreak, !_playerTookDamage);
            CycleRunManager.Instance?.NotifyBossDefeated(_spawnId);
        }

        private void OnDestroy()
        {
            if (_monster != null) _monster.OnDied -= OnMonsterDied;
            if (_player != null) _player.OnHpChanged -= OnPlayerHpChanged;
        }
    }

}
