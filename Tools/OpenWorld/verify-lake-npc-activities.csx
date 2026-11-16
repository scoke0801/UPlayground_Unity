// 기존 일과의 짧은 관찰 기록. NPC 상태나 시간 진행 속도를 주입하지 않는다.
if (!EditorApplication.isPlaying) throw new Exception("Play Mode가 필요합니다.");
const string key = "LakeCamp.ShortObservation";
if (SessionState.GetBool(key, false)) throw new Exception("관찰이 이미 진행 중입니다.");
var routines = UnityEngine.Object.FindObjectsByType<UPlayGround.Components.LivingWorld.NpcRoutine>(FindObjectsSortMode.None)
    .Where(r => r.name == "RedTree_RefugeeA" || r.name == "RedTree_RefugeeB").ToArray();
if (routines.Length != 2) throw new Exception("피난민 일과 2개가 필요합니다.");
var rows = new List<string> { "time\tframe\tactor\tworking\ttarget\tposition" };
float start = Time.time, last = -1, observedSeconds = 0;
double deadline = EditorApplication.timeSinceStartup + 75;
SessionState.SetBool(key, true);
EditorApplication.CallbackFunction sample = null;
sample = delegate {
    bool complete = !EditorApplication.isPlaying || Time.time - start >= 45 || EditorApplication.timeSinceStartup >= deadline;
    if (complete) {
        EditorApplication.update -= sample;
        SessionState.SetBool(key, false);
        System.IO.Directory.CreateDirectory("output/NpcDailyAnimations/LakeFix");
        System.IO.File.WriteAllLines("output/NpcDailyAnimations/LakeFix/routine-observation.tsv", rows);
        System.IO.File.WriteAllText("output/NpcDailyAnimations/LakeFix/routine-observation-status.txt", "elapsedGameSeconds=" + observedSeconds + " rows=" + (rows.Count - 1)
            + " stoppedBy=" + (!EditorApplication.isPlaying ? "PlayModeExit" : Time.time - start >= 45 ? "Duration" : "Deadline"));
        return;
    }
    EditorApplication.QueuePlayerLoopUpdate();
    if (Time.time - last < 0.5f) return;
    last = Time.time;
    observedSeconds = Time.time - start;
    foreach (var r in routines) {
        if (r == null) continue;
        rows.Add((Time.time - start).ToString("F3") + "\t" + Time.frameCount + "\t" + r.name + "\t" + r.IsWorking
            + "\t" + (r.Target == null ? "none" : r.Target.name) + "\t" + r.ActorPosition.ToString("F3"));
    }
};
EditorApplication.update += sample;
return "45초 관찰 시작. 75초 제한 후 콜백 자동 해제.";
