// MCP 실행 전에 CITY_ID를 대상 지역 ID로 바꾼다. 배치는 CSV, 결과는 Unity 씬이 소유한다.
string cityId = "CITY_ID";
bool harbor = cityId == "SaltwindHarbor";
string scenePath = "Assets/01.Scenes/Scenario/"+cityId+".unity";
if(System.IO.File.Exists(scenePath)) return "이미 제작된 씬을 보존합니다: "+scenePath;
string directory="Assets/10.Datas/World/WorldCities/"+cityId;
System.IO.Directory.CreateDirectory(directory);
var sourceScene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("LakeOfLifeScenario");
if(!sourceScene.IsValid()) throw new System.Exception("시나리오 원본 씬을 먼저 추가로 여세요.");
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
var environment=new GameObject("CityEnvironment");
int groundLayer=LayerMask.NameToLayer("Ground");
var terrainData=new TerrainData();
terrainData.heightmapResolution=257;
terrainData.alphamapResolution=256;
terrainData.size=new Vector3(360,65,360);
UnityEditor.AssetDatabase.CreateAsset(terrainData,directory+"/Terrain.asset");
var sourceTerrain=UnityEditor.AssetDatabase.LoadAssetAtPath<TerrainData>("Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/Terrain.asset");
terrainData.terrainLayers=sourceTerrain.terrainLayers;
float[,] heights=new float[257,257];
for(int z=0;z<257;z++)for(int x=0;x<257;x++){
    float wx=x*360f/256,wz=z*360f/256;
    float edge=Mathf.Max(Mathf.Abs(wx-180)-82,Mathf.Abs(wz-180)-130);
    float hill=Mathf.SmoothStep(0,1,Mathf.Clamp01(edge/50))* (17+Mathf.PerlinNoise(wx*.018f,wz*.018f)*18);
    float h=10+hill;
    if(harbor && wz>224)h=Mathf.Lerp(h,2,Mathf.Clamp01((wz-224)/20));
    heights[z,x]=h/65;
}
terrainData.SetHeights(0,0,heights);
int layerCount=terrainData.terrainLayers.Length;
if(layerCount<2)throw new System.Exception("원본 지형 레이어를 확인하세요.");
float[,,] paint=new float[256,256,layerCount];
for(int z=0;z<256;z++)for(int x=0;x<256;x++){
    float wx=x*360f/255,wz=z*360f/255;
    float roadX=180+Mathf.Sin((wz-35)*.045f)*8*Mathf.Clamp01((120-wz)/45);
    bool road=Mathf.Abs(wx-roadX)<6 && wz>24&&wz<275;
    bool square=wx>126&&wx<234&&wz>135&&wz<225;
    float amount=(road||square)?1:0;
    paint[z,x,0]=1-amount;paint[z,x,1]=amount;
}
terrainData.SetAlphamaps(0,0,paint);
var terrainObject=Terrain.CreateTerrainGameObject(terrainData);
terrainObject.name="Terrain";terrainObject.transform.SetParent(environment.transform);terrainObject.layer=groundLayer;
var terrain=terrainObject.GetComponent<Terrain>();
terrain.materialTemplate=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/Terrain.mat");
terrain.drawInstanced=true;terrain.heightmapPixelError=8;
System.Func<string,Vector3,float,float,GameObject> place=(model,position,yaw,scale)=>{
    string p="Assets/03.Prefabs/Environment/WorldCities/"+model+".prefab";
    var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(p);
    if(prefab==null)throw new System.Exception("프리팹 없음: "+p);
    var obj=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,scene);
    obj.transform.SetParent(environment.transform);
    obj.transform.position=position;obj.transform.rotation=Quaternion.Euler(0,yaw,0);obj.transform.localScale=Vector3.one*scale;
    return obj;
};
var culture=System.Globalization.CultureInfo.InvariantCulture;
foreach(var line in System.IO.File.ReadAllLines("ArtSource/WorldCities/"+(harbor?"harbor":"castle")+"-layout.csv")){
    if(line.StartsWith("model,")||string.IsNullOrWhiteSpace(line))continue;
    var values=line.Split(',');
    float x=float.Parse(values[1],culture),z=float.Parse(values[2],culture);
    float y=values[0]=="CoastalBoat"?5.2f:10;
    place(values[0],new Vector3(x,y,z),float.Parse(values[3],culture),float.Parse(values[4],culture));
}
if(!harbor){
    for(int x=126;x<=234;x+=12){
        if(x<=162||x>=198)place("CastleWall",new Vector3(x,10,120),0,1);
        place("CastleWall",new Vector3(x,10,248),0,1);
    }
    for(int z=132;z<248;z+=12){place("CastleWall",new Vector3(120,10,z),90,1);place("CastleWall",new Vector3(240,10,z),90,1);}
}
var stone=new Material(Shader.Find("Universal Render Pipeline/Lit"));stone.color=new Color(.49f,.43f,.33f);stone.SetFloat("_Smoothness",.1f);
UnityEditor.AssetDatabase.CreateAsset(stone,directory+"/LandscapeStone.mat");
System.Func<string,Vector3,Vector3,GameObject> block=(name,position,size)=>{
    var obj=GameObject.CreatePrimitive(PrimitiveType.Cube);obj.name=name;obj.layer=groundLayer;
    obj.transform.SetParent(environment.transform);obj.transform.position=position;obj.transform.localScale=size;
    obj.GetComponent<Renderer>().sharedMaterial=stone;obj.isStatic=true;return obj;
};
if(harbor){
    var water=GameObject.CreatePrimitive(PrimitiveType.Plane);water.name="CoastalWater";
    water.transform.SetParent(environment.transform);water.transform.position=new Vector3(180,5,575);water.transform.localScale=new Vector3(100,1,70);
    UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());
    var sourceWater=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/LakeWater.mat");
    var sea=new Material(sourceWater);UnityEditor.AssetDatabase.CreateAsset(sea,directory+"/Sea.mat");water.GetComponent<Renderer>().sharedMaterial=sea;
    block("Quayside",new Vector3(180,7.8f,228),new Vector3(115,4.4f,10));
    for(int x=124;x<=236;x+=4)if(Mathf.Abs(x-165)>5&&Mathf.Abs(x-195)>5)block("QuayParapet",new Vector3(x,10.65f,232),new Vector3(4,1.3f,.65f));
    foreach(int x in new[]{165,195})block("PierEndRail",new Vector3(x,11,248),new Vector3(7,1,.3f));
    block("WestBreakwater",new Vector3(116,7.8f,250),new Vector3(8,4.4f,50));
    block("BreakwaterParapet",new Vector3(112.5f,10.7f,250),new Vector3(.7f,1.4f,50));
    block("BreakwaterEnd",new Vector3(116,10.7f,274.5f),new Vector3(8,1.4f,.7f));
}
// 주변 산과 수목이 맵 외곽을 읽게 하고, 물 가장자리에는 보이는 난간을 둔다.
var random=new System.Random(harbor?20261024:20261025);
string treePath="Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/BroadleafTree_01_Green.prefab";
var treePrefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(treePath);
if(treePrefab!=null)for(int i=0;i<95;i++){
    float x=35+(float)random.NextDouble()*290,z=35+(float)random.NextDouble()*(harbor?185:290);
    if(x>105&&x<255&&z>65)continue;
    if(Mathf.Abs(x-180)<24)continue;
    var tree=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(treePrefab,scene);
    tree.name="WoodlandTree";tree.transform.SetParent(environment.transform);
    tree.transform.position=new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z)),z);
    tree.transform.rotation=Quaternion.Euler(0,(float)random.NextDouble()*360,0);
    tree.transform.localScale=Vector3.one*(.75f+(float)random.NextDouble()*.55f);
}
foreach(var sourceRoot in sourceScene.GetRootGameObjects())if(sourceRoot.name=="SceneContext"||sourceRoot.name=="Player"||sourceRoot.name=="MainCamera"){
    var clone=UnityEngine.Object.Instantiate(sourceRoot);clone.name=sourceRoot.name;
    UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone,scene);
    if(clone.name=="Player"){clone.transform.position=new Vector3(180,10.25f,55);clone.transform.rotation=Quaternion.identity;clone.SetActive(true);}
    if(clone.name=="SceneContext"){
        var component=clone.GetComponent("SceneContext");var so=new UnityEditor.SerializedObject(component);
        so.FindProperty("MapID").stringValue=cityId;so.FindProperty("_regionInfoOverride").objectReferenceValue=null;so.ApplyModifiedPropertiesWithoutUndo();
    }
    if(clone.name=="MainCamera"){clone.transform.position=new Vector3(180,15,35);clone.transform.rotation=Quaternion.Euler(12,0,0);}
}
var sunObject=new GameObject("Directional Light");var sun=sunObject.AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.3f;sun.color=new Color(1,.94f,.82f);sun.shadows=LightShadows.Soft;sunObject.transform.rotation=Quaternion.Euler(48,-32,0);
RenderSettings.sun=sun;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;
RenderSettings.ambientSkyColor=new Color(.6f,.7f,.82f);RenderSettings.ambientEquatorColor=new Color(.5f,.54f,.57f);RenderSettings.ambientGroundColor=new Color(.29f,.30f,.25f);
RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=200;RenderSettings.fogEndDistance=650;RenderSettings.fogColor=new Color(.63f,.77f,.83f);
// 월드 조명 매니저의 덮어쓰기를 기존 바인딩 계약으로 제어한다.
var bindingType=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Gameplay.World.WorldLightingSceneBinding")).FirstOrDefault(t=>t!=null);
if(bindingType!=null){var binding=new GameObject("CityLighting").AddComponent(bindingType);var so=new UnityEditor.SerializedObject(binding);so.FindProperty("sunLight").objectReferenceValue=sun;so.FindProperty("disableWorldLighting").boolValue=true;so.ApplyModifiedPropertiesWithoutUndo();}
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,scenePath))throw new System.Exception("씬 저장 실패");
UnityEditor.AssetDatabase.SaveAssets();
return cityId+": 도시 씬과 지형·건물 저장 완료";
