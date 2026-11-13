using System;
using UnityEditor;
using UnityEngine;
using UPlayGround.Components;
using UPlayGround.Gameplay.World.ReactiveEnvironment;
using UPlayGround.Manager.World;

namespace UPlayGround.EditorTools
{
    /// <summary>환경 테스트 복제본을 원본과 다른 ID로 같은 소유 지역에 배치한다.</summary>
    public static class ReactiveEnvironmentCheats
    {
        /// <summary>원본 프리팹은 보존하고 활성화 전에 배치 ID를 발급한다.</summary>
        public static ReactiveEnvironmentObject Spawn(GameObject prefab, Transform origin, Vector3 position,
            Quaternion rotation, out string error)
        {
            error = null;
            if (prefab == null || !PrefabUtility.IsPartOfPrefabAsset(prefab)
                || prefab.transform.parent != null || prefab.GetComponent<ReactiveEnvironmentObject>() == null
                || prefab.GetComponent<SceneEntityId>() == null)
            {
                error = "루트에 ReactiveEnvironmentObject가 있는 프리팹 에셋을 지정하세요.";
                return null;
            }
            if (origin == null || string.IsNullOrEmpty(RegionOwnership.Resolve(origin)))
            {
                error = "소유 지역이 준비된 플레이어가 필요합니다.";
                return null;
            }
            var staging = new GameObject("Environment Cheat Staging");
            staging.SetActive(false);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(staging, origin.gameObject.scene);
            GameObject clone = null;
            try
            {
                // Start/등록보다 먼저 ID를 설정해 원본이나 이전 테스트의 저장 상태를 상속하지 않는다.
                clone = UnityEngine.Object.Instantiate(prefab, position, rotation, staging.transform);
                clone.SetActive(false);
                clone.name = prefab.name + " (환경 치트)";
                clone.GetComponent<SceneEntityId>().RuntimeSetGuid(Guid.NewGuid().ToString("N"));
                clone.transform.SetParent(null, true);
                clone.SetActive(true);
                return clone.GetComponent<ReactiveEnvironmentObject>();
            }
            catch
            {
                if (clone != null) UnityEngine.Object.DestroyImmediate(clone);
                throw;
            }
            finally { UnityEngine.Object.DestroyImmediate(staging); }
        }
    }
}
