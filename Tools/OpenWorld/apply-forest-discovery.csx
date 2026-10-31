// recipe의 차이만 적용한다. 전체 생성기와 같은 채집 생성 함수를 호출한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlaying || scene.isDirty || scene.path!=UPlayGround.Editor.OpenWorldSliceAuthoring.HostPath)
 throw new Exception("깨끗한 생성 월드 Edit Mode 필요");
var folder="output/OpenWorld/Exploration/Iteration04";
System.IO.Directory.CreateDirectory(folder);
var authoring=typeof(UPlayGround.Editor.OpenWorldSliceAuthoring);
var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
var recipe=authoring.GetMethod("LoadCityRecipe",flags).Invoke(null,null);
var landscape=recipe.GetType().GetField("landscape").GetValue(recipe);
var props=((Array)landscape.GetType().GetField("props").GetValue(landscape)).Cast<object>().ToArray();
var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).Single();
var rows=new List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"recipe 기반 증분 적용; 전체 월드 재생성 아님"};
foreach(var id in new[]{"Expansion_log_792","Explore_0617","Explore_0618","Explore_0619","Explore_0014","Explore_0036"}){
 var prop=props.Single(entry=>(string)entry.GetType().GetField("id").GetValue(entry)==id);
 var type=prop.GetType();var p=(Vector3)type.GetField("position").GetValue(prop);
 p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
 var item=transforms.Single(t=>t.name==id);var previous=item.position;
 item.localScale=(Vector3)type.GetField("scale").GetValue(prop);
 item.SetPositionAndRotation(p,Quaternion.Euler((float)type.GetField("pitch").GetValue(prop),(float)type.GetField("yaw").GetValue(prop),0));
 var bounds=(Bounds)authoring.GetMethod("GetPropMeshBounds",flags).Invoke(null,new object[]{item.gameObject});
 item.position+=new Vector3(p.x-bounds.center.x,p.y-bounds.min.y,p.z-bounds.center.z);
 rows.Add(id+" "+previous+" -> "+item.position);
}
var root=transforms.Single(t=>t.name=="ExplorationDiscoveries");
var oldLog=transforms.SingleOrDefault(t=>t.name=="Expansion_log_791");
if(oldLog!=null)UnityEngine.Object.DestroyImmediate(oldLog.gameObject);
if(root.Find("c42bb9f480ec4f3cacdf786ab0a59e21")==null)
 authoring.GetMethod("BuildExplorationGatherables",flags).Invoke(null,new object[]{root,new[]{terrain},landscape});
authoring.GetMethod("BakeNavigation",flags).Invoke(null,new object[]{scene});
AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath("Assets/10.Datas/World/OpenWorld/Navigation.asset"));
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
rows.Add("GatheringActor/InteractableActorSO/SceneEntityId 생성 완료; 기존 보상 4개·적 2개 보존");
System.IO.File.WriteAllLines(folder+"/application.txt",rows);
return string.Join("\n",rows.ToArray());
