// Unity MCP execute_code의 메서드 본문으로 실행하는 읽기 전용 내보내기다.
if (EditorApplication.isPlaying || EditorApplication.isCompiling)
    throw new System.InvalidOperationException("Stop Play Mode and wait for compilation before export.");
var output = "Tools/LayaWorker/enemy-data";
System.IO.Directory.CreateDirectory(output);
var queue = new System.Collections.Generic.Queue<UnityEngine.Object>();
var visited = new System.Collections.Generic.HashSet<string>();
var nodes = new System.Collections.Generic.Dictionary<string, object>();
var leaves = new System.Collections.Generic.Dictionary<string, object>();
var actors = new System.Collections.Generic.List<object>();
var problems = new System.Collections.Generic.List<object>();
System.Func<UnityEngine.Object, string> identity = delegate(UnityEngine.Object obj)
{
    if (obj == null) return null;
    string guid; long fileId;
    if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out guid, out fileId))
        throw new System.InvalidOperationException("Non-asset object: " + obj.name);
    return guid + ":" + fileId.ToString(System.Globalization.CultureInfo.InvariantCulture);
};
System.Func<UnityEngine.Object, object> reference = delegate(UnityEngine.Object obj)
{
    if (obj == null) return null;
    var path = AssetDatabase.GetAssetPath(obj);
    if (string.IsNullOrEmpty(path)) return new System.Collections.Generic.Dictionary<string, object> { { "kind", "prefab_local_reference" }, { "type", obj.GetType().FullName } };
    var id = identity(obj);
    var value = new System.Collections.Generic.Dictionary<string, object>();
    value["id"] = id; value["path"] = path; value["type"] = obj.GetType().FullName;
    if (obj is ScriptableObject && path.StartsWith("Assets/10.Datas/", System.StringComparison.Ordinal))
    {
        value["kind"] = "data";
        queue.Enqueue(obj);
    }
    else
    {
        value["kind"] = "metadata_only";
        if (obj is AnimationClip)
        {
            var clip = (AnimationClip)obj;
            value["length_s"] = clip.length; value["frame_rate"] = clip.frameRate; value["is_looping"] = clip.isLooping;
        }
        leaves[id] = value;
    }
    return value;
};
System.Func<UnityEngine.Object, System.Collections.Generic.Dictionary<string, object>> fields = delegate(UnityEngine.Object obj)
{
    var values = new System.Collections.Generic.Dictionary<string, object>();
    var serialized = new SerializedObject(obj);
    var p = serialized.GetIterator();
    bool enterChildren = true;
    while (p.Next(enterChildren))
    {
        enterChildren = p.propertyType == SerializedPropertyType.Generic || p.propertyType == SerializedPropertyType.ManagedReference;
        if (p.propertyPath == "m_Script") continue;
        // 보상·대화는 판단 의존성이 아니며 아이템 데이터 전체로 그래프가 확장되는 것을 막는다.
        if (p.propertyPath == "dropTable" || p.propertyPath == "npcData") continue;
        switch (p.propertyType)
        {
            case SerializedPropertyType.Integer: values[p.propertyPath] = p.longValue; break;
            case SerializedPropertyType.Boolean: values[p.propertyPath] = p.boolValue; break;
            case SerializedPropertyType.Float: values[p.propertyPath] = p.doubleValue; break;
            case SerializedPropertyType.String: values[p.propertyPath] = p.stringValue; break;
            case SerializedPropertyType.Enum:
                var enumValue = new System.Collections.Generic.Dictionary<string, object>();
                enumValue["value"] = p.intValue;
                enumValue["name"] = p.enumValueIndex >= 0 && p.enumValueIndex < p.enumNames.Length ? p.enumNames[p.enumValueIndex] : null;
                values[p.propertyPath] = enumValue; break;
            case SerializedPropertyType.ObjectReference:
                if (p.objectReferenceValue == null && p.objectReferenceInstanceIDValue != 0)
                    problems.Add("Missing object reference: " + obj.name + "/" + p.propertyPath);
                values[p.propertyPath] = reference(p.objectReferenceValue); break;
            case SerializedPropertyType.Vector3:
                values[p.propertyPath] = new System.Collections.Generic.Dictionary<string, object> { { "x", p.vector3Value.x }, { "y", p.vector3Value.y }, { "z", p.vector3Value.z } }; break;
            case SerializedPropertyType.Vector2:
                values[p.propertyPath] = new System.Collections.Generic.Dictionary<string, object> { { "x", p.vector2Value.x }, { "y", p.vector2Value.y } }; break;
            case SerializedPropertyType.ManagedReference:
                values[p.propertyPath + ".$type"] = p.managedReferenceFullTypename;
                if (string.IsNullOrEmpty(p.managedReferenceFullTypename) && p.managedReferenceId >= 0)
                    problems.Add("Missing managed type: " + obj.name + "/" + p.propertyPath);
                break;
        }
        if (p.isArray && p.propertyType != SerializedPropertyType.String)
            values[p.propertyPath + ".$count"] = p.arraySize;
    }
    return values;
};
var actorPaths = new[] { "Assets/10.Datas/Actor/DataBase/Golem_Normal.asset", "Assets/10.Datas/Actor/DataBase/Ent_Normal.asset" };
foreach (var path in actorPaths)
{
    var def = AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDefinitionSO>(path);
    if (def == null || def.prefab == null || def.EffectiveBehaviorData == null || def.EffectiveAbilitySet == null)
        throw new System.InvalidOperationException("Required actor connection missing: " + path);
    var actor = new System.Collections.Generic.Dictionary<string, object>();
    actor["actor_id"] = def.actorId; actor["definition"] = reference(def);
    actor["level"] = def.EffectiveLevel; actor["grade"] = def.EffectiveGrade.ToString();
    actor["behavior"] = reference(def.EffectiveBehaviorData);
    actor["ability_set"] = reference(def.EffectiveAbilitySet);
    actor["scaling"] = reference(def.EffectiveMonsterScaling);
    actor["attribute_profile"] = reference(def.attributeProfile);
    actor["poise"] = reference(def.poiseData); actor["break_gauge"] = reference(def.EffectiveBreakGaugeData);
    var policy = def.EffectiveCombatReactionPolicy;
    if (policy == null || policy.GetRule(def.EffectiveGrade) == null)
        policy = Resources.Load<UPlayGround.Data.Combat.CombatReactionPolicySO>("DefaultCombatReactionPolicy");
    actor["reaction_policy"] = reference(policy);
    actor["defense_policy"] = reference(def.EffectiveCombatDefensePolicy);
    var components = new System.Collections.Generic.List<object>();
    var motionSets = new System.Collections.Generic.List<UPlayGround.Data.Actor.Animation.ActorAnimationMotionSet>();
    foreach (var component in def.prefab.GetComponentsInChildren<Component>(true))
    {
        if (component == null) { problems.Add("Missing prefab component: " + path); continue; }
        var typeName = component.GetType().FullName;
        if (!typeName.StartsWith("UPlayGround.", System.StringComparison.Ordinal) && !typeName.StartsWith("KinematicCharacterController.", System.StringComparison.Ordinal) && !(component is CapsuleCollider)) continue;
        var componentFields = fields(component);
        var row = new System.Collections.Generic.Dictionary<string, object>(); row["type"] = typeName;
        row["hierarchy_path"] = AnimationUtility.CalculateTransformPath(component.transform, def.prefab.transform);
        row["properties"] = componentFields; components.Add(row);
        var serialized = new SerializedObject(component); var property = serialized.GetIterator();
        while (property.Next(true))
            if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue is UPlayGround.Data.Actor.Animation.ActorAnimationMotionSet)
            {
                var set = (UPlayGround.Data.Actor.Animation.ActorAnimationMotionSet)property.objectReferenceValue;
                if (!motionSets.Contains(set)) motionSets.Add(set);
            }
    }
    actor["prefab_components"] = components;
    var setRefs = new System.Collections.Generic.List<object>(); foreach (var set in motionSets) setRefs.Add(reference(set));
    actor["motion_sets"] = setRefs;
    var abilities = new System.Collections.Generic.List<object>();
    foreach (var ability in def.EffectiveAbilitySet.EnumerateAll())
    {
        if (ability == null) { problems.Add("Null effective ability: " + def.actorId); continue; }
        var abilityRow = new System.Collections.Generic.Dictionary<string, object>(); abilityRow["ability_id"] = ability.abilityId; abilityRow["asset"] = reference(ability);
        var variants = new System.Collections.Generic.List<object>();
        foreach (var variant in ability.variants)
        {
            if (variant == null) { problems.Add("Null variant: " + ability.abilityId); continue; }
            var row = new System.Collections.Generic.Dictionary<string, object>(); row["variant_id"] = variant.variantId; row["payload"] = reference(variant.executionPayload);
            var payload = variant.executionPayload as UPlayGround.Ability.UPlayGround.UPlayGroundMotionAbilityPayloadSO;
            if (payload != null && payload.attackInfo != null)
            {
                var info = payload.attackInfo; row["motion_key"] = info.motionKey.value;
                row["ai_selectable"] = info.aiSelectable; row["motion_pending"] = info.isMotionContentPending;
                var matches = new System.Collections.Generic.List<object>();
                var found = new System.Collections.Generic.HashSet<string>();
                foreach (var set in motionSets)
                {
                    var motion = set.GetAbilityMotionAsset(info.motionKey);
                    if (motion != null && found.Add(identity(motion))) matches.Add(reference(motion));
                }
                row["resolved_motions"] = matches;
                if (info.aiSelectable && !info.isMotionContentPending && matches.Count != 1)
                    problems.Add("Expected exactly one motion: " + def.actorId + "/" + ability.abilityId + "/" + variant.variantId + ":" + matches.Count);
            }
            variants.Add(row);
        }
        abilityRow["variants"] = variants; abilities.Add(abilityRow);
    }
    actor["effective_abilities"] = abilities;
    actors.Add(actor);
}
foreach (var name in new[] { "CombatEncounterProfile", "CombatBalanceTargets", "DefaultCombatReactionPolicy" })
{
    var obj = Resources.Load<ScriptableObject>(name);
    if (obj == null) problems.Add("Missing resource: " + name); else reference(obj);
}
while (queue.Count > 0)
{
    var obj = queue.Dequeue(); var id = identity(obj);
    if (!visited.Add(id)) continue;
    if (visited.Count > 2000) throw new System.InvalidOperationException("Dependency graph exceeds selected scope.");
    var node = new System.Collections.Generic.Dictionary<string, object>(); node["id"] = id; node["path"] = AssetDatabase.GetAssetPath(obj);
    node["type"] = obj.GetType().FullName; node["name"] = obj.name; node["properties"] = fields(obj);
    var script = MonoScript.FromScriptableObject((ScriptableObject)obj);
    node["script_path"] = script != null ? AssetDatabase.GetAssetPath(script) : null;
    nodes[id] = node;
}
var document = new System.Collections.Generic.Dictionary<string, object>();
document["schema_version"] = 1; document["scope"] = "selected_enemy_decision_static_graph";
document["unity_version"] = Application.unityVersion; document["exported_at_utc"] = System.DateTime.UtcNow.ToString("o");
document["actors"] = actors; document["objects"] = nodes; document["metadata_only"] = leaves; document["problems"] = problems;
var contextFields = new System.Collections.Generic.List<object>();
foreach (var field in typeof(UPlayGround.AI.CombatDecision.IntentEvaluationContext).GetFields())
{
    var descriptor = new System.Collections.Generic.Dictionary<string, object>();
    descriptor["name"] = field.Name; descriptor["type"] = field.FieldType.Name;
    descriptor["enum_values"] = field.FieldType.IsEnum ? System.Enum.GetNames(field.FieldType) : null;
    contextFields.Add(descriptor);
}
document["context_fields"] = contextFields;
document["intent_names"] = System.Enum.GetNames(typeof(UPlayGround.AI.CombatDecision.CombatIntent));
var jsonType = System.Type.GetType("Newtonsoft.Json.JsonConvert, Newtonsoft.Json", true);
var serialize = jsonType.GetMethod("SerializeObject", new[] { typeof(object) });
System.IO.File.WriteAllText(output + "/unity-export.json", (string)serialize.Invoke(null, new object[] { document }), new System.Text.UTF8Encoding(false));
return new { actors = actors.Count, objects = nodes.Count, metadata_only = leaves.Count, problems = problems.Count, output = output + "/unity-export.json" };
