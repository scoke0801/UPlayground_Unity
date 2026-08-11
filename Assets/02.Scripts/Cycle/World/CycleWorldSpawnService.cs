using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Cycle;
using UPlayGround.Data.Stat;
using UPlayGround.Manager;

namespace UPlayGround.Cycle
{
    public sealed class CycleWorldSpawnService : ICycleWorldSpawnService
    {
        private readonly CycleWorldContext _context;
        private readonly CycleRunManager _runManager;
        private readonly List<GameObject> _spawnedObjects = new();

        public CycleWorldSpawnService(CycleWorldContext context, CycleRunManager runManager)
        {
            _context = context;
            _runManager = runManager;
        }

        public bool TryBuildAndSpawn(
            CycleRunState run,
            Func<CycleRandomStream, System.Random> randomFactory,
            out CycleLayoutState layout,
            out string error)
        {
            layout = null;
            error = null;
            CycleWorldConfigSO config = _context != null ? _context.Config : null;
            if (config == null || !config.Validate(out error)) return false;
            if (!string.Equals(config.mapId, run.mapId, StringComparison.Ordinal))
            {
                error = $"월드 설정 mapId({config.mapId})와 현재 맵({run.mapId})이 다릅니다.";
                return false;
            }

            CycleSpawnPoint[] points = UnityEngine.Object.FindObjectsByType<CycleSpawnPoint>(FindObjectsSortMode.None);
            CentralBossSpawnPoint[] centralPoints = UnityEngine.Object.FindObjectsByType<CentralBossSpawnPoint>(FindObjectsSortMode.None);
            if (centralPoints.Length != 1)
            {
                error = $"CentralBossSpawnPoint는 정확히 하나여야 합니다: {centralPoints.Length}";
                return false;
            }

            // 고정 스폰 도입으로 Layout 스트림의 시작점 추첨은 하지 않는다.
            System.Random layoutRandom = randomFactory(CycleRandomStream.Layout);
            System.Random bossRandom = randomFactory(CycleRandomStream.BossPool);

            CycleSpawnPoint playerPoint = points.FirstOrDefault(point =>
                point != null &&
                string.Equals(point.SpawnId, config.fixedPlayerSpawnId, StringComparison.Ordinal));
            if (playerPoint == null)
            {
                error = $"고정 플레이어 스폰 '{config.fixedPlayerSpawnId}'과(와) 같은 Spawn ID를 가진 CycleSpawnPoint가 씬에 없습니다. " +
                        "월드 설정의 fixedPlayerSpawnId 철자를 확인하거나 해당 Spawn ID의 CycleSpawnPoint를 씬에 배치하세요.";
                return false;
            }
            if (!playerPoint.Allows(CycleSpawnRole.Player))
            {
                error = $"고정 플레이어 스폰 '{config.fixedPlayerSpawnId}'을(를) 씬에서 찾았지만 Player 역할이 설정되어 있지 않습니다. " +
                        $"오브젝트 '{playerPoint.name}'의 CycleSpawnPoint에서 Allowed Roles에 Player를 추가하세요.";
                return false;
            }

            List<CycleSpawnPoint> roleCandidates = points
                .Where(point => point != null &&
                                point.Allows(CycleSpawnRole.OuterBoss) &&
                                !string.IsNullOrWhiteSpace(point.SpawnId))
                .Where(point => !string.Equals(point.SpawnId, playerPoint.SpawnId, StringComparison.Ordinal))
                .ToList();
            List<CycleSpawnPoint> candidates = roleCandidates
                .Where(point =>
                    (point.Position - playerPoint.Position).sqrMagnitude >=
                    playerPoint.SafetyRadius * playerPoint.SafetyRadius)
                .OrderBy(point => point.SpawnId, StringComparer.Ordinal)
                .ToList();

            List<CycleSpawnPoint> selected = SelectOuterPoints(
                candidates,
                config.outerBossCount,
                config.maxSameSectorBossCount,
                layoutRandom);
            if (selected.Count != config.outerBossCount)
            {
                error = $"섹터/안전 반경 조건을 만족하는 외곽 보스 후보가 부족합니다: " +
                        $"{selected.Count}/{config.outerBossCount} " +
                        $"(역할 후보 {roleCandidates.Count}, 안전 반경 통과 {candidates.Count}, " +
                        $"플레이어 반경 {playerPoint.SafetyRadius:0.##}, 플레이어 스폰 {playerPoint.SpawnId})";
                return false;
            }

            List<string> outerPool = config.outerBossActorIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            List<string> centralPool = config.centralBossActorIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .OrderBy(id => id, StringComparer.Ordinal)
                .ToList();
            if (!ValidateActorPool(outerPool, out error) || !ValidateActorPool(centralPool, out error))
                return false;

            layout = new CycleLayoutState { playerSpawnId = playerPoint.SpawnId };
            foreach (CycleSpawnPoint point in selected)
            {
                layout.outerBosses.Add(new CycleBossPlacement
                {
                    spawnId = point.SpawnId,
                    actorId = outerPool[bossRandom.Next(outerPool.Count)],
                });
            }

            layout.centralBoss = new CycleBossPlacement
            {
                spawnId = centralPoints[0].SpawnId,
                actorId = centralPool[bossRandom.Next(centralPool.Count)],
                isCentral = true,
            };
            layout.activeRespawnPointIds = points
                .Where(point => point != null && point.Allows(CycleSpawnRole.Respawn))
                .OrderBy(point => point.SpawnId, StringComparer.Ordinal)
                .Select(point => point.SpawnId)
                .ToList();

            if (!SpawnLayout(run, layout, playerPoint, points, centralPoints[0], out error))
            {
                CleanupRunObjects();
                return false;
            }

            return true;
        }

        public bool TryRestore(CycleRunState run, CycleLayoutState layout, out string error)
        {
            error = null;
            if (layout == null)
            {
                error = "저장된 레이아웃이 없습니다.";
                return false;
            }

            CycleSpawnPoint[] points = UnityEngine.Object.FindObjectsByType<CycleSpawnPoint>(FindObjectsSortMode.None);
            CentralBossSpawnPoint central = UnityEngine.Object.FindFirstObjectByType<CentralBossSpawnPoint>();
            CycleSpawnPoint player = points.FirstOrDefault(point => point.SpawnId == layout.playerSpawnId);
            if (player == null || central == null)
            {
                error = "저장된 spawnId를 현재 씬에서 찾지 못했습니다.";
                return false;
            }

            return SpawnLayout(run, layout, player, points, central, out error, restore: true);
        }

        public void CleanupRunObjects()
        {
            CycleBossMarkerRegistry.Clear();
            foreach (GameObject value in _spawnedObjects)
            {
                if (value == null) continue;
                value.SetActive(false);
                UnityEngine.Object.Destroy(value);
            }
            _spawnedObjects.Clear();
        }

        public void OnSceneChanged(string sceneType)
        {
            CycleBossMarkerRegistry.Clear();
            _spawnedObjects.Clear();
        }

        private bool SpawnLayout(
            CycleRunState run,
            CycleLayoutState layout,
            CycleSpawnPoint playerPoint,
            CycleSpawnPoint[] points,
            CentralBossSpawnPoint centralPoint,
            out string error,
            bool restore = false)
        {
            error = null;
            PlayerActor player = GameObjectManager.Instance?.Player ??
                                 UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
            if (!restore && player != null)
            {
                if (player.ActorController?.Motor != null)
                    player.ActorController.Motor.SetPositionAndRotation(playerPoint.Position, playerPoint.Rotation);
                else
                    player.transform.SetPositionAndRotation(playerPoint.Position, playerPoint.Rotation);
                CameraManager.Instance?.SnapToTarget(playerPoint.Position);
            }

            Dictionary<string, CycleSpawnPoint> lookup = points
                .Where(point => point != null)
                .GroupBy(point => point.SpawnId)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            foreach (CycleBossPlacement boss in layout.outerBosses)
            {
                if (boss.defeated) continue;
                if (!lookup.TryGetValue(boss.spawnId, out CycleSpawnPoint point))
                {
                    error = $"외곽 spawnId 누락: {boss.spawnId}";
                    return false;
                }
                if (!SpawnBoss(run, boss, point.Position, point.Rotation, true, out error))
                    return false;
            }

            bool outerTrialsCleared = layout.outerBosses.Count > 0
                                      && layout.outerBosses.All(boss => boss != null && boss.defeated);
            if (layout.centralBoss != null &&
                !layout.centralBoss.defeated &&
                !SpawnBoss(
                    run,
                    layout.centralBoss,
                    centralPoint.Position,
                    centralPoint.Rotation,
                    outerTrialsCleared,
                    out error))
            {
                return false;
            }

            return true;
        }

        private bool SpawnBoss(
            CycleRunState run,
            CycleBossPlacement placement,
            Vector3 position,
            Quaternion rotation,
            bool activeOnSpawn,
            out string error)
        {
            error = null;
            CycleBossRuntimeHandle[] existing =
                UnityEngine.Object.FindObjectsByType<CycleBossRuntimeHandle>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            if (Array.Exists(existing, handle => handle != null && handle.SpawnId == placement.spawnId))
                return true;

            GameActor actor = ActorSpawnManager.Instance?.SpawnActor(placement.actorId, position, rotation);
            if (actor is not MonsterActor monster)
            {
                error = $"'{placement.actorId}'가 MonsterActor로 생성되지 않았습니다.";
                if (actor != null) UnityEngine.Object.Destroy(actor.gameObject);
                return false;
            }

            CycleDifficultyEntry difficulty = _runManager.GetCurrentDifficulty();
            float hpDifficulty = difficulty?.healthMultiplier ?? 1f;
            int runtimeLevel = Mathf.Max(1, (_context.Config?.baseMonsterLevel ?? 1) + run.cycleIndex - 1);
            monster.ApplyRuntimeLevel(runtimeLevel, hpDifficulty);
            if (monster.AbilitySystem != null && hpDifficulty > 0f)
            {
                float attackDifficulty = difficulty?.attackMultiplier ?? 1f;
                monster.AbilitySystem.TryGetAttribute(
                    global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower,
                    current: false,
                    out float currentAttack);
                monster.AbilitySystem.SetAttributeBase(
                    global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower,
                    currentAttack * attackDifficulty / hpDifficulty);
            }

            float rewardMultiplier = difficulty != null
                ? Mathf.Max(0f, difficulty.rewardMultiplier)
                : 1f;
            monster.SetRuntimeRewards(
                (long)Math.Round(monster.BaseExpReward * rewardMultiplier),
                Mathf.RoundToInt(monster.BaseGoldReward * rewardMultiplier));

            CycleBossRuntimeHandle handle = monster.gameObject.AddComponent<CycleBossRuntimeHandle>();
            handle.Initialize(monster, placement);
            // 외곽 수호자는 탐색 선택지를 주기 위해 처음부터 표시한다.
            // 중앙 평가는 아직 갈 수 없는 목적지를 먼저 찍지 않고, 외곽 3체 완료 시 활성화와 함께 공개한다.
            if (activeOnSpawn || !placement.isCentral)
            {
                CycleBossMarkerRegistry.Register(new CycleBossMarkerData(
                    placement.spawnId,
                    position,
                    placement.discovered,
                    placement.isCentral));
            }
            _spawnedObjects.Add(monster.gameObject);
            if (!activeOnSpawn)
                monster.gameObject.SetActive(false);
            return true;
        }

        public bool TryActivateBoss(string spawnId)
        {
            if (string.IsNullOrWhiteSpace(spawnId))
                return false;

            for (int i = 0; i < _spawnedObjects.Count; i++)
            {
                GameObject spawned = _spawnedObjects[i];
                if (spawned == null)
                    continue;

                CycleBossRuntimeHandle handle = spawned.GetComponent<CycleBossRuntimeHandle>();
                if (handle == null || !string.Equals(handle.SpawnId, spawnId, StringComparison.Ordinal))
                    continue;

                if (!CycleBossMarkerRegistry.TryGet(spawnId, out _))
                {
                    CycleBossMarkerRegistry.Register(new CycleBossMarkerData(
                        handle.SpawnId,
                        spawned.transform.position,
                        handle.IsDiscovered,
                        handle.IsCentral));
                }

                spawned.SetActive(true);
                return true;
            }

            return false;
        }

        private static List<CycleSpawnPoint> SelectOuterPoints(
            List<CycleSpawnPoint> candidates,
            int count,
            int perSector,
            System.Random random)
        {
            List<CycleSpawnPoint> pool = new(candidates);
            List<CycleSpawnPoint> result = new();
            Dictionary<string, int> sectorCounts = new(StringComparer.Ordinal);
            while (pool.Count > 0 && result.Count < count)
            {
                int index = random.Next(pool.Count);
                CycleSpawnPoint point = pool[index];
                pool.RemoveAt(index);
                string sector = point.SectorId;
                sectorCounts.TryGetValue(sector, out int used);
                if (used >= perSector) continue;
                result.Add(point);
                sectorCounts[sector] = used + 1;
            }
            return result;
        }

        private static bool ValidateActorPool(List<string> pool, out string error)
        {
            if (pool.Count == 0)
            {
                error = "보스 Actor ID 풀이 비어 있습니다.";
                return false;
            }

            foreach (string id in pool)
            {
                if (ActorSpawnManager.Instance?.Database != null &&
                    ActorSpawnManager.Instance.Database.Contains(id))
                    continue;
                error = $"ActorDatabase에 보스 ID가 없습니다: {id}";
                return false;
            }

            error = null;
            return true;
        }
    }
}
