#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.UIElements;
using UPlayGround.Data.Actor;
using UPlayGround.Data.AI;
using UPlayGround.UI.Debugging.Laya;
using UPlayGround.EditorTools;

namespace UPlayGround.Tool.Editor
{
    /// <summary>실험 전용 리소스 생성과 재현 가능한 Development 빌드. 런처에는 노출하지 않는다.</summary>
    public static class LayaTrainingAuthoring
    {
        [UPlaygroundTool("Laya/PrepareAssets")]
        public static void PrepareAssets()
        {
            Directory.CreateDirectory("Assets/10.Datas/AI/Laya");
            Directory.CreateDirectory("Assets/Resources/Laya");
            var profiles = new LayaTrainingProfileSO[2];
            string[] ids = { "Golem_Normal", "Ent_Normal" };
            for (int i = 0; i < ids.Length; i++)
            {
                string path = "Assets/10.Datas/AI/Laya/" + ids[i] + ".asset";
                profiles[i] = AssetDatabase.LoadAssetAtPath<LayaTrainingProfileSO>(path);
                if (profiles[i] == null)
                {
                    profiles[i] = ScriptableObject.CreateInstance<LayaTrainingProfileSO>();
                    profiles[i].definition = AssetDatabase.LoadAssetAtPath<ActorDefinitionSO>("Assets/10.Datas/Actor/DataBase/" + ids[i] + ".asset");
                    if (profiles[i].definition == null) throw new InvalidOperationException("Definition 누락: " + ids[i]);
                    AssetDatabase.CreateAsset(profiles[i], path);
                }
            }
            const string panelPath = "Assets/Resources/Laya/PanelSettings.asset";
            var panel = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelPath);
            if (panel == null)
            {
                panel = ScriptableObject.CreateInstance<PanelSettings>();
                panel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
                panel.referenceResolution = new Vector2Int(1280,720);
                panel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
                panel.match = .5f;
                string themePath = "Assets/Resources/Laya/Default.tss";
                if (!File.Exists(themePath)) File.WriteAllText(themePath, "@import url(\"unity-theme://default\");\n");
                AssetDatabase.ImportAsset(themePath);
                panel.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(themePath);
                AssetDatabase.CreateAsset(panel, panelPath);
            }
            const string assetsPath = "Assets/Resources/Laya/DebuggerAssets.asset";
            var assets = AssetDatabase.LoadAssetAtPath<LayaDebuggerAssets>(assetsPath);
            if (assets == null)
            {
                assets = ScriptableObject.CreateInstance<LayaDebuggerAssets>();
                assets.profiles = profiles; assets.panel = panel;
                assets.layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/Resources/Laya/Debugger.uxml");
                assets.style = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/Resources/Laya/Debugger.uss");
                assets.koreanFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/ExternalAssets/Survival_package/Font/NotoSansKR-Bold.ttf");
                AssetDatabase.CreateAsset(assets, assetsPath);
            }
            AssetDatabase.SaveAssetIfDirty(assets);
        }

        [UPlaygroundTool("Laya/BuildDevelopment")]
        public static void BuildDevelopment()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Edit Mode 컴파일 완료 후 실행하세요.");
            const string directory = "Builds/LayaTraining_20261108";
            Directory.CreateDirectory(directory);
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            var previous = settings.BuildAddressablesWithPlayerBuild;
            try
            {
                settings.BuildAddressablesWithPlayerBuild = AddressableAssetSettings.PlayerBuildOption.DoNotBuildWithPlayer;
                var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes = new[] { "Assets/01.Scenes/Test/CombatTestMap.unity" },
                    locationPathName = directory + "/LayaTraining.exe", target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.Development | BuildOptions.CompressWithLz4 });
                var s = report.summary;
                Directory.CreateDirectory("Logs/LayaTraining");
                File.WriteAllText("Logs/LayaTraining/BuildReport.txt", $"{s.result}\nUnity {Application.unityVersion}\nErrors {s.totalErrors}\nWarnings {s.totalWarnings}\nBytes {s.totalSize}\nTime {s.totalTime}\n{s.outputPath}");
                if (s.result != BuildResult.Succeeded) throw new InvalidOperationException("Laya Development 빌드 실패: " + s.result);
            }
            finally { settings.BuildAddressablesWithPlayerBuild = previous; }
        }
    }
}
#endif
