const string reportFolder = "output/FrankWeaponRetarget";
System.IO.Directory.CreateDirectory(reportFolder);
var checks = new System.Collections.Generic.List<string>();
int failures = 0;
System.Action<string, bool, string> check = (id, passed, detail) => {
    checks.Add((passed ? "PASS " : "FAIL ") + id + " " + detail);
    if (!passed) failures++;
};
var paths = UnityEditor.AssetDatabase.FindAssets("t:AnimationClip", new[] { "Assets/07.Animations/Player/Raon/FrankWeapon" }).Select(UnityEditor.AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToArray();
var root = UnityEditor.PrefabUtility.LoadPrefabContents("Assets/03.Prefabs/Actor/Player/Models/PlayerModel_Raon.prefab");
try {
    foreach (var b in root.GetComponentsInChildren<UnityEngine.MonoBehaviour>(true)) if (b != null) b.enabled = false;
    root.SetActive(true);
    var animator = root.GetComponent<UnityEngine.Animator>();
    animator.enabled = true;
    animator.fireEvents = false;
    animator.applyRootMotion = false;
    animator.runtimeAnimatorController = null;
    animator.cullingMode = UnityEngine.AnimatorCullingMode.AlwaysAnimate;
    var animancer = root.GetComponent<Animancer.AnimancerComponent>() ?? root.AddComponent<Animancer.AnimancerComponent>();
    animancer.Animator = animator;
    animancer.enabled = true;
    var socket = root.transform.Find("Weapon/Katana");
    var constraint = socket.GetComponent<UnityEngine.Animations.ParentConstraint>();
    var source = constraint.GetSource(0).sourceTransform;
    var initialScale = socket.localScale;
    var idleSet = UnityEditor.AssetDatabase.LoadAssetAtPath<UPlayGround.Data.Actor.Animation.ActorAnimationMotionSet>("Assets/10.Datas/Actor/Animation/ActorMotion/MotionSet/Player/Bokusei_KatanaAnimationSet.asset");
    var idle = idleSet.motionSlots[UPlayGround.Data.Actor.Animation.MotionTags.Idle].motionSet.motions[0].motionClip;
    check("missing-scripts", root.GetComponentsInChildren<UnityEngine.Transform>(true).Sum(t => UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject)) == 0, "RAON 프리팹");
    foreach (var path in paths) {
        var clip = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.AnimationClip>(path);
        var originalPath = "Assets/ExternalAssets/AnimationOnly/Frank_Slash_Pack/Assets/Animations/Frank_SlashPack_Katana/FBX_Animation/Root_Motion/" + clip.name.Substring(5) + ".FBX";
        if (!System.IO.File.Exists(originalPath)) originalPath = originalPath.Replace("/Root_Motion/", "/In_Place/");
        var original = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(originalPath).OfType<UnityEngine.AnimationClip>().Single(c => !c.name.StartsWith("__"));
        var bindings = UnityEditor.AnimationUtility.GetCurveBindings(clip);
        check(clip.name + ".bindings", bindings.Where(b => b.type == typeof(UnityEngine.Transform)).All(b => root.transform.Find(b.path) != null), "Transform 커브 경로");
        check(clip.name + ".body", clip.isHumanMotion && UnityEngine.Mathf.Abs(clip.length - original.length) < 0.0001f, "Humanoid/재생 길이 보존");
        bool sameBody = true;
        foreach (var b in UnityEditor.AnimationUtility.GetCurveBindings(original).Where(b => b.type == typeof(UnityEngine.Animator))) {
            var a = UnityEditor.AnimationUtility.GetEditorCurve(original, b);
            var c = UnityEditor.AnimationUtility.GetEditorCurve(clip, b);
            if (c == null || !a.keys.SequenceEqual(c.keys)) sameBody = false;
        }
        check(clip.name + ".muscles", sameBody, "몸/루트 커브 원본 일치");
        var state = animancer.Play(clip);
        animancer.Graph.PauseGraph();
        float maxScale = 0;
        bool finite = true;
        float displacement = 0;
        UnityEngine.Vector3 firstPosition = source.position;
        for (int i = 0; i <= UnityEngine.Mathf.CeilToInt(clip.length * 60); i++) {
            state.Time = UnityEngine.Mathf.Min(i / 60f, clip.length);
            animancer.Evaluate(0);
            maxScale = UnityEngine.Mathf.Max(maxScale, socket.localScale.x);
            finite &= !float.IsNaN(source.position.x) && !float.IsInfinity(source.position.x);
            displacement = UnityEngine.Mathf.Max(displacement, UnityEngine.Vector3.Distance(firstPosition, source.position));
        }
        check(clip.name + ".sample", finite && displacement > 0.05f, "60fps 무기 커브 평가. 최대 스케일=" + maxScale + "; 이동=" + displacement + "m. Constraint 패스는 Player에서 별도 검증.");
        if (clip.name.Contains("S3_Skill01")) check("skill-growth", maxScale > 2.9f && maxScale < 3.1f, "원본 칼날 확대 전달");
        var next = animancer.Play(idle);
        next.Time = 0.3f;
        animancer.Evaluate(0);
        check(clip.name + ".reset", UnityEngine.Vector3.Distance(socket.localScale, initialScale) < 0.001f, "대기 전환 스케일=" + socket.localScale);
    }
    check("trail-base", root.transform.Find("Weapon/Katana/Weapon_Katana_01_Blade/Line Bottom") != null, "실제 무기 하위");
    check("trail-tip", root.transform.Find("Weapon/Katana/Weapon_Katana_01_Blade/Line Tip") != null, "실제 무기 하위");
    check("hitbox", root.transform.Find("Weapon/Katana/[HitBox] MainWeapon") != null, "실제 무기 하위");
}
finally {
    foreach (var player in root.GetComponentsInChildren<Animancer.AnimancerComponent>(true))
        if (player.IsGraphInitialized) player.Graph.Destroy();
    UnityEditor.PrefabUtility.UnloadPrefabContents(root);
    System.IO.File.WriteAllLines(reportFolder + "/asset-validation.txt", checks.Concat(new[] { "Failures=" + failures }).ToArray());
}
return new { failures, checks = checks.Count };
