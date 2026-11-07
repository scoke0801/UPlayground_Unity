// 항구 주민의 공간과 끝단 지지면을 조사한다. 추락 후 복귀 동작은 입력 검수가 필요하다.
var host=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string folder="output/OpenWorld/EnvironmentFollowup";
if(EditorApplication.isPlayingOrWillChangePlaymode||host.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode 필요");
var rows=new List<string>();int actors=0,newBlockers=0,unsupported=0;
var content=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/01.Scenes/Scenario/OpenWorld/Regions/SaltwindHarborRegion.unity",UnityEditor.SceneManagement.OpenSceneMode.Additive);
try{
Physics.SyncTransforms();foreach(var actor in content.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.GameActor>(true))){
actors++;var p=actor.transform.position;
var overlaps=Physics.OverlapCapsule(p+Vector3.up*.65f,p+Vector3.up*1.5f,.3f,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Where(c=>c.gameObject.scene==host&&c.GetComponent<Terrain>()==null).ToArray();
int added=overlaps.Count(c=>c.transform.IsChildOf(GameObject.Find("AuthoredEnvironment/환경 후속 개선").transform));newBlockers+=added;
bool support=Physics.RaycastAll(p+Vector3.up*.4f,Vector3.down,2,Physics.DefaultRaycastLayers,QueryTriggerInteraction.Ignore).Any(h=>h.collider.gameObject.scene==host&&h.normal.y>.5f);if(!support)unsupported++;
rows.Add(actor.name+"|position="+p.ToString("F2")+"|newBlockers="+added+"|allOverlaps="+string.Join(",",overlaps.Select(c=>c.name).ToArray())+"|support="+support);
}
}finally{UnityEditor.SceneManagement.EditorSceneManager.CloseScene(content,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(host);}
// 끝단 보행면과 그 바깥의 높이 차를 같은 높이에서 조사한다.
var probes=new[]{new Vector3(1884,50,-353),new Vector3(1884,50,-356),new Vector3(1905,50,-353),new Vector3(1905,50,-356),new Vector3(1932,50,-361),new Vector3(1932,50,-364),new Vector3(1985,50,-352),new Vector3(1990,50,-352),new Vector3(1970,50,-357)};
foreach(var p in probes){var hits=Physics.RaycastAll(p+Vector3.up*1.5f,Vector3.down,20,-1,QueryTriggerInteraction.Ignore).Where(h=>h.collider.gameObject.scene==host).OrderByDescending(h=>h.point.y).ToArray();
rows.Add("edge "+p+"|"+string.Join(";",hits.Select(h=>h.collider.name+" y="+h.point.y.ToString("F2")).ToArray()));}
rows.Add("RESULT regionActors="+actors+" newEnvironmentBlockers="+newBlockers+" unsupported="+unsupported+" inputReturn=UNTESTED");
System.IO.File.WriteAllLines(folder+"/harbor-contact.txt",rows);return string.Join("\n",rows.ToArray());
