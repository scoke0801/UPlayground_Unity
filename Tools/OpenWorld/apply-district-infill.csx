var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
var world=GameObject.Find("AuthoredEnvironment");
if(world.transform.Find("확장 생활권과 지형 경관")!=null)throw new Exception("이미 적용된 저작입니다.");
string folder="output/OpenWorld/DistrictInfill",assetFolder="Assets/10.Datas/World/OpenWorld/DistrictInfill";
if(AssetDatabase.IsValidFolder(assetFolder))throw new Exception("파생 에셋 폴더가 이미 있습니다.");
var terrain=world.GetComponentInChildren<Terrain>();var data=terrain.terrainData;var origin=terrain.transform.position;
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/district-infill-plan.json"),typeof(Dictionary<string,object>));
var rows=(Dictionary<string,object>[])read(plan["placements"].ToString(),typeof(Dictionary<string,object>[]));
var regions=(Dictionary<string,object>[])read(plan["regions"].ToString(),typeof(Dictionary<string,object>[]));
var fields=(Dictionary<string,object>[])read(plan["fields"].ToString(),typeof(Dictionary<string,object>[]));
var patches=(Dictionary<string,object>[])read(plan["patches"].ToString(),typeof(Dictionary<string,object>[]));
var routeRows=(Dictionary<string,object>[])read(plan["reservedRoutes"].ToString(),typeof(Dictionary<string,object>[]));
var lines=routeRows.Select(r=>new{points=(float[][])read(r["points"].ToString(),typeof(float[][])),width=r.ContainsKey("width")?Convert.ToSingle(r["width"]):5f}).ToArray();
Func<Vector2,float> roadDistance=p=>{float best=float.MaxValue;foreach(var line in lines)for(int k=1;k<line.points.Length;k++){var a=new Vector2(line.points[k-1][0],line.points[k-1][1]);var d=new Vector2(line.points[k][0],line.points[k][1])-a;float f=Mathf.Clamp01(Vector2.Dot(p-a,d)/d.sqrMagnitude);best=Mathf.Min(best,Vector2.Distance(p,a+d*f)-line.width*.5f);}return best;};
var assets=rows.Select(r=>r["path"].ToString()).Concat(new[]{plan["cropPath"].ToString()}).Distinct().ToDictionary(p=>p,p=>AssetDatabase.LoadAssetAtPath<GameObject>(p));
foreach(var kv in assets)if(kv.Value==null||kv.Value.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length>0||kv.Value.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0)||kv.Value.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==null||m.shader==null||m.shader.name.Contains("InternalError"))))throw new Exception("에셋 검사 실패: "+kv.Key);
Func<string> identities=()=>string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s));
var protectedPoints=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>new Vector2(c.transform.position.x,c.transform.position.z)).ToArray();
string before=identities();
System.IO.Directory.CreateDirectory(folder+"/backup");
foreach(string path in new[]{scene.path,AssetDatabase.GetAssetPath(data)}){string target=folder+"/backup/"+System.IO.Path.GetFileName(path);if(System.IO.File.Exists(target))throw new Exception("백업 존재: 이전 실행부터 확인");System.IO.File.Copy(path,target);if(System.IO.File.Exists(path+".meta"))System.IO.File.Copy(path+".meta",target+".meta");}
System.IO.File.WriteAllText(folder+"/identities-before.txt",before);
var originalHeights=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);var heights=(float[,])originalHeights.Clone();
var originalMap=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);var map=(float[,,])originalMap.Clone();
float hx=data.size.x/(data.heightmapResolution-1),hz=data.size.z/(data.heightmapResolution-1);
var root=new GameObject("확장 생활권과 지형 경관").transform;root.SetParent(world.transform,false);var groups=new Dictionary<string,Transform>();
foreach(var region in regions){var t=new GameObject(region["name"].ToString()).transform;t.SetParent(root,false);groups.Add(region["id"].ToString(),t);}
var buildings=new List<Bounds>();var existingTrees=new List<Vector2>();
foreach(var t in world.GetComponentsInChildren<Transform>(true).Where(t=>PrefabUtility.IsOutermostPrefabInstanceRoot(t.gameObject))) {
    string path=PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
    if(path.Contains("/Foliage/Trees/")&&!path.Contains("Bush")){existingTrees.Add(new Vector2(t.position.x,t.position.z));continue;}
    if(!(path.Contains("PoorHouse_")||path.Contains("/Dungeon/")||path.Contains("/WorldCities/")||path.Contains("/LakeVillage/")))continue;
    var rs=t.GetComponentsInChildren<MeshRenderer>(true);if(rs.Length==0)continue;var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);b.Expand(new Vector3(8,0,8));buildings.Add(b);
}
Func<float,float,bool> reserved=(x,z)=>protectedPoints.Any(point=>Vector2.Distance(point,new Vector2(x,z))<14)||buildings.Any(b=>x>b.min.x&&x<b.max.x&&z>b.min.z&&z<b.max.z);
var skipped=new System.Text.StringBuilder("id,reason\n");var applied=new System.Text.StringBuilder("id,site,x,y,z,kind\n");
var createdAssets=new List<string>();bool hasAssetFolder=false,hasAppliedFoundations=false;int placed=0,foundationCount=0,crops=0,fieldCount=0;float maximumEarthwork=0;
try {
    foreach(var row in rows.OrderBy(r=>r["kind"].ToString()=="building"?0:1)) {
        string id=row["id"].ToString(),kind=row["kind"].ToString();float x=Convert.ToSingle(row["x"]),z=Convert.ToSingle(row["z"]),scale=Convert.ToSingle(row["scale"]);
        if(kind!="building"&&!hasAppliedFoundations){data.SetHeights(0,0,heights);hasAppliedFoundations=true;}
        float slope=data.GetSteepness((x-origin.x)/data.size.x,(z-origin.z)/data.size.z);
        if(reserved(x,z)||slope>(kind=="building"?14:kind=="prop"?18:43)) {skipped.AppendLine(id+",보호 영역 또는 경사");continue;}
        var go=(GameObject)PrefabUtility.InstantiatePrefab(assets[row["path"].ToString()],groups[row["site"].ToString()]);go.name=id+"_"+go.name;var t=go.transform;
        var p=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))+origin.y,z);t.SetPositionAndRotation(p,Quaternion.Euler(0,Convert.ToSingle(row["yaw"]),0));t.localScale=Vector3.one*scale;
        var rs=go.GetComponentsInChildren<Renderer>(true);var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
        if(kind=="building") {
            int x0=Mathf.Max(0,Mathf.FloorToInt((b.min.x-origin.x-4)/hx)),x1=Mathf.Min(data.heightmapResolution-1,Mathf.CeilToInt((b.max.x-origin.x+4)/hx));
            int z0=Mathf.Max(0,Mathf.FloorToInt((b.min.z-origin.z-4)/hz)),z1=Mathf.Min(data.heightmapResolution-1,Mathf.CeilToInt((b.max.z-origin.z+4)/hz));
            float localMaximum=0;
            for(int iz=z0;iz<=z1;iz++)for(int ix=x0;ix<=x1;ix++){float wx=origin.x+ix*hx,wz=origin.z+iz*hz,dx=Mathf.Max(0,b.min.x-wx,wx-b.max.x),dz=Mathf.Max(0,b.min.z-wz,wz-b.max.z);float weight=1-Mathf.SmoothStep(0,1,Mathf.Sqrt(dx*dx+dz*dz)/4);localMaximum=Mathf.Max(localMaximum,Mathf.Abs(p.y-origin.y-originalHeights[iz,ix]*data.size.y)*weight);}
            if(localMaximum>1.8f){UnityEngine.Object.DestroyImmediate(go);skipped.AppendLine(id+",국소 정지 한도 1.8m 초과");continue;}
            for(int iz=z0;iz<=z1;iz++)for(int ix=x0;ix<=x1;ix++){float wx=origin.x+ix*hx,wz=origin.z+iz*hz,dx=Mathf.Max(0,b.min.x-wx,wx-b.max.x),dz=Mathf.Max(0,b.min.z-wz,wz-b.max.z);float weight=1-Mathf.SmoothStep(0,1,Mathf.Sqrt(dx*dx+dz*dz)/4);heights[iz,ix]=Mathf.Lerp(heights[iz,ix],(p.y-origin.y)/data.size.y,weight);}
            t.position+=Vector3.up*(p.y-b.min.y-.08f);maximumEarthwork=Mathf.Max(maximumEarthwork,localMaximum);foundationCount++;
            b.Expand(new Vector3(2,0,2));buildings.Add(b);
        } else if(kind=="tree")t.position+=Vector3.down*.12f;
        else if(kind=="ground") {
            t.rotation=Quaternion.FromToRotation(Vector3.up,data.GetInterpolatedNormal((x-origin.x)/data.size.x,(z-origin.z)/data.size.z))*t.rotation;
            var sourceRs=assets[row["path"].ToString()].GetComponentsInChildren<Renderer>(true);var sourceBounds=sourceRs[0].bounds;foreach(var r in sourceRs)sourceBounds.Encapsulate(r.bounds);
            t.position+=Vector3.up*(-sourceBounds.min.y*scale-.03f);
        } else {
            float ground=p.y;
            if(kind=="rock"||kind=="outcrop")foreach(float px in new[]{b.min.x,b.center.x,b.max.x})foreach(float pz in new[]{b.min.z,b.center.z,b.max.z})ground=Mathf.Min(ground,terrain.SampleHeight(new Vector3(px,0,pz))+origin.y);
            t.position+=Vector3.up*(ground-b.min.y-(kind=="outcrop"?.5f:kind=="rock"?.2f:.04f));
        }
        PrefabUtility.RecordPrefabInstancePropertyModifications(t);placed++;
        applied.AppendLine(id+","+row["site"]+","+x+","+t.position.y+","+z+","+kind);
    }
    data.SetHeights(0,0,heights);
    AssetDatabase.CreateFolder("Assets/10.Datas/World/OpenWorld","DistrictInfill");hasAssetFolder=true;
    var crop=assets[plan["cropPath"].ToString()];var cropFilters=crop.GetComponentsInChildren<MeshFilter>(true);var random=new System.Random(11022026);
    foreach(var f in fields) {
        float cx=Convert.ToSingle(f["x"]),cz=Convert.ToSingle(f["z"]),w=Convert.ToSingle(f["w"]),d=Convert.ToSingle(f["d"]),spacing=Convert.ToSingle(f["spacing"]);
        var samples=new List<Vector3>();var angles=new List<float>();
        for(float x=cx-w/2+2;x<cx+w/2-2;x+=spacing)for(float z=cz-d/2+2;z<cz+d/2-2;z+=spacing) {
            if(reserved(x,z)||existingTrees.Any(point=>Vector2.Distance(point,new Vector2(x,z))<7)||roadDistance(new Vector2(x,z))<3||data.GetSteepness((x-origin.x)/data.size.x,(z-origin.z)/data.size.z)>16)continue;
            samples.Add(new Vector3(x-cx,terrain.SampleHeight(new Vector3(x,0,z))+origin.y-.04f,z-cz));angles.Add((float)random.NextDouble()*360);
        }
        if(samples.Count<20)continue;
        var field=new GameObject("경작지_"+f["id"]).transform;field.SetParent(groups["farm"],false);field.position=new Vector3(cx,0,cz);var renderers=new List<Renderer>();
        foreach(int level in new[]{1,2}) {
            var source=cropFilters.First(m=>m.name.Contains("LOD"+level));var combines=new CombineInstance[samples.Count];
            for(int k=0;k<samples.Count;k++){combines[k].mesh=source.sharedMesh;combines[k].transform=Matrix4x4.TRS(samples[k],Quaternion.Euler(0,angles[k],0),Vector3.one*.95f)*source.transform.localToWorldMatrix;}
            var mesh=new Mesh();mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;mesh.name="DistrictField_"+f["id"]+"_LOD"+level;mesh.CombineMeshes(combines,true,true);mesh.RecalculateBounds();
            string path=assetFolder+"/"+mesh.name+".asset";AssetDatabase.CreateAsset(mesh,path);createdAssets.Add(path);
            var child=new GameObject("Wheat_LOD"+level);child.transform.SetParent(field,false);child.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=child.AddComponent<MeshRenderer>();renderer.sharedMaterials=source.GetComponent<Renderer>().sharedMaterials;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderers.Add(renderer);
        }
        var lod=field.gameObject.AddComponent<LODGroup>();lod.SetLODs(new[]{new LOD(.12f,new[]{renderers[0]}),new LOD(.008f,new[]{renderers[1]})});lod.RecalculateBounds();fieldCount++;crops+=samples.Count;
    }
    var polygons=regions.Select(r=>(float[][])read(r["polygon"].ToString(),typeof(float[][]))).ToArray();var colors=regions.Select(r=>(float[])read(r["ground"].ToString(),typeof(float[]))).ToArray();
    Func<Vector2,float[][],bool> inside=(p,poly)=>{bool isInside=false;for(int i=0,j=poly.Length-1;i<poly.Length;j=i++){var a=poly[i];var b=poly[j];if((a[1]>p.y)!=(b[1]>p.y)&&p.x<(b[0]-a[0])*(p.y-a[1])/(b[1]-a[1])+a[0])isInside=!isInside;}return isInside;};
    float sx=data.size.x/data.alphamapWidth,sz=data.size.z/data.alphamapHeight;
    for(int iz=0;iz<data.alphamapHeight;iz++)for(int ix=0;ix<data.alphamapWidth;ix++) {
        var p=new Vector2(origin.x+(ix+.5f)*sx,origin.z+(iz+.5f)*sz);int region=-1;for(int i=0;i<polygons.Length;i++)if(inside(p,polygons[i])){region=i;break;}if(region<0||map[iz,ix,6]>.12f||protectedPoints.Any(point=>Vector2.Distance(p,point)<10))continue;
        float distance=roadDistance(p),weight=.65f;int layer=-1;
        if(distance<1.5f){weight=Mathf.Clamp01((2-distance)/2)*.8f;layer=5;}
        else foreach(var patch in patches){float dx=Mathf.Abs((p.x-Convert.ToSingle(patch["x"]))/Convert.ToSingle(patch["rx"])),dz=Mathf.Abs((p.y-Convert.ToSingle(patch["z"]))/Convert.ToSingle(patch["rz"]));float edge=patch["shape"].ToString()=="rectangle"?Mathf.Max(dx,dz):Mathf.Sqrt(dx*dx+dz*dz);float w=Mathf.Clamp01((1-edge)*6)*Convert.ToSingle(patch["strength"]);if(w>weight){weight=w;layer=Convert.ToInt32(patch["layer"]);}}
        if(layer<0)weight*=.65f+.35f*Mathf.PerlinNoise(p.x*.02f,p.y*.02f);
        for(int l=0;l<data.alphamapLayers;l++)map[iz,ix,l]=Mathf.Lerp(map[iz,ix,l],layer>=0?(l==layer?1:0):colors[region][l],weight);
    }
    data.SetAlphamaps(0,0,map);terrain.Flush();
    if(identities()!=before)throw new Exception("기존 진행 ID·좌표 변경");
    EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);Physics.SyncTransforms();
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
    System.IO.File.WriteAllText(folder+"/placements.csv",applied.ToString());System.IO.File.WriteAllText(folder+"/skipped.csv",skipped.ToString());System.IO.File.WriteAllText(folder+"/identities-after.txt",identities());
    string result="프리팹="+placed+" / 건물="+foundationCount+" / 밭="+fieldCount+" / 작물="+crops+" / 파생 메시="+createdAssets.Count+" / 최대 국소 정지="+maximumEarthwork.ToString("F3")+"m / 진행 ID·좌표 보존="+(identities()==before);
    System.IO.File.WriteAllText(folder+"/application.txt",result);return result;
}catch {
    UnityEngine.Object.DestroyImmediate(root.gameObject);data.SetHeights(0,0,originalHeights);data.SetAlphamaps(0,0,originalMap);terrain.Flush();
    foreach(string path in createdAssets)AssetDatabase.DeleteAsset(path);if(hasAssetFolder)AssetDatabase.DeleteAsset(assetFolder);
    EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);throw;
}


