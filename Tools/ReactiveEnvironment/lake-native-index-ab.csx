// 맑음에서 phase0 전용 인덱스/phase1 임시 MPB를 비교하고 원래 바인딩을 복원한다.
if (!UnityEngine.Application.isPlaying) return "Play 초기화 후 실행하세요.";
if (UnityEditor.Compilation.CompilationPipeline.codeOptimization != UnityEditor.Compilation.CodeOptimization.Debug)
    throw new System.InvalidOperationException("Debug 모드 측정만 허용합니다.");
string tag = "ab1";
string key = "LakeDebug.NativeAB";
string status = UnityEditor.SessionState.GetString(key, "");
if (status.StartsWith(tag)) return status;
if (System.IO.File.Exists("output/LakeVillagePerformance/Debug/" + tag + "-frames.csv")) return tag + "-existing";
var weather = UnityEngine.Object.FindFirstObjectByType<UPlayGround.Gameplay.World.LivingWorld.EnvironmentController>();
if (weather == null || !weather.IsReady) return "월드 초기화 대기";
var abRenderers = new System.Collections.Generic.List<UnityEngine.MeshRenderer>();
foreach(var renderer in UnityEngine.Object.FindObjectsByType<UnityEngine.MeshRenderer>(UnityEngine.FindObjectsSortMode.None))
    if((renderer.GetShaderUserValue() & 0x80000000u)!=0 && !renderer.HasPropertyBlock()) abRenderers.Add(renderer);
if(abRenderers.Count<1000) throw new System.InvalidOperationException("전용 바인딩 준비 필요");
bool overlayVisible = UPlayGround.UI.UIFrameRateOverlay.IsVisible;
UPlayGround.UI.UIFrameRateOverlay.SetVisible(true);
var display = UnityEngine.Object.FindFirstObjectByType<UPlayGround.UI.UIFrameRateDisplay>();
if (display == null) { UPlayGround.UI.UIFrameRateOverlay.SetVisible(overlayVisible); return "FPS 표시 초기화 대기"; }
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var label = (TMPro.TMP_Text)typeof(UPlayGround.UI.UIFrameRateDisplay).GetField("_text", flags).GetValue(display);
bool hadPreview = (bool)weather.GetType().GetField("_hasWeatherPreview", flags).GetValue(weather);
var saved = (UPlayGround.Gameplay.World.LivingWorld.EnvironmentSchedule.Sample)weather.GetType().GetField("_weatherPreview", flags).GetValue(weather);
var sample = weather.Current; sample.Rain = 0; sample.Storm = 0; sample.RainCloud = 0;
weather.SetWeatherPreview(sample);
UnityEngine.Profiling.Profiler.enabled = false;
UnityEditor.EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor")).Focus();
UnityEditor.SessionState.SetString(key, tag + "-running");
var frames = new System.Text.StringBuilder("phase,frame,ms\n");
var labels = new System.Text.StringBuilder("phase,frame,text\n");
int phase = 0, count = 0, lastFrame = -1, start = System.Math.Max(300, UnityEngine.Time.frameCount + 30);
string lastText = "";
double absoluteDeadline = UnityEditor.EditorApplication.timeSinceStartup + 120;
double deadline = absoluteDeadline;
UnityEditor.EditorApplication.CallbackFunction callback = null;
callback = delegate {
    bool finished = !UnityEngine.Application.isPlaying || weather == null || UnityEditor.EditorApplication.timeSinceStartup > deadline;
    if (!finished && UnityEngine.Time.frameCount != lastFrame) {
        lastFrame = UnityEngine.Time.frameCount;
        if (lastFrame < start) return;
        if (count == 0) deadline = UnityEditor.EditorApplication.timeSinceStartup + 45;
        frames.AppendLine(phase + "," + lastFrame + "," + (UnityEngine.Time.unscaledDeltaTime * 1000).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        string text = label != null ? label.text : "missing";
        if (text != lastText) { labels.AppendLine(phase + "," + lastFrame + "," + text); lastText = text; }
        count++;
        if (count >= 300) {
            phase++; count = 0; lastText = "";
            if (phase == 1) {
    var properties = new UnityEngine.MaterialPropertyBlock();
    foreach(var renderer in abRenderers) if(renderer!=null) {
        properties.Clear(); properties.SetFloat("_EnvironmentReactionIndex", (float)(renderer.GetShaderUserValue() & 0x7fffffffu));
        renderer.SetPropertyBlock(properties);
    }
    start = lastFrame + 45;
}
            else finished = true;
        }
    }
    if (!finished && UnityEditor.EditorApplication.timeSinceStartup <= absoluteDeadline) return;
    UnityEditor.EditorApplication.update -= callback;
    try {
        foreach(var renderer in abRenderers) if(renderer!=null) renderer.SetPropertyBlock(null);
        if (weather != null) { if (hadPreview) weather.SetWeatherPreview(saved); else weather.ClearWeatherPreview(); }
        UPlayGround.UI.UIFrameRateOverlay.SetVisible(overlayVisible);
        System.IO.File.WriteAllText("output/LakeVillagePerformance/Debug/" + tag + "-frames.csv", frames.ToString());
        System.IO.File.WriteAllText("output/LakeVillagePerformance/Debug/" + tag + "-ui.csv", labels.ToString());
    } finally { UnityEditor.SessionState.SetString(key, tag + "-done-phase" + phase); }
};
UnityEditor.EditorApplication.update += callback;
return tag + "-running";


