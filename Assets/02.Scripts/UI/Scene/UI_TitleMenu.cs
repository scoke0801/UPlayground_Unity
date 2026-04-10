using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UPlayGround.Enum;
using UPlayGround.Data.Path;
using UPlayGround.Manager;

/// <summary>
/// 타이틀 UI
/// </summary>
public class UI_TitleMenu : UI_Base
{
    [Header("UI 버튼")] 
    [SerializeField] private Button continueButton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button newGameButton;
    [SerializeField] private Button optionButton;
    
    protected override void Awake()
    {
        base.Awake();

        if (continueButton)
        {
            continueButton.onClick.AddListener(OnClickContinueButton);
        }
        
        if (loadButton)
        {
            loadButton.onClick.AddListener(OnClickLoadButton);
        }
        if (newGameButton)
        {
            newGameButton.onClick.AddListener(OnClickNewGameButton);
        }
        if (optionButton)
        {
            optionButton.onClick.AddListener(OnClickOptionButton);
        }
    }

    private void OnClickContinueButton()
    {
        UIManager.Instance.HideAllUI();
        SceneManager.Instance.LoadScene(SceneName.InGame);
    }
    
    private void OnClickLoadButton()
    {
        UIManager.Instance.HideAllUI();
        SceneManager.Instance.LoadScene(SceneName.InGame);
    }
    
    private void OnClickNewGameButton()
    {
        UIManager.Instance.HideAllUI();
        SceneManager.Instance.LoadScene(SceneName.InGame);
    }
    
    private void OnClickOptionButton()
    {
        UIManager.Instance.ShowUI(UIKeyType.SettingMenu);
    }
}
