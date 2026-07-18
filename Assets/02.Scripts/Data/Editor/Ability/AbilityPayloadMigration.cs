using UnityEditor;
using UnityEngine;
using UPlayGround.Ability.UPlayGround;
using UPlayGround.Data.Ability;

namespace UPlayGround.Data.Editor.Ability
{
    /// <summary>
    /// Variant 내부 V1 공격 데이터를 프로젝트 전용 실행 Payload로 비파괴 복사한다.
    /// 원본 필드는 역호환과 롤백을 위해 자동 삭제하지 않는다.
    /// </summary>
    public static class AbilityPayloadMigration
    {
        public static int ConvertLegacyVariants(GameplayAbilitySO ability)
        {
            if (ability == null || ability.variants == null) return 0;

            string assetPath = AssetDatabase.GetAssetPath(ability);
            if (string.IsNullOrWhiteSpace(assetPath)) return 0;

            int converted = 0;
            Undo.RegisterCompleteObjectUndo(ability, "Ability 실행 Payload 변환");
            for (int i = 0; i < ability.variants.Count; i++)
            {
                AbilityVariantDefinition variant = ability.variants[i];
                if (variant == null
                    || variant.executionPayload != null
                    || !variant.HasLegacyExecutionData)
                    continue;

                var payload = ScriptableObject.CreateInstance<UPlayGroundMotionAbilityPayloadSO>();
                payload.name = $"{ability.name}_{Sanitize(variant.variantId)}_Payload";
                payload.executionId =
                    $"{ability.abilityId}.{variant.variantId}".Trim('.');
                payload.animKey = variant.animKey;
                payload.playerAttackInfo = CloneAttackInfo(variant);
                AssetDatabase.AddObjectToAsset(payload, ability);
                Undo.RegisterCreatedObjectUndo(payload, "Ability 실행 Payload 생성");
                variant.executionPayload = payload;
                EditorUtility.SetDirty(payload);
                converted++;
            }

            if (converted <= 0) return 0;
            EditorUtility.SetDirty(ability);
            AssetDatabase.ImportAsset(assetPath);
            AssetDatabase.SaveAssets();
            return converted;
        }

        private static global::UPlayGround.Data.PlayerAttackInfo CloneAttackInfo(
            AbilityVariantDefinition variant)
        {
            var payload = ScriptableObject.CreateInstance<UPlayGroundMotionAbilityPayloadSO>();
            try
            {
                EditorJsonUtility.FromJsonOverwrite(
                    EditorJsonUtility.ToJson(variant),
                    payload);
                return payload.playerAttackInfo;
            }
            finally
            {
                Object.DestroyImmediate(payload);
            }
        }

        private static string Sanitize(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? "Variant"
                : value.Replace('/', '_').Replace('\\', '_').Trim();
    }
}
