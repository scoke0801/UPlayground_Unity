// 최종 recipe의 두 책장만 생성기에 쓰이는 bounds 정렬과 같은 방식으로 증분 적용한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlaying || scene.isDirty || scene.path!=UPlayGround.Editor.OpenWorldSliceAuthoring.HostPath)
 throw new Exception("깨끗한 생성 월드 Edit Mode 필요");
var authoring=typeof(UPlayGround.Editor.OpenWorldSliceAuthoring);
var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
var recipe=authoring.GetMethod("LoadCityRecipe",flags).Invoke(null,null);
var landscape=recipe.GetType().GetField("landscape").GetValue(recipe);
var props=((Array)landscape.GetType().GetField("props").GetValue(landscape)).Cast<object>().ToArray();
var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var rows=new List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"recipe 기반 증분 적용; 전체 월드 재생성은 아님"};
foreach(var id in new[]{"Explore_1037","Explore_1038"}){
 var prop=props.Single(entry=>(string)entry.GetType().GetField("id").GetValue(entry)==id);
 var type=prop.GetType();
 var p=(Vector3)type.GetField("position").GetValue(prop);
 var item=transforms.Single(t=>t.name==id);
 var previous=item.position;
 item.SetPositionAndRotation(p,Quaternion.Euler(0,(float)type.GetField("yaw").GetValue(prop),0));
 var bounds=(Bounds)authoring.GetMethod("GetPropMeshBounds",flags).Invoke(null,new object[]{item.gameObject});
 item.position+=new Vector3(p.x-bounds.center.x,p.y-bounds.min.y,p.z-bounds.center.z);
 rows.Add(id+" "+previous+" -> "+item.position+" bounds="+bounds);
}
authoring.GetMethod("BakeNavigation",flags).Invoke(null,new object[]{scene});
AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath("Assets/10.Datas/World/OpenWorld/Navigation.asset"));
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.Directory.CreateDirectory("output/OpenWorld/Exploration/Iteration03");
System.IO.File.WriteAllLines("output/OpenWorld/Exploration/Iteration03/application.txt",rows);
return string.Join("\n",rows.ToArray());

