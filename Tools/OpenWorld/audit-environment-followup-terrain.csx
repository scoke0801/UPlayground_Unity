// 디스크 백업과 현재 Terrain을 비교해 저작 영역 밖의 높이·표면 보존을 검사한다.
string folder="output/OpenWorld/EnvironmentFollowup";var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode 필요");
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();var data=terrain.terrainData;var origin=terrain.transform.position;
var loaded=UnityEditorInternal.InternalEditorUtility.LoadSerializedFileAndForget(folder+"/backup/Assets/10.Datas/World/OpenWorld/Terrain.asset");var original=loaded.OfType<TerrainData>().Single();
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/environment-followup-plan.json"),typeof(Dictionary<string,object>));
Func<string,Dictionary<string,object>[]> patches=key=>(Dictionary<string,object>[])read(plan[key].ToString(),typeof(Dictionary<string,object>[]));
Func<object,float[]> numbers=o=>(float[])read(o.ToString(),typeof(float[]));
var heightPatches=patches("heightFeatures").Select(f=>new[]{numbers(f["center"])[0],numbers(f["center"])[1],numbers(f["radius"])[0],numbers(f["radius"])[1]}).ToArray();
var paintPatches=patches("paint").Select(f=>new[]{numbers(f["center"])[0],numbers(f["center"])[1],numbers(f["radius"])[0]*1.25f,numbers(f["radius"])[1]*1.25f}).ToArray();
int heightChanged=0,heightOutside=0,paintChanged=0,paintOutside=0;float maxDelta=0;int resolution=data.heightmapResolution;
var before=original.GetHeights(0,0,resolution,resolution);var after=data.GetHeights(0,0,resolution,resolution);
for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++){
float delta=Mathf.Abs(before[z,x]-after[z,x])*data.size.y;if(delta<.00001f)continue;heightChanged++;maxDelta=Mathf.Max(maxDelta,delta);
float wx=origin.x+x*data.size.x/(resolution-1),wz=origin.z+z*data.size.z/(resolution-1);
if(!heightPatches.Any(p=>Mathf.Pow((wx-p[0])/p[2],2)+Mathf.Pow((wz-p[1])/p[3],2)<1.001f))heightOutside++;
}
var ba=original.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);var aa=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
for(int z=0;z<data.alphamapHeight;z++)for(int x=0;x<data.alphamapWidth;x++){
bool changed=false;for(int l=0;l<data.alphamapLayers;l++)if(Mathf.Abs(ba[z,x,l]-aa[z,x,l])>.00001f)changed=true;if(!changed)continue;paintChanged++;
float wx=origin.x+(x+.5f)*data.size.x/data.alphamapWidth,wz=origin.z+(z+.5f)*data.size.z/data.alphamapHeight;
if(!paintPatches.Any(p=>Mathf.Abs(wx-p[0])<p[2]&&Mathf.Abs(wz-p[1])<p[3]))paintOutside++;
}
bool layers=original.terrainLayers.Select(l=>AssetDatabase.GetAssetPath(l)).SequenceEqual(data.terrainLayers.Select(l=>AssetDatabase.GetAssetPath(l)));
string result="heightChanged="+heightChanged+" heightOutside="+heightOutside+" maxHeightDelta="+maxDelta.ToString("F4")+" paintChanged="+paintChanged+" paintOutside="+paintOutside+" layersPreserved="+layers;
System.IO.File.WriteAllText(folder+"/terrain-preservation.txt",result);foreach(var obj in loaded)UnityEngine.Object.DestroyImmediate(obj);return result;
