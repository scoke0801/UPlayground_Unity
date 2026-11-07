var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity");
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorUtility.scriptCompilationFailed)throw new Exception("정상 컴파일된 Edit Mode 필요");
string folder="output/OpenWorld/DungeonWallRevision";var report=new List<string>();var errors=new List<string>();
var root=GameObject.Find("AuthoredEnvironment/월드 구간/던전/옛 회랑/석조 회랑 개선");
int missing=root.GetComponentsInChildren<Transform>(true).Sum(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject));
if(missing!=0)errors.Add("Missing Script="+missing);
if(root.GetComponentsInChildren<MeshFilter>(true).Any(f=>f.sharedMesh==null)||root.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||m.shader.name.Contains("InternalError"))))errors.Add("메시·재질·셰이더 누락");
string ids=string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(v=>v));
if(ids!=System.IO.File.ReadAllText(folder+"/identities-before.txt"))errors.Add("진행 ID·좌표 변경");
report.Add("진행 개체="+ids.Split('\n').Length+" / Missing Script="+missing+" / 모듈="+root.transform.childCount);
var points=new[]{new Vector3(650,100,485),new Vector3(650,100,510),new Vector3(650,100,535),new Vector3(650,100,559),new Vector3(650,100,588),new Vector3(680,100,588),new Vector3(630,100,548),new Vector3(610,104,548)};
int[,] pairs={{0,1},{1,2},{2,3},{3,4},{4,5},{2,6},{6,7}};
for(int i=0;i<pairs.GetLength(0);i++)for(int direction=0;direction<2;direction++){
var a=points[pairs[i,direction]];var b=points[pairs[i,1-direction]];UnityEngine.AI.NavMeshHit from,to;var path=new UnityEngine.AI.NavMeshPath();
bool ok=UnityEngine.AI.NavMesh.SamplePosition(a,out from,2,-1)&&UnityEngine.AI.NavMesh.SamplePosition(b,out to,2,-1)&&UnityEngine.AI.NavMesh.CalculatePath(from.position,to.position,-1,path)&&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
report.Add("경로 "+i+" 방향="+direction+" "+(ok?"PASS":"FAIL"));if(!ok)errors.Add("경로 "+i+" 방향="+direction);
}
Physics.SyncTransforms();
foreach(float z in new[]{492f,520f,548f,550f,556f,560f,568f,574f}){
var overlaps=Physics.OverlapCapsule(new Vector3(650,100.65f,z),new Vector3(650,101.45f,z),.4f,-1,QueryTriggerInteraction.Ignore);
var blocked=overlaps.Where(c=>c.transform.IsChildOf(root.transform)).ToArray();
if(blocked.Length>0)errors.Add("중심선 충돌 z="+z+" "+string.Join(",",blocked.Select(c=>c.name)));
}
var surface=GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();report.Add("NavMesh="+AssetDatabase.GetAssetPath(surface.navMeshData));
report.AddRange(errors.Select(e=>"FAIL "+e));report.Add("RESULT errors="+errors.Count+" / Editor 정지 검사; 실제 입력 플레이는 사용자 확인 대기");
System.IO.File.WriteAllLines(folder+"/audit.txt",report);return string.Join("\n",report);
