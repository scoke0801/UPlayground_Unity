using System;
using UnityEngine;

namespace UPlayGround.Components
{
    /// <summary>
    /// 기존 씬의 MonoScript GUID를 보존하기 위한 무권위 마이그레이션 셸.
    /// 신규 런타임 상태와 API를 소유하지 않으며 제거 가능한 씬을 정리한 뒤 삭제한다.
    /// </summary>
    [Obsolete("AbilitySystemComponent가 Attribute 단일 권위입니다.")]
    [DisallowMultipleComponent]
    public sealed class LegacyActorStatMigrationFacade : MonoBehaviour
    {
    }
}
