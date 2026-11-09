var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!="Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity")throw new Exception("저장된 전용 씬 필요");
string data="Assets/10.Datas/World/RiverBasinWorld",input="Tools/RiverBasinWorld";
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var deserialize=json.GetMethod("DeserializeObject",new Type[]{typeof(string),typeof(Type)});
Func<object,Type,object> read=(v,t)=>deserialize.Invoke(null,new object[]{v.ToString(),t});
Func<object,Dictionary<string,object>> dict=v=>(Dictionary<string,object>)read(v,typeof(Dictionary<string,object>));
Func<object,Dictionary<string,object>[]> rows=v=>(Dictionary<string,object>[])read(v,typeof(Dictionary<string,object>[]));
Func<object,float[][]> points=v=>(float[][])read(v,typeof(float[][]));
Func<float[],Vector3> vector=v=>new Vector3(v[0],v[1],v[2]);
var plan=dict(System.IO.File.ReadAllText(input+"/production-plan.json"));

var generated=GameObject.Find("GeneratedEnvironment");var ts=generated.GetComponentsInChildren<Terrain>();var terrains=new Terrain[3,3];
foreach(var t in ts){int tx=Mathf.RoundToInt((t.transform.position.x+768)/512),tz=Mathf.RoundToInt((t.transform.position.z+768)/512);terrains[tx,tz]=t;
var bytes=System.IO.File.ReadAllBytes(input+"/Generated/terrain-"+tx+"-"+tz+".f32");var heights=new float[513,513];Buffer.BlockCopy(bytes,0,heights,0,bytes.Length);t.terrainData.SetHeights(0,0,heights);
bytes=System.IO.File.ReadAllBytes(input+"/Generated/alpha-"+tx+"-"+tz+".f32");var alpha=new float[256,256,7];Buffer.BlockCopy(bytes,0,alpha,0,bytes.Length);t.terrainData.SetAlphamaps(0,0,alpha);AssetDatabase.SaveAssetIfDirty(t.terrainData);}
Func<float,float,float> ground=(x,z)=>{int tx=Mathf.Clamp(Mathf.FloorToInt((x+768)/512),0,2),tz=Mathf.Clamp(Mathf.FloorToInt((z+768)/512),0,2);return terrains[tx,tz].SampleHeight(new Vector3(x,0,z))-32;};

var waterMaterial=AssetDatabase.LoadAssetAtPath<Material>(data+"/WaterBlockout.mat");var waterRoot=GameObject.Find("수계 · 평탄 소와 낙차");
foreach(Transform t in waterRoot.transform.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
Action<string,Vector3[],int[]> meshObject=(name,verts,tris)=>{var m=AssetDatabase.LoadAssetAtPath<Mesh>(data+"/"+name+".asset");m.Clear();m.name=name;m.vertices=verts;m.triangles=tris;var uv=new Vector2[verts.Length];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(verts[i].x/6,verts[i].z/6);m.uv=uv;m.RecalculateNormals();m.RecalculateBounds();EditorUtility.SetDirty(m);AssetDatabase.SaveAssetIfDirty(m);var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(waterRoot.transform);g.GetComponent<MeshFilter>().sharedMesh=m;g.GetComponent<MeshRenderer>().sharedMaterial=waterMaterial;};
var river=points(plan["river"]);var vv=new List<Vector3>();var tt=new List<int>();
for(int i=1;i<river.Length;i++){
 Vector3 a=vector(river[i-1]),b=vector(river[i]);int count=Mathf.CeilToInt(Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z))/24);
 for(int j=0;j<count;j++){var c=Vector3.Lerp(a,b,(float)j/count);var d=Vector3.Lerp(a,b,(float)(j+1)/count);float level=c.y;var across=Vector3.Cross((b-a).normalized,Vector3.up).normalized*(i>=river.Length-2?15:7);c.y=level;d.y=level;int k=vv.Count;vv.Add(c-across);vv.Add(d-across);vv.Add(d+across);vv.Add(c+across);tt.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});if(j<count-1||i<river.Length-1){float next=Vector3.Lerp(a,b,(float)(j+1)/count).y;k=vv.Count;vv.Add(d-across);vv.Add(d+across);vv.Add(new Vector3(d.x+across.x,next,d.z+across.z));vv.Add(new Vector3(d.x-across.x,next,d.z-across.z));tt.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});}}
}
for(int i=0;i<tt.Count;i+=3){int swap=tt[i+1];tt[i+1]=tt[i+2];tt[i+2]=swap;}meshObject("RiverPools",vv.ToArray(),tt.ToArray());
vv.Clear();tt.Clear();vv.Add(new Vector3(-15.36f,96,280));for(int i=0;i<=96;i++){float a=i*Mathf.PI*2/96;vv.Add(new Vector3(-15.36f+190*Mathf.Sin(a),96,280+150*Mathf.Cos(a)));if(i>0)tt.AddRange(new[]{0,i,i+1});}meshObject("LakeSurface",vv.ToArray(),tt.ToArray());
meshObject("SeaSurface",new[]{new Vector3(-768,0,-768),new Vector3(-768,0,-640),new Vector3(768,0,-640),new Vector3(768,0,-768)},new[]{0,1,2,0,2,3});

var placements=rows(plan["placements"]);var art=GameObject.Find("B1 대표 구간 · 생활과 군락");var village=GameObject.Find("호수 마을 · 블록아웃");
foreach(var parent in new[]{art.transform,village.transform})foreach(Transform t in parent.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(t.gameObject);
var materialCopies=new Dictionary<Material,Material>();
foreach(var p in placements){string path=p["prefab"].ToString();var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));g.transform.SetParent(p["role"].ToString()=="마을 블록아웃"?village.transform:art.transform);float x=Convert.ToSingle(p["x"]),z=Convert.ToSingle(p["z"]);g.transform.position=new Vector3(x,ground(x,z),z);g.transform.localScale*=Convert.ToSingle(p["scale"]);g.transform.rotation=Quaternion.Euler(0,Convert.ToSingle(p["yaw"]),0);
 if(path.Contains("BroadleafTree"))foreach(var r in g.GetComponentsInChildren<Renderer>(true)){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){if(mats[i]==null||!mats[i].name.Contains("Broadleaf"))continue;Material copy;if(!materialCopies.TryGetValue(mats[i],out copy)){copy=new Material(mats[i]);if(copy.HasProperty("_BaseColor"))copy.SetColor("_BaseColor",new Color(1,.68f,.28f,1));if(copy.HasProperty("_Color"))copy.SetColor("_Color",new Color(1,.68f,.28f,1));var existing=AssetDatabase.LoadAssetAtPath<Material>(data+"/AutumnLeaves_"+materialCopies.Count+".mat");if(existing!=null){UnityEngine.Object.DestroyImmediate(copy);copy=existing;}else AssetDatabase.CreateAsset(copy,data+"/AutumnLeaves_"+materialCopies.Count+".mat");materialCopies.Add(mats[i],copy);}mats[i]=copy;}r.sharedMaterials=mats;}
}

var stone=AssetDatabase.LoadAssetAtPath<Material>(data+"/BridgeStone.mat");
Action<string,Vector3,Vector3,Transform,Material> cube=(name,pos,size,parent,mat)=>{var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent);g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;};
var fields=new GameObject("외곽 텃밭 · 통행 밖 작업면");fields.transform.SetParent(art.transform);
for(int f=0;f<2;f++)for(int i=0;i<5;i++){float x=112+f*13+i*1.7f,z=37;cube("밭고랑",new Vector3(x,ground(x,z)+.08f,z),new Vector3(.55f,.16f,8),fields.transform,stone);}

foreach(var b in rows(plan["bridges"])){var g=GameObject.Find("Castle_Moat_Bridge (RiverBasinWorld "+b["id"]+")");g.transform.rotation=Quaternion.Euler(0,Convert.ToSingle(b["yaw"]),0);}
Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);return "전용 제작본 지형·수계·군락 수정";
