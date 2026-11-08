// 백업된 저장본에만 적용하는 증분 저작. 전체 월드 생성기를 실행하지 않는다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string folder="output/OpenWorld/EnvironmentPolish";
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
if(!System.IO.File.Exists(folder+"/backup/LakeVillageWorld.unity")||!System.IO.File.Exists(folder+"/backup/Terrain.asset")||System.IO.File.Exists(folder+"/applied.txt"))throw new Exception("백업 누락 또는 이미 적용됨");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/environment-polish-plan.json"),typeof(Dictionary<string,object>));
var env=GameObject.Find("AuthoredEnvironment");var terrain=env.GetComponentInChildren<Terrain>();var data=terrain.terrainData;var origin=terrain.transform.position;
var identities=string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s).ToArray());
if(identities!=System.IO.File.ReadAllText(folder+"/identities-before.txt"))throw new Exception("백업 이후 진행 개체 변경");
string assetFolder="Assets/10.Datas/World/OpenWorld/EnvironmentPolish";
if(!AssetDatabase.IsValidFolder(assetFolder))AssetDatabase.CreateFolder("Assets/10.Datas/World/OpenWorld","EnvironmentPolish");
var alphaBefore=data.GetAlphamaps(0,0,data.alphamapWidth,data.alphamapHeight);var alpha=(float[,,])alphaBefore.Clone();
var created=new List<string>();var rows=new List<string>();int pixels=0,contacts=0,rocks=0;
Undo.IncrementCurrentGroup();int group=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("월드 비율과 지면 접합 개선");
try{
foreach(string path in (string[])read(plan["tunnels"].ToString(),typeof(string[]))){
    var target=GameObject.Find(path);if(target==null)throw new Exception("터널 누락: "+path);
    var filter=target.GetComponentInChildren<MeshFilter>();var original=filter.sharedMesh;var vertices=original.vertices;
    var mesh=new Mesh();mesh.name=target.name+"_MetricUV";mesh.indexFormat=UnityEngine.Rendering.IndexFormat.UInt32;
    var positions=new List<Vector3>();var uv=new List<Vector2>();var normals=new List<Vector3>();var submeshes=new List<int[]>();float tile=Convert.ToSingle(plan["tunnelTileMeters"]);
    for(int sub=0;sub<original.subMeshCount;sub++){
        var source=original.GetTriangles(sub);var indices=new int[source.Length];
        for(int i=0;i<source.Length;i+=3){
            Vector3 a=filter.transform.TransformVector(vertices[source[i]]),b=filter.transform.TransformVector(vertices[source[i+1]]),c=filter.transform.TransformVector(vertices[source[i+2]]);
            var normal=Vector3.Cross(b-a,c-a).normalized;var up=Mathf.Abs(normal.y)<.9f?Vector3.up:filter.transform.forward;
            var tangent=Vector3.Cross(up,normal).normalized;var bitangent=Vector3.Cross(normal,tangent);
            for(int corner=0;corner<3;corner++){
                var local=vertices[source[i+corner]];var world=filter.transform.TransformVector(local)+filter.transform.position-target.transform.position;
                indices[i+corner]=positions.Count;positions.Add(local);normals.Add(original.normals[source[i+corner]]);uv.Add(new Vector2(Vector3.Dot(world,tangent)/tile,Vector3.Dot(world,bitangent)/tile));
            }
        }submeshes.Add(indices);
    }
    mesh.SetVertices(positions);mesh.SetNormals(normals);mesh.SetUVs(0,uv);mesh.subMeshCount=original.subMeshCount;
    for(int sub=0;sub<submeshes.Count;sub++)mesh.SetTriangles(submeshes[sub],sub);
    mesh.RecalculateBounds();mesh.RecalculateTangents();
    if(Vector3.Distance(mesh.bounds.min,original.bounds.min)>.0001f||Vector3.Distance(mesh.bounds.max,original.bounds.max)>.0001f)throw new Exception("터널 외곽 변경");
    string asset=assetFolder+"/"+target.name+".asset";if(AssetDatabase.LoadMainAssetAtPath(asset)!=null)throw new Exception("파생 메시 이미 존재");
    AssetDatabase.CreateAsset(mesh,asset);created.Add(asset);Undo.RecordObject(filter,"터널 텍스처 비율");filter.sharedMesh=mesh;PrefabUtility.RecordPrefabInstancePropertyModifications(filter);
    rows.Add("터널 "+path+" triangles="+positions.Count/3+" bounds/충돌 원본 보존");
}
var lake=GameObject.Find((string)plan["lakeRoot"]);var prefixes=(string[])read(plan["rockPrefixes"].ToString(),typeof(string[]));
foreach(var t in lake.transform.Cast<Transform>().Where(t=>prefixes.Any(prefix=>t.name.StartsWith(prefix)))){
    if(t.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true).Length>0)throw new Exception("진행 개체를 포함한 바위");
    var renderer=t.GetComponentInChildren<MeshRenderer>();if(renderer==null)continue;var scale=t.localScale;
    float limit=Mathf.Max(Mathf.Abs(scale.x),Mathf.Abs(scale.z))*Convert.ToSingle(plan["rockHeightRatio"]);if(scale.y<=limit)continue;
    float bottom=renderer.bounds.min.y;Undo.RecordObject(t,"호안 바위 비율");scale.y=limit;t.localScale=scale;
    t.position+=Vector3.up*(bottom-renderer.bounds.min.y);PrefabUtility.RecordPrefabInstancePropertyModifications(t);rocks++;
    rows.Add("바위 "+AnimationUtility.CalculateTransformPath(t,null)+" scale="+scale.ToString("F3"));
}
// 지형 높이·도로와 충돌은 유지하고, 지역별 재질 혼합과 기단 가장자리만 바꾼다.
var regions=(Dictionary<string,object>[])read(plan["surfaceRegions"].ToString(),typeof(Dictionary<string,object>[]));
var slopes=(Dictionary<string,object>[])read(plan["slopeRegions"].ToString(),typeof(Dictionary<string,object>[]));
var regionShapes=regions.Select(r=>{var c=(float[])read(r["center"].ToString(),typeof(float[]));var size=(float[])read(r["radius"].ToString(),typeof(float[]));return new Vector4(c[0],c[1],size[0],size[1]);}).ToArray();
var regionWeights=regions.Select(r=>(float[])read(r["weights"].ToString(),typeof(float[]))).ToArray();
var regionStrengths=regions.Select(r=>Convert.ToSingle(r["strength"])).ToArray();
if(regionWeights.Any(w=>w.Length!=data.alphamapLayers))throw new Exception("지형 레이어 수 변경");
var slopeShapes=slopes.Select(r=>{var c=(float[])read(r["center"].ToString(),typeof(float[]));var size=(float[])read(r["radius"].ToString(),typeof(float[]));return new Vector4(c[0],c[1],size[0],size[1]);}).ToArray();
var sites=env.transform.Find("던전 공간 재저작");
var footings=sites.GetComponentsInChildren<MeshRenderer>(true).Where(r=>r.name.StartsWith("P_MOD_Wall_")||r.name.StartsWith("P_MOD_Gateway_")).Select(r=>r.bounds).ToArray();
for(int z=0;z<data.alphamapHeight;z++)for(int x=0;x<data.alphamapWidth;x++){
    float wx=origin.x+(x+.5f)/data.alphamapWidth*data.size.x,wz=origin.z+(z+.5f)/data.alphamapHeight*data.size.z;
    float noise=Mathf.PerlinNoise(wx*.027f,wz*.027f),detail=Mathf.PerlinNoise(wx*.11f+31,wz*.11f+67);bool changed=false;
    for(int regionIndex=0;regionIndex<regionShapes.Length;regionIndex++){
        var shape=regionShapes[regionIndex];float dx=(wx-shape.x)/shape.z,dz=(wz-shape.y)/shape.w,distance=Mathf.Sqrt(dx*dx+dz*dz);if(distance>1.08f)continue;
        float blend=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1.08f,distance)))*regionStrengths[regionIndex]*(.65f+.35f*noise);
        var weights=regionWeights[regionIndex];
        for(int l=0;l<weights.Length;l++)alpha[z,x,l]=Mathf.Lerp(alpha[z,x,l],weights[l],blend);changed=true;
    }
    foreach(var shape in slopeShapes){
        float dx=(wx-shape.x)/shape.z,dz=(wz-shape.y)/shape.w,distance=Mathf.Sqrt(dx*dx+dz*dz);if(distance>1)continue;
        float slope=data.GetSteepness((wx-origin.x)/data.size.x,(wz-origin.z)/data.size.z);if(slope<24||alpha[z,x,2]<.35f)continue;
        float blend=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.65f,1,distance)))*.75f;float stone=.38f+noise*.37f;
        var weights=new[]{.05f,1-stone-.05f,stone,0f,0f,0f,0f};
        for(int l=0;l<weights.Length;l++)alpha[z,x,l]=Mathf.Lerp(alpha[z,x,l],weights[l],blend);changed=true;
    }
    if(changed)pixels++;
}
float radiusContact=Convert.ToSingle(plan["contactRadius"]),contactStrength=Convert.ToSingle(plan["contactStrength"]);
var contactMask=new float[data.alphamapHeight,data.alphamapWidth];
foreach(var bounds in footings){
    int x0=Mathf.Clamp(Mathf.FloorToInt((bounds.min.x-radiusContact-origin.x)/data.size.x*data.alphamapWidth),0,data.alphamapWidth-1),x1=Mathf.Clamp(Mathf.CeilToInt((bounds.max.x+radiusContact-origin.x)/data.size.x*data.alphamapWidth),0,data.alphamapWidth-1);
    int z0=Mathf.Clamp(Mathf.FloorToInt((bounds.min.z-radiusContact-origin.z)/data.size.z*data.alphamapHeight),0,data.alphamapHeight-1),z1=Mathf.Clamp(Mathf.CeilToInt((bounds.max.z+radiusContact-origin.z)/data.size.z*data.alphamapHeight),0,data.alphamapHeight-1);
    for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++){
        float wx=origin.x+(x+.5f)/data.alphamapWidth*data.size.x,wz=origin.z+(z+.5f)/data.alphamapHeight*data.size.z;
        float dx=Mathf.Max(bounds.min.x-wx,0,wx-bounds.max.x),dz=Mathf.Max(bounds.min.z-wz,0,wz-bounds.max.z);
        contactMask[z,x]=Mathf.Max(contactMask[z,x],1-Mathf.Clamp01(Mathf.Sqrt(dx*dx+dz*dz)/radiusContact));
    }
}
var contactWeights=new[]{.03f,.61f,.29f,0f,0f,.07f,0f};
for(int z=0;z<data.alphamapHeight;z++)for(int x=0;x<data.alphamapWidth;x++){
    if(contactMask[z,x]<=0)continue;
    float blend=contactMask[z,x]*contactStrength;
    for(int l=0;l<contactWeights.Length;l++)alpha[z,x,l]=Mathf.Lerp(alpha[z,x,l],contactWeights[l],blend);contacts++;
}
data.SetAlphamaps(0,0,alpha);terrain.Flush();Physics.SyncTransforms();
var after=string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s).ToArray());
if(identities!=after)throw new Exception("진행 개체 변경");
EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
Undo.CollapseUndoOperations(group);rows.Add("RESULT tunnels=2 rocks="+rocks+" surfacePixels="+pixels+" contacts="+contacts+" 진행 ID/좌표 보존");
System.IO.File.WriteAllLines(folder+"/applied.txt",rows);return rows.Last();
}catch{
Undo.RevertAllDownToGroup(group);data.SetAlphamaps(0,0,alphaBefore);EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
foreach(string path in created)AssetDatabase.DeleteAsset(path);throw;
}
