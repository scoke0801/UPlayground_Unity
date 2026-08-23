using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UPlayGround.Components;
using UPlayGround.Data;
using UPlayGround.Data.Projectile;

namespace UPlayGround.Manager
{
    public sealed class ProjectileManager : BaseManager<ProjectileManager>,
        IManager, IUpdatableManager, IProjectileService
    {
        private const int ActiveLimit = 256;

        private sealed class PoolEntry
        {
            public ObjectPool<ProjectileRuntime> pool;
            public bool prewarmed;
        }

        private struct PendingSpawn
        {
            public ProjectileSpawnRequest request;
            public AttackData attackData;
            public float remaining;
        }

        private readonly Dictionary<ProjectileDefinitionSO, PoolEntry> _pools = new();
        private readonly List<ProjectileRuntime> _active = new(128);
        private readonly List<PendingSpawn> _pending = new(32);
        private readonly List<ProjectileRuntime> _prewarmBuffer = new(32);
        private int _peakActive;

        public int CountActive => _active.Count;
        public int CountAll
        {
            get
            {
                int count = 0;
                foreach (PoolEntry entry in _pools.Values)
                    count += entry.pool.CountAll;
                return count;
            }
        }
        public int CountInactive
        {
            get
            {
                int count = 0;
                foreach (PoolEntry entry in _pools.Values)
                    count += entry.pool.CountInactive;
                return count;
            }
        }

        public void Init()
        {
        }

        public void AfterInit() { }

        public void Spawn(ProjectileSpawnRequest request)
        {
            QueueSpawn(request, ResolveAttackData(request));
        }

        private void QueueSpawn(ProjectileSpawnRequest request, AttackData attackData)
        {
            if (request.definition == null)
            {
                Debug.LogWarning("[ProjectileManager] Definition이 없는 스폰 요청을 무시합니다.");
                return;
            }
            if (request.generation > request.definition.maxGeneration)
            {
                Debug.LogWarning($"[ProjectileManager] 분열 세대 상한 초과: {request.definition.name}");
                return;
            }
            if (request.delay > 0f)
            {
                _pending.Add(new PendingSpawn
                {
                    request = request,
                    attackData = PlayerAttackController.Copy(attackData),
                    remaining = request.delay,
                });
                return;
            }

            SpawnImmediate(request, attackData);
        }

        public bool TryReflect(GameObject projectileObject, GameObject newOwner, Vector3 direction)
        {
            if (projectileObject == null || newOwner == null)
                return false;
            ProjectileRuntime runtime = projectileObject.GetComponentInParent<ProjectileRuntime>();
            GameActor owner = newOwner.GetComponent<GameActor>();
            return runtime != null && runtime.TryReflect(owner, direction);
        }

        public void OnUpdate()
        {
            float deltaTime = Time.deltaTime;
            TickPending(deltaTime);
            for (int i = _active.Count - 1; i >= 0; i--)
            {
                ProjectileRuntime runtime = _active[i];
                if (runtime == null)
                {
                    _active.RemoveAt(i);
                    continue;
                }
                runtime.Tick(deltaTime);
            }
            _peakActive = Mathf.Max(_peakActive, _active.Count);
        }

        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void OnSceneChanged(string sceneType)
        {
            ReturnAll();
            _pending.Clear();
        }

        public void Dispose()
        {
            ReturnAll();
            _pending.Clear();
            foreach (PoolEntry entry in _pools.Values)
                entry.pool.Clear();
            _pools.Clear();
        }

        private void SpawnImmediate(ProjectileSpawnRequest request, AttackData attackData)
        {
            if (_active.Count >= ActiveLimit)
            {
                ProjectileRuntime oldest = FindOldest();
                oldest?.ForceReturn();
                Debug.LogWarning($"[ProjectileManager] 활성 상한 {ActiveLimit} 초과로 가장 오래된 투사체를 회수했습니다.");
            }

            PoolEntry entry = GetOrCreatePool(request.definition);
            EnsurePrewarmed(request.definition, entry);
            ProjectileRuntime runtime = entry.pool.Get();
            runtime.Initialize(
                request.definition,
                request,
                PlayerAttackController.Copy(attackData),
                ReturnRuntime,
                QueueSpawn);
            _active.Add(runtime);
        }

        private PoolEntry GetOrCreatePool(ProjectileDefinitionSO definition)
        {
            if (_pools.TryGetValue(definition, out PoolEntry existing))
                return existing;

            var entry = new PoolEntry();
            entry.pool = new ObjectPool<ProjectileRuntime>(
                createFunc: () => CreateRuntime(definition),
                actionOnGet: runtime => runtime.gameObject.SetActive(true),
                actionOnRelease: runtime => runtime.OnReturnedToPool(),
                actionOnDestroy: runtime =>
                {
                    if (runtime != null)
                        Destroy(runtime.gameObject);
                },
                collectionCheck: true,
                defaultCapacity: Mathf.Max(1, definition.prewarmCount),
                maxSize: Mathf.Max(1, definition.maxPoolSize));
            _pools.Add(definition, entry);
            return entry;
        }

        private static ProjectileRuntime CreateRuntime(ProjectileDefinitionSO definition)
        {
            GameObject instance = definition.visualPrefab != null
                ? Instantiate(definition.visualPrefab)
                : new GameObject(definition.name);
            instance.name = $"{definition.name} (Pooled)";
            ProjectileRuntime runtime = instance.GetComponent<ProjectileRuntime>();
            if (runtime == null)
                runtime = instance.AddComponent<ProjectileRuntime>();
            BaseProjectile[] legacyProjectiles = instance.GetComponentsInChildren<BaseProjectile>(true);
            for (int i = 0; i < legacyProjectiles.Length; i++)
                legacyProjectiles[i].enabled = false;
            instance.SetActive(false);
            DontDestroyOnLoad(instance);
            return runtime;
        }

        private void EnsurePrewarmed(ProjectileDefinitionSO definition, PoolEntry entry)
        {
            if (entry.prewarmed)
                return;
            entry.prewarmed = true;
            _prewarmBuffer.Clear();
            int count = Mathf.Min(definition.prewarmCount, definition.maxPoolSize);
            for (int i = 0; i < count; i++)
                _prewarmBuffer.Add(entry.pool.Get());
            for (int i = 0; i < _prewarmBuffer.Count; i++)
                entry.pool.Release(_prewarmBuffer[i]);
            _prewarmBuffer.Clear();
        }

        private AttackData ResolveAttackData(in ProjectileSpawnRequest request)
        {
            GameActor actor = request.owner != null
                ? request.owner.GetComponent<GameActor>()
                : null;
            AttackData data = null;
            if (request.hitPhaseIndex >= 0)
            {
                if (actor is PlayerActor player)
                    data = player.GetCombat()?.CreateProjectileAttackData(request.hitPhaseIndex);
                else
                    data = actor != null
                        ? actor.GetComponent<Components.EnemyCombat>()
                            ?.CreateProjectileAttackData(request.hitPhaseIndex)
                        : null;
            }

            data ??= new AttackData
            {
                damage = request.legacyDamage,
                attackKind = Data.EnumType.AttackKind.SkillAttack,
            };
            data.attacker = actor;
            data.damage *= request.damageScale <= 0f ? 1f : request.damageScale;
            data.poiseDamage *= request.damageScale <= 0f ? 1f : request.damageScale;
            data.breakDamage *= request.damageScale <= 0f ? 1f : request.damageScale;
            data.isProjectile = true;
            return data;
        }

        private void ReturnRuntime(ProjectileRuntime runtime, bool expired)
        {
            if (runtime == null)
                return;
            ProjectileDefinitionSO definition = runtime.Definition;
            _active.Remove(runtime);
            if (definition != null && _pools.TryGetValue(definition, out PoolEntry entry))
                entry.pool.Release(runtime);
            else
                Destroy(runtime.gameObject);
        }

        private void TickPending(float deltaTime)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PendingSpawn pending = _pending[i];
                pending.remaining -= deltaTime;
                if (pending.remaining > 0f)
                {
                    _pending[i] = pending;
                    continue;
                }
                _pending.RemoveAt(i);
                pending.request.delay = 0f;
                SpawnImmediate(pending.request, pending.attackData);
            }
        }

        private ProjectileRuntime FindOldest()
        {
            ProjectileRuntime oldest = null;
            float oldestTime = float.MaxValue;
            for (int i = 0; i < _active.Count; i++)
            {
                if (_active[i] != null && _active[i].SpawnTime < oldestTime)
                {
                    oldest = _active[i];
                    oldestTime = _active[i].SpawnTime;
                }
            }
            return oldest;
        }

        private void ReturnAll()
        {
            while (_active.Count > 0)
            {
                ProjectileRuntime runtime = _active[^1];
                if (runtime == null)
                    _active.RemoveAt(_active.Count - 1);
                else
                    runtime.ForceReturn();
            }
        }
    }
}
