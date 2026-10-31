// 현재 생성 월드에 recipe의 고원 차이만 적용한다. 전체 재생성이 아니다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.isDirty || scene.path!=UPlayGround.Editor.OpenWorldSliceAuthoring.HostPath)
 throw new Exception("깨끗하고 준비된 생성 월드 Edit Mode 필요");
var folder="output/OpenWorld/Exploration/Iteration05";
System.IO.Directory.CreateDirectory(folder+"/backup");
var authoring=typeof(UPlayGround.Editor.OpenWorldSliceAuthoring);
var flags=System.Reflection.BindingFlags.Static|System.Reflection.BindingFlags.NonPublic;
var recipe=authoring.GetMethod("LoadCityRecipe",flags).Invoke(null,null);
var landscape=recipe.GetType().GetField("landscape").GetValue(recipe);
var props=((Array)landscape.GetType().GetField("props").GetValue(landscape)).Cast<object>().ToArray();
var transforms=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();
var terrain=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Terrain>()).Single();
var data=terrain.terrainData;
foreach(var path in new[]{scene.path,AssetDatabase.GetAssetPath(data),"Assets/10.Datas/World/OpenWorld/Navigation.asset"}){
 var destination=folder+"/backup/"+System.IO.Path.GetFileName(path);
 if(!System.IO.File.Exists(destination))System.IO.File.Copy(path,destination);
}
var rows=new List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"recipe 기반 증분; 높이맵 변경 없음; 전체 월드 재생성 아님"};
foreach(var id in new[]{"Explore_0630","Explore_0631","Explore_0579","Explore_0582","Explore_0624","Explore_0625","Explore_0626","Expansion_rock_345","Expansion_rock_353"}){
 var prop=props.Single(entry=>(string)entry.GetType().GetField("id").GetValue(entry)==id);
 var type=prop.GetType();var p=(Vector3)type.GetField("position").GetValue(prop);
 p.y=terrain.SampleHeight(p)+terrain.transform.position.y+(float)type.GetField("groundOffset").GetValue(prop);
 var item=transforms.Single(t=>t.name==id);var previous=item.position;
 item.localScale=(Vector3)type.GetField("scale").GetValue(prop);
 item.SetPositionAndRotation(p,Quaternion.Euler((float)type.GetField("pitch").GetValue(prop),(float)type.GetField("yaw").GetValue(prop),0));
 if((bool)type.GetField("alignBounds").GetValue(prop)){
  var bounds=(Bounds)authoring.GetMethod("GetPropMeshBounds",flags).Invoke(null,new object[]{item.gameObject});
  item.position+=new Vector3(p.x-bounds.center.x,p.y-bounds.min.y,p.z-bounds.center.z);
 }
 float embed=(float)type.GetField("slopeEmbedDepth").GetValue(prop);
 if(embed>0){
  var bounds=(Bounds)authoring.GetMethod("GetPropMeshBounds",flags).Invoke(null,new object[]{item.gameObject});
  float lowest=p.y-(float)type.GetField("groundOffset").GetValue(prop);
  foreach(float x in new[]{bounds.min.x,bounds.center.x,bounds.max.x})foreach(float z in new[]{bounds.min.z,bounds.center.z,bounds.max.z})
   lowest=Mathf.Min(lowest,terrain.SampleHeight(new Vector3(x,0,z))+terrain.transform.position.y);
  item.position+=Vector3.up*Mathf.Min(0,lowest-bounds.min.y-embed);
 }
 rows.Add(id+" "+previous+" -> "+item.position);
}
// 새 길과 지워야 할 옛 길의 주변만 복사한다. 임시 Terrain의 높이는 원본에 복사하지 않는다.
var scratch=UnityEngine.Object.Instantiate(data);
var go=Terrain.CreateTerrainGameObject(scratch);go.hideFlags=HideFlags.HideAndDontSave;go.transform.position=terrain.transform.position;
try{
 var temporary=go.GetComponent<Terrain>();
 var before=JsonUtility.FromJson(System.IO.File.ReadAllText(folder+"/input-before.json"),recipe.GetType());
 var oldRoute=((Array)recipe.GetType().GetField("routes").GetValue(before)).Cast<object>().Single(q=>(string)q.GetType().GetField("id").GetValue(q)=="upland-lake-loop");
 var oldLine=(Vector3[])oldRoute.GetType().GetField("points").GetValue(oldRoute);
 var routes=(Array)recipe.GetType().GetField("routes").GetValue(recipe);
 var route=routes.Cast<object>().Single(q=>(string)q.GetType().GetField("id").GetValue(q)=="upland-lake-loop");
 var newLine=(Vector3[])route.GetType().GetField("points").GetValue(route);
 int x0=Mathf.FloorToInt((1028-terrain.transform.position.x)/data.size.x*(data.alphamapWidth-1));
 int z0=Mathf.FloorToInt((1240-terrain.transform.position.z)/data.size.z*(data.alphamapHeight-1));
 int x1=Mathf.CeilToInt((1412-terrain.transform.position.x)/data.size.x*(data.alphamapWidth-1));
 int z1=Mathf.CeilToInt((1432-terrain.transform.position.z)/data.size.z*(data.alphamapHeight-1));
 var weights=data.GetAlphamaps(x0,z0,x1-x0+1,z1-z0+1);
 var basis=data.GetAlphamaps(x0,z0,x1-x0+1,z1-z0+1);
 var selected=new bool[basis.GetLength(0),basis.GetLength(1)];
 var layers=data.terrainLayers.Select(AssetDatabase.GetAssetPath).ToArray();
 int grass=Array.IndexOf(layers,(string)landscape.GetType().GetField("grassLayer").GetValue(landscape));
 int earth=Array.IndexOf(layers,(string)landscape.GetType().GetField("earthLayer").GetValue(landscape));
 int rock=Array.IndexOf(layers,(string)landscape.GetType().GetField("rockLayer").GetValue(landscape));
 if(grass<0||earth<0||rock<0)throw new Exception("기본 지표 누락");
 int count=0;
 for(int z=0;z<weights.GetLength(0);z++)for(int x=0;x<weights.GetLength(1);x++){
  var p=terrain.transform.position+new Vector3((x+x0)*data.size.x/(data.alphamapWidth-1),0,(z+z0)*data.size.z/(data.alphamapHeight-1));
  float distance=float.MaxValue;
  foreach(var line in new[]{oldLine,newLine})distance=Mathf.Min(distance,(float)authoring.GetMethod("GetLineDistance",flags).Invoke(null,new object[]{p,line,0f}));
  if(distance>6)continue;
  selected[z,x]=true;
  // 북쪽 확장부는 PaintLandscape의 전이 범위 밖이다. 전체 MergeWorldGround와 같은 기본 지표로 옛 길을 지운다.
  float u=(p.x-terrain.transform.position.x)/data.size.x,v=(p.z-terrain.transform.position.z)/data.size.z;
  float steep=Mathf.InverseLerp(22,42,data.GetSteepness(u,v));
  float soil=Mathf.Lerp(.12f,.55f,Mathf.InverseLerp(63,46,terrain.SampleHeight(p)+terrain.transform.position.y));
  for(int layer=0;layer<data.alphamapLayers;layer++)basis[z,x,layer]=0;
  basis[z,x,grass]=(1-steep)*(1-soil);basis[z,x,earth]=(1-steep)*soil;basis[z,x,rock]=steep;
  count++;
 }
 scratch.SetAlphamaps(x0,z0,basis);
 authoring.GetMethod("StampRoads",flags).Invoke(null,new object[]{temporary,recipe});
 if(!data.terrainLayers.SequenceEqual(scratch.terrainLayers))throw new Exception("지표 레이어 순서 불일치");
 var generated=scratch.GetAlphamaps(x0,z0,x1-x0+1,z1-z0+1);
 for(int z=0;z<weights.GetLength(0);z++)for(int x=0;x<weights.GetLength(1);x++)
  if(selected[z,x])for(int layer=0;layer<data.alphamapLayers;layer++)weights[z,x,layer]=generated[z,x,layer];
 data.SetAlphamaps(x0,z0,weights);terrain.Flush();rows.Add("이전/새 순환로 6m 이내 지표 표본 갱신="+count+"; 높이 불변");
}finally{UnityEngine.Object.DestroyImmediate(go);UnityEngine.Object.DestroyImmediate(scratch);}
Physics.SyncTransforms();
authoring.GetMethod("BakeNavigation",flags).Invoke(null,new object[]{scene});
AssetDatabase.SaveAssetIfDirty(data);
AssetDatabase.SaveAssetIfDirty(AssetDatabase.LoadMainAssetAtPath("Assets/10.Datas/World/OpenWorld/Navigation.asset"));
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
rows.Add("COMPLETE; 기존 콘텐츠 생성/삭제 없음");
System.IO.File.WriteAllLines(folder+"/application.txt",rows);
return string.Join("\n",rows.ToArray());
