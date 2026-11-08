// 대검으로 교정한 P09 생성 스탯만 갱신한다. 기존 공식과 다르게 조율된 데이터는 거부한다.
bool injectFailure = true;
var recipe=UnityEngine.JsonUtility.FromJson<UPlayGround.Editor.HumanoidGradeAuthoring.Recipe>(System.IO.File.ReadAllText(UPlayGround.Editor.HumanoidGradeAuthoring.RecipePath));
var oldSet=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Ability.AbilitySetSO>("Assets/10.Datas/Ability/Actor/Humanoid_SwordShieldAttackData/AbilitySet_Humanoid_SwordShieldAttackData.asset");
var definitions=recipe.weapons.First(w=>w.key=="GreatSword").definitions.Select(p=>UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDefinitionSO>(p)).Where(d=>UnityEditor.AssetDatabase.GetAssetPath(d).StartsWith("Assets/10.Datas/Actor/P09Progression/Generated/")&&d.EffectiveGrade==UPlayGround.Data.EnumType.MonsterActorGrade.Elite).ToArray();
if(definitions.Length!=5)throw new System.Exception("의도한 P09 대검 5개가 아닙니다.");
foreach(var d in definitions){
    var temp=UnityEngine.Object.Instantiate(d);temp.monsterProfile=UnityEngine.Object.Instantiate(d.monsterProfile);temp.monsterProfile.abilitySet=oldSet;temp.abilitySet=oldSet;
    try{foreach(var p in UPlayGround.Data.Enemy.MonsterStatCalculator.CalculateAtLevel(temp.EffectiveMonsterScaling,temp,temp.EffectiveLevel)){float actual;if(!d.attributeProfile.TryGetBaseValue(p.Key,out actual)||!UnityEngine.Mathf.Approximately(actual,p.Value))throw new System.Exception("수동 조율 또는 이미 교정된 스탯이므로 재생성 거부: "+d.actorId+"/"+p.Key);}}
    finally{UnityEngine.Object.DestroyImmediate(temp.monsterProfile);UnityEngine.Object.DestroyImmediate(temp);}
}
var targets=definitions.SelectMany(d=>new UnityEngine.Object[]{d,d.monsterProfile,d.attributeProfile,d.poiseData,d.EffectiveBreakGaugeData}).Distinct().ToArray();
var backups=targets.Select(UnityEditor.AssetDatabase.GetAssetPath).SelectMany(p=>new[]{p,p+".meta"}).ToDictionary(p=>p,System.IO.File.ReadAllBytes);
string backupRoot="Logs/HumanoidGrade/StatBackup/"+System.DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
foreach(var p in backups){string path=backupRoot+"/"+p.Key;System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));System.IO.File.WriteAllBytes(path,p.Value);}
UnityEditor.Undo.IncrementCurrentGroup();int group=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.RegisterCompleteObjectUndo(targets,"잘못된 검방패 스탯 보정 제거");
try{
    foreach(var d in definitions){
        UPlayGround.Tool.Editor.Balance.MonsterStatBakeService.Bake(d,new UPlayGround.Tool.Editor.Balance.MonsterStatBakeService.Options{ForceRegenerate=true,CreateMissingStat=false,GenerateMissingBreakGauge=false,ReplaceExistingStatAsset=false,SyncGeneratedPoise=true,SyncExistingBreakGauge=true,RecordUndo=true});
        foreach(var target in targets)UnityEditor.AssetDatabase.SaveAssetIfDirty(target);
        if(injectFailure)throw new System.Exception("실패 주입: 첫 대검 스탯 저장 후");
        foreach(var p in UPlayGround.Data.Enemy.MonsterStatCalculator.CalculateAtLevel(d.EffectiveMonsterScaling,d,d.EffectiveLevel)){float actual;if(!d.attributeProfile.TryGetBaseValue(p.Key,out actual)||!UnityEngine.Mathf.Approximately(actual,p.Value))throw new System.Exception("교정 스탯 불일치: "+d.actorId+"/"+p.Key);}
    }
    UnityEditor.Undo.CollapseUndoOperations(group);
    System.IO.File.WriteAllText("Logs/HumanoidGrade/stat-correction.applied.txt","P09 Elite 대검 5개: 기존 검방패 공식 전수 일치 확인, AttributeProfile/Poise/Break 제자리 갱신, GUID 유지, 새 공식 전수 일치");
    return "대검 스탯 5개 교정 완료";
}catch{
    UnityEditor.Undo.RevertAllDownToGroup(group);
    foreach(var p in backups)System.IO.File.WriteAllBytes(p.Key,p.Value);
    foreach(var path in backups.Keys.Where(p=>!p.EndsWith(".meta")))UnityEditor.AssetDatabase.ImportAsset(path,UnityEditor.ImportAssetOptions.ForceSynchronousImport);
    foreach(var p in backups)if(!System.IO.File.ReadAllBytes(p.Key).SequenceEqual(p.Value))throw new System.Exception("스탯 복구 바이트 불일치: "+p.Key);
    System.IO.File.WriteAllText("Logs/HumanoidGrade/stat-correction.rollback.txt","첫 개체 스탯 저장 후 실패: 모든 AttributeProfile/Poise/Break/Definition/Profile/meta 원본 바이트 복구 일치");
    throw;
}
