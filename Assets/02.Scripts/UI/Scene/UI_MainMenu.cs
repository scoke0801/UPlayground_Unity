using UnityEngine;
using UnityEngine.UI;
using UPlayGround.Manager;
using UPlayGround.Enum;

/// <summary>
/// 메인 메뉴 UI
/// UIManager.ShowUI("MainMenu") 로 표시
/// </summary>
public class UI_MainMenu : UI_Base
{
    [Header("버튼")]
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _quitButton;

    [Header("로딩 패널")]
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private Slider _progressBar;

    protected override void Awake()
    {
        base.Awake();
        _layer = CanvasLayer.Scene;

        _startButton?.onClick.AddListener(OnStartClicked);
        _quitButton?.onClick.AddListener(OnQuitClicked);
    }

    protected override void OnInit()
    {
        _loadingPanel?.SetActive(false);
    }

    private void OnStartClicked()
    {
        _startButton.interactable = false;
        _loadingPanel?.SetActive(true);

        var sceneManager = SceneManager.Instance;
        sceneManager.OnLoadProgress += OnProgress;
        sceneManager.OnLoadComplete += OnComplete;
    }

    private void OnProgress(float progress)
    {
        if (_progressBar != null)
            _progressBar.value = progress;
    }

    private void OnComplete(string sceneName)
    {
        SceneManager.Instance.OnLoadProgress -= OnProgress;
        SceneManager.Instance.OnLoadComplete -= OnComplete;
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
