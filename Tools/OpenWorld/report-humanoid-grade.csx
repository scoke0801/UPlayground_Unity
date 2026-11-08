var report=new System.Collections.Generic.List<string>{"# Humanoid 등급 공격 실제 데이터", "", "자동 생성: 현재 Unity 에셋 기준. 피해는 Phase당 한 번 적중한 합계이며 실측 DPS가 아니다.","", "| Set | Ability | 레벨 | 역할 | Phase 피해 | 합계 | Motion 총시간 | 첫 판정 | 마지막 판정 끝 | 후딜 | 판정 이벤트/Phase |", "|---|---|---:|---|---|---:|---:|---:|---:|---:|---|"};
var csv=new System.Collections.Generic.List<string>{"set,ability,level,roles,phases,total,duration,first,lastEnd,recovery,events,motionKey,motionAsset"};
var maps=UnityEditor.AssetDatabase.FindAssets("t:ActorAnimationMotionSet").Select(g=>UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.Animation.ActorAnimationMotionSet>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).ToArray();
var sets=UnityEditor.AssetDatabase.FindAssets("t:AbilitySetSO",new[]{UPlayGround.Editor.HumanoidGradeAuthoring.OutputRoot}).Select(g=>UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Ability.AbilitySetSO>(UnityEditor.AssetDatabase.GUIDToAssetPath(g))).OrderBy(s=>s.name).ToArray();
foreach(var set in sets){
set.RebuildRuntimeIndex();
foreach(var ability in set.EnumerateAll()){
var payload=ability.variants.FirstOrDefault()==null?null:ability.variants[0].executionPayload as UPlayGround.Ability.UPlayGround.UPlayGroundMotionAbilityPayloadSO;
if(payload==null||!payload.attackInfo.aiSelectable)continue;
var info=payload.attackInfo;
var map=maps.First(x=>x.GetAbilityMotionAsset(info.motionKey)!=null);
var asset=map.GetAbilityMotionAsset(info.motionKey);
var motion=asset.motionSet;
var timed=new System.Collections.Generic.List<System.Tuple<UPlayGround.Data.Event.MotionEventBase,float>>();
foreach(var e in motion.globalEvents)timed.Add(System.Tuple.Create(e,0f));
float offset=0f;foreach(var part in motion.motions){foreach(var e in part.events)timed.Add(System.Tuple.Create(e,offset));offset+=part.Duration;}
var hits=timed.Where(t=>t.Item1 is UPlayGround.Data.Event.BeginCollisionEvent||t.Item1 is UPlayGround.Data.Event.SpawnProjectileEvent).ToArray();
float first=hits.Min(t=>t.Item1.startTime+t.Item2), last=hits.Max(t=>t.Item1.endTime+t.Item2);
string phases=string.Join("+",info.baseInfo.hitPhases.Select(p=>p.damage.ToString("0.###",System.Globalization.CultureInfo.InvariantCulture)).ToArray());
string events=string.Join(";",hits.Select(t=>{var c=t.Item1 as UPlayGround.Data.Event.BeginCollisionEvent;var p=t.Item1 as UPlayGround.Data.Event.SpawnProjectileEvent;return (c!=null?"C":"P")+(c!=null?c.hitPhaseIndex:p.hitPhaseIndex)+"@"+(t.Item1.startTime+t.Item2).ToString("F3")+(p==null?"":"/"+p.spawnPattern.GetType().Name);}).ToArray());
float sum=info.baseInfo.hitPhases.Sum(p=>p.damage);
string cols=set.name+" | "+ability.name+" | "+info.requiredLevel+" | "+info.aiRoles+" | "+phases+" | "+sum.ToString("0.###")+" | "+motion.TotalDuration.ToString("F3")+" | "+first.ToString("F3")+" | "+last.ToString("F3")+" | "+(motion.TotalDuration-last).ToString("F3")+" | "+events;
report.Add("| "+cols+" |");
csv.Add(string.Join(",",new[]{set.name,ability.abilityId,info.requiredLevel.ToString(),info.aiRoles.ToString().Replace(",","+"),phases,sum.ToString("0.###"),motion.TotalDuration.ToString("F3"),first.ToString("F3"),last.ToString("F3"),(motion.TotalDuration-last).ToString("F3"),events,info.motionKey.ToString(),UnityEditor.AssetDatabase.GetAssetPath(asset)}));
}}
var owners=new System.Collections.Generic.List<string>{"actorId | grade | level | profile | effectiveSet | prefab | motionSet"};
var finalDamage=new System.Collections.Generic.List<string>{"actorId,grade,level,ability,baseSum,attackPower,defense0.2FinalSum"};
foreach(var g in UnityEditor.AssetDatabase.FindAssets("t:ActorDefinitionSO")){
var d=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.ActorDefinitionSO>(UnityEditor.AssetDatabase.GUIDToAssetPath(g));if(d.EffectiveAbilitySet==null||!sets.Contains(d.EffectiveAbilitySet))continue;
var animator=d.prefab.GetComponentInChildren<UPlayGround.Animation.ActorAnimator>(true);
owners.Add(d.actorId+" | "+d.EffectiveGrade+" | "+d.EffectiveLevel+" | "+UnityEditor.AssetDatabase.GetAssetPath(d.monsterProfile)+" | "+d.EffectiveAbilitySet.name+" | "+UnityEditor.AssetDatabase.GetAssetPath(d.prefab)+" | "+animator.MotionSet.name);
float power=1f;if(d.attributeProfile!=null)d.attributeProfile.TryGetBaseValue(global::UPlayGround.Data.Stat.Attributes.Combat.AttackPower,out power);
foreach(var a in d.EffectiveAbilitySet.EnumerateAll()){
var p=a.variants.FirstOrDefault()==null?null:a.variants[0].executionPayload as UPlayGround.Ability.UPlayGround.UPlayGroundMotionAbilityPayloadSO;if(p==null||!p.attackInfo.aiSelectable||!p.attackInfo.IsUnlockedForLevel(d.EffectiveLevel))continue;
float sum=p.attackInfo.baseInfo.hitPhases.Sum(h=>h.damage);
float final=p.attackInfo.baseInfo.hitPhases.Sum(h=>UPlayGround.Ability.Core.DamageExecution.Calculate(h.damage,power,0.2f,1f,1f,1f));
finalDamage.Add(d.actorId+","+d.EffectiveGrade+","+d.EffectiveLevel+","+a.abilityId+","+sum.ToString("0.###")+","+power.ToString("0.###")+","+final.ToString("0.###"));
}}
System.IO.File.WriteAllLines("Assets/docs/TODO/HUMANOID_GRADE_ATTACK_DATA.md",report);
System.IO.File.WriteAllLines("Logs/HumanoidGrade/attacks.after.csv",csv);
System.IO.File.WriteAllLines("Logs/HumanoidGrade/owners.after.txt",owners);
System.IO.File.WriteAllLines("Logs/HumanoidGrade/final-damage.csv",finalDamage);
return "Sets="+sets.Length+", attack rows="+(csv.Count-1)+", owners="+(owners.Count-1);
