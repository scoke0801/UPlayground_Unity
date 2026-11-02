var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if(EditorApplication.isPlayingOrWillChangePlaymode||EditorApplication.isCompiling||scene.path!="Assets/01.Scenes/Scenario/OpenWorld/LakeVillageWorld.unity")throw new Exception("월드 Edit Mode 필요");
var world=GameObject.Find("AuthoredEnvironment");var root=world.transform.Find("확장 생활권과 지형 경관");
if(root==null)throw new Exception("신규 배치 누락");
string folder="output/OpenWorld/DistrictInfill",navPath="Assets/10.Datas/World/OpenWorld/DistrictInfillNavigation.asset";
if(AssetDatabase.LoadMainAssetAtPath(navPath)!=null)throw new Exception("이미 저장한 NavMesh입니다. 재실행하지 마세요.");
var json=AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Newtonsoft.Json.JsonConvert")).First(t=>t!=null);
var parse=json.GetMethod("DeserializeObject",new[]{typeof(string),typeof(Type)});Func<string,Type,object> read=(s,t)=>parse.Invoke(null,new object[]{s,t});
var plan=(Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/OpenWorld/district-infill-plan.json"),typeof(Dictionary<string,object>));
var routes=(Dictionary<string,object>[])read(plan["routes"].ToString(),typeof(Dictionary<string,object>[]));
if(root.Find("갈림길 우선 이동 영역")==null) {
    var preferences=new GameObject("갈림길 우선 이동 영역").transform;preferences.SetParent(root,false);
    foreach(var route in routes) {
        var points=(float[][])read(route["points"].ToString(),typeof(float[][]));
        for(int i=1;i<points.Length;i++) {
            var a=new Vector3(points[i-1][0],200,points[i-1][1]);var b=new Vector3(points[i][0],200,points[i][1]);
            var go=new GameObject(route["site"]+"_"+i);go.transform.SetParent(preferences,false);go.transform.SetPositionAndRotation((a+b)*.5f,Quaternion.LookRotation(b-a));
            var volume=go.AddComponent<Unity.AI.Navigation.NavMeshModifierVolume>();volume.area=UnityEngine.AI.NavMesh.GetAreaFromName("Walkable");volume.size=new Vector3(10,400,Vector3.Distance(a,b)+8);
        }
    }
}
Physics.SyncTransforms();
System.IO.File.WriteAllText(folder+"/navigation-phase.txt","NavMesh 생성 시작");
var surface=world.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
surface.BuildNavMesh();
if(surface.navMeshData==null)throw new Exception("NavMesh 생성 실패");
AssetDatabase.CreateAsset(surface.navMeshData,navPath);AssetDatabase.SaveAssetIfDirty(surface.navMeshData);
EditorUtility.SetDirty(surface);UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
System.IO.File.WriteAllText(folder+"/navigation-phase.txt","NavMesh·씬 저장 완료");
return navPath;

