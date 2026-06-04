#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;
using UPlayGround.Data.Stat;

namespace UPlayGround.Tool.Editor.Validation
{
    public static class ActorDataValidator
    {
        public static List<EditorValidationIssue> ValidateAll()
        {
            var issues = new List<EditorValidationIssue>();
            var actorIds = new Dictionary<string, ActorDefinitionSO>();

            foreach (string guid in AssetDatabase.FindAssets("t:ActorDefinitionSO"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                if (definition == null)
                    continue;

                ValidateDefinition(issues, actorIds, definition, path);
            }

            ValidateActorDatabases(issues, actorIds);
            return issues;
        }

        private static void ValidateDefinition(
            List<EditorValidationIssue> issues,
            Dictionary<string, ActorDefinitionSO> actorIds,
            ActorDefinitionSO definition,
            string path)
        {
            if (string.IsNullOrWhiteSpace(definition.actorId))
            {
                Add(issues, EditorValidationSeverity.Error, path, definition, "actorId",
                    "actorId가 비어 있습니다.",
                    "actorId는 런타임 스폰/DB 조회 키이므로 에셋 이름과 무관하게 명시해야 합니다.");
            }
            else if (actorIds.TryGetValue(definition.actorId, out ActorDefinitionSO existing))
            {
                Add(issues, EditorValidationSeverity.Error, path, definition, "actorId",
                    $"actorId가 중복됩니다: {definition.actorId}",
                    $"이미 사용하는 에셋: {AssetDatabase.GetAssetPath(existing)}");
            }
            else
            {
                actorIds.Add(definition.actorId, definition);
            }

            if (string.IsNullOrWhiteSpace(definition.displayName))
            {
                Add(issues, EditorValidationSeverity.Warning, path, definition, "displayName",
                    "표시 이름이 비어 있습니다.",
                    "에디터/디버그 UI에서 식별하기 어렵습니다.");
            }

            if (definition.prefab == null)
            {
                Add(issues, EditorValidationSeverity.Error, path, definition, "prefab",
                    "스폰 프리팹이 비어 있습니다.",
                    "런타임 스폰 대상 ActorDefinition이면 GameActor 프리팹을 연결하세요.");
            }
            else if (definition.prefab.GetComponent<GameActor>() == null)
            {
                Add(issues, EditorValidationSeverity.Error, path, definition, "prefab",
                    "프리팹 루트에 GameActor 컴포넌트가 없습니다.",
                    "ActorDatabaseEditor의 프리팹 ID 동기화도 루트 GameActor를 기준으로 동작합니다.");
            }

            bool isMonster = (definition.actorType & ActorType.Monster) == ActorType.Monster;
            bool isPlayer = (definition.actorType & ActorType.Player) == ActorType.Player;
            bool isNpc = (definition.actorType & ActorType.NPC) == ActorType.NPC;
            bool isPlayable = definition.characterType != CharacterActorType.None;

            if (isPlayable && !isPlayer && !isMonster)
            {
                Add(issues, EditorValidationSeverity.Warning, path, definition, "characterType",
                    "characterType은 설정됐지만 ActorType이 Player/Monster가 아닙니다.",
                    "플레이어블 확장 또는 영입 대상이라면 ActorType 플래그 의도를 확인하세요.");
            }

            if (definition.statData == null)
            {
                Add(issues, EditorValidationSeverity.Error, path, definition, "statData",
                    "statData가 비어 있습니다.",
                    "Stat Data Generator에서 누락 스탯을 생성/연결하세요.");
            }
            else
            {
                ValidateStatCoverage(issues, definition, path, definition.statData);
            }

            if (isMonster)
            {
                if (definition.attackData == null)
                {
                    Add(issues, EditorValidationSeverity.Warning, path, definition, "attackData",
                        "몬스터 attackData가 비어 있습니다.",
                        "프리팹 EnemyCombat 폴백을 의도한 경우가 아니면 EnemyAttackDataSO를 연결하세요.");
                }

                if (definition.behaviorData == null)
                {
                    Add(issues, EditorValidationSeverity.Warning, path, definition, "behaviorData",
                        "몬스터 behaviorData가 비어 있습니다.",
                        "프리팹 AI 폴백을 의도한 경우가 아니면 EnemyBehaviorSO를 연결하세요.");
                }

                if (definition.recruitableAs != CharacterActorType.None && definition.characterType == CharacterActorType.None)
                {
                    Add(issues, EditorValidationSeverity.Info, path, definition, "recruitableAs",
                        "처치 합류 대상이지만 characterType은 None입니다.",
                        "몬스터 자체 타입과 합류 캐릭터 타입을 분리하려는 의도인지 확인하세요.");
                }
            }

            if (isNpc && definition.npcData == null)
            {
                Add(issues, EditorValidationSeverity.Warning, path, definition, "npcData",
                    "NPC ActorType인데 npcData가 비어 있습니다.",
                    "상호작용/대화 대상이면 NpcActorSO를 연결하세요.");
            }
        }

        private static void ValidateStatCoverage(
            List<EditorValidationIssue> issues,
            ActorDefinitionSO definition,
            string actorPath,
            ActorStatSO stat)
        {
            foreach (StatType type in System.Enum.GetValues(typeof(StatType)))
            {
                if (stat.TryGetExplicit(type, out _))
                    continue;

                Add(issues, EditorValidationSeverity.Warning, actorPath, definition, $"statData.{type}",
                    $"statData에 {type} 항목이 명시되어 있지 않습니다.",
                    $"{stat.name}은 기본값 폴백으로 동작하지만, 밸런스/검증 툴 기준에서는 명시값을 권장합니다.");
            }
        }

        private static void ValidateActorDatabases(
            List<EditorValidationIssue> issues,
            Dictionary<string, ActorDefinitionSO> allDefinitionsById)
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ActorDatabase"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var database = AssetDatabase.LoadAssetAtPath<ActorDatabase>(path);
                if (database == null)
                    continue;

                var registered = new HashSet<ActorDefinitionSO>();
                var ids = new HashSet<string>();

                foreach (ActorDefinitionSO definition in database.All)
                {
                    if (definition == null)
                    {
                        Add(issues, EditorValidationSeverity.Warning, path, database, "_actors",
                            "ActorDatabase에 Missing 항목이 있습니다.",
                            "Actor Database Editor의 Missing 정리를 실행하세요.");
                        continue;
                    }

                    registered.Add(definition);
                    if (!string.IsNullOrWhiteSpace(definition.actorId) && !ids.Add(definition.actorId))
                    {
                        Add(issues, EditorValidationSeverity.Error, path, database, "_actors",
                            $"ActorDatabase 내부 actorId가 중복됩니다: {definition.actorId}",
                            "중복 ActorDefinition을 제거하거나 actorId를 수정하세요.");
                    }
                }

                foreach (ActorDefinitionSO definition in allDefinitionsById.Values)
                {
                    if (registered.Contains(definition))
                        continue;

                    Add(issues, EditorValidationSeverity.Warning, path, database, "_actors",
                        $"ActorDefinitionSO가 ActorDatabase에 등록되어 있지 않습니다: {definition.actorId}",
                        $"등록 누락 에셋: {AssetDatabase.GetAssetPath(definition)}");
                }
            }
        }

        private static void Add(
            List<EditorValidationIssue> issues,
            EditorValidationSeverity severity,
            string path,
            UnityEngine.Object asset,
            string field,
            string message,
            string fixHint)
        {
            issues.Add(new EditorValidationIssue(severity, "Actor", path, asset, field, message, fixHint));
        }
    }
}
#endif
