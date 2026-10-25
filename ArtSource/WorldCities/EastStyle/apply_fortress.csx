// 참고 이미지의 성벽·단구·대계단을 기존 생활층 바깥에 적용한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.name!="StonegateCitadel"||Application.isPlaying||UnityEditor.EditorApplication.isCompiling)throw new System.Exception("돌등성 Edit Mode 컴파일 완료 필요");
var art=GameObject.Find("StonegateEastArt");
if(art==null)throw new System.Exception("미술 루트 없음");
var jsonType=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var deserialize=jsonType.GetMethod("DeserializeObject",new[]{typeof(string),typeof(System.Type)});
System.Func<object,System.Type,object> Decode=(value,type)=>deserialize.Invoke(null,new object[]{value.ToString(),type});
var cfg=(System.Collections.Generic.Dictionary<string,object>)Decode(System.IO.File.ReadAllText("ArtSource/WorldCities/EastStyle/fortress_layout.json"),typeof(System.Collections.Generic.Dictionary<string,object>));
var wall=(System.Collections.Generic.Dictionary<string,float>)Decode(cfg["wall"],typeof(System.Collections.Generic.Dictionary<string,float>));
var origin=(float[])Decode(cfg["origin"],typeof(float[]));
var points=(float[][])Decode(cfg["perimeter"],typeof(float[][]));
var transitions=(float[])Decode(cfg["lodTransitions"],typeof(float[]));
var terraces=(System.Collections.Generic.Dictionary<string,object>[])Decode(cfg["terraces"],typeof(System.Collections.Generic.Dictionary<string,object>[]));
var stairs=(System.Collections.Generic.Dictionary<string,float>)Decode(cfg["stairs"],typeof(System.Collections.Generic.Dictionary<string,float>));
string assets="Assets/03.Prefabs/Environment/WorldCities/StonegateHighPoly/";
string models="Assets/08.Models/WorldCities/StonegateHighPoly/";
UnityEditor.AssetDatabase.Refresh(UnityEditor.ImportAssetOptions.ForceSynchronousImport);
var stone=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(assets+"Stone.mat");
if(stone==null)throw new System.Exception("기존 석재 재질 없음");
// 기존 팩의 반복 석재 텍스처를 사용해 큰 석축이 단색 블록처럼 보이지 않게 한다.
string textures="Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Textures/Props/StoneStairsWalls/";
var color=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"T_EastLands_StoneWall_C.tga");
var normal=UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(textures+"T_EastLands_StoneWall_N.tga");
if(color==null||normal==null)throw new System.Exception("기존 석재 텍스처 누락");
stone.SetTexture("_BaseMap",color);stone.SetColor("_BaseColor",new Color(.72f,.70f,.65f));
stone.SetTexture("_BumpMap",normal);stone.SetFloat("_BumpScale",.55f);stone.EnableKeyword("_NORMALMAP");
UnityEditor.EditorUtility.SetDirty(stone);UnityEditor.AssetDatabase.SaveAssetIfDirty(stone);
var holder=new GameObject("FortressTerraces");
var lods=new LOD[3];
var offset=new Vector3(origin[0],origin[1],origin[2]);
System.Action<string,Vector3,Vector3,float> Box=(name,center,size,yaw)=>{
 var part=new GameObject(name);part.transform.SetParent(holder.transform,false);part.layer=LayerMask.NameToLayer("Ground");
 part.transform.localPosition=center-offset;part.transform.localRotation=Quaternion.Euler(0,yaw,0);
 var collider=part.AddComponent<BoxCollider>();collider.size=size;
};
try{
 for(int lod=0;lod<3;lod++){
  string path=models+"Fortress_LOD"+lod+".fbx";
  var importer=UnityEditor.AssetImporter.GetAtPath(path) as UnityEditor.ModelImporter;
  if(importer==null)throw new System.Exception("모델 없음 "+path);
  importer.importCameras=false;importer.importLights=false;importer.importAnimation=false;importer.isReadable=false;importer.addCollider=false;
  importer.AddRemap(new UnityEditor.AssetImporter.SourceAssetIdentifier(typeof(Material),"Stone"),stone);importer.SaveAndReimport();
  var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
  var model=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source,scene);model.name="LOD"+lod;model.transform.SetParent(holder.transform,false);
  foreach(var t in model.GetComponentsInChildren<Transform>())t.gameObject.layer=LayerMask.NameToLayer("Ground");
  lods[lod]=new LOD(transitions[lod],model.GetComponentsInChildren<Renderer>());
 }
 for(int i=0;i<points.Length-1;i++){
  var a=new Vector3(points[i][0],0,points[i][1]);var b=new Vector3(points[i+1][0],0,points[i+1][1]);
  var center=(a+b)/2;float length=Vector3.Distance(a,b);float yaw=-Mathf.Atan2(b.z-a.z,b.x-a.x)*Mathf.Rad2Deg;
  center.y=(wall["bottom"]+wall["top"])/2;Box("CurtainWall",center,new Vector3(length+.4f,wall["top"]-wall["bottom"],wall["width"]),yaw);
  var across=new Vector3(-(b.z-a.z),0,b.x-a.x).normalized;
  foreach(int sign in new[]{-1,1})Box("Parapet",(a+b)/2+across*sign*(wall["width"]/2-.28f)+Vector3.up*(wall["top"]+.65f),new Vector3(length,1.3f,.7f),yaw);
 }
 foreach(var terrace in terraces){
  var p=(float[])Decode(terrace["center"],typeof(float[]));float bottom=System.Convert.ToSingle(terrace["bottom"]),top=System.Convert.ToSingle(terrace["top"]);
  Box("KeepTerrace",new Vector3(p[0],(bottom+top)/2,p[1]),new Vector3(System.Convert.ToSingle(terrace["width"]),top-bottom,System.Convert.ToSingle(terrace["depth"])),0);
 }
 foreach(var rail in (float[][])Decode(cfg["terraceRails"],typeof(float[][])))Box("TerraceBalustrade",new Vector3(rail[0],18.65f,rail[1]),new Vector3(rail[2],1.3f,.7f),-rail[3]);
 for(int i=0;i<(int)stairs["steps"];i++){
  float z=stairs["front"]+(i+.5f)*stairs["run"],top=stairs["bottom"]+(i+1)*stairs["rise"];
  Box("GrandStair",new Vector3(stairs["x"],top-.2f,z),new Vector3(stairs["width"],.4f,stairs["run"]+.03f),0);
  foreach(int sign in new[]{-1,1})Box("StairCoping",new Vector3(stairs["x"]+sign*(stairs["width"]/2+.32f),top+.3f,z),new Vector3(.64f,.6f,stairs["run"]+.04f),0);
 }
 var group=holder.AddComponent<LODGroup>();group.fadeMode=LODFadeMode.CrossFade;group.animateCrossFading=true;group.SetLODs(lods);group.RecalculateBounds();
 string prefabPath=assets+"FortressTerraces.prefab";
 var prefab=UnityEditor.PrefabUtility.SaveAsPrefabAsset(holder,prefabPath);
 var castle=art.transform.Find("HighPolyCastle");
 if(castle.Find("FortressTerraces")==null){
  var instance=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,scene);UnityEditor.Undo.RegisterCreatedObjectUndo(instance,"성벽과 단구 적용");
  instance.transform.SetParent(castle,false);instance.transform.localPosition=offset;
  UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
 }
 // 기존 낮은 성벽 인스턴스는 복구 가능하게 보존한다. 원본 프리팹은 변경하지 않는다.
 foreach(Transform old in art.transform){
  if(old.name!="PF_EastLands_StoneWall_01"||!old.gameObject.activeSelf)continue;
  UnityEditor.Undo.RecordObject(old.gameObject,"기존 낮은 성벽 보존");old.gameObject.SetActive(false);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(old.gameObject);
 }
 var dressing=art.transform.Find("FortressDressing");
 if(dressing==null){var root=new GameObject("FortressDressing");root.transform.SetParent(art.transform,false);dressing=root.transform;UnityEditor.Undo.RegisterCreatedObjectUndo(root,"본성 단구 소품");}
 foreach(var entry in (System.Collections.Generic.Dictionary<string,object>[])Decode(cfg["dressing"],typeof(System.Collections.Generic.Dictionary<string,object>[]))){
  string name=entry["name"].ToString();if(dressing.Find(name)!=null)continue;
  var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(entry["prefab"].ToString());if(source==null)throw new System.Exception("소품 프리팹 없음 "+name);
  var obj=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source,scene);obj.name=name;obj.transform.SetParent(dressing,false);
  obj.transform.localRotation=Quaternion.Euler(0,System.Convert.ToSingle(entry["yaw"]),0)*source.transform.localRotation;
  var renderers=obj.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
  obj.transform.localScale*=System.Convert.ToSingle(entry["height"])/bounds.size.y;
  bounds=renderers[0].bounds;foreach(var r in renderers)bounds.Encapsulate(r.bounds);
  var p=(float[])Decode(entry["position"],typeof(float[]));obj.transform.position+=new Vector3(p[0]-bounds.center.x,p[1]-bounds.min.y,p[2]-bounds.center.z);
  foreach(var c in obj.GetComponentsInChildren<Collider>()){c.enabled=false;UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
  UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(obj);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
 }
 UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
 return "성벽 15구간·본성 단구·32단 대계단·여장 적용";
}finally{UnityEngine.Object.DestroyImmediate(holder);}
