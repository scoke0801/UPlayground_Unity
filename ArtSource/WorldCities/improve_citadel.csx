// 돌등성 씬 전용 저작 기록. 런타임 규칙은 추가하지 않으며 결과는 씬과 SO가 소유한다.
var previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("StonegateCitadel");
if (scene.name != "StonegateCitadel" || Application.isPlaying) throw new System.Exception("돌등성 Edit Mode에서 실행하세요.");
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
try {
var environment = scene.GetRootGameObjects().First(r => r.name == "CityEnvironment");
if (environment.transform.Find("CitadelLife") != null) throw new System.Exception("이미 반영된 배치는 씬에서 수정하세요.");
UnityEditor.Undo.RegisterFullObjectHierarchyUndo(environment, "돌등성 생활 공간 개선");
string directory = "Assets/10.Datas/World/WorldCities/StonegateCitadel";
string kit = "Assets/03.Prefabs/Environment/WorldCities/";
string nature = "Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/";
var root = new GameObject("CitadelLife").transform;
root.SetParent(environment.transform);
UnityEditor.Undo.RegisterCreatedObjectUndo(root.gameObject, "돌등성 생활 공간 개선");
var terrain = environment.GetComponentInChildren<Terrain>();
int ground = LayerMask.NameToLayer("Ground");
System.Func<string, Vector3, float, Vector3, GameObject> place = (path, position, yaw, scale) => {
    var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
    if (prefab == null) throw new System.Exception("프리팹 없음: " + path);
    var obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab, scene);
    obj.transform.SetParent(root); obj.transform.position = position;
    obj.transform.rotation = Quaternion.Euler(0, yaw, 0); obj.transform.localScale = scale;
    foreach (var light in obj.GetComponentsInChildren<Light>(true)) { light.shadows = LightShadows.None; light.enabled = false; }
    return obj;
};
System.Func<string, float, float, float, float, GameObject> prop = (name, x, z, yaw, scale) => place(kit + name + ".prefab", new Vector3(x, 10, z), yaw, Vector3.one * scale);
System.Action<GameObject> record = obj => {
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(obj);
};
var atlas = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/WorldCities/CityAtlas.mat");
System.Func<string, Color, Material> material = (name, color) => {
    var mat = new Material(atlas); mat.name = name; mat.color = color;
    UnityEditor.AssetDatabase.CreateAsset(mat, directory + "/" + name + ".mat"); return mat;
};
var pavingMat = material("CitadelPaving", new Color(.56f, .63f, .66f));
var stoneMat = material("CitadelStone", new Color(.77f, .82f, .84f));
var ochreMat = material("CitadelOchre", new Color(1f, .84f, .66f));
var sageMat = material("CitadelSage", new Color(.79f, .9f, .82f));
System.Action<GameObject, Material> tint = (obj, mat) => {
    foreach (var renderer in obj.GetComponentsInChildren<Renderer>()) {
        var mats = renderer.sharedMaterials;
        for (int i=0;i<mats.Length;i++) if (mats[i] == atlas) mats[i] = mat;
        renderer.sharedMaterials = mats;
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    }
};
// 기존 인스턴스를 이동해 연결 GUID와 사용자 배치를 보존한다.
var houses = environment.transform.Cast<Transform>().Where(t => t.name == "CastleTownHouse" && t.position.z > 120).OrderBy(t => t.position.x).ThenBy(t => t.position.z).ToArray();
var housePos = new [] {new Vector3(156,10,144),new Vector3(151,10,166),new Vector3(146,10,195),new Vector3(158,10,218),new Vector3(204,10,217),new Vector3(205,10,140),new Vector3(211,10,161),new Vector3(216,10,191)};
var houseYaw = new [] {84f,96f,76f,171f,188f,268f,258f,278f};
if (houses.Length != housePos.Length) throw new System.Exception("기존 주택 배치 수가 달라졌습니다.");
for (int i=0;i<houses.Length;i++) {
    houses[i].position=housePos[i]; houses[i].rotation=Quaternion.Euler(0,houseYaw[i],0);
    houses[i].localScale=new Vector3(1.1f, i%3==0?1.18f:1f, 1.1f);
    tint(houses[i].gameObject,i%2==0?ochreMat:sageMat);record(houses[i].gameObject);
}
foreach (Transform child in environment.transform) if (child.name.StartsWith("Castle") && child.name != "CastleTownHouse") tint(child.gameObject,stoneMat);
var stalls = environment.transform.Cast<Transform>().Where(t=>t.name=="MarketStall" && t.position.z>120).OrderBy(t=>t.position.x).ThenBy(t=>t.position.z).ToArray();
var stallPos = new [] { new Vector3(169,10,148),new Vector3(168,10,162),new Vector3(191,10,153),new Vector3(195,10,169) };
for(int i=0;i<stalls.Length;i++){stalls[i].position=stallPos[i];stalls[i].rotation=Quaternion.Euler(0,i<2?270:90,0);stalls[i].localScale=Vector3.one;record(stalls[i].gameObject);}
var fountain = environment.transform.Cast<Transform>().First(t=>t.name=="TownFountain");
fountain.position=new Vector3(182,10,191);fountain.localScale=Vector3.one*1.25f;tint(fountain.gameObject,stoneMat);record(fountain.gameObject);
var details=environment.transform.Find("CityDetails");
foreach(Transform detail in details) {
    if(detail.name.StartsWith("CityPaving")) {detail.gameObject.SetActive(false);record(detail.gameObject);}
    if(detail.name.StartsWith("Broadleaf") && detail.position.x<160 && detail.position.z<175){detail.position=new Vector3(137,10,156);record(detail.gameObject);}
    if(detail.name.StartsWith("TownBench")){detail.position=new Vector3(detail.position.x<180?170:194,10,192);record(detail.gameObject);}
}
// 넓은 십자 바닥을 거리·분수광장·공방 앞마당으로 나눈다.
System.Action<float,float,float,float> paving = (x,z,width,depth) => {
    var obj=place(kit+"CityPaving.prefab",new Vector3(x,10.035f,z),0,new Vector3(width/12,1,depth/12));tint(obj,pavingMat);
};
for(int z=126;z<=174;z+=12)paving(180,z,8,12);
for(int z=210;z<=222;z+=12)paving(180,z,8,12);
for(int x=156;x<=216;x+=12)paving(x,180,12,6);
for(int x=168;x<=192;x+=12)paving(x,216,12,6);
for(int x=168;x<=192;x+=6)for(int z=180;z<=204;z+=6)if(Vector2.Distance(new Vector2(x,z),new Vector2(182,191))<14)paving(x,z,6,6);
paving(168,155,8,25);paving(193,162,8,27);paving(218,218,18,14);
// 골목과 식재의 바닥색을 연결하고 건물 사이를 잔디 마당으로 돌린다.
var data=terrain.terrainData;
UnityEditor.Undo.RegisterCompleteObjectUndo(data,"돌등성 길 도색");
int resolution=data.alphamapResolution, layers=data.terrainLayers.Length;
var paint=data.GetAlphamaps(0,0,resolution,resolution);
for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++){
    float wx=x*data.size.x/(resolution-1),wz=z*data.size.z/(resolution-1);
    if(wx<125||wx>235||wz<128||wz>234)continue;
    float street=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(4,7,Mathf.Abs(wx-180)));
    float cross=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(3,6,Mathf.Abs(wz-180)));
    float plaza=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(13,17,Vector2.Distance(new Vector2(wx,wz),new Vector2(182,191))));
    float market=wx>162&&wx<201&&wz<176?.88f:0;
    float yard=wx>208&&wz>209?.8f:0;
    float dirt=Mathf.Max(street,cross,plaza,market,yard);
    paint[z,x,0]=1-dirt;paint[z,x,1]=dirt;
    for(int l=2;l<layers;l++)paint[z,x,l]=0;
}
data.SetAlphamaps(0,0,paint);UnityEditor.EditorUtility.SetDirty(data);
System.Func<string,float,float,float,float,GameObject> plant = (name,x,z,yaw,scale) => {
    var obj=place(nature+name+".prefab",new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z)),z),yaw,Vector3.one*scale);
    foreach(var collider in obj.GetComponentsInChildren<Collider>()) collider.gameObject.layer=ground;
    return obj;
};
var trees=new[]{new Vector3(159,0,184),new Vector3(203,0,187),new Vector3(158,0,202),new Vector3(205,0,204),new Vector3(134,0,143),new Vector3(230,0,145),new Vector3(134,0,181),new Vector3(231,0,174),new Vector3(145,0,235),new Vector3(216,0,240),new Vector3(158,0,132),new Vector3(201,0,128)};
for(int i=0;i<trees.Length;i++)plant("BroadleafTree_02_Green",trees[i].x,trees[i].z,i*43,.9f+(i%3)*.15f);
var beds=new[]{new Vector3(160,0,188),new Vector3(202,0,190),new Vector3(159,0,201),new Vector3(204,0,201),new Vector3(140,0,154),new Vector3(224,0,180),new Vector3(152,0,133),new Vector3(209,0,131)};
var random=new System.Random(10424);
foreach(var bed in beds){
    for(int i=0;i<9;i++){
        float x=bed.x+(float)random.NextDouble()*6-3,z=bed.z+(float)random.NextDouble()*5-2.5f;
        plant(i%3==0?"Bush_01_01":"FlowerMeadow_White",x,z,i*39,i%3==0?.7f:1.3f);
    }
}
// 외벽 너머 수목과 암봉으로 반복되는 민둥 능선을 가린다.
for(int i=0;i<34;i++){
    float x=i%2==0?98+(i%3)*5:257+(i%3)*7,z=98+i*4.7f;
    plant("BroadleafTree_01_Green",x,z,i*51,1.3f+(i%3)*.3f);
}
for(int i=0;i<10;i++)plant("Rock_Big_01",125+i*13,275+(i%3)*8,i*29,2.8f+(i%3)*.4f);
string props="Assets/ExternalAssets/Environment/Fantastic Dungeon Pack/prefabs/PROPS/";
place(props+"misc/P_PROP_cart_dungeon_01.prefab",new Vector3(214,10,223),20,Vector3.one);
place(props+"misc/P_PROP_cart_barrel_dungeon.prefab",new Vector3(156,10,154),-5,Vector3.one);
place(props+"furniture/P_PROP_table_dungeon_01.prefab",new Vector3(198,10,196),90,Vector3.one);
string farm="Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/Props/";
place(farm+"CraftingTable/PF_Farm_CraftingTable_Carpentry_01.prefab",new Vector3(216,10,214),180,Vector3.one);
prop("TownBench",166,198,135,1);prop("TownBench",200,198,225,1);prop("TownBench",171,207,180,1);
foreach(var p in new[]{new Vector3(163,10,145),new Vector3(164,10,166),new Vector3(197,10,149),new Vector3(199,10,169),new Vector3(217,10,221),new Vector3(220,10,213),new Vector3(138,10,218)}){
    prop("CargoCrate",p.x,p.z,17,1);prop("CargoBarrel",p.x+1.6f,p.z+.6f,0,1);
}
string east="Assets/ExternalAssets/Environment/PolyartStudio/DreamscapeEastLands/Prefabs/Props/";
foreach(var p in new[]{new Vector3(173,10,132),new Vector3(187,10,132),new Vector3(173,10,173),new Vector3(187,10,175),new Vector3(171,10,211),new Vector3(190,10,212)}){
    place(east+"Lamps/PF_EastLands_Lamp_Ground_01.prefab",p,0,Vector3.one);
}
foreach(var p in new[]{new Vector3(170,10,126),new Vector3(190,10,126),new Vector3(168,10,224),new Vector3(193,10,224)})place(east+"Banners/PF_EastLands_Banner_01.prefab",p,180,Vector3.one);
var sun=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Light>()).First(l=>l.type==LightType.Directional);
UnityEditor.Undo.RecordObject(sun,"돌등성 햇빛");UnityEditor.Undo.RecordObject(sun.transform,"돌등성 햇빛 방향");
sun.transform.rotation=Quaternion.Euler(34,-42,0);sun.intensity=1.05f;sun.color=new Color(1,.91f,.78f);sun.shadows=LightShadows.Soft;
RenderSettings.ambientSkyColor=new Color(.48f,.59f,.72f);RenderSettings.ambientEquatorColor=new Color(.39f,.43f,.47f);RenderSettings.ambientGroundColor=new Color(.23f,.26f,.22f);
RenderSettings.fogStartDistance=135;RenderSettings.fogEndDistance=410;RenderSettings.fogColor=new Color(.65f,.73f,.76f);
UnityEditor.AssetDatabase.SaveAssetIfDirty(data);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return "돌등성 배치·식재·포장·조명 반영, 저장 전 화면 확인 필요";

} finally { UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousScene); }