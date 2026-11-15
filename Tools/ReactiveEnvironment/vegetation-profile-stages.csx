if (!UnityEngine.Application.isPlaying || UnityEditor.SessionState.GetString("VegetationConversion.PlayOwner", "") != "20261114") return "검증 Play 소유 확인 필요";
var world = UnityEngine.Object.FindFirstObjectByType<UPlayGround.Manager.WorldStateManager>();
if (world == null) return "작성자 대기";
UnityEditor.EditorApplication.isPaused = true;
var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
var export = typeof(UPlayGround.Manager.WorldStateManager).GetMethod("ExportEnvironment", flags);
var import = typeof(UPlayGround.Manager.WorldStateManager).GetMethod("ImportEnvironment", flags);
var prepare = typeof(UPlayGround.Manager.WorldStateManager).GetMethod("PrepareEnvironmentSources", flags);
var source = typeof(UPlayGround.Manager.WorldStateManager).GetMethod("ApplyEnvironmentSources", flags);
var rainBatch = (UPlayGround.Gameplay.World.ReactiveEnvironment.EnvironmentRainBatch)typeof(UPlayGround.Manager.WorldStateManager).GetField("_environmentRain", flags).GetValue(world);
var baseline = new UPlayGround.Data.Save.WorldStateSaveData();
export.Invoke(world, new object[] { baseline });
var bindings = (System.Collections.IList)typeof(UPlayGround.Manager.WorldStateManager).GetField("_environmentBindings", flags).GetValue(world);
int count = bindings.Count;
var targets = new UPlayGround.Gameplay.World.ReactiveEnvironment.IEnvironmentReactionTarget[count];
var reactions = new UPlayGround.Core.ReactiveEnvironment.EnvironmentReaction[count];
var records = new UPlayGround.Data.Save.ReactiveEnvironmentSaveEntry[count];
var rain = new float[count]; var exposure = new float[count];
var type = bindings[0].GetType();
var targetField = type.GetField("target"); var reactionField = type.GetField("reaction"); var recordField = type.GetField("record");
for (int i = 0; i < count; i++)
{
    targets[i] = (UPlayGround.Gameplay.World.ReactiveEnvironment.IEnvironmentReactionTarget)targetField.GetValue(bindings[i]);
    reactions[i] = (UPlayGround.Core.ReactiveEnvironment.EnvironmentReaction)reactionField.GetValue(bindings[i]);
    records[i] = (UPlayGround.Data.Save.ReactiveEnvironmentSaveEntry)recordField.GetValue(bindings[i]);
}
var report = new System.Text.StringBuilder("targets=" + count + "\n");
try
{
    var actualTick = typeof(UPlayGround.Manager.WorldStateManager).GetMethod("TickEnvironment", flags);
    for (int run = 0; run < 3; run++)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        actualTick.Invoke(world, new object[] { 0.2f });
        report.AppendLine("actualTickMs=" + clock.Elapsed.TotalMilliseconds.ToString("F2"));
    }
    for (int run = 0; run < 3; run++)
    {
        var clock = System.Diagnostics.Stopwatch.StartNew();
        prepare.Invoke(world, new object[] { 0.2f });
        double prepared = clock.Elapsed.TotalMilliseconds;
        if (world.EnvironmentSourceCount > 0)
            for (int i = 0; i < count; i++) source.Invoke(world, new object[] { bindings[i], 0.2f });
        double sourced = clock.Elapsed.TotalMilliseconds;
        double collected, completed, sampled, reacted, captured;
        using (rainBatch.Begin())
        {
            for (int i = 0; i < count; i++)
                ((UPlayGround.Gameplay.World.ReactiveEnvironment.IEnvironmentRainBatchTarget)targets[i]).CollectRainSamples(rainBatch);
            collected = clock.Elapsed.TotalMilliseconds;
            rainBatch.Complete();
            completed = clock.Elapsed.TotalMilliseconds;
            for (int i = 0; i < count; i++) targets[i].SampleRain(out rain[i], out exposure[i]);
            sampled = clock.Elapsed.TotalMilliseconds;
            for (int i = 0; i < count; i++) reactions[i].Tick(0.2f, rain[i], exposure[i]);
            reacted = clock.Elapsed.TotalMilliseconds;
            for (int i = 0; i < count; i++)
            {
                records[i].state = reactions[i].State;
                reactions[i].GetPendingInputs(out records[i].pendingHeat, out records[i].pendingWater, out records[i].pendingContinuousWater);
            }
            captured = clock.Elapsed.TotalMilliseconds;
        }
        for (int i = 0; i < count; i++) targets[i].ApplySnapshot(records[i].state, records[i].destruction);
        double presented = clock.Elapsed.TotalMilliseconds;
        float rainMaximum = 0; int exposed = 0;
        for (int i = 0; i < count; i++) { rainMaximum = UnityEngine.Mathf.Max(rainMaximum, rain[i]); if (exposure[i] > 0) exposed++; }
        report.AppendLine("rainMax=" + rainMaximum + " exposed=" + exposed);
        report.AppendLine("run=" + run + " sources=" + world.EnvironmentSourceCount + " prepareMs=" + prepared.ToString("F2") + " sourceReflectionMs=" + (sourced-prepared).ToString("F2") + " rainCollectMs=" + (collected-sourced).ToString("F2") + " rainJobMs=" + (completed-collected).ToString("F2") + " rainResolveMs=" + (sampled-completed).ToString("F2") + " logicMs=" + (reacted-sampled).ToString("F2") + " captureMs=" + (captured-reacted).ToString("F2") + " presentationMs=" + (presented-captured).ToString("F2") + " totalMs=" + presented.ToString("F2"));
    }
    return report.ToString();
}
finally
{
    import.Invoke(world, new object[] { baseline });
    report.AppendLine("환경 상태 복원 완료. 단계 분해용 직접 호출이며 기능/FPS 인수 결과가 아님.");
    System.IO.File.WriteAllText("output/ReactiveEnvironment/VegetationConversion-20261114/runtime-stage-cost.txt", report.ToString());
}
