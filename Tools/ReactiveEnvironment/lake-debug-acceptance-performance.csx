// tag를 바꾸어 새 측정을 만든다. 안정화 중의 프레임만 제외하고 측정 시작 후 스파이크는 모두 기록한다.
const string tag = "acceptance-3";
const string key = "LakeDebug.AcceptanceCapture3";
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
var rendererData=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Rendering.Universal.UniversalRendererData>("Assets/10.Datas/World/AmbientFauna/Rendering/LakeVillage_RPAsset_Renderer_0.asset");
UnityEngine.Rendering.Universal.ScriptableRendererFeature ao=null,decal=null;
foreach(var f in rendererData.rendererFeatures){if(f.GetType().Name=="ScreenSpaceAmbientOcclusion")ao=f;if(f.GetType().Name=="DecalRendererFeature")decal=f;}
if(ao==null||decal==null||UnityEngine.Object.FindObjectsByType<UnityEngine.Rendering.Universal.DecalProjector>(UnityEngine.FindObjectsSortMode.None).Length!=0) return "기능/데칼 확인 필요";
if(pipeline.shadowDistance!=25 || pipeline.shadowCascadeCount!=1 || decal.isActive || ao.isActive) throw new System.InvalidOperationException("저장된 후보 설정이 필요합니다.");
var clock = UPlayGround.Manager.Services.Get<UPlayGround.Manager.IGameTimeService>() as UPlayGround.Manager.GameTimeManager;
if(clock==null) return "시계 초기화 대기";
float originalClock = clock.WorldClockMultiplier;
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

UnityEditor.SessionState.SetFloat(key+".OriginalClock",originalClock);
UnityEditor.SessionState.SetBool(key+".ClockRestorePending",true);

var sample = weather.Current; sample.Rain = 0; sample.Storm = 0; sample.RainCloud = 0;
weather.SetWeatherPreview(sample);
var metadata = new System.Text.StringBuilder();
metadata.AppendLine("utc=" + System.DateTime.UtcNow.ToString("O") + " mode=" + UnityEditor.Compilation.CompilationPipeline.codeOptimization);
metadata.AppendLine("savedProfile=true clockFrozen=false day="+clock.CurrentDay+" minute="+clock.MinuteOfDay+" originalRate="+originalClock);
metadata.AppendLine("scene=" + UnityEngine.SceneManagement.SceneManager.GetActiveScene().path + " targets=" + world.EnvironmentTargetCount);
metadata.AppendLine("camera=" + UnityEngine.Camera.main.pixelWidth + "x" + UnityEngine.Camera.main.pixelHeight + " scale=" + pipeline.renderScale + " msaa=" + pipeline.msaaSampleCount);
metadata.AppendLine("ao="+UnityEngine.JsonUtility.ToJson(ao));
metadata.AppendLine("shadows=" + pipeline.shadowDistance + "/" + pipeline.shadowCascadeCount + " occlusion=" + originalOcclusion);
metadata.AppendLine("maximized=false cap=" + UnityEngine.Application.targetFrameRate + " vsync=" + UnityEngine.QualitySettings.vSyncCount + " profiler=false debugger=" + System.Diagnostics.Debugger.IsAttached);
var frames = new System.Text.StringBuilder("phase,frame,ms\n");
var labels = new System.Text.StringBuilder("phase,frame,text\n");
int phase = 0, count = 0, lastFrame = -1, compilingFrames = 0;
int startFrame = UnityEngine.Time.frameCount + 600;
double now = UnityEditor.EditorApplication.timeSinceStartup;
double warmUntil = now + 30, absoluteDeadline = now + 420, deadline = absoluteDeadline;
string lastText = "";
UnityEditor.SessionState.SetString(key, tag + "-running");
UnityEditor.EditorApplication.CallbackFunction callback = null;
callback = delegate {
    double at = UnityEditor.EditorApplication.timeSinceStartup;
    bool finish = !UnityEngine.Application.isPlaying || weather == null || at > absoluteDeadline || at > deadline;
    if (!finish && UnityEngine.Time.frameCount != lastFrame) {
        lastFrame = UnityEngine.Time.frameCount;
        if (count == 0 && (lastFrame < startFrame || at < warmUntil || UnityEditor.ShaderUtil.anythingCompiling)) return;
        if (count == 0) { deadline = at + 60; metadata.AppendLine("phase="+phase+" activeOcclusion="+UnityEngine.Rendering.GPUResidentDrawer.IsInstanceOcclusionCullingEnabled()+" shadows="+pipeline.shadowDistance+"/"+pipeline.shadowCascadeCount+" scale="+pipeline.renderScale+" msaa="+pipeline.msaaSampleCount+" decal="+decal.isActive); }
        if (UnityEditor.ShaderUtil.anythingCompiling) compilingFrames++;
        frames.AppendLine(phase + "," + lastFrame + "," + (UnityEngine.Time.unscaledDeltaTime * 1000).ToString("R", System.Globalization.CultureInfo.InvariantCulture));
        string text = label != null ? label.text : "missing";
        if (text != lastText) { labels.AppendLine(phase + "," + lastFrame + "," + text); lastText = text; }
        if (++count >= 600) {
            metadata.AppendLine("phase=" + phase + " frames=" + count + " shaderCompilingFrames=" + compilingFrames+" minute="+clock.MinuteOfDay+" clockRate="+clock.WorldClockMultiplier);
            phase++; count = 0; compilingFrames = 0; lastText = "";
            if (phase == 1) {
                sample.Rain=1;sample.Storm=1;weather.SetWeatherPreview(sample);
                startFrame=lastFrame+600;warmUntil=at+30;deadline=absoluteDeadline;
            } else finish=true;
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
        
        if(clock!=null) clock.WorldClockMultiplier=originalClock;
        UnityEditor.SessionState.SetBool(key+".ClockRestorePending",false);
        UPlayGround.UI.UIFrameRateOverlay.SetVisible(originalOverlay);
        if (view != null) view.maximized = originalMaximized;
        UnityEditor.SessionState.SetBool(key + ".RestorePending", false);
        UnityEditor.SessionState.SetString(key, tag + "-done-phase" + phase);
    }
};
UnityEditor.EditorApplication.update += callback;
return tag + "-running";
