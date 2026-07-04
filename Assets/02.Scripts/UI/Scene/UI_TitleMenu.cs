using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UPlayGround.UREnum;
using UPlayGround.Data.Path;
using UPlayGround.Data.UI;
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

    [Header("게임 흐름")]
    [Tooltip("새 게임 시작 씬을 읽어올 맵 설정 DB. DefaultStartMapId 를 사용한다.")]
    [SerializeField] private MapConfigDatabaseSO _mapConfigDB;
    
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
        {
            SaveManager.Instance.LoadGameToScene(recent);
        }
        else
        {
            // 저장이 없어 새 게임으로 폴백하는 경우도 새 게임과 동일하게 상태를 초기화한다.
            SaveManager.Instance.ResetForNewGame();
            LoadStartScene();
        }
    }
    
    private void OnClickLoadButton()
    {
        // 슬롯 선택 UI를 로드 모드로 띄운다. 슬롯 선택 시 저장된 씬으로 진입한다.
        var go = UIManager.Instance.ShowUI(UI_SaveSlotMenu.UIKey);
        go?.GetComponent<UI_SaveSlotMenu>()?.SetMode(UI_SaveSlotMenu.SaveSlotMode.Load);
    }
    
    private void OnClickNewGameButton()
    {
        // 새 게임: 이전 세션의 진행 상태(처치 몬스터·레벨·플래그 등)가 누수되지 않도록
        // 모든 ISaveable 매니저의 인메모리 상태를 초기화한 뒤 진입한다.
        SaveManager.Instance.ResetForNewGame();
        UIManager.Instance.HideAllUI();
        LoadStartScene();
    }

    /// <summary>
    /// 새 게임 시작 씬으로 진입한다. 씬 이름은 코드 상수가 아닌
    /// MapConfigDatabaseSO.DefaultStartMapId(데이터)에서 읽는다.
    /// </summary>
    private void LoadStartScene()
    {
        string startScene = _mapConfigDB != null ? _mapConfigDB.DefaultStartMapId : null;
        if (string.IsNullOrEmpty(startScene))
        {
            Debug.LogError("[UI_TitleMenu] 시작 씬이 지정되지 않았습니다. " +
                           "MapConfigDatabaseSO를 할당하고 DefaultStartMapId를 설정하세요.");
            return;
        }

        SceneManager.Instance.LoadScene(startScene);
    }

    private void OnClickOptionButton()
    {
        UIManager.Instance.ShowUI(UIKeyType.Config);
    }
}
