using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Actor;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Tool.EditorTools
{
    /// <summary>
    /// 몬스터 재스폰 데이터 검증기.
    /// 재스폰 대상(보스/합류 몬스터 제외 몬스터 정의)의 스케일링/보상 데이터 누락을 점검한다.
    ///
    /// - 재스폰 대상인데 monsterScaling이 없으면 error (레벨 스케일링 불가 → 레벨 표기만 변경됨)
    /// - expReward / goldReward 음수는 error
    /// - 재스폰 대상인데 보상이 모두 0이면 warning (성장 보상 루프에서 제외됨)
    /// </summary>
    public static class MonsterRespawnDataValidator
    {
        [MenuItem("UPlayGround/월드/몬스터 재스폰 데이터 검증")]
        public static void Validate()
        {
            string[] guids = AssetDatabase.FindAssets("t:ActorDefinitionSO");
            int errors = 0, warnings = 0, candidates = 0;

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>(path);
                if (definition == null) continue;
                if ((definition.actorType & ActorType.Monster) == 0) continue;

                if (definition.expReward < 0)
                {
                    Debug.LogError($"[재스폰 검증] {definition.name}: expReward가 음수입니다 ({definition.expReward})", definition);
                    errors++;
                }

                if (definition.goldReward < 0)
                {
                    Debug.LogError($"[재스폰 검증] {definition.name}: goldReward가 음수입니다 ({definition.goldReward})", definition);
                    errors++;
                }

                // 재스폰 대상: 보스가 아니고 합류 몬스터도 아닌 몬스터
                bool isRespawnCandidate = definition.grade != MonsterActorGrade.Boss
                                          && definition.recruitableAs == CharacterActorType.None;
                if (!isRespawnCandidate) continue;
                candidates++;

                if (definition.monsterScaling == null)
                {
                    Debug.LogError(
                        $"[재스폰 검증] {definition.name}: 재스폰 대상인데 monsterScaling이 없습니다. " +
                        "레벨 스케일링이 적용되지 않습니다(레벨 표기만 변경).", definition);
                    errors++;
                }

                if (definition.expReward == 0 && definition.goldReward == 0)
                {
                    Debug.LogWarning(
                        $"[재스폰 검증] {definition.name}: 재스폰 대상인데 경험치/골드 보상이 모두 0입니다.", definition);
                    warnings++;
                }
            }

            Debug.Log($"[재스폰 검증] 완료 — 재스폰 후보 {candidates}개 / 오류 {errors}건 / 경고 {warnings}건");
        }
    }
}
