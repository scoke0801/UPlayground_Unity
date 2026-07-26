using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using UPlayGround.Data.Config;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    public class UI_SettingMenu : UI_SceneBase
    {
        [Header("Panels")]
        [SerializeField] private UISettingPageGamePlay _panelGameplay;
        [SerializeField] private UISettingPageGraphic _panelGraphics;
        [SerializeField] private UISettingPageAudio _panelAudio;
        [SerializeField] private UISettingPageKeyBinding _panelKeys;

        [Header("TabButtons")]
        // 탭 하이라이트/단일 선택은 UITabGroup이 관리한다. 인덱스 순서: 0=게임플레이,1=그래픽,2=오디오,3=키설정
        [SerializeField] private UITabGroup _tabGroup;

        [Header("Footer")]
        [SerializeField] private Button _btnApply;
        [SerializeField] private Button _btnCancel;
        [SerializeField] private Button _btnReset;

        [Header("Close")]
        // 상단 X 버튼(선택). 취소(변경 취소 후 닫기)와 동일하게 동작한다.
        [SerializeField] private Button _btnClose;

        [Header("Data")]
        // AudioMixer는 오디오 반영용. null이면 오디오 적용만 스킵된다.
        [SerializeField] private AudioMixer _audioMixer;

        // SettingsManager에서 런타임에 가져온다. Inspector 연결 불필요.
        private SettingsData _settingsData;

        private SettingsSnapshot _snapshot;
        private string _inputBindingSnapshot;

        private UISettingPageBase _currentPage;
        // SyncUIFromData() 중 콜백이 data를 다시 덮어쓰는 것을 방지
        private bool _isSyncing;
        private bool _isApplying;
        private bool _suppressTabSelection;
        private TMP_Text _applyButtonLabel;
        private string _applyButtonDefaultText;

        // 탭 인덱스 → 페이지 (프리팹의 탭 배치 순서와 반드시 일치)
        private UISettingPageBase[] _pages;

        protected override void Awake()
        {
            base.Awake();

            _pages = new UISettingPageBase[] { _panelGameplay, _panelGraphics, _panelAudio, _panelKeys };

            if (_tabGroup != null)
                _tabGroup.SelectionChanged += OnTabSelected;

            ConfigureTabShortcuts(subTabs: _tabGroup);
            ConfigureMainPageShortcut(UIKeyType.Config);
            BindFooterButtons();
        }

        protected override void OnDispose()
        {
            base.OnDispose();

            if (_tabGroup != null)
                _tabGroup.SelectionChanged -= OnTabSelected;
        }

        // UITabGroup 선택 콜백 (탭 클릭 및 초기 Select 모두 여기로 들어온다)
        private void OnTabSelected(int index)
        {
            if (_pages == null || index < 0 || index >= _pages.Length) return;
            if (_suppressTabSelection) return;

            _currentPage = _pages[index];
            ShowTab(_currentPage.gameObject);
            SyncUIFromData();
            RebuildNavigation(index);
        }

        protected override void OnInit()
        {
            TryBindSettingsData();
        }

        // 설정 메뉴는 전체 화면을 덮는 모달이므로 하위(게임플레이 등) 입력을 차단한다.
        // (실제 전체 화면 커버 비주얼은 프리팹 루트 RectTransform을 stretch + 불투명/딤 배경으로 구성해야 한다.)
        protected override bool BlocksLowerInput => true;

        protected override void OnShow()
        {
            base.OnShow();

            if (!TryBindSettingsData())
                return;

            _snapshot = SettingsSnapshot.From(_settingsData);
            _inputBindingSnapshot = Svc.Input?.CaptureBindingProfileSnapshot();
            _panelKeys?.BeginEditSession();

            // 게임플레이 탭(인덱스 0)을 선택 상태로 시작 → SelectionChanged → ShowTab/SyncUIFromData
            if (_tabGroup != null)
            {
                _tabGroup.Select(0);
                SetDefaultFocus(_tabGroup.GetTab(0)?.Button);
            }
            else
            {
                _currentPage = _panelGameplay;
                ShowTab(_panelGameplay.gameObject);
                SyncUIFromData();
            }
        }

        private void RebuildNavigation(int selectedTabIndex)
        {
            if (_tabGroup == null)
                return;

            var tabs = new System.Collections.Generic.List<Selectable>();
            for (int i = 0; i < _tabGroup.TabCount; i++)
            {
                Button button = _tabGroup.GetTab(i)?.Button;
                if (button != null)
                    tabs.Add(button);
            }
            UIFocusNavigation.ConfigureHorizontal(tabs);

            Selectable[] pageControls = _currentPage != null
                ? _currentPage.GetComponentsInChildren<Selectable>(false)
                : System.Array.Empty<Selectable>();
            UIFocusNavigation.ConfigureVertical(pageControls);

            var footer = new Selectable[] { _btnReset, _btnCancel, _btnApply, _btnClose };
            UIFocusNavigation.ConfigureHorizontal(footer);

            Selectable firstPage = UIFocusNavigation.FirstNavigable(pageControls);
            Selectable firstFooter = UIFocusNavigation.FirstNavigable(footer);
            foreach (Selectable tab in tabs)
            {
                Navigation navigation = tab.navigation;
                navigation.selectOnDown = firstPage ?? firstFooter;
                tab.navigation = navigation;
            }

            Selectable selectedTab = selectedTabIndex >= 0 && selectedTabIndex < tabs.Count
                ? tabs[selectedTabIndex]
                : tabs.Count > 0 ? tabs[0] : null;
            Selectable lastPage = null;
            for (int i = pageControls.Length - 1; i >= 0; i--)
            {
                if (UIFocusNavigation.IsNavigable(pageControls[i]))
                {
                    lastPage = pageControls[i];
                    break;
                }
            }

            if (firstPage != null)
            {
                Navigation navigation = firstPage.navigation;
                navigation.selectOnUp = selectedTab;
                firstPage.navigation = navigation;
            }
            if (lastPage != null)
            {
                Navigation navigation = lastPage.navigation;
                navigation.selectOnDown = firstFooter;
                lastPage.navigation = navigation;
            }

            foreach (Selectable button in footer)
            {
                if (button == null)
                    continue;
                Navigation navigation = button.navigation;
                navigation.selectOnUp = lastPage ?? selectedTab;
                button.navigation = navigation;
            }
        }

        // ---- 탭 ----

        private void ShowTab(GameObject target)
        {
            _panelGameplay.gameObject.SetActive(_panelGameplay.gameObject == target);
            _panelGraphics.gameObject.SetActive(_panelGraphics.gameObject == target);
            _panelAudio.gameObject.SetActive(   _panelAudio.gameObject    == target);
            _panelKeys.gameObject.SetActive(    _panelKeys.gameObject     == target);
        }

        // ---- 컨트롤 이벤트 → Data ----

        private void BindControlEvents()
        {
            _panelGameplay?.Bind(_settingsData);
            _panelGraphics?.Bind(_settingsData);
            _panelAudio?.Bind(_settingsData);
            _panelKeys?.Bind(_settingsData);
        }

        private bool TryBindSettingsData()
        {
            if (_settingsData != null)
                return true;

            // SettingsManager가 Addressable 비동기 로드를 완료했는지 확인
            var sm = UISvc.Settings;
            if (!sm.IsLoaded || sm.Data == null)
            {
                Debug.LogWarning("[UI_Settings] SettingsManager가 아직 로드되지 않았습니다.");
                return false;
            }

            _settingsData = sm.Data;
            _settingsData.Load();
            BindControlEvents();
            return true;
        }

        // 동기화 중 콜백 무시
        private void Write(System.Action setter)
        {
            if (!_isSyncing) setter();
        }

        // ---- 하단 버튼 ----

        private void BindFooterButtons()
        {
            _btnApply.onClick.AddListener(OnApply);
            _btnCancel.onClick.AddListener(OnCancel);
            _btnReset.onClick.AddListener(OnReset);
            if (_btnClose != null)
                _btnClose.onClick.AddListener(OnCancel);
        }

        private void OnApply()
        {
            if (_isApplying)
                return;

            ApplyAsync().Forget();
        }

        private async UniTask ApplyAsync()
        {
            int selectedTabIndex = _tabGroup != null ? _tabGroup.SelectedIndex : -1;
            GameObject selectedObject = EventSystem.current != null
                ? EventSystem.current.currentSelectedGameObject
                : null;

            SetApplyBusy(true);

            // 버튼의 "적용 중…" 상태가 먼저 화면에 그려진 뒤 입력 맵/그래픽 설정을 반영한다.
            await UniTask.Yield(PlayerLoopTiming.Update);

            try
            {
                if (_panelKeys != null && !_panelKeys.ApplyPendingChanges())
                    return;

                // 적용은 SettingsManager에 위임한다. 믹서 미연결 시에도 ResolveMixer() 폴백으로
                // 오디오가 즉시 반영되며, 그래픽도 이 시점에 적용된다.
                UISvc.Settings.ApplyCurrentSettings(_audioMixer);

                // 두 데이터를 먼저 PlayerPrefs 메모리에 기록하고 디스크 flush는 한 번만 수행한다.
                _settingsData.Save(flushPlayerPrefs: false);
                Svc.Input?.SaveBindingProfile(flushPlayerPrefs: false);
                PlayerPrefs.Save();

                // 적용 후에도 설정 화면을 유지한다. 이 시점의 값을 새 취소 기준점으로
                // 갱신하지 않으면 이후 취소 시 이미 저장한 키까지 메뉴 진입 전 상태로 되돌아간다.
                _snapshot = SettingsSnapshot.From(_settingsData);
                _inputBindingSnapshot = Svc.Input?.CaptureBindingProfileSnapshot();
                _panelKeys?.BeginEditSession(refreshRows: false);

                // 키 페이지는 OnBindingsChanged에서 이미 새 프로필로 갱신됐다.
                // 여기서 다시 전체 행을 파괴/생성하지 않는다.
                if (_currentPage != _panelKeys)
                    SyncUIFromData();
            }
            finally
            {
                SetApplyBusy(false);
                RestoreSelectionAfterApply(selectedTabIndex, selectedObject);
            }
        }

        private void SetApplyBusy(bool busy)
        {
            _isApplying = busy;
            if (_btnApply == null)
                return;

            // 클릭된 Apply 버튼을 비활성화하면 EventSystem이 첫 번째 탭으로 포커스를
            // 옮기고 UITabButton.OnSelect가 게임플레이 페이지를 여는 문제가 생긴다.
            // 중복 실행은 _isApplying으로 차단하므로 Apply 버튼의 선택 가능 상태는 유지한다.
            if (_btnCancel != null)
                _btnCancel.interactable = !busy;
            if (_btnReset != null)
                _btnReset.interactable = !busy;
            if (_btnClose != null)
                _btnClose.interactable = !busy;
            _applyButtonLabel ??= _btnApply.GetComponentInChildren<TMP_Text>(true);
            if (_applyButtonLabel == null)
                return;

            if (string.IsNullOrEmpty(_applyButtonDefaultText))
                _applyButtonDefaultText = _applyButtonLabel.text;
            _applyButtonLabel.text = busy ? "적용 중…" : _applyButtonDefaultText;
        }

        private void RestoreSelectionAfterApply(int tabIndex, GameObject selectedObject)
        {
            if (_tabGroup != null && tabIndex >= 0 && tabIndex < _tabGroup.TabCount)
            {
                // 입력 액션 맵 재활성화나 포커스 복구가 다른 탭을 선택했더라도
                // 적용 직전 페이지로 되돌린다.
                if (_tabGroup.SelectedIndex != tabIndex || _currentPage != _pages[tabIndex])
                    _tabGroup.Select(tabIndex);
                else
                    _tabGroup.Select(tabIndex, notify: false);
            }

            if (selectedObject == null
                || !selectedObject.activeInHierarchy
                || EventSystem.current == null)
            {
                return;
            }

            Selectable selectable = selectedObject.GetComponent<Selectable>();
            if (selectable == null || selectable.IsInteractable())
                EventSystem.current.SetSelectedGameObject(selectedObject);
        }

        private void OnCancel()
        {
            if (_isApplying)
                return;

            int selectedTabIndex = _tabGroup != null ? _tabGroup.SelectedIndex : -1;
            _suppressTabSelection = true;
            try
            {
                _panelKeys?.DiscardPendingChanges();
                _snapshot.ApplyTo(_settingsData);
                if (!string.IsNullOrWhiteSpace(_inputBindingSnapshot))
                    Svc.Input?.RestoreBindingProfileSnapshot(_inputBindingSnapshot);

                // 입력 맵 복원 중 EventSystem이 첫 탭을 선택해도 실제 페이지는 바꾸지 않는다.
                // 탭 시각 상태도 취소 직전 선택으로 되돌린 뒤 메뉴를 닫는다.
                if (_tabGroup != null
                    && selectedTabIndex >= 0
                    && selectedTabIndex < _tabGroup.TabCount)
                {
                    _tabGroup.Select(selectedTabIndex, notify: false);
                }

                Hide();
            }
            finally
            {
                _suppressTabSelection = false;
            }
        }

        private void OnReset()
        {
            if (_isApplying)
                return;

            _settingsData.ResetToDefault();
            _panelKeys?.StageResetAll();
            SyncUIFromData();
        }

        public override bool PerformBackFunction()
        {
            if (_isApplying)
                return false;

            if (_panelKeys != null && _panelKeys.TryHandleBack())
                return false;

            OnCancel();
            return false;
        }

        protected override bool TryCloseForMainPageSwitch()
        {
            if (_isApplying || Svc.Input?.IsRebindCaptureActive == true)
                return false;
            OnCancel();
            return true;
        }

        // ---- Data → UI 동기화 ----

        private void SyncUIFromData()
        {
            _isSyncing = true;

            _currentPage.SyncUIFromData(_settingsData);

            _isSyncing = false;
        }
    }
}
