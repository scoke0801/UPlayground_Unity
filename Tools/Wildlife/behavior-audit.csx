var errors=new List<string>();var lines=new List<string>();
var prefabPaths=AssetDatabase.FindAssets("APD_ t:Prefab",new[]{"Assets/03.Prefabs/Environment/Wildlife"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
var profiles=new HashSet<UPlayGround.Data.World.WildlifeProfileSO>();
foreach(var path in prefabPaths){
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);var creature=prefab.GetComponent<UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature>();
 if(creature==null||creature.Profile==null){errors.Add(path+": 프로필 누락");continue;}
 var p=creature.Profile;profiles.Add(p);
 foreach(var tr in prefab.GetComponentsInChildren<Transform>(true))if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(tr.gameObject)>0)errors.Add(path+": Missing Script");
 if(p.Idle==null||p.Move==null||p.Flee==null)errors.Add(path+": 필수 모션 누락");
 if(p.UseStagedResponse && !(p.FleeDistance<=p.AvoidDistance&&p.AvoidDistance<=p.AlertDistance&&p.AlertDistance<=p.CalmDistance))errors.Add(path+": 접근 단계 순서 오류");
 foreach(var clip in new[]{p.Idle,p.Move,p.Flee,p.Feed,p.Observe,p.Rest}.Where(c=>c!=null).Distinct()) {
  if(clip.legacy||!AnimationUtility.GetAnimationClipSettings(clip).loopTime||AnimationUtility.GetAnimationEvents(clip).Length!=0)errors.Add(path+": 모션 설정 오류 "+clip.name);
  foreach(var binding in AnimationUtility.GetCurveBindings(clip))if(!string.IsNullOrEmpty(binding.path)&&prefab.transform.Find(binding.path)==null)errors.Add(path+": 본 누락 "+binding.path);
 }
 lines.Add(path+" / "+string.Join(", ",new[]{p.Idle,p.Move,p.Flee,p.Feed,p.Observe,p.Rest}.Select(c=>c==null?"없음":c.name).ToArray()));
}
var h=UnityEngine.Object.FindFirstObjectByType<UPlayGround.Gameplay.World.LivingWorld.WildlifeHabitat>();
var animals=h.GetComponentsInChildren<UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature>(true).Where(a=>a.name.StartsWith("APD_")).ToArray();
var before=animals.ToDictionary(c=>c.name,c=>c.transform.position);
UPlayGround.Tool.Editor.Map.WildlifeAuthoring.PlaceAnimalPackDeluxe();
foreach(var animal in animals){
 if(animal.transform.position!=before[animal.name])errors.Add(animal.name+": 재배치 반복 시 좌표 변경");
 if(animal.Profile.IsFlying)continue;
 var agent=animal.GetComponent<UnityEngine.AI.NavMeshAgent>();
 var filter=new UnityEngine.AI.NavMeshQueryFilter{agentTypeID=agent.agentTypeID,areaMask=agent.areaMask};UnityEngine.AI.NavMeshHit hit;
 if(!UnityEngine.AI.NavMesh.SamplePosition(animal.transform.position,out hit,.5f,filter))errors.Add(animal.name+": NavMesh 연결 누락");
 if(!h.CanOccupy(animal.transform.position))errors.Add(animal.name+": 수면 아래 배치");
}
// 이전 좌표와 다른 수동 이동이 배치 도구로 덮어써지지 않는지 검사하고 즉시 복구한다.
var sample=h.transform.Find("APD_lake-bank_01");var position=sample.position;
try{sample.position+=Vector3.right*.25f;var edited=sample.position;UPlayGround.Tool.Editor.Map.WildlifeAuthoring.PlaceAnimalPackDeluxe();if(sample.position!=edited)errors.Add("수동 위치 수정 보존 실패");}
finally{sample.position=position;PrefabUtility.RecordPrefabInstancePropertyModifications(sample);}
if(profiles.Count!=17||animals.Length!=77)errors.Add("종·개체 수 변경: "+profiles.Count+" / "+animals.Length);
lines.Insert(0,"프로필 "+profiles.Count+" / 프리팹 "+prefabPaths.Length+" / APD 개체 "+animals.Length+" / 오류 "+errors.Count);
System.IO.Directory.CreateDirectory("output/WildlifeObservation");System.IO.File.WriteAllLines("output/WildlifeObservation/asset-audit.txt",lines.Concat(errors));
if(errors.Count>0)throw new Exception(string.Join("\n",errors));return lines[0];
