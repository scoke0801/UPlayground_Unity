// 평평한 유적 마당에 장소별 지면과 무너진 외벽 높낮이를 저작한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();string folder="output/OpenWorld/WorldPlayability";
if(EditorApplication.isPlayingOrWillChangePlaymode||scene.isDirty||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("저장된 월드 Edit Mode 필요");
if(System.IO.File.Exists(folder+"/dressing.txt"))throw new Exception("이미 적용한 경관입니다.");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});
Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/world-playability-plan.json"),typeof(Dictionary<string,object>));
var art=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/dungeon-art-plan.json"),typeof(Dictionary<string,object>));
var sites=(Dictionary<string,object>[])read(art["sites"].ToString(),typeof(Dictionary<string,object>[]));
var terrain=GameObject.Find("AuthoredEnvironment").GetComponentInChildren<Terrain>();var data=terrain.terrainData;var origin=terrain.transform.position;
var before=string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s).ToArray());
if(!System.IO.File.Exists(folder+"/backup/Terrain.asset"))System.IO.File.Copy(AssetDatabase.GetAssetPath(data),folder+"/backup/Terrain.asset");
var patches=new List<float[,,]>();var starts=new List<Vector2Int>();
Undo.IncrementCurrentGroup();int undo=Undo.GetCurrentGroup();Undo.SetCurrentGroupName("유적 지면과 외벽 풍화");int walls=0,pixels=0;
try {
    foreach(var patch in (Dictionary<string,object>[])read(plan["ground"].ToString(),typeof(Dictionary<string,object>[]))) {
        var center=(float[])read(patch["center"].ToString(),typeof(float[]));var radius=(float[])read(patch["radius"].ToString(),typeof(float[]));var weights=(float[])read(patch["weights"].ToString(),typeof(float[]));
        if(weights.Length!=data.alphamapLayers)throw new Exception("Terrain Layer 수 변경");
        int x0=Mathf.Clamp(Mathf.FloorToInt((center[0]-radius[0]*1.25f-origin.x)/data.size.x*data.alphamapWidth),0,data.alphamapWidth-1);
        int z0=Mathf.Clamp(Mathf.FloorToInt((center[1]-radius[1]*1.25f-origin.z)/data.size.z*data.alphamapHeight),0,data.alphamapHeight-1);
        int x1=Mathf.Clamp(Mathf.CeilToInt((center[0]+radius[0]*1.25f-origin.x)/data.size.x*data.alphamapWidth),0,data.alphamapWidth-1);
        int z1=Mathf.Clamp(Mathf.CeilToInt((center[1]+radius[1]*1.25f-origin.z)/data.size.z*data.alphamapHeight),0,data.alphamapHeight-1);
        var alpha=data.GetAlphamaps(x0,z0,x1-x0+1,z1-z0+1);patches.Add((float[,,])alpha.Clone());starts.Add(new Vector2Int(x0,z0));
        for(int z=0;z<alpha.GetLength(0);z++)for(int x=0;x<alpha.GetLength(1);x++) {
            float wx=origin.x+(x0+x+.5f)/data.alphamapWidth*data.size.x,wz=origin.z+(z0+z+.5f)/data.alphamapHeight*data.size.z;
            float dx=(wx-center[0])/radius[0],dz=(wz-center[1])/radius[1];
            float noise=Mathf.PerlinNoise(wx*.065f,wz*.065f);
            float r=Mathf.Sqrt(dx*dx+dz*dz)+(noise-.5f)*.28f;
            float blend=(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(.45f,1.08f,r)))*Convert.ToSingle(patch["strength"])*(.75f+noise*.25f);
            if(blend<=0)continue;
            for(int l=0;l<weights.Length;l++)alpha[z,x,l]=Mathf.Lerp(alpha[z,x,l],weights[l],blend);
            pixels++;
        }
        data.SetAlphamaps(x0,z0,alpha);
    }
    foreach(var wear in (Dictionary<string,object>[])read(plan["weathering"].ToString(),typeof(Dictionary<string,object>[]))) {
        var site=sites.Single(s=>(string)s["id"]==(string)wear["site"]);
        var root=GameObject.Find("AuthoredEnvironment/던전 공간 재저작/"+site["name"]);
        var center=(float[])read(wear["center"].ToString(),typeof(float[]));var sides=(string[])read(wear["sides"].ToString(),typeof(string[]));
        foreach(var wall in root.transform.Cast<Transform>().Where(t=>t.name.StartsWith("wall_")||t.name.StartsWith("tall_"))) {
            var mesh=wall.GetComponentsInChildren<MeshRenderer>(true).First(r=>r.name.StartsWith("P_MOD_Wall_"));var p=mesh.bounds.center;
            bool edge=sides.Any(side=>side=="north"?p.z-center[1]>25:side=="west"?p.x-center[0]<-32:p.x-center[0]>32);
            if(!edge||Mathf.Sin(p.x*.19f+p.z*.27f)<.15f)continue;
            Undo.RecordObject(wall,"외벽 풍화");wall.position-=Vector3.up*Convert.ToSingle(wear["drop"])*(.8f+.2f*Mathf.Sin(p.x*.31f));
            wall.rotation*=Quaternion.Euler(0,0,Mathf.Sin(p.z*.2f)*6);
            PrefabUtility.RecordPrefabInstancePropertyModifications(wall);walls++;
        }
    }
    var after=string.Join("\n",scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.Components.SceneEntityId>(true)).Select(c=>c.Guid+"|"+c.transform.position.ToString("F4")).OrderBy(s=>s).ToArray());
    if(before!=after)throw new Exception("진행 개체 변경");
    terrain.Flush();Physics.SyncTransforms();EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
    Undo.CollapseUndoOperations(undo);string result="장소별 지면=4 / 혼합 표본="+pixels+" / 외벽 풍화="+walls+" / 진행 ID·좌표 보존";
    System.IO.File.WriteAllText(folder+"/dressing.txt",result);return result;
}catch{
    Undo.RevertAllDownToGroup(undo);for(int i=0;i<patches.Count;i++)data.SetAlphamaps(starts[i].x,starts[i].y,patches[i]);
    EditorUtility.SetDirty(data);AssetDatabase.SaveAssetIfDirty(data);throw;
}
