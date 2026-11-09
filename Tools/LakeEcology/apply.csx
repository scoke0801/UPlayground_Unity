// MCP execute_code에서 실행하는 증분 저작. 기존 지형·수면·진행 개체는 수정하지 않는다.
var jsonType = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("Newtonsoft.Json.JsonConvert")).First(t => t != null);
var parse = jsonType.GetMethod("DeserializeObject",new[] { typeof(string),typeof(Type) });
Func<string,Type,object> read = (s,t) => parse.Invoke(null,new object[] { s,t });
var config = (Dictionary<string,object>)read(System.IO.File.ReadAllText("Tools/LakeEcology/ecology.json"),typeof(Dictionary<string,object>));
var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || scene.path != (string)config["scene"] || scene.isDirty)
    throw new Exception("컴파일이 끝난 저장된 정규 월드의 Edit Mode에서 실행해야 합니다.");
string rootName = (string)config["root"], folder = (string)config["assetFolder"];
var transforms = scene.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<Transform>(true)).ToArray();
if (transforms.Any(t => t.name == rootName) || AssetDatabase.IsValidFolder(folder))
    throw new Exception("기존 수중 저작을 덮어쓰지 않습니다. 기존 결과를 검토하세요.");
var water = transforms.Single(t => t.name == (string)config["waterObject"]).GetComponent<UPlayGround.Components.FootstepWaterSurface>();
if (water == null || water.surfaceMesh == null) throw new Exception("등록된 호수 수면이 필요합니다.");
water.RefreshMesh();
var sample = water.GetType().GetMethod("TrySampleHeight", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
var terrain = Terrain.activeTerrains.Single(t => t.gameObject.scene == scene);
var bounds = water.surfaceMesh.GetComponent<Renderer>().bounds;
var protectedTypes = new[] { "SceneEntityId", "SceneArrivalPoint", "PortalActor", "FlowGraphTriggerVolume" };
var protectedPoints = transforms.Where(t => t.GetComponents<MonoBehaviour>().Any(c => c != null && protectedTypes.Contains(c.GetType().Name))).Select(t => t.position).ToArray();
Func<Vector3, float> depthAt = p => {
    object[] args = { p, Vector3.zero };
    if (!(bool)sample.Invoke(water, args)) return -1;
    return ((Vector3)args[1]).y - (terrain.SampleHeight(p) + terrain.transform.position.y);
};
float protection = Convert.ToSingle(config["protectedRadius"]);
Func<Vector3, bool> isProtected = p => protectedPoints.Any(q => new Vector2(q.x-p.x,q.z-p.z).sqrMagnitude < protection*protection)
    || Physics.OverlapSphere(p+Vector3.up*.5f,.65f,~0,QueryTriggerInteraction.Ignore).Any(c => !(c is TerrainCollider) && c.transform.root.name != rootName);
var random = new System.Random(Convert.ToInt32(config["seed"]));
Func<float, float, float> range = (a,b) => Mathf.Lerp(a,b,(float)random.NextDouble());
string source = (string)config["prefabFolder"];
var plants = ((string[])read(config["plants"].ToString(),typeof(string[]))).Select(v => AssetDatabase.LoadAssetAtPath<GameObject>(source+(string)v)).ToArray();
var rocks = ((string[])read(config["rocks"].ToString(),typeof(string[]))).Select(v => AssetDatabase.LoadAssetAtPath<GameObject>(source+(string)v)).ToArray();
var fish = AssetDatabase.LoadAssetAtPath<GameObject>((string)config["fishPrefab"]);
if (plants.Any(p => p == null) || rocks.Any(p => p == null) || fish == null) throw new Exception("원본 프리팹 누락");
System.IO.Directory.CreateDirectory("output/LakeEcology");
if (!System.IO.File.Exists("output/LakeEcology/scene-before.unity")) System.IO.File.Copy(scene.path,"output/LakeEcology/scene-before.unity",false);
var root = new GameObject(rootName);
int patchCount = 0, plantCount = 0, rockCount = 0, schools = 0;
var candidates = new List<Vector3>();
var records = new List<object>();
try {
    AssetDatabase.CreateFolder("Assets/10.Datas/World/OpenWorld",rootName);
    // 폴더명과 루트 이름을 같은 계약으로 유지한다.
    if (!AssetDatabase.IsValidFolder(folder)) throw new Exception("설정의 assetFolder와 root 이름이 다릅니다.");
    Func<GameObject,Transform,Vector3,float,GameObject> place = (prefab,parent,position,height) => {
        var obj = (GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
        obj.transform.SetParent(parent,false);
        obj.transform.position=position;
        obj.transform.rotation=Quaternion.Euler(0,range(0,360),0);
        var renderers=obj.GetComponentsInChildren<Renderer>();
        if(renderers.Length==0) throw new Exception("Renderer가 없는 프리팹: "+prefab.name);
        var box=renderers[0].bounds; foreach(var r in renderers) box.Encapsulate(r.bounds);
        obj.transform.localScale*=height/Mathf.Max(.01f,box.size.y);
        box=renderers[0].bounds; foreach(var r in renderers) box.Encapsulate(r.bounds);
        obj.transform.position+=Vector3.up*(position.y-box.min.y-.05f);
        foreach(var c in obj.GetComponentsInChildren<Collider>(true)) UnityEngine.Object.DestroyImmediate(c);
        foreach(var r in renderers) r.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
        return obj;
    };
    float spacing=Convert.ToSingle(config["spacing"]);
    for(float z=bounds.min.z+spacing;z<bounds.max.z-spacing;z+=spacing)
    for(float x=bounds.min.x+spacing;x<bounds.max.x-spacing;x+=spacing) {
        var p=new Vector3(x+range(-2,2),0,z+range(-2,2));
        float depth=depthAt(p); p.y=terrain.SampleHeight(p)+terrain.transform.position.y;
        if(depth<Convert.ToSingle(config["minimumDepth"]) || depth>Convert.ToSingle(config["maximumDepth"]) || isProtected(p)) continue;
        candidates.Add(p);
        if(patchCount >= Convert.ToInt32(config["maximumPatches"]) || Mathf.PerlinNoise(x*Convert.ToSingle(config["clusterFrequency"]),z*Convert.ToSingle(config["clusterFrequency"]))<Convert.ToSingle(config["clusterThreshold"])) continue;
        var patch=new GameObject("BedPatch_"+patchCount++); patch.transform.SetParent(root.transform); patch.transform.position=p;
        for(int i=0;i<7;i++) {
            var q=p+new Vector3(range(-2.4f,2.4f),0,range(-2.4f,2.4f));
            float localDepth=depthAt(q); q.y=terrain.SampleHeight(q)+terrain.transform.position.y;
            if(localDepth<Convert.ToSingle(config["minimumDepth"]) || isProtected(q)) continue;
            bool isRock=i==0 || depth>4;
            var palette=isRock?rocks:plants; var limits=(float[])read(config[isRock?"rockHeight":"plantHeight"].ToString(),typeof(float[]));
            float height=Mathf.Min(range((float)limits[0],(float)limits[1]),localDepth-Convert.ToSingle(config["surfaceClearance"]));
            if(height<.15f) continue;
            var obj=place(palette[random.Next(palette.Length)],patch.transform,q,height);
            if(isRock) rockCount++; else plantCount++;
            records.Add(new { name=obj.name, position=obj.transform.position.ToString("F3"), depth=localDepth });
        }
    }
    // 작은 순환 경로를 미리 검사하고 애니메이션으로 재생해 물고기별 Update를 추가하지 않는다.
    foreach(var p in candidates.OrderBy(p=>random.Next())) {
        if(schools >= Convert.ToInt32(config["fishSchools"])) break;
        float radius=Convert.ToSingle(config["fishOrbitRadius"]), surface=p.y+depthAt(p), fishY=surface-1.25f;
        bool safe=true;
        for(int i=0;i<64;i++) {
            float a=i*Mathf.PI*2/64;
            var q=p+new Vector3(Mathf.Cos(a)*(radius+.6f),0,Mathf.Sin(a)*(radius+.6f));
            if(depthAt(q)<2.4f || terrain.SampleHeight(q)+terrain.transform.position.y>fishY-.8f || isProtected(new Vector3(q.x,fishY,q.z))) {safe=false;break;}
        }
        if(!safe || root.GetComponentsInChildren<Animation>().Any(a=>Vector3.Distance(a.transform.position,new Vector3(p.x,fishY,p.z))<18)) continue;
        var school=new GameObject("PerchSchool_"+schools);school.transform.SetParent(root.transform);school.transform.position=new Vector3(p.x,fishY,p.z);
        var clip=new AnimationClip { legacy=true,wrapMode=WrapMode.Loop };
        float duration=Convert.ToSingle(config["fishOrbitSeconds"])+schools*2;
        clip.SetCurve("",typeof(Transform),"localEulerAngles.y",AnimationCurve.Linear(0,0,duration,360));
        AssetDatabase.CreateAsset(clip,folder+"/School_"+schools+".anim");
        var motion=school.AddComponent<Animation>();motion.AddClip(clip,"Swim");motion.clip=clip;motion.playAutomatically=true;motion.cullingType=AnimationCullingType.BasedOnRenderers;
        for(int i=0;i<Convert.ToInt32(config["fishPerSchool"]);i++) {
            var obj=(GameObject)PrefabUtility.InstantiatePrefab(fish,scene);obj.transform.SetParent(school.transform,false);
            float angle=i*.22f, r=radius-range(0,.6f);
            obj.transform.localPosition=new Vector3(Mathf.Cos(angle)*r,range(-.2f,.2f),Mathf.Sin(angle)*r);
            obj.transform.localRotation=Quaternion.Euler(0,-angle*Mathf.Rad2Deg+180,0);
            var renderers=obj.GetComponentsInChildren<Renderer>();var box=renderers[0].bounds;foreach(var renderer in renderers)box.Encapsulate(renderer.bounds);
            obj.transform.localScale*=Convert.ToSingle(config["fishLength"])/Mathf.Max(box.size.x,Mathf.Max(box.size.y,box.size.z));
            foreach(var c in obj.GetComponentsInChildren<Collider>(true))UnityEngine.Object.DestroyImmediate(c);
            foreach(var animator in obj.GetComponentsInChildren<Animator>())animator.applyRootMotion=false;
        }
        schools++;
    }
    if(plantCount==0 || rockCount==0 || schools==0) throw new Exception("배치 부족: plants="+plantCount+" rocks="+rockCount+" schools="+schools);
    if(root.GetComponentsInChildren<Transform>(true).Any(t=>GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)>0))throw new Exception("Missing Script 발견");
    if(root.GetComponentsInChildren<Renderer>(true).Any(r=>r.sharedMaterials.Any(m=>m==null || m.shader==null || m.shader.name=="Hidden/InternalErrorShader")))throw new Exception("재료 또는 셰이더 누락");
    PrefabUtility.SaveAsPrefabAssetAndConnect(root,folder+"/LakeUnderwaterEcology.prefab",InteractionMode.AutomatedAction);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new Exception("씬 저장 실패");
    var report=new {patchCount,plantCount,rockCount,schools,fishCount=schools*Convert.ToInt32(config["fishPerSchool"]),records};
    System.IO.File.WriteAllText("output/LakeEcology/placement.json",(string)jsonType.GetMethod("SerializeObject",new[] { typeof(object) }).Invoke(null,new object[] { report }));
    return report;
} catch {
    if(root!=null)UnityEngine.Object.DestroyImmediate(root);
    // 신규 전용 폴더만 정리한다. 기존 씬 사본은 실패 조사와 사용자 변경 보호를 위해 남긴다.
    if(AssetDatabase.IsValidFolder(folder))AssetDatabase.DeleteAsset(folder);
    throw;
}
