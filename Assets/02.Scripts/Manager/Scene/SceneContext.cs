using System.Collections;
using UnityEngine;
using UPlayGround.Manager;

namespace UPlayGround
{
    /// <summary>
    /// 씬마다 배치하는 씬 컨텍스트 베이스 클래스.
    /// Boot 씬을 거치지 않고 단독 실행해도 GameManager가 자동 초기화된다.
    /// </summary>
    public class SceneContext : MonoBehaviour
    {
        public string SceneType;

        [Tooltip("미니맵·전체맵 Config를 조회할 맵 식별자 (MapConfigDatabaseSO의 mapId와 일치해야 함)")]
        public string MapID;

        private IEnumerator Start()
        {
            EnsureGameManagerInitialized();

            yield return new WaitUntil(() => GameManager.Instance.IsInitialized);

            SceneManager.Instance.OnSceneContextReady(this);
        }

        /// <summary>
        /// GameManager가 없으면 생성하고 초기화한다.
        /// DontDestroyOnLoad 포함 전체 씬에서 탐색.
        /// </summary>
        private void EnsureGameManagerInitialized()
        {
            var gm = GameManager.Instance; 
            // 없으면 BaseManager가 자동 생성
            // Instance 접근만으로 생성 + Awake(→ InitializeManagers) 까지 실행됨
        }
    }
}
