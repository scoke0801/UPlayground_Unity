var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("StonegateCitadel");
if(!scene.IsValid()||Application.isPlaying)throw new System.Exception("돌등성 Edit Mode가 필요합니다.");
var environment=scene.GetRootGameObjects().First(r=>r.name=="CityEnvironment");
var residents=scene.GetRootGameObjects().First(r=>r.name=="CitadelResidents");
// 계산대는 주민 허리 높이로 낮추고 지붕은 2m 에이전트의 머리 공간을 확보한다.
string meshPath="Assets/10.Datas/World/WorldCities/StonegateCitadel/CitadelMarketStallHighAwning.asset";
var mesh=UnityEditor.AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/WorldCities/MarketStall.prefab").GetComponent<MeshFilter>().sharedMesh;
if(mesh==null){
    mesh=UnityEngine.Object.Instantiate(source);mesh.name="CitadelMarketStallHighAwning";
    UnityEditor.AssetDatabase.CreateAsset(mesh,meshPath);
}
var vertices=source.vertices;int lifted=0;
for(int i=0;i<vertices.Length;i++)if(vertices[i].y>2.5f){vertices[i].y+=.8f;lifted++;}else vertices[i].y*=.65f;
if(lifted==0||lifted==vertices.Length)throw new System.Exception("좌판 메시 높이 범위를 다시 확인하세요.");
mesh.vertices=vertices;mesh.RecalculateBounds();UnityEditor.EditorUtility.SetDirty(mesh);UnityEditor.AssetDatabase.SaveAssetIfDirty(mesh);
foreach(Transform stall in environment.transform)if(stall.name=="MarketStall"&&stall.position.z>120){
    var filter=stall.GetComponent<MeshFilter>();var collider=stall.GetComponent<MeshCollider>();
    UnityEditor.Undo.RecordObject(filter,"돌등성 좌판 머리 공간");UnityEditor.Undo.RecordObject(collider,"돌등성 좌판 충돌 공간");
    filter.sharedMesh=mesh;collider.sharedMesh=mesh;
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(filter);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(collider);
}
var carpenter=residents.transform.Find("NPC_Citadel_Carpenter");var cargo=residents.transform.Find("NPC_Citadel_CargoWorker");
UnityEditor.Undo.RecordObject(carpenter,"작업대 겹침 해소");UnityEditor.Undo.RecordObject(cargo,"수레 겹침 해소");
carpenter.position=new Vector3(216,10.07f,216);cargo.position=new Vector3(213,10.07f,221);
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(carpenter);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(cargo);
var finish=environment.transform.Find("CitadelLife/MarketFinish");
foreach(Transform bread in finish)if(bread.name.StartsWith("TGV_Bread")){
    var renderers=bread.GetComponentsInChildren<Renderer>();var bounds=renderers[0].bounds;
    foreach(var renderer in renderers)bounds.Encapsulate(renderer.bounds);
    UnityEditor.Undo.RecordObject(bread,"좌판 진열물 바닥 맞춤");
    var stall=environment.transform.Cast<Transform>().Where(t=>t.name=="MarketStall").OrderBy(t=>(t.position-bread.position).sqrMagnitude).First();
    var local=stall.InverseTransformPoint(bounds.center);
    var center=stall.TransformPoint(new Vector3(Mathf.Round(local.x/1.35f)*1.35f,0,1.2f));
    bread.position+=new Vector3(center.x-bounds.center.x,stall.position.y+2.25f*.72f*.65f+.01f-bounds.min.y,center.z-bounds.center.z);
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(bread);
}
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return "좌판 4개 지붕 높이 보정, 작업자 2명 겹침 해소";
