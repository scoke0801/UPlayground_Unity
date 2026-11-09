if(Application.dataPath!="C:/UsingProject/UnityProject/UPlayground/Assets"||!System.IO.File.ReadAllText("Tools/WorldThemes/AgentCoordination/editor.lock").Contains("QuarryPassR1-A-20261108"))throw new Exception("A 잠금 필요");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/QuarryPassR1.unity")throw new Exception("A 씬 필요");
var jt=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);var deserialize=jt.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>deserialize.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/WorldThemes/QuarryPassR1/cliff-plan.json"),typeof(Dictionary<string,object>));var placements=(Dictionary<string,object>[])read(plan["placements"].ToString(),typeof(Dictionary<string,object>[]));
string folder="Assets/10.Datas/World/ThemeStudies/QuarryPassR1";string matPath=folder+"/CutFace.mat";
if(!System.IO.File.Exists(matPath))AssetDatabase.CopyAsset("Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Materials/Stones/Cliffs/M_EastLands_Cliff_02.mat",matPath);
var mat=AssetDatabase.LoadAssetAtPath<Material>(matPath);mat.SetFloat("_USE_COVERAGE_LAYER",0);mat.DisableKeyword("_USE_COVERAGE_LAYER");mat.SetFloat("_Coverage_Multiply",0);mat.SetFloat("_Layer_1_Brightness",1.05f);mat.SetColor("_Color",new Color(.68f,.66f,.60f,1));EditorUtility.SetDirty(mat);AssetDatabase.SaveAssetIfDirty(mat);
var root=GameObject.Find("QuarryA_ConnectedStrata");if(root==null)root=new GameObject("QuarryA_ConnectedStrata");var rows=new List<object>();
foreach(var item in placements){
string name=item["name"].ToString();var center=(float[])read(item["center"].ToString(),typeof(float[]));var size=(float[])read(item["size"].ToString(),typeof(float[]));var go=root.transform.Find(name)==null?(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(plan["prefab"].ToString())):root.transform.Find(name).gameObject;
go.name=name;go.transform.SetParent(root.transform);go.transform.localScale=Vector3.one;go.transform.rotation=Quaternion.identity;go.transform.position=Vector3.zero;
var lod=go.GetComponent<LODGroup>();var renderer=lod.GetLODs()[0].renderers[0];var bounds=renderer.bounds;go.transform.localScale=new Vector3(size[0]/bounds.size.x,size[1]/bounds.size.y,size[2]/bounds.size.z);go.transform.rotation=Quaternion.Euler(0,Convert.ToSingle(item["yaw"]),0);
go.transform.position=new Vector3(center[0],center[1],center[2])-renderer.bounds.center;
foreach(var r in go.GetComponentsInChildren<Renderer>(true))r.sharedMaterials=Enumerable.Repeat(mat,r.sharedMaterials.Length).ToArray();
// 새 지물은 기존 지형과 별개로 충돌을 갖고, 카메라가 절개면을 통과하지 않도록 Ground로 분류한다.
int ground=LayerMask.NameToLayer("Ground");if(ground<0)throw new Exception("Ground 레이어 없음");foreach(var t in go.GetComponentsInChildren<Transform>(true)){t.gameObject.layer=ground;PrefabUtility.RecordPrefabInstancePropertyModifications(t.gameObject);}
if(go.GetComponentsInChildren<Collider>(true).Length==0){var mc=renderer.gameObject.AddComponent<MeshCollider>();mc.sharedMesh=renderer.GetComponent<MeshFilter>().sharedMesh;}
foreach(var c in go.GetComponentsInChildren<Collider>(true)){c.enabled=true;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
PrefabUtility.RecordPrefabInstancePropertyModifications(go);
PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);foreach(var r in go.GetComponentsInChildren<Renderer>(true))PrefabUtility.RecordPrefabInstancePropertyModifications(r);
rows.Add(new{name,sourcePrefab=plan["prefab"].ToString(),bounds=renderer.bounds.ToString("F4"),position=go.transform.position.ToString("F4"),scale=go.transform.localScale.ToString("F4"),colliders=go.GetComponentsInChildren<Collider>(true).Length,camera="Ground -> Solid",lods=lod.lodCount});
}
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);System.IO.File.WriteAllText("output/QuarryPassR1/cliff-instances.json",(string)jt.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{rows}));return rows;
