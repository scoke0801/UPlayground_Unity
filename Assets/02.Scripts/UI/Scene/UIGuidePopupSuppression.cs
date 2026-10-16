using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UPlayGround.UI.Guide
{
    /// <summary>배치된 씬이 활성 씬인 동안 가이드 표시와 최초 안내 수집을 막는다.</summary>
    [DisallowMultipleComponent]
    public sealed class UIGuidePopupSuppression : MonoBehaviour
    {
        private static readonly HashSet<UIGuidePopupSuppression> Sources = new();

        /// <summary>현재 활성 씬에 켜진 가이드 차단 설정이 있는지 반환한다.</summary>
        public static bool IsSuppressed
        {
            get
            {
                var activeScene = SceneManager.GetActiveScene();
                foreach (var source in Sources)
                    if (source != null && source.gameObject.scene == activeScene)
                        return true;
                return false;
            }
        }

        private void OnEnable() => Sources.Add(this);
        private void OnDisable() => Sources.Remove(this);
    }
}
