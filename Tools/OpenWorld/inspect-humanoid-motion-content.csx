var maps=AssetDatabase.FindAssets("t:ActorAnimationMotionSet").Select(g=>AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.Animation.ActorAnimationMotionSet>(AssetDatabase.GUIDToAssetPath(g))).ToArray();
var lines=new System.Collections.Generic.List<string>();
foreach(var weapon in new[]{"Katana","Whip","DualBlade","Speat","Bow"}){
var root="Assets/10.Datas/Ability/Actor/Humanoid_"+weapon+"AttackData";
foreach(var g in AssetDatabase.FindAssets("t:GameplayAbilitySO",new[]{root})){
var a=AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Ability.GameplayAbilitySO>(AssetDatabase.GUIDToAssetPath(g));
var p=a.variants.FirstOrDefault()==null?null:a.variants[0].executionPayload as UPlayGround.Ability.UPlayGround.UPlayGroundMotionAbilityPayloadSO;if(p==null||!p.attackInfo.aiSelectable)continue;
var i=p.attackInfo;var map=maps.FirstOrDefault(m=>m.GetAbilityMotionAsset(i.motionKey)!=null);var asset=map==null?null:map.GetAbilityMotionAsset(i.motionKey);
lines.Add(a.name+" | "+i.aiRoles+" | "+i.attackCategory+" | damage="+string.Join("+",i.baseInfo.hitPhases.Select(h=>h.damage.ToString()).ToArray())+" | CD="+a.cooldown.durationSeconds+"/"+a.cooldown.cooldownGroupId+" | "+i.motionKey+" | "+AssetDatabase.GetAssetPath(asset));
if(asset==null)continue;float offset=0;foreach(var e in asset.motionSet.globalEvents)lines.Add(" E "+e.GetType().Name+" "+e.startTime+".."+e.endTime+" "+JsonUtility.ToJson(e));
foreach(var part in asset.motionSet.motions){lines.Add(" CLIP "+AssetDatabase.GetAssetPath(part.motionClip)+" "+part.motionClip.name+" duration="+part.Duration);foreach(var e in part.events)lines.Add(" E "+e.GetType().Name+" "+(offset+e.startTime)+".."+(offset+e.endTime)+" "+JsonUtility.ToJson(e));offset+=part.Duration;}
lines.Add(" TOTAL "+asset.motionSet.TotalDuration);
}}
System.IO.File.WriteAllLines("Logs/HumanoidGrade/Remaining/motion-content.txt",lines);return lines.Count;

