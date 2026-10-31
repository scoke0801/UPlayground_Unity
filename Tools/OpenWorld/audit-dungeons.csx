var folder="output/OpenWorld/Exploration/Iteration06";int count=int.Parse(System.IO.File.ReadAllText(folder+"/active-sites.txt"));var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var points=new[]{new[]{new Vector3(420,105,580),new Vector3(330,104,520),new Vector3(330,104,535),new Vector3(330,104,548),new Vector3(325,105,560),new Vector3(318,107,570),new Vector3(330,104,585)},new[]{new Vector3(550,145,840),new Vector3(590,140,840),new Vector3(590,140,850),new Vector3(585,140,860),new Vector3(591.2f,144.45f,859.9f),new Vector3(590,140,870),new Vector3(580,140,860)},new[]{new Vector3(1770,94,910),new Vector3(1850,110,912),new Vector3(1870,110,912),new Vector3(1900,110,883),new Vector3(1900,110.5f,898),new Vector3(1930,110,910),new Vector3(1900,110,928)}};
var rows=new List<string>{"UTC="+DateTime.UtcNow.ToString("O"),"자동: Physics floor/headroom and full NavMesh pairs; no movement input or camera acceptance"};int failures=0;
var terrain=UnityEngine.Object.FindFirstObjectByType<Terrain>();
var utility=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);var go=new GameObject("DungeonInteriorCapture"){hideFlags=HideFlags.HideAndDontSave};var camera=go.AddComponent<Camera>();camera.enabled=false;camera.scene=scene;camera.fieldOfView=65;camera.farClipPlane=1000;camera.nearClipPlane=.1f;System.IO.Directory.CreateDirectory(folder+"/interiors");
try{for(int site=0;site<count;site++){
 var sampled=new List<Vector3>();
 for(int i=0;i<points[site].Length;i++){
  var p=points[site][i];if(i==0)p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
  UnityEngine.AI.NavMeshHit hit;bool found=UnityEngine.AI.NavMesh.SamplePosition(p,out hit,2,-1);if(!found)failures++;sampled.Add(found?hit.position:p);
  var floors=Physics.RaycastAll(p+Vector3.up*1.8f,Vector3.down,3,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(h=>h.normal.y>.5f && h.collider.GetComponentInParent<UPlayGround.GameActor>()==null).OrderBy(h=>h.distance).ToArray();
  rows.Add(site+" point="+i+" planned="+p+" nav="+found+" sampled="+hit.position+" supports="+string.Join("|",floors.Select(h=>h.collider.name+"@"+h.point.y).Take(3).ToArray()));
  if(i>0){camera.transform.position=(floors.Length>0?floors[0].point:p)+Vector3.up*1.7f;camera.transform.rotation=Quaternion.LookRotation(i==points[site].Length-1?Vector3.back:Vector3.forward);var tex=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1280,800),false,1000f});System.IO.File.WriteAllBytes(folder+"/interiors/"+site+"-"+i+".png",tex.EncodeToPNG());UnityEngine.Object.DestroyImmediate(tex);}
 }
 for(int i=0;i<sampled.Count;i++)for(int j=i+1;j<sampled.Count;j++)foreach(int direction in new[]{0,1}){var nav=new UnityEngine.AI.NavMeshPath();var a=sampled[direction==0?i:j];var b=sampled[direction==0?j:i];bool found=UnityEngine.AI.NavMesh.CalculatePath(a,b,-1,nav)&&nav.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;rows.Add(site+" pair="+i+"-"+j+" dir="+direction+" "+nav.status+" "+found);if(!found)failures++;}
}}finally{UnityEngine.Object.DestroyImmediate(go);}
var objects=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Transform>(true)).ToArray();var ids=objects.SelectMany(t=>t.GetComponents<UPlayGround.Components.SceneEntityId>()).Select(t=>t.Guid).ToArray();int missing=objects.Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));rows.Add("Scene IDs="+ids.Length+" duplicates="+(ids.Length-ids.Distinct().Count())+" missing="+missing);rows.Add("RESULT failures="+failures);System.IO.File.WriteAllLines(folder+"/dungeon-audit-"+count+".txt",rows);return string.Join("\n",rows.ToArray());


