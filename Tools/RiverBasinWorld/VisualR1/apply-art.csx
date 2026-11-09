var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlaying||scene.isDirty||scene.path!="Assets/01.Scenes/Ingame/Worlds/RiverBasinWorld.unity")throw new Exception("저장된 전용 씬 필요");
if(GameObject.Find("B1_ArtR1")!=null)throw new Exception("적용된 미술 구간 보존. 재실행 금지");
string input="Tools/RiverBasinWorld/VisualR1",data="Assets/10.Datas/World/RiverBasinWorld/VisualR1",prefabs="Assets/03.Prefabs/Environment/RiverBasinWorld/VisualR1";
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<object,Type,object> read=(v,t)=>parse.Invoke(null,new object[]{v.ToString(),t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText(input+"/art-plan.json"),typeof(Dictionary<string,object>));var placements=(Dictionary<string,object>[])read(plan["objects"],typeof(Dictionary<string,object>[]));
foreach(var path in placements.Select(p=>p["prefab"].ToString()).Distinct()){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(asset==null||asset.GetComponentsInChildren<MonoBehaviour>(true).Length>0)throw new Exception("환경 후보 검토 필요 "+path);}
System.IO.Directory.CreateDirectory(data);System.IO.Directory.CreateDirectory(prefabs);AssetDatabase.Refresh();
Undo.IncrementCurrentGroup();int undoGroup=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("B1 대표 구간 미술 R1");
var root=new GameObject("B1_ArtR1");root.transform.SetParent(GameObject.Find("GeneratedEnvironment").transform);Undo.RegisterCreatedObjectUndo(root,"B1 미술");
foreach(int tx in new[]{1,2}){var td=GameObject.Find("Terrain_"+tx+"_1").GetComponent<Terrain>().terrainData;Undo.RegisterCompleteObjectUndo(td,"B1 호안과 표면");var heights=new float[513,513];var bytes=System.IO.File.ReadAllBytes(input+"/Generated/Terrain_"+tx+"_1.f32");Buffer.BlockCopy(bytes,0,heights,0,bytes.Length);td.SetHeights(0,0,heights);var alpha=new float[256,256,7];bytes=System.IO.File.ReadAllBytes(input+"/Generated/Terrain_"+tx+"_1.alpha.f32");Buffer.BlockCopy(bytes,0,alpha,0,bytes.Length);td.SetAlphamaps(0,0,alpha);AssetDatabase.SaveAssetIfDirty(td);}
var terrains=Terrain.activeTerrains;Func<float,float,float> ground=(x,z)=>{var t=terrains.First(a=>x>=a.transform.position.x&&x<=a.transform.position.x+512&&z>=a.transform.position.z&&z<=a.transform.position.z+512);return t.SampleHeight(new Vector3(x,0,z))+t.transform.position.y;};
var previous=GameObject.Find("B1 대표 구간 · 생활과 군락").transform;var hidden=new List<object>();
foreach(var child in previous.Cast<Transform>().ToArray()){var p=child.position;if(p.x<225||p.x>390||p.z<-105||p.z>85)continue;if(!child.name.Contains("BroadleafTree")&&!child.name.Contains("PF_EastLands_RiverRock")&&!child.name.Contains("PF_Farm_"))continue;Undo.RecordObject(child.gameObject,"초기 미술 배치 보존 비활성");hidden.Add(new{name=child.name,position=new[]{p.x,p.y,p.z},globalId=GlobalObjectId.GetGlobalObjectIdSlow(child).ToString()});child.gameObject.SetActive(false);}
var materialCache=new Dictionary<string,Material>();
Func<Material,string,string,Material> material=(source,palette,role)=>{
 string key=AssetDatabase.GetAssetPath(source)+"|"+palette+"|"+role;Material m;if(materialCache.TryGetValue(key,out m))return m;m=new Material(source);m.enableInstancing=true;
 if(m.HasProperty("_Foliage_Color_Top")&&role!="군락 아래 낙엽"){
  var top=new Color(2.4f,1.2f,.19f,1);var bottom=new Color(1.0f,.51f,.10f,1);
  if(palette=="Copper"){top=new Color(2.2f,.56f,.15f,1);bottom=new Color(.86f,.24f,.06f,1);}if(palette=="Green"){top=new Color(.92f,1.15f,.48f,1);bottom=new Color(.43f,.66f,.23f,1);}
  m.SetColor("_Foliage_Color_Top",top);m.SetColor("_Foliage_Color_Bottom",bottom);if(m.HasProperty("_Always_On_Fresnel_Color"))m.SetColor("_Always_On_Fresnel_Color",Color.black);
 }
 if(m.HasProperty("_Color_Tint")&&source.shader.name.Contains("Trunk"))m.SetColor("_Color_Tint",new Color(.83f,.78f,.68f,1));
 if(source.shader.name.Contains("M_Rocks")){if(m.HasProperty("_Stylization_Color"))m.SetColor("_Stylization_Color",new Color(.45f,.46f,.39f,0));if(m.HasProperty("_Dirt_Color"))m.SetColor("_Dirt_Color",new Color(.24f,.28f,.17f,0));if(m.HasProperty("_Edgewear_Color"))m.SetColor("_Edgewear_Color",new Color(.73f,.70f,.59f,0));}
 string path=data+"/M_"+source.name.Replace(" ","_")+"_"+palette+"_"+materialCache.Count+".mat";AssetDatabase.CreateAsset(m,path);AssetDatabase.SaveAssetIfDirty(m);materialCache.Add(key,m);return m;
};
var variants=new Dictionary<string,GameObject>();
Func<string,string,string,GameObject> variant=(path,palette,role)=>{
 string key=path+"|"+palette+"|"+role;GameObject asset;if(variants.TryGetValue(key,out asset))return asset;
 var g=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));g.name=System.IO.Path.GetFileNameWithoutExtension(path)+"_"+palette;
 foreach(var r in g.GetComponentsInChildren<Renderer>(true)){var mats=r.sharedMaterials;for(int i=0;i<mats.Length;i++)if(mats[i]!=null)mats[i]=material(mats[i],palette,role);r.sharedMaterials=mats;if(role=="길가 낮은 풀"||role=="군락 아래 낙엽")r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;}
 foreach(var group in g.GetComponentsInChildren<LODGroup>()){
  var old=group.GetLODs();var keep=old.Where(l=>!l.renderers.Any(r=>r!=null&&r.sharedMaterials.Any(m=>m!=null&&m.shader.name.Contains("Impostor")))).Take(3).ToArray();
  if(keep.Length>0){var kept=new HashSet<Renderer>(keep.SelectMany(l=>l.renderers));foreach(var r in old.SelectMany(l=>l.renderers).Where(r=>r!=null))r.enabled=kept.Contains(r);for(int i=0;i<keep.Length;i++)keep[i].screenRelativeTransitionHeight=new[]{.32f,.10f,.015f}[i];group.SetLODs(keep);group.RecalculateBounds();}
 }
 asset=PrefabUtility.SaveAsPrefabAsset(g,prefabs+"/"+g.name+"_"+variants.Count+".prefab");UnityEngine.Object.DestroyImmediate(g);variants.Add(key,asset);return asset;
};
var groups=new Dictionary<string,Transform>();var resolved=new List<object>();
foreach(var item in placements){string path=item["prefab"].ToString(),palette=item["palette"].ToString(),role=item["role"].ToString();Transform parent;if(!groups.TryGetValue(role,out parent)){parent=new GameObject(role).transform;parent.SetParent(root.transform);groups.Add(role,parent);}
 var g=(GameObject)PrefabUtility.InstantiatePrefab(variant(path,palette,role),parent);float x=Convert.ToSingle(item["x"]),z=Convert.ToSingle(item["z"]);float y=ground(x,z);g.transform.position=new Vector3(x,y,z);g.transform.rotation=Quaternion.Euler(0,Convert.ToSingle(item["yaw"]),0);g.transform.localScale*=Convert.ToSingle(item["scale"]);
 if(role=="군락 아래 낙엽")g.transform.position+=Vector3.up*.025f;
 if(role=="주요 암반"&&g.GetComponentsInChildren<Collider>().Length==0){var render=g.GetComponentsInChildren<Renderer>().First();var bounds=render.localBounds;var collider=render.gameObject.AddComponent<BoxCollider>();collider.center=bounds.center;collider.size=bounds.size*.82f;}
 resolved.Add(new{prefab=path,role,position=new[]{x,g.transform.position.y,z},scale=g.transform.localScale.x});
}
var importer=AssetImporter.GetAtPath("Assets/04.Images/World/RiverBasinWorld/VisualR1/GraniteAlbedo.png") as TextureImporter;importer.sRGBTexture=true;importer.wrapMode=TextureWrapMode.Repeat;importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=true;importer.anisoLevel=4;importer.textureCompression=TextureImporterCompression.CompressedHQ;importer.SaveAndReimport();
var granite=new Material(Shader.Find("Universal Render Pipeline/Lit"));granite.name="B1_Granite";granite.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/04.Images/World/RiverBasinWorld/VisualR1/GraniteAlbedo.png");granite.color=new Color(.83f,.82f,.78f,1);granite.SetFloat("_Smoothness",.10f);AssetDatabase.CreateAsset(granite,data+"/BridgeGranite.mat");AssetDatabase.SaveAssetIfDirty(granite);
var oldBridge=GameObject.Find("Castle_Moat_Bridge (RiverBasinWorld B1)");Undo.RecordObject(oldBridge,"B1 원형 보존");oldBridge.SetActive(false);
var bridge=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/08.Models/World/RiverBasinWorld/VisualR1/B1_BridgeArtR1.fbx"));bridge.name="Castle_Moat_Bridge (RiverBasinWorld B1 ArtR1)";var meshes=bridge.GetComponentsInChildren<MeshRenderer>(true);foreach(var r in meshes){r.enabled=true;r.gameObject.SetActive(true);r.sharedMaterial=granite;}
var lod=bridge.GetComponent<LODGroup>()??bridge.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.22f,new Renderer[]{meshes[0]}),new LOD(.075f,new Renderer[]{meshes[1]}),new LOD(.012f,new Renderer[]{meshes[2]})});lod.RecalculateBounds();
var deck=bridge.AddComponent<BoxCollider>();deck.center=new Vector3(0,-.35f,0);deck.size=new Vector3(5.6f,.7f,36);foreach(float x in new[]{-2.68f,2.68f}){var side=bridge.AddComponent<BoxCollider>();side.center=new Vector3(x,.45f,0);side.size=new Vector3(.24f,.9f,35);}
var bridgeAsset=PrefabUtility.SaveAsPrefabAsset(bridge,prefabs+"/B1_BridgeArtR1.prefab");UnityEngine.Object.DestroyImmediate(bridge);bridge=(GameObject)PrefabUtility.InstantiatePrefab(bridgeAsset,root.transform);bridge.name="Castle_Moat_Bridge (RiverBasinWorld B1 ArtR1)";bridge.transform.position=new Vector3(307.2f,77,-15.36f);bridge.transform.rotation=Quaternion.Euler(0,90,0);
// 전용 하천의 근경 부분만 교체하고 기존 원본 메시를 보존한다.
var riverObject=GameObject.Find("RiverPools");var originalMesh=riverObject.GetComponent<MeshFilter>().sharedMesh;var outside=UnityEngine.Object.Instantiate(originalMesh);outside.name="RiverOutsideArtR1";var vertices=outside.vertices;var oldTriangles=outside.triangles;var keptTriangles=new List<int>();
for(int i=0;i<oldTriangles.Length;i+=3){var center=(vertices[oldTriangles[i]]+vertices[oldTriangles[i+1]]+vertices[oldTriangles[i+2]])/3;if(center.z>=-85&&center.z<=65&&center.x>200&&center.x<400)continue;keptTriangles.Add(oldTriangles[i]);keptTriangles.Add(oldTriangles[i+1]);keptTriangles.Add(oldTriangles[i+2]);}outside.triangles=keptTriangles.ToArray();AssetDatabase.CreateAsset(outside,data+"/RiverOutside.asset");riverObject.GetComponent<MeshFilter>().sharedMesh=outside;
var river=(float[][])read(plan["river"],typeof(float[][]));var vv=new List<Vector3>();var tt=new List<int>();for(int i=0;i<river.Length;i++){var p=river[i];float taper=Mathf.Sin(Mathf.PI*i/(river.Length-1));float width=7+6*Mathf.Sqrt(Mathf.Max(0,taper));vv.Add(new Vector3(p[0]-width,72,p[2]));vv.Add(new Vector3(p[0]+width,72,p[2]));if(i>0){int k=i*2;tt.AddRange(new[]{k-2,k,k+1,k-2,k+1,k-1});}}
var waterMesh=new Mesh();waterMesh.name="B1RiverSurface";waterMesh.SetVertices(vv);waterMesh.SetTriangles(tt,0);waterMesh.uv=vv.Select(v=>new Vector2(v.x/6,v.z/6)).ToArray();waterMesh.RecalculateNormals();waterMesh.RecalculateBounds();AssetDatabase.CreateAsset(waterMesh,data+"/B1RiverSurface.asset");
var waterMaterial=new Material(AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/RiverBasinWorld/RiverWater.mat"));waterMaterial.SetColor("_ShallowColor",new Color(.18f,.36f,.32f,1));waterMaterial.SetColor("_DeepColor",new Color(.045f,.18f,.21f,1));waterMaterial.SetFloat("_WaveScale",.3f);waterMaterial.SetFloat("_WaveStrength",.2f);waterMaterial.SetFloat("_FoamStrength",.35f);waterMaterial.SetFloat("_FoamWidth",.5f);waterMaterial.SetFloat("_DepthDistance",2);AssetDatabase.CreateAsset(waterMaterial,data+"/B1RiverWater.mat");
var water=new GameObject("B1 · 湾曲水面",typeof(MeshFilter),typeof(MeshRenderer));water.transform.SetParent(root.transform);water.GetComponent<MeshFilter>().sharedMesh=waterMesh;water.GetComponent<MeshRenderer>().sharedMaterial=waterMaterial;
Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);Undo.CollapseUndoOperations(undoGroup);
string result=(string)json.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{new{hidden,resolved,variants=variants.Count,materials=materialCache.Count}});System.IO.File.WriteAllText("output/RiverBasinWorld/VisualR1/application.json",result);return "B1 미술 적용: "+resolved.Count+" 배치 / 이전 환경 "+hidden.Count+" 보존 비활성";
