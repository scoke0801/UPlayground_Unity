using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPlayGround.Manager.World
{
    /// <summary>논리 플레이어 위치와 독립된 씬의 저장 소유권을 조회한다.</summary>
    public static class RegionOwnership
    {
        private static readonly Dictionary<int, string> Maps = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Maps.Clear();

        /// <summary>씬이 준비되는 동안에도 생성·복원 대상의 원래 mapId를 유지한다.</summary>
        public static void Register(Scene scene, string mapId) => Maps[scene.handle] = mapId;

        /// <summary>언로드가 끝난 씬의 저장 소유권을 해제한다.</summary>
        public static void Unregister(Scene scene) => Maps.Remove(scene.handle);

        /// <summary>개체의 소유 씬을 기준으로 조회하며 현재 플레이어 지역으로 대체하지 않는다.</summary>
        public static string Resolve(Component owner) => owner != null ? Resolve(owner.gameObject.scene) : null;

        /// <summary>기존 단일 씬은 SceneContext를 통해 같은 소유권 계약을 따른다.</summary>
        public static string Resolve(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;
            if (Maps.TryGetValue(scene.handle, out string mapId)) return mapId;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                SceneContext context = root.GetComponentInChildren<SceneContext>(true);
                if (context != null) return context.MapID;
            }
            return null;
        }
    }
}
