// 최초 실행 전의 도시 씬은 output/WorldTravel/Before에 보관한다. 씬은 Unity에서만 저장한다.
if(Application.isPlaying || UnityEditor.EditorApplication.isCompiling) throw new System.Exception("Edit Mode 컴파일 완료 후 실행하세요.");
var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var report=new System.Collections.Generic.List<string>();
foreach(string city in new[]{"StonegateCitadel","SaltwindHarbor"}) {
    string scenePath="Assets/01.Scenes/Scenario/"+city+".unity";
    var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(scenePath);
    bool opened=!scene.IsValid() || !scene.isLoaded;
    if(!opened && scene.isDirty) throw new System.Exception("미저장 사용자 씬: "+city);
    if(opened) scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,UnityEditor.SceneManagement.OpenSceneMode.Additive);
    UnityEditor.Undo.IncrementCurrentGroup();int undo=UnityEditor.Undo.GetCurrentGroup();
    var nav=UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AI.NavMeshData>("Assets/10.Datas/World/WorldCities/"+city+"/Navigation.asset");
    var offset=new Vector3(20000,0,20000);
    var instance=UnityEngine.AI.NavMesh.AddNavMeshData(nav,offset,Quaternion.identity);
    try {
        UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
        var roots=scene.GetRootGameObjects();
        var terrain=roots.SelectMany(r=>r.GetComponentsInChildren<Terrain>()).First();
        System.Func<float,float,Vector3> ground=(x,z)=>{
            var p=new Vector3(x,0,z);p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
            UnityEngine.AI.NavMeshHit hit;
            if(!UnityEngine.AI.NavMesh.SamplePosition(p+offset,out hit,.75f,UnityEngine.AI.NavMesh.AllAreas))throw new System.Exception("도착 바닥 없음 "+p);
            return hit.position-offset;
        };
        var arrivals=roots.SelectMany(r=>r.GetComponentsInChildren<UPlayGround.SceneArrivalPoint>(true)).ToList();
        string key=city=="StonegateCitadel"?"worldcities.StonegateCitadel.plaza":"worldcities.SaltwindHarbor.market";
        string arrivalId="worldcities."+city+".fast_travel";
        string label=city=="StonegateCitadel"?"돌등성 분수 광장":"소금바람항 시장 어귀";
        Vector3 position=city=="StonegateCitadel"?ground(180,204):ground(180,144);
        var point=arrivals.SingleOrDefault(a=>a.Id==arrivalId);
        if(point==null) {
            var host=new GameObject("CityFastTravelArrival");
            UnityEditor.Undo.RegisterCreatedObjectUndo(host,"도시 지도 거점");
            point=host.AddComponent<UPlayGround.SceneArrivalPoint>();arrivals.Add(point);
            var settings=new UnityEditor.SerializedObject(point);settings.FindProperty("_id").stringValue=arrivalId;settings.ApplyModifiedProperties();
        }
        UnityEditor.Undo.RecordObject(point.transform,"도시 지도 거점 위치");
        point.transform.position=position+Vector3.up*.15f;
        point.transform.rotation=Quaternion.Euler(0,city=="StonegateCitadel"?180:0,0);
        foreach(var arrival in arrivals) {
            UnityEditor.Undo.RecordObject(arrival,"도시 도착 발견 키");
            var settings=new UnityEditor.SerializedObject(arrival);settings.FindProperty("_activationId").stringValue=key;settings.ApplyModifiedProperties();
        }
        if(city=="SaltwindHarbor") {
            var road=roots.Single(r=>r.name=="CityTravel_Castle");
            var sign=road.transform.Find("TravelSign");
            var arrival=arrivals.Single(a=>a.Id=="worldcities.SaltwindHarbor.from.StonegateCitadel");
            UnityEditor.Undo.RecordObjects(new UnityEngine.Object[]{sign,arrival.transform},"갈대 물목 진입 위치");
            sign.position=ground(184,64);
            arrival.transform.position=ground(186,70)+Vector3.up*.15f;
            arrival.transform.rotation=Quaternion.identity;
            float separation=Vector2.Distance(new Vector2(sign.position.x,sign.position.z),new Vector2(arrival.Position.x,arrival.Position.z));
            if(separation<6||separation>7)throw new System.Exception("출구 간격 오류");
            report.Add("물목 출구="+sign.position.ToString("F3")+" 도착="+arrival.Position.ToString("F3")+" 거리="+separation);
        }
        foreach(var arrival in arrivals) {
            UnityEngine.AI.NavMeshHit from;var path=new UnityEngine.AI.NavMeshPath();
            bool valid=UnityEngine.AI.NavMesh.SamplePosition(arrival.Position+offset,out from,1,UnityEngine.AI.NavMesh.AllAreas)
                &&UnityEngine.AI.NavMesh.CalculatePath(from.position,position+offset,UnityEngine.AI.NavMesh.AllAreas,path)
                &&path.status==UnityEngine.AI.NavMeshPathStatus.PathComplete;
            if(!valid)throw new System.Exception("거점 경로 오류: "+arrival.Id);
            report.Add(arrival.Id+" -> "+key+" @ "+arrival.Position.ToString("F3"));
        }
        string regionPath="Assets/10.Datas/World/WorldCities/"+city+"/RegionInfo.asset";
        var region=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.UI.MapRegionInfoSO>(regionPath);
        UnityEditor.Undo.RecordObject(region,"도시 지도 도착 항목");
        var entry=new UPlayGround.Data.UI.MapRegionInfoSO.PortalEntry {label=label,worldPosition=point.Position,targetSceneName=city,arrivalId=arrivalId,activationId=key,requiresActivation=true,startsActivated=false};
        int index=region.portals.FindIndex(p=>p.activationId==key);
        if(index<0)region.portals.Add(entry);else region.portals[index]=entry;
        UnityEditor.EditorUtility.SetDirty(region);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("씬 저장 실패: "+city);
        UnityEditor.AssetDatabase.SaveAssetIfDirty(region);
        UnityEditor.Undo.CollapseUndoOperations(undo);
        report.Add(city+" 지도 거점="+label+" "+point.Position.ToString("F3"));
    } catch {UnityEditor.Undo.RevertAllDownToGroup(undo);throw;}
    finally {
        instance.Remove();
        if(opened)UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);
        if(original.IsValid()&&original.isLoaded)UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);
    }
}
System.IO.Directory.CreateDirectory("output/WorldTravel");System.IO.File.WriteAllLines("output/WorldTravel/city-changes.txt",report);
return string.Join("\n",report.ToArray());
