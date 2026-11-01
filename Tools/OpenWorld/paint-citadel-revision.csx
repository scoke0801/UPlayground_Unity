var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.path!=UPlayGround.Editor.OpenWorldSliceAuthoring.HostPath)throw new Exception("월드 Edit Mode 필요");
var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();var data=terrain.terrainData;
var folder="output/OpenWorld/CitadelArtRevision";var backup=folder+"/backup/Terrain.asset";
if(!System.IO.File.Exists(backup))System.IO.File.Copy(AssetDatabase.GetAssetPath(data),backup,false);
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var settings=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/citadel-revision-surface.json"),typeof(Dictionary<string,object>));
var box=(float[])read(settings["bounds"].ToString(),typeof(float[]));
int grass=Convert.ToInt32(settings["grassLayer"]),yellow=Convert.ToInt32(settings["yellowGrassLayer"]),yard=Convert.ToInt32(settings["yardLayer"]),dark=Convert.ToInt32(settings["darkSoilLayer"]);
float margin=Convert.ToSingle(settings["yardMargin"]),roadMargin=Convert.ToSingle(settings["roadMargin"]),blend=Convert.ToSingle(settings["edgeBlend"]);
var root=GameObject.Find("AuthoredEnvironment").transform.Find("월드 구간/돌등성");
Func<string,List<Bounds>> bounds=path=>{
 var result=new List<Bounds>();var parent=root.Find(path);if(parent==null)return result;
 foreach(var t in parent.GetComponentsInChildren<Transform>(true)){
  if(!PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject))continue;
  var rs=t.GetComponentsInChildren<MeshRenderer>(true);if(rs.Length==0)continue;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);result.Add(b);
 }return result;
};
var houses=bounds("생활 구획");var roads=bounds("도로");roads.AddRange(bounds("시장/작은 중앙 광장"));
float sx=data.size.x/data.alphamapWidth,sz=data.size.z/data.alphamapHeight;var origin=terrain.transform.position;
int x0=Mathf.Clamp(Mathf.FloorToInt((box[0]-origin.x)/sx),0,data.alphamapWidth-1),z0=Mathf.Clamp(Mathf.FloorToInt((box[1]-origin.z)/sz),0,data.alphamapHeight-1);
int x1=Mathf.Clamp(Mathf.CeilToInt((box[2]-origin.x)/sx),x0+1,data.alphamapWidth),z1=Mathf.Clamp(Mathf.CeilToInt((box[3]-origin.z)/sz),z0+1,data.alphamapHeight);
var map=data.GetAlphamaps(x0,z0,x1-x0,z1-z0);
for(int z=0;z<z1-z0;z++)for(int x=0;x<x1-x0;x++){
 float wx=origin.x+(x0+x+.5f)*sx,wz=origin.z+(z0+z+.5f)*sz;
 float noise=Mathf.PerlinNoise(wx*.07f,wz*.07f),houseWeight=0,roadWeight=0;
 foreach(var b in houses){float dx=(wx-b.center.x)/(b.extents.x+margin),dz=(wz-b.center.z)/(b.extents.z+margin);float d=Mathf.Sqrt(dx*dx+dz*dz);houseWeight=Mathf.Max(houseWeight,Mathf.Clamp01((1.15f-d)*3)*(0.85f+noise*.15f));}
 foreach(var b in roads){float dx=Mathf.Max(0,Mathf.Abs(wx-b.center.x)-b.extents.x-roadMargin),dz=Mathf.Max(0,Mathf.Abs(wz-b.center.z)-b.extents.z-roadMargin);roadWeight=Mathf.Max(roadWeight,Mathf.Clamp01(1-Mathf.Sqrt(dx*dx+dz*dz)/blend));}
 houseWeight*=Convert.ToSingle(settings["yardStrength"]);roadWeight*=Convert.ToSingle(settings["roadStrength"]);
 float soil=Mathf.Max(houseWeight,roadWeight),dry=(.05f+noise*.11f)*(1-soil),wet=houseWeight*.08f;
 for(int l=0;l<data.alphamapLayers;l++)map[z,x,l]=0;
 map[z,x,grass]=1-soil-dry;map[z,x,yellow]=dry;map[z,x,yard]=soil-wet;map[z,x,dark]=wet;
}
data.SetAlphamaps(x0,z0,map);terrain.Flush();AssetDatabase.SaveAssetIfDirty(data);
System.IO.File.WriteAllText(folder+"/surface.txt","지형 높이·구멍 보존 / 알파맵 부분 수정 "+(x1-x0)+"x"+(z1-z0)+" / 마당 "+houses.Count+" / 포장 "+roads.Count);
return System.IO.File.ReadAllText(folder+"/surface.txt");
