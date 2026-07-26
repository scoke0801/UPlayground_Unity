using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Data.Config;

namespace UPlayGround.UI
{
    public class UISettingPageBase : MonoBehaviour
    {
        private bool _isBound;

        public void Bind(SettingsData settingsData)
        {
            if (_isBound || settingsData == null)
                return;

            BindControls(settingsData);
            _isBound = true;
        }

        protected virtual void BindControls(SettingsData settingsData)
        {
        }

        public virtual void SyncUIFromData(SettingsData settingsData)
        {
        }

        /// <summary>
        /// 페이지가 자체 게임패드 내비게이션을 구성한다. 키 설정처럼 여러 열(패널)로 나뉜
        /// 화면은 계층 순서의 단일 세로 체인으로 표현할 수 없으므로 이 훅으로 직접 배선한다.
        /// true를 돌려주면 <see cref="UI_SettingMenu"/>는 기본 세로 체인 구성을 건너뛴다.
        /// </summary>
        /// <param name="upNeighbor">페이지 위쪽 이웃(선택된 탭 버튼).</param>
        /// <param name="downNeighbor">페이지 아래쪽 이웃(하단 버튼 열의 첫 항목).</param>
        /// <param name="entry">탭에서 아래로 내려올 때 진입할 항목.</param>
        /// <param name="exit">하단 버튼에서 위로 올라올 때 돌아갈 항목.</param>
        public virtual bool TryConfigureNavigation(
            Selectable upNeighbor,
            Selectable downNeighbor,
            out Selectable entry,
            out Selectable exit)
        {
            entry = null;
            exit = null;
            return false;
        }

        protected static int RoundToInt(float value) => Mathf.RoundToInt(value);

        protected static T GetAt<T>(T[] items, int index) where T : class
            => items != null && index >= 0 && index < items.Length ? items[index] : null;
    }
}
