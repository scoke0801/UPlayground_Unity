const string path = "Assets/01.Scenes/Tests/Equipment/Raon_FrankWeapon_Review.unity";
if (UnityEditor.EditorApplication.isCompiling || UnityEditor.EditorUtility.scriptCompilationFailed)
    throw new System.InvalidOperationException("Unity 컴파일 오류를 먼저 해결하세요.");
var previous = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
var scene = UnityEditor.SceneManagement.EditorSceneManager.NewScene(UnityEditor.SceneManagement.NewSceneSetup.EmptyScene, UnityEditor.SceneManagement.NewSceneMode.Additive);
try {
    var model = (UnityEngine.GameObject)UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab"));
    model.name = "Raon_WeaponValidation";
    // 서비스/저장/전투 이벤트 없이 실물 리그와 Constraint만 실행하는 검증 씬이다.
    foreach (var b in model.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true).Reverse().ToArray())
        if (b != null && !(b is Animancer.AnimancerComponent)) UnityEngine.Object.DestroyImmediate(b);
    foreach (var particle in model.GetComponentsInChildren<UnityEngine.ParticleSystem>(true))
        particle.gameObject.SetActive(false);
    foreach (var vfx in model.GetComponentsInChildren<UnityEngine.VFX.VisualEffect>(true))
        vfx.gameObject.SetActive(false);
    foreach (var t in model.GetComponentsInChildren<UnityEngine.Transform>(true))
        if (t.name == "InteractionObject") t.gameObject.SetActive(false);
    model.SetActive(true);
    var animator = model.GetComponent<UnityEngine.Animator>();
    animator.runtimeAnimatorController = null;
    animator.applyRootMotion = false;
    animator.fireEvents = false;
    animator.cullingMode = UnityEngine.AnimatorCullingMode.AlwaysAnimate;
    var animancer = model.GetComponent<Animancer.AnimancerComponent>();
    animancer.enabled = true;
    animancer.Animator = animator;
    var camera = new UnityEngine.GameObject("Camera").AddComponent<UnityEngine.Camera>();
    camera.tag = "MainCamera";
    camera.transform.position = new UnityEngine.Vector3(2.7f, 2.2f, 3.3f);
    camera.transform.LookAt(new UnityEngine.Vector3(0, 1.05f, 0));
    camera.fieldOfView = 35;
    camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
    camera.backgroundColor = new UnityEngine.Color(0.15f, 0.17f, 0.20f);
    var light = new UnityEngine.GameObject("Directional Light").AddComponent<UnityEngine.Light>();
    light.type = UnityEngine.LightType.Directional;
    light.intensity = 1.4f;
    light.transform.rotation = UnityEngine.Quaternion.Euler(40, 35, 0);
    UnityEngine.RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
    UnityEngine.RenderSettings.ambientLight = new UnityEngine.Color(0.6f, 0.6f, 0.6f);
    var probe = new UnityEngine.GameObject("WeaponAnimationValidation").AddComponent<UPlayGround.CombatValidation.WeaponAnimationPlayerProbe>();
    var serialized = new UnityEditor.SerializedObject(probe);
    serialized.FindProperty("_animancer").objectReferenceValue = animancer;
    serialized.FindProperty("_attachment").objectReferenceValue = model.transform.Find("Weapon/Katana").GetComponent<UnityEngine.Animations.ParentConstraint>();
    var clips = serialized.FindProperty("_clips");
    var names = new[] { "S1_Combo01_01", "S1_Combo01_02", "S1_Combo01_03", "S3_Skill01", "Stance1_Equip" };
    clips.arraySize = names.Length;
    for (int i = 0; i < names.Length; i++)
        clips.GetArrayElementAtIndex(i).objectReferenceValue = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>("Assets/07.Animations/Player/Raon/FrankWeapon/Raon_Frank_RPG_Katana_" + names[i] + ".anim");
    var set = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.Animation.ActorAnimationMotionSet>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bokusei_KatanaAnimationSet.asset");
    serialized.FindProperty("_idle").objectReferenceValue = set.motionSlots[UPlayGround.Data.Actor.Animation.MotionTags.Idle].motionSet.motions[0].motionClip;
    serialized.ApplyModifiedPropertiesWithoutUndo();
    if (!UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene, path)) throw new System.InvalidOperationException("검증 씬 저장 실패");
    return path;
}
finally {
    UnityEngine.SceneManagement.SceneManager.SetActiveScene(previous);
    UnityEditor.SceneManagement.EditorSceneManager.CloseScene(scene, true);
}
