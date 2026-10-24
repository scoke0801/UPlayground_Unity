string cityId="CITY_ID";
bool harbor=cityId=="SaltwindHarbor";
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName(cityId);
if(!scene.IsValid())scene=UnityEditor.SceneManagement.EditorSceneManager.OpenScene("Assets/01.Scenes/Scenario/"+cityId+".unity",UnityEditor.SceneManagement.OpenSceneMode.Single);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
var environment=scene.GetRootGameObjects().First(r=>r.name=="CityEnvironment");
var terrain=environment.GetComponentInChildren<Terrain>();
var data=terrain.terrainData;
int resolution=data.alphamapResolution,layers=data.terrainLayers.Length;
float[,,] paint=new float[resolution,resolution,layers];
for(int z=0;z<resolution;z++)for(int x=0;x<resolution;x++){
    float wx=x*360f/(resolution-1),wz=z*360f/(resolution-1);
    float roadX=180+Mathf.Sin((wz-35)*.045f)*8*Mathf.Clamp01((120-wz)/45);
    float road=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(5,8,Mathf.Abs(wx-roadX)));
    if(wz<24||wz>275)road=0;
    bool yard=wx>126&&wx<234&&wz>135&&wz<225;
    float dirt=yard?.8f:road;
    float rock=Mathf.Clamp01((data.GetInterpolatedHeight(wx/360,wz/360)-22)/18);
    paint[z,x,0]=(1-dirt)*(1-rock);paint[z,x,1]=dirt*(1-rock);paint[z,x,2]=rock;
}
data.SetAlphamaps(0,0,paint);UnityEditor.EditorUtility.SetDirty(data);
var details=environment.transform.Find("CityDetails");
if(details!=null){UnityEditor.AssetDatabase.SaveAssetIfDirty(data);return "지형 재도색 완료, 기존 세부 배치 보존";}
var root=new GameObject("CityDetails");root.transform.SetParent(environment.transform);
System.Func<string,Vector3,float,float,GameObject> place=(model,pos,yaw,scale)=>{
    var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/WorldCities/"+model+".prefab");
    var obj=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,scene);obj.transform.SetParent(root.transform);obj.transform.position=pos;obj.transform.rotation=Quaternion.Euler(0,yaw,0);obj.transform.localScale=Vector3.one*scale;return obj;
};
var paved=new System.Collections.Generic.HashSet<string>();
System.Action<int,int> paving=(x,z)=>{if(paved.Add(x+","+z))place("CityPaving",new Vector3(x,10.035f,z),0,1);};
for(int z=harbor?132:120;z<=228;z+=12)paving(180,z);
for(int x=132;x<=228;x+=12)paving(x,180);
foreach(int x in new[]{168,192})foreach(int z in new[]{168,180,192})paving(x,z);
if(harbor){
    var obstruction=environment.transform.Cast<Transform>().FirstOrDefault(t=>Mathf.Abs(t.position.x-178)<.1f&&Mathf.Abs(t.position.z-143)<.1f);
    if(obstruction!=null)obstruction.position=new Vector3(111,10,154);
    place("HarborLighthouse",new Vector3(116,10,265),0,1);
    place("HarborHouse",new Vector3(118,10,137),110,.85f);
    place("HarborHouse",new Vector3(239,10,142),215,.88f);
    place("HarborHouse",new Vector3(238,10,192),260,.9f);
    for(int x=132;x<=228;x+=12)paving(x,228);
}
foreach(var pos in new[]{new Vector3(151,10,200),new Vector3(155,10,204),new Vector3(208,10,204),new Vector3(211,10,207),new Vector3(143,10,210),new Vector3(214,10,155)}){
    place("CargoCrate",pos,15,1);place("CargoBarrel",pos+new Vector3(1.7f,0,.4f),0,1);
}
place("TownBench",new Vector3(171,10,192),90,1);place("TownBench",new Vector3(189,10,192),270,1);
string nature="Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/";
System.Action<string,Vector3,float> naturePlace=(name,pos,scale)=>{
    var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(nature+name+".prefab");if(prefab==null)return;
    var obj=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,scene);obj.transform.SetParent(root.transform);obj.transform.position=pos;obj.transform.localScale=Vector3.one*scale;
};
foreach(var pos in new[]{new Vector3(152,10,162),new Vector3(208,10,191),new Vector3(126,10,158),new Vector3(232,10,210)})naturePlace("BroadleafTree_02_Green",pos,.72f);
for(int i=0;i<14;i++){
    float x=i%2==0?90:270,z=45+i*14;
    if(harbor&&z>230)break;
    naturePlace("Rock_Big_01",new Vector3(x,terrain.SampleHeight(new Vector3(x,0,z))-2,z),2.2f+i%3*.3f);
}
if(harbor){
    // 물목의 얕은 물과 갈대를 길 양옆에 두고 보행 중심선은 유지한다.
    var sea=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/WorldCities/SaltwindHarbor/Sea.mat");
    float[,] heights=data.GetHeights(0,0,data.heightmapResolution,data.heightmapResolution);
    for(int z=0;z<data.heightmapResolution;z++)for(int x=0;x<data.heightmapResolution;x++){
        float wx=x*360f/(data.heightmapResolution-1),wz=z*360f/(data.heightmapResolution-1);
        if(wz>48&&wz<112){float distance=Mathf.Min(Mathf.Abs(wx-151),Mathf.Abs(wx-211));float blend=1-Mathf.Clamp01((distance-4)/4);heights[z,x]=Mathf.Lerp(heights[z,x],8.5f/65,blend);}
    }
    data.SetHeights(0,0,heights);
    foreach(int x in new[]{151,211}){
        var water=GameObject.CreatePrimitive(PrimitiveType.Plane);water.name="ReedWater";water.transform.SetParent(root.transform);water.transform.position=new Vector3(x,9,80);water.transform.localScale=new Vector3(1.25f,1,6.4f);UnityEngine.Object.DestroyImmediate(water.GetComponent<Collider>());water.GetComponent<Renderer>().sharedMaterial=sea;
        for(int z=50;z<=110;z+=4){naturePlace("Reeds_01",new Vector3(x-5,9.2f,z),1.1f);naturePlace("Cattail_02",new Vector3(x+5,9.2f,z+1),1.1f);}
    }
}
UnityEditor.EditorUtility.SetDirty(data);UnityEditor.AssetDatabase.SaveAssetIfDirty(data);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
return cityId+": 길·포장·작업 소품·주변 지형 보완 완료";
