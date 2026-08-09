using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UPlayGround.Ability.Core;
using UPlayGround.Data.Actor;
using UPlayGround.Data.Cycle;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Item;
using UPlayGround.Data.Quest;
using UPlayGround.Data.Save;
using UPlayGround.Group;
using UPlayGround.Manager;
using UPlayGround.Data.Stat;
using UPlayGround.World.Generation;

namespace UPlayGround.Cycle
{
    public sealed class CycleWorldSpawnService : ICycleWorldSpawnService
    {
        private readonly CycleWorldContext _context;
        private readonly CycleRunManager _runManager;
        private readonly List<GameObject> _spawnedObjects = new();
        private readonly List<GameObject> _placementExclusionProxies = new();

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
            if (centralPoints.Length != 1) { error = $"CentralBossSpawnPoint는 정확히 하나여야 합니다: {centralPoints.Length}"; return false; }

            // 주의: 시드 결정론 — 과거 구현은 여기서 layoutRandom.Next(playerPoints.Count)로 시작점을 추첨했다.
            // 고정 스폰 도입으로 Layout 스트림의 뽑기 1회가 사라졌으므로 같은 시드라도 외곽 보스 배치가 달라진다.
            // 자리 채움 draw로 과거 시드를 억지로 맞추지 않는다. 상세는 docs/cycle/09_DETERMINISTIC_REPLAY_ADDITIONS.md 5절 참고.
            System.Random layoutRandom = randomFactory(CycleRandomStream.Layout);
            System.Random bossRandom = randomFactory(CycleRandomStream.BossPool);

            // 먼저 SpawnId 일치 여부를 전체 포인트에서 확인해, "존재하지 않음"과 "Player 역할 미설정"을 구분해 보고한다.
            CycleSpawnPoint playerPoint = points.FirstOrDefault(point =>
                point != null &&
                string.Equals(
                    point.SpawnId,
                    config.fixedPlayerSpawnId,
                    StringComparison.Ordinal));
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
                .Where(p => p != null && p.Allows(CycleSpawnRole.OuterBoss) && !string.IsNullOrWhiteSpace(p.SpawnId))
                .Where(p => !string.Equals(p.SpawnId, playerPoint.SpawnId, StringComparison.Ordinal))
                .ToList();
            List<CycleSpawnPoint> candidates = roleCandidates
                .Where(p => (p.Position - playerPoint.Position).sqrMagnitude >= playerPoint.SafetyRadius * playerPoint.SafetyRadius)
                .OrderBy(p => p.SpawnId, StringComparer.Ordinal)
                .ToList();

            List<CycleSpawnPoint> selected = SelectOuterPoints(candidates, config.outerBossCount, config.maxSameSectorBossCount, layoutRandom);
            if (selected.Count != config.outerBossCount)
            {
                error = $"섹터/안전 반경 조건을 만족하는 외곽 보스 후보가 부족합니다: " +
                        $"{selected.Count}/{config.outerBossCount} " +
                        $"(역할 후보 {roleCandidates.Count}, 안전 반경 통과 {candidates.Count}, " +
                        $"플레이어 반경 {playerPoint.SafetyRadius:0.##}, 플레이어 스폰 {playerPoint.SpawnId})";
                return false;
            }

            List<string> outerPool = config.outerBossActorIds.Where(id => !string.IsNullOrWhiteSpace(id)).OrderBy(id => id, StringComparer.Ordinal).ToList();
            List<string> centralPool = config.centralBossActorIds.Where(id => !string.IsNullOrWhiteSpace(id)).OrderBy(id => id, StringComparer.Ordinal).ToList();
            if (!ValidateActorPool(outerPool, out error) || !ValidateActorPool(centralPool, out error)) return false;

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
            layout.activeRespawnPointIds = points.Where(p => p != null && p.Allows(CycleSpawnRole.Respawn)).OrderBy(p => p.SpawnId, StringComparer.Ordinal).Select(p => p.SpawnId).ToList();

            if (!TryBuildGeneratedContent(run, layout, playerPoint, selected, centralPoints[0], randomFactory, out error))
                return false;

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
            if (layout == null) { error = "저장된 레이아웃이 없습니다."; return false; }
            CycleSpawnPoint[] points = UnityEngine.Object.FindObjectsByType<CycleSpawnPoint>(FindObjectsSortMode.None);
            CentralBossSpawnPoint central = UnityEngine.Object.FindFirstObjectByType<CentralBossSpawnPoint>();
            CycleSpawnPoint player = points.FirstOrDefault(p => p.SpawnId == layout.playerSpawnId);
            if (player == null || central == null) { error = "저장된 spawnId를 현재 씬에서 찾지 못했습니다."; return false; }
            Dictionary<string, CycleSpawnPoint> lookup = points
                .Where(p => p != null && !string.IsNullOrWhiteSpace(p.SpawnId))
                .GroupBy(p => p.SpawnId)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            List<CycleSpawnPoint> selected = layout.outerBosses
                .Where(value => value != null && lookup.ContainsKey(value.spawnId))
                .Select(value => lookup[value.spawnId])
                .ToList();
            CycleGeneratedContentLayout originalGenerated = layout.generatedContent;
            bool legacyEmptyGenerated = originalGenerated != null &&
                                        originalGenerated.placementValidationVersion == 0 &&
                                        IsGeneratedContentEmpty(originalGenerated);
            if (originalGenerated == null || legacyEmptyGenerated)
            {
                if (!TryBuildGeneratedContent(run, layout, player, selected, central, _runManager.CreateRandom, out error))
                    return false;
            }
            else if (_context.Config.AutoGeneration.enabled)
            {
                List<CycleGenerationRoutePoint> routes = selected
                    .Where(value => value != null && !string.IsNullOrWhiteSpace(value.SpawnId))
                    .Select(value => new CycleGenerationRoutePoint(value.SpawnId, value.Position))
                    .ToList();
                routes.Add(new CycleGenerationRoutePoint(central.SpawnId, central.Position));
                List<Vector3> bossPositions = selected.Select(value => value.Position).ToList();
                bossPositions.Add(central.Position);
                CycleGeneratedContentLayout staged = originalGenerated.Clone();
                if (!TryResolveGeneratedPositions(
                        staged,
                        player.Position,
                        routes,
                        bossPositions,
                        _context.Config.AutoGeneration,
                        allowRelocation: false,
                        out error))
                {
                    return false;
                }
                layout.generatedContent = staged;
            }

            if (SpawnLayout(run, layout, player, points, central, out error, restore: true))
                return true;

            // 복원 도중 일부 스폰에 성공한 뒤 실패해도 이번 시도에서 만든 오브젝트와
            // 런타임 퀘스트를 남기지 않는다.
            layout.generatedContent = originalGenerated;
            CleanupRunObjects();
            return false;
        }

        public void CleanupRunObjects()
        {
            CycleBossMarkerRegistry.Clear();
            QuestManager.Instance?.UnregisterRuntimeQuests("cycle:auto:");
            CleanupPlacementExclusionProxies();
            foreach (GameObject value in _spawnedObjects)
            {
                if (value == null) continue;
                // Destroy는 프레임 끝까지 지연된다. 즉시 비활성화해 같은 프레임 재시도가
                // 파괴 예약된 boss handle/조우를 기존 정상 인스턴스로 오인하지 않게 한다.
                value.SetActive(false);
                UnityEngine.Object.Destroy(value);
            }
            _spawnedObjects.Clear();
        }

        public void OnSceneChanged(string sceneType)
        {
            CycleBossMarkerRegistry.Clear();
            QuestManager.Instance?.UnregisterRuntimeQuests("cycle:auto:");
            CleanupPlacementExclusionProxies();
            _spawnedObjects.Clear();
        }

        private bool SpawnLayout(CycleRunState run, CycleLayoutState layout, CycleSpawnPoint playerPoint, CycleSpawnPoint[] points, CentralBossSpawnPoint centralPoint, out string error, bool restore = false)
        {
            error = null;
            PlayerActor player = GameObjectManager.Instance?.Player ?? UnityEngine.Object.FindFirstObjectByType<PlayerActor>();
            if (!restore && player != null)
            {
                if (player.ActorController?.Motor != null) player.ActorController.Motor.SetPositionAndRotation(playerPoint.Position, playerPoint.Rotation);
                else player.transform.SetPositionAndRotation(playerPoint.Position, playerPoint.Rotation);
                CameraManager.Instance?.SnapToTarget(playerPoint.Position);
            }

            Dictionary<string, CycleSpawnPoint> lookup = points.Where(p => p != null).GroupBy(p => p.SpawnId).ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);
            foreach (CycleBossPlacement boss in layout.outerBosses)
            {
                if (boss.defeated) continue;
                if (!lookup.TryGetValue(boss.spawnId, out CycleSpawnPoint point)) { error = $"외곽 spawnId 누락: {boss.spawnId}"; return false; }
                if (!SpawnBoss(run, boss, point.Position, point.Rotation, out error)) return false;
            }
            if (layout.centralBoss != null && !layout.centralBoss.defeated && !SpawnBoss(run, layout.centralBoss, centralPoint.Position, centralPoint.Rotation, out error)) return false;
            if (!SpawnGeneratedContent(run, layout, restore, out error)) return false;
            if (!RegisterGeneratedQuest(run, layout, out error)) return false;
            return true;
        }

        private bool SpawnBoss(CycleRunState run, CycleBossPlacement placement, Vector3 position, Quaternion rotation, out string error)
        {
            error = null;
            CycleBossRuntimeHandle[] existing = UnityEngine.Object.FindObjectsByType<CycleBossRuntimeHandle>(FindObjectsSortMode.None);
            if (Array.Exists(existing, handle => handle != null && handle.SpawnId == placement.spawnId)) return true;
            GameActor actor = ActorSpawnManager.Instance?.SpawnActor(placement.actorId, position, rotation);
            if (actor is not MonsterActor monster) { error = $"'{placement.actorId}'가 MonsterActor로 생성되지 않았습니다."; if (actor != null) UnityEngine.Object.Destroy(actor.gameObject); return false; }

            ApplyRuntimeDifficulty(monster, run, 0);
            CycleBossRuntimeHandle handle = monster.gameObject.AddComponent<CycleBossRuntimeHandle>();
            handle.Initialize(monster, placement);
            CycleBossMarkerRegistry.Register(new CycleBossMarkerData(placement.spawnId, position, placement.discovered, placement.isCentral));
            _spawnedObjects.Add(monster.gameObject);
            return true;
        }

        private bool TryBuildGeneratedContent(
            CycleRunState run,
            CycleLayoutState layout,
            CycleSpawnPoint playerPoint,
            IReadOnlyList<CycleSpawnPoint> outerPoints,
            CentralBossSpawnPoint centralPoint,
            Func<CycleRandomStream, System.Random> randomFactory,
            out string error)
        {
            error = null;
            CycleWorldAutoGenerationSettings settings = _context.Config.AutoGeneration;
            if (!settings.enabled)
            {
                layout.generatedContent = new CycleGeneratedContentLayout();
                return true;
            }

            ActorDatabase actorDatabase = ActorSpawnManager.Instance?.Database;
            if (actorDatabase == null)
            {
                error = "ActorDatabase가 준비되지 않아 일반 조우를 자동 생성할 수 없습니다.";
                return false;
            }

            HashSet<string> excludedActorIds = new(
                settings.excludedMonsterActorIds?
                    .Where(value => !string.IsNullOrWhiteSpace(value)) ?? Enumerable.Empty<string>(),
                StringComparer.Ordinal);
            List<CycleMonsterCandidate> monsters = actorDatabase.All
                .Where(definition => definition != null && definition.prefab != null)
                .Where(definition => (definition.actorType & ActorType.Monster) != 0)
                .Where(definition => definition.EffectiveGrade != MonsterActorGrade.Boss)
                .Where(definition => !string.IsNullOrWhiteSpace(definition.actorId))
                // 사이클 보스 어시스트와 별개인 파티 캐릭터 해금 경로를 자동 검증 조우가 건드리지 않는다.
                .Where(definition => definition.EffectiveRecruitableAs == CharacterActorType.None)
                .Where(definition => !excludedActorIds.Contains(definition.actorId))
                .Select(definition => new CycleMonsterCandidate(
                    definition.actorId,
                    ResolveDifficultyTier(definition),
                    ResolveThreatCost(definition)))
                .OrderBy(value => value.ActorId, StringComparer.Ordinal)
                .ToList();

            List<int> lootItemIds = ResolveLootItemIds();
            if (settings.lootPickupCount > 0 && lootItemIds.Count == 0)
            {
                error = "ItemDatabase에 자동 루팅 대상으로 사용할 재료/일반 아이템이 없습니다.";
                return false;
            }

            List<CycleGenerationRoutePoint> routes = new();
            if (outerPoints != null)
            {
                foreach (CycleSpawnPoint point in outerPoints)
                    if (point != null && !string.IsNullOrWhiteSpace(point.SpawnId))
                        routes.Add(new CycleGenerationRoutePoint(point.SpawnId, point.Position));
            }
            routes.Add(new CycleGenerationRoutePoint(centralPoint.SpawnId, centralPoint.Position));

            CycleWorldGenerationRequest request = new()
            {
                mapId = run.mapId,
                cycleIndex = run.cycleIndex,
                seed = run.seed,
                playerPosition = playerPoint.Position,
                routePoints = routes,
                monsterCandidates = monsters,
                lootItemIds = lootItemIds,
                settings = settings,
            };
            if (!CycleWorldGenerationPlanner.TryBuild(
                    request,
                    randomFactory(CycleRandomStream.Encounter),
                    randomFactory(CycleRandomStream.Loot),
                    randomFactory(CycleRandomStream.Interaction),
                    out CycleGeneratedContentLayout generated,
                    out error))
            {
                return false;
            }

            CycleGeneratedContentLayout staged = generated.Clone();
            if (!TryResolveGeneratedPositions(
                    staged,
                    playerPoint.Position,
                    routes,
                    routes.Select(value => value.Position).ToList(),
                    settings,
                    allowRelocation: true,
                    out error))
            {
                return false;
            }
            layout.generatedContent = staged;
            return true;
        }

        private static bool IsGeneratedContentEmpty(CycleGeneratedContentLayout generated)
        {
            return generated != null &&
                   (generated.encounters == null || generated.encounters.Count == 0) &&
                   (generated.loot == null || generated.loot.Count == 0) &&
                   (generated.interactions == null || generated.interactions.Count == 0);
        }

        private bool SpawnGeneratedContent(CycleRunState run, CycleLayoutState layout, bool restore, out string error)
        {
            error = null;
            CycleGeneratedContentLayout generated = layout.generatedContent;
            if (generated == null || !_context.Config.AutoGeneration.enabled) return true;

            foreach (CycleGeneratedEncounterPlacement encounter in generated.encounters ?? new List<CycleGeneratedEncounterPlacement>())
            {
                if (encounter == null || encounter.cleared) continue;
                GameObject root = new($"[AUTO] MonsterGroup Z{encounter.difficultyZone} {encounter.encounterId}");
                root.transform.position = encounter.anchorPosition.ToVector3();
                MonsterGroupController group = root.AddComponent<MonsterGroupController>();
                CycleEncounterRuntimeHandle handle = root.AddComponent<CycleEncounterRuntimeHandle>();
                _spawnedObjects.Add(root);
                List<MonsterActor> spawnedMembers = new();

                foreach (CycleGeneratedMonsterPlacement member in encounter.monsters ?? new List<CycleGeneratedMonsterPlacement>())
                {
                    if (member == null || string.IsNullOrWhiteSpace(member.actorId)) continue;
                    GameActor actor = ActorSpawnManager.Instance?.SpawnActor(
                        member.actorId,
                        member.position.ToVector3(),
                        Quaternion.Euler(0f, member.yaw, 0f),
                        group,
                        root.transform);
                    if (actor is not MonsterActor monster)
                    {
                        error = $"자동 조우 '{encounter.encounterId}'의 '{member.actorId}'가 MonsterActor로 생성되지 않았습니다.";
                        return false;
                    }
                    ApplyRuntimeDifficulty(monster, run, encounter.difficultyZone);
                    spawnedMembers.Add(monster);
                }

                if (!group.TryBindRuntimeMembers(spawnedMembers, out string groupError))
                {
                    error = $"자동 조우 '{encounter.encounterId}'의 MonsterGroup 적용 실패: {groupError}";
                    return false;
                }

                group.Activate();
                if (group.AliveCount != spawnedMembers.Count ||
                    group.RegisteredMemberCount != spawnedMembers.Count)
                {
                    error = $"자동 조우 '{encounter.encounterId}'의 MonsterGroup 멤버 수가 일치하지 않습니다: " +
                            $"생성 {spawnedMembers.Count}, 등록 {group.RegisteredMemberCount}, 생존 {group.AliveCount}";
                    return false;
                }

                handle.Initialize(encounter.encounterId, group);
            }

            foreach (CycleGeneratedLootPlacement loot in generated.loot ?? new List<CycleGeneratedLootPlacement>())
            {
                if (loot == null || loot.collected) continue;
                ItemSO item = ItemManager.Instance?.GetItemData(loot.itemId);
                if (item == null)
                {
                    error = $"자동 루팅 아이템 ID를 ItemDatabase에서 찾지 못했습니다: {loot.itemId}";
                    return false;
                }
                GameObject pickup = CreatePrimitiveRuntimeObject(
                    $"[AUTO] Loot {loot.lootId}",
                    PrimitiveType.Sphere,
                    loot.position.ToVector3(),
                    new Vector3(0.65f, 0.65f, 0.65f));
                pickup.AddComponent<CycleLootPickup>().Initialize(loot.lootId, item, loot.count);
                _spawnedObjects.Add(pickup);
            }

            foreach (CycleGeneratedInteractionPlacement interaction in generated.interactions ?? new List<CycleGeneratedInteractionPlacement>())
            {
                if (interaction == null || interaction.completed) continue;
                GameObject target = CreatePrimitiveRuntimeObject(
                    $"[AUTO] Interaction {interaction.interactionId}",
                    PrimitiveType.Cylinder,
                    interaction.position.ToVector3(),
                    new Vector3(0.8f, 1.2f, 0.8f));
                target.AddComponent<CycleInteractionTarget>().Initialize(interaction.interactionId);
                _spawnedObjects.Add(target);
            }

            return true;
        }

        private bool RegisterGeneratedQuest(CycleRunState run, CycleLayoutState layout, out string error)
        {
            error = null;
            CycleGeneratedContentLayout generated = layout.generatedContent;
            QuestManager quests = QuestManager.Instance;
            CycleWorldAutoGenerationSettings settings = _context.Config.AutoGeneration;
            if (generated == null || !settings.enabled || !settings.generateValidationQuest)
            {
                quests?.UnregisterRuntimeQuests("cycle:auto:");
                return true;
            }
            if (quests == null)
            {
                error = "QuestManager가 없어 자동 검증 퀘스트를 생성할 수 없습니다.";
                return false;
            }

            quests.UnregisterRuntimeQuests("cycle:auto:");
            if (!CycleGeneratedQuestAuthoringPlanner.TryBuild(
                    run,
                    layout,
                    out CycleGeneratedQuestDraft draft,
                    out error))
            {
                return false;
            }

            // 런타임 퀘스트 상태는 사이클 레이아웃에서 재구성한다. 이미 모두 끝난 저장을
            // 복원할 때 완료 이벤트와 효과음을 다시 발생시키지 않는다.
            if (draft.alreadyCompleted) return true;

            QuestSO quest = ScriptableObject.CreateInstance<QuestSO>();
            quest.hideFlags = HideFlags.HideAndDontSave;
            quest.questId = draft.questId;
            quest.questName = draft.questName;
            quest.questType = QuestType.Sub;
            quest.shortSummary = draft.shortSummary;
            quest.questDescription = draft.questDescription;
            quest.autoComplete = true;
            quest.objectives.AddRange(draft.objectives);

            if (quest.objectives.Count == 0 || !quests.RegisterRuntimeQuest(quest))
            {
                UnityEngine.Object.Destroy(quest);
                error = "자동 검증 퀘스트 등록에 실패했습니다.";
                return false;
            }

            foreach (CycleGeneratedEncounterPlacement encounter in generated.encounters ?? new List<CycleGeneratedEncounterPlacement>())
                if (encounter?.cleared == true) quests.NotifyEncounterCleared(encounter.encounterId);
            foreach (CycleBossPlacement boss in layout.outerBosses ?? new List<CycleBossPlacement>())
                if (boss?.defeated == true) quests.NotifyCycleBossDefeated(boss.spawnId);
            if (layout.centralBoss?.defeated == true) quests.NotifyCycleBossDefeated(layout.centralBoss.spawnId);
            foreach (CycleGeneratedLootPlacement loot in generated.loot ?? new List<CycleGeneratedLootPlacement>())
                if (loot?.collected == true) quests.NotifyCycleLootCollected(loot.itemId, Mathf.Max(1, loot.count));
            foreach (CycleGeneratedInteractionPlacement interaction in generated.interactions ?? new List<CycleGeneratedInteractionPlacement>())
                if (interaction?.completed == true) quests.NotifyInteractionCompleted(interaction.interactionId);
            return true;
        }

        private void ApplyRuntimeDifficulty(MonsterActor monster, CycleRunState run, int zoneBonus)
        {
            CycleDifficultyEntry difficulty = _runManager.GetCurrentDifficulty();
            float hpDifficulty = difficulty?.healthMultiplier ?? 1f;
            int runtimeLevel = Mathf.Max(
                1,
                (_context.Config?.baseMonsterLevel ?? 1) + run.cycleIndex - 1 + Mathf.Clamp(zoneBonus, 0, 2));
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
            float rewardMultiplier = difficulty != null ? Mathf.Max(0f, difficulty.rewardMultiplier) : 1f;
            monster.SetRuntimeRewards(
                (long)Math.Round(monster.BaseExpReward * rewardMultiplier),
                Mathf.RoundToInt(monster.BaseGoldReward * rewardMultiplier));
        }

        private bool PreparePlacementExclusionProxies(
            CycleWorldAutoGenerationSettings settings,
            out string error)
        {
            CleanupPlacementExclusionProxies();
            error = null;
            List<Material> rawConfigured = settings.excludedSurfaceMaterials ?? new List<Material>();
            if (rawConfigured.Any(value => value == null))
            {
                error = "배치 제외 Material 목록에 유실된 참조가 있습니다.";
                return false;
            }

            List<Material> configured = rawConfigured
                .Distinct()
                .ToList();
            if (configured.Count == 0) return true;

            HashSet<Material> materialSet = new(configured);
            int proxyLayer = LayerMask.NameToLayer("Ignore Raycast");
            if (proxyLayer < 0) proxyLayer = 2;

            MeshRenderer[] renderers = UnityEngine.Object.FindObjectsByType<MeshRenderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            foreach (MeshRenderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled || !renderer.gameObject.activeInHierarchy)
                    continue;

                bool excluded = false;
                foreach (Material material in renderer.sharedMaterials)
                {
                    if (material == null || !materialSet.Contains(material)) continue;
                    excluded = true;
                }
                if (!excluded) continue;

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null)
                {
                    error = $"배치 제외 Material을 사용하는 '{renderer.name}'에 MeshFilter/Mesh가 없습니다.";
                    CleanupPlacementExclusionProxies();
                    return false;
                }

                GameObject proxy = new($"[AUTO] Placement Exclusion {renderer.name}")
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    layer = proxyLayer,
                };
                Transform source = renderer.transform;
                proxy.transform.SetPositionAndRotation(source.position, source.rotation);
                proxy.transform.localScale = source.lossyScale;
                MeshCollider collider = proxy.AddComponent<MeshCollider>();
                collider.sharedMesh = filter.sharedMesh;
                collider.convex = false;
                _placementExclusionProxies.Add(proxy);
            }

            Physics.SyncTransforms();
            return true;
        }

        private void CleanupPlacementExclusionProxies()
        {
            foreach (GameObject proxy in _placementExclusionProxies)
            {
                if (proxy == null) continue;
                foreach (Collider collider in proxy.GetComponents<Collider>())
                    collider.enabled = false;
                UnityEngine.Object.Destroy(proxy);
            }
            _placementExclusionProxies.Clear();
        }

        private List<int> ResolveLootItemIds()
        {
            IReadOnlyList<ItemSO> allItems = ItemManager.Instance?.GetItemDB()?.AllItems;
            if (allItems == null) return new List<int>();

            HashSet<int> configured = new(_runManager.Config?.unsettledMaterialItemIds ?? new List<int>());
            List<int> configuredValid = allItems
                .Where(item => item != null && item.itemId > 0 && configured.Contains(item.itemId))
                .Select(item => item.itemId)
                .Distinct()
                .OrderBy(value => value)
                .ToList();
            if (configuredValid.Count > 0) return configuredValid;

            List<int> materials = allItems
                .Where(item => item != null && item.itemId > 0 && item.itemType == ItemType.MATERIAL)
                .Select(item => item.itemId)
                .Distinct()
                .OrderBy(value => value)
                .ToList();
            if (materials.Count > 0) return materials;

            return allItems
                .Where(item => item != null && item.itemId > 0 && item.itemType != ItemType.EQUIPMENT)
                .Select(item => item.itemId)
                .Distinct()
                .OrderBy(value => value)
                .ToList();
        }

        private static int ResolveDifficultyTier(ActorDefinitionSO definition)
        {
            return definition.EffectiveGrade switch
            {
                MonsterActorGrade.Weak => 0,
                MonsterActorGrade.Normal => definition.EffectiveLevel >= 15 ? 2 : 1,
                MonsterActorGrade.Elite => 2,
                _ => 2,
            };
        }

        private static int ResolveThreatCost(ActorDefinitionSO definition)
        {
            int gradeCost = definition.EffectiveGrade switch
            {
                MonsterActorGrade.Weak => 1,
                MonsterActorGrade.Normal => 2,
                MonsterActorGrade.Elite => 5,
                _ => 8,
            };
            return gradeCost + Mathf.Max(0, (definition.EffectiveLevel - 1) / 10);
        }

        private bool TryResolveGeneratedPositions(
            CycleGeneratedContentLayout generated,
            Vector3 playerPosition,
            IReadOnlyList<CycleGenerationRoutePoint> routePoints,
            IReadOnlyList<Vector3> bossPositions,
            CycleWorldAutoGenerationSettings settings,
            bool allowRelocation,
            out string error)
        {
            error = null;
            if (generated == null) return true;

            List<CycleGeneratedEncounterPlacement> pendingEncounters = generated.encounters?
                .Where(value => value != null && !value.cleared)
                .ToList() ?? new List<CycleGeneratedEncounterPlacement>();
            List<CycleGeneratedLootPlacement> pendingLoot = generated.loot?
                .Where(value => value != null && !value.collected)
                .ToList() ?? new List<CycleGeneratedLootPlacement>();
            List<CycleGeneratedInteractionPlacement> pendingInteractions = generated.interactions?
                .Where(value => value != null && !value.completed)
                .ToList() ?? new List<CycleGeneratedInteractionPlacement>();
            if (pendingEncounters.Count == 0 && pendingLoot.Count == 0 && pendingInteractions.Count == 0)
                return true;

            if (generated.placementValidationVersion != CycleWorldGenerationPlanner.PlacementValidationVersion)
            {
                error = $"자동 생성 배치 검증 버전이 다릅니다: 저장 {generated.placementValidationVersion}, " +
                        $"현재 {CycleWorldGenerationPlanner.PlacementValidationVersion}";
                return false;
            }

            if (!PreparePlacementExclusionProxies(settings, out error)) return false;
            try
            {
                CycleGroundPlacementResolver resolver = new(settings);
                HashSet<string> requiredRouteIds = new(
                    pendingEncounters.Select(value => value.routeId)
                        .Concat(pendingLoot.Select(value => value.routeId))
                        .Concat(pendingInteractions.Select(value => value.routeId))
                        .Where(value => !string.IsNullOrWhiteSpace(value)),
                    StringComparer.Ordinal);
                List<CycleGenerationRoutePoint> requiredRoutePoints = routePoints?
                    .Where(value => requiredRouteIds.Contains(value.Id))
                    .ToList() ?? new List<CycleGenerationRoutePoint>();
                if (!TryBuildGroundRoutes(
                        resolver,
                        playerPosition,
                        requiredRoutePoints,
                        out Dictionary<string, CycleGroundRoute> routes,
                        out error))
                {
                    return false;
                }

                if (!TryBuildKccProfiles(
                        generated,
                        out Dictionary<string, KccPlacementProfile> profiles,
                        out error))
                {
                    return false;
                }

                foreach (CycleGeneratedEncounterPlacement encounter in pendingEncounters)
                {
                    if (!TryGetRoute(routes, encounter.routeId, encounter.encounterId, out CycleGroundRoute route, out error))
                        return false;

                    Vector3 savedAnchor = encounter.anchorPosition.ToVector3();
                    Vector3 desiredAnchor = allowRelocation
                        ? ResolveRouteIntent(route, encounter.routeProgress, encounter.lateralOffset)
                        : savedAnchor;
                    List<CycleGroundMemberRequest> memberRequests = new();
                    List<CycleGeneratedMonsterPlacement> members = encounter.monsters?
                        .Where(value => value != null)
                        .ToList() ?? new List<CycleGeneratedMonsterPlacement>();
                    for (int i = 0; i < members.Count; i++)
                    {
                        CycleGeneratedMonsterPlacement member = members[i];
                        if (!profiles.TryGetValue(member.actorId ?? string.Empty, out KccPlacementProfile profile))
                        {
                            error = $"자동 조우 '{encounter.encounterId}'의 '{member.actorId}' KCC 프로필이 없습니다.";
                            return false;
                        }
                        Vector3 localOffset = allowRelocation
                            ? member.localOffset.ToVector3()
                            : member.position.ToVector3() - savedAnchor;
                        memberRequests.Add(new CycleGroundMemberRequest(
                            $"{encounter.encounterId}:member:{i}:{member.actorId}",
                            localOffset,
                            profile));
                    }

                    CycleGroundGroupRequest request = new()
                    {
                        stableId = $"{generated.generationId}|{encounter.encounterId}",
                        desiredAnchor = desiredAnchor,
                        route = route,
                        routeProgress = encounter.routeProgress,
                        members = memberRequests,
                        bossPositions = bossPositions,
                        allowRelocation = allowRelocation,
                    };
                    if (!resolver.TryResolveGroup(request, out CycleGroundGroupPlacement placement, out error))
                        return false;

                    encounter.anchorPosition = new SerializableVector3(placement.AnchorPosition);
                    for (int i = 0; i < members.Count; i++)
                    {
                        Vector3 position = placement.Members[i].Position;
                        members[i].position = new SerializableVector3(position);
                        members[i].localOffset = new SerializableVector3(position - placement.AnchorPosition);
                    }
                }

                KccPlacementProfile lootProfile = new(0.325f, 0.65f, 0.325f, settings.maxGroundSlopeAngle, settings.maxGroundStepHeight);
                foreach (CycleGeneratedLootPlacement loot in pendingLoot)
                {
                    if (!TryGetRoute(routes, loot.routeId, loot.lootId, out CycleGroundRoute route, out error))
                        return false;
                    Vector3 desired = allowRelocation
                        ? ResolveRouteIntent(route, loot.routeProgress, loot.lateralOffset)
                        : loot.position.ToVector3();
                    if (!resolver.TryResolvePoint(
                            $"{generated.generationId}|{loot.lootId}",
                            desired,
                            lootProfile,
                            route,
                            loot.routeProgress,
                            bossPositions,
                            allowRelocation,
                            out Vector3 position,
                            out error))
                    {
                        return false;
                    }
                    loot.position = new SerializableVector3(position);
                }

                KccPlacementProfile interactionProfile = new(0.8f, 2.4f, 1.2f, settings.maxGroundSlopeAngle, settings.maxGroundStepHeight);
                foreach (CycleGeneratedInteractionPlacement interaction in pendingInteractions)
                {
                    if (!TryGetRoute(routes, interaction.routeId, interaction.interactionId, out CycleGroundRoute route, out error))
                        return false;
                    Vector3 desired = allowRelocation
                        ? ResolveRouteIntent(route, interaction.routeProgress, interaction.lateralOffset)
                        : interaction.position.ToVector3();
                    if (!resolver.TryResolvePoint(
                            $"{generated.generationId}|{interaction.interactionId}",
                            desired,
                            interactionProfile,
                            route,
                            interaction.routeProgress,
                            bossPositions,
                            allowRelocation,
                            out Vector3 position,
                            out error))
                    {
                        return false;
                    }
                    interaction.position = new SerializableVector3(position);
                }

                return true;
            }
            finally
            {
                CleanupPlacementExclusionProxies();
            }
        }

        private static bool TryBuildGroundRoutes(
            CycleGroundPlacementResolver resolver,
            Vector3 startMarkerPosition,
            IReadOnlyList<CycleGenerationRoutePoint> routePoints,
            out Dictionary<string, CycleGroundRoute> routes,
            out string error)
        {
            routes = new Dictionary<string, CycleGroundRoute>(StringComparer.Ordinal);
            error = null;
            foreach (CycleGenerationRoutePoint routePoint in routePoints?
                         .Where(value => !string.IsNullOrWhiteSpace(value.Id))
                         .OrderBy(value => value.Id, StringComparer.Ordinal) ?? Enumerable.Empty<CycleGenerationRoutePoint>())
            {
                if (routes.ContainsKey(routePoint.Id))
                {
                    error = $"자동 생성 지면 경로 ID가 중복됩니다: {routePoint.Id}";
                    return false;
                }
                if (!resolver.TryBuildRoute(startMarkerPosition, routePoint.Position, out CycleGroundRoute route, out string routeError))
                {
                    error = $"지면 경로 '{routePoint.Id}' 생성 실패: {routeError}";
                    return false;
                }
                routes.Add(routePoint.Id, route);
            }

            if (routes.Count > 0) return true;
            error = "자동 생성 지면 경로 마커가 없습니다.";
            return false;
        }

        private static bool TryBuildKccProfiles(
            CycleGeneratedContentLayout generated,
            out Dictionary<string, KccPlacementProfile> profiles,
            out string error)
        {
            profiles = new Dictionary<string, KccPlacementProfile>(StringComparer.Ordinal);
            error = null;
            ActorDatabase database = ActorSpawnManager.Instance?.Database;
            if (database == null)
            {
                error = "ActorDatabase가 준비되지 않아 KCC 배치 프로필을 읽을 수 없습니다.";
                return false;
            }

            Dictionary<string, ActorDefinitionSO> definitions = database.All
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.actorId))
                .GroupBy(value => value.actorId, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            IEnumerable<string> actorIds = generated.encounters?
                .Where(value => value != null && !value.cleared)
                .SelectMany(value => value.monsters ?? new List<CycleGeneratedMonsterPlacement>())
                .Where(value => value != null && !string.IsNullOrWhiteSpace(value.actorId))
                .Select(value => value.actorId)
                .Distinct(StringComparer.Ordinal) ?? Enumerable.Empty<string>();
            foreach (string actorId in actorIds)
            {
                if (!definitions.TryGetValue(actorId, out ActorDefinitionSO definition) || definition.prefab == null)
                {
                    error = $"자동 조우 Actor '{actorId}'의 프리팹을 찾지 못했습니다.";
                    return false;
                }
                if (!KccPlacementProfile.TryCreateFromPrefab(definition.prefab, out KccPlacementProfile profile, out string profileError))
                {
                    error = $"자동 조우 Actor '{actorId}' KCC 프로필 오류: {profileError}";
                    return false;
                }
                profiles.Add(actorId, profile);
            }
            return true;
        }

        private static bool TryGetRoute(
            IReadOnlyDictionary<string, CycleGroundRoute> routes,
            string routeId,
            string placementId,
            out CycleGroundRoute route,
            out string error)
        {
            if (!string.IsNullOrWhiteSpace(routeId) && routes.TryGetValue(routeId, out route))
            {
                error = null;
                return true;
            }
            route = null;
            error = $"자동 배치 '{placementId}'의 지면 경로 '{routeId}'을 찾지 못했습니다.";
            return false;
        }

        private static Vector3 ResolveRouteIntent(CycleGroundRoute route, float progress, float lateralOffset)
        {
            float value = Mathf.Clamp01(progress);
            Vector3 center = route.Evaluate(value);
            Vector3 before = route.Evaluate(Mathf.Max(0f, value - 0.01f));
            Vector3 after = route.Evaluate(Mathf.Min(1f, value + 0.01f));
            Vector3 direction = after - before;
            direction.y = 0f;
            if (direction.sqrMagnitude <= 0.0001f) direction = Vector3.forward;
            Vector3 perpendicular = Vector3.Cross(Vector3.up, direction.normalized);
            return center + perpendicular * lateralOffset;
        }

        private static GameObject CreatePrimitiveRuntimeObject(
            string objectName,
            PrimitiveType primitiveType,
            Vector3 position,
            Vector3 scale)
        {
            GameObject result = GameObject.CreatePrimitive(primitiveType);
            result.name = objectName;
            float baseOffset = primitiveType == PrimitiveType.Cylinder ? scale.y : scale.y * 0.5f;
            result.transform.SetPositionAndRotation(position + Vector3.up * baseOffset, Quaternion.identity);
            result.transform.localScale = scale;
            int interactionLayer = LayerMask.NameToLayer("InteractableObject");
            if (interactionLayer >= 0) result.layer = interactionLayer;
            return result;
        }

        private static List<CycleSpawnPoint> SelectOuterPoints(List<CycleSpawnPoint> candidates, int count, int perSector, System.Random random)
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
            if (pool.Count == 0) { error = "보스 Actor ID 풀이 비어 있습니다."; return false; }
            foreach (string id in pool)
            {
                if (ActorSpawnManager.Instance?.Database == null || !ActorSpawnManager.Instance.Database.Contains(id)) { error = $"ActorDatabase에 보스 ID가 없습니다: {id}"; return false; }
            }
            error = null;
            return true;
        }
    }
}
