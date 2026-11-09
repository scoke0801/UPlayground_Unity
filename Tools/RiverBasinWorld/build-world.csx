// 독립 산출물이 이미 있으면 중단한다. 기존 월드와 공유 자원은 저장하지 않는다.
if(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new Exception("Edit Mode 준비 필요");
for(int i=0;i<UnityEngine.SceneManagement.SceneManager.sceneCount;i++) if(UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).isDirty)throw new Exception("열린 씬 미저장 변경 보존을 위해 중단");
string scenePath="Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity", data="Assets/10.Datas/World/RiverBasinWorld", prefabFolder="Assets/03.Prefabs/Environment/RiverBasinWorld", input="Tools/RiverBasinWorld";
if(System.IO.File.Exists(scenePath)||System.IO.Directory.Exists(data))throw new Exception("기존 제작본 덮어쓰기 금지");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var deserialize=json.GetMethod("DeserializeObject",new Type[]{typeof(string),typeof(Type)});
Func<object,Type,object> read=(v,t)=>deserialize.Invoke(null,new object[]{v.ToString(),t});
Func<object,Dictionary<string,object>> dict=v=>(Dictionary<string,object>)read(v,typeof(Dictionary<string,object>));
Func<object,Dictionary<string,object>[]> rows=v=>(Dictionary<string,object>[])read(v,typeof(Dictionary<string,object>[]));
Func<object,float[][]> points=v=>(float[][])read(v,typeof(float[][]));
Func<float[],Vector3> vector=v=>new Vector3(v[0],v[1],v[2]);
var plan=dict(System.IO.File.ReadAllText(input+"/production-plan.json"));
var placements=rows(plan["placements"]);
foreach(var path in placements.Select(p=>p["prefab"].ToString()).Distinct()){
 var candidate=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(candidate==null)throw new Exception("후보 누락 "+path);
 if(candidate.GetComponentsInChildren<MonoBehaviour>(true).Length>0)throw new Exception("환경 후보 스크립트 검토 필요 "+path);
}
System.IO.Directory.CreateDirectory(System.IO.Path.GetDirectoryName(scenePath));System.IO.Directory.CreateDirectory(data);System.IO.Directory.CreateDirectory(prefabFolder);AssetDatabase.Refresh();
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Single);
var generated=new GameObject("GeneratedEnvironment");var terrainRoot=new GameObject("지형 9타일");terrainRoot.transform.SetParent(generated.transform);
var terrainShader=Shader.Find("Universal Render Pipeline/Terrain/Lit");if(terrainShader==null)throw new Exception("URP Terrain 셰이더 누락");
var terrainMaterial=new Material(terrainShader);AssetDatabase.CreateAsset(terrainMaterial,data+"/Terrain.mat");
string[] sourceLayers={"Assets/10.Datas/World/OpenWorld/LakeVisualV2/Meadow.terrainlayer","Assets/10.Datas/World/OpenWorld/RedForestVisualR1/ForestHumus.terrainlayer","Assets/10.Datas/World/OpenWorld/RedForestVisualR1/ClearingEarth.terrainlayer","Assets/10.Datas/World/OpenWorld/LakeVisualV2/Stone.terrainlayer","Assets/10.Datas/World/OpenWorld/RedForestVisualR1/ClearingEarth.terrainlayer","Assets/10.Datas/World/OpenWorld/LakeVisualV2/Stone.terrainlayer","Assets/10.Datas/World/OpenWorld/LakeVisualV2/Shallows.terrainlayer"};
string[] layerNames={"Meadow","AutumnHumus","Footpath","Rock","Sandstone","Snow","Shore"};var layers=new TerrainLayer[7];
for(int i=0;i<7;i++){var source=AssetDatabase.LoadAssetAtPath<TerrainLayer>(sourceLayers[i]);if(source==null)throw new Exception("지표 레이어 누락");layers[i]=UnityEngine.Object.Instantiate(source);layers[i].name=layerNames[i];layers[i].metallic=0;layers[i].smoothness=.05f;AssetDatabase.CreateAsset(layers[i],data+"/"+layerNames[i]+".terrainlayer");}
layers[1].diffuseTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/04.Images/World/RiverBasinWorld/LeafEarth_Albedo.png");layers[1].normalMapTexture=null;layers[1].tileSize=new Vector2(4,4);layers[1].diffuseRemapMax=new Vector4(.62f,.56f,.45f,1);
layers[4].diffuseRemapMax=new Vector4(.8f,.59f,.36f,1);layers[5].diffuseRemapMax=new Vector4(1,1,1,1);layers[5].diffuseRemapMin=new Vector4(.7f,.74f,.77f,0);
var terrains=new Terrain[3,3];
for(int tz=0;tz<3;tz++)for(int tx=0;tx<3;tx++){
 var td=new TerrainData();td.heightmapResolution=513;td.size=new Vector3(512,288,512);td.alphamapResolution=256;td.terrainLayers=layers;
 var heights=new float[513,513];var bytes=System.IO.File.ReadAllBytes(input+"/Generated/terrain-"+tx+"-"+tz+".f32");Buffer.BlockCopy(bytes,0,heights,0,bytes.Length);td.SetHeights(0,0,heights);
 var alpha=new float[256,256,7];bytes=System.IO.File.ReadAllBytes(input+"/Generated/alpha-"+tx+"-"+tz+".f32");Buffer.BlockCopy(bytes,0,alpha,0,bytes.Length);td.SetAlphamaps(0,0,alpha);
 AssetDatabase.CreateAsset(td,data+"/Terrain_"+tx+"_"+tz+".asset");var go=Terrain.CreateTerrainGameObject(td);go.name="Terrain_"+tx+"_"+tz;go.transform.SetParent(terrainRoot.transform);go.transform.position=new Vector3(-768+512*tx,-32,-768+512*tz);
 var t=go.GetComponent<Terrain>();t.materialTemplate=terrainMaterial;t.heightmapPixelError=4;t.basemapDistance=700;terrains[tx,tz]=t;
}
for(int z=0;z<3;z++)for(int x=0;x<3;x++)terrains[x,z].SetNeighbors(x>0?terrains[x-1,z]:null,z<2?terrains[x,z+1]:null,x<2?terrains[x+1,z]:null,z>0?terrains[x,z-1]:null);
Func<float,float,float> ground=(x,z)=>{int tx=Mathf.Clamp(Mathf.FloorToInt((x+768)/512),0,2),tz=Mathf.Clamp(Mathf.FloorToInt((z+768)/512),0,2);return terrains[tx,tz].SampleHeight(new Vector3(x,0,z))-32;};
var stone=new Material(Shader.Find("Universal Render Pipeline/Lit"));stone.color=new Color(.46f,.43f,.36f);stone.SetFloat("_Smoothness",.12f);stone.mainTexture=layers[3].diffuseTexture;stone.mainTextureScale=Vector2.one;AssetDatabase.CreateAsset(stone,data+"/BridgeStone.mat");
var blockMaterial=new Material(stone);blockMaterial.mainTexture=null;blockMaterial.color=new Color(.42f,.46f,.44f);AssetDatabase.CreateAsset(blockMaterial,data+"/BlockoutStone.mat");
var waterMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));waterMaterial.color=new Color(.065f,.28f,.32f);waterMaterial.SetFloat("_Smoothness",.85f);waterMaterial.SetFloat("_Metallic",.25f);AssetDatabase.CreateAsset(waterMaterial,data+"/WaterBlockout.mat");
var waterRoot=new GameObject("수계 · 평탄 소와 낙차");waterRoot.transform.SetParent(generated.transform);
Action<string,Vector3[],int[]> meshObject=(name,verts,tris)=>{var m=new Mesh();m.name=name;m.vertices=verts;m.triangles=tris;var uv=new Vector2[verts.Length];for(int i=0;i<uv.Length;i++)uv[i]=new Vector2(verts[i].x/6,verts[i].z/6);m.uv=uv;m.RecalculateNormals();m.RecalculateBounds();AssetDatabase.CreateAsset(m,data+"/"+name+".asset");var g=new GameObject(name,typeof(MeshFilter),typeof(MeshRenderer));g.transform.SetParent(waterRoot.transform);g.GetComponent<MeshFilter>().sharedMesh=m;g.GetComponent<MeshRenderer>().sharedMaterial=waterMaterial;};
var river=points(plan["river"]);var vv=new List<Vector3>();var tt=new List<int>();
for(int i=1;i<river.Length;i++){
 Vector3 a=vector(river[i-1]),b=vector(river[i]);int count=Mathf.CeilToInt(Vector2.Distance(new Vector2(a.x,a.z),new Vector2(b.x,b.z))/24);
 for(int j=0;j<count;j++){var c=Vector3.Lerp(a,b,(float)j/count);var d=Vector3.Lerp(a,b,(float)(j+1)/count);float level=c.y;var across=Vector3.Cross((b-a).normalized,Vector3.up).normalized*(i>=river.Length-2?15:7);c.y=level;d.y=level;int k=vv.Count;vv.Add(c-across);vv.Add(d-across);vv.Add(d+across);vv.Add(c+across);tt.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});if(j<count-1||i<river.Length-1){float next=Vector3.Lerp(a,b,(float)(j+1)/count).y;k=vv.Count;vv.Add(d-across);vv.Add(d+across);vv.Add(new Vector3(d.x+across.x,next,d.z+across.z));vv.Add(new Vector3(d.x-across.x,next,d.z-across.z));tt.AddRange(new[]{k,k+1,k+2,k,k+2,k+3});}}
}
for(int i=0;i<tt.Count;i+=3){int swap=tt[i+1];tt[i+1]=tt[i+2];tt[i+2]=swap;}meshObject("RiverPools",vv.ToArray(),tt.ToArray());
vv.Clear();tt.Clear();vv.Add(new Vector3(-15.36f,96,280));for(int i=0;i<=96;i++){float a=i*Mathf.PI*2/96;vv.Add(new Vector3(-15.36f+190*Mathf.Sin(a),96,280+150*Mathf.Cos(a)));if(i>0)tt.AddRange(new[]{0,i,i+1});}meshObject("LakeSurface",vv.ToArray(),tt.ToArray());
meshObject("SeaSurface",new[]{new Vector3(-768,0,-768),new Vector3(-768,0,-640),new Vector3(768,0,-640),new Vector3(768,0,-768)},new[]{0,1,2,0,2,3});
var bridge=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/08.Models/World/RiverBasinWorld/B1_StoneBridge.fbx"));bridge.name="Castle_Moat_Bridge (RiverBasinWorld)";
var renderers=bridge.GetComponentsInChildren<MeshRenderer>(true);foreach(var r in renderers){r.sharedMaterial=stone;r.gameObject.SetActive(true);}
var oldLod=bridge.GetComponent<LODGroup>();if(oldLod!=null)UnityEngine.Object.DestroyImmediate(oldLod);var lod=bridge.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.22f,new Renderer[]{renderers[0]}),new LOD(.08f,new Renderer[]{renderers[1]}),new LOD(.015f,new Renderer[]{renderers[2]})});lod.RecalculateBounds();
var deck=bridge.AddComponent<BoxCollider>();deck.center=new Vector3(0,-.35f,0);deck.size=new Vector3(5.6f,.7f,36);
foreach(float x in new[]{-2.68f,2.68f}){var side=bridge.AddComponent<BoxCollider>();side.center=new Vector3(x,.45f,0);side.size=new Vector3(.24f,.9f,35);}
var bridgeAsset=PrefabUtility.SaveAsPrefabAsset(bridge,prefabFolder+"/B1_StoneBridge.prefab");UnityEngine.Object.DestroyImmediate(bridge);
var crossings=new GameObject("교량과 연속 지지면");crossings.transform.SetParent(generated.transform);
foreach(var b in rows(plan["bridges"])){var g=(GameObject)PrefabUtility.InstantiatePrefab(bridgeAsset);g.name="Castle_Moat_Bridge (RiverBasinWorld "+b["id"]+")";g.transform.SetParent(crossings.transform);g.transform.position=vector((float[])read(b["position"],typeof(float[])));g.transform.rotation=Quaternion.Euler(0,Convert.ToSingle(b["yaw"]),0);}
var art=new GameObject("B1 대표 구간 · 생활과 군락");art.transform.SetParent(generated.transform);var village=new GameObject("호수 마을 · 블록아웃");village.transform.SetParent(generated.transform);
var materialCopies=new Dictionary<Material,Material>();
foreach(var p in placements){string path=p["prefab"].ToString();var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));g.transform.SetParent(p["role"].ToString()=="마을 블록아웃"?village.transform:art.transform);float x=Convert.ToSingle(p["x"]),z=Convert.ToSingle(p["z"]);g.transform.position=new Vector3(x,ground(x,z),z);g.transform.localScale*=Convert.ToSingle(p["scale"]);g.transform.rotation=Quaternion.Euler(0,Convert.ToSingle(p["yaw"]),0);
 if(path.Contains("BroadleafTree"))foreach(var r in g.GetComponentsInChildren<Renderer>(true)){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++){if(mats[i]==null||!mats[i].name.Contains("Broadleaf"))continue;Material copy;if(!materialCopies.TryGetValue(mats[i],out copy)){copy=new Material(mats[i]);if(copy.HasProperty("_BaseColor"))copy.SetColor("_BaseColor",new Color(1,.68f,.28f,1));if(copy.HasProperty("_Color"))copy.SetColor("_Color",new Color(1,.68f,.28f,1));AssetDatabase.CreateAsset(copy,data+"/AutumnLeaves_"+materialCopies.Count+".mat");materialCopies.Add(mats[i],copy);}mats[i]=copy;}r.sharedMaterials=mats;}
}
Action<string,Vector3,Vector3,Transform,Material> cube=(name,pos,size,parent,mat)=>{var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(parent);g.transform.position=pos;g.transform.localScale=size;g.GetComponent<Renderer>().sharedMaterial=mat;};
var landmarks=new GameObject("주요 거점 · 회색 공간 검토");landmarks.transform.SetParent(generated.transform);
foreach(var pad in rows(plan["pads"]).Skip(4)){float x=Convert.ToSingle(pad["x"]),z=Convert.ToSingle(pad["z"]),y=Convert.ToSingle(pad["y"]);var root=new GameObject("Dng_Wall_Plain (RiverBasinWorld 거점) "+x+"_"+z);root.transform.SetParent(landmarks.transform);cube("기단",new Vector3(x,y-.4f,z),new Vector3(26,.8f,22),root.transform,blockMaterial);cube("후면 실루엣",new Vector3(x,y+5,z+9),new Vector3(22,10,2),root.transform,blockMaterial);cube("서측 경계",new Vector3(x-12,y+3,z),new Vector3(2,6,16),root.transform,blockMaterial);}
var fields=new GameObject("외곽 텃밭 · 통행 밖 작업면");fields.transform.SetParent(art.transform);
for(int f=0;f<2;f++)for(int i=0;i<5;i++){float x=112+f*13+i*1.7f,z=37;cube("밭고랑",new Vector3(x,ground(x,z)+.08f,z),new Vector3(.55f,.16f,8),fields.transform,stone);}
var context=new GameObject("RiverBasinWorld · 씬 계약").AddComponent<UPlayGround.SceneContext>();context.SceneType=UPlayGround.Data.EnumType.SceneType.GamePlay;context.MapID="RiverBasinWorld";
var region=ScriptableObject.CreateInstance<UPlayGround.Data.UI.MapRegionInfoSO>();region.continentName="물길 분지";AssetDatabase.CreateAsset(region,data+"/RegionInfo.asset");
var map=ScriptableObject.CreateInstance<UPlayGround.Data.UI.MinimapIconConfigSO>();map.captureCenter=Vector2.zero;map.captureWorldSize=1536;map.captureWorldSizeXY=new Vector2(1536,1536);AssetDatabase.CreateAsset(map,data+"/MapConfig.asset");context.WorldMapConfig=map;
var sc=new SerializedObject(context);sc.FindProperty("_regionInfoOverride").objectReferenceValue=region;sc.ApplyModifiedPropertiesWithoutUndo();
var player=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Player.prefab"));player.name="Player · RiverBasinWorld";player.transform.position=new Vector3(130,ground(130,22)+.15f,22);player.transform.rotation=Quaternion.Euler(0,95,0);
var arrival=new GameObject("Arrival · RiverBasinWorld.B1").AddComponent<UPlayGround.SceneArrivalPoint>();arrival.transform.position=player.transform.position;var arr=new SerializedObject(arrival);arr.FindProperty("_id").stringValue="RiverBasinWorld.B1";arr.ApplyModifiedPropertiesWithoutUndo();
var sun=new GameObject("Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.6f;sun.color=new Color(1,.94f,.84f);sun.transform.rotation=Quaternion.Euler(42,-35,0);sun.shadows=LightShadows.Soft;RenderSettings.sun=sun;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.48f,.59f,.67f);RenderSettings.ambientEquatorColor=new Color(.37f,.42f,.40f);RenderSettings.ambientGroundColor=new Color(.18f,.20f,.17f);RenderSettings.fog=true;RenderSettings.fogMode=FogMode.ExponentialSquared;RenderSettings.fogDensity=.00065f;RenderSettings.fogColor=new Color(.60f,.70f,.74f);
var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=player.transform.position+new Vector3(-4,2,-1);camera.transform.rotation=Quaternion.LookRotation(player.transform.position+Vector3.up*1.2f-camera.transform.position);camera.fieldOfView=60;camera.farClipPlane=2400;camera.nearClipPlane=.1f;camera.gameObject.AddComponent<AudioListener>();
var navigation=generated.AddComponent<Unity.AI.Navigation.NavMeshSurface>();navigation.collectObjects=Unity.AI.Navigation.CollectObjects.Children;navigation.useGeometry=UnityEngine.AI.NavMeshCollectGeometry.PhysicsColliders;navigation.overrideVoxelSize=true;navigation.voxelSize=.3f;
foreach(var layer in layers)AssetDatabase.SaveAssetIfDirty(layer);AssetDatabase.SaveAssetIfDirty(terrainMaterial);AssetDatabase.SaveAssetIfDirty(stone);AssetDatabase.SaveAssetIfDirty(blockMaterial);AssetDatabase.SaveAssetIfDirty(waterMaterial);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,scenePath))throw new Exception("씬 저장 실패");
Physics.SyncTransforms();return "RiverBasinWorld 생성. NavMesh 베이크와 실측 검수 필요";
