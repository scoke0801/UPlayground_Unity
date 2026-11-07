// 합성 입력에 실제 점수 계산 코어를 적용한다. 전투나 난수 선택은 실행하지 않는다.
if (EditorApplication.isPlaying || EditorApplication.isCompiling)
    throw new System.InvalidOperationException("Stop Play Mode and wait for compilation.");
var jsonType = System.Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json", true);
var serialize = jsonType.GetMethod("SerializeObject", new[] { typeof(object) });
var deserialize = jsonType.GetMethod("DeserializeObject", new[] { typeof(string), typeof(System.Type) });
System.Func<object, System.Collections.Generic.Dictionary<string, object>> dictionary = delegate(object value)
{
    var text = value as string ?? (string)serialize.Invoke(null, new[] { value });
    return (System.Collections.Generic.Dictionary<string, object>)deserialize.Invoke(null, new object[] { text, typeof(System.Collections.Generic.Dictionary<string, object>) });
};
var results = new System.Collections.Generic.List<object>();
var names = System.Enum.GetNames(typeof(UPlayGround.AI.CombatDecision.CombatIntent));
var compute = typeof(UPlayGround.AI.CombatDecision.IntentScoreComputer).GetMethod("Compute");
foreach (var file in System.IO.Directory.GetFiles("Tools/LayaWorker/enemy-data/examples", "*.json"))
{
    var snapshot = dictionary(System.IO.File.ReadAllText(file));
    var actorId = (string)snapshot["actor_id"];
    var def = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDefinitionSO>("Assets/10.Datas/Actor/DataBase/" + actorId + ".asset");
    if (def == null || def.EffectiveBehaviorData.intentWeights == null || def.EffectiveBehaviorData.phases.Length != 0 || def.EffectiveBehaviorData.aiRole.ToString() != "Melee")
        throw new System.InvalidOperationException("Fixture scope changed: " + actorId);
    var context = dictionary(snapshot["context"]);
    if (System.Convert.ToInt32(snapshot["phase_index"]) != -1 || (string)snapshot["last_intent"] != "None" || System.Convert.ToSingle(context["PredictionConfidence"]) != 0f)
        throw new System.InvalidOperationException("Fixture has non-neutral modifiers.");
    var group = dictionary(snapshot["group_bias"]);
    foreach (var pair in group)
        if (System.Convert.ToSingle(pair.Value) != (pair.Key.EndsWith("Multiplier") ? 1f : 0f))
            throw new System.InvalidOperationException("Fixture group is non-neutral.");
    object boxed = new UPlayGround.AI.CombatDecision.IntentEvaluationContext();
    foreach (var field in boxed.GetType().GetFields())
    {
        if (!context.ContainsKey(field.Name)) throw new System.InvalidOperationException("Missing context: " + field.Name);
        field.SetValue(boxed, field.FieldType.IsEnum
            ? System.Enum.Parse(field.FieldType, (string)context[field.Name])
            : System.Convert.ChangeType(context[field.Name], field.FieldType, System.Globalization.CultureInfo.InvariantCulture));
    }
    var scores = new System.Collections.Generic.Dictionary<string, float>();
    var rawScores = new System.Collections.Generic.Dictionary<string, float>();
    foreach (var name in names)
    {
        var intent = (UPlayGround.AI.CombatDecision.CombatIntent)System.Enum.Parse(typeof(UPlayGround.AI.CombatDecision.CombatIntent), name);
        var entry = def.EffectiveBehaviorData.intentWeights.GetEntry(intent);
        var score = (float)compute.Invoke(null, new[] { (object)entry, boxed });
        rawScores[name] = score; scores[name] = Mathf.Clamp01(score);
    }
    var sorted = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<string, float>>(scores);
    sorted.Sort(delegate(System.Collections.Generic.KeyValuePair<string, float> a, System.Collections.Generic.KeyValuePair<string, float> b) { return b.Value.CompareTo(a.Value); });
    var pool = new System.Collections.Generic.List<string>();
    for (int i = 0; i < 3; i++) pool.Add(sorted[i].Key);
    results.Add(new { request_id = snapshot["request_id"], actor_id = actorId, context = context,
        base_scores = rawScores, clamped_scores = scores, candidate_pool = pool,
        boundary_tie = sorted[2].Value == sorted[3].Value,
        scope = "native_score_core_neutral_modifiers_synthetic_context_not_BT_execution" });
}
System.IO.File.WriteAllText("Tools/LayaWorker/enemy-data/native-score-fixtures.json", (string)serialize.Invoke(null, new object[] { results }), new System.Text.UTF8Encoding(false));
return new { fixtures = results.Count, play_mode_executed = false, random_selection_executed = false };
