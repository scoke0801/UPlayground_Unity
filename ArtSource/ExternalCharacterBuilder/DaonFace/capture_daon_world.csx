var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath("Assets/01.Scenes/Scenario/LakeOfLifeScenario.unity");
var actor=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.NpcActor>(true)).Single(a=>new UnityEditor.SerializedObject(a).FindProperty("_actorId").stringValue=="Npc_Daon");
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var skins=actor.GetComponentsInChildren<UnityEngine.Renderer>(true);var enabled=skins.Select(r=>r.enabled).ToArray();
var clone=UnityEngine.Object.Instantiate(actor.gameObject);UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(clone,scene);
var cameraObject=new UnityEngine.GameObject("Daon validation camera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
System.IDisposable pose=null;UnityEngine.RenderTexture target=null;var previousTarget=UnityEngine.RenderTexture.active;
try
{
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
    for(int i=0;i<skins.Length;i++)skins[i].enabled=false;
    var helper=typeof(UPlayGround.Editor.P09Builder.PrefabBuildPipeline).Assembly.GetType("UPlayGround.Editor.P09Builder.P09PosePreview");
    pose=(System.IDisposable)System.Activator.CreateInstance(helper,new object[]{clone});
    var motion=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Female_Idle.asset");
    helper.GetMethod("Sample").Invoke(pose,new object[]{motion,.35f,null});
    foreach(var filter in clone.GetComponentsInChildren<UnityEngine.MeshFilter>())if(filter.name=="P09 Pose Preview")filter.sharedMesh.RecalculateBounds();
    var camera=cameraObject.AddComponent<UnityEngine.Camera>();camera.enabled=false;camera.overrideSceneCullingMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(scene);
    camera.nearClipPlane=.03f;camera.farClipPlane=1000;camera.clearFlags=UnityEngine.CameraClearFlags.Skybox;camera.fieldOfView=33;
    var focus=actor.transform.position+UnityEngine.Vector3.up*.9f;camera.transform.position=focus+actor.transform.forward*3.4f+actor.transform.right*.8f;camera.transform.LookAt(focus);
    target=new UnityEngine.RenderTexture(1024,1024,24,UnityEngine.RenderTextureFormat.ARGB32);camera.targetTexture=target;camera.Render();UnityEngine.RenderTexture.active=target;
    var texture=new UnityEngine.Texture2D(1024,1024,UnityEngine.TextureFormat.RGBA32,false);texture.ReadPixels(new UnityEngine.Rect(0,0,1024,1024),0,0);texture.Apply();
    System.IO.File.WriteAllBytes("output/DaonProductionIntegration/World_Lake_Daon.png",texture.EncodeToPNG());UnityEngine.Object.DestroyImmediate(texture);
    return "output/DaonProductionIntegration/World_Lake_Daon.png";
}
finally
{
    if(pose!=null)pose.Dispose();for(int i=0;i<skins.Length;i++)if(skins[i]!=null)skins[i].enabled=enabled[i];
    UnityEngine.RenderTexture.active=previousTarget;if(target!=null)UnityEngine.Object.DestroyImmediate(target);
    UnityEngine.Object.DestroyImmediate(cameraObject);UnityEngine.Object.DestroyImmediate(clone);UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
}
