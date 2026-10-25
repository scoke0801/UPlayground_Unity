// 모델·설정 검증 뒤 전용 프리팹을 만들고 돌등성의 성 건축물만 교체한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="StonegateCitadel"||Application.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("돌등성 Edit Mode 컴파일 완료 필요");
var art=GameObject.Find("StonegateEastArt");
if(art==null)throw new System.Exception("기존 미술 루트 없음");
bool hasCastle=art.transform.Find("HighPolyCastle")!=null;
var jsonType=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var deserialize=jsonType.GetMethod("DeserializeObject",new[]{typeof(string),typeof(System.Type)});
System.Func<object,System.Type,object> Decode=(value,type)=>deserialize.Invoke(null,new object[]{value.ToString(),type});
var config=(System.Collections.Generic.Dictionary<string,object>)Decode(System.IO.File.ReadAllText("ArtSource/WorldCities/EastStyle/castle_highpoly.json"),typeof(System.Collections.Generic.Dictionary<string,object>));
var palette=(System.Collections.Generic.Dictionary<string,float[]>)Decode(config["palette"],typeof(System.Collections.Generic.Dictionary<string,float[]>));
var transitions=(float[])Decode(config["lodTransitions"],typeof(float[]));
string models="Assets/08.Models/WorldCities/StonegateHighPoly/";
string assets="Assets/03.Prefabs/Environment/WorldCities/StonegateHighPoly/";
System.IO.Directory.CreateDirectory(assets);
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var materials=new System.Collections.Generic.Dictionary<string,Material>();
foreach(var pair in palette){
 string path=assets+pair.Key+".mat";
 var material=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(path);
 if(material==null){
  if(pair.Key=="Roof"||pair.Key=="Timber")material=new Material(UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/WorldCities/EastStyle/EastRoof"+(pair.Key=="Roof"?"Clay":"Timber")+".mat"));
  else{
   material=new Material(Shader.Find("Universal Render Pipeline/Lit"));
   material.SetColor("_BaseColor",new Color(pair.Value[0],pair.Value[1],pair.Value[2],1));
   material.SetFloat("_Smoothness",pair.Key=="Bronze"?.25f:.08f);
   material.SetFloat("_Metallic",pair.Key=="Bronze"?.35f:0);
  }
  material.name=pair.Key;material.enableInstancing=true;UnityEditor.AssetDatabase.CreateAsset(material,path);
 }
 materials.Add(pair.Key,material);
}
var prefabs=new System.Collections.Generic.Dictionary<string,GameObject>();
System.Action<Transform,string,Vector3,Vector3> Collider=(parent,name,center,size)=>{
 var part=new GameObject(name);part.transform.SetParent(parent,false);part.layer=LayerMask.NameToLayer("Ground");
 var collider=part.AddComponent<BoxCollider>();collider.center=center;collider.size=size;
};
foreach(string kind in new[]{"Keep","Gate","Watch"}){
 var cfg=(System.Collections.Generic.Dictionary<string,object>)Decode(config[kind.ToLowerInvariant()],typeof(System.Collections.Generic.Dictionary<string,object>));
 float width=System.Convert.ToSingle(cfg["width"]),depth=System.Convert.ToSingle(cfg["depth"]),storey=System.Convert.ToSingle(cfg["storey"]);int floors=System.Convert.ToInt32(cfg["floors"]);
 var holder=new GameObject(kind);var lods=new LOD[3];
 try{
  for(int level=0;level<3;level++){
   string path=models+kind+"_LOD"+level+".fbx";
   var importer=UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.ModelImporter;
   if(importer==null)throw new System.Exception("모델 없음: "+path);
   importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.isReadable=false;importer.addCollider=false;
   foreach(var pair in materials)importer.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);
   importer.SaveAndReimport();
   var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
   var model=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source,scene);model.name="LOD"+level;model.transform.SetParent(holder.transform,false);
   var renderers=model.GetComponentsInChildren<Renderer>();
   foreach(var renderer in renderers){
    string key=renderer.name.Substring(renderer.name.LastIndexOf('_')+1);renderer.sharedMaterial=materials[key];
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
   }
   foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=LayerMask.NameToLayer("Ground");
   lods[level]=new LOD(transitions[level],renderers);
  }
  var group=holder.AddComponent<LODGroup>();group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=true;group.SetLODs(lods);group.RecalculateBounds();
  // 입면 설정과 같은 칸만 막아 닫힌 전각과 열린 누각의 충돌을 구분한다.
  if(kind=="Keep"){
   Collider(holder.transform,"FoundationCollision",new Vector3(0,-4,0),new Vector3(width+3,8,depth+3));
   Collider(holder.transform,"CommandDais",new Vector3(0,.73f,0),new Vector3(10,.5f,7));
   Collider(holder.transform,"EntryLanding",new Vector3(0,.26f,-depth/2-.5f),new Vector3(8,.5f,2));
   for(int step=0;step<System.Convert.ToInt32(cfg["entrySteps"]);step++){
    float run=System.Convert.ToSingle(cfg["entryRun"]),rise=System.Convert.ToSingle(cfg["entryRise"]);
    Collider(holder.transform,"EntryStep",new Vector3(0,.36f-step*rise,-depth/2-1.65f-step*run),new Vector3(8+step*.1f,.3f,run+.04f));
   }
  }else if(kind=="Gate"){
   float passage=System.Convert.ToSingle(cfg["passageWidth"]),height=System.Convert.ToSingle(cfg["passageHeight"]),pier=(width-passage)/2;
   for(int side=-1;side<=1;side+=2)Collider(holder.transform,"GatePier",new Vector3(side*(passage/2+pier/2),height/2,0),new Vector3(pier,height,depth));
   float spring=System.Convert.ToSingle(cfg["archSpring"]),rise=System.Convert.ToSingle(cfg["archRise"]);
   for(int i=0;i<16;i++){
    float x=-passage/2+(i+.5f)*passage/16;
    float edge=Mathf.Max(0,Mathf.Abs(x)-passage/32);
    float bottom=spring+rise*Mathf.Sqrt(1-edge*edge/(passage*passage/4));
    Collider(holder.transform,"ArchHead",new Vector3(x,(height+bottom)/2,0),new Vector3(passage/16,height-bottom,depth));
   }
  }else{
   float foundation=cfg.ContainsKey("foundationHeight")?System.Convert.ToSingle(cfg["foundationHeight"]):5;
   Collider(holder.transform,"WatchFoundation",new Vector3(0,foundation/2,0),new Vector3(width+.6f,foundation,depth+.6f));
  }
  var facades=(System.Collections.Generic.Dictionary<string,object>[])Decode(cfg["facades"],typeof(System.Collections.Generic.Dictionary<string,object>[]));
  float baseHeight=kind=="Keep"?.25f:kind=="Watch"?(cfg.ContainsKey("foundationHeight")?System.Convert.ToSingle(cfg["foundationHeight"])+.2f:5.2f):System.Convert.ToSingle(cfg["passageHeight"]);
  var heights=cfg.ContainsKey("storeyHeights")?(float[])Decode(cfg["storeyHeights"],typeof(float[])):Enumerable.Repeat(storey,floors).ToArray();
  var roofConfig=(System.Collections.Generic.Dictionary<string,object>)Decode(config["roof"],typeof(System.Collections.Generic.Dictionary<string,object>));
  for(int floor=0;floor<floors;floor++){
   float w=width-floor*System.Convert.ToSingle(cfg["shrink"]),d=Mathf.Max(5,depth-floor*System.Convert.ToSingle(cfg["depthShrink"]));
   float b=baseHeight+heights.Take(floor).Sum(),h=heights[floor]-.65f;
   if(floor<floors-1)h=heights[floor]-System.Convert.ToSingle(roofConfig["interstoreyDrop"])-.15f;
   Collider(holder.transform,"Floor",new Vector3(0,b+.1f,0),new Vector3(w+.65f,.32f,d+.65f));
   Collider(holder.transform,"Ceiling",new Vector3(0,b+h-.16f,0),new Vector3(w,.25f,d));
   for(int side=0;side<4;side++){
    bool front=side<2;int sign=side%2==0?-1:1;
    var pattern=(string[])Decode(facades[floor][front?(sign==1?"front":"back"):"side"],typeof(string[]));
    float length=front?w:d,bay=length/pattern.Length;
    // Blender의 +Y 정면은 Unity의 -Z다.
    for(int i=0;i<=pattern.Length;i++){
     float along=-length/2+i*bay;
     var p=front?new Vector3(along,b+h/2,-sign*d/2):new Vector3(sign*w/2,b+h/2,-along);
     Collider(holder.transform,"Column",p,new Vector3(.58f,h,.58f));
    }
    for(int i=0;i<pattern.Length;i++){
     if(pattern[i]=="open")continue;float along=-length/2+(i+.5f)*bay;
     var p=front?new Vector3(along,b+h/2,-sign*d/2):new Vector3(sign*w/2,b+h/2,-along);
     bool enclosed=facades[floor].ContainsKey("enclosed")&&System.Convert.ToBoolean(facades[floor]["enclosed"]);
     if(enclosed)p+=front?new Vector3(0,0,sign*.20f):new Vector3(-sign*.20f,0,0);
     Collider(holder.transform,"WallPanel",p,front?new Vector3(enclosed?bay+.08f:bay-.55f,enclosed?h:h-.5f,enclosed?.42f:.3f):new Vector3(enclosed?.42f:.3f,enclosed?h:h-.5f,enclosed?bay+.08f:bay-.55f));
    }
   }
  }
  string prefabPath=assets+kind+".prefab";
  prefabs.Add(kind,UnityEditor.PrefabUtility.SaveAsPrefabAsset(holder,prefabPath));
 }finally{UnityEngine.Object.DestroyImmediate(holder);}
}
if(hasCastle){
 var castle=art.transform.Find("HighPolyCastle");
 foreach(var pair in prefabs){
  var cfg=(System.Collections.Generic.Dictionary<string,object>)Decode(config[pair.Key.ToLowerInvariant()],typeof(System.Collections.Generic.Dictionary<string,object>));
  var positions=pair.Key=="Watch"?(float[][])Decode(cfg["positions"],typeof(float[][])):new[]{(float[])Decode(cfg["position"],typeof(float[]))};
  for(int i=0;i<positions.Length;i++){
   var building=castle.Find(pair.Key+"_"+i);if(building==null)throw new System.Exception("기존 성 인스턴스 없음");
   UnityEditor.Undo.RecordObject(building,"성 배치 갱신");var p=positions[i];building.localPosition=new Vector3(p[0],p[1],p[2]);
   UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(building);
  }
 }
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
 return "기존 성 인스턴스의 프리팹/LOD/구조 충돌 갱신";
}
UnityEditor.Undo.IncrementCurrentGroup();int undo=UnityEditor.Undo.GetCurrentGroup();UnityEditor.Undo.SetCurrentGroupName("돌등성 고밀도 성 적용");
try{
 var castle=new GameObject("HighPolyCastle");UnityEditor.Undo.RegisterCreatedObjectUndo(castle,"성 생성");castle.transform.SetParent(art.transform,false);
 foreach(Transform old in art.transform){
  bool replace=old.name=="HighKeep"||old.name=="KeepEnclosure"||old.name=="WatchPavilion"||old.name=="GatePavilion"||old.name=="GateDeck"||old.name=="GateFrontRail"||old.name.StartsWith("GateBaluster_")||old.name=="GateDiagonalBrace";
  // 교체되는 성문 기단의 기존 석재는 겹치는 면만 숨긴다.
  if(old.name.StartsWith("PF_EastLands_StoneWall")){
   var rs=old.GetComponentsInChildren<Renderer>();
   if(rs.Length>0){var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);replace|=Mathf.Abs(b.center.z-120)<.1f&&b.center.x>=166&&b.center.x<=194;}
  }
  if(!replace)continue;UnityEditor.Undo.RecordObject(old.gameObject,"기존 성 외형 보존");old.gameObject.SetActive(false);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(old.gameObject);
 }
 foreach(string kind in new[]{"Keep","Gate","Watch"}){
  var cfg=(System.Collections.Generic.Dictionary<string,object>)Decode(config[kind.ToLowerInvariant()],typeof(System.Collections.Generic.Dictionary<string,object>));
  var positions=kind=="Watch"?(float[][])Decode(cfg["positions"],typeof(float[][])):new[]{(float[])Decode(cfg["position"],typeof(float[]))};
  for(int i=0;i<positions.Length;i++){
   var obj=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefabs[kind],scene);UnityEditor.Undo.RegisterCreatedObjectUndo(obj,"성 배치");obj.transform.SetParent(castle.transform,false);obj.name=kind+"_"+i;
   var p=positions[i];obj.transform.localPosition=new Vector3(p[0],p[1],p[2]);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
  }
 }
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);UnityEditor.Undo.CollapseUndoOperations(undo);
 return "본성 1 / 성문 1 / 망루 4: 전용 프리팹과 LOD 적용";
}catch{UnityEditor.Undo.RevertAllDownToGroup(undo);throw;}
