// 논리 상태/저장 슬롯을 변경하지 않고 같은 물리 씬의 강수 차폐 결과만 전수 대조한다.
if (!UnityEngine.Application.isPlaying) return "Play 초기화 필요";
const string key = "LakeDebug.RainParityValidity";
const string reportPath = "output/LakeVillagePerformance/Debug/burst-rain-parity-validity.txt";
if (UnityEditor.SessionState.GetString(key, "") == "done") return System.IO.File.ReadAllText(reportPath);
var world = UnityEngine.Object.FindFirstObjectByType<UPlayGround.Manager.WorldStateManager>();
var weather = UnityEngine.Object.FindFirstObjectByType<UPlayGround.Gameplay.World.LivingWorld.EnvironmentController>();
if (world == null || weather == null || !weather.IsReady) return "초기화 대기";
if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "LakeVillageWorld") return "대상 씬 필요";
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var rain = (UPlayGround.Gameplay.World.ReactiveEnvironment.EnvironmentRainBatch)world.GetType().GetField("_environmentRain", flags).GetValue(world);
var bindings = (System.Collections.IList)world.GetType().GetField("_environmentBindings", flags).GetValue(world);
var effects = (UPlayGround.Gameplay.World.LivingWorld.EnvironmentEffects)weather.GetType().GetField("_effects", flags).GetValue(weather);
var targetField = bindings[0].GetType().GetField("target");
var targets = new UPlayGround.Gameplay.World.ReactiveEnvironment.IEnvironmentRainBatchTarget[bindings.Count];
for (int i = 0; i < bindings.Count; i++)
    targets[i] = targetField.GetValue(bindings[i]) as UPlayGround.Gameplay.World.ReactiveEnvironment.IEnvironmentRainBatchTarget;
var setter = weather.GetType().GetProperty("Current").GetSetMethod(true);
var original = weather.Current; var sample = original; sample.Rain = 1;
bool paused = UnityEditor.EditorApplication.isPaused;
var report = new System.Text.StringBuilder();
UnityEditor.SessionState.SetString(key, "running");
UnityEditor.EditorApplication.isPaused = true;
try {
    setter.Invoke(weather, new object[] { sample });
    var expectedPositions = new System.Collections.Generic.List<UnityEngine.Vector3>();
    foreach (var target in targets) {
        var obj = target as UPlayGround.Gameplay.World.ReactiveEnvironment.ReactiveEnvironmentObject;
        if (obj != null) {
            var anchors = (UnityEngine.Transform[])obj.GetType().GetField("_rainSamples", flags).GetValue(obj);
            foreach (var anchor in anchors)
                if (anchor != null && anchor.gameObject.activeInHierarchy) expectedPositions.Add(anchor.position);
        } else {
            var reaction = target as UPlayGround.Gameplay.World.ReactiveEnvironment.IEnvironmentReactionTarget;
            if (reaction != null) expectedPositions.Add(reaction.ReactionPosition);
        }
    }
    var watch = System.Diagnostics.Stopwatch.StartNew();
    using (rain.Begin()) {
        foreach (var target in targets) {
            if (target != null) target.CollectRainSamples(rain);
        }
        double collect = watch.Elapsed.TotalMilliseconds;
        rain.Complete();
        double completed = watch.Elapsed.TotalMilliseconds;
        var requests = (System.Collections.IList)rain.GetType().GetField("_requests", flags).GetValue(rain);
        int mismatches = 0, sheltered = 0, positionDifferences = 0;
        float maximumError = 0;
        if (expectedPositions.Count != requests.Count) throw new System.InvalidOperationException("직접 위치/쿼리 수 불일치");
        var requestType = requests[0].GetType();
        var position = requestType.GetField("position"); var receiver = requestType.GetField("receiver");
        var result = requestType.GetField("sheltered");
        for (int i = 0; i < requests.Count; i++) {
            var request = requests[i];
            bool actual = (bool)result.GetValue(request);
            float error = UnityEngine.Vector3.Distance(expectedPositions[i], (UnityEngine.Vector3)position.GetValue(request));
            maximumError = UnityEngine.Mathf.Max(maximumError, error);
            if (error > 0.0001f) positionDifferences++;
            bool expected = effects.IsRainSheltered(expectedPositions[i], (UnityEngine.Transform)receiver.GetValue(request));
            if (actual) sheltered++;
            if (actual != expected) { mismatches++; if (mismatches <= 8) report.AppendLine("차이 index=" + i); }
        }
        report.AppendLine("targets=" + bindings.Count + " queries=" + requests.Count + " sheltered=" + sheltered + " differences=" + mismatches);
        report.AppendLine("positionDifferences=" + positionDifferences + " maximumPositionError=" + maximumError);
        report.AppendLine("collectMs=" + collect.ToString("F3") + " completeMs=" + (completed - collect).ToString("F3") + " totalWithDirectReferenceMs=" + watch.Elapsed.TotalMilliseconds.ToString("F3"));
        if (requests.Count < 18000 || mismatches != 0 || positionDifferences != 0) throw new System.InvalidOperationException(report.ToString());
    }
    UnityEditor.SessionState.SetString(key, "done");
    return report.ToString();
} catch (System.Exception error) {
    UnityEditor.SessionState.SetString(key, "failed"); report.AppendLine(error.ToString()); throw;
} finally {
    setter.Invoke(weather, new object[] { original });
    UnityEditor.EditorApplication.isPaused = paused;
    System.IO.File.WriteAllText(reportPath, report.ToString());
}
