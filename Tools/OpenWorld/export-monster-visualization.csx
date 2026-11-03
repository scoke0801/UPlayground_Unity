// 현재 씬을 읽기만 하며 HTML에 쓸 위치와 상태를 내보낸다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlaying || scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("Lake Village Edit Mode 필요");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/monster-population-resolved.json"),typeof(Dictionary<string,object>));
var overrides=(Dictionary<string,object>[])read(plan["existingOverrides"].ToString(),typeof(Dictionary<string,object>[]));
var overridePaths=new HashSet<string>(overrides.Select(o=>(string)o["path"]));
var context=UnityEngine.Object.FindFirstObjectByType<UPlayGround.SceneContext>();
var config=(UPlayGround.Data.UI.MinimapIconConfigSO)new SerializedObject(context).FindProperty("WorldMapConfig").objectReferenceValue;
var monsters=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.MonsterActor>(true)).Select(a=>{
    var path=AnimationUtility.CalculateTransformPath(a.transform,null);
    var d=a.Definition;var group=a.GetComponentInParent<UPlayGround.Group.MonsterGroupController>(true);
    return new {path,name=a.name,id=d!=null?d.actorId:a.ActorId,label=d!=null?d.displayName:a.ActorId,
        level=d!=null?d.EffectiveLevel:a.Level,grade=d!=null?d.EffectiveGrade.ToString():a.Grade.ToString(),
        position=new[]{a.transform.position.x,a.transform.position.y,a.transform.position.z},
        group=group!=null?AnimationUtility.CalculateTransformPath(group.transform,null):path,
        category=path.Contains("필드 몬스터 성장 배치")?"new":overridePaths.Contains(path)?"retuned":path.StartsWith("시나리오/")?"story":"existing",
        isInfill=path.Contains("숲과 유적 내부 보강"),missingDefinition=d==null,active=a.gameObject.activeInHierarchy,prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(a.gameObject)};
}).ToArray();
var snapshot=new {scene=scene.path,exportedAt=DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),image=AssetDatabase.GetAssetPath(config.backgroundSprite),
    center=new[]{config.captureCenter.x,config.captureCenter.y},size=new[]{config.captureWorldSizeXY.x,config.captureWorldSizeXY.y},flipX=config.flipX,flipY=config.flipY,rotation=config.rotationDegrees,monsters};
System.IO.Directory.CreateDirectory("output/OpenWorld/MonsterPopulation");
var serialize=json.GetMethod("SerializeObject",new[]{typeof(object)});
System.IO.File.WriteAllText("output/OpenWorld/MonsterPopulation/visualization-data.json",(string)serialize.Invoke(null,new object[]{snapshot}));
return new {count=monsters.Length,image=snapshot.image,categories=monsters.GroupBy(m=>m.category).Select(g=>g.Key+":"+g.Count()).ToArray()};
