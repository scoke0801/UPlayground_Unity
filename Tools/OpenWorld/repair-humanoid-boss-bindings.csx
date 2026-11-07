// 일회성 연결 교정. 전체 P09 생성과 플레이어 원본은 수정하지 않는다.
int failurePoint = 0;
if(EditorApplication.isUpdating||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed)throw new System.InvalidOperationException("임포트와 컴파일이 끝난 Edit Mode 필요");
string[] paths={"Assets/10.Datas/Actor/DataBase/Humanoid/MonsterSera.asset","Assets/10.Datas/Actor/DataBase/Humanoid/MonsterSeolA.asset"};
string[] weapons={"GreatSword","Bow"};
var defs=paths.Select(AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDefinitionSO>).ToArray();
var sets=weapons.Select(w=>AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Ability.AbilitySetSO>(UPlayGround.Editor.HumanoidGradeAuthoring.OutputRoot+"/"+w+"/AbilitySet_"+w+"_Boss.asset")).ToArray();
var targets=defs.Cast<UnityEngine.Object>().Concat(defs.Select(d=>d.monsterProfile)).Distinct().ToArray();
var files=targets.Select(AssetDatabase.GetAssetPath).Concat(defs.Select(d=>AssetDatabase.GetAssetPath(d.prefab))).Distinct().SelectMany(p=>new[]{p,p+".meta"}).ToDictionary(p=>p,System.IO.File.ReadAllBytes);
var backup="Logs/HumanoidGrade/Remaining/BossBindingBackup/"+System.DateTime.UtcNow.ToString("yyyyMMddHHmmssfff");
foreach(var pair in files){var p=backup+"/"+pair.Key;System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(p));System.IO.File.WriteAllBytes(p,pair.Value);}
Undo.IncrementCurrentGroup();int undoGroup=Undo.GetCurrentGroup();Undo.RegisterCompleteObjectUndo(targets,"보스 무기/모션 연결 교정");
try{
for(int index=0;index<defs.Length;index++){
var d=defs[index];var prefabPath=AssetDatabase.GetAssetPath(d.prefab);var root=PrefabUtility.LoadPrefabContents(prefabPath);
try{
var actorAnimator=root.GetComponentInChildren<UPlayGround.Animation.ActorAnimator>(true);
var map=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.Animation.ActorAnimationMotionSet>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Humanoid/Humanoid_"+weapons[index]+"AnimationSet.asset");
var serialized=new SerializedObject(actorAnimator);serialized.FindProperty("_motionSet").objectReferenceValue=map;serialized.ApplyModifiedPropertiesWithoutUndo();
if(index==0){
var donor=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Sera.prefab");
var source=donor.transform.Find("Weapon/GreatSword");
if(root.transform.Find("Weapon/GreatSword")!=null)throw new System.InvalidOperationException("대검 교정이 이미 적용됐습니다.");
if(donor.GetComponent<Animator>().avatar!=root.GetComponent<Animator>().avatar)throw new System.InvalidOperationException("무기 부착 원본 Avatar 불일치");
var copy=UnityEngine.Object.Instantiate(source.gameObject,root.transform.Find("Weapon"),false);copy.name="GreatSword";copy.SetActive(true);
foreach(var hit in donor.GetComponentsInChildren<UPlayGround.Combat.CombatHitbox>(true).Where(h=>!h.transform.IsChildOf(source))){
var parentPath=AnimationUtility.CalculateTransformPath(hit.transform.parent,donor.transform);var parent=root.transform.Find(parentPath);if(parent==null)throw new System.InvalidOperationException("신체 판정 본 누락: "+parentPath);
var bodyHit=UnityEngine.Object.Instantiate(hit.gameObject,parent,false);bodyHit.name=hit.name;
}
foreach(var c in copy.GetComponentsInChildren<UnityEngine.Animations.ParentConstraint>(true)){
for(int s=0;s<c.sourceCount;s++){var cs=c.GetSource(s);var bonePath=AnimationUtility.CalculateTransformPath(cs.sourceTransform,donor.transform);cs.sourceTransform=root.transform.Find(bonePath);if(cs.sourceTransform==null)throw new System.InvalidOperationException("대상 본 누락: "+bonePath);cs.weight=s==0?1:0;c.SetSource(s,cs);}c.constraintActive=true;
}
// 이전 무기 오브젝트는 fileID와 외부 역참조를 보존하고 표시만 끈다.
root.transform.Find("Weapon/Weapon").gameObject.SetActive(false);
var oldGuard=root.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="GuardPosition"&&!t.IsChildOf(copy.transform));
var newGuard=copy.transform.Find("GuardPosition");
foreach(var component in root.GetComponents<MonoBehaviour>().Where(c=>c!=null)){
var so=new SerializedObject(component);var it=so.GetIterator();bool changed=false;while(it.NextVisible(true))if(it.propertyType==SerializedPropertyType.ObjectReference&&oldGuard!=null&&it.objectReferenceValue==oldGuard){it.objectReferenceValue=newGuard;changed=true;}if(changed)so.ApplyModifiedPropertiesWithoutUndo();
}
}else{
var bow=root.transform.Find("Weapon/Bow/Bow_004");if(bow==null)throw new System.InvalidOperationException("실제 활 메시 누락");
if(root.GetComponentsInChildren<UPlayGround.Combat.CombatHitbox>(true).Length>0)throw new System.InvalidOperationException("활 판정이 이미 적용됐습니다.");
var renderer=bow.GetComponentInChildren<Renderer>(true);var bounds=renderer.localBounds;
var hit=new GameObject("[HitBox] MainWeapon");hit.transform.SetParent(renderer.transform,false);
var box=hit.AddComponent<BoxCollider>();box.center=bounds.center;box.size=bounds.size;
hit.AddComponent<UPlayGround.Combat.CombatHitbox>().Configure("MainWeapon",box,true,0.15f,8);
var c=bow.parent.GetComponent<UnityEngine.Animations.ParentConstraint>();for(int s=0;s<c.sourceCount;s++){var cs=c.GetSource(s);cs.weight=s==0?1:0;c.SetSource(s,cs);}c.constraintActive=true;
}
var combat=root.GetComponent<UPlayGround.Components.EnemyCombat>();var combatSo=new SerializedObject(combat);combatSo.FindProperty("_abilitySet").objectReferenceValue=sets[index];combatSo.ApplyModifiedPropertiesWithoutUndo();
if(root.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new System.InvalidOperationException("Missing Script");
var groups=root.GetComponentsInChildren<UPlayGround.Combat.CombatHitbox>(true).Select(h=>h.GroupId).ToArray();
foreach(var ability in sets[index].EnumerateAll())foreach(var variant in ability.variants){var p=variant.executionPayload as UPlayGround.Ability.UPlayGround.UPlayGroundMotionAbilityPayloadSO;if(p==null||!p.attackInfo.aiSelectable)continue;var motion=map.GetAbilityMotionAsset(p.attackInfo.motionKey);if(motion==null||SerializationUtility.HasManagedReferencesWithMissingTypes(motion))throw new System.InvalidOperationException("Motion 누락: "+ability.name);foreach(var e in motion.motionSet.globalEvents.Concat(motion.motionSet.motions.SelectMany(m=>m.events)).OfType<UPlayGround.Data.Event.BeginCollisionEvent>())if(e.collisionSource==UPlayGround.Combat.CollisionSourceType.AttachedHitboxGroup&&!groups.Contains(e.hitboxGroupId))throw new System.InvalidOperationException("HitBox 누락: "+ability.name+"/"+e.hitboxGroupId);}
PrefabUtility.SaveAsPrefabAsset(root,prefabPath);
}finally{PrefabUtility.UnloadPrefabContents(root);}
if(failurePoint==1&&index==0)throw new System.InvalidOperationException("실패 주입: 첫 프리팹 저장 후");
d.abilitySet=sets[index];d.monsterProfile.abilitySet=sets[index];EditorUtility.SetDirty(d);EditorUtility.SetDirty(d.monsterProfile);
if(d.EffectiveAbilitySet!=sets[index])throw new System.InvalidOperationException("유효 Set 연결 실패");
}
if(failurePoint==2)throw new System.InvalidOperationException("실패 주입: Profile 연결 후");
foreach(var t in targets)AssetDatabase.SaveAssetIfDirty(t);Undo.CollapseUndoOperations(undoGroup);return "Sera GreatSword/Bow Nenmir 기존 GUID·actorId·Profile·프리팹 경로 유지. 스토리 보스 별도 Profile 유지.";
}catch{
Undo.RevertAllDownToGroup(undoGroup);
foreach(var pair in files)System.IO.File.WriteAllBytes(pair.Key,pair.Value);
foreach(var p in files.Keys.Where(p=>!p.EndsWith(".meta")))AssetDatabase.ImportAsset(p,ImportAssetOptions.ForceSynchronousImport);
if(files.Any(p=>!System.IO.File.ReadAllBytes(p.Key).SequenceEqual(p.Value)))throw new System.InvalidOperationException("보스 연결 복구 바이트 불일치");
System.IO.File.WriteAllText("Logs/HumanoidGrade/Remaining/boss-bindings.rollback"+failurePoint+".txt","프리팹·Definition·Profile·meta 원본 바이트 일치");throw;
}
