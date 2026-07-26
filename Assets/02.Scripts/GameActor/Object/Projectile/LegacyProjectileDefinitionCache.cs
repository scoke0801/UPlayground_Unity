using System.Collections.Generic;
using UnityEngine;
using UPlayGround.Data.Projectile;

namespace UPlayGround
{
    /// <summary>
    /// 기존 투사체 프리팹을 에셋 재직렬화 없이 새 풀링 런타임으로 연결한다.
    /// 캐시된 Definition은 저장되지 않으며 프리팹 하나당 한 번만 생성된다.
    /// </summary>
    public static class LegacyProjectileDefinitionCache
    {
        private static readonly Dictionary<BaseProjectile, ProjectileDefinitionSO> Definitions = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset()
        {
            Definitions.Clear();
        }

        public static ProjectileDefinitionSO GetOrCreate(BaseProjectile prefab)
        {
            if (prefab == null)
                return null;
            if (Definitions.TryGetValue(prefab, out ProjectileDefinitionSO definition)
                && definition != null)
                return definition;

            definition = prefab.CreateCompatibilityDefinition();
            Definitions[prefab] = definition;
            return definition;
        }
    }
}
