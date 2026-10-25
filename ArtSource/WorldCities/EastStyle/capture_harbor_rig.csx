// 다른 열린 씬의 가시성·저장 상태를 바꾸지 않고 항구만 촬영한다.
var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByName("SaltwindHarbor");
if(!scene.IsValid()||Application.isPlaying)throw new System.Exception("항구 Edit Mode가 필요합니다.");
if(UnityEditor.AnimationMode.InAnimationMode())throw new System.Exception("기존 애니메이션 미리보기를 먼저 종료하세요.");
var previous=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
ulong originalMask=UnityEditor.SceneManagement.EditorSceneManager.GetSceneCullingMask(scene);
ulong captureMask=UnityEditor.SceneManagement.EditorSceneManager.CalculateAvailableSceneCullingMask();
var cameraObject=new GameObject("CitadelPreviewCamera");UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraObject,scene);
var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=58;camera.farClipPlane=800;
camera.transform.position=new Vector3(213,44,105);camera.transform.LookAt(new Vector3(180,13,187));
camera.clearFlags=CameraClearFlags.Skybox;
Texture2D texture=null;
var actors=scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<UPlayGround.NpcActor>()).ToArray();
var previews=new System.Collections.Generic.List<GameObject>();
var graphs=new System.Collections.Generic.List<UnityEngine.Playables.PlayableGraph>();
var meshes=new System.Collections.Generic.List<Mesh>();
var visibility=new System.Collections.Generic.Dictionary<Renderer,bool>();
try{
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
    UnityEditor.SceneManagement.EditorSceneManager.SetSceneCullingMask(scene,captureMask);camera.overrideSceneCullingMask=captureMask;
    // 에디터 외형 검수용 포즈다. 런타임 재생이나 작업 모션 검증을 대신하지 않는다.
    var idle=UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Animation.MotionSetAsset>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Npc/Npc_Male_Idle.asset");
    foreach(var actor in actors){
        var preview=UnityEngine.Object.Instantiate(actor.gameObject);previews.Add(preview);preview.name="HarborPosePreview";
        var animator=preview.GetComponent<Animator>();animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;animator.applyRootMotion=false;
        var brain=actor.GetComponent<UPlayGround.Components.NpcBrain>();
        var motion=brain.WorkMotion!=null?brain.WorkMotion:idle;var clip=motion.motionSet.motions.Last().motionClip;
        var graph=UnityEngine.Playables.PlayableGraph.Create("HarborPosePreview");graphs.Add(graph);
        graph.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.Manual);
        var output=UnityEngine.Animations.AnimationPlayableOutput.Create(graph,"Pose",animator);
        var playable=UnityEngine.Animations.AnimationClipPlayable.Create(graph,clip);
        UnityEngine.Playables.PlayableOutputExtensions.SetSourcePlayable(output,playable);
        graph.Play();UnityEngine.Playables.PlayableExtensions.SetTime(playable,Mathf.Min(.6f,clip.length*.5f));graph.Evaluate(0);
        preview.transform.SetPositionAndRotation(actor.transform.position,actor.transform.rotation);
        foreach(var skin in preview.GetComponentsInChildren<SkinnedMeshRenderer>()){
            if(!skin.enabled)continue;
            var mesh=new Mesh();skin.BakeMesh(mesh);meshes.Add(mesh);
            var view=new GameObject("HarborPoseMesh");view.transform.SetParent(skin.transform,false);
            view.AddComponent<MeshFilter>().sharedMesh=mesh;view.AddComponent<MeshRenderer>().sharedMaterials=skin.sharedMaterials;skin.enabled=false;
        }
        foreach(var renderer in actor.GetComponentsInChildren<Renderer>()){visibility.Add(renderer,renderer.enabled);renderer.enabled=false;}
    }
    var utility=System.AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("UPlayGround.Tool.Editor.MinimapCaptureUtility")).First(t=>t!=null);
    var positions=new[]{new Vector3(184.7f,11.5f,161.2f),new Vector3(145,11.5f,217)};
    var targets=new[]{new Vector3(188.7f,11.2f,161.2f),new Vector3(145,11.1f,221.25f)};
    var names=new[]{"merchant-animated","cargo-worker-animated"};
    for(int i=0;i<names.Length;i++){
        camera.transform.position=positions[i];camera.transform.LookAt(targets[i]);
        texture=(Texture2D)utility.GetMethod("Render").Invoke(null,new object[]{camera,new Vector2Int(1440,900),true,800f});
        System.IO.File.WriteAllBytes("output/HarborEast/After/Posed/"+names[i]+".png",texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);texture=null;
    }
    return "항구 두 주민 PlayableGraph 포즈 캡처. 런타임 검증과 구분.";}finally{
    foreach(var graph in graphs)if(graph.IsValid())graph.Destroy();
    foreach(var preview in previews)if(preview!=null)UnityEngine.Object.DestroyImmediate(preview);
    foreach(var mesh in meshes)UnityEngine.Object.DestroyImmediate(mesh);
    foreach(var pair in visibility)if(pair.Key!=null)pair.Key.enabled=pair.Value;
    if(texture!=null)UnityEngine.Object.DestroyImmediate(texture);
    UnityEngine.Object.DestroyImmediate(cameraObject);
    UnityEditor.SceneManagement.EditorSceneManager.SetSceneCullingMask(scene,originalMask);
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
}
