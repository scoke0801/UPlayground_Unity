using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using TMPro;
using UPlayGround.Data.Config;
using UPlayGround.Manager;

public class UI_SettingMenu : UI_Base
{
    [Header("Panels")]
    [SerializeField] private GameObject _panelGameplay;
    [SerializeField] private GameObject _panelGraphics;
    [SerializeField] private GameObject _panelAudio;
    [SerializeField] private GameObject _panelKeys;

    [Header("Tab Buttons")]
    [SerializeField] private Button _tabGameplay;
    [SerializeField] private Button _tabGraphics;
    [SerializeField] private Button _tabAudio;
    [SerializeField] private Button _tabKeys;

    [Header("Gameplay")]
    [SerializeField] private Slider _sliderSensX;
    [SerializeField] private Slider _sliderSensY;
    [SerializeField] private Toggle _toggleInvertY;
    [SerializeField] private Toggle _toggleScreenShake;
    [SerializeField] private Toggle _toggleAimAssist;
    [SerializeField] private TMP_Dropdown _dropdownLanguage;

    [Header("Graphics")]
    [SerializeField] private TMP_Dropdown _dropdownResolution;
    [SerializeField] private Toggle _toggleFullscreen;
    [SerializeField] private TMP_Dropdown _dropdownQuality;
    [SerializeField] private Slider _sliderBrightness;

    [Header("Audio")]
    [SerializeField] private Slider _sliderMaster;
    [SerializeField] private Slider _sliderBGM;
    [SerializeField] private Slider _sliderSFX;
    [SerializeField] private Slider _sliderVoice;

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

    // SyncUIFromData() 중 콜백이 data를 다시 덮어쓰는 것을 방지
    private bool _isSyncing;

    protected override void OnInit()
    {
        // SettingsManager가 Addressable 비동기 로드를 완료했는지 확인
        var sm = SettingsManager.Instance;
        if (!sm.IsLoaded || sm.Data == null)
        {
            Debug.LogWarning("[UI_Settings] SettingsManager가 아직 로드되지 않았습니다.");
            return;
        }

        _settingsData = sm.Data;
        _settingsData.Load();
        BindTabButtons();
        BindControlEvents();
        BindFooterButtons();
    }

    protected override void OnShow()
    {
        // OnInit 이후 호출되므로 _settingsData가 null이면 바인딩도 안 됐다는 뜻
        if (_settingsData == null) return;

        _snapshot = SettingsSnapshot.From(_settingsData);
        SyncUIFromData();
        ShowTab(_panelGameplay);
    }

    // ---- 탭 ----

    private void BindTabButtons()
    {
        _tabGameplay.onClick.AddListener(() => ShowTab(_panelGameplay));
        _tabGraphics.onClick.AddListener(() => ShowTab(_panelGraphics));
        _tabAudio.onClick.AddListener(   () => ShowTab(_panelAudio));
        _tabKeys.onClick.AddListener(    () => ShowTab(_panelKeys));
    }

    private void ShowTab(GameObject target)
    {
        _panelGameplay.SetActive(_panelGameplay == target);
        _panelGraphics.SetActive(_panelGraphics == target);
        _panelAudio.SetActive(   _panelAudio    == target);
        _panelKeys.SetActive(    _panelKeys     == target);
    }

    // ---- 컨트롤 이벤트 → Data ----

    private void BindControlEvents()
    {
        _sliderSensX.onValueChanged.AddListener(        v => Write(() => _settingsData.sensitivityX   = (int)v));
        _sliderSensY.onValueChanged.AddListener(        v => Write(() => _settingsData.sensitivityY   = (int)v));
        _toggleInvertY.onValueChanged.AddListener(      v => Write(() => _settingsData.invertY        = v));
        _toggleScreenShake.onValueChanged.AddListener(  v => Write(() => _settingsData.screenShake    = v));
        _toggleAimAssist.onValueChanged.AddListener(    v => Write(() => _settingsData.aimAssist      = v));
        _dropdownLanguage.onValueChanged.AddListener(   v => Write(() => _settingsData.languageIndex  = v));

        _dropdownResolution.onValueChanged.AddListener( v => Write(() => _settingsData.resolutionIndex = v));
        _toggleFullscreen.onValueChanged.AddListener(   v => Write(() => _settingsData.fullscreen      = v));
        _dropdownQuality.onValueChanged.AddListener(    v => Write(() => _settingsData.qualityIndex    = v));
        _sliderBrightness.onValueChanged.AddListener(   v => Write(() => _settingsData.brightness      = (int)v));

        _sliderMaster.onValueChanged.AddListener( v => Write(() => _settingsData.masterVolume = (int)v));
        _sliderBGM.onValueChanged.AddListener(    v => Write(() => _settingsData.bgmVolume    = (int)v));
        _sliderSFX.onValueChanged.AddListener(    v => Write(() => _settingsData.sfxVolume    = (int)v));
        _sliderVoice.onValueChanged.AddListener(  v => Write(() => _settingsData.voiceVolume  = (int)v));
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

        _sliderSensX.value          = _settingsData.sensitivityX;
        _sliderSensY.value          = _settingsData.sensitivityY;
        _toggleInvertY.isOn         = _settingsData.invertY;
        _toggleScreenShake.isOn     = _settingsData.screenShake;
        _toggleAimAssist.isOn       = _settingsData.aimAssist;
        _dropdownLanguage.value     = _settingsData.languageIndex;

        _dropdownResolution.value   = _settingsData.resolutionIndex;
        _toggleFullscreen.isOn      = _settingsData.fullscreen;
        _dropdownQuality.value      = _settingsData.qualityIndex;
        _sliderBrightness.value     = _settingsData.brightness;

        _sliderMaster.value = _settingsData.masterVolume;
        _sliderBGM.value    = _settingsData.bgmVolume;
        _sliderSFX.value    = _settingsData.sfxVolume;
        _sliderVoice.value  = _settingsData.voiceVolume;

        _isSyncing = false;
    }
}
