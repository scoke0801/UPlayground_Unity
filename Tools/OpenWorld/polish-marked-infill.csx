var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
string folder="output/OpenWorld/MarkedInfill";
if(System.IO.File.Exists(folder+"/polish-phase.txt"))throw new Exception("이미 실행 기록이 있습니다.");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/marked-infill-plan.json"),typeof(Dictionary<string,object>));
var rows=(Dictionary<string,object>[])read(plan["placements"].ToString(),typeof(Dictionary<string,object>[]));
var root=GameObject.Find("AuthoredEnvironment/지도 표시 구역 생활 경관");var terrain=Terrain.activeTerrain;
var surface=GameObject.Find("AuthoredEnvironment").GetComponent<Unity.AI.Navigation.NavMeshSurface>();var saved=surface.navMeshData;
if(AssetDatabase.GetAssetPath(saved)!="Assets/10.Datas/World/OpenWorld/MarkedInfillNavigation.asset")throw new Exception("작업 NavMesh 연결 누락");
Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("서쪽 쉼터 길 중심 충돌 보정");
foreach(string id in new[]{"marked-0020","marked-0022","marked-0023"}) {
    var row=rows.Single(r=>r["id"].ToString()==id);var t=root.GetComponentsInChildren<Transform>().Single(item=>item.name.StartsWith(id+"_"));
    Undo.RecordObject(t,"길가 소품 이동");float x=Convert.ToSingle(row["x"]),z=Convert.ToSingle(row["z"]);
    var p=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z)),z);float offset=t.position.y-terrain.SampleHeight(t.position);p.y+=offset;t.position=p;
    PrefabUtility.RecordPrefabInstancePropertyModifications(t);
}
Physics.SyncTransforms();var updated=UnityEngine.Object.Instantiate(saved);
System.IO.File.WriteAllText(folder+"/polish-phase.txt","소품 세 개 이동 / NavMesh 증분 생성 중");
try {
    var operation=surface.UpdateNavMesh(updated);
    operation.completed+=_=> {
        try {
            surface.RemoveData();EditorUtility.CopySerialized(updated,saved);surface.navMeshData=saved;surface.AddData();
            EditorUtility.SetDirty(saved);AssetDatabase.SaveAssetIfDirty(saved);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
            if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
            var applied=new System.Text.StringBuilder("id,site,x,y,z,kind\n");
            foreach(var t in root.GetComponentsInChildren<Transform>().Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))) {
                var row=rows.Single(r=>t.name.StartsWith(r["id"].ToString()+"_"));
                applied.AppendLine(row["id"]+","+row["site"]+","+t.position.x+","+t.position.y+","+t.position.z+","+row["kind"]);
            }
            System.IO.File.WriteAllText(folder+"/placements.csv",applied.ToString());
            Undo.CollapseUndoOperations(group);System.IO.File.WriteAllText(folder+"/polish-phase.txt","완료: 소품 세 개와 NavMesh 증분 저장");
        }catch(Exception error){Undo.RevertAllDownToGroup(group);System.IO.File.WriteAllText(folder+"/polish-phase.txt","실패: "+error);}
        finally{UnityEngine.Object.DestroyImmediate(updated);}
    };
}catch{Undo.RevertAllDownToGroup(group);UnityEngine.Object.DestroyImmediate(updated);throw;}
return "증분 생성 예약: polish-phase.txt로 완료 확인";

