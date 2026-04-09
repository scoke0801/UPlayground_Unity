using UnityEngine;

namespace UPlayGround.Manager
{
    /// <summary>
    /// 치트 옵션 관리 매니저.
    /// GameManager에 등록되며 개발/테스트용 옵션을 중앙 관리한다.
    /// </summary>
    public class CheatManager : BaseManager<CheatManager>, IManager
    {
        [Header("전투 치트")]
        [Tooltip("활성화 시 어떤 상태에서도 적의 공격을 패리할 수 있다")]
        [SerializeField] private bool _alwaysParry = false;

        /// <summary> 항상 패리 가능 여부 </summary>
        public bool IsAlwaysParryEnabled => _alwaysParry;

        public void SetAlwaysParry(bool value)
        {
            _alwaysParry = value;
            Debug.Log($"[CheatManager] 항상 패리: {(_alwaysParry ? "ON" : "OFF")}");
        }

        public void ToggleAlwaysParry()     => SetAlwaysParry(!_alwaysParry);

        #region IManager

        public void Init()                          => Debug.Log("[CheatManager] 초기화");
        public void AfterInit()                     { }
        public void Dispose()                       { }
        public void OnUpdate()                      { }
        public void OnFixedUpdate()                 { }
        public void OnLateUpdate()                  { }
        public void OnSceneChanged(string sceneType){ }

        #endregion
    }
}
