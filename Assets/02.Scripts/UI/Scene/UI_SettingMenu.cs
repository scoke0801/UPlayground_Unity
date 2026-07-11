using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using UPlayGround.Data.Config;
using UPlayGround.Manager;

namespace UPlayGround.UI
{
    public class UI_SettingMenu : UI_Base
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

        private UISettingPageBase _currentPage;
        // SyncUIFromData() 중 콜백이 data를 다시 덮어쓰는 것을 방지
        private bool _isSyncing;

        // 탭 인덱스 → 페이지 (프리팹의 탭 배치 순서와 반드시 일치)
        private UISettingPageBase[] _pages;

        protected override void Awake()
        {
            base.Awake();

            _pages = new UISettingPageBase[] { _panelGameplay, _panelGraphics, _panelAudio, _panelKeys };

            if (_tabGroup != null)
                _tabGroup.SelectionChanged += OnTabSelected;

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

            _currentPage = _pages[index];
            ShowTab(_currentPage.gameObject);
            SyncUIFromData();
        }

        protected override void OnInit()
        {
            TryBindSettingsData();
        }

        protected override void OnShow()
        {
            if (!TryBindSettingsData())
                return;

            _snapshot = SettingsSnapshot.From(_settingsData);

            // 게임플레이 탭(인덱스 0)을 선택 상태로 시작 → SelectionChanged → ShowTab/SyncUIFromData
            if (_tabGroup != null)
            {
                _tabGroup.Select(0);
            }
            else
            {
                _currentPage = _panelGameplay;
                ShowTab(_panelGameplay.gameObject);
                SyncUIFromData();
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
            var sm = SettingsManager.Instance;
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
            // 적용은 SettingsManager에 위임한다. 믹서 미연결 시에도 ResolveMixer() 폴백으로
            // 오디오가 즉시 반영되며, 그래픽도 이 시점에 적용된다.
            SettingsManager.Instance.ApplyCurrentSettings(_audioMixer);
            _settingsData.Save();
            Hide();
        }

        private void OnCancel()
        {
            _snapshot.ApplyTo(_settingsData);
            Hide();
        }

        private void OnReset()
        {
            _settingsData.ResetToDefault();
            SyncUIFromData();
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
