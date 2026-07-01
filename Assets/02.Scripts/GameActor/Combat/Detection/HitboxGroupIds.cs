using System;
using System.Collections.Generic;

namespace UPlayGround.Combat
{
    /// <summary>
    /// HitBox 그룹 ID 목록 정규화 공용 유틸.
    /// 런타임(Player/Enemy/Residual Combat)과 이벤트/에디터 경로가 동일한 규칙
    /// (Trim / 공백 제거 / 대소문자 무시 중복 제거)을 공유하도록 한 곳에 모은다.
    /// </summary>
    public static class HitboxGroupIds
    {
        /// <summary>
        /// primaryGroupId를 선두로 additionalGroupIds를 이어 붙여 정규화한 목록을 만든다.
        /// 유효한 그룹이 하나도 없으면 null을 반환한다(단일/기본 그룹 경로로 폴백 유도).
        /// </summary>
        public static List<string> Normalize(string primaryGroupId, IReadOnlyList<string> additionalGroupIds)
        {
            if (additionalGroupIds == null || additionalGroupIds.Count == 0)
                return null;

            var result = new List<string>(additionalGroupIds.Count + 1);
            TryAddUnique(result, primaryGroupId);
            for (int i = 0; i < additionalGroupIds.Count; i++)
                TryAddUnique(result, additionalGroupIds[i]);

            return result.Count > 0 ? result : null;
        }

        /// <summary>실패 로그용 라벨. 목록이 있으면 콤마 결합, 없으면 단일 그룹/Default.</summary>
        public static string Describe(string groupId, IReadOnlyList<string> groupIds)
            => groupIds != null && groupIds.Count > 0
                ? string.Join(", ", groupIds)
                : groupId ?? CombatHitbox.DefaultGroupId;

        private static void TryAddUnique(List<string> list, string groupId)
        {
            if (string.IsNullOrWhiteSpace(groupId))
                return;

            groupId = groupId.Trim();
            for (int j = 0; j < list.Count; j++)
            {
                if (string.Equals(list[j], groupId, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            list.Add(groupId);
        }
    }
}
