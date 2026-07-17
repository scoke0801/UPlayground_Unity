using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UPlayGround.Data.Combat;
using UPlayGround.Data.EnumType;

namespace UPlayGround.Data.Combat.EditorTools
{
    /// <summary>
    /// §5.2 입장 변형(Entry Variant) 슬롯 일괄 저작 툴.
    ///
    /// 모든 PlayerAttackDataSO의 기본 entryAttack을 깊은 복사해
    /// entryAttackVsGroggy(그로기 타깃) / entryAttackVsAirborne(공중 타깃) 슬롯을 채우고
    /// 사용 토글을 켠다. 각 무기의 자기 entryAttack을 템플릿으로 쓰므로 animKey가 항상 유효하다.
    ///
    /// 안전장치:
    /// - 해당 변형 토글이 이미 켜져 있으면 건너뛴다(사용자 튜닝 보존, 재실행 멱등).
    /// - entryAttack.baseInfo가 없으면 건너뛴다.
    ///
    /// ⚠️ 시각적 "모션 다양화"는 변형 슬롯의 animKey를 각 캐릭터 MotionSet에 존재하는
    ///    별도 클립으로 교체해야 완성된다(여기선 안전하게 entryAttack 모션을 재사용한다).
    /// </summary>
    public static class EntryVariantSetupTool
    {
        private const float GroggyDamageMultiplier = 1.5f; // 그로기 타깃 punish 데미지 배수(데모 기본값)

        [MenuItem("UPlayGround/게임플레이/전투/입장 변형 슬롯 채우기 (모든 무기)",
            priority = UPlayGround.Tool.Editor.UPlaygroundMenuPriority.GameplayCombat + 22)]
        public static void PopulateEntryVariants()
        {
            string[] guids = AssetDatabase.FindAssets("t:PlayerAttackDataSO");
            int populated = 0, skipped = 0;
            var touched = new List<string>();

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<PlayerAttackDataSO>(path);
                if (so == null) continue;

                if (so.entryAttack?.baseInfo == null)
                {
                    skipped++;
                    continue;
                }

                bool changed = false;

                if (!so.useEntryAttackVsGroggy)
                {
                    so.entryAttackVsGroggy = MakeVariant(so.entryAttack, GroggyDamageMultiplier, AttackReactionType.Heavy);
                    so.useEntryAttackVsGroggy = true;
                    changed = true;
                }

                if (!so.useEntryAttackVsAirborne)
                {
                    so.entryAttackVsAirborne = MakeVariant(so.entryAttack, 1f, AttackReactionType.Airborne);
                    so.useEntryAttackVsAirborne = true;
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(so);
                    populated++;
                    touched.Add(System.IO.Path.GetFileNameWithoutExtension(path));
                }
                else
                {
                    skipped++;
                }
            }

            if (populated > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }

            Debug.Log($"[EntryVariantSetup] 입장 변형 슬롯 저작 완료 — 적용 {populated}개, 건너뜀 {skipped}개(이미 설정/빈 entryAttack).\n" +
                      $"적용: {string.Join(", ", touched)}\n" +
                      "※ 변형은 entryAttack 모션을 재사용합니다. 시각적 다양화가 필요하면 각 변형의 animKey를 " +
                      "캐릭터 MotionSet에 존재하는 클립으로 교체하세요(예: 공중 변형 → 런치/추격 모션).");
        }

        /// <summary>entryAttack을 깊은 복사(JsonUtility)해 데미지 배수/리액션을 조정한 변형을 만든다.</summary>
        private static PlayerAttackInfo MakeVariant(PlayerAttackInfo source, float damageMul, AttackReactionType reaction)
        {
            var copy = JsonUtility.FromJson<PlayerAttackInfo>(JsonUtility.ToJson(source));
            if (copy?.baseInfo?.hitPhases != null)
            {
                foreach (var phase in copy.baseInfo.hitPhases)
                {
                    if (phase == null) continue;
                    phase.damage = Mathf.Round(phase.damage * damageMul);
                    phase.reactionType = reaction;
                }
            }
            return copy;
        }
    }
}
