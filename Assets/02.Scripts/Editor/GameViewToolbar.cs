using System;
using System.IO;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;

/// <summary>
/// GameView 상단에 표시되는 커스텀 툴바 UI
/// 프로젝트 폴더 열기 등의 유용한 기능들을 제공합니다.
/// </summary>
[InitializeOnLoad]
public class GameViewToolbar : EditorWindow
{
    private static GameViewToolbar _instance;
    private static EditorWindow _gameView;
    private static readonly Type GameViewType = Type.GetType("UnityEditor.GameView,UnityEditor");

    // 툴바 설정
    private const float ToolbarHeight = 25f;
    private const float ButtonWidth = 100f;
    private const float Padding = 10f;

    static GameViewToolbar()
    {
        // 에디터 업데이트 이벤트에 등록
        EditorApplication.update += UpdateToolbarPosition;
        // 플레이 모드 변경 시 툴바 표시/숨김
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    /// <summary>
    /// 툴바 표시/숨김 토글 메뉴
    /// </summary>
    [MenuItem("Tools/GameView Toolbar/Toggle Toolbar")]
    public static void ToggleToolbar()
    {
        if (_instance != null)
        {
            CloseToolbar();
        }
        else
        {
            ShowToolbar();
        }
    }

    /// <summary>
    /// 툴바 창 표시
    /// </summary>
    public static void ShowToolbar()
    {
        if (_instance == null)
        {
            _instance = CreateInstance<GameViewToolbar>();
            _instance.titleContent = new GUIContent("GameView Toolbar");
            _instance.ShowPopup();
            _instance.minSize = new Vector2(200, ToolbarHeight);
            _instance.maxSize = new Vector2(4000, ToolbarHeight);
        }
    }

    /// <summary>
    /// 툴바 창 숨김
    /// </summary>
    public static void CloseToolbar()
    {
        if (_instance != null)
        {
            _instance.Close();
            _instance = null;
        }
    }

    /// <summary>
    /// GameView를 찾고 툴바 위치를 업데이트
    /// </summary>
    private static void UpdateToolbarPosition()
    {
        if (_instance == null || GameViewType == null) return;

        // GameView 찾기
        _gameView = GetGameView();
        if (_gameView == null) return;

        // GameView 위에 툴바 위치 설정
        var gameViewRect = _gameView.position;
        var toolbarRect = new Rect(
            gameViewRect.x + Padding,
            gameViewRect.y + 20, // 윈도우 타이틀바 아래
            gameViewRect.width - (Padding * 2),
            ToolbarHeight
        );

        _instance.position = toolbarRect;
    }

    /// <summary>
    /// GameView 인스턴스 가져오기
    /// </summary>
    private static EditorWindow GetGameView()
    {
        if (GameViewType == null) return null;
        
        var gameViews = Resources.FindObjectsOfTypeAll(GameViewType);
        if (gameViews.Length > 0)
        {
            return gameViews[0] as EditorWindow;
        }
        
        return null;
    }

    /// <summary>
    /// 플레이 모드 변경 시 호출
    /// </summary>
    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        switch (state)
        {
            case PlayModeStateChange.EnteredPlayMode:
                // 플레이 모드 진입 시 툴바 자동 표시
                ShowToolbar();
                break;
                
            case PlayModeStateChange.ExitingPlayMode:
                // 플레이 모드 종료 시 툴바 숨김
                CloseToolbar();
                break;
        }
    }

    /// <summary>
    /// 툴바 GUI 그리기
    /// </summary>
    private void OnGUI()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            GUILayout.Label("개발 도구:", EditorStyles.boldLabel, GUILayout.Width(70));
            
            // 프로젝트 폴더 열기 버튼
            if (GUILayout.Button("프로젝트 폴더 열기", EditorStyles.toolbarButton, GUILayout.Width(ButtonWidth)))
            {
                OpenProjectFolder();
            }
            
            GUILayout.Space(5);
            
            // 추가 기능 버튼들
            if (GUILayout.Button("로그 폴더 열기", EditorStyles.toolbarButton, GUILayout.Width(ButtonWidth)))
            {
                OpenLogsFolder();
            }
            
            GUILayout.Space(5);
            
            if (GUILayout.Button("스크린샷", EditorStyles.toolbarButton, GUILayout.Width(80)))
            {
                TakeScreenshot();
            }
            
            GUILayout.FlexibleSpace();
            
            // 현재 시간 표시
            GUILayout.Label($"시간: {DateTime.Now:HH:mm:ss}", EditorStyles.toolbarButton);
            
            GUILayout.Space(5);
            
            // 닫기 버튼
            if (GUILayout.Button("✕", EditorStyles.toolbarButton, GUILayout.Width(20)))
            {
                CloseToolbar();
            }
        }
    }

    /// <summary>
    /// 현재 프로젝트 폴더 열기
    /// </summary>
    private void OpenProjectFolder()
    {
        string projectPath = Application.dataPath.Replace("/Assets", "");
        
        try
        {
#if UNITY_EDITOR_WIN
            Process.Start("explorer.exe", projectPath.Replace("/", "\\"));
#elif UNITY_EDITOR_OSX
            Process.Start("open", projectPath);
#elif UNITY_EDITOR_LINUX
            Process.Start("xdg-open", projectPath);
#endif
            UnityEngine.Debug.Log($"프로젝트 폴더 열었습니다: {projectPath}");
        }
        catch (Exception e)
        {
            UnityEngine.Debug.LogError($"프로젝트 폴더 열기 실패: {e.Message}");
        }
    }

    /// <summary>
    /// 로그 폴더 열기
    /// </summary>
    private void OpenLogsFolder()
    {
        string logsPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), "Logs");
        
        if (Directory.Exists(logsPath))
        {
            try
            {
#if UNITY_EDITOR_WIN
                Process.Start("explorer.exe", logsPath.Replace("/", "\\"));
#elif UNITY_EDITOR_OSX
                Process.Start("open", logsPath);
#elif UNITY_EDITOR_LINUX
                Process.Start("xdg-open", logsPath);
#endif
                UnityEngine.Debug.Log($"로그 폴더 열었습니다: {logsPath}");
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError($"로그 폴더 열기 실패: {e.Message}");
            }
        }
        else
        {
            UnityEngine.Debug.LogWarning("로그 폴더가 존재하지 않습니다.");
        }
    }

    /// <summary>
    /// 게임뷰 스크린샷 캡처
    /// </summary>
    private void TakeScreenshot()
    {
        string screenshotPath = Path.Combine(Application.dataPath.Replace("/Assets", ""), 
            $"Screenshot_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.png");
        
        ScreenCapture.CaptureScreenshot(screenshotPath);
        UnityEngine.Debug.Log($"스크린샷 저장: {screenshotPath}");
    }

    /// <summary>
    /// 창 업데이트 (지속적으로 시간 갱신 등을 위해)
    /// </summary>
    private void Update()
    {
        // 1초마다 GUI 갱신 (시간 표시 업데이트용)
        if (Time.realtimeSinceStartup % 1.0f < 0.02f)
        {
            Repaint();
        }
    }

    /// <summary>
    /// 창이 닫힐 때 호출
    /// </summary>
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }
}
