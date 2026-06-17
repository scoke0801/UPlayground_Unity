using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using UPlayGround.Data.Config;
using UPlayGround.Manager;

public class UI_SettingMenu : UI_Base
{
    [Header("Panels")]
    [SerializeField] private UISettingPageGamePlay _panelGameplay;
    [SerializeField] private UISettingPageGraphic _panelGraphics;
    [SerializeField] private UISettingPageAudio _panelAudio;
    [SerializeField] private UISettingPageKeyBinding _panelKeys;

    [Header("TabButtons")]
    [SerializeField] private Button _btnGamePlay;
    [SerializeField] private Button _btnGraphic;
    [SerializeField] private Button _btnAudio;
    [SerializeField] private Button _btnKeyBinding;
    
    [Header("Footer")]
    [SerializeField] private Button _btnApply;
    [SerializeField] private Button _btnCancel;
    [SerializeField] private Button _btnReset;

    [Header("Data")]
    // AudioMixer는 오디오 반영용. null이면 오디오 적용만 스킵된다.
    [SerializeField] private AudioMixer _audioMixer;

    // SettingsManager에서 런타임에 가져온다. Inspector 연결 불필요.
    private SettingsData _settingsData;

    private SettingsSnapshot _snapshot;

    private UISettingPageBase _currentPage;
    // SyncUIFromData() 중 콜백이 data를 다시 덮어쓰는 것을 방지
    private bool _isSyncing;

    protected override void Awake()
    {
        base.Awake();
        
        _btnGamePlay.onClick.AddListener(OnClickedGamePlay);
        _btnGraphic.onClick.AddListener(OnClickedGraphic);
        _btnAudio.onClick.AddListener(OnClickedAudio);
        _btnKeyBinding.onClick.AddListener(OnClickedKeyBinding);
        
        BindFooterButtons();
    }

    private void OnClickedGamePlay()
    {
        _currentPage = _panelGameplay;
        ShowTab(_currentPage.gameObject);
        SyncUIFromData();
    }

    private void OnClickedGraphic()
    {        
        _currentPage = _panelGraphics;
        ShowTab(_currentPage.gameObject);
        SyncUIFromData();
    }

    private void OnClickedAudio()
    {        
        _currentPage = _panelAudio;
        ShowTab(_currentPage.gameObject);
        SyncUIFromData();
    }

    private void OnClickedKeyBinding()
    {
        _currentPage = _panelKeys;
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

        _currentPage = _panelGameplay;
        _snapshot = SettingsSnapshot.From(_settingsData);
        ShowTab(_panelGameplay.gameObject);
        SyncUIFromData();
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
    }

    private void OnApply()
    {
        SettingsApplier.ApplyAll(_settingsData, _audioMixer);
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
