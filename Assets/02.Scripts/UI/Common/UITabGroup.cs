using System;
using System.Collections.Generic;
using UnityEngine;

namespace UPlayGround.UI
{
    /// <summary>
    /// 재사용 탭 그룹 — 등록된 <see cref="UITabButton"/> 중 하나만 선택 상태로 유지한다.
    ///
    /// 사용 흐름:
    ///   1) 메뉴는 <see cref="SelectionChanged"/>를 구독해 선택된 인덱스에 맞춰 콘텐츠를 갱신한다.
    ///   2) 메뉴를 열 때 초기 표시 탭을 <see cref="Select"/>로 지정한다.
    ///   3) 사용자가 탭을 누르면 그룹이 자동으로 단일 선택을 처리하고 SelectionChanged를 발생시킨다.
    ///
    /// UI_Base 파생이 아니므로 접두사 규약상 UITabGroup으로 명명.
    /// </summary>
    public class UITabGroup : MonoBehaviour
    {
        [SerializeField] private List<UITabButton> _tabs = new List<UITabButton>();

        /// <summary> 탭이 선택될 때 발생(인자: 탭 인덱스). notify=false 호출 시에는 발생하지 않는다. </summary>
        public event Action<int> SelectionChanged;

        public int SelectedIndex { get; private set; } = -1;
        public int TabCount => _tabs.Count;

        private void Awake()
        {
            for (int i = 0; i < _tabs.Count; i++)
            {
                if (_tabs[i] == null) continue;
                int index = i; // 클로저 캡처
                _tabs[i].Clicked += () => Select(index);
            }
        }

        /// <summary>
        /// 지정 인덱스 탭을 선택 상태로 만들고 나머지는 해제한다.
        /// notify=false면 <see cref="SelectionChanged"/>를 발생시키지 않는다(초기 세팅 등).
        /// </summary>
        public void Select(int index, bool notify = true)
        {
            if (index < 0 || index >= _tabs.Count) return;

            SelectedIndex = index;
            for (int i = 0; i < _tabs.Count; i++)
                _tabs[i]?.SetSelected(i == index);

            if (notify) SelectionChanged?.Invoke(index);
        }

        public UITabButton GetTab(int index)
            => (index >= 0 && index < _tabs.Count) ? _tabs[index] : null;

        /// <summary>
        /// 현재 탭을 기준으로 이전/다음 활성 탭을 순환 선택한다.
        /// 숄더 버튼 전환은 EventSystem 포커스를 탭으로 빼앗지 않고 콘텐츠 포커스를 유지한다.
        /// </summary>
        public bool SelectRelative(int direction, bool wrap = true)
        {
            if (_tabs.Count == 0 || direction == 0)
                return false;

            int step = direction < 0 ? -1 : 1;
            int start = SelectedIndex >= 0 && SelectedIndex < _tabs.Count
                ? SelectedIndex
                : step > 0 ? -1 : _tabs.Count;

            for (int offset = 1; offset <= _tabs.Count; offset++)
            {
                int candidate = start + step * offset;
                if (wrap)
                {
                    candidate %= _tabs.Count;
                    if (candidate < 0)
                        candidate += _tabs.Count;
                }
                else if (candidate < 0 || candidate >= _tabs.Count)
                {
                    return false;
                }

                UITabButton tab = _tabs[candidate];
                if (tab == null
                    || tab.Button == null
                    || !tab.Button.gameObject.activeInHierarchy
                    || !tab.Button.IsActive()
                    || !tab.Button.IsInteractable())
                {
                    continue;
                }

                Select(candidate);
                return true;
            }

            return false;
        }

        /// <summary> 빌더/에디터에서 탭 목록을 주입할 때 사용. </summary>
        public void SetTabs(IEnumerable<UITabButton> tabs)
        {
            _tabs.Clear();
            _tabs.AddRange(tabs);
        }
    }
}
