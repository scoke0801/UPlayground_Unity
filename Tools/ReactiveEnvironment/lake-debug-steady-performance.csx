// tag를 바꾸어 새 측정을 만든다. 안정화 중의 프레임만 제외하고 측정 시작 후 스파이크는 모두 기록한다.
const string tag = "steady-off-1";
const string key = "LakeDebug.SteadyCapture";
if (!UnityEngine.Application.isPlaying) return "Play 초기화 필요";
if (UnityEditor.Compilation.CompilationPipeline.codeOptimization != UnityEditor.Compilation.CodeOptimization.Debug)
    throw new System.InvalidOperationException("Debug 모드가 필요합니다.");
if (UnityEditor.SessionState.GetString(key, "").StartsWith(tag)) return UnityEditor.SessionState.GetString(key, "");
string folder = "output/LakeVillagePerformance/Debug/";
System.IO.Directory.CreateDirectory(folder);
if (System.IO.File.Exists(folder + tag + "-frames.csv")) return "기존 측정 보존: " + tag;
var weather = UnityEngine.Object.FindFirstObjectByType<UPlayGround.Gameplay.World.LivingWorld.EnvironmentController>();
var world = UnityEngine.Object.FindFirstObjectByType<UPlayGround.Manager.WorldStateManager>();
if (weather == null || !weather.IsReady || world == null) return "월드 초기화 대기";
var pipeline = (UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset)UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
bool hadPreview = (bool)weather.GetType().GetField("_hasWeatherPreview", flags).GetValue(weather);
var savedWeather = (UPlayGround.Gameplay.World.LivingWorld.EnvironmentSchedule.Sample)weather.GetType().GetField("_weatherPreview", flags).GetValue(weather);
bool originalOcclusion = pipeline.gpuResidentDrawerEnableOcclusionCullingInCameras;
bool originalOverlay = UPlayGround.UI.UIFrameRateOverlay.IsVisible;
UPlayGround.UI.UIFrameRateOverlay.SetVisible(true);
var display = UnityEngine.Object.FindFirstObjectByType<UPlayGround.UI.UIFrameRateDisplay>();
if (display == null) { UPlayGround.UI.UIFrameRateOverlay.SetVisible(originalOverlay); return "FPS 표시 준비 대기"; }
var label = (TMPro.TMP_Text)display.GetType().GetField("_text", flags).GetValue(display);
var view = UnityEditor.EditorWindow.GetWindow(System.Type.GetType("UnityEditor.GameView,UnityEditor"));
bool originalMaximized = view.maximized;
view.maximized = false; view.Focus();
UnityEngine.Profiling.Profiler.enabled = false;
UnityEditor.SessionState.SetBool(key + ".OriginalOcclusion", originalOcclusion);
UnityEditor.SessionState.SetBool(key + ".RestorePending", true);
pipeline.gpuResidentDrawerEnableOcclusionCullingInCameras = false;
var sample = weather.Current; sample.Rain = 0; sample.Storm = 0; sample.RainCloud = 0;
weather.SetWeatherPreview(sample);
var metadata = new System.Text.StringBuilder();
metadata.AppendLine("utc=" + System.DateTime.UtcNow.ToString("O") + " mode=" + UnityEditor.Compilation.CompilationPipeline.codeOptimization);
metadata.AppendLine("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + " targets=" + world.EnvironmentTargetCount);
metadata.AppendLine("camera=" + UnityEngine.Camera.main.pixelWidth + "x" + UnityEngine.Camera.main.pixelHeight + " scale=" + pipeline.renderScale + " msaa=" + pipeline.msaaSampleCount);
metadata.AppendLine("shadows=" + pipeline.shadowDistance + "/" + pipeline.shadowCascadeCount + " occlusion=false original=" + originalOcclusion);
metadata.AppendLine("maximized=false cap=" + UnityEngine.Application.targetFrameRate + " vsync=" + UnityEngine.QualitySettings.vSyncCount + " profiler=false debugger=" + System.Diagnostics.Debugger.IsAttached);
var frames = new System.Text.StringBuilder("phase,frame,ms\n");
var labels = new System.Text.StringBuilder("phase,frame,text\n");
int phase = 0, count = 0, lastFrame = -1, compilingFrames = 0;
int startFrame = UnityEngine.Time.frameCount + 600;
double now = UnityEditor.EditorApplication.timeSinceStartup;
double warmUntil = now + 30, absoluteDeadline = now + 300, deadline = absoluteDeadline;
string lastText = "";
UnityEditor.SessionState.SetString(key, tag + "-running");
UnityEditor.EditorApplication.CallbackFunction callback = null;
callback = delegate {
    double at = UnityEditor.EditorApplication.timeSinceStartup;
    bool finish = !UnityEngine.Application.isPlaying || weather == null || at > absoluteDeadline || at > deadline;
    if (!finish && UnityEngine.Time.frameCount != lastFrame) {
        lastFrame = UnityEngine.Time.frameCount;
        if (count == 0 && (lastFrame < startFrame || at < warmUntil || UnityEditor.ShaderUtil.anythingCompiling)) return;
        if (count == 0) deadline = at + 60;
        if (UnityEditor.ShaderUtil.anythingCompiling) compilingFrames++;
        frames.AppendLine(phase + "," + lastFrame + "," + (UnityEngine.Time.unscaledDeltaTime * 1000).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        string text = label != null ? label.text : "missing";
        if (text != lastText) { labels.AppendLine(phase + "," + lastFrame + "," + text); lastText = text; }
        if (++count >= 300) {
            metadata.AppendLine("phase=" + phase + " frames=" + count + " shaderCompilingFrames=" + compilingFrames);
            phase++; count = 0; compilingFrames = 0; lastText = "";
            if (phase == 1) {
                sample.Rain = 1; sample.Storm = 1; weather.SetWeatherPreview(sample);
                startFrame = lastFrame + 600; warmUntil = at + 30; deadline = absoluteDeadline;
            } else finish = true;
        }
    }
    if (!finish) return;
    UnityEditor.EditorApplication.update -= callback;
    try {
        metadata.AppendLine("finishedPhase=" + phase + " incompleteFrames=" + count + " finalMode=" + UnityEditor.Compilation.CompilationPipeline.codeOptimization);
        System.IO.File.WriteAllText(folder + tag + "-frames.csv", frames.ToString());
        System.IO.File.WriteAllText(folder + tag + "-ui.csv", labels.ToString());
        System.IO.File.WriteAllText(folder + tag + "-metadata.txt", metadata.ToString());
    } finally {
        if (weather != null) { if (hadPreview) weather.SetWeatherPreview(savedWeather); else weather.ClearWeatherPreview(); }
        pipeline.gpuResidentDrawerEnableOcclusionCullingInCameras = originalOcclusion;
        UPlayGround.UI.UIFrameRateOverlay.SetVisible(originalOverlay);
        if (view != null) view.maximized = originalMaximized;
        UnityEditor.SessionState.SetBool(key + ".RestorePending", false);
        UnityEditor.SessionState.SetString(key, tag + "-done-phase" + phase);
    }
};
UnityEditor.EditorApplication.update += callback;
return tag + "-running";
