using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UPlayGround.Enum;
using UPlayGround.Manager;

/// <summary>
/// 타이틀 UI
/// </summary>
public class UI_TitleMenu : UI_Base
{
    [Header("UI 버튼")] 
    [SerializeField] private Button combatSceneButton;
    [SerializeField] private Button movementSceneButton;
    [SerializeField] private Button interactionSceneButton;
    [SerializeField] private Button cameraSceneButton;
    [SerializeField] private Button defaultSceneButton;
    
    protected override void Awake()
    {
        base.Awake();

        // 버튼 이벤트 연결
        if (combatSceneButton)
        {
            combatSceneButton.onClick.AddListener(OnClickCombatSceneButton);
        }
        
        if (movementSceneButton)
        {
            movementSceneButton.onClick.AddListener(OnClickKccSceneButton);
        }
        
        if (interactionSceneButton)
        {
            interactionSceneButton.onClick.AddListener(OnClickInteractionSceneButton);
        }
        
        if (cameraSceneButton)
        {
            cameraSceneButton.onClick.AddListener(OnClickCameraSceneButton);
        }
        
        if (defaultSceneButton)
        {
            defaultSceneButton.onClick.AddListener(OnClickInGameSceneButton);
        }
    }

    private void OnClickCombatSceneButton()
    {
        SceneManager.Instance.LoadScene(SceneName.CombatTest);
    }

    private void OnClickKccSceneButton()
    {
        SceneManager.Instance.LoadScene(SceneName.KccTest);
    }

    private void OnClickCameraSceneButton()
    {
        SceneManager.Instance.LoadScene(SceneName.CameraTest);
    }

    private void OnClickInteractionSceneButton()
    {
        SceneManager.Instance.LoadScene(SceneName.InteractionTest);
    }

    private void OnClickInGameSceneButton()
    {
        SceneManager.Instance.LoadScene(SceneName.InGame);
    }
}
