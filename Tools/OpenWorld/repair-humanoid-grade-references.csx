// 최초 대검 적용 시 저장 순서 오류로 비었던 신규 에셋의 참조만 복구한다. GUID/피해/공유 원본은 유지한다.
var recipe=UnityEngine.JsonUtility.FromJson<UPlayGround.Editor.HumanoidGradeAuthoring.Recipe>(System.IO.File.ReadAllText(UPlayGround.Editor.HumanoidGradeAuthoring.RecipePath));
var weapon=recipe.weapons.Single(w=>w.key=="GreatSword");
string root=UPlayGround.Editor.HumanoidGradeAuthoring.OutputRoot+"/"+weapon.key;
UnityEditor.Undo.IncrementCurrentGroup();
int group=UnityEditor.Undo.GetCurrentGroup();
var previous=new System.Collections.Generic.Dictionary<string,UPlayGround.Data.Ability.GameplayAbilitySO>();
var targets=new System.Collections.Generic.List<UnityEngine.Object>();
try {
for(int grade=0;grade<3;grade++) {
    string gradeName=((UPlayGround.Data.EnumType.MonsterActorGrade)grade).ToString();
    var set=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Ability.AbilitySetSO>(root+"/AbilitySet_"+weapon.key+"_"+gradeName+".asset");
    UnityEditor.Undo.RegisterCompleteObjectUndo(set,"신규 참조 복구");
    set.additionalAbilities.Clear();
    if(grade>0)set.abilityOverrides.Clear();
    foreach(var entry in weapon.attacks.Where(a=>a.grade==grade)) {
        var original=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Ability.GameplayAbilitySO>(entry.source);
        bool fork=entry.level>1||entry.replace||entry.latePhase||!string.IsNullOrEmpty(entry.roles)||!string.IsNullOrEmpty(entry.category)||(entry.damage!=null&&entry.damage.Length>0);
        var ability=original;
        if(fork) {
            string name="GA_"+weapon.key+"_"+gradeName+"_"+original.name;
            ability=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Ability.GameplayAbilitySO>(root+"/"+name+".asset");
            var payload=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Ability.UPlayGround.UPlayGroundMotionAbilityPayloadSO>(root+"/Payload_"+name+".asset");
            if(ability==null||payload==null)throw new System.Exception("복구 대상 누락: "+name);
            UnityEditor.Undo.RegisterCompleteObjectUndo(ability,"신규 Payload 참조 복구");
            ability.variants[0].executionPayload=payload;
            targets.Add(ability);
        }
        if(entry.replace) set.abilityOverrides.Add(new UPlayGround.Data.Ability.AbilitySetSO.AbilityOverrideEntry{sourceAbility=previous[entry.source],replacementAbility=ability});
        else if(grade>0||fork)set.additionalAbilities.Add(ability);
        previous[entry.source]=ability;
    }
    set.RebuildRuntimeIndex();
    UPlayGround.Editor.HumanoidGradeAuthoring.ValidateSet(set,grade>0);
    targets.Add(set);
}
foreach(var target in targets){UnityEditor.EditorUtility.SetDirty(target);UnityEditor.AssetDatabase.SaveAssetIfDirty(target);}
UnityEditor.Undo.CollapseUndoOperations(group);
return "대검 신규 Payload/Ability/Set 참조 복구 완료";
} catch {UnityEditor.Undo.RevertAllDownToGroup(group);foreach(var target in targets){UnityEditor.EditorUtility.SetDirty(target);UnityEditor.AssetDatabase.SaveAssetIfDirty(target);}throw;}
