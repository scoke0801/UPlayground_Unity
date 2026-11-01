var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling)throw new Exception("월드 Edit Mode 필요");
var terrain=Terrain.activeTerrain;var data=terrain.terrainData;var origin=terrain.transform.position;
if(!System.IO.File.Exists("output/OpenWorld/HarborArtRevision/backup/Terrain.asset"))throw new Exception("지형 백업 필요");
// 내륙의 직사각형 수조 자국을 메우되 바다와 기존 주민 지면은 보존한다.
float hs=data.size.x/(data.heightmapResolution-1),hz=data.size.z/(data.heightmapResolution-1);
int hx0=Mathf.FloorToInt((1835-origin.x)/hs),hz0=Mathf.FloorToInt((-318-origin.z)/hz);
int hx1=Mathf.CeilToInt((1888-origin.x)/hs),hz1=Mathf.CeilToInt((-255-origin.z)/hz);
var heights=data.GetHeights(hx0,hz0,hx1-hx0+1,hz1-hz0+1);
for(int z=0;z<heights.GetLength(0);z++)for(int x=0;x<heights.GetLength(1);x++){
 float wx=origin.x+(hx0+x)*hs,wz=origin.z+(hz0+z)*hz;
 float edge=Mathf.Min(Mathf.Min(wx-1835,1888-wx),Mathf.Min(wz+318,-255-wz));float w=Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/7));
 heights[z,x]=Mathf.Lerp(heights[z,x],(50-origin.y)/data.size.y,w);
}
data.SetHeights(hx0,hz0,heights);
var root=GameObject.Find("AuthoredEnvironment").transform.Find("월드 구간/항구");
var houses=new List<Bounds>();var roads=new List<Bounds>();
foreach(var t in root.GetComponentsInChildren<Transform>(true)){
 if(!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))continue;
 string path=AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(t.gameObject));
 bool grounded=path.Contains("/LakeVillage/")||path.Contains("/PoorVillage/")||path.Contains("/StonegateEast/")||path.Contains("/Foliage/");
 if(grounded&&t.position.z> -322&&!(path.Contains("Road_")||path.Contains("Terrain_Boulder"))){
  var rs=t.GetComponentsInChildren<Renderer>(true);if(rs.Length==0)continue;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
  t.position+=Vector3.up*(terrain.SampleHeight(t.position)+origin.y-b.min.y);PrefabUtility.RecordPrefabInstancePropertyModifications(t);
 }
 var renderers=t.GetComponentsInChildren<Renderer>(true);if(renderers.Length==0)continue;var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
 if(path.Contains("House_"))houses.Add(bounds);if(path.Contains("Road_"))roads.Add(bounds);
}
float sx=data.size.x/data.alphamapWidth,sz=data.size.z/data.alphamapHeight;
int x0=Mathf.FloorToInt((1840-origin.x)/sx),z0=Mathf.FloorToInt((-326-origin.z)/sz),x1=Mathf.CeilToInt((1995-origin.x)/sx),z1=Mathf.CeilToInt((-218-origin.z)/sz);
var map=data.GetAlphamaps(x0,z0,x1-x0,z1-z0);
for(int z=0;z<z1-z0;z++)for(int x=0;x<x1-x0;x++){
 float wx=origin.x+(x0+x+.5f)*sx,wz=origin.z+(z0+z+.5f)*sz,noise=Mathf.PerlinNoise(wx*.13f,wz*.13f),soil=0;
 foreach(var b in houses){float dx=Mathf.Max(0,Mathf.Abs(wx-b.center.x)-b.extents.x),dz=Mathf.Max(0,Mathf.Abs(wz-b.center.z)-b.extents.z);soil=Mathf.Max(soil,Mathf.Clamp01(1-Mathf.Sqrt(dx*dx+dz*dz)/4)*.7f);}
 foreach(var b in roads){float dx=Mathf.Max(0,Mathf.Abs(wx-b.center.x)-b.extents.x),dz=Mathf.Max(0,Mathf.Abs(wz-b.center.z)-b.extents.z);soil=Mathf.Max(soil,Mathf.Clamp01(1-Mathf.Sqrt(dx*dx+dz*dz)/2)*.8f);}
 float shore=Mathf.Clamp01((-wz-310)/15)*.75f;soil=Mathf.Max(soil,shore);float dry=(.12f+noise*.18f)*(1-soil);
 float edge=Mathf.Min(Mathf.Min(wx-1840,1995-wx),Mathf.Min(wz+326,-218-wz));float weight=Mathf.Clamp01(edge/5);
 for(int l=0;l<data.alphamapLayers;l++){float target=l==0?1-soil-dry:l==3?dry:l==5?soil:0;map[z,x,l]=Mathf.Lerp(map[z,x,l],target,weight);}
}
data.SetAlphamaps(x0,z0,map);terrain.Flush();AssetDatabase.SaveAssetIfDirty(data);
// 선체의 분수 제곱 입력이 잘못되면 bounds보다 먼저 원인을 드러낸다.
foreach(string guid in AssetDatabase.FindAssets("t:Mesh",new[]{"Assets/03.Prefabs/Environment/HarborRevision"})){
 var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));foreach(var v in mesh.vertices)if(float.IsNaN(v.x)||float.IsNaN(v.y)||float.IsNaN(v.z))throw new Exception("유효하지 않은 항만 메시 "+mesh.name);
}
var sea=root.Find("바다").GetComponentInChildren<Renderer>();string seaPath="Assets/03.Prefabs/Environment/HarborRevision/Harbor_Sea.mat";var seaMaterial=AssetDatabase.LoadAssetAtPath<Material>(seaPath);
if(seaMaterial==null){seaMaterial=new Material(sea.sharedMaterial);seaMaterial.name="Harbor_Sea";seaMaterial.SetFloat("_Smoothness",.3f);seaMaterial.SetFloat("_SpecularHighlights",0);seaMaterial.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");AssetDatabase.CreateAsset(seaMaterial,seaPath);}
sea.sharedMaterial=seaMaterial;EditorUtility.SetDirty(sea);
var navigation=GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();navigation.BuildNavMesh();
// 공용 Navigation.asset은 보존하고 이 개정 전용 산출물로 연결한다.
string navFolder="Assets/10.Datas/World/OpenWorld/HarborArtRevision";System.IO.Directory.CreateDirectory(navFolder);AssetDatabase.Refresh();
string navPath=navFolder+"/HarborRevisionNavigation.asset";var savedNavigation=AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(navPath);
if(savedNavigation==null)AssetDatabase.CreateAsset(navigation.navMeshData,navPath);
else{var baked=navigation.navMeshData;navigation.RemoveData();EditorUtility.CopySerialized(baked,savedNavigation);EditorUtility.SetDirty(savedNavigation);AssetDatabase.SaveAssetIfDirty(savedNavigation);navigation.navMeshData=savedNavigation;navigation.AddData();}
EditorUtility.SetDirty(navigation);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.File.WriteAllText("output/OpenWorld/HarborArtRevision/surface.txt","내륙 수조 복구 1835..1888 / -318..-255, 항구 알파맵 부분 저작, 건물 접지 재정렬");
return "항구 지면 및 메시 bounds 정리";
