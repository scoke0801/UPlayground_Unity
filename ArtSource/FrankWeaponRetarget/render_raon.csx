var root = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab");
var preview = new UnityEditor.PreviewRenderUtility();
try {
    foreach (var b in root.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true)) if (b != null) {
        b.enabled = false;
        if (b is UPlayGround.Animation.ActorAnimator) UnityEngine.Object.DestroyImmediate(b);
    }
    root.SetActive(true);
    foreach (var t in root.GetComponentsInChildren<UnityEngine.Transform>(true))
        if (t.name == "InteractionObject") t.gameObject.SetActive(false);
    var animator = root.GetComponent<UnityEngine.Animator>();
    animator.enabled = true;
    animator.fireEvents = false;
    animator.applyRootMotion = false;
    animator.cullingMode = UnityEngine.AnimatorCullingMode.AlwaysAnimate;
    var animancer = root.GetComponent<Animancer.AnimancerComponent>();
    animancer.Animator = animator;
    animancer.enabled = true;
    foreach (var constraint in root.GetComponentsInChildren<UnityEngine.Animations.ParentConstraint>(true)) constraint.enabled = false;
    preview.AddSingleGO(root);
    preview.camera.transform.position = new UnityEngine.Vector3(2.7f, 2.2f, 3.3f);
    preview.camera.transform.LookAt(new UnityEngine.Vector3(0, 1.05f, 0));
    preview.camera.fieldOfView = 35;
    preview.camera.nearClipPlane = 0.01f;
    preview.camera.farClipPlane = 30;
    preview.camera.clearFlags = UnityEngine.CameraClearFlags.SolidColor;
    preview.camera.backgroundColor = new UnityEngine.Color(0.15f, 0.17f, 0.20f);
    preview.lights[0].intensity = 1.4f;
    preview.lights[0].transform.rotation = UnityEngine.Quaternion.Euler(40, 35, 0);
    preview.lights[1].intensity = 0.7f;
    preview.ambientColor = UnityEngine.Color.gray;
    var names = new[] { "S1_Combo01_01", "S3_Skill01", "Stance1_Equip" };
    var times = new[] { new[] { 0.1f, 0.4f, 0.7f }, new[] { 0.3f, 1.1f, 1.4f }, new[] { 0.1f, 0.6f, 1.2f } };
    System.IO.Directory.CreateDirectory("output/FrankWeaponRetarget");
    for (int clipIndex = 0; clipIndex < names.Length; clipIndex++) {
        var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>("Assets/07.Animations/Player/Raon/FrankWeapon/Raon_Frank_RPG_Katana_" + names[clipIndex] + ".anim");
        var state = animancer.Play(clip);
        animancer.Graph.PauseGraph();
        for (int frame = 0; frame < times[clipIndex].Length; frame++) {
            state.Time = times[clipIndex][frame];
            animancer.Evaluate(0);
            // PreviewRenderUtility에는 PlayerLoop의 Constraint 패스가 없으므로 발도 소스를 명시적으로 평가한다.
            var weapon = root.transform.Find("Weapon/Katana");
            var c = weapon.GetComponent<UnityEngine.Animations.ParentConstraint>();
            var source = c.GetSource(0).sourceTransform;
            weapon.SetPositionAndRotation(source.position, source.rotation);
            preview.BeginStaticPreview(new UnityEngine.Rect(0, 0, 900, 900));
            preview.Render(true, true);
            var texture = preview.EndStaticPreview();
            System.IO.File.WriteAllBytes("output/FrankWeaponRetarget/" + names[clipIndex] + "_" + frame + ".png", texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }
}
finally {
    foreach (var player in root.GetComponentsInChildren<Animancer.AnimancerComponent>(true))
        if (player.IsGraphInitialized) player.Graph.Destroy();
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
    preview.Cleanup();
}
return "output/FrankWeaponRetarget";
