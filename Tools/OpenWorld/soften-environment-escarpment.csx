// 변경 높이만 완만하게 연결하고 원래 보존된 표본에는 필터를 번지게 하지 않는다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string folder="output/OpenWorld/EnvironmentForm";
if(scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity"||EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||EditorUtility.scriptCompilationFailed||System.IO.File.Exists(folder+"/softened.txt"))throw new Exception("최종 사면 보정 상태 불일치");
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();var data=terrain.terrainData;int x0=355,z0=2515,w=560,h=640;
var loaded=UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(folder+"/backup/Assets/10.Datas/World/OpenWorld/Terrain.asset");
try{
var baseline=loaded.OfType<TerrainData>().Single().GetHeights(x0,z0,w,h);var current=data.GetHeights(x0,z0,w,h);var original=(float[,])current.Clone();var delta=new float[h,w];for(int z=0;z<h;z++)for(int x=0;x<w;x++)delta[z,x]=(current[z,x]-baseline[z,x])*data.size.y;
for(int pass=0;pass<3;pass++){
var horizontal=new float[h,w];var filtered=new float[h,w];int radius=7;
for(int z=0;z<h;z++)for(int x=0;x<w;x++){float sum=0,weight=0;for(int dx=-radius;dx<=radius;dx++){int xx=Mathf.Clamp(x+dx,0,w-1);float k=radius+1-Mathf.Abs(dx);sum+=delta[z,xx]*k;weight+=k;}horizontal[z,x]=sum/weight;}
for(int z=0;z<h;z++)for(int x=0;x<w;x++){float sum=0,weight=0;for(int dz=-radius;dz<=radius;dz++){int zz=Mathf.Clamp(z+dz,0,h-1);float k=radius+1-Mathf.Abs(dz);sum+=horizontal[zz,x]*k;weight+=k;}float blend=Mathf.SmoothStep(0,1,Mathf.Clamp01(Mathf.Abs((original[z,x]-baseline[z,x])*data.size.y)/3));filtered[z,x]=Mathf.Lerp(delta[z,x],sum/weight,blend);if(Mathf.Abs(original[z,x]-baseline[z,x])<.0000001f)filtered[z,x]=0;}
delta=filtered;
}
for(int z=0;z<h;z++)for(int x=0;x<w;x++)current[z,x]=baseline[z,x]+delta[z,x]/data.size.y;
var rocks=new[]{GameObject.Find("AuthoredEnvironment/환경 후속 개선").transform,GameObject.Find("AuthoredEnvironment/사면과 거점 후속 저작").transform}.SelectMany(t=>t.Cast<Transform>()).Where(t=>t.position.z>1000).ToArray();var oldGround=rocks.Select(t=>terrain.SampleHeight(t.position)).ToArray();data.SetHeights(x0,z0,current);
var moved=System.IO.File.ReadAllLines(folder+"/moved-transforms.txt").ToList();for(int i=0;i<rocks.Length;i++){float change=terrain.SampleHeight(rocks[i].position)-oldGround[i];if(Mathf.Abs(change)<.001f)continue;rocks[i].position+=Vector3.up*change;PrefabUtility.RecordPrefabInstancePropertyModifications(rocks[i]);if(rocks[i].parent.name=="환경 후속 개선")moved.Add(GlobalObjectId.GetGlobalObjectIdSlow(PrefabUtility.GetPrefabInstanceHandle(rocks[i])).ToString());}System.IO.File.WriteAllLines(folder+"/moved-transforms.txt",moved.Distinct().ToArray());
terrain.Flush();Physics.SyncTransforms();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");System.IO.File.WriteAllText(folder+"/softened.txt","PASS 변경 높이 3회 완화, 보존 표본 필터 제외");return "PASS";
}finally{foreach(var asset in loaded)UnityEngine.Object.DestroyImmediate(asset);}
