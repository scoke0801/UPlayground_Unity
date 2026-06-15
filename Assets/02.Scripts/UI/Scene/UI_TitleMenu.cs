using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UPlayGround.UREnum;
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
        // 이어하기: 가장 최근 슬롯을 로드하고 저장된 씬으로 진입.
        // 저장이 없으면 새 게임으로 폴백.
        int recent = SaveManager.Instance.GetMostRecentSlot();
        UIManager.Instance.HideAllUI();

        if (recent >= 0)
            SaveManager.Instance.LoadGameToScene(recent);
        else
            SceneManager.Instance.LoadScene(SceneName.InGame);
    }
    
    private void OnClickLoadButton()
    {
        // 슬롯 선택 UI를 로드 모드로 띄운다. 슬롯 선택 시 저장된 씬으로 진입한다.
        var go = UIManager.Instance.ShowUI(UI_SaveSlotMenu.UIKey);
        go?.GetComponent<UI_SaveSlotMenu>()?.SetMode(UI_SaveSlotMenu.SaveSlotMode.Load);
    }
    
    private void OnClickNewGameButton()
    {
        UIManager.Instance.HideAllUI();
        SceneManager.Instance.LoadScene(SceneName.InGame);
    }
    
    private void OnClickOptionButton()
    {
        UIManager.Instance.ShowUI(UIKeyType.Config);
    }
}
