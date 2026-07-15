#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UPlayGround.Components;
using UPlayGround.Data.Cycle;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Cycle.Editor
{
    public static class CycleP0Validator
    {
        [MenuItem("UPlayGround/사이클/P0 현재 씬 검증")]
        public static void ValidateCurrentScene()
        {
            int errors = 0;
            int warnings = 0;
            CycleSpawnPoint[] points = Object.FindObjectsByType<CycleSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (IGrouping<string, CycleSpawnPoint> group in points.GroupBy(p => p.SpawnId))
            {
                if (string.IsNullOrWhiteSpace(group.Key)) { Debug.LogError("[CycleValidator] spawnId가 빈 CycleSpawnPoint가 있습니다."); errors++; }
                else if (group.Count() > 1) { Debug.LogError($"[CycleValidator] 중복 spawnId: {group.Key}"); errors++; }
            }
            if (!points.Any(point => point.Allows(CycleSpawnRole.Player)))
            { Debug.LogError("[CycleValidator] Player 역할 CycleSpawnPoint가 없습니다."); errors++; }
            if (points.Count(point => point.Allows(CycleSpawnRole.OuterBoss)) < 3)
            { Debug.LogError("[CycleValidator] OuterBoss 후보는 최소 3개가 필요합니다."); errors++; }
            const CycleSpawnRole validRoles = CycleSpawnRole.Player | CycleSpawnRole.OuterBoss | CycleSpawnRole.Respawn;
            foreach (CycleSpawnPoint point in points.Where(value => (value.AllowedRoles & ~validRoles) != 0))
            { Debug.LogError($"[CycleValidator] 정의되지 않은 역할 비트가 있습니다. Everything 대신 필요한 역할만 선택하세요: {point.name} ({(int)point.AllowedRoles})", point); errors++; }
            foreach (CycleSpawnPoint point in points.Where(value => value.Allows(CycleSpawnRole.OuterBoss) && string.IsNullOrWhiteSpace(value.SectorId)))
            { Debug.LogError($"[CycleValidator] 외곽 보스 후보의 Sector ID가 비어 있습니다: {point.name}", point); errors++; }

            CentralBossSpawnPoint[] centralPoints = Object.FindObjectsByType<CentralBossSpawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            int centralCount = centralPoints.Length;
            if (centralCount != 1) { Debug.LogError($"[CycleValidator] CentralBossSpawnPoint는 정확히 하나여야 합니다: {centralCount}"); errors++; }
            else if (string.IsNullOrWhiteSpace(centralPoints[0].SpawnId))
            { Debug.LogError("[CycleValidator] CentralBossSpawnPoint의 spawnId가 비어 있습니다.", centralPoints[0]); errors++; }
            else if (centralPoints[0].TryGetComponent(out CycleSpawnPoint centralAsRegular) && centralAsRegular.Allows(CycleSpawnRole.OuterBoss))
            { Debug.LogError("[CycleValidator] 중앙 보스 스폰 오브젝트가 OuterBoss 후보로도 설정되어 있습니다. CycleSpawnPoint를 제거하거나 OuterBoss 역할을 해제하세요.", centralPoints[0]); errors++; }

            ValidateRespawnPoints(points, ref errors);
            ValidateWorldContext(ref errors);
            ValidateSpawnGeometry(points, ref errors);
            ValidateExitPortal(ref errors);
            ValidateAssistBootstrap(ref errors);
            ValidateInputActions(ref errors, ref warnings);

            CharacterModelData[] models = Object.FindObjectsByType<CharacterModelData>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (CharacterModelData model in models)
            {
                if (model.weightProfile == null)
                {
                    if (IsP0WeightTarget(model.characterType))
                    { Debug.LogError($"[CycleValidator] P0 대상 {model.characterType} weightProfile 누락", model); errors++; }
                    else
                    { Debug.LogWarning($"[CycleValidator] {model.characterType} weightProfile 미정의 — P0 명세에 수치가 없어 수동 분류가 필요합니다.", model); warnings++; }
                    continue;
                }
                if (!model.weightProfile.Validate(out string profileError)) { Debug.LogError($"[CycleValidator] {model.characterType} 프로필 오류: {profileError}", model.weightProfile); errors++; }
                if (model.weightProfile.weightClass == CharacterWeightClass.Heavy && model.weightProfile.recoveryPolicy == null) { Debug.LogError($"[CycleValidator] 중량 {model.characterType} 회복 정책 누락", model); errors++; }
                if (model.weightProfile.weightClass == CharacterWeightClass.Light && model.weightProfile.recoveryPolicy == null) { Debug.LogError($"[CycleValidator] 경량 {model.characterType} 회복 정책 누락", model); errors++; }
            }
            if (errors == 0) Debug.Log($"[CycleValidator] P0 현재 씬 검증 통과 — 스폰 {points.Length}개, 캐릭터 모델 {models.Length}개, 경고 {warnings}개");
            else Debug.LogError($"[CycleValidator] P0 현재 씬 검증 실패: 오류 {errors}개, 경고 {warnings}개");
        }

        private static bool IsP0WeightTarget(CharacterActorType characterType)
        {
            return characterType is CharacterActorType.Honoka or CharacterActorType.Bokusei or CharacterActorType.H09;
        }

        private static void ValidateRespawnPoints(CycleSpawnPoint[] points, ref int errors)
        {
            CycleRespawnPoint[] respawns = Object.FindObjectsByType<CycleRespawnPoint>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (CycleRespawnPoint respawn in respawns)
            {
                if (string.IsNullOrWhiteSpace(respawn.RespawnId))
                { Debug.LogError($"[CycleValidator] Respawn ID가 비어 있습니다: {respawn.name}", respawn); errors++; continue; }
                CycleSpawnPoint sameObject = respawn.GetComponent<CycleSpawnPoint>();
                if (sameObject == null || !sameObject.Allows(CycleSpawnRole.Respawn) || sameObject.SpawnId != respawn.RespawnId)
                { Debug.LogError($"[CycleValidator] 같은 오브젝트의 Respawn Spawn ID/역할이 일치하지 않습니다: {respawn.name}", respawn); errors++; }
            }

            foreach (CycleSpawnPoint point in points.Where(value => value.Allows(CycleSpawnRole.Respawn)))
            {
                CycleRespawnPoint respawn = point.GetComponent<CycleRespawnPoint>();
                if (respawn == null || respawn.RespawnId != point.SpawnId)
                { Debug.LogError($"[CycleValidator] Respawn 역할 SpawnPoint에 일치하는 CycleRespawnPoint가 없습니다: {point.name}", point); errors++; }
            }
        }

        private static void ValidateWorldContext(ref int errors)
        {
            CycleWorldContext[] contexts = Object.FindObjectsByType<CycleWorldContext>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (contexts.Length != 1)
            { Debug.LogError($"[CycleValidator] CycleWorldContext는 정확히 하나여야 합니다: {contexts.Length}"); errors++; return; }

            CycleWorldContext context = contexts[0];
            SerializedObject serialized = new(context);
            CycleConfigSO runConfig = serialized.FindProperty("_runConfig").objectReferenceValue as CycleConfigSO;
            RemainsActor remainsPrefab = serialized.FindProperty("_remainsPrefab").objectReferenceValue as RemainsActor;
            if (context.Config == null) { Debug.LogError("[CycleValidator] CycleWorldContext.Config 누락", context); errors++; }
            else if (!context.Config.Validate(out string worldError)) { Debug.LogError($"[CycleValidator] CycleWorldContext 설정 오류: {worldError}", context.Config); errors++; }
            if (runConfig == null) { Debug.LogError("[CycleValidator] CycleWorldContext.Run Config 누락", context); errors++; }
            else if (!runConfig.ValidateP0(out string runError)) { Debug.LogError($"[CycleValidator] 공통 설정 오류: {runError}", runConfig); errors++; }
            if (remainsPrefab == null) { Debug.LogError("[CycleValidator] CycleWorldContext.Remains Prefab 누락", context); errors++; }

            SceneContext sceneContext = Object.FindFirstObjectByType<SceneContext>(FindObjectsInactive.Include);
            if (sceneContext == null || string.IsNullOrWhiteSpace(sceneContext.MapID))
            { Debug.LogError("[CycleValidator] SceneContext 또는 MapID 누락"); errors++; }
            else if (context.Config != null && sceneContext.MapID != context.Config.mapId)
            { Debug.LogError($"[CycleValidator] MapID 불일치: SceneContext={sceneContext.MapID}, CycleWorldConfig={context.Config.mapId}", context.Config); errors++; }
        }

        private static void ValidateSpawnGeometry(CycleSpawnPoint[] points, ref int errors)
        {
            CycleWorldContext context = Object.FindFirstObjectByType<CycleWorldContext>(FindObjectsInactive.Include);
            CycleWorldConfigSO config = context != null ? context.Config : null;
            if (config == null) return;

            CycleSpawnPoint[] outerPoints = points
                .Where(value => value != null && value.Allows(CycleSpawnRole.OuterBoss) && !string.IsNullOrWhiteSpace(value.SpawnId))
                .ToArray();
            foreach (CycleSpawnPoint player in points.Where(value => value != null && value.Allows(CycleSpawnRole.Player)))
            {
                CycleSpawnPoint[] safeCandidates = outerPoints
                    .Where(value => value.SpawnId != player.SpawnId)
                    .Where(value => (value.Position - player.Position).sqrMagnitude >= player.SafetyRadius * player.SafetyRadius)
                    .ToArray();
                int sectorCapacity = safeCandidates
                    .GroupBy(value => value.SectorId)
                    .Sum(group => Mathf.Min(group.Count(), config.maxSameSectorBossCount));
                if (safeCandidates.Length >= config.outerBossCount && sectorCapacity >= config.outerBossCount) continue;

                float nearestDistance = outerPoints
                    .Where(value => value.SpawnId != player.SpawnId)
                    .Select(value => Vector3.Distance(value.Position, player.Position))
                    .DefaultIfEmpty(0f)
                    .Min();
                Debug.LogError(
                    $"[CycleValidator] 플레이어 스폰 '{player.SpawnId}'에서 외곽 보스 {config.outerBossCount}개를 배치할 수 없습니다. " +
                    $"안전 반경 통과={safeCandidates.Length}, 섹터 수용량={sectorCapacity}, " +
                    $"Safety Radius={player.SafetyRadius:0.##}, 가장 가까운 후보 거리={nearestDistance:0.##}",
                    player);
                errors++;
            }
        }

        private static void ValidateExitPortal(ref int errors)
        {
            PortalActor[] portals = Object.FindObjectsByType<PortalActor>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(value => value.IsCycleExitPortal).ToArray();
            if (portals.Length == 0)
            { Debug.LogError("[CycleValidator] 사이클 탈출 PortalActor가 없습니다."); errors++; }
            foreach (PortalActor portal in portals)
            {
                if (portal.Type == PortalType.SceneTransition && string.IsNullOrWhiteSpace(portal.TargetSceneName))
                { Debug.LogError($"[CycleValidator] 탈출 포털 목표 씬이 비어 있습니다: {portal.name}", portal); errors++; }
            }
        }

        private static void ValidateAssistBootstrap(ref int errors)
        {
            BossAssistBootstrap[] bootstraps = Object.FindObjectsByType<BossAssistBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (bootstraps.Length != 1)
            { Debug.LogError($"[CycleValidator] BossAssistBootstrap은 정확히 하나여야 합니다: {bootstraps.Length}"); errors++; return; }
            SerializedObject serialized = new(bootstraps[0]);
            BossAssistDatabaseSO database = serialized.FindProperty("_database").objectReferenceValue as BossAssistDatabaseSO;
            if (database == null)
            { Debug.LogError("[CycleValidator] BossAssistBootstrap DB 누락", bootstraps[0]); errors++; return; }
            List<string> ids = database.definitions.Where(value => value != null).Select(value => value.assistId).ToList();
            if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != ids.Count)
            { Debug.LogError("[CycleValidator] BossAssist DB의 Assist ID가 비었거나 중복됩니다.", database); errors++; }
        }

        private static void ValidateInputActions(ref int errors, ref int warnings)
        {
            const string path = "Assets/Resources/Input/PlayerInputActions.inputactions";
            InputActionAsset asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(path);
            InputAction action = asset?.FindActionMap("PlayerAction", false)?.FindAction("BossAssist", false);
            if (action == null)
            { Debug.LogError("[CycleValidator] PlayerAction.BossAssist 액션이 없습니다.", asset); errors++; return; }
            if (action.bindings.Count == 0)
            { Debug.LogWarning("[CycleValidator] BossAssist 액션에 바인딩이 없습니다.", asset); warnings++; }
            if (!action.bindings.Any(value => value.path.StartsWith("<Keyboard>") || value.path.StartsWith("<Mouse>")))
            { Debug.LogWarning("[CycleValidator] BossAssist 키보드/마우스 바인딩이 없습니다.", asset); warnings++; }
            if (!action.bindings.Any(value => value.path.StartsWith("<Gamepad>")))
            { Debug.LogWarning("[CycleValidator] BossAssist 게임패드 바인딩이 없습니다.", asset); warnings++; }
        }
    }
}
#endif
