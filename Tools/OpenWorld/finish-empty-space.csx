var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode 필요");
var folder="output/OpenWorld/EmptySpace";var root=GameObject.Find("AuthoredEnvironment/빈 공간 생활권");if(root==null)throw new Exception("배치 누락");
var surface=GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();
surface.BuildNavMesh();if(surface.navMeshData==null)throw new Exception("NavMesh 생성 실패");
string navPath="Assets/10.Datas/World/OpenWorld/EmptySpaceNavigation.asset";var saved=AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>(navPath);
if(saved==null)AssetDatabase.CreateAsset(surface.navMeshData,navPath);
else{var baked=surface.navMeshData;surface.RemoveData();EditorUtility.CopySerialized(baked,saved);EditorUtility.SetDirty(saved);AssetDatabase.SaveAssetIfDirty(saved);surface.navMeshData=saved;surface.AddData();}
EditorUtility.SetDirty(surface);
var terrain=Terrain.activeTerrain;var report=new System.Text.StringBuilder();int missing=0,materials=0;
foreach(var t in root.GetComponentsInChildren<Transform>(true))missing+=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
foreach(var r in root.GetComponentsInChildren<Renderer>(true))foreach(var m in r.sharedMaterials)if(m==null||m.shader==null||m.shader.name.Contains("InternalError"))materials++;
report.AppendLine("Missing Script="+missing+" / 재질·셰이더 오류="+materials);
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/empty-space-plan.json"),typeof(Dictionary<string,object>));
var routes=(Dictionary<string,object>[])read(plan["routes"].ToString(),typeof(Dictionary<string,object>[]));int passed=0,failed=0;
foreach(var route in routes){var points=(float[][])read(route["points"].ToString(),typeof(float[][]));var hits=new List<Vector3>();bool sample=true;
 foreach(var p in points){var pos=new Vector3(p[0],0,p[1]);pos.y=terrain.SampleHeight(pos)+terrain.transform.position.y;UnityEngine.AI.NavMeshHit hit;if(!UnityEngine.AI.NavMesh.SamplePosition(pos,out hit,4,UnityEngine.AI.NavMesh.AllAreas)){sample=false;break;}hits.Add(hit.position);}
 bool complete=sample;float length=0;var navPathResult=new UnityEngine.AI.NavMeshPath();
 if(sample)for(int k=1;k<hits.Count;k++)foreach(bool reverse in new[]{false,true}){var a=hits[reverse?k:k-1];var b=hits[reverse?k-1:k];bool ok=UnityEngine.AI.NavMesh.CalculatePath(a,b,UnityEngine.AI.NavMesh.AllAreas,navPathResult)&&navPathResult.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;complete&=ok;if(ok)for(int j=1;j<navPathResult.corners.Length;j++)length+=Vector3.Distance(navPathResult.corners[j-1],navPathResult.corners[j]);}
 if(complete)passed++;else failed++;report.AppendLine(route["group"]+" / 접지점="+sample+" / 구간 양방향="+complete+" / 왕복 길이="+length.ToString("F1"));
}
var ids=string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));bool same=ids==System.IO.File.ReadAllText(folder+"/identities-before.txt");report.AppendLine("기존 진행 ID·좌표 보존="+same+" / 경로 통과="+passed+" 실패="+failed);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);System.IO.File.WriteAllText(folder+"/audit.txt",report.ToString());return report.ToString();
