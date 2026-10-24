// 원래 열린 미저장 씬을 유지한 채 별도 검수 씬과 반복 재생 컨트롤러를 만든다.
string folder = "Assets/08.Models/Equipment/P09RangerVest";
string controllerPath = folder + "/RangerVest_Review.controller";
var controller = AssetDatabase.LoadAssetAtPath<UnityEditor.Animations.AnimatorController>(controllerPath);
if (controller == null)
{
    controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
    var machine = controller.layers[0].stateMachine;
    string prefix = "Assets/ExternalAssets/AnimationOnly/RPG_Animations_Pack/FBX_Animations/";
    string[] paths = { "Base Move/Walk/Base/InPlace/Walk_F_InPlace.fbx", "Base Move/Run/Base/InPlace/Run_F_InPlace.fbx", "Base Move/Crouch/Base/InPlace/Crouch_F_InPlace.fbx", "One Hand Base/Attack_A/OneHand_Base_Attack_A_1.fbx" };
    string[] labels = { "걷기", "달리기", "웅크리기", "공격 자세" };
    var states = new UnityEditor.Animations.AnimatorState[4];
    for (int i = 0; i < paths.Length; i++)
    {
        AnimationClip clip = null;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(prefix + paths[i]))
            if (asset is AnimationClip && !asset.name.StartsWith("__preview__")) { clip = (AnimationClip)asset; break; }
        if (clip == null) throw new System.InvalidOperationException("검수 클립 누락");
        states[i] = machine.AddState(labels[i], new Vector3(i * 220, 0, 0));
        states[i].motion = clip;
    }
    machine.defaultState = states[0];
    for (int i = 0; i < states.Length; i++)
    {
        var transition = states[i].AddTransition(states[(i + 1) % states.Length]);
        transition.hasExitTime = true; transition.exitTime = i == 3 ? 1 : 3;
        transition.hasFixedDuration = true; transition.duration = 0.2f;
    }
    EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(controller);
}
// 실제 전투 수신자가 없는 모델 검수에서는 원본 이벤트를 실행하지 않는다.
int clipIndex = 0;
foreach (var entry in controller.layers[0].stateMachine.states)
{
    var sourceClip = entry.state.motion as AnimationClip;
    string clipPath = folder + "/Review_" + clipIndex++ + ".anim";
    var previewClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
    if (previewClip == null)
    {
        previewClip = UnityEngine.Object.Instantiate(sourceClip);
        AnimationUtility.SetAnimationEvents(previewClip, new AnimationEvent[0]);
        AssetDatabase.CreateAsset(previewClip, clipPath);
    }
    entry.state.motion = previewClip; EditorUtility.SetDirty(entry.state);
}
EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(controller);
var previousScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
try
{
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(scene);
    var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/EquipmentReview/P09_RangerVest_Model.prefab"), scene);
    var animator = model.GetComponent<Animator>();
    animator.runtimeAnimatorController = controller; animator.applyRootMotion = false;
    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    PrefabUtility.RecordPrefabInstancePropertyModifications(animator);
    var camera = new GameObject("Main Camera").AddComponent<Camera>();
    camera.tag = "MainCamera"; camera.gameObject.AddComponent<AudioListener>();
    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.13f,0.15f,0.16f);
    camera.transform.position = new Vector3(0.8f,1.2f,2.7f);
    camera.transform.LookAt(new Vector3(0,0.82f,0)); camera.fieldOfView = 38;
    var light = new GameObject("Key Light").AddComponent<Light>();
    light.type = LightType.Directional; light.intensity = 1.25f;
    light.transform.rotation = Quaternion.Euler(35,210,0);
    var fill = new GameObject("Fill Light").AddComponent<Light>();
    fill.type = LightType.Directional; fill.intensity = 0.4f;
    fill.transform.rotation = Quaternion.Euler(20,100,0);
    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
    RenderSettings.ambientLight = new Color(0.6f,0.6f,0.6f);
    string sceneFolder = "Assets/01.Scenes/Tests/Equipment";
    System.IO.Directory.CreateDirectory(sceneFolder); AssetDatabase.Refresh();
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, sceneFolder + "/P09_RangerVest_Review.unity"))
        throw new System.InvalidOperationException("검수 씬 저장 실패");
}
finally
{
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previousScene);
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
}
return "Assets/01.Scenes/Tests/Equipment/P09_RangerVest_Review.unity";
