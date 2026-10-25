string path="Assets/01.Scenes/Tests/Equipment/Reine_P09_Review.unity";
if(AssetDatabase.LoadMainAssetAtPath(path)!=null)throw new Exception("기존 검수 씬을 보존합니다.");
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene=UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene,UnityEditor.SceneManagement.NewSceneMode.Additive);
try{
var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
var motion=AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
int index=0;
foreach(string name in new[]{"Reine_P09Compatible","Reine_P09_Alternate"}){
var root=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/ExternalModels/"+name+".prefab"),scene);root.transform.position=new Vector3(index++==0?-.65f:.65f,0,0);
using(var pose=(IDisposable)Activator.CreateInstance(helper,new object[]{root})){helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});}
}
var cameraObject=new GameObject("검수 카메라");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);var camera=cameraObject.AddComponent<Camera>();camera.tag="MainCamera";camera.transform.position=new Vector3(0,1.1f,4.2f);camera.transform.LookAt(new Vector3(0,.85f,0));camera.fieldOfView=30;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.18f,.19f,.21f);
var lightObject=new GameObject("주광");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(lightObject,scene);var light=lightObject.AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.2f;light.transform.rotation=Quaternion.Euler(30,150,0);
var fillObject=new GameObject("보조광");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(fillObject,scene);var fill=fillObject.AddComponent<Light>();fill.type=LightType.Directional;fill.intensity=.5f;fill.transform.rotation=Quaternion.Euler(20,-30,0);
if(!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene,path))throw new Exception("검수 씬 저장 실패");
return path;
}finally{UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene,true);}

