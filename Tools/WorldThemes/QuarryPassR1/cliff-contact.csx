if(Application.dataPath!="C:/UsingProject/UnityProject/UPlayground/Assets"||!System.IO.File.ReadAllText("Tools/WorldThemes/AgentCoordination/editor.lock").Contains("QuarryPassR1-A-20261108"))throw new Exception("A 잠금 필요");
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();if(scene.path!="Assets/01.Scenes/ArtReview/WorldThemes/QuarryPassR1.unity")throw new Exception("A 씬 필요");
var terrain=Terrain.activeTerrain;var root=GameObject.Find("QuarryA_ConnectedStrata");var rows=new List<object>();
foreach(Transform t in root.transform){var lod=t.GetComponent<LODGroup>();var renderer=lod.GetLODs()[0].renderers[0];var filter=renderer.GetComponent<MeshFilter>();var mesh=filter.sharedMesh;float cutoff=mesh.bounds.min.y+mesh.bounds.size.y*.18f;
var lower=mesh.vertices.Where(v=>v.y<=cutoff).Select(v=>filter.transform.TransformPoint(v)).ToArray();float above=lower.Max(p=>p.y-terrain.SampleHeight(p)-terrain.transform.position.y);
// 실제 하단 정점이 가장 낮은 사면에서도 잠기도록 매몰한다. bounds 중심 접지로 대체하지 않는다.
float sink=Mathf.Max(0,above+.6f);t.position-=Vector3.up*sink;PrefabUtility.RecordPrefabInstancePropertyModifications(t);
var after=lower.Select(p=>p-Vector3.up*sink).ToArray();float maxGap=after.Max(p=>p.y-terrain.SampleHeight(p)-terrain.transform.position.y);
rows.Add(new{name=t.name,lowerVertices=lower.Length,maxGapBefore=above,sink,maxGapAfter=maxGap,finalPosition=new[]{t.position.x,t.position.y,t.position.z},test="LOD0 하단18% 실제 정점 대 Terrain 높이; 측면·상면은 화면 별도 검사"});}
Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);var jt=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);System.IO.File.WriteAllText("output/QuarryPassR1/cliff-contact.json",(string)jt.GetMethod("SerializeObject",new[]{typeof(object)}).Invoke(null,new object[]{rows}));return rows;
