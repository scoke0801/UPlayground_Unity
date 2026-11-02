var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.isDirty || scene.path != "Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")
    throw new Exception("저장된 LakeVillageWorld Edit Mode가 필요합니다.");
var world = GameObject.Find("AuthoredEnvironment");
if (world.transform.Find("지도 표시 구역 생활 경관") != null) throw new Exception("이미 적용된 저작입니다. 재실행하지 마세요.");
string folder = "output/OpenWorld/MarkedInfill", assetFolder = "Assets/10.Datas/World/OpenWorld/MarkedInfill";
if(AssetDatabase.IsValidFolder(assetFolder))throw new Exception("작물 파생 폴더가 이미 있습니다. 기존 작업을 덮어쓰지 않습니다.");
System.IO.Directory.CreateDirectory(folder + "/backup");
var terrain = world.GetComponentInChildren<Terrain>();
var data = terrain.terrainData; var origin = terrain.transform.position;
var json = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert")).First(t => t != null);
var parse = json.GetMethod("DeserializeObject", new[] { typeof(string), typeof(Type) });
Func<string, Type, object> read = (s,t) => parse.Invoke(null,new object[]{s,t});
var plan = (Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/marked-infill-plan.json"),typeof(Dictionary<string,object>));
var rows = (Dictionary<string,object>[])read(plan["placements"].ToString(),typeof(Dictionary<string,object>[]));
var sites = (Dictionary<string,object>[])read(plan["sites"].ToString(),typeof(Dictionary<string,object>[]));
var routes = (Dictionary<string,object>[])read(plan["routes"].ToString(),typeof(Dictionary<string,object>[]));
var patches = (Dictionary<string,object>[])read(plan["patches"].ToString(),typeof(Dictionary<string,object>[]));
var assets = rows.Select(r=>r["path"].ToString()).Distinct().ToDictionary(p=>p,p=>AssetDatabase.LoadAssetAtPath<GameObject>(p));
foreach(var kv in assets) {
    var p=kv.Value;
    if(p==null || p.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length>0 || p.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0) || p.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||m.shader.name.Contains("InternalError"))))
        throw new Exception("장식 에셋 검사 실패: "+kv.Key);
}
Func<string> identities = () => string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));
string before=identities();
var protectedPoints=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.transform.position).ToArray();
var existing=world.GetComponentsInChildren<Transform>(true).Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject)).Select(t=>t.position).ToArray();
foreach(string p in new[]{scene.path,AssetDatabase.GetAssetPath(data)}) {
    string dest=folder+"/backup/"+System.IO.Path.GetFileName(p);
    if(System.IO.File.Exists(dest))throw new Exception("기존 백업 존재: 중복 실행 여부를 확인하세요.");
    System.IO.File.Copy(p,dest); if(System.IO.File.Exists(p+".meta"))System.IO.File.Copy(p+".meta",dest+".meta");
}
System.IO.File.WriteAllText(folder+"/identities-before.txt",before);
var originalHeights=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);
var heights=(float[,])originalHeights.Clone();
var originalMap=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);
var map=(float[,,])originalMap.Clone();
float hx=data.size.x/(data.heightmapResolution-1),hz=data.size.z/(data.heightmapResolution-1);
var createdAssets=new List<string>();bool hasCreatedAssetFolder=false;
var root=new GameObject("지도 표시 구역 생활 경관").transform;root.SetParent(world.transform,false);
var groups=new Dictionary<string,Transform>();
foreach(var site in sites){var g=new GameObject(site["name"].ToString()).transform;g.SetParent(root,false);groups.Add(site["id"].ToString(),g);}
var applied=new System.Text.StringBuilder("id,site,x,y,z,kind\n");
var skipped=new System.Text.StringBuilder("id,reason\n");int placed=0,crops=0,foundations=0;
try {
    foreach(var row in rows.Where(r=>r["kind"].ToString()!="crop")) {
        string kind=row["kind"].ToString(),id=row["id"].ToString();float x=Convert.ToSingle(row["x"]),z=Convert.ToSingle(row["z"]);
        float slope=data.GetSteepness((x-origin.x)/data.size.x,(z-origin.z)/data.size.z);
        if(protectedPoints.Any(point=>new Vector2(point.x-x,point.z-z).sqrMagnitude<144) || ((kind=="tree"||kind=="rock"||kind=="bush")&&existing.Any(point=>new Vector2(point.x-x,point.z-z).sqrMagnitude<64)) || slope>(kind=="building"?9:kind=="prop"?15:42)) {skipped.AppendLine(id+",보호 영역·기존 배치 근접·급사면");continue;}
        var go=(GameObject)PrefabUtility.InstantiatePrefab(assets[row["path"].ToString()],groups[row["site"].ToString()]);
        go.name=id+"_"+go.name;var t=go.transform;
        var p=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+origin.y,z);
        t.SetPositionAndRotation(p,Quaternion.Euler(0,Convert.ToSingle(row["yaw"]),0));t.localScale=Vector3.one*Convert.ToSingle(row["scale"]);
        var rs=go.GetComponentsInChildren<Renderer>(true);var bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
        if(kind=="building") {
            int x0=Mathf.Max(0,Mathf.FloorToInt((bounds.min.x-origin.x-3)/hx)),x1=Mathf.Min(data.heightmapResolution-1,Mathf.CeilToInt((bounds.max.x-origin.x+3)/hx));
            int z0=Mathf.Max(0,Mathf.FloorToInt((bounds.min.z-origin.z-3)/hz)),z1=Mathf.Min(data.heightmapResolution-1,Mathf.CeilToInt((bounds.max.z-origin.z+3)/hz));
            for(int iz=z0;iz<=z1;iz++)for(int ix=x0;ix<=x1;ix++) {
                float wx=origin.x+ix*hx,wz=origin.z+iz*hz,dx=Mathf.Max(0,bounds.min.x-wx,wx-bounds.max.x),dz=Mathf.Max(0,bounds.min.z-wz,wz-bounds.max.z);
                float weight=1-Mathf.SmoothStep(0,1,Mathf.Sqrt(dx*dx+dz*dz)/3),target=(p.y-origin.y)/data.size.y;
                float change=Mathf.Abs(target-originalHeights[iz,ix])*data.size.y*weight;
                if(change>.9f)throw new Exception("기초 정지 0.9m 한도 초과: "+id+" / "+change);
                heights[iz,ix]=Mathf.Lerp(heights[iz,ix],target,weight);
            }
            t.position+=Vector3.up*(p.y-bounds.min.y-.08f);foundations++;
        } else if(kind=="tree") t.position+=Vector3.down*.12f;
        else if(kind=="ground") {
            t.rotation=Quaternion.FromToRotation(Vector3.up,data.GetInterpolatedNormal((x-origin.x)/data.size.x,(z-origin.z)/data.size.z))*t.rotation;
            var sourceRenderers=assets[row["path"].ToString()].GetComponentsInChildren<Renderer>(true);
            var sourceBounds=sourceRenderers[0].bounds;foreach(var r in sourceRenderers)sourceBounds.Encapsulate(r.bounds);
            // 기울인 월드 AABB의 하단을 중심 지면에 맞추면 바닥 소품이 사면 위로 뜬다.
            t.position+=Vector3.up*(-sourceBounds.min.y*t.localScale.y-.03f);
        }
        else {
            bounds=rs[0].bounds;foreach(var r in rs)bounds.Encapsulate(r.bounds);
            float low=p.y;
            if(kind=="rock")foreach(float px in new[]{bounds.min.x,bounds.max.x})foreach(float pz in new[]{bounds.min.z,bounds.max.z})low=Mathf.Min(low,terrain.SampleHeight(new Vector3(px,0,pz))+origin.y);
            t.position+=Vector3.up*(low-bounds.min.y-(kind=="rock"?.2f:.04f));
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(t);placed++;
        applied.AppendLine(id+","+row["site"]+","+x+","+t.position.y+","+z+","+kind);
    }
    data.SetHeights(0,0,heights);
    AssetDatabase.CreateFolder("Assets/10.Datas/World/OpenWorld","MarkedInfill");hasCreatedAssetFolder=true;
    int fieldIndex=0;
    foreach(var patch in patches.Where(p=>Convert.ToInt32(p["layer"])==1)) {
        float cx=Convert.ToSingle(patch["x"]),cz=Convert.ToSingle(patch["z"]);
        var fieldRows=rows.Where(r=>r["kind"].ToString()=="crop"&&Mathf.Abs(Convert.ToSingle(r["x"])-cx)<14&&Mathf.Abs(Convert.ToSingle(r["z"])-cz)<16).ToArray();
        var field=new GameObject("밭_"+fieldIndex).transform;field.SetParent(groups["farm"],false);field.position=new Vector3(cx,0,cz);
        var renderers=new List<Renderer>();
        foreach(int level in new[]{1,2}) {
            var combines=new List<CombineInstance>();MeshRenderer sourceRenderer=null;
            foreach(var row in fieldRows) {
                float x=Convert.ToSingle(row["x"]),z=Convert.ToSingle(row["z"]);
                if(protectedPoints.Any(point=>new Vector2(point.x-x,point.z-z).sqrMagnitude<144))continue;
                var mf=assets[row["path"].ToString()].GetComponentsInChildren<MeshFilter>(true).First(m=>m.name.Contains("LOD"+level));
                sourceRenderer=mf.GetComponent<MeshRenderer>();
                var p=new Vector3(x-cx,terrain.SampleHeight(new Vector3(x,0,z))+origin.y-.03f,z-cz);
                var combine=new CombineInstance();combine.mesh=mf.sharedMesh;combine.transform=Matrix4x4.TRS(p,Quaternion.Euler(0,Convert.ToSingle(row["yaw"]),0),Vector3.one*Convert.ToSingle(row["scale"]))*mf.transform.localToWorldMatrix;combines.Add(combine);
                if(level==1)crops++;
            }
            if(combines.Count==0)continue;
            var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.name="MarkedField_"+fieldIndex+"_LOD"+level;mesh.CombineMeshes(combines.ToArray(),true,true);mesh.RecalculateBounds();
            string path=assetFolder+"/"+mesh.name+".asset";AssetDatabase.CreateAsset(mesh,path);createdAssets.Add(path);
            var child=new GameObject("Wheat_LOD"+level);child.transform.SetParent(field,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;
            var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterials=sourceRenderer.sharedMaterials;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderers.Add(renderer);
        }
        if(renderers.Count==2){var lod=field.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.12f,new[]{renderers[0]}),new LOD(.008f,new[]{renderers[1]})});lod.RecalculateBounds();}fieldIndex++;
    }
    float sx=data.size.x/data.alphamapWidth,sz=data.size.z/data.alphamapHeight;
    var lines=routes.Select(r=>new{points=(float[][])read(r["points"].ToString(),typeof(float[][])),width=Convert.ToSingle(r["width"])}).ToArray();
    var siteBounds=sites.Select(s=>new{center=(float[])read(s["center"].ToString(),typeof(float[])),radius=Convert.ToSingle(s["radius"])+70}).ToArray();
    // 기존 길은 보존하고 새 갈림길과 생활 영역의 지표만 섞는다.
    for(int iz=0;iz<data.alphamapHeight;iz++)for(int ix=0;ix<data.alphamapWidth;ix++) {
        var p=new Vector2(origin.x+(ix+.5f)*sx,origin.z+(iz+.5f)*sz);
        if(!siteBounds.Any(s=>Vector2.Distance(p,new Vector2(s.center[0],s.center[1]))<s.radius))continue;
        float weight=0;int layer=5;
        foreach(var line in lines)for(int k=1;k<line.points.Length;k++) {
            var a=new Vector2(line.points[k-1][0],line.points[k-1][1]);var d=new Vector2(line.points[k][0],line.points[k][1])-a;
            float f=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude),dist=Vector2.Distance(p,a+d*f);
            float w=Mathf.Clamp01((line.width*.5f+1.5f-dist)/1.5f)*.72f;
            if(w>weight){weight=w;layer=5;}
        }
        foreach(var patch in patches){float dx=(p.x-Convert.ToSingle(patch["x"]))/Convert.ToSingle(patch["rx"]),dz=(p.y-Convert.ToSingle(patch["z"]))/Convert.ToSingle(patch["rz"]);float w=Mathf.Clamp01((1-Mathf.Sqrt(dx*dx+dz*dz))*4)*Convert.ToSingle(patch["strength"]);if(w>weight){weight=w;layer=Convert.ToInt32(patch["layer"]);}}
        if(weight<=0)continue;
        for(int l=0;l<data.alphamapLayers;l++)map[iz,ix,l]=Mathf.Lerp(map[iz,ix,l],l==layer?1:0,weight);
    }
    data.SetAlphamaps(0,0,map);terrain.Flush();
    if(before!=identities())throw new Exception("진행 ID·좌표 보존 실패");
    EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
    Physics.SyncTransforms();UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
    System.IO.File.WriteAllText(folder+"/placements.csv",applied.ToString());System.IO.File.WriteAllText(folder+"/skipped.csv",skipped.ToString());System.IO.File.WriteAllText(folder+"/identities-after.txt",identities());
    string result="구역="+groups.Count+" / 프리팹="+placed+" / 작물="+crops+" / 작물 메시="+createdAssets.Count+" / 국소 기초="+foundations+" / 진행 ID·좌표 보존="+(before==identities());
    System.IO.File.WriteAllText(folder+"/application.txt",result);return result;
} catch {
    UnityEngine.Object.DestroyImmediate(root.gameObject);data.SetHeights(0,0,originalHeights);data.SetAlphamaps(0,0,originalMap);terrain.Flush();
    foreach(string path in createdAssets)AssetDatabase.DeleteAsset(path);
    if(hasCreatedAssetFolder)AssetDatabase.DeleteAsset(assetFolder);
    EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);throw;
}

