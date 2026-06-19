using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Path;
using UPlayGround.Group;

namespace UPlayGround.Manager
{
    /// <summary>
    /// ActorID 기반으로 런타임에 Actor를 스폰하고 추적하는 매니저.
    /// GameManager에서 초기화 순서 중 GameObjectManager 이후에 등록한다.
    /// </summary>
    public class ActorSpawnManager : BaseManager<ActorSpawnManager>, IManager, IAsyncInitializableManager,
        IUpdatableManager
    {
        private const string DATABASE_KEY = "ActorDatabase";
        private ActorDatabase _database;
        
        public bool IsDBLoaded { get; private set; } = false;
        
        // instanceID → 스폰 정보
        private readonly Dictionary<int, SpawnedActorInfo> _spawnedActors = new();

        // CleanupDestroyedActors에서 재사용해 GC 할당 방지
        private readonly List<int> _cleanupBuffer = new();

        public ActorDatabase Database => _database;

        /// <summary>현재 살아있는 스폰 정보 맵 (읽기 전용)</summary>
        public IReadOnlyDictionary<int, SpawnedActorInfo> SpawnedActors => _spawnedActors;
        
        // DB 로드 전 들어온 등록 요청을 임시 보관하는 리스트
        private readonly List<GameActor> _pendingRegistrationQueue = new();
       
        // ── IManager 구현 ────────────────────────────────────────────

        public void Init()
        {
            _spawnedActors.Clear();
        }

        public UniTask InitializeAsync(CancellationToken cancellationToken) =>
            LoadDatabaseAsync(cancellationToken);

        #region 데이터베이스 로드

        private async UniTask LoadDatabaseAsync(CancellationToken cancellationToken)
        {
            try
            {
                _database = await AssetManager.Instance.LoadGlobalAsync<ActorDatabase>(
                    DATABASE_KEY,
                    nameof(ActorSpawnManager),
                    cancellationToken);

                _database.Initialize();
                IsDBLoaded = true;
                
                // DB 로드가 완료되었으므로 대기 중인 Actor들을 처리합니다.
                ProcessPendingActors();
                
                Debug.Log("[ActorSpawnManager] Database 로드 완료");
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception e)
            {
                Debug.LogError($"[ActorSpawnManager] Database 로드 실패: {e.Message}");
                throw;
            }
        }
        /// <summary>
        /// DB 로드 완료 후, 대기 리스트에 있던 Actor들에게 데이터를 주입하고 정식 등록합니다.
        /// </summary>
        private void ProcessPendingActors()
        {
            if (_pendingRegistrationQueue.Count == 0) return;

            foreach (var actor in _pendingRegistrationQueue)
            {
                if (actor != null)
                {
                    RegisterActor(actor);
                }
            }

            _pendingRegistrationQueue.Clear();
        }
        #endregion

        public void AfterInit()
        {
            // 씬에 이미 배치된 Actor를 자동 스캔하여 등록
            var sceneActors = FindObjectsByType<GameActor>(FindObjectsSortMode.None);
            int count = 0;
            foreach (var actor in sceneActors)
            {
                if (!string.IsNullOrEmpty(actor.ActorId))
                {
                    RegisterActor(actor);
                    count++;
                }
            }
            if (count > 0)
                Debug.Log($"[ActorSpawnManager] 씬 배치 Actor {count}개 자동 등록 완료");
        }

        public void Dispose()
        {
            _spawnedActors.Clear();
            _pendingRegistrationQueue.Clear();

            _database = null;
            IsDBLoaded = false;
        }

        public void OnUpdate() => CleanupDestroyedActors();

        public void OnFixedUpdate() { }
        public void OnLateUpdate() { }

        public void OnSceneChanged(string sceneType)
        {
            // 씬 전환 시 스폰 기록 초기화 (실제 오브젝트는 Unity가 정리)
            _spawnedActors.Clear();
        }

        // ── 스폰 API ─────────────────────────────────────────────────

        /// <summary>
        /// enum 기반 오버로드. actorId 문자열 오타를 컴파일 타임에 방지한다.
        /// </summary>
        public GameActor SpawnActor(
            ActorIdType actorIdType,
            Vector3 position,
            Quaternion rotation,
            MonsterGroupController group = null,
            Transform parent = null)
        {
            return SpawnActor(actorIdType.ToActorId(), position, rotation, group, parent);
        }

        /// <summary>
        /// actorId에 해당하는 Actor를 지정 위치에 스폰한다.
        /// </summary>
        /// <param name="actorId">ActorDatabase에 등록된 actorId</param>
        /// <param name="position">스폰 위치</param>
        /// <param name="rotation">스폰 회전</param>
        /// <param name="group">소속 그룹 (MonsterActor에만 적용). null이면 그룹 없음.</param>
        /// <param name="parent">부모 Transform. null이면 씬 루트에 스폰.</param>
        /// <returns>스폰된 GameActor. 실패 시 null.</returns>
        public GameActor SpawnActor(
            string actorId,
            Vector3 position,
            Quaternion rotation,
            MonsterGroupController group = null,
            Transform parent = null)
        {
            if (_database == null)
            {
                Debug.LogError("[ActorSpawnManager] ActorDatabase가 null입니다.");
                return null;
            }

            if (!_database.TryGetDefinition(actorId, out var definition))
            {
                Debug.LogError($"[ActorSpawnManager] actorId '{actorId}'를 ActorDatabase에서 찾을 수 없습니다.");
                return null;
            }

            if (definition.prefab == null)
            {
                Debug.LogError($"[ActorSpawnManager] actorId '{actorId}'의 prefab이 null입니다.");
                return null;
            }

            var go = Instantiate(definition.prefab, position, rotation, parent);
            var actor = go.GetComponent<GameActor>();

            if (actor == null)
            {
                Debug.LogError($"[ActorSpawnManager] 스폰된 프리팹 '{definition.prefab.name}'에 GameActor 컴포넌트가 없습니다.");
                Destroy(go);
                return null;
            }

            // 정의 주입 (스탯, ID 등 재적용)
            actor.SetDefinition(definition);

            var info = new SpawnedActorInfo
            {
                actorId       = actorId,
                actor         = actor,
                spawnTime     = Time.time,
                spawnPosition = position,
                group         = group,
            };
            _spawnedActors[go.GetInstanceID()] = info;

            // MonsterActor면 그룹에 등록
            if (group != null && actor is MonsterActor monsterActor)
                group.RegisterMember(monsterActor, MemberPriority.Normal);

            Debug.Log($"[ActorSpawnManager] '{actorId}' 스폰 완료 — 위치: {position}, 그룹: {group?.name ?? "없음"}");
            return actor;
        }

        /// <summary>
        /// Actor를 추적 목록에 수동 등록한다.
        /// 씬에 배치된 Actor나 스킬로 수동 소환된 Actor에 사용.
        /// 이미 등록된 경우 무시한다.
        /// </summary>
        /// <param name="actor">등록할 GameActor</param>
        /// <param name="actorIdOverride">actorId 강제 지정. null이면 actor.ActorId를 사용.</param>
        public void RegisterActor(GameActor actor, string actorIdOverride = null)
        {
            if (actor == null) return;
            
            if (!IsDBLoaded)
            {
                if (!_pendingRegistrationQueue.Contains(actor))
                {
                    _pendingRegistrationQueue.Add(actor);
                }
                return;
            }
            
            int instanceId = actor.gameObject.GetInstanceID();
            if (_spawnedActors.ContainsKey(instanceId)) return;

            string id = !string.IsNullOrEmpty(actorIdOverride) ? actorIdOverride : actor.ActorId;
            if (string.IsNullOrEmpty(id)) return;

            // Definition이 없으면 Database에서 자동 주입
            if (actor.Definition == null && _database != null &&
                _database.TryGetDefinition(id, out var def))
                actor.SetDefinition(def);

            _spawnedActors[instanceId] = new SpawnedActorInfo
            {
                actorId       = id,
                actor         = actor,
                spawnTime     = Time.time,
                spawnPosition = actor.transform.position,
                group         = null,
            };
        }

        /// <summary>enum 기반 actorId로 스폰된 살아있는 Actor 목록 반환.</summary>
        public List<GameActor> GetSpawnedActors(ActorIdType actorIdType)
            => GetSpawnedActors(actorIdType.ToActorId());

        /// <summary>특정 actorId로 스폰된 살아있는 Actor 목록 반환.</summary>
        public List<GameActor> GetSpawnedActors(string actorId)
        {
            var result = new List<GameActor>();
            foreach (var kv in _spawnedActors)
            {
                if (kv.Value.actorId == actorId && kv.Value.actor != null)
                    result.Add(kv.Value.actor);
            }
            return result;
        }

        /// <summary>스폰된 모든 Actor 반환 (파괴된 항목 제외).</summary>
        public List<GameActor> GetAllSpawnedActors()
        {
            var result = new List<GameActor>();
            foreach (var kv in _spawnedActors)
            {
                if (kv.Value.actor != null)
                    result.Add(kv.Value.actor);
            }
            return result;
        }

        // ── 내부 ─────────────────────────────────────────────────────

        private void CleanupDestroyedActors()
        {
            _cleanupBuffer.Clear();
            foreach (var kv in _spawnedActors)
            {
                if (kv.Value.actor == null)
                    _cleanupBuffer.Add(kv.Key);
            }
            foreach (var key in _cleanupBuffer)
                _spawnedActors.Remove(key);
        }

        // ── 데이터 구조 ───────────────────────────────────────────────

        public class SpawnedActorInfo
        {
            public string                actorId;
            public GameActor             actor;
            public float                 spawnTime;
            public Vector3               spawnPosition;
            public MonsterGroupController group;
        }
    }
}
