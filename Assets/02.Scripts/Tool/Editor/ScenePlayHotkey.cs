using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// 어느 씬에서 작업 중이든 Ctrl+H 단축키로 지정된 씬을 바로 플레이한다.
/// playModeStartScene 을 사용하므로 현재 열린 씬과 저장하지 않은 변경은 그대로 보존되며,
/// 플레이를 종료하면 원래 작업하던 씬으로 복귀한다.
/// 대상 씬은 메뉴로 지정하며, 지정하지 않은 경우 빌드 설정의 첫 활성 씬(보통 Boot)을 사용한다.
/// </summary>
public static class ScenePlayHotkey
{
    // 대상 씬 GUID 저장 키 (프로젝트별 EditorPrefs)
    private static readonly string TargetScenePrefKey = $"ScenePlayHotkey.TargetSceneGuid.{PlayerSettings.productName}";

    // 이번 플레이가 핫키로 시작되었는지 추적 (도메인 리로드 후에도 유지되도록 SessionState 사용)
    private const string StartedByHotkeyKey = "ScenePlayHotkey.StartedByHotkey";

    [InitializeOnLoadMethod]
    private static void Init()
    {
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    /// <summary>
    /// 지정 씬 플레이 (Ctrl+H). 이미 플레이 중이면 정지한다.
    /// </summary>
    [MenuItem("Tools/씬 핫키/지정 씬 바로 플레이 %h", false, 0)]
    public static void PlayTargetScene()
    {
        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        SceneAsset target = GetTargetScene();
        if (target == null)
        {
            EditorUtility.DisplayDialog("씬 핫키",
                "플레이할 씬이 지정되지 않았습니다.\n\n" +
                "'Tools/씬 핫키/현재 씬을 핫키 대상으로 지정' 메뉴로 먼저 씬을 지정하거나,\n" +
                "빌드 설정(Build Settings)에 씬을 등록하세요.",
                "확인");
            return;
        }

        // 작업 중인 씬에 저장하지 않은 변경이 있으면 저장 여부를 묻는다. 취소하면 플레이하지 않음.
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EditorSceneManager.playModeStartScene = target;
        SessionState.SetBool(StartedByHotkeyKey, true);
        EditorApplication.isPlaying = true;
        Debug.Log($"[씬 핫키] '{target.name}' 씬을 플레이합니다.");
    }

    /// <summary>
    /// 현재 활성 씬을 핫키 대상으로 지정한다.
    /// </summary>
    [MenuItem("Tools/씬 핫키/현재 씬을 핫키 대상으로 지정", false, 20)]
    public static void SetCurrentSceneAsTarget()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog("씬 핫키", "현재 씬이 저장되지 않았습니다. 먼저 씬을 저장하세요.", "확인");
            return;
        }

        string guid = AssetDatabase.AssetPathToGUID(scene.path);
        EditorPrefs.SetString(TargetScenePrefKey, guid);
        EditorUtility.DisplayDialog("씬 핫키",
            $"핫키 대상 씬이 '{scene.name}'(으)로 지정되었습니다.\n이제 어디서든 Ctrl+H 로 이 씬을 플레이합니다.",
            "확인");
    }

    /// <summary>
    /// 현재 지정된 핫키 대상 씬을 표시한다.
    /// </summary>
    [MenuItem("Tools/씬 핫키/현재 대상 씬 확인", false, 21)]
    public static void ShowCurrentTarget()
    {
        SceneAsset target = GetTargetScene();
        string msg = target != null
            ? $"현재 핫키 대상 씬: '{target.name}'"
            : "지정된 대상 씬이 없습니다. (빌드 설정의 첫 활성 씬도 없음)";
        EditorUtility.DisplayDialog("씬 핫키", msg, "확인");
    }

    private static SceneAsset GetTargetScene()
    {
        // 1) 명시적으로 지정한 씬
        string guid = EditorPrefs.GetString(TargetScenePrefKey, string.Empty);
        if (!string.IsNullOrEmpty(guid))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!string.IsNullOrEmpty(path))
            {
                var asset = AssetDatabase.LoadAssetAtPath<SceneAsset>(path);
                if (asset != null)
                    return asset;
            }
        }

        // 2) 폴백: 빌드 설정의 첫 번째 활성 씬 (보통 Boot)
        foreach (var s in EditorBuildSettings.scenes)
        {
            if (s.enabled && !string.IsNullOrEmpty(s.path))
                return AssetDatabase.LoadAssetAtPath<SceneAsset>(s.path);
        }

        return null;
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        // 핫키로 시작한 플레이가 끝나면 playModeStartScene 을 비워서
        // 이후 일반 플레이(Ctrl+P)는 현재 열린 씬을 그대로 사용하도록 복구한다.
        if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(StartedByHotkeyKey, false))
        {
            EditorSceneManager.playModeStartScene = null;
            SessionState.SetBool(StartedByHotkeyKey, false);
        }
    }
}
