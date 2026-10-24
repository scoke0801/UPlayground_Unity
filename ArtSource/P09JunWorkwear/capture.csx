// 실제 Unity 재질과 Humanoid 애니메이션을 저장 없는 미리보기 씬에서 검사한다.
string captureFolder = "output/P09JunWorkwear/FeedbackRevision/";
System.IO.Directory.CreateDirectory(captureFolder);
var allReports = new System.Collections.Generic.List<object>();
foreach(string actorId in new[]{"NPC_Jun_Workwear"})
{
var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/03.Prefabs/Characters/EquipmentReview/JunWorkwear/"+actorId+"_Model.prefab");
var preview = new PreviewRenderUtility();
var root = UnityEngine.Object.Instantiate(prefab);
preview.AddSingleGO(root);
var graph = UnityEngine.Playables.PlayableGraph.Create("RangerVestReview");
var meshes = new System.Collections.Generic.List<Mesh>();
var skins = new System.Collections.Generic.List<SkinnedMeshRenderer>();
var report = new System.Collections.Generic.List<string>();
Bounds? sampledBounds = null;
try
{
    var animator = root.GetComponent<Animator>();
    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    animator.applyRootMotion = false;
    foreach (var skin in root.GetComponentsInChildren<SkinnedMeshRenderer>())
    {
        if (!skin.enabled) continue;
        var mesh = new Mesh();
        // 정점 검사는 BakeMesh로 수행하고 화면은 실제 SkinnedMeshRenderer로 그린다.
        skin.updateWhenOffscreen=false;
        skin.forceMatrixRecalculationPerRender=true;
        skins.Add(skin); meshes.Add(mesh);
    }
    var camera = preview.camera;
    camera.clearFlags = CameraClearFlags.SolidColor;
    camera.backgroundColor = new Color(0.13f, 0.15f, 0.16f);
    camera.orthographic = true;
    camera.orthographicSize = 0.88f;
    camera.nearClipPlane = 0.01f; camera.farClipPlane = 20;
    preview.ambientColor = new Color(0.65f, 0.65f, 0.65f);
    preview.lights[0].intensity = 1.2f; preview.lights[1].intensity = 0.6f;
    System.Action<string, float> capture = (name, yaw) => {
        foreach (var constraint in root.GetComponentsInChildren<UnityEngine.Animations.ParentConstraint>(true))
        {
            if (!constraint.enabled || !constraint.constraintActive || constraint.sourceCount != 1) continue;
            var source = constraint.GetSource(0).sourceTransform;
            if (source != null) constraint.transform.SetPositionAndRotation(source.TransformPoint(constraint.GetTranslationOffset(0)), source.rotation * Quaternion.Euler(constraint.GetRotationOffset(0)));
        }
        for (int i = 0; i < skins.Count; i++)
        {
            skins[i].BakeMesh(meshes[i]);
            foreach (var v in meshes[i].vertices)
            {
                if (float.IsNaN(v.x) || float.IsInfinity(v.x) || float.IsNaN(v.y) || float.IsInfinity(v.y) || float.IsNaN(v.z) || float.IsInfinity(v.z))
                    throw new System.InvalidOperationException("비정상 변형 정점");
                if (skins[i].transform.IsChildOf(root.transform.Find("AuthoredJunWorkwear")))
                {
                    var point = skins[i].rootBone.InverseTransformPoint(skins[i].transform.TransformPoint(v));
                    if (!skins[i].localBounds.Contains(point))
                        throw new System.InvalidOperationException("의상 컬링 범위 이탈: " + name);
                    var bounds = sampledBounds ?? new Bounds(point, Vector3.zero);
                    bounds.Encapsulate(point); sampledBounds = bounds;
                }
            }
        }
        bool closeup=name.StartsWith("Hair_"); camera.orthographicSize=closeup?.17f:.88f; var target=closeup?new Vector3(0,1.465f,0):new Vector3(0,.82f,0);
        camera.transform.position = target + Quaternion.Euler(0, yaw, 0) * Vector3.forward * 3;
        camera.transform.LookAt(target);
        preview.lights[0].transform.rotation = camera.transform.rotation * Quaternion.Euler(25, -35, 0);
        preview.lights[1].transform.rotation = camera.transform.rotation * Quaternion.Euler(-10, 60, 0);
        preview.BeginStaticPreview(new Rect(0, 0, 900, 1100)); preview.Render(true);
        UnityEngine.Object.DestroyImmediate(preview.EndStaticPreview());
        preview.BeginStaticPreview(new Rect(0, 0, 900, 1100)); preview.Render(true);
        var image = preview.EndStaticPreview();
        System.IO.File.WriteAllBytes(captureFolder + actorId + "_" + name + ".png", image.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(image);
    };
    capture("Unity_Front", 20); capture("Unity_Back", 160); capture("Hair_Front",0); capture("Hair_Side",90); capture("Hair_Back",180);
    string animationRoot = "Assets/ExternalAssets/AnimationOnly/RPG_Animations_Pack/FBX_Animations/";
    string[] paths = { "Base Move/Walk/Base/InPlace/Walk_F_InPlace.fbx", "Base Move/Run/Base/InPlace/Run_F_InPlace.fbx", "Base Move/Crouch/Base/InPlace/Crouch_F_InPlace.fbx", "One Hand Base/Attack_A/OneHand_Base_Attack_A_1.fbx", "Assets/07.Animations/LivingWorld/NPC/LW_CarryWalk.anim", "Assets/07.Animations/LivingWorld/NPC/LW_PackSuppliesLoop.anim", "Assets/07.Animations/LivingWorld/NPC/LW_RepairLoop.anim" };
    string[] labels = { "Walk", "Run", "Crouch", "Attack", "Carry", "Pack", "Repair" };
    var output = UnityEngine.Animations.AnimationPlayableOutput.Create(graph, "Pose", animator);
    graph.SetTimeUpdateMode(UnityEngine.Playables.DirectorUpdateMode.Manual);
    for (int k = 0; k < paths.Length; k++)
    {
        AnimationClip clip = null;
        foreach (var asset in AssetDatabase.LoadAllAssetsAtPath(paths[k].StartsWith("Assets/")?paths[k]:animationRoot + paths[k]))
            if (asset is AnimationClip && !asset.name.StartsWith("__preview__")) { clip = (AnimationClip)asset; break; }
        if (clip == null || !clip.humanMotion) throw new System.InvalidOperationException("Humanoid 클립 누락: " + paths[k]);
        var playable = UnityEngine.Animations.AnimationClipPlayable.Create(graph, clip);
        UnityEngine.Playables.PlayableOutputExtensions.SetSourcePlayable(output, playable);
        graph.Play();
        for (int sample = 0; sample < 3; sample++)
        {
            UnityEngine.Playables.PlayableExtensions.SetTime(playable, clip.length * (0.2 + sample * 0.3));
            graph.Evaluate(0);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            capture("Unity_" + labels[k] + "_" + sample, sample == 2 ? 160 : 20);
        }
        report.Add(labels[k] + ": 3 Editor Humanoid samples, finite baked vertices");
        UnityEngine.Playables.PlayableExtensions.Destroy(playable);
    }
    System.IO.File.WriteAllLines(captureFolder + actorId + "_pose-validation.txt", report.ToArray());
    System.IO.File.WriteAllText(captureFolder + actorId + "_sampled-bounds.json", JsonUtility.ToJson(sampledBounds.Value));
}
finally { if (graph.IsValid()) graph.Destroy(); foreach (var mesh in meshes) UnityEngine.Object.DestroyImmediate(mesh); preview.Cleanup(); }
allReports.Add(new { actorId, report, bounds = sampledBounds.Value.ToString() });
}
return allReports;
