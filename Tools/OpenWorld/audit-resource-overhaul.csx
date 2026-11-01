var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(scene.isDirty||EditorApplication.isPlayingOrWillChangePlaymode)throw new Exception("저장된 Edit Mode 씬 필요");
var env=GameObject.Find("AuthoredEnvironment").transform;
var rows=new List<object>();var protectedRows=new List<string>();
Func<Transform,bool> hasGameplay=t=>t.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length>0||t.GetComponentsInChildren<UPlayGround.GameActor>(true).Length>0;
Action<Transform> visit=null;
visit=t=>{
 if(t.name=="EnvironmentKitRefresh"||t.GetComponent<Terrain>()!=null)return;
 if(hasGameplay(t)){protectedRows.Add(AnimationUtility.CalculateTransformPath(t,env));return;}
 var renderers=t.GetComponentsInChildren<MeshRenderer>(true);
 if(renderers.Length==0){foreach(Transform child in t)visit(child);return;}
 string prefab=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
 bool root=PrefabUtility.IsAnyPrefabInstanceRoot(t.gameObject);
 bool kit=root&&(prefab.Contains("/PoorVillage/")||prefab.Contains("/WorldCities/")||prefab.Contains("/Environment/Dungeon/"));
 if(kit&&t.gameObject.activeInHierarchy)return;
 bool compound=System.Text.RegularExpressions.Regex.IsMatch(t.name,@"^(CourtyardHouse|OuterThatchedHouse|JoinedHouse|Storehouse|RearLane|CoastalTileHouse|CoastalThatchedHouse|HillHouse|OuterCoastalHouse|MarketTileAwning|COMP_Column|COMP_Wall|COMP_PROP_|NetDryingRack|FishDryingFrame)");
 bool unit=!t.gameObject.activeSelf||t.GetComponent<MeshFilter>()!=null||t.GetComponent<LODGroup>()!=null||compound||root&&!prefab.Contains("/OpenWorld/Dungeons/");
 if(!unit){foreach(Transform child in t)visit(child);return;}
 var b=new Bounds();bool first=true;
 foreach(var filter in t.GetComponentsInChildren<MeshFilter>(true)){
  if(filter.sharedMesh==null)continue;
  var mb=filter.sharedMesh.bounds;var matrix=t.worldToLocalMatrix*filter.transform.localToWorldMatrix;
  for(int i=0;i<8;i++){var p=matrix.MultiplyPoint3x4(mb.center+Vector3.Scale(mb.extents,new Vector3((i&1)==0?-1:1,(i&2)==0?-1:1,(i&4)==0?-1:1)));if(first){b=new Bounds(p,Vector3.zero);first=false;}else b.Encapsulate(p);}
 }
 if(first)return;
 rows.Add(new {id=GlobalObjectId.GetGlobalObjectIdSlow(t.gameObject).ToString(),instanceId=t.gameObject.GetInstanceID(),path=AnimationUtility.CalculateTransformPath(t,env),name=t.name,prefab=prefab,active=t.gameObject.activeInHierarchy,
 mesh=t.GetComponent<MeshFilter>()!=null?AssetDatabase.GetAssetPath(t.GetComponent<MeshFilter>().sharedMesh):"",
 materials=renderers.SelectMany(r=>r.sharedMaterials).Where(m=>m!=null).Select(AssetDatabase.GetAssetPath).Distinct().ToArray(),
 position=new[]{t.position.x,t.position.y,t.position.z},rotation=new[]{t.eulerAngles.x,t.eulerAngles.y,t.eulerAngles.z},
 center=new[]{b.center.x,b.center.y,b.center.z},size=new[]{b.size.x,b.size.y,b.size.z},scale=new[]{t.lossyScale.x,t.lossyScale.y,t.lossyScale.z},
 colliders=t.GetComponentsInChildren<Collider>(true).Length,meshCount=t.GetComponentsInChildren<MeshFilter>(true).Length});
};
foreach(Transform t in env)visit(t);
var serializer=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var json=(string)serializer.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{new {scene=scene.path,units=rows,protectedObjects=protectedRows}});
System.IO.Directory.CreateDirectory("output/OpenWorld/ResourceOverhaul");
System.IO.File.WriteAllText("output/OpenWorld/ResourceOverhaul/inventory.json",json);
return "교체 후보 "+rows.Count+" / 진행 개체 보호 "+protectedRows.Count;
