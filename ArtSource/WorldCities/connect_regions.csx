// 기존 지역 ID와 저장 계약을 유지하며 새 지역만 등록한다.
var database=UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/10.Datas/Map/MapConfigDatabase.asset");
var sourceConfig=UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>("Assets/10.Datas/World/ScenarioMaps/LakeOfLifeScenario/MinimapConfig.asset");
var db=new UnityEditor.SerializedObject(database);
var entries=db.FindProperty("_entries");
var sceneNames=new[]{"SaltwindHarbor","StonegateCitadel"};
var labels=new[]{"소금바람항","돌등성"};
for(int n=0;n<sceneNames.Length;n++){
    string id=sceneNames[n],directory="Assets/10.Datas/World/WorldCities/"+id;
    var config=UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(directory+"/MinimapConfig.asset");
    if(config==null){
        config=UnityEngine.Object.Instantiate(sourceConfig);config.name="MinimapConfig";
        var so=new UnityEditor.SerializedObject(config);
        so.FindProperty("backgroundSprite").objectReferenceValue=null;
        so.FindProperty("captureCenter").vector2Value=new Vector2(180,180);
        so.FindProperty("captureWorldSize").floatValue=360;
        so.FindProperty("captureWorldSizeXY").vector2Value=new Vector2(360,360);
        so.FindProperty("mapZoom").floatValue=4;
        so.FindProperty("flipX").boolValue=false;so.FindProperty("flipY").boolValue=false;so.FindProperty("rotationDegrees").floatValue=0;
        so.ApplyModifiedPropertiesWithoutUndo();UnityEditor.AssetDatabase.CreateAsset(config,directory+"/MinimapConfig.asset");
    }
    var region=UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(directory+"/RegionInfo.asset");
    if(region==null){
        region=ScriptableObject.CreateInstance("MapRegionInfoSO");region.name="RegionInfo";
        var so=new UnityEditor.SerializedObject(region);so.FindProperty("continentName").stringValue=labels[n];
        so.FindProperty("description").stringValue=n==0?"물길을 따라 모인 짐과 바다로 나서는 배가 만나는 항구.":"농경지와 고갯길을 오가는 사람들이 성문 아래 모이는 도시.";
        so.FindProperty("recommendedLevelMin").intValue=1;so.FindProperty("recommendedLevelMax").intValue=30;
        so.ApplyModifiedPropertiesWithoutUndo();UnityEditor.AssetDatabase.CreateAsset(region,directory+"/RegionInfo.asset");
    }
    bool exists=false;
    for(int i=0;i<entries.arraySize;i++)if(entries.GetArrayElementAtIndex(i).FindPropertyRelative("mapId").stringValue==id)exists=true;
    if(!exists){int i=entries.arraySize;entries.InsertArrayElementAtIndex(i);var e=entries.GetArrayElementAtIndex(i);e.FindPropertyRelative("mapId").stringValue=id;e.FindPropertyRelative("config").objectReferenceValue=config;e.FindPropertyRelative("regionInfo").objectReferenceValue=region;}
}
db.ApplyModifiedPropertiesWithoutUndo();UnityEditor.AssetDatabase.SaveAssetIfDirty(database);
var buildScenes=new System.Collections.Generic.List<UnityEditor.EditorBuildSettingsScene>(UnityEditor.EditorBuildSettings.scenes);
foreach(string id in sceneNames){string path="Assets/01.Scenes/Scenario/"+id+".unity";if(!buildScenes.Any(s=>s.path==path))buildScenes.Add(new UnityEditor.EditorBuildSettingsScene(path,true));}
UnityEditor.EditorBuildSettings.scenes=buildScenes.ToArray();
var assemblies=System.AppDomain.CurrentDomain.GetAssemblies();
var portalType=assemblies.Select(a=>a.GetType("UPlayGround.PortalActor")).First(t=>t!=null);
var arrivalType=assemblies.Select(a=>a.GetType("UPlayGround.SceneArrivalPoint")).First(t=>t!=null);
// 출구 위치, 도착점 위치, 바라보는 방향을 분리하여 즉시 재이동을 방지한다.
System.Action<string,string,string,string,Vector3,Vector3,float> link=(source,target,key,label,exit,arrival,yaw)=>{
    var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName(source);
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
    string rootName="CityTravel_"+key;
    if(scene.GetRootGameObjects().Any(r=>r.name==rootName))return;
    var root=new GameObject(rootName);
    var point=new GameObject("Arrival");point.transform.SetParent(root.transform);point.transform.position=arrival;point.transform.rotation=Quaternion.Euler(0,yaw,0);
    var component=point.AddComponent(arrivalType);var a=new UnityEditor.SerializedObject(component);a.FindProperty("_id").stringValue="worldcities."+source+".from."+target;a.ApplyModifiedPropertiesWithoutUndo();
    var roadSign=new GameObject("TravelSign");roadSign.transform.SetParent(root.transform);roadSign.transform.position=exit;
    int interactionLayer=LayerMask.NameToLayer("InteractableObject");if(interactionLayer<0)throw new System.Exception("상호작용 레이어가 없습니다.");roadSign.layer=interactionLayer;
    var sphere=roadSign.AddComponent<SphereCollider>();sphere.radius=2.2f;sphere.center=new Vector3(0,1,0);sphere.isTrigger=true;
    var portal=roadSign.AddComponent(portalType);var p=new UnityEditor.SerializedObject(portal);
    p.FindProperty("_portalType").enumValueIndex=0;p.FindProperty("_travelOnInteract").boolValue=true;
    p.FindProperty("_targetSceneName").stringValue=target;p.FindProperty("_targetArrivalId").stringValue="worldcities."+target+".from."+source;
    p.FindProperty("_mapLabel").stringValue=label;p.FindProperty("_requiresActivation").boolValue=false;p.FindProperty("_startsActivated").boolValue=true;
    p.FindProperty("_activationId").stringValue="worldcities.travel."+source+"."+target;
    p.FindProperty("_mapArrivalPoint").objectReferenceValue=point.transform;
    // 지역간 출구와 그 지역의 빠른 이동 거점을 섞지 않는다. 현장 상호작용으로 이동한다.
    p.FindProperty("_showOnMap").boolValue=false;p.ApplyModifiedPropertiesWithoutUndo();
    var visual=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Environment/WorldCities/TravelSign.prefab"),scene);
    visual.transform.SetParent(roadSign.transform,false);
    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
    if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene))throw new System.Exception("연결 저장 실패: "+source);
};
link("LakeOfLifeScenario","SaltwindHarbor","Harbor","소금바람항으로 가는 길",new Vector3(1125,51.96f,380),new Vector3(1118,52.5f,388),0);
link("SaltwindHarbor","LakeOfLifeScenario","Lake","생명의 호수로 가는 길",new Vector3(176,10,40),new Vector3(180,10.25f,55),0);
link("LakeOfLifeScenario","StonegateCitadel","Castle","돌등성으로 가는 길",new Vector3(1005,62.68f,430),new Vector3(1009,62.83f,425),90);
link("StonegateCitadel","LakeOfLifeScenario","Lake","생명의 호수로 가는 길",new Vector3(176,10,40),new Vector3(180,10.25f,55),0);
link("SaltwindHarbor","StonegateCitadel","Castle","돌등성으로 가는 길",new Vector3(122,10,133),new Vector3(128,10.25f,133),90);
link("StonegateCitadel","SaltwindHarbor","Harbor","소금바람항으로 가는 길",new Vector3(220,10,102),new Vector3(214,10.25f,102),270);
UnityEditor.AssetDatabase.SaveAssets();
return "지역 2개 등록, 방향별 출구 6개 연결. 기본 시작 맵 보존.";
