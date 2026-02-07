using System.Collections;
using UnityEngine;
using UPlayGround.Enum;

namespace UPlayGround.Manager
{

    public partial class SceneManager : BaseManager<SceneManager>, IManager
    {
        public void Init()
        {
            ChangeSceneType(SceneType.GamePlay);
        }

        public void AfterInit()
        {
            
        }
        
        public void Dispose()
        {
        }

        public void OnUpdate()
        {
        }

        public void OnFixedUpdate()
        {
        }

        public void OnLateUpdate()
        {
        }
    }

    public partial class SceneManager : BaseManager<SceneManager>, IManager
    {
        
        private SceneType _sceneType = SceneType.GamePlay;

        public void ChangeSceneType(SceneType sceneType)
        {
            if (UIManager.Instance.IsInitialized == false)
            {
                StartCoroutine(CoChangeSceneType(sceneType));
                return;
            }

            ChangeSceneTypeInner(sceneType);
        }

        private void ChangeSceneTypeInner(SceneType sceneType)
        {
            _sceneType = sceneType;
               
            // [TODO] Scene 전환에 따른 무언가 처리가 필요하다면 처리
            if (sceneType == SceneType.GamePlay)
            {
                UIManager.Instance.ShowUI("GamePlay");
            }
            else
            {
                UIManager.Instance.HideUI("GamePlay");
            }
        }

        private IEnumerator CoChangeSceneType(SceneType sceneType)
        {
            while (true)
            {
                if (UIManager.Instance.IsInitialized == false)
                {
                    yield return new WaitForSeconds(0.1f);
                }
                else
                {
                    ChangeSceneTypeInner(sceneType);
                    yield break;
                }
            }
        }
    }
}