// 새 동물의 실제 에셋·본·모션 변형·씬 연결과 반복 배치를 검사한다.
var errors=new List<string>();var checks=new List<string>();
var paths=AssetDatabase.FindAssets("APD_ t:Prefab",new[]{"Assets/03.Prefabs/Environment/Wildlife"}).Select(AssetDatabase.GUIDToAssetPath).ToArray();
foreach(var path in paths){
 var root=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));root.SetActive(false);
 var baked=new Mesh();
 try{
  var creature=root.GetComponent<UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature>();var profile=creature.Profile;
  if(profile==null)throw new Exception(path+": 프로필 누락");
  if(!profile.IsFlying && !profile.ProjectGroundMovement)errors.Add(path+": 지면 보정 누락");
  foreach(var clip in new[]{profile.Idle,profile.Move,profile.Flee,profile.Feed}.Distinct()){
   if(clip==null){errors.Add(path+": 클립 누락");continue;}
   if(clip.legacy || !AnimationUtility.GetAnimationClipSettings(clip).loopTime || AnimationUtility.GetAnimationEvents(clip).Length!=0)errors.Add(path+": 루프·이벤트 설정 "+clip.name);
   foreach(var binding in AnimationUtility.GetCurveBindings(clip))if(!string.IsNullOrEmpty(binding.path) && root.transform.Find(binding.path)==null)errors.Add(path+": 본 누락 "+binding.path);
  }
  var renderer=root.GetComponentInChildren<SkinnedMeshRenderer>();
  if(renderer==null || renderer.bones.Any(b=>b==null))errors.Add(path+": 스킨 연결 누락");
  else {
   profile.Move.SampleAnimation(root,profile.Move.length*.13f);renderer.BakeMesh(baked);var before=baked.vertices;
   profile.Move.SampleAnimation(root,profile.Move.length*.61f);renderer.BakeMesh(baked);var after=baked.vertices;
   bool deforms=false;for(int i=0;i<before.Length;i++)if((before[i]-after[i]).sqrMagnitude>0.00000001f){deforms=true;break;}
   if(!deforms)errors.Add(path+": 이동 모션 변형 없음");
  }
  checks.Add(path+": 검사 완료");
 }finally{UnityEngine.Object.DestroyImmediate(baked);UnityEngine.Object.DestroyImmediate(root);}
}
var habitat=UnityEngine.Object.FindFirstObjectByType<UPlayGround.Gameplay.World.LivingWorld.WildlifeHabitat>();
var animals=habitat.GetComponentsInChildren<UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature>(true).Where(c=>c.name.StartsWith("APD_")).ToArray();
int count=animals.Length;
UPlayGround.Tool.Editor.Map.WildlifeAuthoring.PlaceAnimalPackDeluxe();
if(count!=habitat.GetComponentsInChildren<UPlayGround.Gameplay.World.LivingWorld.WildlifeCreature>(true).Count(c=>c.name.StartsWith("APD_")))errors.Add("재실행 시 개체 중복");
var terrain=Terrain.activeTerrain;
foreach(var animal in animals){
 if(animal.Profile.IsFlying)continue;
 float height=terrain.SampleHeight(animal.transform.position)+terrain.transform.position.y+animal.Profile.GroundClearance;
 if(Mathf.Abs(animal.transform.position.y-height)>.03f)errors.Add(animal.name+": 지면 높이 불일치");
}
UPlayGround.Tool.Editor.Map.WildlifeAuthoring.Validate();
checks.Insert(0,"프리팹 "+paths.Length+" / 신규 개체 "+count+" / 오류 "+errors.Count);
System.IO.Directory.CreateDirectory("output/Wildlife/AnimalPackDeluxe");
System.IO.File.WriteAllLines("output/Wildlife/AnimalPackDeluxe/audit.txt",checks.Concat(errors));
if(errors.Count>0)throw new Exception(string.Join("\n",errors));
return checks[0];
