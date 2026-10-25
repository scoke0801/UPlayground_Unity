// 공유 에디터 사용 순서를 받은 뒤 실행한다. 기존 씬을 닫거나 저장하지 않는다.
if (Application.isPlaying) throw new System.Exception("Edit Mode에서만 조립합니다.");
string art = "Assets/08.Models/WorldCities/EastStyle";
string materials = "Assets/10.Datas/World/WorldCities/EastStyle";
string prefabs = "Assets/03.Prefabs/Environment/WorldCities/EastStyle";
string targetScene = "Assets/01.Scenes/ArtReview/EastStyleToneReview.unity";
if (System.IO.File.Exists(targetScene)) throw new System.Exception("기존 검수 씬을 덮어쓰지 않습니다.");
System.IO.Directory.CreateDirectory(art);
System.IO.Directory.CreateDirectory(materials);
System.IO.Directory.CreateDirectory(prefabs);
System.IO.Directory.CreateDirectory("Assets/01.Scenes/ArtReview");
foreach (string file in new[]{"EastRoof_Straight_2m.fbx","EastRoof_Corner_3p2m.fbx","EastRoof_Ridge_2m.fbx","EastRoofClay.png"})
    System.IO.File.Copy("ArtSource/WorldCities/EastStyle/"+file,art+"/"+file,false);
UnityEditor.AssetDatabase.Refresh();
var clay = new Material(UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/ExternalAssets/Environment/Toon Golden Valley/Models/Materials/TGV_Atlas_1A.mat"));
clay.name="EastRoofClay";
clay.shader=UnityEditor.AssetDatabase.LoadAssetAtPath<Shader>("Assets/ExternalAssets/Environment/Toon Golden Valley/Shaders/TGV_CustomToon.shader");
clay.SetTexture("_TextureSample",UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(art+"/EastRoofClay.png"));
UnityEditor.AssetDatabase.CreateAsset(clay,materials+"/EastRoofClay.mat");
var timber = new Material(clay);timber.name="EastRoofTimber";
var woodColor = new Texture2D(2,2);woodColor.SetPixels(new[]{new Color(.30f,.19f,.11f),new Color(.30f,.19f,.11f),new Color(.30f,.19f,.11f),new Color(.30f,.19f,.11f)});woodColor.Apply();
System.IO.File.WriteAllBytes(art+"/TimberColor.png",woodColor.EncodeToPNG());UnityEngine.Object.DestroyImmediate(woodColor);
UnityEditor.AssetDatabase.ImportAsset(art+"/TimberColor.png");
timber.SetTexture("_TextureSample",UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>(art+"/TimberColor.png"));
UnityEditor.AssetDatabase.CreateAsset(timber,materials+"/EastRoofTimber.mat");
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
var house=new GameObject("EastStyle_TGV_TileHouse_Prototype");
string kit="Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Buildings/Building Kit/";
System.Func<string,Vector3,float,GameObject> piece=(name,position,angle)=>{
    var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(kit+name+".prefab");
    if(source==null)throw new System.Exception("TGV 부품 없음: "+name);
    var obj=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source,scene);
    obj.transform.SetParent(house.transform,false);obj.transform.localPosition=position;obj.transform.localRotation=Quaternion.Euler(0,angle,0);
    return obj;
};
// 원본 2m 벽의 길이는 local Z이다. 문과 창은 원본 슬롯으로 연결한다.
for(int i=0;i<4;i++){
    piece(i==1?"TGV_Wall_06A":"TGV_Wall_04A",new Vector3(-3+i*2,.4f,-2),90);
    piece("TGV_Wall_02A",new Vector3(-3+i*2,.4f,2),90);
}
for(int i=0;i<2;i++){
    piece("TGV_Wall_04A",new Vector3(-4,.4f,-1+i*2),0);
    piece("TGV_Wall_02A",new Vector3(4,.4f,-1+i*2),0);
}
for(int x=0;x<4;x++)for(int z=0;z<2;z++){
    var foundation=piece("TGV_Foundation_01A",new Vector3(-3+x*2,-.1f,-1+z*2),0);
}
System.Func<string,Vector3,float,GameObject> roof=(name,position,angle)=>{
    var source=UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(art+"/"+name+".fbx");
    if(source==null)throw new System.Exception("지붕 모델 없음: "+name);
    var obj=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(source,scene);
    obj.transform.SetParent(house.transform,false);obj.transform.localPosition=position;
    // FBX 루트의 축 보정을 보존하고 Unity 좌표계에서 조립 방향을 적용한다.
    obj.transform.localRotation=Quaternion.Euler(0,angle+180,0)*source.transform.localRotation;
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(obj.transform);
    foreach(var renderer in obj.GetComponentsInChildren<MeshRenderer>()){
        var slots=renderer.sharedMaterials;
        for(int i=0;i<slots.Length;i++)slots[i]=i==0?clay:timber;
        renderer.sharedMaterials=slots;
        UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
    }
    return obj;
};
// Blender +Y는 Unity +Z로 임포트되는지 bounds 검사 후 조립 결과를 검수한다.
roof("EastRoof_Straight_2m",new Vector3(-2,3.4f,0),0);
roof("EastRoof_Straight_2m",new Vector3(0,3.4f,0),0);
roof("EastRoof_Straight_2m",new Vector3(2,3.4f,0),180);
roof("EastRoof_Straight_2m",new Vector3(0,3.4f,0),180);
roof("EastRoof_Corner_3p2m",new Vector3(2,3.4f,0),0);
roof("EastRoof_Corner_3p2m",new Vector3(2,3.4f,0),90);
roof("EastRoof_Corner_3p2m",new Vector3(-2,3.4f,0),180);
roof("EastRoof_Corner_3p2m",new Vector3(-2,3.4f,0),270);
roof("EastRoof_Ridge_2m",new Vector3(-2,5.29f,0),0);
roof("EastRoof_Ridge_2m",new Vector3(0,5.29f,0),0);
UnityEditor.PrefabUtility.SaveAsPrefabAsset(house,prefabs+"/EastStyle_TileHouse_Prototype.prefab");
house.transform.position=new Vector3(6,0,0);
var reference=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ExternalAssets/Environment/Toon Golden Valley/Prefabs/Buildings/Presets/House_01.prefab"),scene);
reference.name="Reference_TGV_ThatchedHouse";reference.transform.position=new Vector3(-7,0,0);reference.transform.rotation=Quaternion.Euler(0,90,0);
var actor=(GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab"),scene);
actor.name="Reference_lilToon_Raon";actor.transform.position=new Vector3(.5f,0,-6);actor.transform.rotation=Quaternion.Euler(0,180,0);
foreach(var behaviour in actor.GetComponentsInChildren<MonoBehaviour>(true))behaviour.enabled=false;
actor.SetActive(true);
foreach(Transform child in actor.transform)if(child.name=="Weapon"||child.name=="Weapon_back")child.gameObject.SetActive(false);
var ground=GameObject.CreatePrimitive(PrimitiveType.Plane);ground.name="NeutralGround";ground.transform.localScale=new Vector3(10,1,10);
var groundMaterial=new Material(Shader.Find("Universal Render Pipeline/Lit"));groundMaterial.color=new Color(.33f,.37f,.31f);groundMaterial.SetFloat("_Smoothness",0);
UnityEditor.AssetDatabase.CreateAsset(groundMaterial,materials+"/ReviewGround.mat");ground.GetComponent<Renderer>().sharedMaterial=groundMaterial;
var sun=new GameObject("Review_Sun").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=1.4f;sun.color=new Color(1,.96f,.88f);sun.shadows=LightShadows.Soft;sun.transform.rotation=Quaternion.Euler(43,-30,0);
RenderSettings.sun=sun;RenderSettings.ambientMode=UnityEngine.Rendering.AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.60f,.68f,.8f);RenderSettings.ambientEquatorColor=new Color(.44f,.46f,.44f);RenderSettings.ambientGroundColor=new Color(.3f,.28f,.24f);RenderSettings.fog=false;
var camera=new GameObject("ReviewCamera").AddComponent<Camera>();camera.transform.position=new Vector3(0,4,-25);camera.transform.LookAt(new Vector3(0,3,0));camera.fieldOfView=50;camera.nearClipPlane=.1f;camera.farClipPlane=150;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.52f,.63f,.74f);
foreach(var root in scene.GetRootGameObjects())foreach(var component in root.GetComponentsInChildren<Component>(true)){
    if(component==null)continue;
    UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(component);
    if(component is Transform)UnityEditor.PrefabUtility.RecordPrefabInstancePropertyModifications(component.gameObject);
}
UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,targetScene);
UnityEditor.AssetDatabase.SaveAssets();
var report=new System.Text.StringBuilder();
foreach(var renderer in house.GetComponentsInChildren<MeshRenderer>())if(renderer.sharedMaterials.Any(m=>m==null||m.shader==null))report.AppendLine("잘못된 재질: "+renderer.name);
report.AppendLine("검수 씬 생성: "+targetScene);
report.AppendLine("원래 열린 씬 유지: "+previous.path);
return report.ToString();
