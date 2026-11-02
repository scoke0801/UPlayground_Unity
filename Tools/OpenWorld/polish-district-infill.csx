var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
string folder="output/OpenWorld/DistrictInfill",phase=folder+"/repair-phase.txt";
if(System.IO.File.Exists(phase))throw new Exception("보정 실행 기록이 있습니다. 완료·실패 상태부터 확인하세요.");
var world=GameObject.Find("AuthoredEnvironment");var root=world.transform.Find("확장 생활권과 지형 경관");
var terrain=world.GetComponentInChildren<Terrain>();var data=terrain.terrainData;
var surface=world.GetComponent<Unity.AI.Navigation.NavMeshSurface>();var navigation=surface.navMeshData;
if(root==null||AssetDatabase.GetAssetPath(navigation)!="Assets/10.Datas/World/OpenWorld/DistrictInfillNavigation.asset")throw new Exception("확장 생활권 저장본이 아닙니다.");
if(!System.IO.File.ReadAllText(folder+"/route-preflight.txt").Contains("실패=0"))throw new Exception("수정 동선 사전 검사가 필요합니다.");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/district-infill-plan.json"),typeof(Dictionary<string,object>));
var routeRows=(Dictionary<string,object>[])read(plan["routes"].ToString(),typeof(Dictionary<string,object>[]));
var routes=routeRows.Select(r=>new{site=r["site"].ToString(),width=Convert.ToSingle(r["width"]),points=(float[][])read(r["points"].ToString(),typeof(float[][]))}).ToArray();
var old=(Dictionary<string,object>)read(System.IO.File.ReadAllText(folder+"/plan-before-resume.json"),typeof(Dictionary<string,object>));
var oldRows=(Dictionary<string,object>[])read(old["routes"].ToString(),typeof(Dictionary<string,object>[]));
var oldRoutes=oldRows.Select(r=>new{width=Convert.ToSingle(r["width"]),points=(float[][])read(r["points"].ToString(),typeof(float[][]))}).ToArray();
var reservedRows=(Dictionary<string,object>[])read(old["reservedRoutes"].ToString(),typeof(Dictionary<string,object>[]));
var preservedRoutes=reservedRows.Take(reservedRows.Length-oldRoutes.Length).Select(r=>new{width=Convert.ToSingle(r["width"]),points=(float[][])read(r["points"].ToString(),typeof(float[][]))}).ToArray();
Func<Vector2,Vector2,Vector2,float> distance=(p,a,b)=>Vector2.Distance(p,a+(b-a)*Mathf.Clamp01(Vector2.Dot(p-a,b-a)/(b-a).sqrMagnitude));
Func<Vector2,float> road=p=>{float best=float.MaxValue;foreach(var r in routes)for(int i=1;i<r.points.Length;i++)best=Mathf.Min(best,distance(p,new Vector2(r.points[i-1][0],r.points[i-1][1]),new Vector2(r.points[i][0],r.points[i][1]))-r.width*.5f);return best;};
Func<string> identities=()=>string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));
string before=identities();
if(before!=System.IO.File.ReadAllText(folder+"/identities-before.txt"))throw new Exception("기존 진행 ID·좌표 불일치");
var filters=root.GetComponentsInChildren<MeshFilter>().Where(m=>m.name.StartsWith("Wheat_LOD")).ToArray();
var paths=new[]{scene.path,AssetDatabase.GetAssetPath(data),AssetDatabase.GetAssetPath(navigation)}.Concat(filters.Select(m=>AssetDatabase.GetAssetPath(m.sharedMesh))).Distinct().ToArray();
System.IO.Directory.CreateDirectory(folder+"/resume-backup");
foreach(string path in paths){string target=folder+"/resume-backup/"+System.IO.Path.GetFileName(path);System.IO.File.Copy(path,target);if(System.IO.File.Exists(path+".meta"))System.IO.File.Copy(path+".meta",target+".meta");}
System.IO.File.WriteAllText(phase,"지표·이동 영역·작물 보정 시작");
var report=new System.Text.StringBuilder();
try {
    var map=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);var original=(float[,,])map.Clone();
    var protectedPoints=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>new Vector2(c.transform.position.x,c.transform.position.z)).ToArray();
    float sx=data.size.x/data.alphamapWidth,sz=data.size.z/data.alphamapHeight;int painted=0,faded=0;
    for(int iz=0;iz<data.alphamapHeight;iz++)for(int ix=0;ix<data.alphamapWidth;ix++) {
        var p=new Vector2(terrain.transform.position.x+(ix+.5f)*sx,terrain.transform.position.z+(iz+.5f)*sz);float d=road(p);
        float oldDistance=float.MaxValue;Vector2 nearest=Vector2.zero,side=Vector2.zero;
        foreach(var r in oldRoutes)for(int i=1;i<r.points.Length;i++) {
            var a=new Vector2(r.points[i-1][0],r.points[i-1][1]);var b=new Vector2(r.points[i][0],r.points[i][1]);
            float value=distance(p,a,b)-r.width*.5f;
            if(value<oldDistance){oldDistance=value;nearest=a+(b-a)*Mathf.Clamp01(Vector2.Dot(p-a,b-a)/(b-a).sqrMagnitude);side=new Vector2(-(b-a).y,(b-a).x).normalized;}
        }
        if(d>2&&oldDistance>1.5f)continue;
        if(original[iz,ix,6]>.12f||protectedPoints.Any(v=>Vector2.Distance(p,v)<10))continue;
        if(d>2&&oldDistance<1.5f) {
            bool shared=false;
            foreach(var r in preservedRoutes)for(int i=1;i<r.points.Length;i++)if(distance(p,new Vector2(r.points[i-1][0],r.points[i-1][1]),new Vector2(r.points[i][0],r.points[i][1]))<r.width*.5f+2)shared=true;
            if(shared)continue;
            // 양쪽 지표를 섞어 새 우회로와 무관한 짧은 옛 발자국만 옅게 한다.
            for(int l=0;l<data.alphamapLayers;l++) {
                float sum=0;
                foreach(float sign in new[]{-1f,1f}){var v=nearest+side*sign*10;int x=Mathf.Clamp(Mathf.FloorToInt((v.x-terrain.transform.position.x)/sx),0,data.alphamapWidth-1),z=Mathf.Clamp(Mathf.FloorToInt((v.y-terrain.transform.position.z)/sz),0,data.alphamapHeight-1);sum+=original[z,x,l];}
                map[iz,ix,l]=Mathf.Lerp(original[iz,ix,l],sum*.5f,.8f);
            }
            faded++;
        } else if(d<1.5f) {
            float weight=Mathf.Clamp01((2-d)/2)*.8f;
            for(int l=0;l<data.alphamapLayers;l++)map[iz,ix,l]=Mathf.Lerp(original[iz,ix,l],l==5?1:0,weight);
            painted++;
        }
    }
    data.SetAlphamaps(0,0,map);terrain.Flush();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
    report.AppendLine("새 길 지표 표본="+painted+" / 옛 길 완화 표본="+faded+" / 높이 변경 없음");
    var preferences=root.Find("갈림길 우선 이동 영역");
    if(preferences==null)throw new Exception("기존 이동 우선 영역 누락");
    foreach(var child in preferences.Cast<Transform>().ToArray())UnityEngine.Object.DestroyImmediate(child.gameObject);
    foreach(var route in routes)for(int i=1;i<route.points.Length;i++) {
        var a=new Vector3(route.points[i-1][0],200,route.points[i-1][1]);var b=new Vector3(route.points[i][0],200,route.points[i][1]);
        var go=new GameObject(route.site+"_"+i);go.transform.SetParent(preferences,false);go.transform.SetPositionAndRotation((a+b)*.5f,Quaternion.LookRotation(b-a));
        var volume=go.AddComponent<Unity.AI.Navigation.NavMeshModifierVolume>();volume.area=UnityEngine.AI.NavMesh.GetAreaFromName("Walkable");volume.size=new Vector3(10,400,Vector3.Distance(a,b)+8);
    }
    var crop=AssetDatabase.LoadAssetAtPath<GameObject>(plan["cropPath"].ToString());var sources=crop.GetComponentsInChildren<MeshFilter>();int removed=0;
    foreach(var field in filters.Select(f=>f.transform.parent).Distinct()) {
        var near=field.Find("Wheat_LOD1").GetComponent<MeshFilter>().sharedMesh;int sourceCount=sources.First(m=>m.name.Contains("LOD1")).sharedMesh.vertexCount;
        if(near.vertexCount%sourceCount!=0)throw new Exception("작물 결합 구조 불일치");
        int count=near.vertexCount/sourceCount;var vertices=near.vertices;var remove=new bool[count];
        for(int k=0;k<count;k++) {
            var b=new Bounds(vertices[k*sourceCount],Vector3.zero);for(int j=1;j<sourceCount;j++)b.Encapsulate(vertices[k*sourceCount+j]);
            var p=field.TransformPoint(b.center);remove[k]=road(new Vector2(p.x,p.z))<3;if(remove[k])removed++;
        }
        foreach(int level in new[]{1,2}) {
            var mesh=field.Find("Wheat_LOD"+level).GetComponent<MeshFilter>().sharedMesh;int batch=sources.First(m=>m.name.Contains("LOD"+level)).sharedMesh.vertexCount;
            if(mesh.vertexCount!=batch*count)throw new Exception("작물 LOD 대응 불일치");
            var triangles=mesh.triangles;var kept=new List<int>(triangles.Length);
            for(int k=0;k<triangles.Length;k+=3){int group=triangles[k]/batch;if(triangles[k+1]/batch!=group||triangles[k+2]/batch!=group)throw new Exception("작물 결합 삼각형 불일치");if(!remove[group]){kept.Add(triangles[k]);kept.Add(triangles[k+1]);kept.Add(triangles[k+2]);}}
            mesh.SetTriangles(kept,0);EditorUtility.SetDirty(mesh);AssetDatabase.SaveAssetIfDirty(mesh);
        }
    }
    report.AppendLine("길 주변 작물 제거="+removed+"묶음 / 메시 GUID 유지");
    if(identities()!=before)throw new Exception("진행 ID·좌표 변경");
    Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
    System.IO.File.WriteAllText(folder+"/repair-application.txt",report.ToString());System.IO.File.WriteAllText(phase,"지표·작물·씬 저장 완료 / NavMesh 갱신 중");
    var operation=surface.UpdateNavMesh(navigation);
    operation.completed+=_=>{try{EditorUtility.SetDirty(navigation);AssetDatabase.SaveAssetIfDirty(navigation);System.IO.File.WriteAllText(phase,"완료: 지표·작물·씬·NavMesh 저장");}catch(Exception error){System.IO.File.WriteAllText(phase,"NavMesh 저장 실패: "+error);}};
    return report+"NavMesh 갱신 예약. repair-phase.txt로 완료 확인";
}catch(Exception error){System.IO.File.WriteAllText(phase,"실패: "+error+" / resume-backup 보존. 재실행 금지");throw;}
