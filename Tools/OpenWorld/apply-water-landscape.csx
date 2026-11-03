// 현재 씬에만 증분 적용한다. 전체 월드 생성기와 이전 저작 도구는 실행하지 않는다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new Exception("저장된 LakeVillageWorld Edit Mode가 필요합니다.");
var world=GameObject.Find("AuthoredEnvironment");
if(world.transform.Find("남서 초지와 물길 숲 보강")!=null)throw new Exception("이미 적용된 경관입니다.");
string folder="output/OpenWorld/WaterLandscape";
System.IO.Directory.CreateDirectory(folder);
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/water-landscape-plan.json"),typeof(Dictionary<string,object>));
var rows=(Dictionary<string,object>[])read(plan["placements"].ToString(),typeof(Dictionary<string,object>[]));
var clusters=(object[][])read(plan["clusters"].ToString(),typeof(object[][]));
var terrain=world.GetComponentInChildren<Terrain>();var data=terrain.terrainData;var origin=terrain.transform.position;
var assets=rows.Select(r=>r["path"].ToString()).Distinct().ToDictionary(p=>p,p=>AssetDatabase.LoadAssetAtPath<GameObject>(p));
foreach(var pair in assets)
    if(pair.Value==null||pair.Value.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length!=0||pair.Value.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))
        throw new Exception("장식 프리팹 검사 실패: "+pair.Key);
Func<string> identities=()=>string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));
string before=identities();
var protectedPoints=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.transform.position).ToArray();
var existing=world.GetComponentsInChildren<Transform>(true).Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).Select(t=>t.position).ToList();
var solids=world.GetComponentsInChildren<Collider>(true).Where(c=>c.enabled&&!c.isTrigger&&!(c is TerrainCollider)).ToArray();
var originalMap=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
var map=(float[,,])originalMap.Clone();
System.IO.File.WriteAllText(folder+"/identities-before.txt",before);
var root=new GameObject("남서 초지와 물길 숲 보강").transform;root.SetParent(world.transform,false);
var groups=new Dictionary<string,Transform>();
foreach(string key in new[]{"west","river"}){var g=new GameObject(key=="west"?"남서쪽 숲과 초지":"물길 서쪽 혼합림").transform;g.SetParent(root,false);groups.Add(key,g);}
var placed=new System.Text.StringBuilder("id,site,x,y,z,kind\n");int count=0,skipped=0;
try {
    foreach(var row in rows) {
        string kind=row["kind"].ToString();float x=Convert.ToSingle(row["x"]),z=Convert.ToSingle(row["z"]);
        float u=(x-origin.x)/data.size.x,v=(z-origin.z)/data.size.z;
        int ix=Mathf.Clamp((int)(u*data.alphamapWidth),0,data.alphamapWidth-1),iz=Mathf.Clamp((int)(v*data.alphamapHeight),0,data.alphamapHeight-1);
        var p=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+origin.y,z);
        float clearance=kind=="tree"?9:kind=="rock"?5:2;
        bool painted=data.alphamapLayers>6&&(originalMap[iz,ix,5]>.42f||originalMap[iz,ix,6]>.12f||originalMap[iz,ix,1]>.65f);
        if(p.y<48||data.GetSteepness(u,v)>27||painted||protectedPoints.Any(q=>new Vector2(q.x-x,q.z-z).sqrMagnitude<225)||existing.Any(q=>new Vector2(q.x-x,q.z-z).sqrMagnitude<clearance*clearance)||solids.Any(c=>c.gameObject.activeInHierarchy&&c.bounds.SqrDistance(p+Vector3.up)<clearance*clearance)) {skipped++;continue;}
        var go=(GameObject)PrefabUtility.InstantiatePrefab(assets[row["path"].ToString()],groups[row["site"].ToString()]);
        go.name=row["id"]+"_"+go.name;var t=go.transform;
        t.SetPositionAndRotation(p,Quaternion.Euler(0,Convert.ToSingle(row["yaw"]),0));t.localScale=Vector3.one*Convert.ToSingle(row["scale"]);
        if(kind=="tree") {t.position+=Vector3.down*.12f;existing.Add(p);}
        else if(kind=="rock") {
            var rs=go.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
            float ground=p.y;foreach(float bx in new[]{b.min.x,b.max.x})foreach(float bz in new[]{b.min.z,b.max.z})ground=Mathf.Min(ground,terrain.SampleHeight(new Vector3(bx,0,bz))+origin.y);
            t.position+=Vector3.up*(ground-b.min.y-.18f);existing.Add(p);
        } else {
            t.rotation=Quaternion.FromToRotation(Vector3.up,data.GetInterpolatedNormal(u,v))*t.rotation;t.position+=Vector3.down*.03f;
            foreach(var c in go.GetComponentsInChildren<Collider>(true)){c.enabled=false;PrefabUtility.RecordPrefabInstancePropertyModifications(c);}
            foreach(var r in go.GetComponentsInChildren<Renderer>(true)){r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;PrefabUtility.RecordPrefabInstancePropertyModifications(r);}
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(t);count++;
        placed.AppendLine(row["id"]+","+row["site"]+","+x+","+t.position.y+","+z+","+kind);
    }
    // 기존 길·밭을 보존하고 군락 아래에만 흙색을 얇게 섞는다.
    int earth=Array.FindIndex(data.terrainLayers,l=>l.name.IndexOf("Earth",StringComparison.OrdinalIgnoreCase)>=0);
    if(earth<0)earth=5;
    for(int iz=0;iz<data.alphamapHeight;iz++)for(int ix=0;ix<data.alphamapWidth;ix++) {
        float x=origin.x+(ix+.5f)*data.size.x/data.alphamapWidth,z=origin.z+(iz+.5f)*data.size.z/data.alphamapHeight;
        float weight=0;
        foreach(var c in clusters){float dx=(x-Convert.ToSingle(c[1]))/Convert.ToSingle(c[3]),dz=(z-Convert.ToSingle(c[2]))/Convert.ToSingle(c[4]);weight=Mathf.Max(weight,Mathf.Clamp01(1-Mathf.Sqrt(dx*dx+dz*dz)));}
        if(weight==0||originalMap[iz,ix,5]>.3f||originalMap[iz,ix,6]>.12f||originalMap[iz,ix,1]>.5f)continue;
        weight*=.22f*Mathf.PerlinNoise(x*.035f,z*.035f);
        for(int l=0;l<data.alphamapLayers;l++)map[iz,ix,l]=Mathf.Lerp(map[iz,ix,l],l==earth?1:0,weight);
    }
    if(count<100)throw new Exception("배치가 지나치게 적습니다: "+count);
    if(before!=identities())throw new Exception("진행 ID·좌표 보존 실패");
    data.SetAlphamaps(0,0,map);terrain.Flush();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
    Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
    System.IO.File.WriteAllText(folder+"/placements.csv",placed.ToString());System.IO.File.WriteAllText(folder+"/identities-after.txt",identities());
    string result="환경 배치="+count+" / 보호 영역 제외="+skipped+" / 기존 진행 ID·좌표 보존="+(before==identities());
    System.IO.File.WriteAllText(folder+"/application.txt",result);return result;
}catch {
    UnityEngine.Object.DestroyImmediate(root.gameObject);data.SetAlphamaps(0,0,originalMap);terrain.Flush();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);throw;
}

