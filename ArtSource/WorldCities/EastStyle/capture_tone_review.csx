// 검수 씬만 촬영한다. 다른 씬은 저장하거나 가시성을 수정하지 않는다.
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("EastStyleToneReview");
if(!scene.IsValid()||Application.isPlaying)throw new System.Exception("톤 검수 씬 Edit Mode가 필요합니다.");
var camera=scene.GetRootGameObjects().SelectMany(o=>o.GetComponentsInChildren<Camera>()).First();
var originalPosition=camera.transform.position;var originalRotation=camera.transform.rotation;
ulong originalMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(scene);
ulong mask=UnityEditor.SceneManagement.EditorSceneManager.CalculateAvailableSceneCullingMask();
var captures=new[]{
    new {name="comparison_front",position=new Vector3(0,4,-25),target=new Vector3(0,3,0)},
    new {name="comparison_45",position=new Vector3(23,10,-23),target=new Vector3(0,2.6f,0)},
    new {name="comparison_eye",position=new Vector3(0,1.65f,-19),target=new Vector3(0,2.6f,0)}
};
var actor=scene.GetRootGameObjects().First(o=>o.name=="Reference_lilToon_Raon");
var animator=actor.GetComponent<Animator>();
UnityEngine.Playables.PlayableGraph graph=default(UnityEngine.Playables.PlayableGraph);
try{
    UnityEditor.SceneManagement.EditorSceneManager.SetSceneCullingMask(scene,mask);camera.overrideSceneCullingMask=mask;
    if(animator!=null){
        var motion=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Common/Raon_Common_Idle.asset");
        var clip=motion.motionSet.motions.First().motionClip;
        animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        graph=UnityEngine.Playables.PlayableGraph.Create("EastStyleToneReviewPose");graph.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.Manual);
        var output=UnityEngine.Animations.AnimationPlayableOutput.Create(graph,"Pose",animator);
        var playable=UnityEngine.Animations.AnimationClipPlayable.Create(graph,clip);
        UnityEngine.Playables.PlayableOutputExtensions.SetSourcePlayable(output,playable);graph.Play();UnityEngine.Playables.PlayableExtensions.SetTime(playable,.25);graph.Evaluate(0);
    }
    var utility=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
    foreach(var view in captures){
        camera.transform.position=view.position;camera.transform.LookAt(view.target);
        var texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1600,1000),true,150f});
        try{System.IO.File.WriteAllBytes("output/CityToneBoard/"+view.name+".png",texture.EncodeToPNG());}
        finally{UnityEngine.Object.DestroyImmediate(texture);}
    }
    return "정면·45도·눈높이 Editor 비교 캡처 저장";
}finally{
    if(graph.IsValid())graph.Destroy();
    camera.transform.SetPositionAndRotation(originalPosition,originalRotation);camera.overrideSceneCullingMask=originalMask;
    UnityEditor.SceneManagement.EditorSceneManager.SetSceneCullingMask(scene,originalMask);
}
