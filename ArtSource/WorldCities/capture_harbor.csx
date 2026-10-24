var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("SaltwindHarbor");
if(!scene.IsValid()||UnityEditor.EditorApplication.isPlaying||UnityEditor.AnimationMode.InAnimationMode())throw new System.Exception("항구 Edit Mode에서 다른 애니메이션 미리보기를 종료하세요.");
UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
var utility=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
var cameraObject=new GameObject("HarborCapture");var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=65;camera.farClipPlane=800;
camera.scene=scene;
ulong originalMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(scene);
ulong captureMask=UnityEditor.SceneManagement.EditorSceneManager.CalculateAvailableSceneCullingMask();
UnityEditor.SceneManagement.EditorSceneManager.SetSceneCullingMask(scene,captureMask);
camera.overrideSceneCullingMask=captureMask;
camera.clearFlags=CameraClearFlags.Skybox;
var positions=new[]{new Vector3(180,12.5f,139),new Vector3(174,12.5f,181),new Vector3(173,14,223),new Vector3(235,95,102),new Vector3(178,12,150),new Vector3(151,12.3f,225),new Vector3(205,12,230)};
var targets=new[]{new Vector3(181,13.2f,180),new Vector3(180,12,218),new Vector3(153,15,249),new Vector3(175,10,194),new Vector3(170,11.3f,155),new Vector3(145,11.3f,220),new Vector3(199,11.2f,224)};
var names=new[]{"market-entry","market-life","working-harbor","overview","merchant","cargo-worker","meal"};
System.IO.Directory.CreateDirectory("output/SaltwindHarbor");
try{
    var animators=scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.NpcActor>()).Select(n=>n.GetComponentInChildren<Animator>()).Where(a=>a!=null).ToArray();
    var localPositions=animators.Select(a=>a.transform.localPosition).ToArray();
    var localRotations=animators.Select(a=>a.transform.localRotation).ToArray();
    // 저장되는 배치를 바꾸지 않고 실제 Idle·작업 클립의 한 프레임만 에디터에서 미리 본다.
    UnityEditor.AnimationMode.StartAnimationMode();
    UnityEditor.AnimationMode.BeginSampling();
    foreach(var npc in scene.GetRootGameObjects().SelectMany(g=>g.GetComponentsInChildren<UPlayGround.NpcActor>())){
        var animator=npc.GetComponentInChildren<Animator>();var brain=npc.GetComponent<UPlayGround.Components.NpcBrain>();
        var motion=brain.WorkMotion!=null?brain.WorkMotion:UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Male_Idle.asset");
        var so=new UnityEditor.SerializedObject(motion);var clip=(AnimationClip)so.FindProperty("motionSet.motions").GetArrayElementAtIndex(0).FindPropertyRelative("motionClip").objectReferenceValue;
        if(animator!=null&&clip!=null)UnityEditor.AnimationMode.SampleAnimationClip(animator.gameObject,clip,Mathf.Min(1f,clip.length*.5f));
    }
    UnityEditor.AnimationMode.EndSampling();
    // Humanoid 미리보기는 루트를 원점으로 옮기므로 씬 배치를 다시 적용한다.
    for(int i=0;i<animators.Length;i++){animators[i].transform.localPosition=localPositions[i];animators[i].transform.localRotation=localRotations[i];}
    for(int i=0;i<names.Length;i++){
        camera.transform.position=positions[i];camera.transform.LookAt(targets[i]);
        var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1440,900),true,800f});
        try{System.IO.File.WriteAllBytes("output/SaltwindHarbor/"+names[i]+".png",texture.EncodeToPNG());}finally{UnityEngine.Object.DestroyImmediate(texture);}
    }
}finally{if(UnityEditor.AnimationMode.InAnimationMode())UnityEditor.AnimationMode.StopAnimationMode();UnityEditor.SceneManagement.EditorSceneManager.SetSceneCullingMask(scene,originalMask);UnityEngine.Object.DestroyImmediate(cameraObject);}
return "항구 환경·주민 7개 시점 Editor 미리보기";
