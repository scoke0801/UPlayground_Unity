// execute_code에 본문을 전달한다. 현재 상태만 읽고 씬·시계·재질을 변경하지 않는다.
var text = new System.Text.StringBuilder();
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
text.AppendLine("scene=" + scene.path + " dirty=" + scene.isDirty);
text.AppendLine("play=" + UnityEditor.EditorApplication.isPlaying + " paused=" + UnityEditor.EditorApplication.isPaused);
text.AppendLine("realtime=" + UnityEngine.Time.realtimeSinceStartupAsDouble + " timeScale=" + UnityEngine.Time.timeScale);
UPlayGround.Manager.IGameTimeService time;
bool hasTime = UPlayGround.Manager.Services.TryGet<UPlayGround.Manager.IGameTimeService>(out time);
text.AppendLine("timeService=" + hasTime);
if (hasTime)
    text.AppendLine("day=" + time.CurrentDay + " minute=" + time.MinuteOfDay);
var manager = time as UPlayGround.Manager.GameTimeManager;
if (manager != null)
    text.AppendLine("clockMultiplier=" + manager.WorldClockMultiplier + " gamePaused=" + manager.IsPaused
        + " minutesPerSecond=" + (manager.Settings != null ? manager.Settings.gameMinutesPerRealSecond : 1)
        + " pauseStopsWorld=" + (manager.Settings == null || manager.Settings.pauseStopsWorldTime));
var material = UnityEngine.RenderSettings.skybox;
text.AppendLine("sky=" + (material != null ? material.name : "null"));
if (material != null)
{
    foreach (string property in new[] { "_CloudClock", "_UseCloudClock", "_CloudMotionScale", "_CloudFlowMultiplier",
        "_CloudScale", "_CloudHeight", "_HighCloudSpeed", "_CloudEvolution", "_SphericalProjection" })
        if (material.HasProperty(property)) text.AppendLine(property + "=" + material.GetFloat(property));
    foreach (string property in new[] { "_CloudVelocity", "_CloudWindOffset" })
        if (material.HasProperty(property)) text.AppendLine(property + "=" + material.GetVector(property).ToString("F6"));
}
return text.ToString();
