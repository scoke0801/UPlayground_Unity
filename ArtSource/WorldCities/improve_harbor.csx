// 일회성 씬 저작 기록. 재실행은 거부하며, 이후 배치의 원본은 저장된 씬이다.
var scene = UnityEngine.SceneManagement.SceneManager.GetSceneByName("SaltwindHarbor");
if (!scene.IsValid() || UnityEditor.EditorApplication.isPlaying || UnityEditor.EditorApplication.isCompiling)
    throw new System.Exception("소금바람항을 Edit Mode에서 여세요.");
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
var environment = scene.GetRootGameObjects().First(g => g.name == "CityEnvironment");
if (environment.transform.Find("HarborLife") != null) throw new System.Exception("이미 적용된 배치를 보존합니다.");
string directory = "Assets/10.Datas/World/WorldCities/SaltwindHarbor/Life";
System.IO.Directory.CreateDirectory(directory);
UnityEditor.AssetDatabase.Refresh();
var root = new GameObject("HarborLife").transform;
root.SetParent(environment.transform);
int ground = LayerMask.NameToLayer("Ground");
System.Func<string,string> prefabPath = name => {
    var paths = UnityEditor.AssetDatabase.FindAssets(name + " t:Prefab").Select(UnityEditor.AssetDatabase.GUIDToAssetPath)
        .Where(p => System.IO.Path.GetFileNameWithoutExtension(p) == name).ToArray();
    if (paths.Length != 1) throw new System.Exception("프리팹 식별 실패: " + name);
    return paths[0];
};
System.Func<string,Vector3,float,float,GameObject> place = (name,pos,yaw,scale) => {
    var obj = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath(name)),scene);
    obj.transform.SetParent(root); obj.transform.position=pos; obj.transform.rotation=Quaternion.Euler(0,yaw,0); obj.transform.localScale=Vector3.one*scale;
    foreach(var t in obj.GetComponentsInChildren<Transform>(true)) {t.gameObject.layer=ground; t.gameObject.isStatic=true;}
    return obj;
};
System.Func<string,Color,Material> linen = (name,color) => {
    var source=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/Village/MarketLinen.mat");
    var mat=new Material(source);mat.name=name;mat.color=color;mat.SetFloat("_Cull",0);UnityEditor.AssetDatabase.CreateAsset(mat,directory+"/"+name+".mat");return mat;
};
var blue=linen("HarborBlue",new Color(.22f,.49f,.56f));
var rust=linen("HarborRust",new Color(.7f,.30f,.19f));
var ochre=linen("HarborOchre",new Color(.88f,.66f,.31f));
var cream=linen("HarborCream",new Color(.9f,.87f,.71f));
var colors=new[]{blue,rust,ochre,cream};
var oak=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/Village/VillageOak.mat");
System.Func<string,Vector3,Vector3,Material,GameObject> block=(name,pos,size,mat)=>{
    var g=GameObject.CreatePrimitive(PrimitiveType.Cube);g.name=name;g.transform.SetParent(root);g.transform.position=pos;g.transform.localScale=size;
    g.GetComponent<Renderer>().sharedMaterial=mat;g.layer=ground;g.isStatic=true;return g;
};
// 건물 사이의 빈 광장을 10m 안팎의 가로와 두 개의 작은 마당으로 바꾼다.
var houses=environment.transform.Cast<Transform>().Where(t=>t.name=="HarborHouse").ToArray();
var positions=new[]{new Vector3(160,10,149),new Vector3(158,10,169),new Vector3(157,10,190),new Vector3(168,10,132),new Vector3(140,10,150),new Vector3(196,10,137),new Vector3(201,10,155),new Vector3(203,10,176),new Vector3(209,10,195),new Vector3(137,10,176),new Vector3(136,10,201)};
var angles=new[]{85f,95f,80f,165f,170f,195f,265f,275f,260f,85f,170f};
for(int i=0;i<houses.Length;i++){
    houses[i].position=positions[i];houses[i].rotation=Quaternion.Euler(0,angles[i],0);
    houses[i].localScale=new Vector3(1.08f,1f+(i%3)*.12f,1.05f);
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(houses[i]);
}
var details=environment.transform.Find("CityDetails");
foreach(var p in details.Cast<Transform>().Where(t=>t.name=="CityPaving").ToArray()) p.gameObject.SetActive(false);
foreach(var p in environment.transform.Cast<Transform>().Where(t=>t.name=="MarketStall")) p.gameObject.SetActive(false);
var fountain=environment.transform.Find("TownFountain");fountain.position=new Vector3(187,10,193);fountain.localScale=Vector3.one*.8f;
UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(fountain);
// 보행축, 작은 시장 마당, 부두길을 구분한다. 포장 메시에는 충돌이 없어 지형을 그대로 걷는다.
for(int z=132;z<=216;z+=12){var p=place("CityPaving",new Vector3(180,10.045f,z),0,1);p.transform.localScale=new Vector3(.75f,1,1);}
foreach(int z in new[]{156,168,180,192}){var p=place("CityPaving",new Vector3(180,10.048f,z),0,1);p.transform.localScale=new Vector3(2.15f,1,1);}
for(int x=120;x<=228;x+=12){var p=place("CityPaving",new Vector3(x,10.05f,228),0,1);p.transform.localScale=new Vector3(1,1,.65f);}
for(int x=132;x<=168;x+=12){var p=place("CityPaving",new Vector3(x,10.045f,133),0,1);p.transform.localScale=new Vector3(1,1,.45f);}
var stalls=new[]{new Vector3(171,10,155),new Vector3(190,10,164),new Vector3(170,10,175),new Vector3(196,10,186)};
for(int i=0;i<stalls.Length;i++){
    var stall=place("VillageMarketStall",stalls[i],i%2==0?90:270,1.15f);stall.name="Market_"+i;
    foreach(var r in stall.GetComponentsInChildren<Renderer>()){
        var mats=r.sharedMaterials;for(int j=0;j<mats.Length;j++)if(mats[j].name.Contains("Linen"))mats[j]=colors[i];r.sharedMaterials=mats;
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(r);
    }
}
// 기존 리넨 재질의 천 차양으로 목조 건물과 시장의 색을 구별한다.
var cloth=new Mesh();cloth.name="HarborAwning";
cloth.vertices=new[]{new Vector3(-1,0,-1),new Vector3(1,0,-1),new Vector3(-1,-.15f,0),new Vector3(1,-.15f,0),new Vector3(-1,-.45f,1),new Vector3(1,-.45f,1)};
cloth.uv=new[]{new Vector2(0,0),new Vector2(1,0),new Vector2(0,.5f),new Vector2(1,.5f),new Vector2(0,1),new Vector2(1,1)};
cloth.triangles=new[]{0,2,1,1,2,3,2,4,3,3,4,5};cloth.RecalculateNormals();cloth.RecalculateBounds();
UnityEditor.AssetDatabase.CreateAsset(cloth,directory+"/Awning.asset");
System.Action<string,Vector3,Vector3,float,Material> canopy=(name,pos,size,yaw,mat)=>{
    var g=new GameObject(name);g.transform.SetParent(root);g.transform.position=pos;g.transform.rotation=Quaternion.Euler(0,yaw,0);g.transform.localScale=size;
    g.AddComponent<MeshFilter>().sharedMesh=cloth;g.AddComponent<MeshRenderer>().sharedMaterial=mat;g.isStatic=true;
};
canopy("BlueCloth_Market",new Vector3(170,13.6f,164),new Vector3(3.5f,1,2.4f),90,blue);
canopy("RustCloth_Market",new Vector3(191,13.7f,176),new Vector3(3.5f,1,2.2f),270,rust);
canopy("OchreCloth_Workshop",new Vector3(146,13.5f,220),new Vector3(4,1,2),180,ochre);
foreach(var pos in new[]{new Vector3(172.2f,11.6f,160.5f),new Vector3(172.2f,11.6f,167.5f),new Vector3(188.8f,11.6f,172.5f),new Vector3(188.8f,11.6f,179.5f)})block("AwningPost",pos,new Vector3(.13f,3.2f,.13f),oak);
// 머리 위의 작은 삼각 천으로 시장 방향을 읽게 하되 시야 중심은 비운다.
var pennant=new Mesh();pennant.name="HarborPennant";pennant.vertices=new[]{new Vector3(-.3f,0,0),new Vector3(.3f,0,0),new Vector3(0,-.85f,0)};pennant.uv=new[]{Vector2.zero,Vector2.right,Vector2.up};pennant.triangles=new[]{0,1,2};pennant.RecalculateNormals();UnityEditor.AssetDatabase.CreateAsset(pennant,directory+"/Pennant.asset");
foreach(int z in new[]{147,171,202}){
    var a=new Vector3(165,16,z);var b=new Vector3(198,16,z+3);
    var cord=block("MarketLine",(a+b)*.5f,new Vector3(.035f,.035f,Vector3.Distance(a,b)),oak);cord.transform.rotation=Quaternion.LookRotation(b-a);UnityEngine.Object.DestroyImmediate(cord.GetComponent<Collider>());
    for(int i=0;i<23;i++){var g=new GameObject("MarketPennant");g.transform.SetParent(root);g.transform.position=Vector3.Lerp(a,b,(i+.5f)/23);g.transform.rotation=Quaternion.Euler(0,-5,0);g.AddComponent<MeshFilter>().sharedMesh=pennant;g.AddComponent<MeshRenderer>().sharedMaterial=colors[i%4];g.isStatic=true;}
}
// 물건을 길 가장자리에 모아 작업 자리와 수송 통로를 구별한다.
foreach(var pos in new[]{new Vector3(167,10,158),new Vector3(195,10,166),new Vector3(165,10,178),new Vector3(199,10,189),new Vector3(149,10,222),new Vector3(205,10,224),new Vector3(160,10,229)}){
    place("CargoCrate",pos,12,1);place("CargoCrate",pos+new Vector3(1.5f,0,.2f),-8,.85f);place("CargoBarrel",pos+new Vector3(-1.3f,0,.4f),0,1);
    place("PF_Farm_Basket_01",pos+new Vector3(0,1.3f,0),0,1);
}
place("PF_Farm_Cart_01",new Vector3(164,10,207),20,1.3f);
place("PF_Carriage_Crates",new Vector3(206,10,205),-20,.8f);
foreach(var pos in new[]{new Vector3(168,10,151),new Vector3(194,10,159),new Vector3(167,10,182),new Vector3(200,10,190),new Vector3(162,10,141),new Vector3(198,10,145),new Vector3(184,10,198),new Vector3(123,10,229)}){
    place("PF_Farm_FlowersPot_A_01",pos,20,1.5f);place("PF_Farm_BasilPot_01",pos+new Vector3(.8f,0,.4f),-15,1.3f);
}
foreach(var pos in new[]{new Vector3(164,10,184),new Vector3(200,10,148),new Vector3(193,10,198),new Vector3(126,10,218)}) place("BroadleafTree_02_Green",pos,25,.85f);
place("TownBench",new Vector3(125,10,228),180,1);
place("TownBench",new Vector3(134,10,228),180,1);
place("TownBench",new Vector3(183,10,200),180,1);
place("PF_Farm_WoodTable_01",new Vector3(145,10,220),0,1.4f);
place("PF_Farm_WoodTable_01",new Vector3(199,10,225),0,1.2f);
place("PF_Fried_Fish",new Vector3(199,11,225),0,1.4f);
place("PF_Farm_Basket_01",new Vector3(196.7f,10.25f,244),0,1.2f);
// 기존 선박·등대 실루엣을 살리고 방파제 바깥에 해안 암반을 둔다.
place("CoastalBoat",new Vector3(184,5.2f,266),-18,1.65f);
for(int i=0;i<9;i++)place("Rock_Big_01",new Vector3(106-i%2*4,5.5f,234+i*5),i*37,1.5f+i%3*.3f);
var stone=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/WorldCities/SaltwindHarbor/LandscapeStone.mat");
foreach(var t in environment.transform.Cast<Transform>().Where(t=>t.name=="Quayside"||t.name.Contains("Parapet")||t.name=="WestBreakwater")){
    var r=t.GetComponent<Renderer>();if(r!=null)r.sharedMaterial=stone;
}
// 넓고 균일한 흙 사각형을 지우고 가로 주변에 마른 풀과 흙을 섞는다.
var terrain=environment.GetComponentInChildren<Terrain>();var td=terrain.terrainData;int res=td.alphamapResolution;int count=td.terrainLayers.Length;
var paint=td.GetAlphamaps(0,0,res,res);
for(int z=0;z<res;z++)for(int x=0;x<res;x++){
    float wx=x*360f/(res-1),wz=z*360f/(res-1);if(wx<120||wx>238||wz<122||wz>224)continue;
    float axis=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(6,14,Mathf.Abs(wx-180)));
    float dirt=Mathf.Max(axis,.28f+.25f*Mathf.PerlinNoise(wx*.16f,wz*.16f));
    for(int l=0;l<count;l++)paint[z,x,l]=0;paint[z,x,0]=1-dirt;paint[z,x,1]=dirt;
}
td.SetAlphamaps(0,0,paint);UnityEditor.EditorUtility.SetDirty(td);UnityEditor.AssetDatabase.SaveAssetIfDirty(td);
var sun=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<Light>()).First(l=>l.type==LightType.Directional);
sun.transform.rotation=Quaternion.Euler(36,-42,0);sun.color=new Color(1,.92f,.8f);sun.intensity=1.15f;
RenderSettings.ambientSkyColor=new Color(.53f,.67f,.78f);RenderSettings.ambientEquatorColor=new Color(.46f,.5f,.52f);RenderSettings.ambientGroundColor=new Color(.28f,.29f,.26f);
RenderSettings.fogStartDistance=160;RenderSettings.fogEndDistance=600;RenderSettings.fogColor=new Color(.67f,.8f,.83f);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return "시장 가로·생활 소품·부두 실루엣 적용";
