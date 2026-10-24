var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("StonegateCitadel");
if(!scene.IsValid()||Application.isPlaying)throw new System.Exception("돌등성 Edit Mode가 필요합니다.");
var environment=scene.GetRootGameObjects().First(r=>r.name=="CityEnvironment");
var life=environment.transform.Find("CitadelLife");
if(life.Find("MarketFinish")!=null)throw new System.Exception("세부 배치가 이미 있습니다.");
var root=new GameObject("MarketFinish").transform;root.SetParent(life);
System.Func<string,Vector3,float,float,GameObject> place=(path,position,yaw,scale)=>{
    var prefab=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
    if(prefab==null)throw new System.Exception(path);
    var obj=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab,scene);obj.transform.SetParent(root);
    obj.transform.position=position;obj.transform.rotation=Quaternion.Euler(0,yaw,0);obj.transform.localScale=Vector3.one*scale;
    foreach(var light in obj.GetComponentsInChildren<Light>(true)){light.enabled=false;light.shadows=LightShadows.None;}
    return obj;
};
System.Action<GameObject> record=obj=>{UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(obj);};
// 계산대와 벤치를 사람 체격에 맞추고, 손님이 서는 쪽은 비워 둔다.
var stalls=environment.transform.Cast<Transform>().Where(t=>t.name=="MarketStall"&&t.position.z>120).ToArray();
foreach(var stall in stalls){stall.localScale=Vector3.one*.72f;record(stall.gameObject);}
foreach(var bench in environment.GetComponentsInChildren<Transform>().Where(t=>t.name=="TownBench")){bench.localScale=Vector3.one*.72f;record(bench.gameObject);}
string nature="Assets/ExternalAssets/Environment/Idyllic Fantasy Nature/Prefabs/";
foreach(var p in new[]{new Vector3(173,10,141),new Vector3(187,10,160),new Vector3(173,10,172),new Vector3(198,10,181)})place(nature+"BroadleafTree_02_Green.prefab",p,p.z,1.45f);
foreach(Transform tree in life)if(tree.name=="BroadleafTree_02_Green"&&tree.position.z>180&&tree.position.z<209){tree.localScale*=1.25f;record(tree.gameObject);}
string farm="Assets/ExternalAssets/Environment/PolyartStudio/FarmLand/PolyartStudio/Farmlands/Prefabs/";
foreach(var p in new[]{new Vector3(161,10,147),new Vector3(204,10,151),new Vector3(214,10,217)}){
    var canopy=place(farm+"BuildingModules/Decoration/PF_Farm_Canopy_01.prefab",p,90,1);
    var rs=canopy.GetComponentsInChildren<Renderer>();var b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
    canopy.transform.localScale*=4.5f/Mathf.Max(b.size.x,b.size.z);
    rs=canopy.GetComponentsInChildren<Renderer>();b=rs[0].bounds;foreach(var r in rs)b.Encapsulate(r.bounds);
    canopy.transform.position+=Vector3.up*(10-b.min.y);
}
string golden="Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Props/";
foreach(var stall in stalls){
    for(int i=-1;i<=1;i++){
        var pos=stall.TransformPoint(new Vector3(i*1.35f,2.3f,-1.1f));
        place(golden+"Food/TGV_Bread_01A.prefab",pos,stall.eulerAngles.y,1);
    }
    var basket=place(farm+"Props/FarmTools/PF_Farm_Basket_01.prefab",stall.position+stall.right*2,0,1);
}
// 성의 깃발은 문 옆 벽면에 걸어 멀리서도 성문과 본성의 방향을 읽게 한다.
var banners=life.Cast<Transform>().Where(t=>t.name.Contains("Banner")).ToArray();
var positions=new[]{new Vector3(169,15,114.8f),new Vector3(191,15,114.8f),new Vector3(172,20,226.1f),new Vector3(188,20,226.1f)};
for(int i=0;i<banners.Length;i++){banners[i].position=positions[i];banners[i].localScale=Vector3.one*1.7f;banners[i].rotation=Quaternion.identity;record(banners[i].gameObject);}
foreach(var p in new[]{new Vector3(174,10,146),new Vector3(187,10,172),new Vector3(171,10,204),new Vector3(208,10,215)})place(golden+"Exterior Props/TGV_Lamp_Post_01A.prefab",p,0,1);
var sourceMat=UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/10.Datas/World/WorldCities/StonegateCitadel/CitadelStone.mat");
var water=new Material(sourceMat);water.name="CitadelFountainWater";water.mainTexture=null;water.color=new Color(.15f,.43f,.47f);water.SetFloat("_Smoothness",.65f);
UnityEditor.AssetDatabase.CreateAsset(water,"Assets/10.Datas/World/WorldCities/StonegateCitadel/CitadelFountainWater.mat");
var basin=GameObject.CreatePrimitive(PrimitiveType.Cylinder);basin.name="FountainWater";basin.transform.SetParent(root);basin.transform.position=new Vector3(182,11.015f,191);basin.transform.localScale=new Vector3(7.1f,.012f,7.1f);UnityEngine.Object.DestroyImmediate(basin.GetComponent<Collider>());basin.GetComponent<Renderer>().sharedMaterial=water;
var region=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.UI.MapRegionInfoSO>("Assets/10.Datas/World/WorldCities/StonegateCitadel/RegionInfo.asset");
region.description="고갯길을 오가는 사람들이 쉬어 가는 성. 성문 안 왼쪽 장터에서는 여행용품을 팔고, 오른쪽 안쪽 공방에서는 모아 온 재료를 산다. 분수 뒤 벤치에서 일행과 잠시 쉬어 갈 수 있다.";
UnityEditor.EditorUtility.SetDirty(region);UnityEditor.AssetDatabase.SaveAssetIfDirty(region);
UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
return "사람 크기에 맞춘 장터와 수관·차양·깃발·분수 세부 반영";
