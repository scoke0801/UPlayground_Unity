using System.Collections;
using UnityEngine;
using UPlayGround.Data.Path;
using UPlayGround.Enum;

namespace UPlayGround.Manager
{
    public partial class SceneManager : BaseManager<SceneManager>, IManager
    {
        private string _currentSceneType;
        private string _currentMapID;

        public string CurrentSceneType => _currentSceneType;
        public string CurrentMapID     => _currentMapID;

        public void Init() { }

        public void AfterInit() { }

        public void Dispose() { }

        public void OnUpdate() { }

        public void OnFixedUpdate() { }

        public void OnLateUpdate() { }

        public void OnSceneChanged(string sceneType) { }

        /// <summary>
        /// 씬에 배치된 SceneContext가 Start()에서 호출한다.
        /// </summary>
        public void OnSceneContextReady(SceneContext context)
        {
            _currentMapID = context.MapID;
            ChangeSceneType(context.SceneType);
        }

        private void ChangeSceneType(string sceneType)
        {
            if (UIManager.Instance.IsInitialized == false)
            {
                StartCoroutine(CoChangeSceneType(sceneType));
                return;
            }

            ApplySceneType(sceneType);
        }

        private void ApplySceneType(string sceneType)
        {
            _currentSceneType = sceneType;

            // 매니저들에 씬 전환 통보 (UI 처리보다 먼저 — 레퍼런스 재수집 선행)
            GameManager.Instance.NotifySceneChanged(sceneType);

            if (sceneType == SceneType.GamePlay)
            {
                UIManager.Instance.HideUI(UIKeyType.TitleMenu);
                UIManager.Instance.HideUI(UIKeyType.PauseMenu);
                UIManager.Instance.ShowUI(UIKeyType.GamePlay);
            }
            else if (sceneType == SceneType.Title)
            {
                UIManager.Instance.HideUI(UIKeyType.PauseMenu);
                UIManager.Instance.HideUI(UIKeyType.GamePlay);
                UIManager.Instance.ShowUI(UIKeyType.TitleMenu);
            }
            else
            {
                UIManager.Instance.HideUI(UIKeyType.PauseMenu);
                UIManager.Instance.HideUI(UIKeyType.GamePlay);
                UIManager.Instance.HideUI(UIKeyType.TitleMenu);
            }
        }

        private IEnumerator CoChangeSceneType(string sceneType)
        {
            yield return new WaitUntil(() => UIManager.Instance.IsInitialized);
            ApplySceneType(sceneType);
        }
    }
}