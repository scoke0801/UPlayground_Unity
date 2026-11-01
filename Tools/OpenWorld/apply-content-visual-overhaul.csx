var folder="output/OpenWorld/ResourceOverhaul";
var serializer=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=serializer.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
var rows=(Dictionary<string,object>[])parse.Invoke(null,new object[]{System.IO.File.ReadAllText("Tools/OpenWorld/content-visual-overhaul-plan.json"),typeof(Dictionary<string,object>[])});
var allScenes=Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Select(UnityEngine.SceneManagement.SceneManager.GetSceneAt).Where(s=>s.path.Contains("/Scenario/OpenWorld/")).ToArray();
Func<string> identities=()=>string.Join("\n",allScenes.SelectMany(s=>s.GetRootGameObjects()).SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(id=>id.gameObject.scene.path+"|"+id.Guid).OrderBy(s=>s));
string before=identities();System.IO.File.WriteAllText(folder+"/content-identities-before.txt",before);
var prefabs=new Dictionary<string,GameObject>();
foreach(var row in rows){
 string path=row["target"].ToString();if(prefabs.ContainsKey(path))continue;
 var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(path);
 if(prefab==null||!prefab.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh!=null))throw new Exception("콘텐츠 모델 메시 누락 "+path);
 if(prefab.GetComponentsInChildren<MeshRenderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==null)))throw new Exception("콘텐츠 모델 재질 누락 "+path);
 prefabs.Add(path,prefab);
}
var unpack=new HashSet<GameObject>();
foreach(var row in rows){var filter=EditorUtility.InstanceIDToObject(Convert.ToInt32(row["instanceId"])) as MeshFilter;if(filter==null)throw new Exception("조사 후 개체 변경");var root=PrefabUtility.GetOutermostPrefabInstanceRoot(filter.gameObject);if(root!=null)unpack.Add(root);}
foreach(var root in unpack)PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
var cache=new Dictionary<string,Mesh>();var colliderMeshes=new Dictionary<Mesh,Mesh>();int index=0;
foreach(var row in rows){
 var filter=EditorUtility.InstanceIDToObject(Convert.ToInt32(row["instanceId"])) as MeshFilter;
 string path=row["target"].ToString();var prefab=prefabs[path];var sample=prefab.GetComponentsInChildren<MeshFilter>(true).First(f=>f.sharedMesh!=null);
 var old=filter.sharedMesh;string key=old.GetInstanceID()+"|"+path;Mesh mesh;
 if(!cache.TryGetValue(key,out mesh)){
  mesh=UnityEngine.Object.Instantiate(sample.sharedMesh);mesh.name="OverhaulContent_"+index;
  var vertices=mesh.vertices;var transform=prefab.transform.worldToLocalMatrix*sample.transform.localToWorldMatrix;
  for(int v=0;v<vertices.Length;v++)vertices[v]=transform.MultiplyPoint3x4(vertices[v]);mesh.vertices=vertices;mesh.RecalculateBounds();
  var b=mesh.bounds;var ob=old.bounds;var scale=new Vector3(ob.size.x/Mathf.Max(.001f,b.size.x),Mathf.Max(.02f,ob.size.y)/Mathf.Max(.001f,b.size.y),ob.size.z/Mathf.Max(.001f,b.size.z));
  for(int v=0;v<vertices.Length;v++)vertices[v]=ob.center+Vector3.Scale(vertices[v]-b.center,scale);
  mesh.vertices=vertices;mesh.RecalculateBounds();mesh.RecalculateNormals();mesh.RecalculateTangents();
  string output="Assets/10.Datas/World/OpenWorld/ResourceOverhaul/ContentVisual_"+index+".asset";
  if(AssetDatabase.LoadMainAssetAtPath(output)!=null)throw new Exception("기존 모델 보호 "+output);
  AssetDatabase.CreateAsset(mesh,output);cache.Add(key,mesh);index++;
 }
 filter.sharedMesh=mesh;EditorUtility.SetDirty(filter);
 var renderer=filter.GetComponent<MeshRenderer>();var modelRenderer=sample.GetComponent<MeshRenderer>();
 if(renderer!=null){renderer.sharedMaterials=modelRenderer.sharedMaterials;EditorUtility.SetDirty(renderer);}
 colliderMeshes[old]=mesh;
}
int colliders=0;
foreach(var collider in allScenes.SelectMany(s=>s.GetRootGameObjects()).SelectMany(g=>g.GetComponentsInChildren<MeshCollider>(true))){
 Mesh mesh;if(collider.sharedMesh==null||!colliderMeshes.TryGetValue(collider.sharedMesh,out mesh))continue;
 collider.sharedMesh=mesh;EditorUtility.SetDirty(collider);colliders++;
}
string after=identities();if(before!=after)throw new Exception("콘텐츠 ID 변경 감지");System.IO.File.WriteAllText(folder+"/content-identities-after.txt",after);
foreach(var scene in allScenes)UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
System.IO.File.WriteAllText(folder+"/content-visual-result.txt","시각 메시 "+rows.Length+" / 새 공유 메시 "+index+" / 충돌 메시 "+colliders+" / 진행 ID 동일");
return "시각 메시 "+rows.Length+" / 새 공유 메시 "+index+" / 충돌 메시 "+colliders+" / 진행 ID 동일";

